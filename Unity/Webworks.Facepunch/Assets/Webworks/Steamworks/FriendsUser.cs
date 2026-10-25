#pragma warning disable CS0067
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks.Data;
using Webworks.Internal;

namespace Steamworks
{
	public static partial class SteamFriends
	{
		public static event Action<Friend> OnPersonaStateChange;
		public static event Action<Friend> OnFriendRichPresenceUpdate;
		public static event Action<Friend, string> OnGameRichPresenceJoinRequested;
		public static event Action<Lobby, SteamId> OnGameLobbyJoinRequested;
		public static event Action<bool> OnGameOverlayActivated;
		public static event Action<string, string> OnGameServerChangeRequested;

		public static IEnumerable<Friend> GetFriends()
		{
			var list = new List<Friend>();
			foreach (var id in FriendDirectory.AllIds) list.Add(new Friend(id));
			return list;
		}
		public static IEnumerable<Friend> GetBlocked() { return new Friend[0]; }
		public static IEnumerable<Friend> GetPlayedWith() { return new Friend[0]; }

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
					PlayerDirectory.Set(id, WebworksJson.Str(fd, "name"));
					FriendDirectory.Upsert(id, ParsePersonaState(WebworksJson.Str(fd, "personaState")), WebworksJson.Str(fd, "avatarBase64"));
				}
				FriendDirectory.ReplaceAll(ids);
				return (string)null;
			}, "RefreshFriendsList");
		}

		static FriendState ParsePersonaState(string s)
		{
			return string.Equals(s, "Online", StringComparison.OrdinalIgnoreCase) ? FriendState.Online : FriendState.Offline;
		}

		internal static void DispatchPersonaStateChange(string json)
		{
			var d = WebworksJson.Obj(json);
			if (d == null) return;
			ulong id = WebworksJson.Ulong(d, "steamId");
			if (id == 0) return;

			PlayerDirectory.Set(id, WebworksJson.Str(d, "name"));
			FriendDirectory.Upsert(id, ParsePersonaState(WebworksJson.Str(d, "personaState")));
			Callbacks.RaiseNow(OnPersonaStateChange, new Friend(id));
		}

		internal static void DispatchLobbyInvite(string json)
		{
			var d = WebworksJson.Obj(json);
			if (d == null) return;
			ulong lobbyId = WebworksJson.Ulong(d, "lobbyId");
			ulong from = WebworksJson.Ulong(d, "fromSteamId");
			if (lobbyId == 0) return;

			PlayerDirectory.Set(from, WebworksJson.Str(d, "fromName"));
			RaiseGameLobbyJoinRequested(new Lobby { Id = lobbyId }, (SteamId)from);
		}

		internal static void RaiseGameLobbyJoinRequested(Lobby lobby, SteamId from)
		{
			Callbacks.RaiseNow(OnGameLobbyJoinRequested, lobby, from);
		}

		internal static void RaiseGameOverlayActivated(bool active)
		{
			Callbacks.RaiseNow(OnGameOverlayActivated, active);
		}

		public static bool SetRichPresence(string key, string value)
		{
			RememberRichPresence(key, value);
			ApiClient.Fire(() => ApiClient.PostAsync("/Friends/RichPresence", "{\"key\":" + WebworksJson.Quote(key) + ",\"value\":" + WebworksJson.Quote(value) + "}"), "SetRichPresence");
			return true;
		}

		public static void ClearRichPresence()
		{
			ForgetRichPresence();
			ApiClient.Fire(() => ApiClient.DeleteAsync("/Friends/RichPresence"), "ClearRichPresence");
		}

		public static Task<Image?> GetSmallAvatarAsync(SteamId steamid) { return FetchAvatar(steamid, "small"); }
		public static Task<Image?> GetMediumAvatarAsync(SteamId steamid) { return FetchAvatar(steamid, "medium"); }
		public static Task<Image?> GetLargeAvatarAsync(SteamId steamid) { return FetchAvatar(steamid, "large"); }

		static async Task<Image?> FetchAvatar(SteamId steamid, string size)
		{
			var d = WebworksJson.Obj(await ApiClient.GetAsync("/Friends/Avatar?steamid=" + steamid + "&size=" + size));
			var b64 = WebworksJson.Str(d, "data");
			if (string.IsNullOrEmpty(b64)) return null;
			return new Image { Width = (uint)WebworksJson.Int(d, "width"), Height = (uint)WebworksJson.Int(d, "height"), Data = Convert.FromBase64String(b64) };
		}

		public static void OpenOverlay(string type) { WebworksOverlay.Show(type); }
		public static void OpenUserOverlay(SteamId id, string type) { WebworksOverlay.ShowUser(type, id); }
		public static void OpenStoreOverlay(AppId id)
		{
			WebworksOverlay.ShowInfo("Store", "There's no store front-end in a Webworks guest session (app " + id.Value + ").");
		}

		public static void OpenGameInviteOverlay(SteamId lobby) { WebworksOverlay.ShowInviteDialog(lobby); }
		public static void OpenGameInviteOverlay(Lobby lobby) { WebworksOverlay.ShowInviteDialog(lobby.Id); }

		public static void OpenWebOverlay(string url, bool modal = false) { WebworksOverlay.ShowWebPage(url); }
	}

	public static partial class SteamUser
	{
		public static SteamId SteamId { get { return SteamClient.SteamId; } }
		public static SteamId Id { get { return SteamClient.SteamId; } }

		static byte[] _ticket = new byte[0];
		static DateTime _ticketExpiry = DateTime.MinValue;

		internal static void PrefetchTicket()
		{
			ApiClient.Fire(async () => { await GetAuthSessionTicketAsync(); return (string)null; }, "PrefetchTicket");
		}

		public static async Task<AuthTicket> GetAuthSessionTicketAsync()
		{
			var t = WebworksJson.Str(WebworksJson.Obj(await ApiClient.PostAsync("/User/AuthTicket", "{}")), "ticket");
			_ticket = string.IsNullOrEmpty(t) ? new byte[0] : Convert.FromBase64String(t);
			_ticketExpiry = DateTime.UtcNow.AddMinutes(9);
			return new AuthTicket { Data = _ticket };
		}

		public static AuthTicket GetAuthSessionTicket()
		{
			if (DateTime.UtcNow >= _ticketExpiry) PrefetchTicket();
			return new AuthTicket { Data = _ticket };
		}

		public static AuthTicket GetAuthSessionTicket(NetIdentity identity) { return GetAuthSessionTicket(); }
		public static Task<AuthTicket> GetAuthSessionTicketAsync(NetIdentity identity, double timeoutSeconds = 10.0) { return GetAuthSessionTicketAsync(); }

		public static bool BeginAuthSession(byte[] ticketData, SteamId steamid)
		{
			var b64 = Convert.ToBase64String(ticketData ?? new byte[0]);
			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.PostAsync("/User/BeginAuthSession", "{\"ticket\":" + WebworksJson.Quote(b64) + "}"));
				bool valid = WebworksJson.Bool(d, "valid");
				if (!valid) Debug.LogWarning("[Webworks] auth ticket from " + steamid + " failed validation");
				RaiseValidateAuthTicketResponse(steamid, valid ? AuthResponse.OK : AuthResponse.AuthTicketInvalid);
				return (string)null;
			}, "BeginAuthSession");
			return true;
		}

		public static void EndAuthSession(SteamId steamid)
		{
			ApiClient.Fire(() => ApiClient.PostAsync("/User/EndAuthSession", "{\"steamId\":\"" + steamid + "\"}"), "EndAuthSession");
		}
	}
}
