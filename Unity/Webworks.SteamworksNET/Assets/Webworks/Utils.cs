using System;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	public static partial class SteamUtils
	{
		public static AppId_t GetAppID() { uint v; return uint.TryParse(SessionState.AppId, out v) ? new AppId_t(v) : AppId_t.Invalid; }
		public static uint GetServerRealTime() { return (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(); }
		public static string GetSteamUILanguage() { return "english"; }
		public static string GetIPCountry() { return "US"; }
		public static EUniverse GetConnectedUniverse() { return EUniverse.k_EUniversePublic; }
		public static bool IsAPICallCompleted(SteamAPICall_t hSteamAPICall, out bool pbFailed) { pbFailed = false; return false; }
		// Webworks draws its own overlay panel (Webworks/Overlay), so the overlay is always
		// available -- there is no native window that can be disabled.
		public static bool IsOverlayEnabled() { return true; }
		public static bool BOverlayNeedsPresent() { return false; }
		public static bool IsSteamRunningInVR() { return false; }
		public static bool IsSteamInBigPictureMode() { return false; }
		public static bool IsSteamChinaLauncher() { return false; }
		public static uint GetSecondsSinceAppActive() { return (uint)Time.realtimeSinceStartup; }
		public static uint GetSecondsSinceComputerActive() { return (uint)Time.realtimeSinceStartup; }
		public static byte GetCurrentBatteryPower() { return 255; }
		public static void SetOverlayNotificationPosition(ENotificationPosition eNotificationPosition) { }
		public static void SetOverlayNotificationInset(int nHorizontalInset, int nVerticalInset) { }
		public static void StartVRDashboard() { }
		public static bool ShowGamepadTextInput(EGamepadTextInputMode eInputMode, EGamepadTextInputLineMode eLineInputMode, string pchDescription, uint unCharMax, string pchExistingText = "")
		{
			WebworksGamepadKeyboard.Show(eInputMode, eLineInputMode, pchDescription, unCharMax, pchExistingText, floating: false);
			return true;
		}

		static string _lastGamepadText = "";
		internal static void SetLastGamepadText(string text) { _lastGamepadText = text ?? ""; }
		public static bool GetEnteredGamepadTextInput(out string pchText, uint cchText)
		{
			string t = _lastGamepadText ?? "";
			if (cchText > 0 && t.Length >= cchText) t = t.Substring(0, (int)cchText - 1);
			pchText = t;
			return true;
		}
		public static uint GetEnteredGamepadTextLength() { return (uint)_lastGamepadText.Length; }

		public static bool ShowFloatingGamepadTextInput(EFloatingGamepadTextInputMode eKeyboardMode, int nTextFieldXPosition, int nTextFieldYPosition, int nTextFieldWidth, int nTextFieldHeight)
		{
			WebworksGamepadKeyboard.Show(EGamepadTextInputMode.k_EGamepadTextInputModeNormal, EGamepadTextInputLineMode.k_EGamepadTextInputLineModeSingleLine, "", 999, "", floating: true);
			return true;
		}

		public static bool DismissFloatingGamepadTextInput() { WebworksGamepadKeyboard.Dismiss(); return true; }
		public static bool DismissGamepadTextInput() { WebworksGamepadKeyboard.Dismiss(); return true; }

		public static bool GetImageSize(int iImage, out uint pnWidth, out uint pnHeight)
		{
			byte[] rgba;
			return AvatarCache.TryGet(iImage, out pnWidth, out pnHeight, out rgba);
		}

		public static bool GetImageRGBA(int iImage, byte[] pubDest, int nDestBufferSize)
		{
			uint w, h; byte[] rgba;
			if (!AvatarCache.TryGet(iImage, out w, out h, out rgba)) return false;
			var n = Math.Min(rgba.Length, nDestBufferSize);
			Array.Copy(rgba, pubDest, n);
			return true;
		}
	}

	
	
	
	
}
