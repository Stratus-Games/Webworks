using System;
using Webworks.Internal;

namespace Steamworks.Data
{
	public struct FriendGameInfo
	{
		public ulong GameID;
		public uint GameIP;
		public int ConnectionPort;
		public int QueryPort;
		public ulong SteamIDLobby;
	}
}

namespace Steamworks
{
	public static partial class SteamApps
	{
		static bool Mine(AppId appid) { return true; }

		public static bool IsSubscribed { get { return true; } }
		public static bool IsSubscribedFromFreeWeekend { get { return false; } }
		public static bool IsSubscribedFromFamilySharing { get { return false; } }
		public static bool IsCybercafe { get { return false; } }
		public static bool IsVACBanned { get { return false; } }
		public static bool IsLowViolence { get { return false; } }

		public static bool IsSubscribedToApp(AppId appid) { return Mine(appid); }
		public static bool IsAppInstalled(AppId appid) { return Mine(appid); }
		public static bool IsDlcInstalled(AppId appid) { return true; }

		public static SteamId AppOwner { get { return SteamClient.SteamId; } }
		public static string GameLanguage { get { return "english"; } }
		public static string[] AvailableLanguages { get { return new[] { "english" }; } }
		public static string CurrentBetaName { get { return "public"; } }
		public static int BuildId { get { return 0; } }

		public static string AppInstallDir(AppId appid = default(AppId)) { return UnityEngine.Application.dataPath; }
	}

	public static partial class SteamUtils
	{
		public static AppId AppId { get { return SteamClient.AppId; } }
		public static string IpCountry { get { return "US"; } }
		public static bool IsOverlayEnabled { get { return false; } }
		public static bool IsSteamRunningInVR { get { return false; } }
		public static bool IsRunningOnSteamDeck { get { return false; } }
		public static DateTime SteamServerTime { get { return DateTime.UtcNow; } }
		public static uint SecondsSinceAppActive { get { return (uint)UnityEngine.Time.realtimeSinceStartup; } }
	}
}
