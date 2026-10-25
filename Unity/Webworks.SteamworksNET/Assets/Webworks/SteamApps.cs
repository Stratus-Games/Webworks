using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	public static partial class SteamApps
	{
		static AppId_t OwnedAppId(AppId_t fallback)
		{
			uint parsed;
			if (uint.TryParse(SessionState.AppId, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) && parsed != 0)
				return new AppId_t(parsed);
			return fallback;
		}

		public static bool BIsSubscribed() { return true; }
		public static bool BIsLowViolence() { return false; }
		public static bool BIsCybercafe() { return false; }
		public static bool BIsVACBanned() { return false; }
		public static string GetCurrentGameLanguage() { return "english"; }
		public static string GetAvailableGameLanguages() { return "english"; }
		public static bool BIsSubscribedApp(AppId_t appID) { return true; }
		public static bool BIsDlcInstalled(AppId_t appID) { return true; }
		public static uint GetEarliestPurchaseUnixTime(AppId_t nAppID) { return 0; }
		public static bool BIsSubscribedFromFreeWeekend() { return false; }
		public static int GetDLCCount() { return 0; }

		public static bool BGetDLCDataByIndex(int iDLC, out AppId_t pAppID, out bool pbAvailable, out string pchName, int cchNameBufferSize)
		{
			pAppID = AppId_t.Invalid;
			pbAvailable = false;
			pchName = string.Empty;
			return false;
		}

		public static void InstallDLC(AppId_t nAppID) { }
		public static void UninstallDLC(AppId_t nAppID) { }
		public static void RequestAppProofOfPurchaseKey(AppId_t nAppID) { }
		public static bool GetCurrentBetaName(out string pchName, int cchNameBufferSize) { pchName = string.Empty; return false; }
		public static bool MarkContentCorrupt(bool bMissingFilesOnly) { return false; }
		public static uint GetInstalledDepots(AppId_t appID, DepotId_t[] pvecDepots, uint cMaxDepots) { return 0; }

		public static uint GetAppInstallDir(AppId_t appID, out string pchFolder, uint cchFolderBufferSize)
		{
			string dir = Application.dataPath;
			if (cchFolderBufferSize > 0 && dir.Length >= cchFolderBufferSize) dir = dir.Substring(0, (int)cchFolderBufferSize - 1);
			pchFolder = dir;
			return (uint)dir.Length;
		}

		public static bool BIsAppInstalled(AppId_t appID) { return true; }
		public static CSteamID GetAppOwner() { return SteamUser.GetSteamID(); }
		public static string GetLaunchQueryParam(string pchKey) { return string.Empty; }

		public static bool GetDlcDownloadProgress(AppId_t nAppID, out ulong punBytesDownloaded, out ulong punBytesTotal)
		{
			punBytesDownloaded = 0;
			punBytesTotal = 0;
			return false;
		}

		public static int GetAppBuildId() { return 1; }
		public static void RequestAllProofOfPurchaseKeys() { }

		public static SteamAPICall_t GetFileDetails(string pszFileName)
		{
			var call = ResultBus.NewCall();
			MainThreadDispatcher.Enqueue(() => ResultBus.Complete(call, new FileDetailsResult_t { m_eResult = EResult.k_EResultFileNotFound }));
			return call;
		}

		public static int GetLaunchCommandLine(out string pszCommandLine, int cubCommandLine) { pszCommandLine = string.Empty; return 0; }
		public static bool BIsSubscribedFromFamilySharing() { return false; }

		public static bool BIsTimedTrial(out uint punSecondsAllowed, out uint punSecondsPlayed)
		{
			punSecondsAllowed = 0;
			punSecondsPlayed = 0;
			return false;
		}

		public static bool SetDlcContext(AppId_t nAppID) { return true; }

		public static int GetNumBetas(out int pnAvailable, out int pnPrivate)
		{
			pnAvailable = 0;
			pnPrivate = 0;
			return 0;
		}

		public static bool GetBetaInfo(int iBetaIndex, out uint punFlags, out uint punBuildID, out string pchBetaName, int cchBetaName, out string pchDescription, int cchDescription, out uint punLastUpdated)
		{
			punFlags = 0;
			punBuildID = 0;
			pchBetaName = string.Empty;
			pchDescription = string.Empty;
			punLastUpdated = 0;
			return false;
		}

		public static bool SetActiveBeta(string pchBetaName) { return false; }
	}

	public struct FileDetailsResult_t
	{
		public EResult m_eResult;
		public ulong m_ulFileSize;
		public byte[] m_FileSHA;
		public uint m_unFlags;
	}
}
