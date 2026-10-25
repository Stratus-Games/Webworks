using System;
using System.Collections.Generic;

namespace Webworks.SNetInternal
{
	internal static class VoiceEngine
	{
		public const int CaptureRate = 16000;
		public const int OptimalRate = 16000;
		public const int HeaderSize = 10;
		public const int MinFrameSamples = 320;
		public const int MaxFrameSamples = 2400;
		public const int MaxPacketBytes = HeaderSize + (MaxFrameSamples + 1) / 2;

		public const int Ok = 0, NotRecording = 1, NoData = 2, BufferTooSmall = 3, Corrupted = 4;

		const int Consumer = 0;
		const byte Magic = 0x57;
		const byte CodecAdpcm = 1;

		static readonly int[] StepTable =
		{
			7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 21, 23, 25, 28, 31, 34, 37, 41, 45, 50, 55, 60, 66, 73, 80, 88, 97, 107, 118, 130, 143,
			157, 173, 190, 209, 230, 253, 279, 307, 337, 371, 408, 449, 494, 544, 598, 658, 724, 796, 876, 963, 1060, 1166, 1282, 1411, 1552,
			1707, 1878, 2066, 2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487,
			12635, 13899, 15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767
		};
		static readonly int[] IndexTable = { -1, -1, -1, -1, 2, 4, 6, 8, -1, -1, -1, -1, 2, 4, 6, 8 };

		static bool _recording;
		static readonly List<short> _tail = new List<short>();
		static readonly short[] _pcm = new short[MaxFrameSamples];
		static readonly short[] _drain = new short[4096];

		public static bool Recording { get { return _recording; } }

		public static void Start()
		{
			if (_recording) return;
			_recording = true;
			MicCapture.Open(Consumer, CaptureRate);
		}

		public static void Stop()
		{
			if (!_recording) return;
			_recording = false;
			int n;
			while (_tail.Count < CaptureRate * 2 && (n = MicCapture.Read(Consumer, _drain, _drain.Length)) > 0)
				for (int i = 0; i < n; i++) _tail.Add(_drain[i]);
			MicCapture.Close(Consumer);
		}

		static int SamplesAvailable() { return _tail.Count + (_recording ? MicCapture.Available(Consumer) : 0); }

		static int Take(short[] dest, int max)
		{
			int got = 0;
			if (_tail.Count > 0)
			{
				int n = Math.Min(_tail.Count, max);
				_tail.CopyTo(0, dest, 0, n);
				_tail.RemoveRange(0, n);
				got = n;
			}
			if (got < max && _recording)
			{
				int n = MicCapture.Read(Consumer, _drain, Math.Min(max - got, _drain.Length));
				Array.Copy(_drain, 0, dest, got, n);
				got += n;
			}
			return got;
		}

		public static int AvailableBytes()
		{
			int n = Math.Min(SamplesAvailable(), MaxFrameSamples);
			int need = _recording ? MinFrameSamples : 1;
			return n < need ? 0 : HeaderSize + (n + 1) / 2;
		}

		public static int GetVoice(byte[] dest, int destSize, out int written)
		{
			written = 0;
			int avail = SamplesAvailable();
			int need = _recording ? MinFrameSamples : 1;
			if (avail < need) return _recording ? NoData : NotRecording;
			if (dest == null) return BufferTooSmall;
			if (destSize > dest.Length) destSize = dest.Length;
			int fit = (destSize - HeaderSize) * 2;
			if (fit < need) return BufferTooSmall;
			int n = Take(_pcm, Math.Min(Math.Min(avail, MaxFrameSamples), fit));
			if (n <= 0) return NoData;
			written = Encode(_pcm, n, MicCapture.ActualRate(Consumer, CaptureRate), dest, 0);
			return Ok;
		}


		internal static int Encode(short[] pcm, int count, int rate, byte[] dest, int off)
		{
			int predictor = pcm[0];
			int probe = Math.Min(count, 32), sum = 0;
			for (int i = 1; i < probe; i++) sum += Math.Abs(pcm[i] - pcm[i - 1]);
			int mean = probe > 1 ? sum / (probe - 1) : 0;
			int index = 0;
			while (index < 88 && StepTable[index] < mean) index++;
			dest[off] = Magic;
			dest[off + 1] = CodecAdpcm;
			dest[off + 2] = (byte)(rate & 0xFF);
			dest[off + 3] = (byte)((rate >> 8) & 0xFF);
			dest[off + 4] = (byte)(count & 0xFF);
			dest[off + 5] = (byte)((count >> 8) & 0xFF);
			dest[off + 6] = (byte)(predictor & 0xFF);
			dest[off + 7] = (byte)((predictor >> 8) & 0xFF);
			dest[off + 8] = (byte)index;
			dest[off + 9] = 0;

			int outPos = off + HeaderSize;
			for (int i = 0; i < count; i++)
			{
				int diff = pcm[i] - predictor;
				int sign = 0;
				if (diff < 0) { sign = 8; diff = -diff; }
				int step = StepTable[index];
				int delta = 0, vpdiff = step >> 3;
				if (diff >= step) { delta |= 4; diff -= step; vpdiff += step; }
				step >>= 1;
				if (diff >= step) { delta |= 2; diff -= step; vpdiff += step; }
				step >>= 1;
				if (diff >= step) { delta |= 1; vpdiff += step; }
				predictor += sign != 0 ? -vpdiff : vpdiff;
				if (predictor > 32767) predictor = 32767; else if (predictor < -32768) predictor = -32768;
				delta |= sign;
				index += IndexTable[delta];
				if (index < 0) index = 0; else if (index > 88) index = 88;

				if ((i & 1) == 0) dest[outPos] = (byte)delta;
				else { dest[outPos] |= (byte)(delta << 4); outPos++; }
			}
			if ((count & 1) != 0) outPos++;
			return outPos - off;
		}

		public static int Decompress(byte[] src, int srcLen, byte[] dest, int destSize, int desiredRate, out int written)
		{
			written = 0;
			if (src == null) return Corrupted;
			if (srcLen > src.Length) srcLen = src.Length;
			if (srcLen < HeaderSize || src[0] != Magic || src[1] != CodecAdpcm) return Corrupted;
			int rate = src[2] | (src[3] << 8);
			int count = src[4] | (src[5] << 8);
			int predictor = (short)(src[6] | (src[7] << 8));
			int index = src[8];
			if (rate < 4000 || rate > 96000 || count == 0 || count > MaxFrameSamples || index > 88) return Corrupted;
			if (srcLen < HeaderSize + (count + 1) / 2) return Corrupted;

			if (desiredRate <= 0) desiredRate = OptimalRate;
			if (desiredRate > 96000) desiredRate = 96000;
			int outCount = (int)((long)count * desiredRate / rate);
			if (outCount < 1) outCount = 1;
			int bytes = outCount * 2;
			if (dest == null) { written = bytes; return BufferTooSmall; }
			if (destSize > dest.Length) destSize = dest.Length;
			if (bytes > destSize) { written = bytes; return BufferTooSmall; }

			var pcm = new short[count];
			int inPos = HeaderSize;
			for (int i = 0; i < count; i++)
			{
				int d = (i & 1) == 0 ? (src[inPos] & 0x0F) : (src[inPos++] >> 4);
				int step = StepTable[index];
				int vpdiff = step >> 3;
				if ((d & 4) != 0) vpdiff += step;
				if ((d & 2) != 0) vpdiff += step >> 1;
				if ((d & 1) != 0) vpdiff += step >> 2;
				predictor += (d & 8) != 0 ? -vpdiff : vpdiff;
				if (predictor > 32767) predictor = 32767; else if (predictor < -32768) predictor = -32768;
				index += IndexTable[d];
				if (index < 0) index = 0; else if (index > 88) index = 88;
				pcm[i] = (short)predictor;
			}

			for (int j = 0; j < outCount; j++)
			{
				int v;
				if (desiredRate == rate) v = pcm[j];
				else
				{
					double t = j * (double)rate / desiredRate;
					int i0 = (int)t;
					if (i0 > count - 1) i0 = count - 1;
					int i1 = i0 + 1 < count ? i0 + 1 : i0;
					v = (int)Math.Round(pcm[i0] + (pcm[i1] - pcm[i0]) * (t - i0));
				}
				dest[j * 2] = (byte)(v & 0xFF);
				dest[j * 2 + 1] = (byte)((v >> 8) & 0xFF);
			}
			written = bytes;
			return Ok;
		}
	}
}
