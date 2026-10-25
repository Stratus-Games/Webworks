using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Steamworks.Data;
using Webworks.Internal;

namespace Steamworks
{
	internal class WebworksOverlay : MonoBehaviour
	{
		static WebworksOverlay _instance;
		static WebworksOverlay I
		{
			get
			{
				if (_instance != null) return _instance;
				var go = new GameObject("WebworksOverlay (Webworks custom overlay)");
				UnityEngine.Object.DontDestroyOnLoad(go);
				_instance = go.AddComponent<WebworksOverlay>();
				return _instance;
			}
		}

		enum Panel { None, Friends, User, Invite, WebPage, Info, ControllerBinding }
		Panel _panel = Panel.None;

		string _tab = "friends";
		SteamId _userTarget;
		SteamId _inviteLobby;
		string _webUrl;
		string _infoTitle, _infoBody;
		string _status;
		Vector2 _scroll;
		string _chatBox = "";
		string _awaitingRebindDigital;


		internal static void Show(string dialog)
		{
			I._tab = string.IsNullOrEmpty(dialog) ? "friends" : dialog;
			I.Open(Panel.Friends);
		}

		internal static void ShowUser(string dialog, SteamId steamId)
		{
			I._tab = string.IsNullOrEmpty(dialog) ? "steamid" : dialog;
			I._userTarget = steamId;
			I.Open(Panel.User);
		}

		internal static void ShowInviteDialog(SteamId lobbyId)
		{
			I._inviteLobby = lobbyId;
			I._status = null;
			I.Open(Panel.Invite);
		}

		internal static void ShowWebPage(string url)
		{
			I._webUrl = url;
			I.Open(Panel.WebPage);
		}

		internal static void ShowInfo(string title, string body)
		{
			I._infoTitle = title;
			I._infoBody = body;
			I.Open(Panel.Info);
		}

		internal static void ShowControllerBinding()
		{
			I.Open(Panel.ControllerBinding);
		}

		internal static void Hide()
		{
			if (_instance != null) _instance.Close();
		}

		void Open(Panel p)
		{
			bool wasClosed = _panel == Panel.None;
			_panel = p;
			if (wasClosed) SteamFriends.RaiseGameOverlayActivated(true);
		}

		void Close()
		{
			if (_panel == Panel.None) return;
			_panel = Panel.None;
			SteamFriends.RaiseGameOverlayActivated(false);
		}

		void Update()
		{
			if (_panel != Panel.None && Input.GetKeyDown(KeyCode.Escape)) Close();
		}


		static GUIStyle _boxStyle, _headerStyle, _rowStyle;
		void EnsureStyles()
		{
			if (_boxStyle != null) return;
			_boxStyle = new GUIStyle(GUI.skin.box);
			_headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 16 };
			_rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
		}

		void OnGUI()
		{
			if (_panel == Panel.None) return;
			EnsureStyles();

			if (_awaitingRebindDigital != null && Event.current.type == EventType.KeyDown && Event.current.keyCode != KeyCode.None)
			{
				SteamInput.SetDigitalBinding(_awaitingRebindDigital, Event.current.keyCode);
				_awaitingRebindDigital = null;
				Event.current.Use();
			}

			float w = Mathf.Min(420, Screen.width - 40);
			float h = Mathf.Min(480, Screen.height - 40);
			var rect = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);

			var prevColor = GUI.color;
			GUI.color = new UnityEngine.Color(0, 0, 0, 0.55f);
			GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
			GUI.color = prevColor;

			GUILayout.BeginArea(rect, _boxStyle);
			GUILayout.BeginHorizontal();
			GUILayout.Label("Webworks Overlay", _headerStyle);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Close", GUILayout.Width(60))) Close();
			GUILayout.EndHorizontal();
			GUILayout.Space(6);

			switch (_panel)
			{
				case Panel.Friends: DrawFriends(); break;
				case Panel.User: DrawUser(); break;
				case Panel.Invite: DrawInvite(); break;
				case Panel.WebPage: DrawWebPage(); break;
				case Panel.Info: DrawInfo(); break;
				case Panel.ControllerBinding: DrawControllerBinding(); break;
			}

			GUILayout.EndArea();
		}

		void DrawFriends()
		{
			GUILayout.Label("Signed in as " + SteamClient.Name + "  (guest id " + SteamClient.SteamId + ")", _rowStyle);
			GUILayout.Space(4);

			var friends = SteamFriends.GetFriends().ToList();
			GUILayout.Label(friends.Count + " friend(s)", _rowStyle);
			_scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
			foreach (var f in friends)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label((f.IsOnline ? "\u25CF " : "\u25CB ") + f.Name, _rowStyle, GUILayout.Width(220));
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("View", GUILayout.Width(60)))
				{
					_userTarget = f.Id;
					_panel = Panel.User;
				}
				GUILayout.EndHorizontal();
			}
			if (friends.Count == 0) GUILayout.Label("(nobody yet -- friends appear here once added on the Webworks server)", _rowStyle);
			GUILayout.EndScrollView();
		}

		void DrawUser()
		{
			var f = new Friend(_userTarget);
			GUILayout.Label(f.Name + (f.IsOnline ? "  (online)" : "  (offline)"), _headerStyle);
			GUILayout.Space(8);

			GUILayout.Label("Send a message:", _rowStyle);
			_chatBox = GUILayout.TextField(_chatBox);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Send") && !string.IsNullOrEmpty(_chatBox))
			{
				bool ok = f.SendMessage(_chatBox);
				_status = ok ? "Sent." : "Could not send (not connected?).";
				_chatBox = "";
			}
			GUILayout.EndHorizontal();

			GUILayout.FlexibleSpace();
			if (!string.IsNullOrEmpty(_status)) GUILayout.Label(_status, _rowStyle);
			if (GUILayout.Button("Back")) _panel = Panel.Friends;
		}

		void DrawInvite()
		{
			GUILayout.Label("Invite friends to this lobby", _headerStyle);
			GUILayout.Space(6);

			if (GUILayout.Button("Create shareable invite link"))
			{
				var lobby = new Lobby { Id = _inviteLobby };
				_status = "Creating link...";
				CreateInviteAsyncFireAndForget(lobby);
			}
			GUILayout.Space(10);

			GUILayout.Label("Or invite a friend directly:", _rowStyle);
			_scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
			foreach (var f in SteamFriends.GetFriends())
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(f.Name, _rowStyle, GUILayout.Width(220));
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Invite", GUILayout.Width(70)))
				{
					new Lobby { Id = _inviteLobby }.InviteFriend(f.Id);
					_status = "Invite sent to " + f.Name + ".";
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndScrollView();

			if (!string.IsNullOrEmpty(_status)) GUILayout.Label(_status, _rowStyle);
		}

		async void CreateInviteAsyncFireAndForget(Lobby lobby)
		{
			try
			{
				var invite = await lobby.CreateInviteAsync();
				if (invite == null) { _status = "Could not create an invite link."; return; }
				string text = invite.Value.Url ?? invite.Value.Token;
				GUIUtility.systemCopyBuffer = text;
				_status = "Copied to clipboard: " + text;
			}
			catch (Exception ex)
			{
				_status = "Could not create an invite link: " + ex.Message;
			}
		}

		void DrawWebPage()
		{
			GUILayout.Label("Open link", _headerStyle);
			GUILayout.Space(6);
			GUILayout.Label(_webUrl, _rowStyle);
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Open in browser"))
			{
				Application.OpenURL(_webUrl);
				Close();
			}
			if (GUILayout.Button("Cancel")) Close();
			GUILayout.EndHorizontal();
		}

		void DrawInfo()
		{
			GUILayout.Label(_infoTitle, _headerStyle);
			GUILayout.Space(6);
			GUILayout.Label(_infoBody, _rowStyle);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("OK")) Close();
		}

		void DrawControllerBinding()
		{
			GUILayout.Label("Controller Bindings", _headerStyle);
			GUILayout.Space(6);

			if (_awaitingRebindDigital != null)
			{
				GUILayout.Label("Press any key to bind \"" + _awaitingRebindDigital + "\" -- Esc cancels.", _rowStyle);
				if (Input.GetKeyDown(KeyCode.Escape)) _awaitingRebindDigital = null;
			}

			_scroll = GUILayout.BeginScrollView(_scroll);

			if (SteamInput.KnownDigitalActions.Count == 0 && SteamInput.KnownAnalogActions.Count == 0)
			{
				GUILayout.Label("No actions queried yet -- open this once the game has started reading input.", _rowStyle);
			}

			foreach (var action in SteamInput.KnownDigitalActions)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(action, _rowStyle, GUILayout.Width(160));
				GUILayout.Label(SteamInput.GetDigitalBinding(action).ToString(), _rowStyle, GUILayout.Width(100));
				if (GUILayout.Button("Rebind", GUILayout.Width(70))) _awaitingRebindDigital = action;
				GUILayout.EndHorizontal();
			}

			foreach (var action in SteamInput.KnownAnalogActions)
			{
				string ax, ay;
				SteamInput.GetAnalogBinding(action, out ax, out ay);
				GUILayout.BeginHorizontal();
				GUILayout.Label(action, _rowStyle, GUILayout.Width(160));
				GUILayout.Label((ax ?? "") + " / " + (ay ?? ""), _rowStyle, GUILayout.Width(160));
				GUILayout.EndHorizontal();
			}

			GUILayout.EndScrollView();
			GUILayout.FlexibleSpace();
			GUILayout.Label("Analog actions use Unity's Input axes (Edit > Project Settings > Input) -- rebind those there.", _rowStyle);
		}
	}
}
