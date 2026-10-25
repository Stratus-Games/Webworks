using System;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	// Diagnose-style calls. Webworks has no signed-file or IPv6 probe, so the answers are
	// explicit rather than invented.
	public static partial class SteamUtils
	{
		public static ESteamAPICallFailure GetAPICallFailureReason(SteamAPICall_t hSteamAPICall)
		{
			// ResultBus only ever completes successful calls, so there is no recorded
			// failure for any handle it knows about.
			return ESteamAPICallFailure.k_ESteamAPICallFailureNone;
		}

		public static SteamAPICall_t CheckFileSignature(string szFileName)
		{
			var call = ResultBus.NewCall();
			MainThreadDispatcher.Enqueue(() => ResultBus.Complete(call, new CheckFileSignature_t
			{
				m_eCheckFileSignature = ECheckFileSignature.k_ECheckFileSignatureNoSignaturesFoundForThisApp,
			}));
			return call;
		}

		public static ESteamIPv6ConnectivityState GetIPv6ConnectivityState(ESteamIPv6ConnectivityProtocol eProtocol)
		{
			return ESteamIPv6ConnectivityState.k_ESteamIPv6ConnectivityState_Unknown;
		}
	}
}
