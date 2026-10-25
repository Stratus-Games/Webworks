using System;
using System.Net;
using System.Threading.Tasks;
using Steamworks.Data;
using Webworks.Internal;
using Webworks.GameServer;

namespace Steamworks
{
	public struct SteamServerInit
	{
		public IPAddress IpAddress;
		public ushort SteamPort;
		public ushort GamePort;
		public ushort QueryPort;
		public bool Secure;
		public string VersionString;
		public string ModDir;
		public string GameDescription;
		public bool DedicatedServer;

		public SteamServerInit(string modDir, string gameDesc)
		{
			IpAddress = null;
			SteamPort = 0;
			GamePort = 27015;
			QueryPort = 27016;
			Secure = true;
			VersionString = "1.0.0.0";
			ModDir = modDir;
			GameDescription = gameDesc;
			DedicatedServer = true;
		}

		public SteamServerInit WithRandomSteamPort() { SteamPort = (ushort)new Random().Next(10000, 60000); return this; }
		public SteamServerInit WithQueryShareGamePort() { QueryPort = 0xFFFF; return this; }
	}

	public static class SteamServer
	{
		public static event Action<SteamNetworkingAvailability> OnSteamNetAuthenticationStatus;
		public static event Action<Result, bool> OnSteamServerConnectFailure;
		public static event Action OnSteamServersConnected;
		public static event Action<Result> OnSteamServersDisconnected;
		public static event Action<SteamId, SteamId, AuthResponse> OnValidateAuthTicketResponse;

		public static bool IsValid { get { return LoggedOn; } }
		public static bool LoggedOn { get; private set; }
		public static SteamId SteamId { get; private set; }
		public static IPAddress PublicIp { get { return IPAddress.Any; } }

		public static AppId AppId { get; private set; }
		public static bool DedicatedServer { get; set; }
		public static int MaxPlayers { get; set; }
		public static int BotCount { get; set; }
		public static string MapName { get; set; }
		public static string GameTags { get; set; }
		public static string ServerName { get; set; }
		public static bool Passworded { get; set; }
		public static string Product { get; set; }
		public static string ModDir { get; set; }
		public static string GameDescription { get; set; }
		public static bool AutomaticHeartbeats { get; set; } = true;
		public static int AutomaticHeartbeatRate { get; set; } = 15;
		public static bool AdvertiseServer { get; set; } = true;

		static SteamServerInit _init;
		static float _lastHeartbeat;
		static readonly System.Collections.Generic.Dictionary<SteamId, PendingTicket> _pendingAuth = new System.Collections.Generic.Dictionary<SteamId, PendingTicket>();
		struct PendingTicket { public string Ticket; }

		public static void Init(AppId appid, SteamServerInit init, bool asyncCallbacks = true)
		{
			AppId = appid;
			_init = init;
			DedicatedServer = init.DedicatedServer;
			ServerName = init.GameDescription;
			GameDescription = init.GameDescription;
			ModDir = init.ModDir;
			LogOnAnonymous();
		}

		public static void Shutdown()
		{
			if (LoggedOn) LogOff();
		}

		public static void RunCallbacks()
		{
			if (!LoggedOn) return;
			if (!AutomaticHeartbeats) return;

			_lastHeartbeat += UnityEngine.Time.unscaledDeltaTime;
			if (_lastHeartbeat >= Math.Max(1, AutomaticHeartbeatRate))
			{
				_lastHeartbeat = 0f;
				ForceHeartbeat();
			}
		}

		public static async void LogOnAnonymous()
		{
			try
			{
				var bootBody = "{\"appId\":" + WebworksJson.Quote(AppId.Value.ToString()) + "}";
				var boot = WebworksJson.Obj(await GameServerApi.PostAsync("/User/GuestSession", bootBody).ConfigureAwait(false));
				var bootToken = boot != null ? WebworksJson.Str(boot, "sessionToken") : null;
				if (bootToken == null) { RaiseConnectFailure(Result.NoConnection); return; }

				GameServerApi.SessionToken = bootToken;
				var loginBody = "{\"gamePort\":" + _init.GamePort + ",\"queryPort\":" + _init.QueryPort
					+ ",\"versionString\":" + WebworksJson.Quote(_init.VersionString ?? "1.0.0.0")
					+ ",\"dedicated\":" + (DedicatedServer ? "true" : "false") + "}";
				var d = WebworksJson.Obj(await GameServerApi.PostAsync("/GameServer/LogOn", loginBody).ConfigureAwait(false));
				var token = d != null ? WebworksJson.Str(d, "sessionToken") : null;
				var serverId = d != null ? WebworksJson.Str(d, "serverId") : null;
				if (token == null || serverId == null) { RaiseConnectFailure(Result.Fail); return; }

				GameServerApi.SessionToken = token;
				ulong id; ulong.TryParse(serverId, out id);
				SteamId = id;
				LoggedOn = true;
				OnSteamServersConnected?.Invoke();
				if (AutomaticHeartbeats) ForceHeartbeat();
			}
			catch (Exception)
			{
				RaiseConnectFailure(Result.NoConnection);
			}
		}

		static void RaiseConnectFailure(Result r)
		{
			OnSteamServerConnectFailure?.Invoke(r, false);
			OnSteamServersDisconnected?.Invoke(r);
		}

		public static void LogOff()
		{
			if (!LoggedOn) return;
			LoggedOn = false;
			GameServerApi.Fire(() => GameServerApi.PostAsync("/GameServer/LogOff", "{}"), "LogOff");
			GameServerApi.SessionToken = null;
			OnSteamServersDisconnected?.Invoke(Result.OK);
		}

		public static void ForceHeartbeat()
		{
			if (!LoggedOn) return;
			var body = "{\"name\":" + WebworksJson.Quote(ServerName ?? "") + ",\"map\":" + WebworksJson.Quote(MapName ?? "")
				+ ",\"maxPlayers\":" + MaxPlayers + ",\"botCount\":" + BotCount
				+ ",\"tags\":" + WebworksJson.Quote(GameTags ?? "") + ",\"passworded\":" + (Passworded ? "true" : "false") + "}";
			GameServerApi.Fire(() => GameServerApi.PostAsync("/GameServer/Heartbeat", body), "ForceHeartbeat");
		}

		static readonly System.Collections.Generic.Dictionary<string, string> _pendingKeys = new System.Collections.Generic.Dictionary<string, string>();

		public static void SetKey(string key, string value)
		{
			if (string.IsNullOrEmpty(key)) return;
			_pendingKeys[key] = value ?? "";
			FlushKeys(false);
		}

		public static void ClearKeys()
		{
			_pendingKeys.Clear();
			FlushKeys(true);
		}

		static void FlushKeys(bool clearAll)
		{
			if (!LoggedOn) return;
			var sb = new System.Text.StringBuilder("{\"clearAll\":").Append(clearAll ? "true" : "false").Append(",\"set\":{");
			int i = 0;
			foreach (var kv in _pendingKeys) sb.Append(i++ > 0 ? "," : "").Append(WebworksJson.Quote(kv.Key)).Append(":").Append(WebworksJson.Quote(kv.Value));
			sb.Append("}}");
			GameServerApi.Fire(() => GameServerApi.PostAsync("/GameServer/Keys", sb.ToString()), "SetKey/ClearKeys");
		}

		public static void UpdatePlayer(SteamId steamid, string name, int score)
		{
			if (!LoggedOn) return;
			var body = "{\"steamId\":" + WebworksJson.Quote(steamid.Value.ToString()) + ",\"name\":" + WebworksJson.Quote(name ?? "") + ",\"score\":" + score + "}";
			GameServerApi.Fire(() => GameServerApi.PostAsync("/GameServer/Player", body), "UpdatePlayer");
		}

		public static void EndSession(SteamId steamid)
		{
			if (!LoggedOn) return;
			var body = "{\"steamId\":" + WebworksJson.Quote(steamid.Value.ToString()) + ",\"end\":true}";
			GameServerApi.Fire(() => GameServerApi.PostAsync("/GameServer/Player", body), "EndSession");
			_pendingAuth.Remove(steamid);
		}

		public static bool BeginAuthSession(byte[] data, SteamId steamid)
		{
			if (!LoggedOn || data == null) return false;
			var ticket = System.Text.Encoding.UTF8.GetString(data);
			_pendingAuth[steamid] = new PendingTicket { Ticket = ticket };
			ValidateAsync(steamid, ticket);
			return true;
		}

		static async void ValidateAsync(SteamId steamid, string ticket)
		{
			try
			{
				var body = "{\"ticket\":" + WebworksJson.Quote(ticket) + "}";
				var d = WebworksJson.Obj(await GameServerApi.PostAsync("/User/BeginAuthSession", body).ConfigureAwait(false));
				var valid = d != null && WebworksJson.Bool(d, "valid");
				OnValidateAuthTicketResponse?.Invoke(steamid, steamid, valid ? AuthResponse.OK : AuthResponse.AuthTicketInvalid);
			}
			catch (Exception)
			{
				OnValidateAuthTicketResponse?.Invoke(steamid, steamid, AuthResponse.AuthTicketInvalid);
			}
		}

		public static UserHasLicenseForAppResult UserHasLicenseForApp(SteamId steamid, AppId appid) { return UserHasLicenseForAppResult.HasLicense; }

		public static OutgoingPacket? GetOutgoingPacket() { return null; }
		public static void HandleIncomingPacket(byte[] data, int size, uint address, ushort port) { }
	}
}
