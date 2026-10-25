using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	public static partial class SteamRemoteStorage
	{
		class Entry { public byte[] Data; public DateTime Time; }

		static readonly Dictionary<string, Entry> _files = new Dictionary<string, Entry>();
		static readonly Dictionary<string, byte[]> _pending = new Dictionary<string, byte[]>();
		static ulong _quota = 5UL * 1024UL * 1024UL;
		static bool _flushing;
		static bool _cloudEnabledForApp = true;

		public static bool IsCloudEnabledForAccount() { return true; }
		public static bool IsCloudEnabledForApp() { return _cloudEnabledForApp; }
		public static void SetCloudEnabledForApp(bool bEnabled) { _cloudEnabledForApp = bEnabled; }

		public static bool GetQuota(out ulong pnTotalBytes, out ulong puAvailableBytes)
		{
			pnTotalBytes = _quota;
			ulong used = 0;
			foreach (var e in _files.Values) used += (ulong)e.Data.Length;
			puAvailableBytes = used >= _quota ? 0UL : _quota - used;
			return true;
		}

		public static int GetFileCount() { return _files.Count; }

		public static int GetFileNameAndSize(int iFile, out int pnFileSizeInBytes)
		{
			int i = 0;
			foreach (var kv in _files)
			{
				if (i++ == iFile) { pnFileSizeInBytes = kv.Value.Data.Length; return 0;  }
			}
			pnFileSizeInBytes = 0;
			return 0;
		}

		public static string GetFileNameAndSize(int iFile, out uint pnFileSizeInBytes)
		{
			int i = 0;
			foreach (var kv in _files)
			{
				if (i++ == iFile) { pnFileSizeInBytes = (uint)kv.Value.Data.Length; return kv.Key; }
			}
			pnFileSizeInBytes = 0;
			return null;
		}

		public static bool FileExists(string pchFile) { return pchFile != null && _files.ContainsKey(pchFile); }
		public static bool FilePersisted(string pchFile) { return FileExists(pchFile); }
		public static bool FileForget(string pchFile) { return FileExists(pchFile); }

		public static int GetFileSize(string pchFile)
		{
			Entry e;
			return pchFile != null && _files.TryGetValue(pchFile, out e) ? e.Data.Length : 0;
		}

		public static long GetFileTimestamp(string pchFile)
		{
			Entry e;
			return pchFile != null && _files.TryGetValue(pchFile, out e) ? new DateTimeOffset(e.Time).ToUnixTimeSeconds() : 0L;
		}

		public static int FileRead(string pchFile, byte[] pvData, int cubDataToRead)
		{
			Entry e;
			if (pchFile == null || !_files.TryGetValue(pchFile, out e)) return 0;
			var n = Math.Min(e.Data.Length, cubDataToRead);
			Array.Copy(e.Data, pvData, n);
			return n;
		}

		public static bool FileWrite(string pchFile, byte[] pvData, int cubData)
		{
			if (!ValidName(pchFile) || pvData == null) return false;

			ulong existing = 0;
			Entry old;
			if (_files.TryGetValue(pchFile, out old)) existing = (ulong)old.Data.Length;
			ulong used = 0;
			foreach (var e in _files.Values) used += (ulong)e.Data.Length;
			if (used - existing + (ulong)cubData > _quota) return false;

			var copy = new byte[cubData];
			Array.Copy(pvData, copy, cubData);
			_files[pchFile] = new Entry { Data = copy, Time = DateTime.UtcNow };
			_pending[pchFile] = copy;
			Flush();
			return true;
		}

		public static bool FileDelete(string pchFile)
		{
			if (!FileExists(pchFile)) return false;
			_files.Remove(pchFile);
			_pending[pchFile] = null;
			Flush();
			return true;
		}


		public static SteamAPICall_t FileWriteAsync(string pchFile, byte[] pvData, uint cubData)
		{
			var call = Webworks.SNetInternal.ResultBus.NewCall();
			var ok = FileWrite(pchFile, pvData, (int)cubData);
			MainThreadDispatcher.Enqueue(() => Webworks.SNetInternal.ResultBus.Complete(call, new RemoteStorageFileWriteAsyncComplete_t { m_eResult = ok ? EResult.k_EResultOK : EResult.k_EResultFail }));
			return call;
		}

		public static SteamAPICall_t FileReadAsync(string pchFile, uint nOffset, uint cubToRead)
		{
			var call = Webworks.SNetInternal.ResultBus.NewCall();
			MainThreadDispatcher.Enqueue(() => Webworks.SNetInternal.ResultBus.Complete(call, new RemoteStorageFileReadAsyncComplete_t { m_hFileReadAsync = call, m_eResult = FileExists(pchFile) ? EResult.k_EResultOK : EResult.k_EResultFileNotFound, m_nOffset = nOffset, m_cubRead = (uint)GetFileSize(pchFile) }));
			return call;
		}

		public static bool FileReadAsyncComplete(SteamAPICall_t hReadCall, byte[] pvBuffer, uint cubToRead)
		{
			return false;
		}

		static bool ValidName(string name)
		{
			if (string.IsNullOrEmpty(name) || name.Length > 260 || name[0] == '/') return false;
			foreach (char c in name) if (c < ' ' || c == '\\') return false;
			foreach (var part in name.Split('/')) if (part.Length == 0 || part == "." || part == "..") return false;
			return true;
		}


		internal static async Task LoadAsync()
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			// Browser builds keep these files in IndexedDB (WEBWORKS-SAVES) rather than on
			// the server; PollWebStore picks the snapshot up once it finishes opening.
			InitWebStore();
			await Task.Delay(0);
#else
			LoadFromJson(await ApiClient.GetAsync("/RemoteStorage/Files"));
#endif
		}

		internal static void LoadFromJson(string json)
		{
			var d = WebworksJson.Obj(json);
			if (d == null) return;

			double quota = WebworksJson.Num(d, "quota");
			if (quota > 0) _quota = (ulong)quota;

			var list = WebworksJson.Arr(d, "files");
			if (list == null) return;
			foreach (var o in list)
			{
				var f = o as Dictionary<string, object>;
				if (f == null) continue;

				string name = WebworksJson.Str(f, "name");
				string b64 = WebworksJson.Str(f, "data");
				if (name == null || b64 == null || _pending.ContainsKey(name)) continue;

				var time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(WebworksJson.Num(f, "time"));

				// A write made before the store finished loading is newer than the snapshot;
				// keep it rather than rolling the file back to what was on disk a moment ago.
				Entry current;
				if (_files.TryGetValue(name, out current) && current.Time >= time) continue;

				_files[name] = new Entry
				{
					Data = Convert.FromBase64String(b64),
					Time = time,
				};
			}
		}

		static void Flush()
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			FlushWebStore();
#else
			FlushServer();
#endif
		}

#if !(UNITY_WEBGL && !UNITY_EDITOR)
		static async void FlushServer()
		{
			if (_flushing) return;
			_flushing = true;
			try
			{
				while (_pending.Count > 0)
				{
					string name = null;
					foreach (var key in _pending.Keys) { name = key; break; }
					byte[] data = _pending[name];
					_pending.Remove(name);

					try
					{
						string body = data == null
							? "{\"name\":" + WebworksJson.Quote(name) + "}"
							: "{\"name\":" + WebworksJson.Quote(name) + ",\"data\":\"" + Convert.ToBase64String(data) + "\"}";
						var reply = WebworksJson.Obj(await ApiClient.PostAsync(data == null ? "/RemoteStorage/Delete" : "/RemoteStorage/Write", body));
						if (reply != null && WebworksJson.Str(reply, "error") != null)
							Debug.LogWarning("[Webworks] RemoteStorage file '" + name + "' rejected by the server: " + WebworksJson.Str(reply, "error"));
					}
					catch (Exception ex)
					{
						Debug.LogWarning("[Webworks] RemoteStorage sync of '" + name + "' failed: " + ex.Message);
					}
				}
			}
			finally
			{
				_flushing = false;
			}
		}
#endif
	}
}
