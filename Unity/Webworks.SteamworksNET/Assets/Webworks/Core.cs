using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{

	public struct CSteamID : IEquatable<CSteamID>, IComparable<CSteamID>
	{
		public ulong m_SteamID;

		public CSteamID(ulong steamId) { m_SteamID = steamId; }

		public static readonly CSteamID Nil = new CSteamID(0);
		public static readonly CSteamID OutofDateGS = new CSteamID(1);
		public static readonly CSteamID LanModeGS = new CSteamID(2);
		public static readonly CSteamID NotInitYetGS = new CSteamID(3);
		public static readonly CSteamID NonSteamGS = new CSteamID(4);

		public void Clear() { m_SteamID = 0; }
		public uint GetAccountID() { return (uint)(m_SteamID & 0xFFFFFFFFul); }
		public bool IsValid() { return m_SteamID != 0; }
		public bool IsLobby() { return true; }
		public bool BIndividualAccount() { return m_SteamID != 0; }
		public EAccountType GetEAccountType() { return EAccountType.k_EAccountTypeIndividual; }
		public EUniverse GetEUniverse() { return EUniverse.k_EUniversePublic; }

		public static implicit operator ulong(CSteamID id) { return id.m_SteamID; }
		public static implicit operator CSteamID(ulong id) { return new CSteamID(id); }

		public override string ToString() { return m_SteamID.ToString(); }
		public override int GetHashCode() { return m_SteamID.GetHashCode(); }
		public override bool Equals(object o) { return o is CSteamID && ((CSteamID)o).m_SteamID == m_SteamID; }
		public bool Equals(CSteamID o) { return m_SteamID == o.m_SteamID; }
		public int CompareTo(CSteamID o) { return m_SteamID.CompareTo(o.m_SteamID); }
		public static bool operator ==(CSteamID a, CSteamID b) { return a.m_SteamID == b.m_SteamID; }
		public static bool operator !=(CSteamID a, CSteamID b) { return a.m_SteamID != b.m_SteamID; }
	}

	public struct AppId_t : IEquatable<AppId_t>, IComparable<AppId_t>
	{
		public uint m_AppId;
		public AppId_t(uint value) { m_AppId = value; }
		public static readonly AppId_t Invalid = new AppId_t(0);
		public static explicit operator AppId_t(uint value) { return new AppId_t(value); }
		public static implicit operator uint(AppId_t id) { return id.m_AppId; }
		public override string ToString() { return m_AppId.ToString(); }
		public override int GetHashCode() { return (int)m_AppId; }
		public override bool Equals(object o) { return o is AppId_t && ((AppId_t)o).m_AppId == m_AppId; }
		public bool Equals(AppId_t o) { return m_AppId == o.m_AppId; }
		public int CompareTo(AppId_t o) { return m_AppId.CompareTo(o.m_AppId); }
		public static bool operator ==(AppId_t a, AppId_t b) { return a.m_AppId == b.m_AppId; }
		public static bool operator !=(AppId_t a, AppId_t b) { return a.m_AppId != b.m_AppId; }
	}

	public struct SteamAPICall_t : IEquatable<SteamAPICall_t>
	{
		public ulong m_SteamAPICall;
		public SteamAPICall_t(ulong value) { m_SteamAPICall = value; }
		public static readonly SteamAPICall_t Invalid = new SteamAPICall_t(0x0);
		public static explicit operator SteamAPICall_t(ulong value) { return new SteamAPICall_t(value); }
		public static explicit operator ulong(SteamAPICall_t call) { return call.m_SteamAPICall; }
		public override string ToString() { return m_SteamAPICall.ToString(); }
		public override int GetHashCode() { return m_SteamAPICall.GetHashCode(); }
		public override bool Equals(object o) { return o is SteamAPICall_t && ((SteamAPICall_t)o).m_SteamAPICall == m_SteamAPICall; }
		public bool Equals(SteamAPICall_t o) { return m_SteamAPICall == o.m_SteamAPICall; }
		public static bool operator ==(SteamAPICall_t a, SteamAPICall_t b) { return a.m_SteamAPICall == b.m_SteamAPICall; }
		public static bool operator !=(SteamAPICall_t a, SteamAPICall_t b) { return a.m_SteamAPICall != b.m_SteamAPICall; }
	}

	public struct HAuthTicket : IEquatable<HAuthTicket>
	{
		public uint m_HAuthTicket;
		public HAuthTicket(uint value) { m_HAuthTicket = value; }
		public static readonly HAuthTicket Invalid = new HAuthTicket(0);
		public override int GetHashCode() { return (int)m_HAuthTicket; }
		public override bool Equals(object o) { return o is HAuthTicket && ((HAuthTicket)o).m_HAuthTicket == m_HAuthTicket; }
		public bool Equals(HAuthTicket o) { return m_HAuthTicket == o.m_HAuthTicket; }
		public static bool operator ==(HAuthTicket a, HAuthTicket b) { return a.m_HAuthTicket == b.m_HAuthTicket; }
		public static bool operator !=(HAuthTicket a, HAuthTicket b) { return a.m_HAuthTicket != b.m_HAuthTicket; }
	}

	public struct SteamLeaderboard_t : IEquatable<SteamLeaderboard_t>
	{
		public ulong m_SteamLeaderboard;
		public SteamLeaderboard_t(ulong value) { m_SteamLeaderboard = value; }
		public static readonly SteamLeaderboard_t Invalid = new SteamLeaderboard_t(0);
		public override int GetHashCode() { return m_SteamLeaderboard.GetHashCode(); }
		public override bool Equals(object o) { return o is SteamLeaderboard_t && ((SteamLeaderboard_t)o).m_SteamLeaderboard == m_SteamLeaderboard; }
		public bool Equals(SteamLeaderboard_t o) { return m_SteamLeaderboard == o.m_SteamLeaderboard; }
		public static bool operator ==(SteamLeaderboard_t a, SteamLeaderboard_t b) { return a.m_SteamLeaderboard == b.m_SteamLeaderboard; }
		public static bool operator !=(SteamLeaderboard_t a, SteamLeaderboard_t b) { return a.m_SteamLeaderboard != b.m_SteamLeaderboard; }
	}

	public struct SteamLeaderboardEntries_t : IEquatable<SteamLeaderboardEntries_t>
	{
		public ulong m_SteamLeaderboardEntries;
		public SteamLeaderboardEntries_t(ulong value) { m_SteamLeaderboardEntries = value; }
		public static readonly SteamLeaderboardEntries_t Invalid = new SteamLeaderboardEntries_t(0);
		public override int GetHashCode() { return m_SteamLeaderboardEntries.GetHashCode(); }
		public override bool Equals(object o) { return o is SteamLeaderboardEntries_t && ((SteamLeaderboardEntries_t)o).m_SteamLeaderboardEntries == m_SteamLeaderboardEntries; }
		public bool Equals(SteamLeaderboardEntries_t o) { return m_SteamLeaderboardEntries == o.m_SteamLeaderboardEntries; }
		public static bool operator ==(SteamLeaderboardEntries_t a, SteamLeaderboardEntries_t b) { return a.m_SteamLeaderboardEntries == b.m_SteamLeaderboardEntries; }
		public static bool operator !=(SteamLeaderboardEntries_t a, SteamLeaderboardEntries_t b) { return a.m_SteamLeaderboardEntries != b.m_SteamLeaderboardEntries; }
	}

	public struct PublishedFileId_t : IEquatable<PublishedFileId_t>
	{
		public ulong m_PublishedFileId;
		public PublishedFileId_t(ulong value) { m_PublishedFileId = value; }
		public static readonly PublishedFileId_t Invalid = new PublishedFileId_t(0);
		public override int GetHashCode() { return m_PublishedFileId.GetHashCode(); }
		public override bool Equals(object o) { return o is PublishedFileId_t && ((PublishedFileId_t)o).m_PublishedFileId == m_PublishedFileId; }
		public bool Equals(PublishedFileId_t o) { return m_PublishedFileId == o.m_PublishedFileId; }
		public static bool operator ==(PublishedFileId_t a, PublishedFileId_t b) { return a.m_PublishedFileId == b.m_PublishedFileId; }
		public static bool operator !=(PublishedFileId_t a, PublishedFileId_t b) { return a.m_PublishedFileId != b.m_PublishedFileId; }
	}

	

	
	

	
	
	
	

	

	

	
	
	

	

	
	

	
	
	
	

	
	
	

	public struct LobbyCreated_t { public EResult m_eResult; public ulong m_ulSteamIDLobby; }
	public struct LobbyEnter_t { public ulong m_ulSteamIDLobby; public uint m_rgfChatPermissions; public bool m_bLocked; public uint m_EChatRoomEnterResponse; }
	public struct LobbyDataUpdate_t { public ulong m_ulSteamIDLobby; public ulong m_ulSteamIDMember; public byte m_bSuccess; }
	public struct LobbyChatUpdate_t { public ulong m_ulSteamIDLobby; public ulong m_ulSteamIDUserChanged; public ulong m_ulSteamIDMakingChange; public uint m_rgfChatMemberStateChange; }
	public struct LobbyChatMsg_t { public ulong m_ulSteamIDLobby; public ulong m_ulSteamIDUser; public byte m_eChatEntryType; public uint m_iChatID; }
	public struct LobbyMatchList_t { public uint m_nLobbiesMatching; }
	public struct LobbyGameCreated_t { public ulong m_ulSteamIDLobby; public ulong m_ulSteamIDGameServer; public uint m_unIP; public ushort m_usPort; }
	public struct LobbyInvite_t { public ulong m_ulSteamIDUser; public ulong m_ulSteamIDLobby; public ulong m_ulGameID; }
	public struct GameLobbyJoinRequested_t { public ulong m_steamIDLobby; public ulong m_steamIDFriend; }
	public struct GameRichPresenceJoinRequested_t { public ulong m_steamIDFriend; public string m_rgchConnect; }

	public struct FriendChatMessage_t { public ulong m_ulSteamIDFriend; public string m_szMessage; }

	public struct PersonaStateChange_t { public ulong m_ulSteamID; public int m_nChangeFlags; }
	public struct GameOverlayActivated_t { public byte m_bActive; }
	public struct SteamServersConnected_t { }
	public struct SteamServersDisconnected_t { public EResult m_eResult; }
	public struct SteamServerConnectFailure_t { public EResult m_eResult; public bool m_bStillRetrying; }

	public struct UserStatsReceived_t { public ulong m_nGameID; public EResult m_eResult; public ulong m_steamIDUser; }
	public struct UserStatsStored_t { public ulong m_nGameID; public EResult m_eResult; }
	public struct UserAchievementStored_t { public ulong m_nGameID; public bool m_bGroupAchievement; public string m_rgchAchievementName; public uint m_nCurProgress; public uint m_nMaxProgress; }
	public struct UserAchievementIconFetched_t { public ulong m_nGameID; public string m_rgchAchievementName; public bool m_bAchieved; public int m_nIconHandle; }
	public struct GlobalStatsReceived_t { public ulong m_nGameID; public EResult m_eResult; }

	public struct LeaderboardFindResult_t { public ulong m_hSteamLeaderboard; public byte m_bLeaderboardFound; }
	public struct LeaderboardScoresDownloaded_t { public ulong m_hSteamLeaderboard; public ulong m_hSteamLeaderboardEntries; public int m_cEntryCount; }
	public struct LeaderboardScoreUploaded_t { public byte m_bSuccess; public ulong m_hSteamLeaderboard; public int m_nScore; public byte m_bScoreChanged; public int m_nGlobalRankNew; public int m_nGlobalRankPrevious; }
	public struct LeaderboardEntry_t { public CSteamID m_steamIDUser; public int m_nGlobalRank; public int m_nScore; public int m_cDetails; public ulong m_hUGC; }

	public struct P2PSessionRequest_t { public CSteamID m_steamIDRemote; }
	public struct P2PSessionConnectFail_t { public CSteamID m_steamIDRemote; public byte m_eP2PSessionError; }
	public struct P2PSessionState_t { public byte m_bConnectionActive; public byte m_bConnecting; public byte m_eP2PSessionError; public byte m_bUsingRelay; public int m_nBytesQueuedForSend; public int m_nPacketsQueuedForSend; public uint m_nRemoteIP; public ushort m_nRemotePort; }

	public struct GetAuthSessionTicketResponse_t { public HAuthTicket m_hAuthTicket; public EResult m_eResult; }
	public struct ValidateAuthTicketResponse_t { public CSteamID m_SteamID; public EAuthSessionResponse m_eAuthSessionResponse; public CSteamID m_OwnerSteamID; }

	public struct GamepadTextInputDismissed_t { public bool m_bSubmitted; public uint m_unSubmittedText; }
	public struct FloatingGamepadTextInputDismissed_t { }

	public struct RemoteStorageFileWriteAsyncComplete_t { public EResult m_eResult; }
	public struct RemoteStorageFileReadAsyncComplete_t { public SteamAPICall_t m_hFileReadAsync; public EResult m_eResult; public uint m_nOffset; public uint m_cubRead; }

	public sealed class Callback<T>
	{
		Action<T> _func;
		Callback() { }

		public static Callback<T> Create(Action<T> func) { var c = new Callback<T> { _func = func }; CallbackBus<T>.Add(c.Invoke); c._registered = true; return c; }
		public static Callback<T> CreateGameServer(Action<T> func) { return Create(func); }

		bool _registered;
		void Invoke(T v) { _func?.Invoke(v); }

		public void Dispose() { if (_registered) { CallbackBus<T>.Remove(Invoke); _registered = false; } }
		public void Unregister() { Dispose(); }
	}

	public sealed class CallResult<T>
	{
		Action<T, bool> _func;
		CallResult() { }

		public static CallResult<T> Create(Action<T, bool> func = null) { return new CallResult<T> { _func = func }; }

		public void Set(SteamAPICall_t call, Action<T, bool> onComplete = null)
		{
			ResultBus.Await(call, onComplete ?? _func ?? ((r, f) => { }));
		}

		public bool IsActive() { return ResultBus.IsPending(); }
		public void Cancel() { }
	}

	public delegate void SteamAPIWarningMessageHook_t(int nSeverity, System.Text.StringBuilder pchDebugText);

	public static partial class SteamClient
	{
		public static string DesiredName { get; private set; }
		internal static string DesiredAccountToken;
		public static string LinkedAccount { get { return SessionState.LinkedAccount; } }

		public static void SetDesiredName(string name)
		{
			if (SessionState.Initialized) { Debug.LogWarning("[Webworks] SetDesiredName() called after SteamAPI.Init() -- it only takes effect on the next session."); return; }
			DesiredName = string.IsNullOrEmpty(name) ? null : name;
		}

		public static void SetAccountToken(string accountToken)
		{
			if (SessionState.Initialized) { Debug.LogWarning("[Webworks] SetAccountToken() called after SteamAPI.Init() -- it only takes effect on the next session."); return; }
			DesiredAccountToken = string.IsNullOrEmpty(accountToken) ? null : accountToken;
		}

		static SteamAPIWarningMessageHook_t _hook;
		public static void SetWarningMessageHook(SteamAPIWarningMessageHook_t pFunction) { _hook = pFunction; }
		public static IntPtr GetHSteamPipe() { return new IntPtr(1); }
		public static IntPtr GetHSteamUser() { return new IntPtr(1); }
	}

	public static class Packsize
	{
		public static bool Test() { return true; }
	}

	public static class DllCheck
	{
		public static bool Test() { return true; }
	}

	public static class SteamAPI
	{
		public static bool Init()
		{
			WebworksConfig.Load();
			WebworksConfig.PrepareDeviceKey();
			if (SessionState.Initialized) return true;

#if !(UNITY_WEBGL && !UNITY_EDITOR)
			try
			{
				var init = Task.Run(() => InitAsync());
				if (!init.Wait(WebworksConfig.RequestTimeoutMs * 2))
					throw new TimeoutException("timed out after " + (WebworksConfig.RequestTimeoutMs * 2) + " ms");
			}
			catch (AggregateException ex)
			{
				Debug.LogError("[Webworks] SteamAPI.Init failed (is the server running at " + WebworksConfig.ServerUrl + "?): " + ex.GetBaseException().Message);
				return false;
			}
			catch (TimeoutException ex)
			{
				Debug.LogError("[Webworks] SteamAPI.Init " + ex.Message + " (is the server running at " + WebworksConfig.ServerUrl + "?)");
				return false;
			}
			return true;
#else
			if (WebworksConfig.IdentityMode != "guest")
			{
				Debug.LogError("[Webworks] identity_mode=bootstrap is not implemented yet -- set identity_mode=guest in shim.config.txt.");
				return false;
			}

			var raw = WebworksNative.Webworks_HttpSync("POST", ApiClient.FullUrl("/User/GuestSession"), GuestSessionBody(), null);
			var session = WebworksJson.Obj(raw);
			if (session == null || WebworksJson.Str(session, "sessionToken") == null)
			{
				Debug.LogError("[Webworks] GuestSession failed -- is the server running at " + WebworksConfig.ServerUrl + " (and does it allow CORS from this page)?");
				return false;
			}

			var started = StartSession(session);
			if (started.IsFaulted) { Debug.LogError("[Webworks] " + started.Exception.GetBaseException().Message); return false; }
			return true;
#endif
		}

		static string GuestSessionBody()
		{
			var appId = WebworksConfig.ResolveAppId(0);
			var body = "{\"appId\":" + WebworksJson.Quote(appId);
			if (WebworksConfig.PersistentId && WebworksConfig.DeviceKey != null) body += ",\"deviceKey\":" + WebworksJson.Quote(WebworksConfig.DeviceKey);
			if (!string.IsNullOrEmpty(SteamClient.DesiredName)) body += ",\"name\":" + WebworksJson.Quote(SteamClient.DesiredName);
			if (!string.IsNullOrEmpty(SteamClient.DesiredAccountToken)) body += ",\"accountToken\":" + WebworksJson.Quote(SteamClient.DesiredAccountToken);
			return body + "}";
		}

		static async Task InitAsync()
		{
			WebworksConfig.Load();
			if (WebworksConfig.IdentityMode != "guest")
				throw new NotImplementedException("identity_mode=bootstrap is not implemented yet -- set identity_mode=guest in shim.config.txt.");

			var d = WebworksJson.Obj(await ApiClient.PostAsync("/User/GuestSession", GuestSessionBody()));
			await StartSession(d);
		}

		static async Task StartSession(Dictionary<string, object> d)
		{
			var token = WebworksJson.Str(d, "sessionToken");
			if (token == null) throw new InvalidOperationException("Webworks: GuestSession failed -- is the server running at server_url?");

			SessionState.SessionToken = token;
			SteamUGC.WarmUp();
			SessionState.SteamId = WebworksJson.Str(d, "steamId");
			SessionState.Name = WebworksJson.Str(d, "name");
			SessionState.AppId = WebworksJson.Str(d, "appId");
			SessionState.LinkedAccount = WebworksJson.Str(d, "linkedAccount");
			SessionState.Initialized = true;
			ulong myId; ulong.TryParse(SessionState.SteamId, out myId);
			SNetState.Players.Set(myId, SessionState.Name);
			Debug.Log("[Webworks] Guest session started: " + SessionState.Name + " (" + SessionState.SteamId + ") for app id " + SessionState.AppId);

			SNetState.Realtime = new RealtimeClient();
			SNetState.Realtime.OnMessage += json => MainThreadDispatcher.Enqueue(() => RealtimeDispatch.HandleMessage(json));
			SNetState.Realtime.OnDisconnected += () => MainThreadDispatcher.Enqueue(() =>
			{
				Debug.LogWarning("[Webworks] realtime channel disconnected");
				CallbackBus<SteamServersDisconnected_t>.Fire(new SteamServersDisconnected_t { m_eResult = EResult.k_EResultNoConnection });
			});
			await SNetState.Realtime.ConnectAsync();
			CallbackBus<SteamServersConnected_t>.Fire(new SteamServersConnected_t());

#if UNITY_WEBGL && !UNITY_EDITOR
			// Browser builds keep RemoteStorage files in IndexedDB "WEBWORKS-SAVES".
			SteamRemoteStorage.InitWebStore();
#else
			try { await SteamRemoteStorage.LoadAsync(); }
			catch (Exception ex) { Debug.LogWarning("[Webworks] could not load RemoteStorage files: " + ex.Message); }
#endif

			SteamUserStats.RequestCurrentStats();
			SteamUser.PrefetchTicket();
			SteamFriends.LoadPersonaState();
			SteamFriends.RefreshFriendsList();
		}

		public static void RunCallbacks()
		{
			Webworks.SNetInternal.MediaPump.Poll();
#if UNITY_WEBGL && !UNITY_EDITOR
			SteamRemoteStorage.PollWebStore();
#endif
		}
		public static void Shutdown()
		{
			if (SNetState.Realtime != null) SNetState.Realtime.Close();
			SessionState.Initialized = false;
		}

		public static bool RestartAppIfNecessary(AppId_t unOwnAppID) { return false; }
		public static bool IsSteamRunning() { return true; }
		public static uint GetHSteamUser() { return 1; }

		public static bool IsAPICallCompleted(SteamAPICall_t hSteamAPICall, out bool bFailed) { bFailed = false; return false; }
	}
}

namespace Webworks.SNetInternal
{
	using Steamworks;

	internal static class CallbackBus<T>
	{
		static event Action<T> _handlers;
		public static void Add(Action<T> h) { _handlers += h; }
		public static void Remove(Action<T> h) { _handlers -= h; }
		public static void Fire(T v) { Callbacks.Raise(_handlers, v); }
	}

	internal static class ResultBus
	{
		static ulong _next = 1;
		static readonly Dictionary<ulong, Delegate> _pending = new Dictionary<ulong, Delegate>();

		public static SteamAPICall_t NewCall() { return new SteamAPICall_t(_next++); }

		public static void Await<T>(SteamAPICall_t call, Action<T, bool> handler)
		{
			_pending[call.m_SteamAPICall] = handler;
		}

		public static void Complete<T>(SteamAPICall_t call, T result, bool ioFailure = false)
		{
			Delegate h;
			if (_pending.TryGetValue(call.m_SteamAPICall, out h))
			{
				_pending.Remove(call.m_SteamAPICall);
				Callbacks.Raise((Action<T, bool>)h, result, ioFailure);
			}
			CallbackBus<T>.Fire(result);
		}

		public static bool IsPending() { return _pending.Count > 0; }
	}
}
