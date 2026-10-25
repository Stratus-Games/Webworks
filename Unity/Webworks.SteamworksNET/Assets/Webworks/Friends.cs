#pragma warning disable CS0067
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	public static partial class SteamUser
	{
		public static CSteamID GetSteamID() { ulong v; ulong.TryParse(SessionState.SteamId, out v); return new CSteamID(v); }
		public static bool BLoggedOn() { return SessionState.Initialized; }
		public static EUserRestriction GetUserRestrictions() { return EUserRestriction.k_nUserRestrictionNone; }

		static byte[] _ticket = new byte[0];
		static DateTime _ticketExpiry = DateTime.MinValue;
		static uint _nextTicketHandle = 1;

		internal static void PrefetchTicket()
		{
			ApiClient.Fire(async () => { await FetchTicketAsync(); return (string)null; }, "PrefetchTicket");
		}

		static async Task<byte[]> FetchTicketAsync()
		{
			var t = WebworksJson.Str(WebworksJson.Obj(await ApiClient.PostAsync("/User/AuthTicket", "{}")), "ticket");
			_ticket = string.IsNullOrEmpty(t) ? new byte[0] : Convert.FromBase64String(t);
			_ticketExpiry = DateTime.UtcNow.AddMinutes(9);
			return _ticket;
		}

		public static HAuthTicket GetAuthSessionTicket(byte[] pDestBuffer, int cbMaxTicket, out uint pcbTicket)
		{
			if (DateTime.UtcNow >= _ticketExpiry) PrefetchTicket();
			var n = Math.Min(_ticket.Length, cbMaxTicket);
			if (pDestBuffer != null && n > 0) Array.Copy(_ticket, pDestBuffer, n);
			pcbTicket = (uint)n;
			var handle = new HAuthTicket(_nextTicketHandle++);
			ApiClient.Fire(async () =>
			{
				await FetchTicketAsync();
				CallbackBus<GetAuthSessionTicketResponse_t>.Fire(new GetAuthSessionTicketResponse_t { m_hAuthTicket = handle, m_eResult = EResult.k_EResultOK });
				return (string)null;
			}, "GetAuthSessionTicket");
			return handle;
		}

		public static HAuthTicket GetAuthSessionTicket(byte[] pTicket, int cbMaxTicket, out uint pcbTicket, ref SteamNetworkingIdentity pSteamNetworkingIdentity)
		{
			return GetAuthSessionTicket(pTicket, cbMaxTicket, out pcbTicket);
		}

		public static void CancelAuthTicket(HAuthTicket hAuthTicket) { }

		public static EBeginAuthSessionResult BeginAuthSession(byte[] pAuthTicket, int cbAuthTicket, CSteamID steamID)
		{
			var b64 = Convert.ToBase64String(pAuthTicket ?? new byte[0]);
			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.PostAsync("/User/BeginAuthSession", "{\"ticket\":" + WebworksJson.Quote(b64) + "}"));
				bool valid = WebworksJson.Bool(d, "valid");
				if (!valid) Debug.LogWarning("[Webworks] auth ticket from " + steamID + " failed validation");
				CallbackBus<ValidateAuthTicketResponse_t>.Fire(new ValidateAuthTicketResponse_t
				{
					m_SteamID = steamID,
					m_OwnerSteamID = steamID,
					m_eAuthSessionResponse = valid ? EAuthSessionResponse.k_EAuthSessionResponseOK : EAuthSessionResponse.k_EAuthSessionResponseAuthTicketInvalid,
				});
				return (string)null;
			}, "BeginAuthSession");
			return EBeginAuthSessionResult.k_EBeginAuthSessionResultOK;
		}

		public static void EndAuthSession(CSteamID steamID)
		{
			ApiClient.Fire(() => ApiClient.PostAsync("/User/EndAuthSession", "{\"steamId\":\"" + steamID.m_SteamID + "\"}"), "EndAuthSession");
		}

		public static EUserHasLicenseForAppResult UserHasLicenseForApp(CSteamID steamID, AppId_t appID) { return EUserHasLicenseForAppResult.k_EUserHasLicenseResultHasLicense; }
		public static bool BIsBehindNAT() { return false; }
		public static bool BIsPhoneVerified() { return true; }
		public static bool BIsPhoneIdentifying() { return true; }
		public static bool BIsPhoneRequiringVerification() { return false; }
	}

	public enum EUserRestriction
	{
		k_nUserRestrictionNone = 0, k_nUserRestrictionUnknown = 1, k_nUserRestrictionAnyChat = 2, k_nUserRestrictionVoiceChat = 4,
		k_nUserRestrictionGroupChat = 8, k_nUserRestrictionRating = 16, k_nUserRestrictionGameInvites = 32, k_nUserRestrictionTrading = 64,
	}

	public static partial class SteamFriends
	{
		static readonly Dictionary<string, string> _myRichPresence = new Dictionary<string, string>();

		public static int GetFriendCount(EFriendFlags iFriendFlags) { return new List<ulong>(SNetState.Friends.AllIds).Count; }

		public static CSteamID GetFriendByIndex(int iFriend, EFriendFlags iFriendFlags)
		{
			var list = new List<ulong>(SNetState.Friends.AllIds);
			return iFriend >= 0 && iFriend < list.Count ? new CSteamID(list[iFriend]) : CSteamID.Nil;
		}

		public static EFriendRelationship GetFriendRelationship(CSteamID steamIDFriend)
		{
			return SNetState.Friends.IsFriend(steamIDFriend.m_SteamID) ? EFriendRelationship.k_EFriendRelationshipFriend : EFriendRelationship.k_EFriendRelationshipNone;
		}

		public static EPersonaState GetFriendPersonaState(CSteamID steamIDFriend)
		{
			if (steamIDFriend == SteamUser.GetSteamID()) return GetPersonaState();
			return SNetState.Friends.StateOf(steamIDFriend.m_SteamID);
		}

		public static string GetFriendPersonaName(CSteamID steamIDFriend)
		{
			if (steamIDFriend == SteamUser.GetSteamID()) return GetPersonaName();
			return SNetState.Players.Get(steamIDFriend.m_SteamID) ?? "Unknown";
		}

		public static string GetPersonaName() { return SessionState.Name ?? ""; }

		public static EPersonaState GetPersonaState()
		{
			return SessionState.Initialized ? ParsePersonaState(SessionState.PersonaState) : EPersonaState.k_EPersonaStateOffline;
		}

		// Steamworks has no SetPersonaState (the Steam client owns the dropdown); here the
		// account service owns it, so a game or launcher can set it and every friend sees
		// the change through the realtime channel.
		public static bool SetPersonaState(EPersonaState ePersonaState)
		{
			if (!SessionState.Initialized) return false;
			var state = NameOf(ePersonaState);
			SessionState.PersonaState = state;
			SNetState.Friends.Upsert(SteamUser.GetSteamID().m_SteamID, ePersonaState);
			CallbackBus<PersonaStateChange_t>.Fire(new PersonaStateChange_t
			{
				m_ulSteamID = SteamUser.GetSteamID().m_SteamID,
				m_nChangeFlags = (int)EPersonaChange.k_EPersonaChangeStatus,
			});

			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.PostAsync("/User/PersonaState", "{\"state\":" + WebworksJson.Quote(state) + "}"));
				var accepted = WebworksJson.Str(d, "personaState");
				if (accepted != null) SessionState.PersonaState = accepted;
				return "";
			}, "SetPersonaState");
			return true;
		}

		internal static void LoadPersonaState()
		{
			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.GetAsync("/User/PersonaState"));
				var state = WebworksJson.Str(d, "personaState");
				if (state != null) SessionState.PersonaState = state;
				var name = WebworksJson.Str(d, "personaName");
				if (!string.IsNullOrEmpty(name)) SessionState.Name = name;
				SNetState.Friends.Upsert(SteamUser.GetSteamID().m_SteamID, ParsePersonaState(SessionState.PersonaState));
				return "";
			}, "LoadPersonaState");
		}

		internal static string NameOf(EPersonaState state)
		{
			switch (state)
			{
				case EPersonaState.k_EPersonaStateAway: return "Away";
				case EPersonaState.k_EPersonaStateBusy: return "Busy";
				case EPersonaState.k_EPersonaStateSnooze: return "Snooze";
				case EPersonaState.k_EPersonaStateLookingToTrade: return "LookingToTrade";
				case EPersonaState.k_EPersonaStateLookingToPlay: return "LookingToPlay";
				case EPersonaState.k_EPersonaStateInvisible: return "Invisible";
				case EPersonaState.k_EPersonaStateOffline: return "Offline";
				default: return "Online";
			}
		}

		public static bool RequestUserInformation(CSteamID steamIDUser, bool bRequireNameOnly) { return false; }

		public static void RefreshFriendsList()
		{
			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.GetAsync("/Friends/List"));
				var arr = WebworksJson.Arr(d, "friends") ?? new List<object>();

				var ids = new List<ulong>();
				foreach (var f in arr)
				{
					var fd = f as Dictionary<string, object>;
					if (fd == null) continue;
					ulong id = WebworksJson.Ulong(fd, "steamId");
					if (id == 0) continue;
					ids.Add(id);
					SNetState.Players.Set(id, WebworksJson.Str(fd, "name"));
					SNetState.Friends.Upsert(id, ParsePersonaState(WebworksJson.Str(fd, "personaState")));
					var game = WebworksJson.Str(fd, "game");
					uint friendApp = 0;
					var inGame = game != null && uint.TryParse(game, out friendApp);
					SetFriendGame(id, inGame ? friendApp : 0u, inGame ? WebworksJson.Ulong(fd, "lobbyId") : 0, 0, 0, 0);
				}
				SNetState.Friends.ReplaceAll(ids);
				return (string)null;
			}, "RefreshFriendsList");
		}

		static EPersonaState ParsePersonaState(string s)
		{
			if (string.IsNullOrEmpty(s)) return EPersonaState.k_EPersonaStateOffline;
			switch (s.ToLowerInvariant())
			{
				case "online": return EPersonaState.k_EPersonaStateOnline;
				case "away": return EPersonaState.k_EPersonaStateAway;
				case "busy": return EPersonaState.k_EPersonaStateBusy;
				case "snooze": return EPersonaState.k_EPersonaStateSnooze;
				case "lookingtotrade": return EPersonaState.k_EPersonaStateLookingToTrade;
				case "lookingtoplay": return EPersonaState.k_EPersonaStateLookingToPlay;
				case "invisible": return EPersonaState.k_EPersonaStateInvisible;
				default: return EPersonaState.k_EPersonaStateOffline;
			}
		}

		public static bool SendFriendMessage(CSteamID steamIDFriend, string message)
		{
			ApiClient.Fire(() => ApiClient.PostAsync("/Friends/Message", "{\"to\":" + WebworksJson.Quote(steamIDFriend.m_SteamID.ToString()) + ",\"message\":" + WebworksJson.Quote(message) + "}"), "SendFriendMessage");
			return true;
		}

		public static bool InviteUserToGame(CSteamID steamIDFriend, string pchConnectString)
		{
			ApiClient.Fire(() => ApiClient.PostAsync("/Friends/GameInvite", "{\"to\":" + WebworksJson.Quote(steamIDFriend.m_SteamID.ToString()) + ",\"connect\":" + WebworksJson.Quote(pchConnectString ?? "") + "}"), "InviteUserToGame");
			return true;
		}

		internal static void DispatchGameInvite(string json)
		{
			var d = WebworksJson.Obj(json);
			if (d == null) return;
			ulong from = WebworksJson.Ulong(d, "from");
			if (from == 0) return;
			CallbackBus<GameRichPresenceJoinRequested_t>.Fire(new GameRichPresenceJoinRequested_t { m_steamIDFriend = from, m_rgchConnect = WebworksJson.Str(d, "connect") ?? "" });
		}

		internal static void DispatchFriendMessage(string json)
		{
			var d = WebworksJson.Obj(json);
			if (d == null) return;
			ulong from = WebworksJson.Ulong(d, "from");
			if (from == 0) return;
			SNetState.Players.Set(from, WebworksJson.Str(d, "fromName"));
			QueueFriendMessage(from, WebworksJson.Str(d, "message"));
			CallbackBus<FriendChatMessage_t>.Fire(new FriendChatMessage_t { m_ulSteamIDFriend = from, m_szMessage = WebworksJson.Str(d, "message") ?? "" });
		}

		internal static void DispatchPersonaStateChange(string json)
		{
			var d = WebworksJson.Obj(json);
			if (d == null) return;
			ulong id = WebworksJson.Ulong(d, "steamId");
			if (id == 0) return;

			SNetState.Players.Set(id, WebworksJson.Str(d, "name"));
			var incoming = ParsePersonaState(WebworksJson.Str(d, "personaState"));
			SNetState.Friends.Upsert(id, incoming);
			if (id == SteamUser.GetSteamID().m_SteamID) SessionState.PersonaState = NameOf(incoming);
			var playing = WebworksJson.Str(d, "game");
			uint playingApp;
			SetFriendGame(id, playing != null && uint.TryParse(playing, out playingApp) ? playingApp : 0u, 0, 0, 0, 0);
			CallbackBus<PersonaStateChange_t>.Fire(new PersonaStateChange_t { m_ulSteamID = id, m_nChangeFlags = (int)(EPersonaChange.k_EPersonaChangeStatus | EPersonaChange.k_EPersonaChangeComeOnline) });
		}

		internal static void DispatchLobbyInvite(string json)
		{
			var d = WebworksJson.Obj(json);
			if (d == null) return;
			ulong lobbyId = WebworksJson.Ulong(d, "lobbyId");
			ulong from = WebworksJson.Ulong(d, "fromSteamId");
			if (lobbyId == 0) return;

			SNetState.Players.Set(from, WebworksJson.Str(d, "fromName"));
			CallbackBus<GameLobbyJoinRequested_t>.Fire(new GameLobbyJoinRequested_t { m_steamIDLobby = lobbyId, m_steamIDFriend = from });
			CallbackBus<LobbyInvite_t>.Fire(new LobbyInvite_t { m_ulSteamIDUser = from, m_ulSteamIDLobby = lobbyId, m_ulGameID = SessionState.AppId != null ? ulong.Parse(SessionState.AppId) : 0UL });
		}

		public static bool SetRichPresence(string pchKey, string pchValue)
		{
			if (pchValue == null) _myRichPresence.Remove(pchKey);
			else _myRichPresence[pchKey] = pchValue;
			ApiClient.Fire(() => pchValue == null
				? ApiClient.DeleteAsync("/Friends/RichPresence")
				: ApiClient.PostAsync("/Friends/RichPresence", "{\"key\":" + WebworksJson.Quote(pchKey) + ",\"value\":" + WebworksJson.Quote(pchValue) + "}"),
				"SetRichPresence");
			return true;
		}

		public static void ClearRichPresence()
		{
			_myRichPresence.Clear();
			ApiClient.Fire(() => ApiClient.DeleteAsync("/Friends/RichPresence"), "ClearRichPresence");
		}

		public static string GetFriendRichPresence(CSteamID steamIDFriend, string pchKey)
		{
			if (steamIDFriend != SteamUser.GetSteamID()) return "";
			string v;
			return _myRichPresence.TryGetValue(pchKey, out v) ? v : "";
		}

		public static int GetSmallFriendAvatar(CSteamID steamIDFriend) { return AvatarCache.RequestHandle(steamIDFriend.m_SteamID, "small"); }
		public static int GetMediumFriendAvatar(CSteamID steamIDFriend) { return AvatarCache.RequestHandle(steamIDFriend.m_SteamID, "medium"); }
		public static int GetLargeFriendAvatar(CSteamID steamIDFriend) { return AvatarCache.RequestHandle(steamIDFriend.m_SteamID, "large"); }

		// The Steam overlay is a native window; this shim ships its own in-game panel instead
		// (Webworks/Overlay, also available as prefabs). These calls are what games actually
		// use, so they open the matching page there rather than only logging.
		public static void ActivateGameOverlay(string pchDialog)
		{
			switch ((pchDialog ?? "").Trim().ToLowerInvariant())
			{
				case "achievements":
					Webworks.Overlay.WebworksOverlay.ShowAchievements();
					break;
				case "settings":
					Webworks.Overlay.WebworksOverlay.Open(Webworks.Overlay.WebworksOverlayScreen.Settings);
					break;
				case "invites":
				case "invite":
					Webworks.Overlay.WebworksOverlay.ShowInviteDialog(0);
					break;
				case "controller":
					Webworks.Overlay.WebworksOverlay.ShowController();
					break;
				default:
					Webworks.Overlay.WebworksOverlay.ShowFriends();
					break;
			}
		}

		public static void ActivateGameOverlayToUser(string pchDialog, CSteamID steamID)
		{
			Webworks.Overlay.WebworksOverlay.ShowUser(pchDialog, steamID.m_SteamID);
		}

		public static void ActivateGameOverlayToWebPage(string pchURL)
		{
			Webworks.Overlay.WebworksOverlay.ShowLink(pchURL);
		}

		public static void ActivateGameOverlayInviteDialog(CSteamID steamIDLobby)
		{
			Webworks.Overlay.WebworksOverlay.ShowInviteDialog(steamIDLobby.m_SteamID);
		}
	}

	internal static class AvatarCache
	{
		class Entry { public uint Width, Height; public byte[] Rgba; }
		static readonly Dictionary<int, Entry> _byHandle = new Dictionary<int, Entry>();
		static readonly Dictionary<string, int> _byKey = new Dictionary<string, int>();
		static int _next = 1;

		public static int RequestHandle(ulong steamId, string size)
		{
			var key = steamId + ":" + size;
			int handle;
			if (_byKey.TryGetValue(key, out handle)) return handle;

			handle = _next++;
			_byKey[key] = handle;
			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.GetAsync("/Friends/Avatar?steamid=" + steamId + "&size=" + size));
				var b64 = WebworksJson.Str(d, "data");
				if (string.IsNullOrEmpty(b64)) return (string)null;
				_byHandle[handle] = new Entry { Width = (uint)WebworksJson.Int(d, "width"), Height = (uint)WebworksJson.Int(d, "height"), Rgba = Convert.FromBase64String(b64) };
				return (string)null;
			}, "FetchAvatar");
			return handle;
		}

		public static bool TryGet(int handle, out uint width, out uint height, out byte[] rgba)
		{
			Entry e;
			if (_byHandle.TryGetValue(handle, out e)) { width = e.Width; height = e.Height; rgba = e.Rgba; return true; }
			width = 0; height = 0; rgba = null;
			return false;
		}
	}
}
