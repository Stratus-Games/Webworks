using System;
using System.Collections.Generic;
using UnityEngine;
using Webworks.SNetInternal;

namespace Webworks.Overlay
{
	public sealed partial class WebworksOverlay
	{
		sealed class FriendEntry
		{
			public string Username;
			public string DisplayName;
			public string PersonaState;
			public string Game;
			public ulong SteamId;

			public bool Online
			{
				get { return PersonaState == "Online" || PersonaState == "LookingToPlay" || PersonaState == "LookingToTrade"; }
			}

			public string Detail
			{
				get
				{
					if (!string.IsNullOrEmpty(Game)) return "in game " + Game;
					if (string.IsNullOrEmpty(PersonaState)) return "offline";
					return PersonaState == "Offline" ? "offline" : PersonaState;
				}
			}
		}

		sealed class AchievementEntry
		{
			public string Key;
			public string Name;
			public string Description;
			public bool Unlocked;
			public float Percent;
		}

		List<FriendEntry> _friends = new List<FriendEntry>();
		List<string> _incoming = new List<string>();
		List<string> _outgoing = new List<string>();
		List<AchievementEntry> _achievements = new List<AchievementEntry>();

		bool _friendsLoading;
		bool _friendsLoaded;
		string _friendsError;
		string _addName = "";
		string _addStatus;
		string _messageText = "";
		string _messageStatus;
		string _userTarget;
		ulong _userSteamId;
		string _inviteLobby;
		string _infoTitle, _infoBody;
		string _linkUrl;
		bool _achievementsLoaded;

		// --- static entry points used by the Steamworks shim -------------------

		public static void ShowFriends()
		{
			Open(WebworksOverlayScreen.Friends);
		}

		public static void ShowUser(string dialog, ulong steamId)
		{
			var overlay = Ensure();
			overlay._userSteamId = steamId;
			overlay._userTarget = null;
			overlay._messageText = "";
			overlay._messageStatus = null;
			overlay.Show(WebworksOverlayScreen.User, dialog);
		}

		public static void ShowInviteDialog(ulong lobbyId)
		{
			var overlay = Ensure();
			overlay._inviteLobby = lobbyId == 0 ? null : lobbyId.ToString();
			overlay.Show(WebworksOverlayScreen.Invites, overlay._inviteLobby);
		}

		public static void ShowLink(string url)
		{
			Open(WebworksOverlayScreen.Link, url);
		}

		public static void ShowInfo(string title, string body)
		{
			var overlay = Ensure();
			overlay._infoTitle = title;
			overlay._infoBody = body;
			overlay.Show(WebworksOverlayScreen.Info, title);
		}

		public static void ShowAchievements()
		{
			Open(WebworksOverlayScreen.Achievements);
		}

		public static void ShowController()
		{
			Open(WebworksOverlayScreen.Controller);
		}

		// --- lifecycle --------------------------------------------------------

		void OnScreenShown(WebworksOverlayScreen screen, string argument)
		{
			switch (screen)
			{
				case WebworksOverlayScreen.Friends:
				case WebworksOverlayScreen.Requests:
				case WebworksOverlayScreen.Invites:
					LoadFriends(false);
					break;
				case WebworksOverlayScreen.Achievements:
					LoadAchievements();
					break;
				case WebworksOverlayScreen.User:
					// The argument is the dialog that asked for the page ("steamid", "chat",
					// "friendadd", ...), which is only a hint for which panel to emphasise.
					if (!string.IsNullOrEmpty(argument) && argument != "steamid" && argument != "chat") _userTarget = null;
					break;
				case WebworksOverlayScreen.Link:
					_linkUrl = argument;
					break;
			}
		}

		int RowCount()
		{
			switch (_screen)
			{
				case WebworksOverlayScreen.Friends: return _friends.Count;
				case WebworksOverlayScreen.Requests: return _incoming.Count;
				case WebworksOverlayScreen.Invites: return _inviteLobby == null ? 0 : _friends.Count;
				default: return 0;
			}
		}

		void ActivateSelection()
		{
			switch (_screen)
			{
				case WebworksOverlayScreen.Friends:
					if (_selection >= 0 && _selection < _friends.Count) OpenUser(_friends[_selection]);
					break;
				case WebworksOverlayScreen.Requests:
					if (_selection >= 0 && _selection < _incoming.Count) AcceptFriendAsync(_incoming[_selection]);
					break;
				case WebworksOverlayScreen.Invites:
					if (_inviteLobby != null && _selection >= 0 && _selection < _friends.Count) InviteFriend(_friends[_selection]);
					break;
			}
		}

		// --- loading ----------------------------------------------------------

		async void LoadFriends(bool force)
		{
			if (_friendsLoading) return;
			if (_friendsLoaded && !force) return;

			_friendsLoading = true;
			_friendsError = null;
			try
			{
				string json = await ApiClient.GetAsync("/Friends/Graph");

				var parsed = new List<FriendEntry>();
				var incoming = new List<string>();
				var outgoing = new List<string>();

				var root = WebworksJson.Obj(json);
				var accepted = WebworksJson.Arr(root, "accepted") ?? new List<object>();
				foreach (var raw in accepted)
				{
					var entry = raw as Dictionary<string, object>;
					if (entry == null) continue;

					string username = WebworksJson.Str(entry, "username");
					if (string.IsNullOrEmpty(username)) continue;

					var friend = new FriendEntry
					{
						Username = username,
						DisplayName = WebworksJson.Str(entry, "displayName") ?? WebworksJson.Str(entry, "name") ?? username,
						PersonaState = WebworksJson.Str(entry, "personaState") ?? "Offline",
						Game = WebworksJson.Str(entry, "game"),
					};
					ulong steamId = WebworksJson.Ulong(entry, "steamId");
					friend.SteamId = steamId;
					parsed.Add(friend);
				}

				var pendingIn = WebworksJson.Arr(root, "incoming") ?? new List<object>();
				foreach (var raw in pendingIn)
				{
					string name = SocialName(raw);
					if (!string.IsNullOrEmpty(name)) incoming.Add(name);
				}

				var pendingOut = WebworksJson.Arr(root, "outgoing") ?? new List<object>();
				foreach (var raw in pendingOut)
				{
					string name = SocialName(raw);
					if (!string.IsNullOrEmpty(name)) outgoing.Add(name);
				}

				// Swap whole lists so a background continuation never mutates one OnGUI reads.
				_friends = parsed;
				_incoming = incoming;
				_outgoing = outgoing;
			}
			catch (Exception ex)
			{
				_friendsError = ex.Message;
			}
			finally
			{
				_friendsLoading = false;
				_friendsLoaded = true;
			}
		}

		static string SocialName(object raw)
		{
			var text = raw as string;
			if (text != null) return text;
			var map = raw as Dictionary<string, object>;
			return map == null ? null : WebworksJson.Str(map, "username");
		}

		void OpenUser(FriendEntry friend)
		{
			_userTarget = friend.Username;
			_userSteamId = friend.SteamId;
			_messageText = "";
			_messageStatus = null;
			_screen = WebworksOverlayScreen.User;
			_selection = 0;
		}

		async void AcceptFriendAsync(string username)
		{
			if (string.IsNullOrEmpty(username)) return;
			try
			{
				await ApiClient.PostAsync("/Friends/Accept", "{\"username\":" + WebworksJson.Quote(username) + "}");
				LoadFriends(true);
			}
			catch (Exception ex)
			{
				_addStatus = "Could not accept " + username + ": " + ex.Message;
			}
		}

		async void AddFriendAsync(string username)
		{
			if (string.IsNullOrEmpty(username)) return;
			_addStatus = "Sending a request to " + username + "...";
			try
			{
				await ApiClient.PostAsync("/Friends/Add", "{\"username\":" + WebworksJson.Quote(username) + "}");
				_addName = "";
				_addStatus = "Friend request sent to " + username + ".";
				LoadFriends(true);
			}
			catch (Exception ex)
			{
				_addStatus = "Could not add " + username + ": " + ex.Message;
			}
		}

		void InviteFriend(FriendEntry friend)
		{
			if (string.IsNullOrEmpty(_inviteLobby))
			{
				_addStatus = "Open this from a lobby (SteamMatchmaking opens it with the lobby id).";
				return;
			}

			ulong lobbyId;
			if (!ulong.TryParse(_inviteLobby, out lobbyId))
			{
				_addStatus = "That lobby id is not valid.";
				return;
			}

			try
			{
				Steamworks.SteamMatchmaking.InviteUserToLobby(new Steamworks.CSteamID(lobbyId), new Steamworks.CSteamID(friend.SteamId));
				_messageStatus = "Invite sent to " + friend.DisplayName + ".";
			}
			catch (Exception ex)
			{
				_messageStatus = "Could not invite: " + ex.Message;
			}
		}

		// --- achievements -----------------------------------------------------

		void LoadAchievements()
		{
			if (_achievementsLoaded) return;
			_achievementsLoaded = true;

			var rows = new List<AchievementEntry>();
			try
			{
				uint count = Steamworks.SteamUserStats.GetNumAchievements();
				for (uint i = 0; i < count; i++)
				{
					string key = Steamworks.SteamUserStats.GetAchievementKey(i);
					if (string.IsNullOrEmpty(key)) continue;

					bool unlocked;
					Steamworks.SteamUserStats.GetAchievement(key, out unlocked);
					float percent;
					Steamworks.SteamUserStats.GetAchievementAchievedPercent(key, out percent);

					rows.Add(new AchievementEntry
					{
						Key = key,
						Name = Steamworks.SteamUserStats.GetAchievementDisplayAttribute(key, "name"),
						Description = Steamworks.SteamUserStats.GetAchievementDisplayAttribute(key, "desc"),
						Unlocked = unlocked,
						Percent = percent,
					});
				}
			}
			catch (Exception ex)
			{
				_friendsError = ex.Message;
			}
			_achievements = rows;
		}

		// --- pages ------------------------------------------------------------

		void DrawFriends()
		{
			int online = 0;
			foreach (var friend in _friends) if (friend.Online) online++;

			Section("FRIENDS \u00b7 " + _friends.Count + " total \u00b7 " + online + " online");
			if (_friendsLoading) Empty("Loading friends...");
			if (!string.IsNullOrEmpty(_friendsError)) Empty("Could not load friends: " + _friendsError);

			if (_friends.Count == 0 && !_friendsLoading)
				Empty("No friends yet. Add someone by username below, and they will show up here once they accept.");

			_scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
			for (int i = 0; i < _friends.Count; i++)
			{
				var friend = _friends[i];
				GUILayout.BeginHorizontal(GUILayout.Height(24));

				var previous = GUI.color;
				GUI.color = i == _selection ? accentBrightColor : textColor;
				if (GUILayout.Button((friend.Online ? "\u25CF " : "\u25CB ") + friend.DisplayName, _rowStyle, GUILayout.MinWidth(240)))
					OpenUser(friend);
				GUI.color = previous;

				GUILayout.FlexibleSpace();
				var muted = GUI.color;
				GUI.color = mutedColor;
				GUILayout.Label(friend.Detail, _badgeStyle);
				GUI.color = muted;

				if (GUILayout.Button("Message", _buttonStyle, GUILayout.Width(76))) OpenUser(friend);
				if (GUILayout.Button("Invite", _buttonStyle, GUILayout.Width(58))) InviteFriend(friend);
				GUILayout.EndHorizontal();
			}
			GUILayout.EndScrollView();

			GUILayout.Space(6);
			GUILayout.BeginHorizontal();
			_addName = GUILayout.TextField(_addName ?? "", 32, _fieldStyle, GUILayout.Width(240));
			if (GUILayout.Button("Add friend", _buttonStyle, GUILayout.Width(96))) AddFriendAsync((_addName ?? "").Trim());
			if (GUILayout.Button("Refresh", _buttonStyle, GUILayout.Width(80))) LoadFriends(true);
			GUILayout.EndHorizontal();
			if (!string.IsNullOrEmpty(_addStatus)) Empty(_addStatus);
		}

		void DrawRequests()
		{
			Section("FRIEND REQUESTS");
			if (_friendsLoading) Empty("Loading...");

			_scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

			Section("WAITING FOR YOU \u00b7 " + _incoming.Count);
			if (_incoming.Count == 0) Empty("Nothing waiting.");
			for (int i = 0; i < _incoming.Count; i++)
			{
				GUILayout.BeginHorizontal(GUILayout.Height(24));
				var previous = GUI.color;
				GUI.color = i == _selection ? accentBrightColor : textColor;
				GUILayout.Label(_incoming[i], _rowStyle, GUILayout.MinWidth(220));
				GUI.color = previous;
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Accept", _buttonStyle, GUILayout.Width(80))) AcceptFriendAsync(_incoming[i]);
				GUILayout.EndHorizontal();
			}

			GUILayout.Space(8);
			Section("YOU SENT \u00b7 " + _outgoing.Count);
			if (_outgoing.Count == 0) Empty("No requests sent.");
			for (int i = 0; i < _outgoing.Count; i++)
				Row(_outgoing[i], "waiting", -1);

			GUILayout.EndScrollView();
			if (!string.IsNullOrEmpty(_addStatus)) Empty(_addStatus);
		}

		void DrawAchievements()
		{
			int unlocked = 0;
			foreach (var achievement in _achievements) if (achievement.Unlocked) unlocked++;

			Section("ACHIEVEMENTS \u00b7 " + unlocked + " of " + _achievements.Count);
			if (_achievements.Count == 0)
			{
				Empty("This game has not registered any achievements with the shim yet.");
				return;
			}

			_scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
			foreach (var achievement in _achievements)
			{
				GUILayout.BeginHorizontal(GUILayout.Height(24));
				var previous = GUI.color;
				GUI.color = achievement.Unlocked ? onlineColor : mutedColor;
				GUILayout.Label(achievement.Unlocked ? "\u2714" : "\u25CB", _rowStyle, GUILayout.Width(18));
				GUI.color = achievement.Unlocked ? textColor : mutedColor;
				GUILayout.Label(string.IsNullOrEmpty(achievement.Name) ? achievement.Key : achievement.Name, _rowStyle, GUILayout.MinWidth(240));
				GUI.color = previous;
				GUILayout.FlexibleSpace();
				if (achievement.Percent > 0f) GUILayout.Label(achievement.Percent.ToString("0.0") + "% of players", _badgeStyle);
				GUILayout.EndHorizontal();
				if (!string.IsNullOrEmpty(achievement.Description))
				{
					var muted = GUI.color;
					GUI.color = mutedColor;
					GUILayout.Label(achievement.Description, _smallStyle);
					GUI.color = muted;
				}
			}
			GUILayout.EndScrollView();
		}

		void DrawInvites()
		{
			Section("INVITE FRIENDS");
			if (string.IsNullOrEmpty(_inviteLobby))
			{
				Empty("This page opens with a lobby id. Games call SteamFriends.ActivateGameOverlayInviteDialog(lobby) and this panel lists the friends who can be invited.");
				return;
			}

			GUILayout.Label("Lobby " + _inviteLobby, _labelStyle);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Copy lobby id", _buttonStyle, GUILayout.Width(120)))
			{
				GUIUtility.systemCopyBuffer = _inviteLobby;
				_addStatus = "Lobby id copied: " + _inviteLobby;
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(6);
			if (_friendsLoading) Empty("Loading friends...");
			if (_friends.Count == 0 && !_friendsLoading) Empty("No friends to invite yet.");

			_scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
			for (int i = 0; i < _friends.Count; i++)
			{
				var friend = _friends[i];
				GUILayout.BeginHorizontal(GUILayout.Height(24));
				var previous = GUI.color;
				GUI.color = i == _selection ? accentBrightColor : textColor;
				GUILayout.Label((friend.Online ? "\u25CF " : "\u25CB ") + friend.DisplayName, _rowStyle, GUILayout.MinWidth(240));
				GUI.color = previous;
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Invite", _buttonStyle, GUILayout.Width(70))) InviteFriend(friend);
				GUILayout.EndHorizontal();
			}
			GUILayout.EndScrollView();

			if (!string.IsNullOrEmpty(_addStatus)) Empty(_addStatus);
			if (!string.IsNullOrEmpty(_messageStatus)) Empty(_messageStatus);
		}

		void DrawUser()
		{
			string name = _userTarget;
			if (string.IsNullOrEmpty(name))
			{
				try { name = Steamworks.SteamFriends.GetFriendPersonaName(new Steamworks.CSteamID(_userSteamId)); }
				catch (Exception) { name = null; }
			}
			if (string.IsNullOrEmpty(name)) name = "Friend";

			Section(name.ToUpperInvariant());
			GUILayout.Label("Send a message through the server this game is connected to.", _mutedStyle);
			GUILayout.BeginHorizontal();
			_messageText = GUILayout.TextField(_messageText ?? "", 240, _fieldStyle, GUILayout.Width(320));
			if (GUILayout.Button("Send", _buttonStyle, GUILayout.Width(64)))
			{
				try
				{
					bool sent = Steamworks.SteamFriends.SendFriendMessage(new Steamworks.CSteamID(_userSteamId), _messageText);
					_messageStatus = sent ? "Message sent." : "Could not send the message.";
					if (sent) _messageText = "";
				}
				catch (Exception ex)
				{
					_messageStatus = "Could not send: " + ex.Message;
				}
			}
			GUILayout.EndHorizontal();

			if (!string.IsNullOrEmpty(_messageStatus)) Empty(_messageStatus);

			GUILayout.Space(10);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Invite to my game", _buttonStyle, GUILayout.Width(150)))
			{
				try
				{
					Steamworks.SteamFriends.InviteUserToGame(new Steamworks.CSteamID(_userSteamId), "");
					_messageStatus = "Game invite sent to " + name + ".";
				}
				catch (Exception ex)
				{
					_messageStatus = "Could not invite: " + ex.Message;
				}
			}
			if (GUILayout.Button("Back", _buttonStyle, GUILayout.Width(80)))
			{
				_screen = ScreenFor(_tab);
				_selection = 0;
			}
			GUILayout.EndHorizontal();
		}
	}
}
