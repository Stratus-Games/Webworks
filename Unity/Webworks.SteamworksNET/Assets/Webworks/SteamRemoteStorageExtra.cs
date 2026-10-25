using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	// Streamed writes, UGC reads, sync platforms and local file change tracking. Cloud
	// files still ride the same RemoteStorage routes as the rest of the shim.
	public static partial class SteamRemoteStorage
	{
		class Stream
		{
			public string Name;
			public readonly List<byte> Buffer = new List<byte>();
			public bool Closed;
		}

		static readonly Dictionary<ulong, Stream> _streams = new Dictionary<ulong, Stream>();
		static readonly Dictionary<string, ERemoteStoragePlatform> _syncPlatforms = new Dictionary<string, ERemoteStoragePlatform>();
		static readonly Dictionary<ulong, UgcContent> _ugc = new Dictionary<ulong, UgcContent>();
		static ulong _nextStream = 1;
		static int _batchDepth;
		static readonly List<string> _localChanges = new List<string>();

		class UgcContent
		{
			public ulong Handle;
			public byte[] Data = new byte[0];
			public string Name = "";
			public ulong Owner;
			public bool Complete;
		}

		internal static void RegisterUGC(ulong handle, byte[] data, string name, ulong owner)
		{
			_ugc[handle] = new UgcContent { Handle = handle, Data = data ?? new byte[0], Name = name ?? "", Owner = owner, Complete = true };
		}

		internal static void NoteLocalChange(string file)
		{
			if (string.IsNullOrEmpty(file)) return;
			if (!_localChanges.Contains(file)) _localChanges.Add(file);
		}

		public static bool SetSyncPlatforms(string pchFile, ERemoteStoragePlatform eRemoteStoragePlatform)
		{
			if (string.IsNullOrEmpty(pchFile)) return false;
			_syncPlatforms[pchFile] = eRemoteStoragePlatform;
			return true;
		}

		public static ERemoteStoragePlatform GetSyncPlatforms(string pchFile)
		{
			ERemoteStoragePlatform platform;
			return pchFile != null && _syncPlatforms.TryGetValue(pchFile, out platform)
				? platform
				: ERemoteStoragePlatform.k_ERemoteStoragePlatformAll;
		}

		static Stream StreamFor(UGCFileWriteStreamHandle_t handle)
		{
			Stream stream;
			return _streams.TryGetValue(handle.m_UGCFileWriteStreamHandle, out stream) ? stream : null;
		}

		public static UGCFileWriteStreamHandle_t FileWriteStreamOpen(string pchFile)
		{
			if (!ValidName(pchFile)) return new UGCFileWriteStreamHandle_t(0);
			var id = _nextStream++;
			_streams[id] = new Stream { Name = pchFile };
			return new UGCFileWriteStreamHandle_t(id);
		}

		public static bool FileWriteStreamWriteChunk(UGCFileWriteStreamHandle_t writeHandle, byte[] pvData, int cubData)
		{
			var stream = StreamFor(writeHandle);
			if (stream == null || stream.Closed || pvData == null || cubData <= 0) return false;
			for (int i = 0; i < cubData && i < pvData.Length; i++) stream.Buffer.Add(pvData[i]);
			return true;
		}

		public static bool FileWriteStreamClose(UGCFileWriteStreamHandle_t writeHandle)
		{
			var stream = StreamFor(writeHandle);
			if (stream == null || stream.Closed) return false;
			stream.Closed = true;
			_streams.Remove(writeHandle.m_UGCFileWriteStreamHandle);
			var ok = FileWrite(stream.Name, stream.Buffer.ToArray(), stream.Buffer.Count);
			if (ok) NoteLocalChange(stream.Name);
			return ok;
		}

		public static bool FileWriteStreamCancel(UGCFileWriteStreamHandle_t writeHandle)
		{
			var stream = StreamFor(writeHandle);
			if (stream == null) return false;
			stream.Closed = true;
			_streams.Remove(writeHandle.m_UGCFileWriteStreamHandle);
			return true;
		}

		public static bool GetUGCDownloadProgress(UGCHandle_t hContent, out int pnBytesDownloaded, out int pnBytesExpected)
		{
			UgcContent download;
			if (_ugc.TryGetValue(hContent.m_UGCHandle, out download))
			{
				pnBytesDownloaded = download.Complete ? download.Data.Length : 0;
				pnBytesExpected = download.Data.Length;
				return true;
			}
			pnBytesDownloaded = 0;
			pnBytesExpected = 0;
			return false;
		}

		public static bool GetUGCDetails(UGCHandle_t hContent, out AppId_t pnAppID, out string ppchName, out int pnFileSizeInBytes, out CSteamID pSteamIDOwner)
		{
			UgcContent download;
			if (_ugc.TryGetValue(hContent.m_UGCHandle, out download))
			{
				uint appId;
				uint.TryParse(SessionState.AppId, out appId);
				pnAppID = new AppId_t(appId);
				ppchName = download.Name;
				pnFileSizeInBytes = download.Data.Length;
				pSteamIDOwner = new CSteamID(download.Owner);
				return true;
			}

			pnAppID = AppId_t.Invalid;
			ppchName = string.Empty;
			pnFileSizeInBytes = 0;
			pSteamIDOwner = CSteamID.Nil;
			return false;
		}

		public static int UGCRead(UGCHandle_t hContent, byte[] pvData, int cubDataToRead, uint cOffset, EUGCReadAction eAction)
		{
			UgcContent download;
			if (!_ugc.TryGetValue(hContent.m_UGCHandle, out download) || pvData == null) return 0;

			var from = (int)Math.Min(cOffset, (uint)download.Data.Length);
			var count = Math.Min(download.Data.Length - from, Math.Min(cubDataToRead, pvData.Length));
			Array.Copy(download.Data, from, pvData, 0, count);

			if (eAction == EUGCReadAction.k_EUGCRead_Close && from + count >= download.Data.Length)
				_ugc.Remove(hContent.m_UGCHandle);

			return count;
		}

		public static int GetCachedUGCCount() { return _ugc.Count; }

		// --- published file update handles ---------------------------------------------

		class Update
		{
			public ulong FileId;
			public string File;
			public string Preview;
			public string Title;
			public string Description;
			public string ChangeDescription;
			public ERemoteStoragePublishedFileVisibility? Visibility;
			public List<string> Tags;
		}

		static readonly Dictionary<ulong, Update> _updates = new Dictionary<ulong, Update>();
		static ulong _nextUpdate = 1;

		internal static bool ApplyUpdate(ulong handle)
		{
			Update update;
			if (!_updates.TryGetValue(handle, out update)) return false;
			_updates.Remove(handle);
			return true;
		}

		static Update UpdateFor(PublishedFileUpdateHandle_t handle)
		{
			Update update;
			return _updates.TryGetValue(handle.m_PublishedFileUpdateHandle, out update) ? update : null;
		}

		public static PublishedFileUpdateHandle_t CreatePublishedFileUpdateRequest(PublishedFileId_t unPublishedFileId)
		{
			var id = _nextUpdate++;
			_updates[id] = new Update { FileId = unPublishedFileId.m_PublishedFileId };
			return new PublishedFileUpdateHandle_t(id);
		}

		public static bool UpdatePublishedFileFile(PublishedFileUpdateHandle_t updateHandle, string pchFile)
		{
			var update = UpdateFor(updateHandle);
			if (update == null) return false;
			update.File = pchFile;
			return true;
		}

		public static bool UpdatePublishedFilePreviewFile(PublishedFileUpdateHandle_t updateHandle, string pchPreviewFile)
		{
			var update = UpdateFor(updateHandle);
			if (update == null) return false;
			update.Preview = pchPreviewFile;
			return true;
		}

		public static bool UpdatePublishedFileTitle(PublishedFileUpdateHandle_t updateHandle, string pchTitle)
		{
			var update = UpdateFor(updateHandle);
			if (update == null) return false;
			update.Title = pchTitle;
			return true;
		}

		public static bool UpdatePublishedFileDescription(PublishedFileUpdateHandle_t updateHandle, string pchDescription)
		{
			var update = UpdateFor(updateHandle);
			if (update == null) return false;
			update.Description = pchDescription;
			return true;
		}

		public static bool UpdatePublishedFileVisibility(PublishedFileUpdateHandle_t updateHandle, ERemoteStoragePublishedFileVisibility eVisibility)
		{
			var update = UpdateFor(updateHandle);
			if (update == null) return false;
			update.Visibility = eVisibility;
			return true;
		}

		public static bool UpdatePublishedFileTags(PublishedFileUpdateHandle_t updateHandle, System.Collections.Generic.IList<string> pTags)
		{
			var update = UpdateFor(updateHandle);
			if (update == null) return false;
			update.Tags = pTags != null ? new List<string>(pTags) : new List<string>();
			return true;
		}

		public static bool UpdatePublishedFileSetChangeDescription(PublishedFileUpdateHandle_t updateHandle, string pchChangeDescription)
		{
			var update = UpdateFor(updateHandle);
			if (update == null) return false;
			update.ChangeDescription = pchChangeDescription;
			return true;
		}

		// --- local file changes ---------------------------------------------------------

		public static int GetLocalFileChangeCount() { return _localChanges.Count; }

		public static string GetLocalFileChange(int iFile, out ERemoteStorageLocalFileChange pEChangeType, out ERemoteStorageFilePathType pEFilePathType)
		{
			pEChangeType = ERemoteStorageLocalFileChange.k_ERemoteStorageLocalFileChange_Invalid;
			pEFilePathType = ERemoteStorageFilePathType.k_ERemoteStorageFilePathType_Invalid;
			if (iFile < 0 || iFile >= _localChanges.Count) return string.Empty;

			var name = _localChanges[iFile];
			pEChangeType = FileExists(name)
				? ERemoteStorageLocalFileChange.k_ERemoteStorageLocalFileChange_FileUpdated
				: ERemoteStorageLocalFileChange.k_ERemoteStorageLocalFileChange_FileDeleted;
			pEFilePathType = ERemoteStorageFilePathType.k_ERemoteStorageFilePathType_Absolute;
			return name;
		}

		public static bool BeginFileWriteBatch() { _batchDepth++; return true; }

		public static bool EndFileWriteBatch()
		{
			if (_batchDepth > 0) _batchDepth--;
			if (_batchDepth == 0) Flush();
			return true;
		}
	}
}
