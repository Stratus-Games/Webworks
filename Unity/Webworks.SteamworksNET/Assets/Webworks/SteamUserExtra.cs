using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	public static partial class SteamUser
	{
		static EDurationControlOnlineState _durationState = EDurationControlOnlineState.k_EDurationControlOnlineState_Offline;
		static CSteamID _advertisedServer;
		static uint _advertisedIp;
		static ushort _advertisedPort;
		static string _lastWebApiIdentity = "";

		internal static string WebApiIdentity { get { return _lastWebApiIdentity; } }

		public static int InitiateGameConnection_DEPRECATED(byte[] pAuthBlob, int cbMaxAuthBlob, CSteamID steamIDGameServer, uint unIPServer, ushort usPortServer, bool bSecure)
		{
			// Legacy handshake bytes are still handed out, but the shim authenticates with a
			// session ticket instead, so there is nothing native to copy here.
			if (pAuthBlob != null && cbMaxAuthBlob > 2)
			{
				pAuthBlob[0] = 0;
				pAuthBlob[1] = 0;
				return 2;
			}
			return 0;
		}

		public static void TerminateGameConnection_DEPRECATED(uint unIPServer, ushort usPortServer)
		{
			EndAuthSession(new CSteamID(0));
		}

		public static void TrackAppUsageEvent(CGameID gameID, int eAppUsageEvent, string pchExtraInfo)
		{
			if (!SessionState.Initialized) return;
			var body = "{\"type\":" + WebworksJson.Quote("app_usage")
				+ ",\"description\":" + WebworksJson.Quote((eAppUsageEvent.ToString(CultureInfo.InvariantCulture) + " " + (pchExtraInfo ?? "")).Trim())
				+ ",\"game\":" + WebworksJson.Quote(gameID.ToString())
				+ "}";
			ApiClient.Fire(() => ApiClient.PostAsync("/Timeline/Event", body), "TrackAppUsageEvent");
		}

		public static bool GetUserDataFolder(out string pchBuffer, int cubBuffer)
		{
			pchBuffer = Application.persistentDataPath;
			if (cubBuffer > 0 && pchBuffer.Length >= cubBuffer) pchBuffer = pchBuffer.Substring(0, Math.Max(0, cubBuffer - 1));
			return !string.IsNullOrEmpty(pchBuffer);
		}

		public static HAuthTicket GetAuthTicketForWebApi(string pchIdentity)
		{
			// Same ticket machinery as the session ticket, tagged with the caller's identity
			// so a web backend can tell API tickets from game-server tickets.
			_lastWebApiIdentity = pchIdentity ?? "";
			uint size;
			return GetAuthSessionTicket(new byte[1024], 1024, out size);
		}

		public static void AdvertiseGame(CSteamID steamIDGameServer, uint unIPServer, ushort usPortServer)
		{
			_advertisedServer = steamIDGameServer;
			_advertisedIp = unIPServer;
			_advertisedPort = usPortServer;
		}

		internal static CSteamID AdvertisedServer { get { return _advertisedServer; } }
		internal static uint AdvertisedIp { get { return _advertisedIp; } }
		internal static ushort AdvertisedPort { get { return _advertisedPort; } }

		public static bool GetEncryptedAppTicket(byte[] pTicket, int cbMaxTicket, out uint pcbTicket)
		{
			// Filled by RequestEncryptedAppTicket from the server's HMAC-signed envelope.
			var ticket = EncryptedTicket;
			pcbTicket = 0;
			if (ticket == null || ticket.Length == 0 || pTicket == null) return false;

			var count = Math.Min(cbMaxTicket, Math.Min(pTicket.Length, ticket.Length));
			if (count <= 0) return false;
			Array.Copy(ticket, pTicket, count);
			pcbTicket = (uint)count;
			return true;
		}

		public static int GetGameBadgeLevel(int nSeries, bool bFoil) { return 0; }
		public static int GetPlayerSteamLevel() { return SessionState.Initialized ? 1 : 0; }
		public static bool BIsTwoFactorEnabled() { return false; }

		public static bool BSetDurationControlOnlineState(EDurationControlOnlineState eNewState)
		{
			_durationState = eNewState;
			return true;
		}

		internal static EDurationControlOnlineState DurationState { get { return _durationState; } }
	}
}
