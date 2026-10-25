using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Webworks.Internal;

namespace Steamworks
{
	public static class SteamRemoteStorage
	{
		class Entry { public byte[] Data; public DateTime Time; }

		static readonly Dictionary<string, Entry> _files = new Dictionary<string, Entry>();
		static readonly Dictionary<string, byte[]> _pending = new Dictionary<string, byte[]>();
		static ulong _quota = 5UL * 1024UL * 1024UL;
		static bool _flushing;

		public static bool IsCloudEnabled { get { return true; } }
		public static bool IsCloudEnabledForAccount { get { return true; } }
		public static bool IsCloudEnabledForApp { get { return true; } set { } }

		public static ulong QuotaBytes { get { return _quota; } }

		public static ulong QuotaUsedBytes
		{
			get
			{
				ulong total = 0;
				foreach (var e in _files.Values) total += (ulong)e.Data.Length;
				return total;
			}
		}

		public static ulong QuotaRemainingBytes { get { ulong used = QuotaUsedBytes; return used >= _quota ? 0UL : _quota - used; } }

		public static int FileCount { get { return _files.Count; } }
		public static IEnumerable<string> Files { get { return new List<string>(_files.Keys); } }

		public static bool FileExists(string filename) { return filename != null && _files.ContainsKey(filename); }
		public static bool FilePersisted(string filename) { return FileExists(filename); }
		public static bool FileForget(string filename) { return FileExists(filename); }

		public static int FileSize(string filename)
		{
			Entry e;
			return filename != null && _files.TryGetValue(filename, out e) ? e.Data.Length : 0;
		}

		public static DateTime FileTime(string filename)
		{
			Entry e;
			return filename != null && _files.TryGetValue(filename, out e) ? e.Time : DateTime.MinValue;
		}

		public static byte[] FileRead(string filename)
		{
			Entry e;
			if (filename == null || !_files.TryGetValue(filename, out e)) return null;
			return (byte[])e.Data.Clone();
		}

		public static bool FileWrite(string filename, byte[] data)
		{
			if (!ValidName(filename) || data == null) return false;

			ulong existing = 0;
			Entry old;
			if (_files.TryGetValue(filename, out old)) existing = (ulong)old.Data.Length;
			if (QuotaUsedBytes - existing + (ulong)data.Length > _quota) return false;

			var copy = (byte[])data.Clone();
			_files[filename] = new Entry { Data = copy, Time = DateTime.UtcNow };
			_pending[filename] = copy;
			Flush();
			return true;
		}

		public static bool FileWrite(string filename, byte[] data, int length)
		{
			if (data == null || length < 0 || length > data.Length) return false;
			var part = new byte[length];
			Buffer.BlockCopy(data, 0, part, 0, length);
			return FileWrite(filename, part);
		}

		public static bool FileWrite(string filename, string text)
		{
			return FileWrite(filename, Encoding.UTF8.GetBytes(text ?? ""));
		}

		public static bool FileDelete(string filename)
		{
			if (!FileExists(filename)) return false;
			_files.Remove(filename);
			_pending[filename] = null;
			Flush();
			return true;
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
			LoadFromJson(await ApiClient.GetAsync("/RemoteStorage/Files"));
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

				_files[name] = new Entry
				{
					Data = Convert.FromBase64String(b64),
					Time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(WebworksJson.Num(f, "time")),
				};
			}
		}

		static async void Flush()
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
							Debug.LogWarning("[Webworks] cloud save of '" + name + "' rejected by the server: " + WebworksJson.Str(reply, "error"));
					}
					catch (Exception ex)
					{
						Debug.LogWarning("[Webworks] cloud sync of '" + name + "' failed: " + ex.Message);
					}
				}
			}
			finally
			{
				_flushing = false;
			}
		}
	}
}
