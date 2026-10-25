using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	public struct GameServerChangeRequested_t
	{
		public string m_rgchServer;
		public string m_rgchPassword;
	}

	public static partial class SteamGameServer
	{
		static bool _loggedOn;
		static ulong _steamId;
		static string _appId = "0";
		static ushort _gamePort;
		static ushort _queryPort;
		static ushort _spectatorPort;
		static string _version = "1.0.0.0";
		static string _serverName = "";
		static string _mapName = "";
		static string _gameDescription = "";
		static string _modDir = "";
		static string _product = "";
		static string _gameTags = "";
		static string _gameData = "";
		static string _region = "";
		static string _spectatorName = "";
		static int _maxPlayers;
		static int _botCount;
		static bool _passworded;
		static bool _dedicated = true;
		static bool _secure = true;
		static bool _advertise = true;
		static float _heartbeat;
		static float _heartbeatInterval = 15f;
		static bool _loggingOn;
		static readonly Dictionary<string, string> _keys = new Dictionary<string, string>();
		static readonly Dictionary<CSteamID, bool> _authSessions = new Dictionary<CSteamID, bool>();

		public static bool Init(uint unIP, ushort usGamePort, ushort usQueryPort, EServerMode eServerMode, string pchVersionString)
		{
			WebworksConfig.Load();
			_gamePort = usGamePort;
			_queryPort = usQueryPort;
			_version = string.IsNullOrEmpty(pchVersionString) ? "1.0.0.0" : pchVersionString;
			_secure = eServerMode != EServerMode.eServerModeNoAuthentication;
			_appId = WebworksConfig.ResolveAppId(0).ToString(CultureInfo.InvariantCulture);
			LogOnAnonymous();
			return true;
		}

		public static void Shutdown()
		{
			if (_loggedOn) LogOff();
			_authSessions.Clear();
			_keys.Clear();
		}

		public static void LogOnAnonymous()
		{
			if (_loggedOn || _loggingOn) return;
			_loggingOn = true;

			ApiClient.Fire(async () =>
			{
				var boot = WebworksJson.Obj(await ApiClient.PostAsync("/User/GuestSession", "{\"appId\":" + WebworksJson.Quote(_appId) + ",\"name\":\"GameServer\"}"));
				var bootToken = boot != null ? WebworksJson.Str(boot, "sessionToken") : null;
				if (bootToken == null)
				{
					_loggingOn = false;
					CallbackBus<SteamServerConnectFailure_t>.Fire(new SteamServerConnectFailure_t { m_eResult = EResult.k_EResultNoConnection });
					return "";
				}

				SessionState.SessionToken = bootToken;
				var loginBody = "{\"gamePort\":" + _gamePort + ",\"queryPort\":" + _queryPort
					+ ",\"versionString\":" + WebworksJson.Quote(_version)
					+ ",\"dedicated\":" + (_dedicated ? "true" : "false") + "}";
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/GameServer/LogOn", loginBody));
				var token = payload != null ? WebworksJson.Str(payload, "sessionToken") : null;
				var serverId = payload != null ? WebworksJson.Str(payload, "serverId") : null;
				_loggingOn = false;

				if (token == null || serverId == null)
				{
					CallbackBus<SteamServerConnectFailure_t>.Fire(new SteamServerConnectFailure_t { m_eResult = EResult.k_EResultFail });
					return "";
				}

				SessionState.SessionToken = token;
				ulong parsed;
				ulong.TryParse(serverId, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
				_steamId = parsed;
				_loggedOn = true;
				_heartbeat = 0f;
				CallbackBus<SteamServersConnected_t>.Fire(new SteamServersConnected_t());
				FlushKeys();
				Heartbeat();
				return "";
			}, "game server logon");
		}

		public static void LogOn(string pszToken) { LogOnAnonymous(); }

		public static void LogOff()
		{
			if (!_loggedOn) return;
			_loggedOn = false;
			ApiClient.Fire(() => ApiClient.PostAsync("/GameServer/LogOff", "{}"), "game server logoff");
			CallbackBus<SteamServersDisconnected_t>.Fire(new SteamServersDisconnected_t { m_eResult = EResult.k_EResultOK });
		}

		public static bool BLoggedOn() { return _loggedOn; }
		public static bool BSecure() { return _secure; }
		public static CSteamID GetSteamID() { return new CSteamID(_steamId); }
		public static bool WasRestartRequested() { return false; }

		public static void SetMaxPlayerCount(int cPlayersMax) { _maxPlayers = cPlayersMax; }
		public static void SetBotPlayerCount(int cBotplayers) { _botCount = cBotplayers; }
		public static void SetServerName(string pszServerName) { _serverName = pszServerName ?? ""; }
		public static void SetMapName(string pszMapName) { _mapName = pszMapName ?? ""; }
		public static void SetPasswordProtected(bool bPasswordProtected) { _passworded = bPasswordProtected; }
		public static void SetSpectatorPort(ushort unSpectatorPort) { _spectatorPort = unSpectatorPort; }
		public static void SetSpectatorServerName(string pszSpectatorServerName) { _spectatorName = pszSpectatorServerName ?? ""; }
		public static void ClearAllKeyValues() { _keys.Clear(); FlushKeys(true); }
		public static void SetGameTags(string pchGameTags) { _gameTags = pchGameTags ?? ""; }
		public static void SetGameData(string pchGameData) { _gameData = pchGameData ?? ""; }
		public static void SetRegion(string pszRegion) { _region = pszRegion ?? ""; }
		public static void SetAdvertiseServerActive(bool bActive) { _advertise = bActive; }
		public static void SetModDir(string pszModDir) { _modDir = pszModDir ?? ""; }
		public static void SetDedicatedServer(bool bDedicated) { _dedicated = bDedicated; }
		public static void SetProduct(string pszProduct) { _product = pszProduct ?? ""; }
		public static void SetGameDescription(string pszGameDescription) { _gameDescription = pszGameDescription ?? ""; }

		public static void SetKeyValue(string pKey, string pValue)
		{
			if (string.IsNullOrEmpty(pKey)) return;
			_keys[pKey] = pValue ?? "";
			FlushKeys();
		}

		static void FlushKeys(bool clearAll = false)
		{
			if (!_loggedOn) return;
			var body = new StringBuilder("{\"clearAll\":").Append(clearAll ? "true" : "false").Append(",\"set\":{");
			int index = 0;
			foreach (var kv in _keys) body.Append(index++ > 0 ? "," : "").Append(WebworksJson.Quote(kv.Key)).Append(":").Append(WebworksJson.Quote(kv.Value));
			body.Append("}}");
			ApiClient.Fire(() => ApiClient.PostAsync("/GameServer/Keys", body.ToString()), "game server keys");
		}

		static void Heartbeat()
		{
			if (!_loggedOn) return;
			var body = "{\"name\":" + WebworksJson.Quote(_serverName)
				+ ",\"map\":" + WebworksJson.Quote(_mapName)
				+ ",\"maxPlayers\":" + _maxPlayers
				+ ",\"botCount\":" + _botCount
				+ ",\"tags\":" + WebworksJson.Quote(_gameTags)
				+ ",\"passworded\":" + (_passworded ? "true" : "false")
				+ ",\"gamePort\":" + _gamePort
				+ ",\"queryPort\":" + _queryPort + "}";
			ApiClient.Fire(() => ApiClient.PostAsync("/GameServer/Heartbeat", body), "game server heartbeat");
		}

		public static void RunCallbacks()
		{
			if (!_loggedOn) return;
			_heartbeat += Time.deltaTime;
			if (_heartbeat < _heartbeatInterval) return;
			_heartbeat = 0f;
			Heartbeat();
		}

		public static void ForceHeartbeat()
		{
			_heartbeat = 0f;
			Heartbeat();
		}

		public static HAuthTicket GetAuthSessionTicket(byte[] pTicket, int cbMaxTicket, out uint pcbTicket, ref SteamNetworkingIdentity pSnid)
		{
			pcbTicket = 0;
			if (!_loggedOn) return HAuthTicket.Invalid;

			var handle = new HAuthTicket((uint)(DateTime.UtcNow.Ticks & 0xffffffff));

#if UNITY_WEBGL && !UNITY_EDITOR
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/User/AuthTicket", "{}"));
				var ticket = payload != null ? WebworksJson.Str(payload, "ticket") : null;
				if (ticket != null && pTicket != null && cbMaxTicket > 0)
				{
					var bytes = Encoding.UTF8.GetBytes(ticket);
					var count = Math.Min(bytes.Length, cbMaxTicket);
					Array.Copy(bytes, pTicket, count);
				}
				CallbackBus<GetAuthSessionTicketResponse_t>.Fire(new GetAuthSessionTicketResponse_t
				{
					m_hAuthTicket = handle,
					m_eResult = ticket != null ? EResult.k_EResultOK : EResult.k_EResultFail,
				});
				return "";
			}, "GetAuthSessionTicket");
#else
			try
			{
				var payload = WebworksJson.Obj(ApiClient.PostAsync("/User/AuthTicket", "{}").GetAwaiter().GetResult());
				var ticket = payload != null ? WebworksJson.Str(payload, "ticket") : null;
				if (ticket == null) return HAuthTicket.Invalid;

				var bytes = Encoding.UTF8.GetBytes(ticket);
				if (pTicket != null && cbMaxTicket > 0)
				{
					var count = Math.Min(bytes.Length, cbMaxTicket);
					Array.Copy(bytes, pTicket, count);
					pcbTicket = (uint)count;
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Webworks] GetAuthSessionTicket failed: " + ex.Message);
				return HAuthTicket.Invalid;
			}
#endif

			return handle;
		}

		public static EBeginAuthSessionResult BeginAuthSession(byte[] pAuthTicket, int cbAuthTicket, CSteamID steamID)
		{
			if (pAuthTicket == null || cbAuthTicket <= 0) return EBeginAuthSessionResult.k_EBeginAuthSessionResultInvalidTicket;
			var ticket = Encoding.UTF8.GetString(pAuthTicket, 0, Math.Min(cbAuthTicket, pAuthTicket.Length));

			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/User/BeginAuthSession", "{\"ticket\":" + WebworksJson.Quote(ticket) + "}"));
				var valid = payload != null && WebworksJson.Bool(payload, "valid");
				var owner = payload != null ? WebworksJson.Str(payload, "steamId") : null;
				ulong ownerId;
				ulong.TryParse(owner ?? "0", NumberStyles.Integer, CultureInfo.InvariantCulture, out ownerId);

				_authSessions[steamID] = valid;
				CallbackBus<ValidateAuthTicketResponse_t>.Fire(new ValidateAuthTicketResponse_t
				{
					m_SteamID = steamID,
					m_OwnerSteamID = new CSteamID(ownerId == 0 ? steamID.m_SteamID : ownerId),
					m_eAuthSessionResponse = valid ? EAuthSessionResponse.k_EAuthSessionResponseOK : EAuthSessionResponse.k_EAuthSessionResponseAuthTicketInvalid,
				});
				return "";
			}, "BeginAuthSession");

			return EBeginAuthSessionResult.k_EBeginAuthSessionResultOK;
		}

		public static void EndAuthSession(CSteamID steamID) { _authSessions.Remove(steamID); }
		public static void CancelAuthTicket(HAuthTicket hAuthTicket) { }
		public static EUserHasLicenseForAppResult UserHasLicenseForApp(CSteamID steamID, AppId_t appID) { return EUserHasLicenseForAppResult.k_EUserHasLicenseResultHasLicense; }
		public static bool RequestUserGroupStatus(CSteamID steamIDUser, CSteamID steamIDGroup) { return false; }
		public static void GetGameplayStats() { }
		public static SteamAPICall_t GetServerReputation() { return ResultBus.NewCall(); }
		public static SteamIPAddress_t GetPublicIP() { return default(SteamIPAddress_t); }
		public static bool HandleIncomingPacket(byte[] pData, int cbData, uint srcIP, ushort srcPort) { return false; }
		public static int GetNextOutgoingPacket(byte[] pOut, int cbMaxOut, out uint pNetAdr, out ushort pPort) { pNetAdr = 0; pPort = 0; return 0; }
		public static SteamAPICall_t AssociateWithClan(CSteamID steamIDClan) { return ResultBus.NewCall(); }
		public static SteamAPICall_t ComputeNewPlayerCompatibility(CSteamID steamIDNewPlayer) { return ResultBus.NewCall(); }

		public static bool SendUserConnectAndAuthenticate_DEPRECATED(uint unIPClient, byte[] pvAuthBlob, uint cubAuthBlobSize, out CSteamID pSteamIDUser)
		{
			pSteamIDUser = CSteamID.Nil;
			return false;
		}

		public static CSteamID CreateUnauthenticatedUserConnection() { return CSteamID.Nil; }
		public static void SendUserDisconnect_DEPRECATED(CSteamID steamIDUser) { }

		public static bool BUpdateUserData(CSteamID steamIDUser, string pchPlayerName, uint uScore)
		{
			if (!_loggedOn) return false;
			var body = "{\"steamId\":" + WebworksJson.Quote(steamIDUser.m_SteamID.ToString(CultureInfo.InvariantCulture))
				+ ",\"name\":" + WebworksJson.Quote(pchPlayerName ?? "")
				+ ",\"score\":" + uScore + "}";
			ApiClient.Fire(() => ApiClient.PostAsync("/GameServer/Player", body), "BUpdateUserData");
			return true;
		}
	}
}
