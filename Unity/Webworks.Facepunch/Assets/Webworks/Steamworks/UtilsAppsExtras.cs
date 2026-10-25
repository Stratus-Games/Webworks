#pragma warning disable CS0067
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks.Data;
using Webworks.Internal;

namespace Steamworks
{
	public static partial class SteamUtils
	{
		public static event Action OnIpCountryChanged;
		public static event Action<int> OnLowBatteryPower;
		public static event Action OnSteamShutdown;
		public static event Action<bool> OnGamepadTextInputDismissed;

		public static Universe ConnectedUniverse { get { return Universe.Public; } }
		public static float CurrentBatteryPower { get { return 255f; } }
		public static bool UsingBatteryPower { get { return false; } }
		public static bool DoesOverlayNeedPresent { get { return false; } }
		public static bool IsSteamChinaLauncher { get { return false; } }
		public static bool IsSteamInBigPictureMode { get { return false; } }
		public static bool VrHeadsetStreaming { get; set; }
		public static string SteamUILanguage { get { return "english"; } }
		public static uint SecondsSinceComputerActive { get { return (uint)Time.realtimeSinceStartup; } }
		public static NotificationPosition OverlayNotificationPosition { get; set; }

		public static void SetOverlayNotificationInset(int horizontal, int vertical) { }
		public static void StartVRDashboard() { }
		public static string GetEnteredGamepadText() { return ""; }

		public static bool ShowGamepadTextInput(GamepadTextInputMode inputMode, GamepadTextInputLineMode lineInputMode, string description, int maxChars, string existingText = "")
		{
			return false;
		}

		public static Task<CheckFileSignature> CheckFileSignatureAsync(string filename)
		{
			return Task.FromResult(CheckFileSignature.NoSignaturesFoundForThisApp);
		}

		public static Image? GetImage(int image) { return null; }

		public static bool GetImageSize(int image, out uint width, out uint height)
		{
			width = 0;
			height = 0;
			return false;
		}
	}

	public static partial class SteamApps
	{
		public static event Action<AppId> OnDlcInstalled;
		public static event Action OnNewLaunchParameters;

		public static string CommandLine { get { return ""; } }
		public static string GetLaunchParam(string param) { return ""; }

		public static void InstallDlc(AppId appid) { }
		public static void UninstallDlc(AppId appid) { }
		public static bool MarkContentCorrupt(bool missingFilesOnly) { return false; }
		public static DateTime PurchaseTime(AppId appid = default(AppId)) { return DateTime.UtcNow; }
		public static DownloadProgress DlcDownloadProgress(AppId appid) { return new DownloadProgress(); }
		public static IEnumerable<DepotId> InstalledDepots(AppId appid = default(AppId)) { return new DepotId[0]; }
		public static IEnumerable<Steamworks.Data.DlcInformation> DlcInformation() { return new Steamworks.Data.DlcInformation[0]; }

		public static Task<FileDetails?> GetFileDetailsAsync(string filename) { return Task.FromResult<FileDetails?>(null); }
	}
}

namespace Steamworks.Data
{
	public struct DlcInformation
	{
		public AppId AppId;
		public string Name;
		public bool Available;
	}

	public struct DownloadProgress
	{
		public bool Active;
		public ulong BytesDownloaded;
		public ulong BytesTotal;
	}

	public struct FileDetails
	{
		public ulong SizeInBytes;
		public string Sha1;
		public uint Flags;
	}

	public struct DepotId
	{
		public uint Value;
		public static implicit operator DepotId(uint value) { return new DepotId { Value = value }; }
		public static implicit operator uint(DepotId id) { return id.Value; }
		public override string ToString() { return Value.ToString(); }
	}

	public struct GameId
	{
		public ulong Value;
		public static implicit operator GameId(ulong value) { return new GameId { Value = value }; }
		public static implicit operator ulong(GameId id) { return id.Value; }
		public override string ToString() { return Value.ToString(); }
	}

	public struct PublishedFileId
	{
		public ulong Value;
		public static implicit operator PublishedFileId(ulong value) { return new PublishedFileId { Value = value }; }
		public static implicit operator ulong(PublishedFileId id) { return id.Value; }
		public override string ToString() { return Value.ToString(); }
	}
}
