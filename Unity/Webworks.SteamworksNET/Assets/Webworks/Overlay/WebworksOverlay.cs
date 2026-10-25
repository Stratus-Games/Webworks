using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using Webworks.SNetInternal;

namespace Webworks.Overlay
{
	public enum WebworksOverlayScreen
	{
		None,
		Friends,
		Requests,
		Achievements,
		Invites,
		Controller,
		Settings,
		User,
		Info,
		Link,
	}

	/// <summary>
	/// The Webworks equivalent of the Steam overlay: a panel drawn over the running game that
	/// carries the friend list, friend requests, achievements, lobby invites, controller state
	/// and settings.
	///
	/// It draws with IMGUI on purpose. An IMGUI panel is always painted after the game's own
	/// canvases, so the overlay is above the game UI no matter what sorting order a project uses
	/// and without editing that project's scenes. Dropping one of the shipped prefabs into a
	/// scene overrides the defaults; otherwise <see cref="Install"/> creates an instance on the
	/// first scene load, so `SteamFriends.ActivateGameOverlay("friends")` has somewhere to go.
	///
	/// Nothing here talks to Steam: every call is the Webworks shim talking to the node server
	/// that the game is already connected to.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed partial class WebworksOverlay : MonoBehaviour
	{
		[Header("Overlay")]
		[SerializeField] WebworksOverlayScreen initialScreen = WebworksOverlayScreen.Friends;
		[SerializeField] bool openOnStart;
		[SerializeField] bool hotkeyEnabled = true;
		[SerializeField] bool pauseGameWhileOpen = true;
		[Tooltip("IMGUI depth. Lower numbers draw on top of higher ones.")]
		[SerializeField] int guiDepth = -100;

		[Header("Look")]
		[SerializeField] Color panelColor = new Color(0.07f, 0.055f, 0.06f, 0.985f);
		[SerializeField] Color headerColor = new Color(0.10f, 0.075f, 0.08f, 1f);
		[SerializeField] Color accentColor = new Color(0.80f, 0.08f, 0.14f, 1f);
		[SerializeField] Color accentBrightColor = new Color(0.92f, 0.16f, 0.20f, 1f);
		[SerializeField] Color textColor = new Color(0.95f, 0.95f, 0.95f);
		[SerializeField] Color mutedColor = new Color(0.55f, 0.50f, 0.50f);
		[SerializeField] Color onlineColor = new Color(0.45f, 0.80f, 0.45f);
		[SerializeField] Color errorColor = new Color(1f, 0.35f, 0.35f);

		// --- pages ------------------------------------------------------------

		enum Tab { Friends, Requests, Achievements, Invites, Controller, Settings }

		static readonly string[] TabNames = { "FRIENDS", "REQUESTS", "ACHIEVEMENTS", "INVITES", "CONTROLLER", "SETTINGS" };

		WebworksOverlayScreen _screen = WebworksOverlayScreen.None;
		Tab _tab = Tab.Friends;
		string _argument;
		int _selection;
		Vector2 _scroll;

		bool _previousCursorVisible;
		CursorLockMode _previousLockMode;
		float _previousTimeScale = 1f;

		// --- lifetime ---------------------------------------------------------

		static WebworksOverlay _instance;

		public static WebworksOverlay Instance { get { return _instance; } }

		public static bool IsOpen { get { return _instance != null && _instance._screen != WebworksOverlayScreen.None; } }

		public static WebworksOverlayScreen CurrentScreen
		{
			get { return _instance == null ? WebworksOverlayScreen.None : _instance._screen; }
		}

		/// <summary>Creates the overlay on the first scene load so overlay calls always have a target.</summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static void Install()
		{
			if (_instance != null) return;
			if (FindObjectOfType<WebworksOverlay>() != null) return;

			// A project that ships its own prefab can put it in Resources to get its look.
			var prefab = Resources.Load<GameObject>("WebworksOverlay");
			var go = prefab != null ? Instantiate(prefab) : new GameObject("WebworksOverlay");
			go.name = "WebworksOverlay";
			DontDestroyOnLoad(go);
			var overlay = go.GetComponent<WebworksOverlay>();
			if (overlay == null) overlay = go.AddComponent<WebworksOverlay>();
			_instance = overlay;
		}

		void Awake()
		{
			if (_instance != null && _instance != this)
			{
				Destroy(gameObject);
				return;
			}
			_instance = this;
			DontDestroyOnLoad(gameObject);
			if (openOnStart) Show(initialScreen, null);
		}

		void OnDestroy()
		{
			if (_instance == this) _instance = null;
		}

		// --- open / close -----------------------------------------------------

		public static WebworksOverlay Open(WebworksOverlayScreen screen, string argument = null)
		{
			var overlay = Ensure();
			overlay.Show(screen, argument);
			return overlay;
		}

		public static void OpenFriends()
		{
			Open(WebworksOverlayScreen.Friends);
		}

		public static void Toggle()
		{
			var overlay = Ensure();
			if (overlay._screen == WebworksOverlayScreen.None) overlay.Show(WebworksOverlayScreen.Friends, null);
			else overlay.Close();
		}

		public static void CloseOpenOverlay()
		{
			if (_instance != null) _instance.Close();
		}

		static WebworksOverlay Ensure()
		{
			if (_instance != null) return _instance;
			var existing = FindObjectOfType<WebworksOverlay>();
			if (existing != null) return existing;
			var go = new GameObject("WebworksOverlay");
			DontDestroyOnLoad(go);
			var overlay = go.AddComponent<WebworksOverlay>();
			_instance = overlay;
			return overlay;
		}

		public void Show(WebworksOverlayScreen screen, string argument)
		{
			if (screen == WebworksOverlayScreen.None)
			{
				Close();
				return;
			}

			bool wasClosed = _screen == WebworksOverlayScreen.None;
			_screen = screen;
			_argument = argument;
			_selection = 0;
			_scroll = Vector2.zero;
			_tab = TabFor(screen);

			if (wasClosed)
			{
				_previousCursorVisible = Cursor.visible;
				_previousLockMode = Cursor.lockState;
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
				if (pauseGameWhileOpen)
				{
					_previousTimeScale = Time.timeScale;
					Time.timeScale = 0f;
				}
				CallbackBus<GameOverlayActivated_t>.Fire(new GameOverlayActivated_t { m_bActive = 1 });
			}

			OnScreenShown(screen, argument);
		}

		public void Close()
		{
			if (_screen == WebworksOverlayScreen.None) return;
			_screen = WebworksOverlayScreen.None;
			_argument = null;
			Cursor.visible = _previousCursorVisible;
			Cursor.lockState = _previousLockMode;
			if (pauseGameWhileOpen) Time.timeScale = _previousTimeScale;
			CallbackBus<GameOverlayActivated_t>.Fire(new GameOverlayActivated_t { m_bActive = 0 });
		}

		static Tab TabFor(WebworksOverlayScreen screen)
		{
			switch (screen)
			{
				case WebworksOverlayScreen.Requests: return Tab.Requests;
				case WebworksOverlayScreen.Achievements: return Tab.Achievements;
				case WebworksOverlayScreen.Invites: return Tab.Invites;
				case WebworksOverlayScreen.Controller: return Tab.Controller;
				case WebworksOverlayScreen.Settings: return Tab.Settings;
				default: return Tab.Friends;
			}
		}

		static WebworksOverlayScreen ScreenFor(Tab tab)
		{
			switch (tab)
			{
				case Tab.Requests: return WebworksOverlayScreen.Requests;
				case Tab.Achievements: return WebworksOverlayScreen.Achievements;
				case Tab.Invites: return WebworksOverlayScreen.Invites;
				case Tab.Controller: return WebworksOverlayScreen.Controller;
				case Tab.Settings: return WebworksOverlayScreen.Settings;
				default: return WebworksOverlayScreen.Friends;
			}
		}

		void SwitchTab(int delta)
		{
			int count = TabNames.Length;
			int next = ((int)_tab + delta + count) % count;
			_tab = (Tab)next;
			_screen = ScreenFor(_tab);
			_argument = null;
			_selection = 0;
			_scroll = Vector2.zero;
			OnScreenShown(_screen, null);
		}

		// --- frame ------------------------------------------------------------

		void Update()
		{
			if (hotkeyEnabled && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Tab)) Toggle();
			else if (Input.GetKey(KeyCode.JoystickButton6) && Input.GetKeyDown(KeyCode.JoystickButton7)) Toggle();

			if (_screen == WebworksOverlayScreen.None) return;

			var action = WebworksOverlayInput.Poll();
			if (action == WebworksOverlayInput.Action.None) return;

			switch (action)
			{
				case WebworksOverlayInput.Action.Cancel:
					if (_screen == WebworksOverlayScreen.User || _screen == WebworksOverlayScreen.Info || _screen == WebworksOverlayScreen.Link)
					{
						_screen = ScreenFor(_tab);
						_selection = 0;
					}
					else Close();
					break;
				case WebworksOverlayInput.Action.PreviousTab:
					SwitchTab(-1);
					break;
				case WebworksOverlayInput.Action.NextTab:
					SwitchTab(1);
					break;
				case WebworksOverlayInput.Action.Up:
					MoveSelection(-1);
					break;
				case WebworksOverlayInput.Action.Down:
					MoveSelection(1);
					break;
				case WebworksOverlayInput.Action.Submit:
					ActivateSelection();
					break;
			}
		}

		void MoveSelection(int delta)
		{
			int count = RowCount();
			if (count <= 0)
			{
				_selection = 0;
				return;
			}
			_selection = Mathf.Clamp(_selection + delta, 0, count - 1);
			float rowHeight = 26f;
			float visible = 340f;
			float top = _selection * rowHeight;
			if (top < _scroll.y) _scroll.y = top;
			else if (top + rowHeight > _scroll.y + visible) _scroll.y = top + rowHeight - visible;
		}

		// --- painting ---------------------------------------------------------

		static GUIStyle _titleStyle, _labelStyle, _mutedStyle, _smallStyle, _rowStyle, _rowSelectedStyle;
		static GUIStyle _buttonStyle, _tabStyle, _fieldStyle, _badgeStyle;
		static bool _stylesBuilt;

		static void BuildStyles()
		{
			if (_stylesBuilt) return;
			_stylesBuilt = true;

			_titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
			_labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft, wordWrap = true };
			_mutedStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleLeft, wordWrap = true };
			_smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleLeft, wordWrap = true };
			_rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
			_rowSelectedStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
			_buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, alignment = TextAnchor.MiddleCenter, fixedHeight = 24 };
			_tabStyle = new GUIStyle(GUI.skin.button) { fontSize = 11, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fixedHeight = 26 };
			_fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 13, fixedHeight = 24 };
			_badgeStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleRight };
		}

		void OnGUI()
		{
			if (_screen == WebworksOverlayScreen.None) return;

			BuildStyles();
			GUI.depth = guiDepth;

			var full = new Rect(0, 0, Screen.width, Screen.height);
			Fill(full, new Color(0f, 0f, 0f, 0.62f));

			float width = Mathf.Min(900f, Screen.width - 48f);
			float height = Mathf.Min(580f, Screen.height - 48f);
			var window = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

			Fill(window, new Color(0f, 0f, 0f, 0.35f));
			Fill(new Rect(window.x + 2, window.y + 2, window.width - 4, window.height - 4), panelColor);
			Fill(new Rect(window.x, window.y, window.width, 4), accentColor);

			float pad = 16f;
			var header = new Rect(window.x + pad, window.y + 14, window.width - pad * 2, 30);
			var previous = GUI.color;
			GUI.color = textColor;
			GUI.Label(new Rect(header.x, header.y, 190, 26), "WEBWORKS OVERLAY", _titleStyle);
			GUI.color = previous;

			DrawTabs(new Rect(window.x + pad + 195, window.y + 14, window.width - pad * 2 - 195 - 74, 26));
			if (GUI.Button(new Rect(window.x + window.width - pad - 66, window.y + 15, 66, 24), "Close", _buttonStyle)) Close();

			var content = new Rect(window.x + pad, window.y + 52, window.width - pad * 2, window.height - 52 - 54);
			GUILayout.BeginArea(content);
			switch (_screen)
			{
				case WebworksOverlayScreen.Friends: DrawFriends(); break;
				case WebworksOverlayScreen.Requests: DrawRequests(); break;
				case WebworksOverlayScreen.Achievements: DrawAchievements(); break;
				case WebworksOverlayScreen.Invites: DrawInvites(); break;
				case WebworksOverlayScreen.Controller: DrawController(); break;
				case WebworksOverlayScreen.Settings: DrawSettings(); break;
				case WebworksOverlayScreen.User: DrawUser(); break;
				case WebworksOverlayScreen.Info: DrawInfo(); break;
				case WebworksOverlayScreen.Link: DrawLink(); break;
			}
			GUILayout.EndArea();

			var footer = new Rect(window.x + pad, window.y + window.height - 40, window.width - pad * 2, 28);
			var footerPrev = GUI.color;
			GUI.color = mutedColor;
			GUI.Label(new Rect(footer.x, footer.y, footer.width, 20), WebworksOverlayInput.Glyphs() + "   \u00b7   Shift+Tab toggles", _smallStyle);
			GUI.color = footerPrev;
		}

		void DrawTabs(Rect area)
		{
			float gap = 2f;
			float tabWidth = Mathf.Max(52f, (area.width - gap * (TabNames.Length - 1)) / TabNames.Length);
			for (int i = 0; i < TabNames.Length; i++)
			{
				var rect = new Rect(area.x + i * (tabWidth + gap), area.y, tabWidth, area.height);
				bool active = (Tab)i == _tab
					&& _screen != WebworksOverlayScreen.User
					&& _screen != WebworksOverlayScreen.Info
					&& _screen != WebworksOverlayScreen.Link;

				var previousColor = GUI.color;
				GUI.color = active ? accentColor : new Color(1f, 1f, 1f, 0.10f);
				if (GUI.Button(rect, TabNames[i], _tabStyle))
				{
					_tab = (Tab)i;
					_screen = ScreenFor(_tab);
					_argument = null;
					_selection = 0;
					_scroll = Vector2.zero;
					OnScreenShown(_screen, null);
				}
				GUI.color = previousColor;
			}
		}

		static void Fill(Rect rect, Color color)
		{
			var previous = GUI.color;
			GUI.color = color;
			GUI.DrawTexture(rect, Texture2D.whiteTexture);
			GUI.color = previous;
		}

		void Section(string title)
		{
			GUILayout.Space(2);
			var previous = GUI.color;
			GUI.color = mutedColor;
			GUILayout.Label(title, _smallStyle);
			GUI.color = previous;
		}

		void Empty(string text)
		{
			var previous = GUI.color;
			GUI.color = mutedColor;
			GUILayout.Label(text, _mutedStyle);
			GUI.color = previous;
		}

		void Row(string text, string detail, int index)
		{
			bool selected = index == _selection;
			var style = selected ? _rowSelectedStyle : _rowStyle;
			var previous = GUI.color;
			GUI.color = selected ? accentBrightColor : textColor;
			GUILayout.BeginHorizontal(GUILayout.Height(24));
			GUILayout.Label(text, style, GUILayout.MinWidth(220));
			GUILayout.FlexibleSpace();
			GUI.color = previous;
			var muted = GUI.color;
			GUI.color = mutedColor;
			GUILayout.Label(detail, _badgeStyle);
			GUI.color = muted;
			GUILayout.EndHorizontal();
		}

		// --- settings ---------------------------------------------------------

		string _personaStatus;

		void DrawSettings()
		{
			Section("PROFILE");
			GUILayout.Label("Signed in as " + SafeName(), _labelStyle);
			GUILayout.Label(WebworksConfig.ServerUrl + "   \u00b7   app " + WebworksConfig.AppId, _mutedStyle);
			GUILayout.Space(8);

			Section("STATUS");
			GUILayout.BeginHorizontal();
			PersonaButton("Online", EPersonaState.k_EPersonaStateOnline);
			PersonaButton("Away", EPersonaState.k_EPersonaStateAway);
			PersonaButton("Busy", EPersonaState.k_EPersonaStateBusy);
			PersonaButton("Looking to play", EPersonaState.k_EPersonaStateLookingToPlay);
			PersonaButton("Invisible", EPersonaState.k_EPersonaStateInvisible);
			GUILayout.EndHorizontal();
			if (!string.IsNullOrEmpty(_personaStatus)) Empty(_personaStatus);

			GUILayout.Space(10);
			Section("ACCOUNT");
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Open the account page", _buttonStyle, GUILayout.Width(180))) OpenLink(WebworksConfig.ServerUrl + "/account");
			if (GUILayout.Button("Copy server address", _buttonStyle, GUILayout.Width(170)))
			{
				GUIUtility.systemCopyBuffer = WebworksConfig.ServerUrl;
				_personaStatus = "Server address copied.";
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(10);
			Section("OVERLAY");
			GUILayout.Label("Shift+Tab or Start+Back opens and closes this panel. While it is open the game is paused and the mouse is released. A controller can drive it on its own.", _mutedStyle);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Achievements", _buttonStyle, GUILayout.Width(130))) { _tab = Tab.Achievements; _screen = WebworksOverlayScreen.Achievements; }
			if (GUILayout.Button("Controller", _buttonStyle, GUILayout.Width(130))) { _tab = Tab.Controller; _screen = WebworksOverlayScreen.Controller; }
			GUILayout.EndHorizontal();
		}

		void PersonaButton(string label, EPersonaState state)
		{
			if (!GUILayout.Button(label, _buttonStyle, GUILayout.Width(112))) return;
			bool ok = Steamworks.SteamFriends.SetPersonaState(state);
			_personaStatus = ok ? "Status set to " + label + "." : "Could not change status.";
		}

		static string SafeName()
		{
			try
			{
				string name = Steamworks.SteamFriends.GetPersonaName();
				return string.IsNullOrEmpty(name) ? "a guest player" : name;
			}
			catch (Exception)
			{
				return "a guest player";
			}
		}

		// --- controller -------------------------------------------------------

		void DrawController()
		{
			Section("CONNECTED CONTROLLERS");
			string[] names;
			try { names = Input.GetJoystickNames(); }
			catch (Exception) { names = new string[0]; }

			int connected = 0;
			for (int i = 0; i < names.Length; i++)
			{
				if (string.IsNullOrEmpty(names[i])) continue;
				connected++;
				Row("Pad " + (i + 1) + "  " + names[i], "connected", -1);
			}
			if (connected == 0) Empty("No controller detected. A keyboard and mouse still drive the overlay.");

			GUILayout.Space(8);
			Section("LIVE INPUT");
			float horizontal = AxisOf("Horizontal");
			float vertical = AxisOf("Vertical");
			GUILayout.Label("Left stick / arrows    x " + horizontal.ToString("0.00") + "   y " + vertical.ToString("0.00"), _mutedStyle);
			GUILayout.Label("Buttons: " + HeldButtons(), _mutedStyle);

			GUILayout.Space(8);
			Section("STEAM INPUT");
			GUILayout.Label("Steam's controller configurator is native-only, so the shim exposes the parts that make sense in a browser: the overlay is fully navigable with a pad, and the on-screen keyboard can be requested from the game or from here.", _mutedStyle);
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Open on-screen keyboard", _buttonStyle, GUILayout.Width(200)))
			{
				Steamworks.SteamUtils.ShowGamepadTextInput(
					EGamepadTextInputMode.k_EGamepadTextInputModeNormal,
					EGamepadTextInputLineMode.k_EGamepadTextInputLineModeSingleLine,
					"Webworks on-screen keyboard", 64, "");
			}
			if (GUILayout.Button("Copy glyph legend", _buttonStyle, GUILayout.Width(160)))
				GUIUtility.systemCopyBuffer = WebworksOverlayInput.Glyphs();
			GUILayout.EndHorizontal();
		}

		static float AxisOf(string axis)
		{
			try { return Input.GetAxisRaw(axis); }
			catch (Exception) { return 0f; }
		}

		static string HeldButtons()
		{
			var held = new List<string>();
			for (int i = 0; i < 10; i++)
			{
				KeyCode key = KeyCode.None;
				switch (i)
				{
					case 0: key = KeyCode.JoystickButton0; break;
					case 1: key = KeyCode.JoystickButton1; break;
					case 2: key = KeyCode.JoystickButton2; break;
					case 3: key = KeyCode.JoystickButton3; break;
					case 4: key = KeyCode.JoystickButton4; break;
					case 5: key = KeyCode.JoystickButton5; break;
					case 6: key = KeyCode.JoystickButton6; break;
					case 7: key = KeyCode.JoystickButton7; break;
					case 8: key = KeyCode.JoystickButton8; break;
					case 9: key = KeyCode.JoystickButton9; break;
				}
				bool down;
				try { down = Input.GetKey(key); }
				catch (Exception) { down = false; }
				if (down) held.Add(GlyphFor(i));
			}
			return held.Count == 0 ? "none" : string.Join(" ", held.ToArray());
		}

		static string GlyphFor(int index)
		{
			switch (index)
			{
				case 0: return "A";
				case 1: return "B";
				case 2: return "X";
				case 3: return "Y";
				case 4: return "LB";
				case 5: return "RB";
				case 6: return "Back";
				case 7: return "Start";
				case 8: return "LS";
				case 9: return "RS";
				default: return index.ToString();
			}
		}

		// --- simple pages -----------------------------------------------------

		void DrawInfo()
		{
			Section(string.IsNullOrEmpty(_infoTitle) ? "NOTICE" : _infoTitle.ToUpperInvariant());
			GUILayout.Label(_infoBody ?? "", _labelStyle);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("OK", _buttonStyle, GUILayout.Width(90)))
			{
				_screen = ScreenFor(_tab);
				_selection = 0;
			}
		}

		void DrawLink()
		{
			Section("OPEN LINK");
			string url = _argument ?? "";
			GUILayout.Label(url, _labelStyle);
			GUILayout.Label("The overlay can show the address, but the page opens in the system browser: a WebGL build has no in-game browser.", _mutedStyle);
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Open in browser", _buttonStyle, GUILayout.Width(140))) OpenLink(url);
			if (GUILayout.Button("Copy", _buttonStyle, GUILayout.Width(70))) GUIUtility.systemCopyBuffer = url;
			if (GUILayout.Button("Back", _buttonStyle, GUILayout.Width(70)))
			{
				_screen = ScreenFor(_tab);
				_selection = 0;
			}
			GUILayout.EndHorizontal();
		}

		void OpenLink(string url)
		{
			if (string.IsNullOrEmpty(url)) return;
			try { Application.OpenURL(url); }
			catch (Exception ex) { Debug.LogWarning("[Webworks] could not open " + url + ": " + ex.Message); }
		}
	}
}
