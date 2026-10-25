using System;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	internal class WebworksGamepadKeyboard : MonoBehaviour
	{
		static WebworksGamepadKeyboard _instance;

		static WebworksGamepadKeyboard Instance
		{
			get
			{
				if (_instance == null)
				{
					var go = new GameObject("WebworksGamepadKeyboard") { hideFlags = HideFlags.HideAndDontSave };
					UnityEngine.Object.DontDestroyOnLoad(go);
					_instance = go.AddComponent<WebworksGamepadKeyboard>();
				}
				return _instance;
			}
		}

		bool _visible;
		bool _floating;
		bool _password;
		bool _multiline;
		string _description = "";
		string _text = "";
		uint _charMax = 999;
		string _controlName = "WebworksGamepadKeyboardField";
		bool _focusPending;

		public static void Show(EGamepadTextInputMode mode, EGamepadTextInputLineMode lineMode, string description, uint charMax, string existingText, bool floating)
		{
			var kb = Instance;
			kb._visible = true;
			kb._floating = floating;
			kb._password = mode == EGamepadTextInputMode.k_EGamepadTextInputModePassword;
			kb._multiline = lineMode == EGamepadTextInputLineMode.k_EGamepadTextInputLineModeMultipleLines;
			kb._description = description ?? "";
			kb._text = existingText ?? "";
			kb._charMax = charMax == 0 ? 999 : charMax;
			kb._focusPending = true;
		}

		public static void Dismiss()
		{
			if (_instance != null) _instance.Close(false);
		}

		void Close(bool submitted)
		{
			_visible = false;
			var text = _text;
			GUI.FocusControl(null);

			if (_floating)
			{
				CallbackBus<FloatingGamepadTextInputDismissed_t>.Fire(new FloatingGamepadTextInputDismissed_t());
			}
			else
			{
				SteamUtils.SetLastGamepadText(submitted ? text : "");
				CallbackBus<GamepadTextInputDismissed_t>.Fire(new GamepadTextInputDismissed_t { m_bSubmitted = submitted, m_unSubmittedText = (uint)(submitted ? text.Length : 0) });
			}
		}

		void OnGUI()
		{
			if (!_visible) return;

			var e = Event.current;
			if (e.type == EventType.KeyDown && !_multiline && e.keyCode == KeyCode.Return) { Close(true); return; }
			if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { Close(false); return; }

			float w = Mathf.Min(520, Screen.width - 40);
			float h = _multiline ? 160 : (_floating ? 70 : 110);
			float x = (Screen.width - w) * 0.5f;
			float y = _floating ? Screen.height - h - 40 : (Screen.height - h) * 0.5f;
			var box = new Rect(x, y, w, h);

			if (!_floating)
			{
				var old = GUI.color;
				GUI.color = new Color(0, 0, 0, 0.6f);
				GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
				GUI.color = old;
			}

			GUI.Box(box, GUIContent.none);
			float pad = 12;
			var inner = new Rect(box.x + pad, box.y + pad, box.width - pad * 2, box.height - pad * 2);

			if (!string.IsNullOrEmpty(_description))
			{
				GUI.Label(new Rect(inner.x, inner.y, inner.width, 20), _description);
				inner.y += 24; inner.height -= 24;
			}

			GUI.SetNextControlName(_controlName);
			float fieldH = _multiline ? inner.height - 36 : 24;
			string next;
			if (_password) next = GUI.PasswordField(new Rect(inner.x, inner.y, inner.width, fieldH), _text, '*', (int)_charMax);
			else if (_multiline) next = GUI.TextArea(new Rect(inner.x, inner.y, inner.width, fieldH), _text, (int)_charMax);
			else next = GUI.TextField(new Rect(inner.x, inner.y, inner.width, fieldH), _text, (int)_charMax);
			_text = next ?? "";

			if (_focusPending) { GUI.FocusControl(_controlName); _focusPending = false; }

			var btnY = inner.y + fieldH + 8;
			if (GUI.Button(new Rect(inner.x, btnY, inner.width * 0.5f - 4, 24), "OK (Enter)")) Close(true);
			if (GUI.Button(new Rect(inner.x + inner.width * 0.5f + 4, btnY, inner.width * 0.5f - 4, 24), "Cancel (Esc)")) Close(false);
		}
	}
}
