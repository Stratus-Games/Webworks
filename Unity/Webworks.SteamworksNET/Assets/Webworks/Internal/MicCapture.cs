using System;
using System.Collections.Generic;
using UnityEngine;

namespace Webworks.SNetInternal
{
	internal static class MicCapture
	{
		public const int StateIdle = 0, StatePending = 1, StateLive = 2, StateUnavailable = 3;

#if UNITY_WEBGL && !UNITY_EDITOR
		static byte[] _scratch = new byte[0];

		public static void Open(int consumer, int sampleRate) { WebworksNative.Webworks_Mic_Start(consumer, sampleRate); }
		public static void Close(int consumer) { WebworksNative.Webworks_Mic_Stop(consumer); }
		public static int State { get { return WebworksNative.Webworks_Mic_State(); } }
		public static int Available(int consumer) { return WebworksNative.Webworks_Mic_Available(consumer); }
		public static int ActualRate(int consumer, int requested) { return requested; }

		public static int Read(int consumer, short[] dest, int maxSamples)
		{
			if (maxSamples > dest.Length) maxSamples = dest.Length;
			if (maxSamples <= 0) return 0;
			if (_scratch.Length < maxSamples * 2) _scratch = new byte[maxSamples * 2];
			int n = WebworksNative.Webworks_Mic_Read(consumer, _scratch, maxSamples);
			if (n > 0) Buffer.BlockCopy(_scratch, 0, dest, 0, n * 2);
			return n;
		}
#elif UNITY_WEBGL
		public static void Open(int consumer, int sampleRate) { }
		public static void Close(int consumer) { }
		public static int State { get { return StateUnavailable; } }
		public static int Available(int consumer) { return 0; }
		public static int ActualRate(int consumer, int requested) { return requested; }
		public static int Read(int consumer, short[] dest, int maxSamples) { return 0; }
#else
		sealed class Source { public AudioClip Clip; public int Last; }
		static readonly Dictionary<int, Source> _sources = new Dictionary<int, Source>();
		static bool _failed;

		public static void Open(int consumer, int sampleRate)
		{
			Close(consumer);
			_failed = false;
			if (Microphone.devices.Length == 0)
			{
				_failed = true;
				Debug.LogWarning("[Webworks] No microphone found; voice recording will stay silent.");
				return;
			}
			var clip = Microphone.Start(null, true, 2, sampleRate);
			if (clip == null) { _failed = true; return; }
			_sources[consumer] = new Source { Clip = clip, Last = 0 };
		}

		public static void Close(int consumer)
		{
			if (_sources.Remove(consumer)) Microphone.End(null);
		}

		public static int State
		{
			get
			{
				if (_sources.Count == 0) return _failed ? StateUnavailable : StateIdle;
				return Microphone.GetPosition(null) > 0 ? StateLive : StatePending;
			}
		}

		public static int ActualRate(int consumer, int requested)
		{
			Source s;
			return _sources.TryGetValue(consumer, out s) && s.Clip != null ? s.Clip.frequency : requested;
		}

		public static int Available(int consumer)
		{
			Source s;
			if (!_sources.TryGetValue(consumer, out s) || s.Clip == null) return 0;
			int pos = Microphone.GetPosition(null), len = s.Clip.samples;
			return pos >= s.Last ? pos - s.Last : len - s.Last + pos;
		}

		public static int Read(int consumer, short[] dest, int maxSamples)
		{
			Source s;
			if (!_sources.TryGetValue(consumer, out s) || s.Clip == null) return 0;
			int count = Math.Min(Math.Min(Available(consumer), maxSamples), dest.Length);
			if (count <= 0) return 0;
			int len = s.Clip.samples, done = 0;
			while (done < count)
			{
				int chunk = Math.Min(count - done, len - s.Last);
				var f = new float[chunk];
				s.Clip.GetData(f, s.Last);
				for (int i = 0; i < chunk; i++)
				{
					float v = f[i] * 32767f;
					dest[done + i] = (short)(v > 32767f ? 32767f : (v < -32768f ? -32768f : v));
				}
				done += chunk;
				s.Last = (s.Last + chunk) % len;
			}
			return count;
		}
#endif
	}
}
