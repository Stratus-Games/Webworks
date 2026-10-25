using System;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	// Account calls that are not about a session ticket or the data folder. The encrypted
	// app ticket is minted by the Webworks server (HMAC envelope) so a backend can verify
	// it; the store and market have no equivalent and say so.
	public static partial class SteamUser
	{
		static byte[] _encryptedTicket;

		internal static byte[] EncryptedTicket { get { return _encryptedTicket; } }

		public static HSteamUser GetHSteamUser()
		{
			return new HSteamUser(SessionState.Initialized ? 1 : 0);
		}

		public static SteamAPICall_t RequestEncryptedAppTicket(byte[] pDataToInclude, int cbDataToInclude)
		{
			var call = ResultBus.NewCall();

			string data = "";
			if (pDataToInclude != null && cbDataToInclude > 0)
			{
				var count = Math.Min(cbDataToInclude, pDataToInclude.Length);
				var slice = new byte[count];
				Array.Copy(pDataToInclude, slice, count);
				data = Convert.ToBase64String(slice);
			}

			var body = "{\"data\":" + WebworksJson.Quote(data) + "}";
			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.PostAsync("/User/EncryptedAppTicket", body));
				var ticket = d != null ? WebworksJson.Str(d, "ticket") : null;

				if (!string.IsNullOrEmpty(ticket))
				{
					try { _encryptedTicket = Convert.FromBase64String(ticket); }
					catch (FormatException) { _encryptedTicket = null; }
				}
				else
				{
					_encryptedTicket = null;
				}

				ResultBus.Complete(call, new EncryptedAppTicketResponse_t
				{
					m_eResult = _encryptedTicket != null ? EResult.k_EResultOK : EResult.k_EResultFail,
				});
				return "";
			}, "RequestEncryptedAppTicket");
			return call;
		}

		public static SteamAPICall_t RequestStoreAuthURL(string pchRedirectURL)
		{
			// There is no store, so there is no URL to authorise. Completing with an empty
			// URL is clearer than leaving the call pending.
			var call = ResultBus.NewCall();
			MainThreadDispatcher.Enqueue(() =>
			{
				var response = new StoreAuthURLResponse_t();
				response.m_szURL = "";
				ResultBus.Complete(call, response);
			});
			return call;
		}

		public static SteamAPICall_t GetMarketEligibility()
		{
			var call = ResultBus.NewCall();
			MainThreadDispatcher.Enqueue(() => ResultBus.Complete(call, new MarketEligibilityResponse_t
			{
				m_bAllowed = false,
				m_eNotAllowedReason = EMarketNotAllowedReasonFlags.k_EMarketNotAllowedReason_AccountDisabled,
				m_rtAllowedAtTime = new RTime32(0),
			}));
			return call;
		}

		public static SteamAPICall_t GetDurationControl()
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.GetAsync("/User/DurationControl"));
				var progress = d != null ? WebworksJson.Str(d, "progress") : null;

				EDurationControlProgress mapped = EDurationControlProgress.k_EDurationControlProgress_None;
				if (string.Equals(progress, "half", StringComparison.OrdinalIgnoreCase)) mapped = EDurationControlProgress.k_EDurationControlProgress_Half;
				else if (string.Equals(progress, "full", StringComparison.OrdinalIgnoreCase)) mapped = EDurationControlProgress.k_EDurationControlProgress_Full;

				uint appId;
				uint.TryParse(SessionState.AppId, out appId);

				ResultBus.Complete(call, new DurationControl_t
				{
					m_eResult = d != null ? EResult.k_EResultOK : EResult.k_EResultFail,
					m_appid = new AppId_t(appId),
					m_bApplicable = d != null,
					m_progress = mapped,
					m_notification = EDurationControlNotification.k_EDurationControlNotification_None,
					m_csecsRemaining = d != null ? WebworksJson.Int(d, "remainingSeconds") : 0,
				});
				return "";
			}, "GetDurationControl");
			return call;
		}
	}
}
