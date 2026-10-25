using System;
using UnityEngine;

namespace Webworks.Overlay
{
	/// <summary>
	/// Input for the overlay, from a keyboard or a controller. Steam's overlay is usable with a
	/// gamepad alone, so the Webworks overlay navigates with the d-pad/stick, A/B, and the
	/// bumpers, and shows those glyphs in its footer. Text fields still take typing normally.
	/// </summary>
	internal static class WebworksOverlayInput
	{
		public enum Action
		{
			None,
			Up,
			Down,
			Left,
			Right,
			Submit,
			Cancel,
			PreviousTab,
			NextTab,
		}

		const float Deadzone = 0.55f;
		const float RepeatDelay = 0.32f;
		const float FirstRepeatDelay = 0.42f;

		static float _nextMoveAt;
		static int _heldDirection;

		/// <summary>Polled once per frame by the overlay while it is open.</summary>
		public static Action Poll()
		{
			if (Pressed(KeyCode.Escape) || PadPressed(1)) return Action.Cancel;
			if (Pressed(KeyCode.Return) || Pressed(KeyCode.KeypadEnter) || PadPressed(0)) return Action.Submit;

			// Bumpers switch tabs; brackets do the same on a keyboard without stealing the
			// arrows from text fields.
			if (PadPressed(4) || Pressed(KeyCode.LeftBracket)) return Action.PreviousTab;
			if (PadPressed(5) || Pressed(KeyCode.RightBracket)) return Action.NextTab;

			int direction = 0;
			if (Pressed(KeyCode.UpArrow)) direction = 1;
			else if (Pressed(KeyCode.DownArrow)) direction = -1;
			else if (Pressed(KeyCode.LeftArrow)) direction = -3;
			else if (Pressed(KeyCode.RightArrow)) direction = 3;
			else
			{
				float horizontal = Axis("Horizontal");
				float vertical = Axis("Vertical");
				if (Math.Abs(horizontal) > Math.Abs(vertical))
				{
					if (horizontal > Deadzone) direction = 3;
					else if (horizontal < -Deadzone) direction = -3;
				}
				else
				{
					if (vertical > Deadzone) direction = 1;
					else if (vertical < -Deadzone) direction = -1;
				}
			}

			if (direction == 0)
			{
				_heldDirection = 0;
				return Action.None;
			}

			float now = Time.realtimeSinceStartup;
			if (direction != _heldDirection)
			{
				_heldDirection = direction;
				_nextMoveAt = now + FirstRepeatDelay;
				return ToAction(direction);
			}
			if (now < _nextMoveAt) return Action.None;
			_nextMoveAt = now + RepeatDelay;
			return ToAction(direction);
		}

		static Action ToAction(int direction)
		{
			switch (direction)
			{
				case 1: return Action.Up;
				case -1: return Action.Down;
				case 3: return Action.Right;
				case -3: return Action.Left;
				default: return Action.None;
			}
		}

		static bool Pressed(KeyCode key)
		{
			try { return Input.GetKeyDown(key); }
			catch (Exception) { return false; }
		}

		// JoystickButton0 is A on an Xbox pad (Cross on PlayStation), 1 is B/Circle,
		// 4 and 5 are the bumpers.
		static bool PadPressed(int index)
		{
			switch (index)
			{
				case 0: return Pressed(KeyCode.JoystickButton0);
				case 1: return Pressed(KeyCode.JoystickButton1);
				case 4: return Pressed(KeyCode.JoystickButton4);
				case 5: return Pressed(KeyCode.JoystickButton5);
				default: return false;
			}
		}

		static float Axis(string name)
		{
			try { return Input.GetAxisRaw(name); }
			catch (Exception) { return 0f; }
		}

		/// <summary>Glyph names for the footer, so a controller-only player knows the controls.</summary>
		public static string Glyphs()
		{
			return ControllerConnected()
				? "A select \u00b7 B back \u00b7 LB/RB tabs \u00b7 stick moves"
				: "Enter select \u00b7 Esc back \u00b7 [ ] tabs \u00b7 arrows move";
		}

		public static bool ControllerConnected()
		{
			try
			{
				foreach (var name in Input.GetJoystickNames())
					if (!string.IsNullOrEmpty(name)) return true;
			}
			catch (Exception) { }
			return false;
		}
	}
}
