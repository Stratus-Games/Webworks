#pragma warning disable CS0067
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks.Data;
using Webworks.Internal;

namespace Steamworks
{
	public struct SteamId : IEquatable<SteamId>
	{
		public ulong Value;

		public static implicit operator SteamId(ulong value) { return new SteamId { Value = value }; }
		public static implicit operator ulong(SteamId id) { return id.Value; }

		public uint AccountId { get { return (uint)(Value & 0xFFFFFFFFul); } }
		public bool IsValid { get { return Value != 0; } }

		public override string ToString() { return Value.ToString(); }
		public override int GetHashCode() { return Value.GetHashCode(); }
		public override bool Equals(object obj) { return obj is SteamId && ((SteamId)obj).Value == Value; }
		public bool Equals(SteamId other) { return Value == other.Value; }
		public static bool operator ==(SteamId a, SteamId b) { return a.Value == b.Value; }
		public static bool operator !=(SteamId a, SteamId b) { return a.Value != b.Value; }
	}

	public struct AppId
	{
		public uint Value;
		public static implicit operator AppId(uint value) { return new AppId { Value = value }; }
		public static implicit operator uint(AppId id) { return id.Value; }
		public override string ToString() { return Value.ToString(); }
	}

	public partial struct Friend : IEquatable<Friend>
	{
		public SteamId Id;

		public Friend(SteamId id) { Id = id; }

		public string Name
		{
			get
			{
				if (Id == SteamClient.SteamId) return SteamClient.Name;
				return PlayerDirectory.Get(Id.Value) ?? "Unknown";
			}
		}

		public bool IsMe { get { return Id == SteamClient.SteamId; } }
		public bool IsFriend { get { return IsMe || FriendDirectory.IsFriend(Id.Value); } }
		public bool IsOnline { get { return IsMe || FriendDirectory.StateOf(Id.Value) == FriendState.Online; } }
		public bool IsAway { get { return false; } }
		public bool IsBusy { get { return false; } }
		public bool IsPlaying { get { return true; } }
		public bool IsPlayingThisGame { get { return true; } }

		public FriendGameInfo? GameInfo { get { return new FriendGameInfo { GameID = SteamClient.AppId.Value }; } }

		public string GetRichPresence(string key) { return null; }

		public Task<Image?> GetSmallAvatarAsync() { return SteamFriends.GetSmallAvatarAsync(Id); }
		public Task<Image?> GetMediumAvatarAsync() { return SteamFriends.GetMediumAvatarAsync(Id); }
		public Task<Image?> GetLargeAvatarAsync() { return SteamFriends.GetLargeAvatarAsync(Id); }

		public override string ToString() { return Name; }
		public override int GetHashCode() { return Id.GetHashCode(); }
		public override bool Equals(object obj) { return obj is Friend && ((Friend)obj).Id == Id; }
		public bool Equals(Friend other) { return Id == other.Id; }
		public static bool operator ==(Friend a, Friend b) { return a.Id == b.Id; }
		public static bool operator !=(Friend a, Friend b) { return a.Id != b.Id; }
	}

	public enum P2PSend : int { Unreliable = 0, UnreliableNoDelay = 1, Reliable = 2, ReliableWithBuffering = 3 }

	public enum P2PSessionError : int { None = 0, NotRunningApp = 1, NoRightsToApp = 2, DestinationNotLoggedIn = 3, Timeout = 4, Max = 5 }

	public enum RoomEnter : int
	{
		Success = 1, DoesntExist = 2, NotAllowed = 3, Full = 4, Error = 5, Banned = 6, Limited = 7,
		ClanDisabled = 8, CommunityBan = 9, MemberBlockedYou = 10, YouBlockedMember = 11, RatelimitExceeded = 15,
	}

	public enum LobbyType : int { Private = 0, FriendsOnly = 1, Public = 2, Invisible = 3, PrivateUnique = 4 }

	public enum Result : int
	{
		None = 0, OK = 1, Fail = 2, NoConnection = 3, InvalidPassword = 5, LoggedInElsewhere = 6,
		InvalidProtocolVer = 7, InvalidParam = 8, FileNotFound = 9, Busy = 10, InvalidState = 11,
		InvalidName = 12, InvalidEmail = 13, DuplicateName = 14, AccessDenied = 15, Timeout = 16,
		Banned = 17, AccountNotFound = 18, InvalidSteamID = 19, ServiceUnavailable = 20, NotLoggedOn = 21,
		Pending = 22, EncryptionFailure = 23, InsufficientPrivilege = 24, LimitExceeded = 25,
		Revoked = 26, Expired = 27, AlreadyRedeemed = 28, DuplicateRequest = 29,
	}

	public static partial class SteamClient
	{
		public static bool IsValid { get { return SessionState.Initialized; } }
		public static bool IsLoggedOn { get { return SessionState.Initialized; } }
		public static string Name { get { return SessionState.Name ?? ""; } }
		public static AppId AppId { get; private set; }

		public static string DesiredName { get; private set; }

		public static void SetDesiredName(string name)
		{
			if (SessionState.Initialized)
			{
				Debug.LogWarning("[Webworks] SetDesiredName() called after Init() -- it only takes effect on the next session.");
				return;
			}
			DesiredName = string.IsNullOrEmpty(name) ? null : name;
		}

		public static SteamId SteamId
		{
			get { ulong v; ulong.TryParse(SessionState.SteamId, out v); return v; }
		}

		public static string LinkedAccount { get { return SessionState.LinkedAccount; } }

		static string _desiredAccountToken;

		public static void SetAccountToken(string accountToken)
		{
			if (SessionState.Initialized)
			{
				Debug.LogWarning("[Webworks] SetAccountToken() called after Init() -- use LinkAccountAsync() instead.");
				return;
			}
			_desiredAccountToken = string.IsNullOrEmpty(accountToken) ? null : accountToken;
		}

		public static async Task<bool> LinkAccountAsync(string accountToken)
		{
			var d = WebworksJson.Obj(await ApiClient.PostAsync("/User/LinkAccount", "{\"accountToken\":" + WebworksJson.Quote(accountToken) + "}"));
			var linked = WebworksJson.Str(d, "linkedAccount");
			if (string.IsNullOrEmpty(linked)) return false;

			SessionState.SteamId = WebworksJson.Str(d, "steamId");
			SessionState.Name = WebworksJson.Str(d, "name");
			SessionState.LinkedAccount = linked;
			PlayerDirectory.Set(SteamId.Value, Name);
			SteamFriends.RefreshFriendsList();
			return true;
		}

		internal static RealtimeClient Realtime;

		internal static string GuestSessionBody()
		{
			WebworksConfig.PrepareDeviceKey();
			var body = "{\"appId\":" + WebworksJson.Quote(WebworksConfig.ResolveAppId(AppId.Value));
			if (WebworksConfig.PersistentId && WebworksConfig.DeviceKey != null) body += ",\"deviceKey\":" + WebworksJson.Quote(WebworksConfig.DeviceKey);
			if (!string.IsNullOrEmpty(DesiredName)) body += ",\"name\":" + WebworksJson.Quote(DesiredName);
			if (!string.IsNullOrEmpty(_desiredAccountToken)) body += ",\"accountToken\":" + WebworksJson.Quote(_desiredAccountToken);
			return body + "}";
		}

		public static bool RestartAppIfNecessary(uint appId) { return false; }

		public static bool Init(uint appId, bool asyncCallbacks = true)
		{
			AppId = appId;
			if (SessionState.Initialized) return true;

			WebworksConfig.Load();
			WebworksConfig.PrepareDeviceKey();

#if !(UNITY_WEBGL && !UNITY_EDITOR)
			try
			{
				var init = Task.Run(() => InitAsync());
				if (!init.Wait(WebworksConfig.RequestTimeoutMs * 2))
					throw new TimeoutException("timed out after " + (WebworksConfig.RequestTimeoutMs * 2) + " ms");
			}
			catch (AggregateException ex)
			{
				throw new InvalidOperationException("Webworks: SteamClient.Init failed (is the server running at " + WebworksConfig.ServerUrl + "?): " + ex.GetBaseException().Message, ex);
			}
			catch (TimeoutException ex)
			{
				throw new InvalidOperationException("Webworks: SteamClient.Init " + ex.Message + " (is the server running at " + WebworksConfig.ServerUrl + "?)", ex);
			}
			return true;
#else
			if (WebworksConfig.IdentityMode != "guest")
				throw new NotImplementedException("identity_mode=bootstrap is not implemented yet -- set identity_mode=guest in shim.config.txt.");

			var raw = WebworksNative.Webworks_HttpSync("POST", ApiClient.FullUrl("/User/GuestSession"), GuestSessionBody(), null);
			var session = WebworksJson.Obj(raw);
			if (session == null || WebworksJson.Str(session, "sessionToken") == null)
				throw new InvalidOperationException("Webworks: GuestSession failed -- is the server running at " + WebworksConfig.ServerUrl + " (and does it allow CORS from this page)?");

			var started = StartSession(session);
			if (started.IsFaulted) throw started.Exception.GetBaseException();

			try { SteamRemoteStorage.LoadFromJson(WebworksNative.Webworks_HttpSync("GET", ApiClient.FullUrl("/RemoteStorage/Files"), null, SessionState.SessionToken)); }
			catch (Exception ex) { Debug.LogWarning("[Webworks] could not load cloud files: " + ex.Message); }
			return true;
#endif
		}

		public static async Task InitAsync()
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
			SessionState.SteamId = WebworksJson.Str(d, "steamId");
			SessionState.Name = WebworksJson.Str(d, "name");
			SessionState.AppId = WebworksJson.Str(d, "appId");
			SessionState.LinkedAccount = WebworksJson.Str(d, "linkedAccount");
			SessionState.Initialized = true;
			PlayerDirectory.Set(SteamId.Value, Name);
			Debug.Log("[Webworks] Guest session started: " + Name + " (" + SteamId + ") for app id " + SessionState.AppId
				+ (SessionState.LinkedAccount != null ? " [linked account: " + SessionState.LinkedAccount + "]" : ""));

			Realtime = new RealtimeClient();
			Realtime.OnMessage += json => MainThreadDispatcher.Enqueue(() => RealtimeDispatch.HandleMessage(json));
			Realtime.OnDisconnected += () => MainThreadDispatcher.Enqueue(() => Debug.LogWarning("[Webworks] realtime channel disconnected"));
			await Realtime.ConnectAsync();

#if !(UNITY_WEBGL && !UNITY_EDITOR)
			try { await SteamRemoteStorage.LoadAsync(); }
			catch (Exception ex) { Debug.LogWarning("[Webworks] could not load cloud files: " + ex.Message); }
#endif

			SteamUserStats.RequestCurrentStats();
			SteamUser.PrefetchTicket();
			SteamFriends.RefreshFriendsList();
			SteamMatchmaking.CheckForPendingInvite();
		}

		public static void RunCallbacks() { }

		public static void Shutdown()
		{
			if (Realtime != null) Realtime.Close();
			SessionState.Initialized = false;
		}
	}
}
