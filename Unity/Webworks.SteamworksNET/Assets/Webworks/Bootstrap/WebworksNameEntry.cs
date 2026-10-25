using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Webworks.SNetInternal;
#if TMP_PRESENT
using TMPro;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Webworks.SteamworksNET
{
	[DisallowMultipleComponent]
	public class WebworksNameEntry : MonoBehaviour
	{
		[Header("Scene flow")]
		[Tooltip("Scene loaded after a sign-in, registration or guest entry completes.")]
#if UNITY_EDITOR
		[SerializeField] SceneAsset sceneToLoad;
#endif
		[SerializeField, HideInInspector] string _sceneNameToLoad;

#if UNITY_EDITOR
		void OnValidate()
		{
			_sceneNameToLoad = sceneToLoad != null ? sceneToLoad.name : null;
		}
#endif

		[Header("Player name")]
		[SerializeField] string playerPrefsKey = "webworks_player_name";
		[SerializeField] string guestPlaceholder = "Display name";
		[SerializeField] int maxNameLength = 20;

		[Header("Accounts")]
		[SerializeField] bool showAccounts = true;
		[SerializeField] bool rememberSignIn = true;
		[SerializeField] string accountTokenPrefsKey = "webworks_account_token";
		[SerializeField] string accountUsernamePrefsKey = "webworks_account_username";
		[SerializeField] string accountDisplayNamePrefsKey = "webworks_account_display_name";
		[SerializeField] int avatarSize = 184;

		[Header("Friends step")]
		[Tooltip("After signing in with an account (not a guest), show the friends page before loading the game.")]
		[SerializeField] bool showFriendsStep = true;
		[SerializeField] string friendsTitle = "YOUR FRIENDS";
		[SerializeField] string friendsContinueLabel = "Continue to game";
		[SerializeField] string addFriendPlaceholder = "Add a friend by username";
		[SerializeField] float friendsPanelHeight = 660f;

		[Header("Live friends")]
		[Tooltip("How often the friends page refreshes while it is on screen. Requests and presence show up without pressing anything.")]
		[SerializeField] float friendsRefreshSeconds = 4f;
		[Tooltip("How often this client tells the server it is still here. Keeps a player Online while they sit on this page.")]
		[SerializeField] float presenceHeartbeatSeconds = 20f;
		[Tooltip("Prefs key the game scene reads to join the room this page opened or joined.")]
		[SerializeField] string joinLobbyPrefsKey = "webworks_join_lobby";

		[Header("Look")]
		[SerializeField] Color backdropColor = new Color(0.02f, 0.01f, 0.015f, 0.96f);
		[SerializeField] Color panelColor = new Color(0.07f, 0.055f, 0.06f, 0.98f);
		[SerializeField] Color accentColor = new Color(0.80f, 0.08f, 0.14f, 1f);
		[SerializeField] Color accentColorBright = new Color(0.92f, 0.16f, 0.20f, 1f);
		[SerializeField] Color fieldColor = new Color(0.11f, 0.09f, 0.095f, 1f);
		[SerializeField] Color textColor = new Color(0.95f, 0.95f, 0.95f);
		[SerializeField] Color mutedTextColor = new Color(0.55f, 0.50f, 0.50f);
		[SerializeField] Color errorColor = new Color(1f, 0.35f, 0.35f);

		enum Mode { SignIn, Register }

		Mode _mode = Mode.SignIn;
		GameObject _canvasGo;

		string _cachedToken = "";
		string _cachedUsername = "";
		string _cachedAccountDisplayName = "";
		string _cachedGuestName = "";

		string _avatarBase64;
		Texture2D _avatarTexture;
		bool _pickingAvatar;

#if TMP_PRESENT
		TMP_InputField _userInput, _passInput, _displayNameInput, _guestInput;
		TextMeshProUGUI _signInStatus, _guestStatus, _avatarStatus;
		TextMeshProUGUI _signInButtonLabel;
#else
		InputField _userInput, _passInput, _displayNameInput, _guestInput;
		Text _signInStatus, _guestStatus, _avatarStatus;
		Text _signInButtonLabel;
#endif
		RawImage _avatarPreview;
		Button _signInButton;

		bool _signingIn;

		// The friends page is page two of the flow: it only exists for players who signed in
		// with a real account, because guest names have no friend graph to show.
		string _accountToken = "";
		string _accountName = "";
		string _sessionToken = "";
		string _lobbyId = "";
		string _friendsSignature = "";
		int _renderedInviteCount = -1;
		RectTransform _friendsContent;
		bool _friendsLoading;
		Image _liveDot;
		readonly List<string[]> _friendRows = new List<string[]>();
		readonly List<LobbyInvite> _pendingInvites = new List<LobbyInvite>();

		class LobbyInvite
		{
			public string lobbyId;
			public string inviteToken;
			public string fromName;
			public int memberCount;
		}

#if TMP_PRESENT
		TMP_InputField _addFriendInput;
		TextMeshProUGUI _friendsStatus, _friendsSummary;
#else
		InputField _addFriendInput;
		Text _friendsStatus, _friendsSummary;
#endif

		void Start()
		{
			WebworksConfig.Load();
			_cachedToken = showAccounts ? PlayerPrefs.GetString(accountTokenPrefsKey, "") : "";
			_cachedUsername = showAccounts ? PlayerPrefs.GetString(accountUsernamePrefsKey, "") : "";
			_cachedAccountDisplayName = showAccounts ? PlayerPrefs.GetString(accountDisplayNamePrefsKey, "") : "";
			_cachedGuestName = PlayerPrefs.GetString(playerPrefsKey, "");
			BuildUi(showAccounts);
		}

		void Update()
		{
			PollWebGLPicker();

			if (_guestInput != null && IsFocused(_guestInput) && SubmitPressed())
				TrySubmitGuest();

			if (!_signingIn && (_userInput != null || _passInput != null)
				&& (IsFocused(_userInput) || IsFocused(_passInput))
				&& SubmitPressed())
				TrySignIn();
		}

		static bool SubmitPressed()
		{
			return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
		}

#if TMP_PRESENT
		static bool IsFocused(TMP_InputField f) { return f != null && f.isFocused; }
#else
		static bool IsFocused(InputField f) { return f != null && f.isFocused; }
#endif

		void TrySubmitGuest()
		{
			string typed = _guestInput == null ? null : _guestInput.text.Trim();
			if (string.IsNullOrEmpty(typed))
			{
				SetStatus(_guestStatus, "Type a name first.", true);
				return;
			}

			if (typed.Length > maxNameLength) typed = typed.Substring(0, maxNameLength);
			PlayerPrefs.SetString(playerPrefsKey, typed);
			PlayerPrefs.Save();
			Steamworks.SteamClient.SetDesiredName(typed);
			AdvanceToNextScene();
		}

		void TrySignIn()
		{
			if (_signingIn) return;


			string username = _userInput == null ? null : _userInput.text.Trim();
			string password = _passInput == null ? null : _passInput.text;

			if (_mode == Mode.SignIn && !string.IsNullOrEmpty(_cachedToken) && string.IsNullOrEmpty(password)
				&& (string.IsNullOrEmpty(username) || username == _cachedUsername))
			{
				Steamworks.SteamClient.SetAccountToken(_cachedToken);
				if (!string.IsNullOrEmpty(_cachedAccountDisplayName))
				{
					PlayerPrefs.SetString(playerPrefsKey, _cachedAccountDisplayName);
					PlayerPrefs.Save();
				}
				_accountToken = _cachedToken;
				_accountName = string.IsNullOrEmpty(_cachedAccountDisplayName) ? username : _cachedAccountDisplayName;
				ShowFriendsStep();
				return;
			}

			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
			{
				SetStatus(_signInStatus, "Enter your username and password.", true);
				return;
			}

			if (_mode == Mode.Register)
			{
				string displayName = _displayNameInput == null ? null : _displayNameInput.text.Trim();
				StartCoroutine(RegisterCoroutine(username, password, displayName));
			}
			else
			{
				StartCoroutine(SignInCoroutine(username, password));
			}
		}

		class AccountsResponse
		{
			public bool success;
			public long statusCode;
			public string bodyText;
		}

		IEnumerator RegisterCoroutine(string username, string password, string displayName)
		{
			_signingIn = true;
			SetSignInInteractable(false);
			SetStatus(_signInStatus, "Creating account...", false);

			string body = "{\"username\":" + WebworksJson.Quote(username) + ",\"password\":" + WebworksJson.Quote(password)
				+ (string.IsNullOrEmpty(displayName) ? "" : ",\"displayName\":" + WebworksJson.Quote(displayName))
				+ (string.IsNullOrEmpty(_avatarBase64) ? "" : ",\"avatarBase64\":" + WebworksJson.Quote(_avatarBase64))
				+ "}";

			AccountsResponse result = null;
			yield return PostJson("/Register", body, r => result = r);

			if (!result.success)
			{
				SetStatus(_signInStatus, FriendlyAuthError(result), true);
				_signingIn = false;
				SetSignInInteractable(true);
				yield break;
			}

			var d = WebworksJson.Obj(result.bodyText);
			string token = d == null ? null : WebworksJson.Str(d, "token");
			if (string.IsNullOrEmpty(token))
			{
				SetStatus(_signInStatus, "The server did not return a session.", true);
				_signingIn = false;
				SetSignInInteractable(true);
				yield break;
			}

			ApplySignedIn(token, username, d);
		}

		IEnumerator SignInCoroutine(string username, string password)
		{
			_signingIn = true;
			SetSignInInteractable(false);
			SetStatus(_signInStatus, "Signing in...", false);

			string body = "{\"username\":" + WebworksJson.Quote(username) + ",\"password\":" + WebworksJson.Quote(password) + "}";
			AccountsResponse result = null;
			yield return PostJson("/Login", body, r => result = r);

			if (!result.success)
			{
				SetStatus(_signInStatus, FriendlyAuthError(result), true);
				_signingIn = false;
				SetSignInInteractable(true);
				yield break;
			}

			var d = WebworksJson.Obj(result.bodyText);
			string token = d == null ? null : WebworksJson.Str(d, "token");
			if (string.IsNullOrEmpty(token))
			{
				SetStatus(_signInStatus, "The server did not return a session.", true);
				_signingIn = false;
				SetSignInInteractable(true);
				yield break;
			}

			ApplySignedIn(token, username, d);
		}

		void ApplySignedIn(string token, string username, Dictionary<string, object> response)
		{
			var accountObj = WebworksJson.Map(response, "account");
			string displayName = accountObj != null ? WebworksJson.Str(accountObj, "displayName") : null;
			if (string.IsNullOrEmpty(displayName)) displayName = username;

			Steamworks.SteamClient.SetAccountToken(token);
			if (rememberSignIn)
			{
				PlayerPrefs.SetString(accountTokenPrefsKey, token);
				PlayerPrefs.SetString(accountUsernamePrefsKey, username);
				PlayerPrefs.SetString(accountDisplayNamePrefsKey, displayName);
			}
			PlayerPrefs.SetString(playerPrefsKey, displayName);
			PlayerPrefs.Save();

			_accountToken = token;
			_accountName = displayName;
			ShowFriendsStep();
		}

		/// <summary>
		/// Page two: the player is signed in, so let them see and add friends before the game
		/// loads. Guests never reach this -- their name is all they have.
		/// </summary>
		void ShowFriendsStep()
		{
			if (!showFriendsStep || string.IsNullOrEmpty(_accountToken))
			{
				AdvanceToNextScene();
				return;
			}

			if (_canvasGo != null) Destroy(_canvasGo);
			_friendRows.Clear();
			_friendsSignature = "";
			_pendingInvites.Clear();
			BuildFriendsUi();
			StartCoroutine(FriendsLoop());
			StartCoroutine(PulseLiveDot());
		}

		/// <summary>
		/// A game session is what the friends and lobby routes authenticate with. The bootstrap
		/// only holds the account token, so it asks for a session once and reuses it for the
		/// rest of this screen: friends, presence and room invites all go through it.
		/// </summary>
		IEnumerator EnsureGameSession()
		{
			if (!string.IsNullOrEmpty(_sessionToken)) yield break;

			string body = "{\"appId\":" + WebworksJson.Quote(WebworksConfig.AppId)
				+ ",\"accountToken\":" + WebworksJson.Quote(_accountToken)
				+ ",\"name\":" + WebworksJson.Quote(_accountName ?? "") + "}";

			AccountsResponse result = null;
			yield return PostJsonTo("/User/GuestSession", body, _accountToken, r => result = r);
			if (result != null && result.success)
			{
				var d = WebworksJson.Obj(result.bodyText);
				string token = d == null ? null : WebworksJson.Str(d, "sessionToken");
				if (!string.IsNullOrEmpty(token)) _sessionToken = token;
			}
		}

		string SessionToken()
		{
			return string.IsNullOrEmpty(_sessionToken) ? _accountToken : _sessionToken;
		}

		/// <summary>
		/// The loop behind the friends page. It refreshes quietly in the background so a
		/// request the other player just sent, or their presence flipping, shows up without
		/// pressing anything, and it keeps the heartbeat going so this player stays Online
		/// for their friends while they sit here.
		/// </summary>
		IEnumerator FriendsLoop()
		{
			yield return StartCoroutine(EnsureGameSession());
			StartCoroutine(HeartbeatLoop());

			bool first = true;
			while (_canvasGo != null)
			{
				yield return StartCoroutine(LoadFriends(!first));
				yield return StartCoroutine(LoadInvites());
				first = false;
				yield return new WaitForSecondsRealtime(Mathf.Max(1f, friendsRefreshSeconds));
			}
		}

		IEnumerator HeartbeatLoop()
		{
			while (_canvasGo != null)
			{
				AccountsResponse result = null;
				yield return PostJsonTo("/User/Heartbeat", "{}", SessionToken(), r => result = r);
				yield return new WaitForSecondsRealtime(Mathf.Max(5f, presenceHeartbeatSeconds));
			}
		}

		IEnumerator PulseLiveDot()
		{
			if (_liveDot == null) yield break;
			float t = 0f;
			while (_canvasGo != null && _liveDot != null)
			{
				t += Time.unscaledDeltaTime * 2f;
				var c = _liveDot.color;
				c.a = 0.35f + Mathf.Abs(Mathf.Sin(t)) * 0.65f;
				_liveDot.color = c;
				yield return null;
			}
		}

		IEnumerator LoadFriends(bool soft)
		{
			_friendsLoading = true;
			if (!soft) SetStatus(_friendsStatus, "Loading friends...", false);

			AccountsResponse result = null;
			yield return GetJson("/Friends/Graph", SessionToken(), r => result = r);
			_friendsLoading = false;

			if (!result.success)
			{
				if (soft) yield break;
				ClearFriendsList();
				AddFriendRow("Friends could not be loaded", "the server did not answer", null, null, false);
				SetStatus(_friendsStatus, result.statusCode == 0 ? "Couldn't reach the server." : "Couldn't load friends (" + result.statusCode + ").", true);
				SetFriendsSummary("You can still continue to the game.");
				if (_liveDot != null) _liveDot.color = new Color(0.55f, 0.5f, 0.5f, 0.8f);
				yield break;
			}
			if (_liveDot != null) _liveDot.color = new Color(0.43f, 0.86f, 0.55f, 1f);

			var d = WebworksJson.Obj(result.bodyText);
			var accepted = WebworksJson.Arr(d, "accepted") ?? new List<object>();
			var incoming = WebworksJson.Arr(d, "incoming") ?? new List<object>();
			var outgoing = WebworksJson.Arr(d, "outgoing") ?? new List<object>();

			// Rebuilding identical rows every few seconds would flicker and steal focus, so a
			// poll only lays the page out again when something actually changed.
			string signature = FriendsSignature(accepted, incoming, outgoing);
			if (soft && signature == _friendsSignature) yield break;
			_friendsSignature = signature;

			ClearFriendsList();
			_friendRows.Clear();

			RenderInvites();

			var ordered = new List<Dictionary<string, object>>();
			foreach (var raw in accepted)
			{
				var fd = raw as Dictionary<string, object>;
				if (fd != null) ordered.Add(fd);
			}
			ordered.Sort((a, b) =>
			{
				int ao = WebworksJson.Str(a, "personaState") == "Online" ? 0 : 1;
				int bo = WebworksJson.Str(b, "personaState") == "Online" ? 0 : 1;
				if (ao != bo) return ao - bo;
				return string.Compare(FriendDisplay(a), FriendDisplay(b), StringComparison.OrdinalIgnoreCase);
			});

			int onlineCount = 0;
			foreach (var fd in ordered)
			{
				string username = WebworksJson.Str(fd, "username");
				string display = FriendDisplay(fd);
				string persona = WebworksJson.Str(fd, "personaState");
				string game = WebworksJson.Str(fd, "game");
				string steamId = WebworksJson.Str(fd, "steamId");
				bool online = persona == "Online";
				if (online) onlineCount++;

				string detail = !string.IsNullOrEmpty(game) ? "in game" : (online ? "online" : "offline");
				string targetName = username;
				string targetId = steamId;
				AddFriendRow(display, detail, online ? "Invite" : null,
					online ? (Action)(() => InviteFriend(targetName, targetId)) : null,
					online);
				_friendRows.Add(new[] { username, display });
			}

			foreach (var raw in incoming)
			{
				string username = ItemText(raw);
				if (string.IsNullOrEmpty(username)) continue;
				string name = username;
				AddFriendRow(name, "wants to be friends", "Accept", () => StartCoroutine(AcceptFriend(username)), false);
			}

			foreach (var raw in outgoing)
			{
				string username = ItemText(raw);
				if (string.IsNullOrEmpty(username)) continue;
				AddFriendRow(username, "request sent", null, null, false);
			}

			if (accepted.Count == 0 && incoming.Count == 0 && outgoing.Count == 0)
			{
				AddFriendRow("No friends yet", "add someone by username below", null, null, false);
			}

			SetFriendsSummary(accepted.Count + (accepted.Count == 1 ? " friend" : " friends") + " \u00b7 " + onlineCount + " online"
				+ (incoming.Count > 0 ? " \u00b7 " + incoming.Count + " waiting" : ""));
		}

		static string FriendDisplay(Dictionary<string, object> fd)
		{
			string display = WebworksJson.Str(fd, "displayName");
			if (string.IsNullOrEmpty(display)) display = WebworksJson.Str(fd, "name");
			if (string.IsNullOrEmpty(display)) display = WebworksJson.Str(fd, "username");
			return display;
		}

		string FriendsSignature(List<object> accepted, List<object> incoming, List<object> outgoing)
		{
			var sb = new StringBuilder();
			foreach (var raw in accepted)
			{
				var fd = raw as Dictionary<string, object>;
				if (fd == null) continue;
				sb.Append(WebworksJson.Str(fd, "username")).Append('|').Append(WebworksJson.Str(fd, "personaState"))
					.Append('|').Append(WebworksJson.Str(fd, "game")).Append('|').Append(FriendDisplay(fd)).Append(';');
			}
			sb.Append('#');
			foreach (var raw in incoming) sb.Append(ItemText(raw)).Append(',');
			sb.Append('#');
			foreach (var raw in outgoing) sb.Append(ItemText(raw)).Append(',');
			sb.Append('#').Append(_pendingInvites.Count);
			foreach (var invite in _pendingInvites) sb.Append(invite.lobbyId).Append(',');
			return sb.ToString();
		}

		IEnumerator LoadInvites()
		{
			AccountsResponse result = null;
			yield return GetJson("/Matchmaking/Invites", SessionToken(), r => result = r);
			if (!result.success) yield break;

			var d = WebworksJson.Obj(result.bodyText);
			var list = WebworksJson.Arr(d, "invites") ?? new List<object>();
			_pendingInvites.Clear();
			foreach (var raw in list)
			{
				var invite = raw as Dictionary<string, object>;
				if (invite == null) continue;
				_pendingInvites.Add(new LobbyInvite
				{
					lobbyId = WebworksJson.Str(invite, "lobbyId"),
					inviteToken = WebworksJson.Str(invite, "inviteToken"),
					fromName = WebworksJson.Str(invite, "fromName"),
					memberCount = WebworksJson.Int(invite, "memberCount"),
				});
			}

			// Invites sit above the friend rows, so a new one re-lays out the list once.
			if (_pendingInvites.Count != _renderedInviteCount)
			{
				_friendsSignature = "";
				yield return StartCoroutine(LoadFriends(true));
			}
		}

		void RenderInvites()
		{
			_renderedInviteCount = _pendingInvites.Count;
			foreach (var invite in _pendingInvites)
			{
				var captured = invite;
				string who = string.IsNullOrEmpty(invite.fromName) ? "A friend" : invite.fromName;
				string detail = (invite.memberCount > 0 ? invite.memberCount + " in room \u00b7 " : "") + "tap Join";
				AddFriendRow(who + " invited you", detail, "Join", () => StartCoroutine(JoinInviteCoroutine(captured)), true);
			}
		}

		/// <summary>
		/// Inviting from this page opens a room on first use, then hands the friend a one-time
		/// invite. The room id is stored for the game scene so "Continue" lands in the same room.
		/// </summary>
		void InviteFriend(string username, string steamId)
		{
			if (_friendsLoading) return;
			if (string.IsNullOrEmpty(steamId))
			{
				SetStatus(_friendsStatus, "That friend has no steam id yet. Try again in a moment.", true);
				return;
			}
			StartCoroutine(InviteFriendCoroutine(username, steamId));
		}

		IEnumerator InviteFriendCoroutine(string username, string steamId)
		{
			_friendsLoading = true;
			SetStatus(_friendsStatus, "Opening your room...", false);

			if (string.IsNullOrEmpty(_lobbyId))
			{
				AccountsResponse created = null;
				yield return PostJsonTo("/Matchmaking/CreateLobby", "{\"maxMembers\":8}", SessionToken(), r => created = r);
				if (created == null || !created.success)
				{
					_friendsLoading = false;
					SetStatus(_friendsStatus, "Could not open a room (" + (created == null ? 0 : created.statusCode) + ").", true);
					yield break;
				}
				var lobby = WebworksJson.Obj(created.bodyText);
				_lobbyId = lobby == null ? null : WebworksJson.Str(lobby, "id");
				if (string.IsNullOrEmpty(_lobbyId))
				{
					_friendsLoading = false;
					SetStatus(_friendsStatus, "The server did not return a room id.", true);
					yield break;
				}
				PlayerPrefs.SetString(joinLobbyPrefsKey, _lobbyId);
				PlayerPrefs.Save();
			}

			SetStatus(_friendsStatus, "Inviting " + username + "...", false);
			string body = "{\"lobbyId\":" + WebworksJson.Quote(_lobbyId) + ",\"friendSteamId\":" + WebworksJson.Quote(steamId) + "}";
			AccountsResponse result = null;
			yield return PostJsonTo("/Matchmaking/InviteFriend", body, SessionToken(), r => result = r);
			_friendsLoading = false;

			if (result == null || !result.success)
			{
				SetStatus(_friendsStatus, FriendlyInviteError(result), true);
				yield break;
			}
			SetStatus(_friendsStatus, "Invited " + username + ". Room code " + _lobbyId + ".", false);
		}

		static string FriendlyInviteError(AccountsResponse resp)
		{
			if (resp == null || resp.statusCode == 0) return "Couldn't reach the server.";
			Dictionary<string, object> body = null;
			try { body = WebworksJson.Obj(resp.bodyText); } catch (Exception) { }
			string code = body == null ? null : WebworksJson.Str(body, "error");
			switch (code)
			{
				case "cannot_invite_yourself": return "That is you.";
				case "missing_friend_steam_id": return "That friend is not known to the server yet.";
				case "lobby_not_found": return "Your room has closed. Try again.";
				case "not_a_lobby_member": return "Your room has closed. Try again.";
				case "invalid_or_missing_session": return "Sign in again to invite friends.";
				default: return "Could not invite them (" + resp.statusCode + ").";
			}
		}

		IEnumerator JoinInviteCoroutine(LobbyInvite invite)
		{
			SetStatus(_friendsStatus, "Joining " + (string.IsNullOrEmpty(invite.fromName) ? "the room" : invite.fromName + "'s room") + "...", false);
			string body = "{\"lobbyId\":" + WebworksJson.Quote(invite.lobbyId) + "}";
			AccountsResponse result = null;
			yield return PostJsonTo("/Matchmaking/JoinLobby", body, SessionToken(), r => result = r);

			if (result == null || !result.success)
			{
				SetStatus(_friendsStatus, result != null && result.statusCode == 404 ? "That room has closed." : "Could not join that room.", true);
				yield break;
			}

			var lobby = WebworksJson.Obj(result.bodyText);
			_lobbyId = lobby == null ? invite.lobbyId : (WebworksJson.Str(lobby, "id") ?? invite.lobbyId);
			PlayerPrefs.SetString(joinLobbyPrefsKey, _lobbyId);
			PlayerPrefs.Save();

			var dismiss = invite;
			StartCoroutine(DismissInviteCoroutine(dismiss));
			SetStatus(_friendsStatus, "Joined " + (string.IsNullOrEmpty(invite.fromName) ? "the room" : invite.fromName + "'s room") + ". Continue to play.", false);
			_friendsSignature = "";
			yield return StartCoroutine(LoadInvites());
			yield return StartCoroutine(LoadFriends(true));
		}

		IEnumerator DismissInviteCoroutine(LobbyInvite invite)
		{
			string body = "{\"lobbyId\":" + WebworksJson.Quote(invite.lobbyId) + "}";
			AccountsResponse ignored = null;
			yield return PostJsonTo("/Matchmaking/Invites/Dismiss", body, SessionToken(), r => ignored = r);
			_pendingInvites.RemoveAll(i => i.lobbyId == invite.lobbyId);
			_friendsSignature = "";
			if (_canvasGo != null) yield return StartCoroutine(LoadFriends(true));
		}

		void BuildFriendsUi()
		{
			EnsureEventSystem();

			var canvasGo = new GameObject("WebworksFriendsCanvas");
			_canvasGo = canvasGo;
			canvasGo.transform.SetParent(transform, false);
			var canvas = canvasGo.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			var scaler = canvasGo.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1280, 800);
			canvasGo.AddComponent<GraphicRaycaster>();

			var bg = MakeUiObject("Backdrop", canvasGo.transform);
			var bgImage = bg.AddComponent<Image>();
			bgImage.color = backdropColor;
			StretchToParent(bg.GetComponent<RectTransform>());

			float panelWidth = 560;
			float panelHeight = friendsPanelHeight;
			var logoTex = Resources.Load<Texture2D>("WebworksLogo");
			if (logoTex != null) panelHeight += 60;

			var panel = MakeUiObject("FriendsPanel", bg.transform);
			var panelImage = panel.AddComponent<Image>();
			panelImage.color = panelColor;
			SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(panelWidth, panelHeight));

			var accentBar = MakeUiObject("AccentBar", panel.transform);
			var accentImage = accentBar.AddComponent<Image>();
			accentImage.color = accentColor;
			var accentRect = accentBar.GetComponent<RectTransform>();
			accentRect.anchorMin = new Vector2(0, 1);
			accentRect.anchorMax = new Vector2(1, 1);
			accentRect.pivot = new Vector2(0.5f, 1f);
			accentRect.sizeDelta = new Vector2(0, 4);
			accentRect.anchoredPosition = Vector2.zero;

			float y = panelHeight / 2f - 40;
			if (logoTex != null)
			{
				var logoGo = MakeUiObject("Logo", panel.transform);
				SetRect(logoGo.GetComponent<RectTransform>(), new Vector2(0, y - 16), new Vector2(48, 48));
				var logoImage = logoGo.AddComponent<RawImage>();
				logoImage.texture = logoTex;
				logoImage.color = Color.white;
				y -= 60;
			}

			var title = MakeText(panel.transform, "Title", friendsTitle, 22, TextAnchor.MiddleCenter, textColor, FontStyle.Bold);
			SetRect(title.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(panelWidth - 40, 30));
			y -= 30;

			var who = MakeText(panel.transform, "SignedInAs", "Signed in as " + (_accountName ?? ""), 13, TextAnchor.MiddleCenter, mutedTextColor, FontStyle.Normal);
			SetRect(who.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(panelWidth - 40, 20));

			// A quiet heartbeat dot: green while the page is polling, grey when the server is
			// not answering. PulseLiveDot animates its alpha so it reads as live.
			var liveGo = MakeUiObject("LiveDot", panel.transform);
			SetRect(liveGo.GetComponent<RectTransform>(), new Vector2(panelWidth / 2f - 30, y), new Vector2(8, 8));
			_liveDot = liveGo.AddComponent<Image>();
			_liveDot.color = new Color(0.43f, 0.86f, 0.55f, 1f);
			y -= 34;

			_friendsContent = MakeScrollArea(panel.transform, new Vector2(0, y - 158), new Vector2(panelWidth - 48, 316));
			var loading = MakeText(_friendsContent, "Loading", "Loading friends...", 13, TextAnchor.MiddleCenter, mutedTextColor, FontStyle.Normal);
			SetRect(loading.GetComponent<RectTransform>(), Vector2.zero, new Vector2(panelWidth - 64, 24));
			y -= 336;

			var summary = MakeText(panel.transform, "Summary", "", 12, TextAnchor.MiddleCenter, mutedTextColor, FontStyle.Normal);
			SetRect(summary.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(panelWidth - 48, 18));
#if TMP_PRESENT
			_friendsSummary = summary.GetComponent<TextMeshProUGUI>();
#else
			_friendsSummary = summary.GetComponent<Text>();
#endif
			y -= 26;

			_addFriendInput = MakeInputField(panel.transform, new Vector2(-52, y), new Vector2(panelWidth - 200, 38), addFriendPlaceholder, false);
			var addGo = MakeUiObject("AddFriendButton", panel.transform);
			SetRect(addGo.GetComponent<RectTransform>(), new Vector2(panelWidth / 2f - 78, y), new Vector2(120, 38));
			var addImage = addGo.AddComponent<Image>();
			addImage.color = new Color(1f, 1f, 1f, 0.06f);
			var addButton = addGo.AddComponent<Button>();
			addButton.onClick.AddListener(AddFriendByName);
			var addColors = addButton.colors;
			addColors.highlightedColor = new Color(1f, 1f, 1f, 0.14f);
			addButton.colors = addColors;
			var addLabel = MakeText(addGo.transform, "Label", "Add friend", 13, TextAnchor.MiddleCenter, accentColorBright, FontStyle.Bold);
			StretchToParent(addLabel.GetComponent<RectTransform>());
			y -= 46;

			var status = MakeText(panel.transform, "FriendsStatus", "", 12, TextAnchor.MiddleCenter, mutedTextColor, FontStyle.Normal);
			SetRect(status.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(panelWidth - 40, 18));
#if TMP_PRESENT
			_friendsStatus = status.GetComponent<TextMeshProUGUI>();
#else
			_friendsStatus = status.GetComponent<Text>();
#endif
			y -= 34;

			var continueGo = MakeUiObject("ContinueButton", panel.transform);
			SetRect(continueGo.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(panelWidth - 64, 44));
			var continueImage = continueGo.AddComponent<Image>();
			continueImage.color = accentColor;
			var continueButton = continueGo.AddComponent<Button>();
			var continueColors = continueButton.colors;
			continueColors.highlightedColor = accentColorBright;
			continueColors.pressedColor = accentColorBright;
			continueButton.colors = continueColors;
			continueButton.onClick.AddListener(AdvanceToNextScene);
			var continueLabel = MakeText(continueGo.transform, "Label", friendsContinueLabel, 15, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
			StretchToParent(continueLabel.GetComponent<RectTransform>());
			y -= 34;

			var signOutGo = MakeUiObject("SignOut", panel.transform);
			SetRect(signOutGo.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(panelWidth - 64, 22));
			var signOutHit = signOutGo.AddComponent<Image>();
			signOutHit.color = new Color(0, 0, 0, 0);
			var signOutButton = signOutGo.AddComponent<Button>();
			signOutButton.onClick.AddListener(SignOutAndRebuild);
			var signOutLabel = MakeText(signOutGo.transform, "Label", "Use a different account", 12, TextAnchor.MiddleCenter, mutedTextColor, FontStyle.Bold);
			StretchToParent(signOutLabel.GetComponent<RectTransform>());

			if (_addFriendInput != null) { _addFriendInput.Select(); _addFriendInput.ActivateInputField(); }
		}

		void SignOutAndRebuild()
		{
			StopAllCoroutines();
			SignOut(playerPrefsKey, accountTokenPrefsKey, accountUsernamePrefsKey, accountDisplayNamePrefsKey);
			_accountToken = "";
			_accountName = "";
			_sessionToken = "";
			_lobbyId = "";
			_cachedToken = "";
			_cachedUsername = "";
			_cachedAccountDisplayName = "";
			_pendingInvites.Clear();
			if (_canvasGo != null) Destroy(_canvasGo);
			_mode = Mode.SignIn;
			BuildUi(showAccounts);
		}

		void SetFriendsSummary(string text)
		{
			if (_friendsSummary != null) _friendsSummary.text = text;
		}

		IEnumerator AcceptFriend(string username)
		{
			SetStatus(_friendsStatus, "Accepting " + username + "...", false);
			AccountsResponse result = null;
			yield return PostJsonTo("/Friends/Accept", "{\"username\":" + WebworksJson.Quote(username) + "}", _accountToken, r => result = r);
			if (!result.success)
			{
				SetStatus(_friendsStatus, "Could not accept " + username + " (" + result.statusCode + ").", true);
				yield break;
			}
			yield return LoadFriends(true);
		}

		void AddFriendByName()
		{
			if (_friendsLoading) return;
			string username = _addFriendInput == null ? null : _addFriendInput.text.Trim();
			if (string.IsNullOrEmpty(username))
			{
				SetStatus(_friendsStatus, "Type the username you want to add.", true);
				return;
			}
			StartCoroutine(AddFriendCoroutine(username));
		}

		IEnumerator AddFriendCoroutine(string username)
		{
			_friendsLoading = true;
			SetStatus(_friendsStatus, "Sending a request to " + username + "...", false);
			AccountsResponse result = null;
			yield return PostJsonTo("/Friends/Add", "{\"username\":" + WebworksJson.Quote(username) + "}", _accountToken, r => result = r);

			if (!result.success)
			{
				_friendsLoading = false;
				SetStatus(_friendsStatus, FriendlyFriendError(result), true);
				yield break;
			}

			if (_addFriendInput != null) _addFriendInput.text = "";
			yield return LoadFriends(true);
			_friendsLoading = false;
			SetStatus(_friendsStatus, "Friend request sent to " + username + ".", false);
		}

		static string FriendlyFriendError(AccountsResponse resp)
		{
			if (resp == null || resp.statusCode == 0) return "Couldn't reach the server.";
			Dictionary<string, object> body = null;
			try { body = WebworksJson.Obj(resp.bodyText); } catch (Exception) { }
			string code = body == null ? null : WebworksJson.Str(body, "error");
			switch (code)
			{
				case "not_signed_in": return "Sign in with an account first.";
				case "no_such_account": return "No account with that username.";
				case "missing_username": return "Type the username you want to add.";
				case "cannot_friend_yourself": return "That is you.";
				case "rate_limited": return "Too many requests. Wait a moment.";
				default: return "Could not add that player (" + resp.statusCode + ").";
			}
		}

		void ClearFriendsList()
		{
			if (_friendsContent == null) return;
			for (int i = _friendsContent.childCount - 1; i >= 0; i--)
				Destroy(_friendsContent.GetChild(i).gameObject);
		}

		void AddFriendRow(string title, string detail, string actionLabel, Action action, bool online)
		{
			if (_friendsContent == null) return;

			var row = MakeUiObject("FriendRow", _friendsContent);
			var element = row.AddComponent<LayoutElement>();
			element.minHeight = 40;
			element.preferredHeight = 40;

			// Round avatar with the first letter of the name, tinted per player. A muted avatar
			// is offline; a bright one is online, which is readable at a glance in a list.
			var avatarGo = MakeUiObject("Avatar", row.transform);
			var avatarRect = avatarGo.GetComponent<RectTransform>();
			avatarRect.anchorMin = new Vector2(0, 0.5f);
			avatarRect.anchorMax = new Vector2(0, 0.5f);
			avatarRect.pivot = new Vector2(0, 0.5f);
			avatarRect.anchoredPosition = new Vector2(8, 0);
			avatarRect.sizeDelta = new Vector2(26, 26);
			var avatarImage = avatarGo.AddComponent<Image>();
			avatarImage.sprite = CircleSprite();
			var avatarColor = AvatarColor(title);
			avatarColor.a = online ? 1f : 0.45f;
			avatarImage.color = avatarColor;

			string initial = string.IsNullOrEmpty(title) ? "?" : title.Substring(0, 1).ToUpperInvariant();
			var initialLabel = MakeText(avatarGo.transform, "Initial", initial, 12, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, online ? 1f : 0.7f), FontStyle.Bold);
			StretchToParent(initialLabel.GetComponent<RectTransform>());

			var nameLabel = MakeText(row.transform, "Name", title ?? "", 14, TextAnchor.MiddleLeft, online ? textColor : mutedTextColor, online ? FontStyle.Bold : FontStyle.Normal);
			var nameRect = nameLabel.GetComponent<RectTransform>();
			nameRect.anchorMin = new Vector2(0, 0);
			nameRect.anchorMax = new Vector2(1, 1);
			nameRect.offsetMin = new Vector2(44, 0);
			nameRect.offsetMax = new Vector2(action != null ? -96 : -8, 0);

			if (!string.IsNullOrEmpty(detail))
			{
				var detailLabel = MakeText(row.transform, "Detail", detail, 12, TextAnchor.MiddleRight, mutedTextColor, FontStyle.Normal);
				var detailRect = detailLabel.GetComponent<RectTransform>();
				detailRect.anchorMin = new Vector2(0.45f, 0);
				detailRect.anchorMax = new Vector2(1, 1);
				detailRect.offsetMin = new Vector2(0, 0);
				detailRect.offsetMax = new Vector2(action != null ? -96 : -10, 0);
			}

			if (action == null) return;

			var buttonGo = MakeUiObject("Action", row.transform);
			var buttonRect = buttonGo.GetComponent<RectTransform>();
			buttonRect.anchorMin = new Vector2(1, 0.5f);
			buttonRect.anchorMax = new Vector2(1, 0.5f);
			buttonRect.pivot = new Vector2(1, 0.5f);
			buttonRect.anchoredPosition = new Vector2(-6, 0);
			buttonRect.sizeDelta = new Vector2(84, 26);
			var buttonImage = buttonGo.AddComponent<Image>();
			buttonImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.85f);
			var button = buttonGo.AddComponent<Button>();
			var captured = action;
			button.onClick.AddListener(() => captured());
			var buttonColors = button.colors;
			buttonColors.highlightedColor = accentColorBright;
			buttonColors.pressedColor = accentColorBright;
			button.colors = buttonColors;
			var label = MakeText(buttonGo.transform, "Label", actionLabel ?? "", 12, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
			StretchToParent(label.GetComponent<RectTransform>());
		}

		static Sprite _circleSprite;

		static Sprite CircleSprite()
		{
			if (_circleSprite != null) return _circleSprite;
			const int size = 40;
			var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
			texture.hideFlags = HideFlags.HideAndDontSave;
			var pixels = new Color32[size * size];
			float centre = (size - 1) / 2f;
			float radius = centre - 0.5f;
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float dx = x - centre;
					float dy = y - centre;
					float distance = Mathf.Sqrt(dx * dx + dy * dy);
					float alpha = Mathf.Clamp01(radius - distance + 1f);
					pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
				}
			}
			texture.SetPixels32(pixels);
			texture.Apply();
			_circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
			return _circleSprite;
		}

		static Color AvatarColor(string name)
		{
			int hash = 17;
			if (!string.IsNullOrEmpty(name))
			{
				foreach (char c in name) hash = unchecked(hash * 31 + c);
			}
			float hue = Mathf.Abs(hash % 360) / 360f;
			return Color.HSVToRGB(hue, 0.55f, 0.72f);
		}

		// A vertically scrolling list of fixed-height rows. The rows are created at runtime, so
		// the content fits its own height instead of needing a hand-authored prefab.
		static RectTransform MakeScrollArea(Transform parent, Vector2 anchoredPosition, Vector2 size)
		{
			var scrollGo = MakeUiObject("Scroll", parent);
			SetRect(scrollGo.GetComponent<RectTransform>(), anchoredPosition, size);
			var scroll = scrollGo.AddComponent<ScrollRect>();
			scroll.horizontal = false;
			scroll.vertical = true;
			scroll.movementType = ScrollRect.MovementType.Clamped;
			scroll.scrollSensitivity = 24f;

			var viewport = MakeUiObject("Viewport", scrollGo.transform);
			StretchToParent(viewport.GetComponent<RectTransform>());
			var viewportImage = viewport.AddComponent<Image>();
			viewportImage.color = new Color(0f, 0f, 0f, 0.18f);
			viewport.AddComponent<RectMask2D>();
			scroll.viewport = viewport.GetComponent<RectTransform>();

			var content = MakeUiObject("Content", viewport.transform);
			var contentRect = content.GetComponent<RectTransform>();
			contentRect.anchorMin = new Vector2(0, 1);
			contentRect.anchorMax = new Vector2(1, 1);
			contentRect.pivot = new Vector2(0.5f, 1f);
			contentRect.anchoredPosition = Vector2.zero;
			contentRect.sizeDelta = Vector2.zero;

			var layout = content.AddComponent<VerticalLayoutGroup>();
			layout.childControlHeight = true;
			layout.childControlWidth = true;
			layout.childForceExpandHeight = false;
			layout.childForceExpandWidth = true;
			layout.spacing = 2f;
			layout.padding = new RectOffset(6, 6, 6, 6);

			var fitter = content.AddComponent<ContentSizeFitter>();
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

			scroll.content = contentRect;
			return contentRect;
		}

		static string ItemText(object raw)
		{
			var text = raw as string;
			if (text != null) return text;
			var map = raw as Dictionary<string, object>;
			return map == null ? null : WebworksJson.Str(map, "username");
		}

		IEnumerator GetJson(string endpoint, string token, Action<AccountsResponse> onDone)
		{
			AccountsResponse result = null;
			foreach (var engine in WebworksConfig.EngineCandidates())
			{
				string url = string.IsNullOrEmpty(engine)
					? WebworksConfig.ServerUrl + "/api" + endpoint
					: WebworksConfig.ServerUrl + "/api/" + engine + endpoint;

				using (var req = UnityWebRequest.Get(url))
				{
					req.downloadHandler = new DownloadHandlerBuffer();
					req.timeout = Mathf.Max(1, WebworksConfig.RequestTimeoutMs / 1000);
					ApplySession(req, token);
					yield return req.SendWebRequest();

					result = new AccountsResponse
					{
						success = RequestSucceeded(req),
						statusCode = req.responseCode,
						bodyText = req.downloadHandler != null ? req.downloadHandler.text : null
					};
				}

				if (!RouteMissing(result)) break;
			}
			onDone(result);
		}

		IEnumerator PostJsonTo(string endpoint, string jsonBody, string token, Action<AccountsResponse> onDone)
		{
			AccountsResponse result = null;
			foreach (var engine in WebworksConfig.EngineCandidates())
			{
				string url = string.IsNullOrEmpty(engine)
					? WebworksConfig.ServerUrl + "/api" + endpoint
					: WebworksConfig.ServerUrl + "/api/" + engine + endpoint;

				using (var req = new UnityWebRequest(url, "POST"))
				{
					req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
					req.downloadHandler = new DownloadHandlerBuffer();
					req.SetRequestHeader("Content-Type", "application/json");
					req.timeout = Mathf.Max(1, WebworksConfig.RequestTimeoutMs / 1000);
					ApplySession(req, token);
					yield return req.SendWebRequest();

					result = new AccountsResponse
					{
						success = RequestSucceeded(req),
						statusCode = req.responseCode,
						bodyText = req.downloadHandler != null ? req.downloadHandler.text : null
					};
				}

				if (!RouteMissing(result)) break;
			}
			onDone(result);
		}

		static void ApplySession(UnityWebRequest req, string token)
		{
			if (string.IsNullOrEmpty(token)) return;
			req.SetRequestHeader("x-webworks-session", token);
			req.SetRequestHeader("x-webeam-session", token);
			req.SetRequestHeader("Accept", "application/json");
		}

		static void EnsureEventSystem()
		{
			if (EventSystem.current != null) return;
			var es = new GameObject("EventSystem");
			es.AddComponent<EventSystem>();
			es.AddComponent<StandaloneInputModule>();
		}

		/// <summary>
		/// Where an account call may live, in order of preference: this server's engine mount, the
		/// same handlers under the bare /accounts prefix, and finally the pre-rename Accounts Worker
		/// (which the old shim talked to directly as POST /accounts/login). Older servers served only
		/// the last two, so a new shim keeps working there without any config change.
		/// </summary>
		static string[] AccountUrls(string endpoint)
		{
			var urls = new List<string>();
			urls.Add(WebworksConfig.AccountsApiBase + "/" + endpoint);
			urls.Add(WebworksConfig.ServerUrl + "/accounts/" + endpoint);
			if (!string.IsNullOrEmpty(WebworksConfig.AccountsWorkerUrl))
				urls.Add(WebworksConfig.AccountsWorkerUrl + "/accounts/" + endpoint.ToLowerInvariant());
			return urls.ToArray();
		}

		// Express answers an unknown route with HTML; our own errors are JSON. Only the former means
		// "this server doesn't mount the account routes here, try the next base".
		static bool RouteMissing(AccountsResponse reply)
		{
			if (reply == null) return true;
			if (reply.statusCode != 404 && reply.statusCode != 405) return false;
			var body = (reply.bodyText ?? "").TrimStart();
			return body.Length == 0 || body[0] != '{';
		}

		IEnumerator PostJson(string endpoint, string jsonBody, Action<AccountsResponse> onDone)
		{
			AccountsResponse result = null;
			foreach (var url in AccountUrls(endpoint))
			{
				using (var req = new UnityWebRequest(url, "POST"))
				{
					req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
					req.downloadHandler = new DownloadHandlerBuffer();
					req.SetRequestHeader("Content-Type", "application/json");
					req.timeout = Mathf.Max(1, WebworksConfig.RequestTimeoutMs / 1000);
					yield return req.SendWebRequest();

					result = new AccountsResponse
					{
						success = RequestSucceeded(req),
						statusCode = req.responseCode,
						bodyText = req.downloadHandler != null ? req.downloadHandler.text : null
					};
				}

				if (!RouteMissing(result)) break;
			}
			onDone(result);
		}

		static bool RequestSucceeded(UnityWebRequest req)
		{
#if UNITY_2020_2_OR_NEWER
			return req.result == UnityWebRequest.Result.Success;
#else
			return !req.isNetworkError && !req.isHttpError;
#endif
		}

		static string FriendlyAuthError(AccountsResponse resp)
		{
			if (resp == null) return "Couldn't reach the server.";
			Dictionary<string, object> body = null;
			try { body = WebworksJson.Obj(resp.bodyText); } catch (Exception) { }
			string code = body == null ? null : WebworksJson.Str(body, "error");

			switch (code)
			{
				case "bad_credentials": return "Incorrect username or password.";
				case "no_such_account": return "No account with that username.";
				case "invalid_username": return "Use 3-32 letters, numbers, dots, dashes or underscores, starting with a letter or number.";
				case "invalid_password": return "Password must be 6-256 characters.";
				case "username_taken": return "That username is already taken.";
				case "avatar_too_large": return "That icon is too large. Pick a smaller image.";
				case "avatar_too_small": return "That icon is too small. Pick a larger image.";
				case "unsupported_avatar_format": return "Use a PNG, JPEG or WebP image.";
				case "missing_avatar": return "The icon could not be read.";
				case "rate_limited": return "Too many attempts. Wait a minute and try again.";
				case "accounts_unavailable": return "The accounts service is unavailable.";
				default:
					if (resp.statusCode == 0) return "Couldn't reach the server.";
					return "Something went wrong (" + resp.statusCode + ").";
			}
		}

		void SetSignInInteractable(bool interactable)
		{
			if (_signInButton != null) _signInButton.interactable = interactable;
			if (_signInButtonLabel != null) _signInButtonLabel.text = interactable ? (_mode == Mode.Register ? "Create Account" : "Sign In") : (_mode == Mode.Register ? "Creating..." : "Signing In...");
			if (_userInput != null) _userInput.interactable = interactable;
			if (_passInput != null) _passInput.interactable = interactable;
			if (_displayNameInput != null) _displayNameInput.interactable = interactable;
		}

		void AdvanceToNextScene()
		{
			if (string.IsNullOrEmpty(_sceneNameToLoad))
			{
				Debug.LogWarning("[Webworks] WebworksNameEntry has no \"Scene To Load\" assigned -- staying on this scene.");
				return;
			}
			SceneManager.LoadScene(_sceneNameToLoad);
		}

		public static void SignOut(string playerPrefsKey = "webworks_player_name", string accountTokenPrefsKey = "webworks_account_token", string accountUsernamePrefsKey = "webworks_account_username", string accountDisplayNamePrefsKey = "webworks_account_display_name")
		{
			PlayerPrefs.DeleteKey(playerPrefsKey);
			PlayerPrefs.DeleteKey(accountTokenPrefsKey);
			PlayerPrefs.DeleteKey(accountUsernamePrefsKey);
			PlayerPrefs.DeleteKey(accountDisplayNamePrefsKey);
			PlayerPrefs.Save();
		}

		public static void ClearCachedName(string playerPrefsKey = "webworks_player_name")
		{
			PlayerPrefs.DeleteKey(playerPrefsKey);
			PlayerPrefs.Save();
		}

		void ToggleMode()
		{
			if (_signingIn) return;
			_mode = _mode == Mode.SignIn ? Mode.Register : Mode.SignIn;
			if (_canvasGo != null) Destroy(_canvasGo);
			BuildUi(showAccounts);
		}

		void PickAvatar()
		{
			if (_pickingAvatar) return;

#if UNITY_EDITOR
			string path = EditorUtility.OpenFilePanel("Choose account icon", "", "png,jpg,jpeg,webp");
			if (!string.IsNullOrEmpty(path)) StartCoroutine(LoadAvatarFromPath(path));
#elif UNITY_WEBGL && !UNITY_EDITOR
			_pickingAvatar = true;
			WebworksNative.Webworks_PickImage();
			SetStatus(_avatarStatus, "Choose an image in the browser...", false);
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			string path = NativeImagePicker.Open("Choose account icon", "Image files\0*.png;*.jpg;*.jpeg;*.webp\0All files\0*.*\0");
			if (!string.IsNullOrEmpty(path)) StartCoroutine(LoadAvatarFromPath(path));
#else
			SetStatus(_avatarStatus, "Icon picking is not available on this platform. Type an image path in the field above.", true);
#endif
		}

		void PollWebGLPicker()
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			if (!_pickingAvatar) return;
			string dataUrl = WebworksNative.Webworks_TakePickedImage();
			if (string.IsNullOrEmpty(dataUrl)) return;
			_pickingAvatar = false;
			if (dataUrl.StartsWith("data:image/"))
			{
				int comma = dataUrl.IndexOf(',');
				if (comma > 0) ApplyAvatarBytes(Convert.FromBase64String(dataUrl.Substring(comma + 1)));
			}
			else
			{
				SetStatus(_avatarStatus, "That file is not an image.", true);
			}
#endif
		}

		IEnumerator LoadAvatarFromPath(string path)
		{
			_pickingAvatar = true;
			SetStatus(_avatarStatus, "Reading image...", false);
			byte[] bytes = null;
			try { bytes = File.ReadAllBytes(path); }
			catch (Exception ex) { SetStatus(_avatarStatus, "Could not read that file: " + ex.Message, true); }

			yield return null;
			if (bytes != null)
			{
				ApplyAvatarBytes(bytes);
			}
			_pickingAvatar = false;
		}

		void ApplyAvatarBytes(byte[] bytes)
		{
			try
			{
				var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
				if (!source.LoadImage(bytes))
				{
					SetStatus(_avatarStatus, "That file is not a supported image.", true);
					return;
				}

				var square = ResizeSquare(source, avatarSize);
				_avatarBase64 = Convert.ToBase64String(square.EncodeToPNG());
				if (_avatarTexture != null) Destroy(_avatarTexture);
				_avatarTexture = square;

				if (_avatarPreview != null)
				{
					_avatarPreview.texture = square;
					_avatarPreview.color = Color.white;
				}
				SetStatus(_avatarStatus, "Icon ready.", false);
			}
			catch (Exception ex)
			{
				SetStatus(_avatarStatus, "Could not use that image: " + ex.Message, true);
			}
		}

		static Texture2D ResizeSquare(Texture2D source, int size)
		{
			int side = Mathf.Min(source.width, source.height);
			int offsetX = (source.width - side) / 2;
			int offsetY = (source.height - side) / 2;

			var result = new Texture2D(size, size, TextureFormat.RGBA32, false);
			var pixels = new Color32[size * size];
			var sourcePixels = source.GetPixels32();

			for (int y = 0; y < size; y++)
			{
				int sy = offsetY + Mathf.Clamp(Mathf.RoundToInt((y + 0.5f) * side / size), 0, side - 1);
				for (int x = 0; x < size; x++)
				{
					int sx = offsetX + Mathf.Clamp(Mathf.RoundToInt((x + 0.5f) * side / size), 0, side - 1);
					pixels[y * size + x] = sourcePixels[sy * source.width + sx];
				}
			}

			result.SetPixels32(pixels);
			result.Apply();
			return result;
		}

		void BuildUi(bool accountsEnabled)
		{
			EnsureEventSystem();

			var canvasGo = new GameObject("WebworksNameEntryCanvas");
			_canvasGo = canvasGo;
			canvasGo.transform.SetParent(transform, false);
			var canvas = canvasGo.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			var scaler = canvasGo.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1280, 800);
			canvasGo.AddComponent<GraphicRaycaster>();

			var bg = MakeUiObject("Backdrop", canvasGo.transform);
			var bgImage = bg.AddComponent<Image>();
			bgImage.color = backdropColor;
			StretchToParent(bg.GetComponent<RectTransform>());

			bool isRegisterMode = accountsEnabled && _mode == Mode.Register;
			float panelWidth = 480;
			float panelHeight = !accountsEnabled ? 280 : (isRegisterMode ? 760 : 660);
			// The Webworks mark sits at the top of the panel when the shim ships with it.
			var logoTex = Resources.Load<Texture2D>("WebworksLogo");
			if (logoTex != null) panelHeight += 72;

			var panel = MakeUiObject("Panel", bg.transform);
			var panelImage = panel.AddComponent<Image>();
			panelImage.color = panelColor;
			SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(panelWidth, panelHeight));

			var accentBar = MakeUiObject("AccentBar", panel.transform);
			var accentImage = accentBar.AddComponent<Image>();
			accentImage.color = accentColor;
			var accentRect = accentBar.GetComponent<RectTransform>();
			accentRect.anchorMin = new Vector2(0, 1);
			accentRect.anchorMax = new Vector2(1, 1);
			accentRect.pivot = new Vector2(0.5f, 1f);
			accentRect.sizeDelta = new Vector2(0, 4);
			accentRect.anchoredPosition = Vector2.zero;

			float y = panelHeight / 2f - 40;
			if (logoTex != null)
			{
				var logoGo = MakeUiObject("Logo", panel.transform);
				SetRect(logoGo.GetComponent<RectTransform>(), new Vector2(0, y - 16), new Vector2(56, 56));
				var logoImage = logoGo.AddComponent<RawImage>();
				logoImage.texture = logoTex;
				logoImage.color = Color.white;
				y -= 72;
			}
			bool hasCachedAccount = accountsEnabled && _mode == Mode.SignIn && !string.IsNullOrEmpty(_cachedToken);
			string titleText = !accountsEnabled ? "WHAT SHOULD WE CALL YOU?" : (isRegisterMode ? "CREATE ACCOUNT" : (hasCachedAccount ? "WELCOME BACK" : "SIGN IN"));

			var title = MakeText(panel.transform, "Title", titleText, 22, TextAnchor.MiddleCenter, textColor, FontStyle.Bold);
			SetRect(title.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(panelWidth - 40, 32));
			y -= 44;

			if (accountsEnabled)
			{
				_userInput = MakeInputField(panel.transform, new Vector2(0, y), new Vector2(panelWidth - 64, 42), "Username", false);
				if (!string.IsNullOrEmpty(_cachedUsername)) _userInput.text = _cachedUsername;
				y -= 54;

				_passInput = MakeInputField(panel.transform, new Vector2(0, y), new Vector2(panelWidth - 64, 42), hasCachedAccount ? "Password (leave blank to continue)" : "Password", true);
				y -= 46;

				if (isRegisterMode)
				{
					_displayNameInput = MakeInputField(panel.transform, new Vector2(0, y), new Vector2(panelWidth - 64, 42), "Display name (optional)", false);
					y -= 58;

					var avatarRow = MakeUiObject("AvatarRow", panel.transform);
					SetRect(avatarRow.GetComponent<RectTransform>(), new Vector2(0, y - 8), new Vector2(panelWidth - 64, 96));

					var avatarFrame = MakeUiObject("AvatarFrame", avatarRow.transform);
					SetRect(avatarFrame.GetComponent<RectTransform>(), new Vector2(-150, 0), new Vector2(88, 88));
					var frameImage = avatarFrame.AddComponent<Image>();
					frameImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.45f);

					var previewGo = MakeUiObject("AvatarPreview", avatarFrame.transform);
					StretchToParent(previewGo.GetComponent<RectTransform>());
					previewGo.GetComponent<RectTransform>().offsetMin = new Vector2(2, 2);
					previewGo.GetComponent<RectTransform>().offsetMax = new Vector2(-2, -2);
					_avatarPreview = previewGo.AddComponent<RawImage>();
					_avatarPreview.color = new Color(1, 1, 1, 0.08f);
					_avatarPreview.texture = null;

					var pickGo = MakeUiObject("PickAvatar", avatarRow.transform);
					SetRect(pickGo.GetComponent<RectTransform>(), new Vector2(55, 18), new Vector2(210, 40));
					var pickImage = pickGo.AddComponent<Image>();
					pickImage.color = new Color(1, 1, 1, 0.05f);
					var pickButton = pickGo.AddComponent<Button>();
					pickButton.onClick.AddListener(PickAvatar);
					var pickLabel = MakeText(pickGo.transform, "Label", "Choose account icon", 14, TextAnchor.MiddleCenter, accentColorBright, FontStyle.Bold);
					StretchToParent(pickLabel.GetComponent<RectTransform>());

					var avatarStatusGo = MakeText(avatarRow.transform, "AvatarStatus", _avatarBase64 != null ? "Icon ready." : "PNG, JPEG or WebP.", 11, TextAnchor.MiddleCenter, mutedTextColor, FontStyle.Normal);
					SetRect(avatarStatusGo.GetComponent<RectTransform>(), new Vector2(55, -12), new Vector2(210, 18));
#if TMP_PRESENT
					_avatarStatus = avatarStatusGo.GetComponent<TextMeshProUGUI>();
#else
					_avatarStatus = avatarStatusGo.GetComponent<Text>();
#endif

					if (_avatarTexture != null)
					{
						_avatarPreview.texture = _avatarTexture;
						_avatarPreview.color = Color.white;
					}

					y -= 104;
				}

				var signInGo = MakeUiObject("SignInButton", panel.transform);
				SetRect(signInGo.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(panelWidth - 64, 44));
				var signInImage = signInGo.AddComponent<Image>();
				signInImage.color = accentColor;
				_signInButton = signInGo.AddComponent<Button>();
				var signInColors = _signInButton.colors;
				signInColors.highlightedColor = accentColorBright;
				signInColors.pressedColor = accentColorBright;
				signInColors.disabledColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.4f);
				_signInButton.colors = signInColors;
				_signInButton.onClick.AddListener(TrySignIn);

				var signInLabelGo = MakeText(signInGo.transform, "Label", isRegisterMode ? "Create Account" : (hasCachedAccount ? "Continue" : "Sign In"), 16, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
				StretchToParent(signInLabelGo.GetComponent<RectTransform>());
#if TMP_PRESENT
				_signInButtonLabel = signInLabelGo.GetComponent<TextMeshProUGUI>();
#else
				_signInButtonLabel = signInLabelGo.GetComponent<Text>();
#endif
				y -= 30;

				var signInStatusGo = MakeText(panel.transform, "SignInStatus", "", 13, TextAnchor.MiddleCenter, mutedTextColor, FontStyle.Normal);
				SetRect(signInStatusGo.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(panelWidth - 40, 20));
#if TMP_PRESENT
				_signInStatus = signInStatusGo.GetComponent<TextMeshProUGUI>();
#else
				_signInStatus = signInStatusGo.GetComponent<Text>();
#endif
				y -= 30;

				var toggleGo = MakeUiObject("ModeToggle", panel.transform);
				SetRect(toggleGo.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(panelWidth - 64, 22));
				var toggleHitArea = toggleGo.AddComponent<Image>();
				toggleHitArea.color = new Color(0, 0, 0, 0);
				var toggleButton = toggleGo.AddComponent<Button>();
				toggleButton.onClick.AddListener(ToggleMode);
				var toggleLabelGo = MakeText(toggleGo.transform, "Label", isRegisterMode ? "Already have an account? Sign In" : "Need an account? Register", 13, TextAnchor.MiddleCenter, accentColorBright, FontStyle.Bold);
				StretchToParent(toggleLabelGo.GetComponent<RectTransform>());
				y -= 34;

				var dividerLine = MakeUiObject("DividerLine", panel.transform);
				var dividerImage = dividerLine.AddComponent<Image>();
				dividerImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.35f);
				SetRect(dividerLine.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(panelWidth - 64, 1));
				var dividerBacking = MakeUiObject("DividerLabelBg", panel.transform);
				var dividerBackingImage = dividerBacking.AddComponent<Image>();
				dividerBackingImage.color = panelColor;
				SetRect(dividerBacking.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(200, 20));
				var dividerLabel = MakeText(panel.transform, "DividerLabel", "OR CONTINUE AS GUEST", 11, TextAnchor.MiddleCenter, mutedTextColor, FontStyle.Bold);
				SetRect(dividerLabel.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(220, 20));
				y -= 34;
			}

			_guestInput = MakeInputField(panel.transform, new Vector2(0, y), new Vector2(panelWidth - 64, 42), guestPlaceholder, false);
			if (!string.IsNullOrEmpty(_cachedGuestName)) _guestInput.text = _cachedGuestName;
			y -= 54;

			var guestButtonGo = MakeUiObject("GuestButton", panel.transform);
			SetRect(guestButtonGo.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(panelWidth - 64, 40));
			var guestButtonImage = guestButtonGo.AddComponent<Image>();
			guestButtonImage.color = accountsEnabled ? new Color(1, 1, 1, 0.04f) : accentColor;
			var guestButton = guestButtonGo.AddComponent<Button>();
			var guestColors = guestButton.colors;
			guestColors.highlightedColor = accountsEnabled ? new Color(1, 1, 1, 0.09f) : accentColorBright;
			guestColors.pressedColor = guestColors.highlightedColor;
			guestButton.colors = guestColors;
			guestButton.onClick.AddListener(TrySubmitGuest);

			var guestLabelGo = MakeText(guestButtonGo.transform, "Label", accountsEnabled ? "Continue as Guest" : "Continue", 15, TextAnchor.MiddleCenter, accountsEnabled ? accentColorBright : Color.white, FontStyle.Bold);
			StretchToParent(guestLabelGo.GetComponent<RectTransform>());
			y -= 28;

			var guestStatusGo = MakeText(panel.transform, "GuestStatus", "", 12, TextAnchor.MiddleCenter, mutedTextColor, FontStyle.Normal);
			SetRect(guestStatusGo.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(panelWidth - 40, 18));
#if TMP_PRESENT
			_guestStatus = guestStatusGo.GetComponent<TextMeshProUGUI>();
#else
			_guestStatus = guestStatusGo.GetComponent<Text>();
#endif

			if (accountsEnabled && _userInput != null) { _userInput.Select(); _userInput.ActivateInputField(); }
			else if (_guestInput != null) { _guestInput.Select(); _guestInput.ActivateInputField(); }
		}

		void SetStatus(
#if TMP_PRESENT
			TextMeshProUGUI label,
#else
			Text label,
#endif
			string message, bool isError)
		{
			if (label == null) return;
			label.text = message;
			label.color = isError ? errorColor : mutedTextColor;
		}

		static GameObject MakeUiObject(string name, Transform parent)
		{
			var go = new GameObject(name, typeof(RectTransform));
			go.transform.SetParent(parent, false);
			return go;
		}

		static GameObject MakeText(Transform parent, string name, string content, float fontSize, TextAnchor alignment, Color color, FontStyle style)
		{
			var go = MakeUiObject(name, parent);
#if TMP_PRESENT
			var text = go.AddComponent<TextMeshProUGUI>();
			text.text = content;
			text.fontSize = fontSize;
			text.alignment = ToTmpAlignment(alignment);
			text.color = color;
			text.fontStyle = style == FontStyle.Bold ? FontStyles.Bold : FontStyles.Normal;
#else
			var text = go.AddComponent<Text>();
			SetDefaultFont(text);
			text.text = content;
			text.fontSize = (int)fontSize;
			text.alignment = alignment;
			text.color = color;
			text.fontStyle = style;
#endif
			return go;
		}

#if TMP_PRESENT
		TMP_InputField MakeInputField(Transform parent, Vector2 anchoredPosition, Vector2 size, string placeholder, bool isPassword)
#else
		InputField MakeInputField(Transform parent, Vector2 anchoredPosition, Vector2 size, string placeholder, bool isPassword)
#endif
		{
			var borderGo = MakeUiObject("Field_" + placeholder, parent);
			SetRect(borderGo.GetComponent<RectTransform>(), anchoredPosition, new Vector2(size.x + 2, size.y + 2));
			var borderImage = borderGo.AddComponent<Image>();
			borderImage.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.4f);

			var fieldGo = MakeUiObject("FieldFill", borderGo.transform);
			StretchToParent(fieldGo.GetComponent<RectTransform>());
			var fieldImage = fieldGo.AddComponent<Image>();
			fieldImage.color = fieldColor;

			var textArea = MakeUiObject("Text", fieldGo.transform);
			var textAreaRect = textArea.GetComponent<RectTransform>();
			StretchToParent(textAreaRect);
			textAreaRect.offsetMin = new Vector2(12, 4);
			textAreaRect.offsetMax = new Vector2(-12, -4);

			var placeholderGo = MakeUiObject("Placeholder", fieldGo.transform);
			var placeholderRect = placeholderGo.GetComponent<RectTransform>();
			StretchToParent(placeholderRect);
			placeholderRect.offsetMin = new Vector2(12, 4);
			placeholderRect.offsetMax = new Vector2(-12, -4);

#if TMP_PRESENT
			var input = fieldGo.AddComponent<TMP_InputField>();
			input.characterLimit = isPassword ? 0 : 64;
			input.lineType = TMP_InputField.LineType.SingleLine;
			if (isPassword) input.contentType = TMP_InputField.ContentType.Password;

			var inputText = textArea.AddComponent<TextMeshProUGUI>();
			inputText.color = textColor;
			inputText.fontSize = 16;
			inputText.alignment = TextAlignmentOptions.MidlineLeft;
			input.textComponent = inputText;

			var placeholderText = placeholderGo.AddComponent<TextMeshProUGUI>();
			placeholderText.text = placeholder;
			placeholderText.fontSize = 16;
			placeholderText.color = mutedTextColor;
			placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
			input.placeholder = placeholderText;
#else
			var input = fieldGo.AddComponent<InputField>();
			input.characterLimit = isPassword ? 0 : 64;
			input.lineType = InputField.LineType.SingleLine;
			if (isPassword) input.contentType = InputField.ContentType.Password;

			var inputText = textArea.AddComponent<Text>();
			SetDefaultFont(inputText);
			inputText.color = textColor;
			inputText.alignment = TextAnchor.MiddleLeft;
			input.textComponent = inputText;

			var placeholderText = placeholderGo.AddComponent<Text>();
			SetDefaultFont(placeholderText);
			placeholderText.text = placeholder;
			placeholderText.color = mutedTextColor;
			placeholderText.fontStyle = FontStyle.Italic;
			input.placeholder = placeholderText;
#endif
			return input;
		}

#if TMP_PRESENT
		static TextAlignmentOptions ToTmpAlignment(TextAnchor anchor)
		{
			switch (anchor)
			{
				case TextAnchor.MiddleLeft: return TextAlignmentOptions.MidlineLeft;
				case TextAnchor.MiddleRight: return TextAlignmentOptions.MidlineRight;
				default: return TextAlignmentOptions.Center;
			}
		}
#else
		static Font _cachedFont;

		static void SetDefaultFont(Text text)
		{
			if (_cachedFont == null)
			{
				_cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
				if (_cachedFont == null) _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
			}
			text.font = _cachedFont;
		}
#endif

		static void StretchToParent(RectTransform rt)
		{
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;
		}

		static void SetRect(RectTransform rt, Vector2 anchoredPosition, Vector2 size)
		{
			rt.anchorMin = new Vector2(0.5f, 0.5f);
			rt.anchorMax = new Vector2(0.5f, 0.5f);
			rt.anchoredPosition = anchoredPosition;
			rt.sizeDelta = size;
		}
	}

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
	internal static class NativeImagePicker
	{
		[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
		struct OpenFileName
		{
			public int lStructSize;
			public IntPtr hwndOwner;
			public IntPtr hInstance;
			public string lpstrFilter;
			public string lpstrCustomFilter;
			public int nMaxCustFilter;
			public int nFilterIndex;
			public string lpstrFile;
			public int nMaxFile;
			public string lpstrFileTitle;
			public int nMaxFileTitle;
			public string lpstrInitialDir;
			public string lpstrTitle;
			public int Flags;
			public short nFileOffset;
			public short nFileExtension;
			public string lpstrDefExt;
			public IntPtr lCustData;
			public IntPtr lpfnHook;
			public string lpTemplateName;
			public IntPtr pvReserved;
			public int dwReserved;
			public int flagsEx;
		}

		[System.Runtime.InteropServices.DllImport("comdlg32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
		static extern bool GetOpenFileName(ref OpenFileName ofn);

		public static string Open(string title, string filter)
		{
			var ofn = new OpenFileName();
			ofn.lStructSize = System.Runtime.InteropServices.Marshal.SizeOf(ofn);
			ofn.lpstrFilter = filter;
			ofn.lpstrFile = new string(new char[1024]);
			ofn.nMaxFile = 1024;
			ofn.lpstrTitle = title;
			ofn.Flags = 0x00080000 | 0x00001000 | 0x00000800;
			return GetOpenFileName(ref ofn) ? ofn.lpstrFile.TrimEnd('\0') : null;
		}
	}
#endif
}
