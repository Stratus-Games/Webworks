#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using UnityEngine;

// A WebGL stand-in for microphone capture. It lives in the shim's own namespace on purpose:
// declaring UnityEngine.Microphone again collides with the real type on any project that has
// Unity's audio module, so this must never be public surface or sit in UnityEngine.
namespace Webworks.SNetInternal
{
	internal static class WebGLMicrophone
	{
		public const string VirtualDeviceName = "Webworks Microphone";

		public static bool PretendDeviceExists = true;

		const int Consumer = 1;

		sealed class Session { public AudioClip Clip; public bool Loop; public int Length; public long Written; }
		static Session _s;
		static GameObject _pump;
		static bool _warnedUnavailable;
		static readonly short[] _read = new short[4096];

		public static string[] devices { get { return PretendDeviceExists ? new[] { VirtualDeviceName } : new string[0]; } }

		public static AudioClip Start(string deviceName, bool loop, int lengthSec, int frequency)
		{
			if (!PretendDeviceExists) return null;
			if (frequency <= 0) frequency = 44100;
			if (lengthSec < 1) lengthSec = 1;
			End(deviceName);

			var clip = AudioClip.Create("Microphone", lengthSec * frequency, 1, frequency, false);
			_s = new Session { Clip = clip, Loop = loop, Length = lengthSec * frequency };
			_warnedUnavailable = false;
			MicCapture.Open(Consumer, frequency);
			EnsurePump();
			return clip;
		}

		public static void End(string deviceName)
		{
			if (_s == null) return;
			Pump();
			MicCapture.Close(Consumer);
			_s = null;
		}

		public static bool IsRecording(string deviceName) { return _s != null; }

		public static int GetPosition(string deviceName)
		{
			if (_s == null) return 0;
			Pump();
			long w = _s.Written;
			if (w == 0) return 1;
			return _s.Loop ? (int)(w % _s.Length) : (int)Math.Min(w, _s.Length);
		}

		public static void GetDeviceCaps(string deviceName, out int minFreq, out int maxFreq) { minFreq = 0; maxFreq = 0; }


		static void EnsurePump()
		{
			if (_pump != null) return;
			_pump = new GameObject("WebworksMicrophonePump");
			_pump.hideFlags = HideFlags.HideAndDontSave;
			UnityEngine.Object.DontDestroyOnLoad(_pump);
			_pump.AddComponent<MicPump>();
		}

		sealed class MicPump : MonoBehaviour { void Update() { WebGLMicrophone.Pump(); } }

		static void Pump()
		{
			var s = _s;
			if (s == null) return;
			if (!_warnedUnavailable && MicCapture.State == MicCapture.StateUnavailable)
			{
				_warnedUnavailable = true;
				Debug.LogWarning("[Webworks] Microphone.Start: no microphone audio is available (permission denied, no device, or the page isn't https/localhost). The clip will stay silent.");
			}
			int n;
			while ((n = MicCapture.Read(Consumer, _read, _read.Length)) > 0)
			{
				var f = new float[n];
				for (int i = 0; i < n; i++) f[i] = _read[i] / 32768f;
				WriteToClip(s, f);
			}
		}

		static void WriteToClip(Session s, float[] data)
		{
			int done = 0;
			while (done < data.Length)
			{
				int head;
				if (s.Loop) head = (int)(s.Written % s.Length);
				else
				{
					if (s.Written >= s.Length) return;
					head = (int)s.Written;
				}
				int chunk = Math.Min(data.Length - done, s.Length - head);
				float[] piece;
				if (done == 0 && chunk == data.Length) piece = data;
				else { piece = new float[chunk]; Array.Copy(data, done, piece, 0, chunk); }
				s.Clip.SetData(piece, head);
				s.Written += chunk;
				done += chunk;
			}
		}
	}
}
#endif
