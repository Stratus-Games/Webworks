using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	public static partial class SteamUtils
	{
		static bool _vrStreaming;
		static bool _filterReady;
		static bool _launcherMode;
		static readonly HashSet<string> _profanity = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"fuck", "shit", "bitch", "cunt", "asshole", "bastard", "dick", "piss", "slut", "whore",
		};

		// The native callback table does not exist here, so a call result can never be
		// observed through a raw pointer; the typed CallResult<T> path is the supported one.
		public static bool GetAPICallResult(SteamAPICall_t hSteamAPICall, IntPtr pCallback, int cubCallback, int iCallbackExpected, out bool pbFailed)
		{
			pbFailed = false;
			return false;
		}

		public static uint GetIPCCallCount() { return SteamClient.GetIPCCallCount(); }

		public static void SetWarningMessageHook(SteamAPIWarningMessageHook_t pFunction)
		{
			SteamClient.SetWarningMessageHook(pFunction);
		}

		public static bool IsVRHeadsetStreamingEnabled() { return _vrStreaming; }
		public static void SetVRHeadsetStreamingEnabled(bool bEnabled) { _vrStreaming = bEnabled; }

		public static bool InitFilterText(uint unFilterOptions = 0)
		{
			_filterReady = true;
			return true;
		}

		public static int FilterText(ETextFilteringContext eContext, CSteamID sourceSteamID, string pchInputMessage, out string pchOutFilteredText, uint nByteSizeOutFilteredText)
		{
			var input = pchInputMessage ?? "";
			if (!_filterReady)
			{
				pchOutFilteredText = input;
				return input.Length + 1;
			}

			var cleaned = Regex.Replace(input, "[A-Za-z]+", (match) =>
			{
				if (!_profanity.Contains(match.Value)) return match.Value;
				return new string('*', match.Value.Length);
			});

			int max = Math.Max(0, (int)nByteSizeOutFilteredText - 1);
			if (nByteSizeOutFilteredText > 0 && cleaned.Length > max) cleaned = cleaned.Substring(0, max);

			pchOutFilteredText = cleaned;
			return cleaned.Length + 1;
		}

		public static bool IsSteamRunningOnSteamDeck() { return false; }

		public static void SetGameLauncherMode(bool bLauncherMode) { _launcherMode = bLauncherMode; }

		internal static bool LauncherMode { get { return _launcherMode; } }
	}
}
