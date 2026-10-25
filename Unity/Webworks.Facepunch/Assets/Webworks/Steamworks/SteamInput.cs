using System;
using System.Collections.Generic;
using UnityEngine;
using Webworks.Internal;

namespace Steamworks
{
	public struct DigitalState { public bool Pressed; public bool Active; }
	public struct AnalogState { public InputSourceMode EMode; public float X; public float Y; public bool Active; }
	public struct MotionState { public float RotQuatX, RotQuatY, RotQuatZ, RotQuatW, PosAccelX, PosAccelY, PosAccelZ, RotVelX, RotVelY, RotVelZ; }

	public enum ControllerHapticLocation { Left = 1, Right = 2, Both = 3 }

	public struct Controller : IEquatable<Controller>
	{
		public ulong Id;
		internal Controller(ulong id) { Id = id; }

		public InputType InputType { get { return SteamInput.InputTypeFor(Id); } }
		public string ActionSet { set { SteamInput.SetActionSet(Id, value); } }

		public void ActivateLayer(string layer) { SteamInput.ActivateLayer(Id, layer); }
		public void DeactivateLayer(string layer) { SteamInput.DeactivateLayer(Id, layer); }
		public void ClearLayers() { SteamInput.ClearLayers(Id); }

		public DigitalState GetDigitalState(string action) { return SteamInput.GetDigitalState(action); }
		public AnalogState GetAnalogState(string action) { return SteamInput.GetAnalogState(action); }
		public MotionState GetMotionData() { return default(MotionState); }

		public void TriggerVibration(ushort leftSpeed, ushort rightSpeed) { }
		public void TriggerVibrationExtended(ushort l, ushort r, ushort lt, ushort rt) { }
		public void TriggerSimpleHapticEvent(ControllerHapticLocation location, byte intensity, sbyte gainDb, byte otherIntensity = 0, sbyte otherGainDb = 0) { }
		public void StopVibration() { }
		public void SetLEDColor(byte r, byte g, byte b) { }
		public void RestoreLEDColor() { }

		public bool ShowBindingPanel() { WebworksOverlay.ShowControllerBinding(); return true; }

		public override string ToString() { return InputType + "." + Id; }
		public override int GetHashCode() { return Id.GetHashCode(); }
		public override bool Equals(object o) { return o is Controller && ((Controller)o).Id == Id; }
		public bool Equals(Controller other) { return other.Id == Id; }
		public static bool operator ==(Controller a, Controller b) { return a.Id == b.Id; }
		public static bool operator !=(Controller a, Controller b) { return a.Id != b.Id; }
	}

	public static class SteamInput
	{
		const ulong KeyboardHandle = 1;

		public static void RunFrame() { }

		public static IEnumerable<Controller> Controllers
		{
			get
			{
				yield return new Controller(KeyboardHandle);
				var names = Input.GetJoystickNames();
				for (int i = 0; i < names.Length; i++)
				{
					if (!string.IsNullOrEmpty(names[i])) yield return new Controller((ulong)(2 + i));
				}
			}
		}

		public static int GetControllers(Controller[] buffer)
		{
			int i = 0;
			foreach (var c in Controllers) { if (i >= buffer.Length) break; buffer[i++] = c; }
			return i;
		}

		internal static InputType InputTypeFor(ulong id)
		{
			return id == KeyboardHandle ? InputType.Unknown  : InputType.GenericGamepad;
		}

		internal static void SetActionSet(ulong controller, string name) { }
		internal static void ActivateLayer(ulong controller, string layer) { }
		internal static void DeactivateLayer(ulong controller, string layer) { }
		internal static void ClearLayers(ulong controller) { }

		public static string GetDigitalActionGlyph(Controller controller, string action) { return null; }

		static readonly Dictionary<string, KeyCode> _digital = new Dictionary<string, KeyCode>();
		static readonly Dictionary<string, string> _analogAxisX = new Dictionary<string, string>();
		static readonly Dictionary<string, string> _analogAxisY = new Dictionary<string, string>();
		internal static readonly List<string> KnownDigitalActions = new List<string>();
		internal static readonly List<string> KnownAnalogActions = new List<string>();

		const string PrefPrefix = "webworks_input_";

		static KeyCode DefaultKeyFor(int index)
		{
			KeyCode[] defaults = { KeyCode.Space, KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.E, KeyCode.LeftShift, KeyCode.LeftControl, KeyCode.R, KeyCode.F, KeyCode.Tab, KeyCode.Escape };
			return index < defaults.Length ? defaults[index] : KeyCode.None;
		}

		internal static KeyCode GetDigitalBinding(string action)
		{
			KeyCode bound;
			if (_digital.TryGetValue(action, out bound)) return bound;

			KnownDigitalActions.Add(action);
			var prefKey = PrefPrefix + "digital_" + action;
			if (PlayerPrefs.HasKey(prefKey))
			{
				Enum.TryParse(PlayerPrefs.GetString(prefKey), out bound);
			}
			else
			{
				bound = DefaultKeyFor(KnownDigitalActions.Count - 1);
			}
			_digital[action] = bound;
			return bound;
		}

		internal static void SetDigitalBinding(string action, KeyCode key)
		{
			_digital[action] = key;
			PlayerPrefs.SetString(PrefPrefix + "digital_" + action, key.ToString());
			PlayerPrefs.Save();
		}

		static readonly string[][] DefaultAxisPairs = { new[] { "Horizontal", "Vertical" }, new[] { "Mouse X", "Mouse Y" } };

		internal static void GetAnalogBinding(string action, out string axisX, out string axisY)
		{
			if (_analogAxisX.TryGetValue(action, out axisX)) { axisY = _analogAxisY[action]; return; }

			KnownAnalogActions.Add(action);
			var idx = KnownAnalogActions.Count - 1;
			var pair = idx < DefaultAxisPairs.Length ? DefaultAxisPairs[idx] : new[] { "", "" };
			axisX = PlayerPrefs.HasKey(PrefPrefix + "analogx_" + action) ? PlayerPrefs.GetString(PrefPrefix + "analogx_" + action) : pair[0];
			axisY = PlayerPrefs.HasKey(PrefPrefix + "analogy_" + action) ? PlayerPrefs.GetString(PrefPrefix + "analogy_" + action) : pair[1];
			_analogAxisX[action] = axisX;
			_analogAxisY[action] = axisY;
		}

		internal static DigitalState GetDigitalState(string action)
		{
			var key = GetDigitalBinding(action);
			if (key == KeyCode.None) return new DigitalState { Pressed = false, Active = false };
			return new DigitalState { Pressed = Input.GetKey(key), Active = true };
		}

		internal static AnalogState GetAnalogState(string action)
		{
			string ax, ay;
			GetAnalogBinding(action, out ax, out ay);
			if (string.IsNullOrEmpty(ax)) return new AnalogState { Active = false };
			float x = 0f, y = 0f;
			bool active = true;
			try { x = string.IsNullOrEmpty(ax) ? 0f : Input.GetAxis(ax); } catch { active = false; }
			try { y = string.IsNullOrEmpty(ay) ? 0f : Input.GetAxis(ay); } catch { }
			return new AnalogState { EMode = InputSourceMode.JoystickMove, X = x, Y = y, Active = active };
		}
	}
}
