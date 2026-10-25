using System;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	

	

	public static partial class SteamUser
	{
		static EVoiceResult MapVoice(int r)
		{
			switch (r)
			{
				case VoiceEngine.Ok: return EVoiceResult.k_EVoiceResultOK;
				case VoiceEngine.NotRecording: return EVoiceResult.k_EVoiceResultNotRecording;
				case VoiceEngine.NoData: return EVoiceResult.k_EVoiceResultNoData;
				case VoiceEngine.BufferTooSmall: return EVoiceResult.k_EVoiceResultBufferTooSmall;
				default: return EVoiceResult.k_EVoiceResultDataCorrupted;
			}
		}

		static int ClampInt(uint v) { return v > int.MaxValue ? int.MaxValue : (int)v; }

		public static void StartVoiceRecording() { VoiceEngine.Start(); }
		public static void StopVoiceRecording() { VoiceEngine.Stop(); }

		public static EVoiceResult GetAvailableVoice(out uint pcbCompressed)
		{
			int bytes = VoiceEngine.AvailableBytes();
			pcbCompressed = (uint)bytes;
			if (bytes > 0) return EVoiceResult.k_EVoiceResultOK;
			return VoiceEngine.Recording ? EVoiceResult.k_EVoiceResultNoData : EVoiceResult.k_EVoiceResultNotRecording;
		}

		public static EVoiceResult GetAvailableVoice(out uint pcbCompressed, out uint pcbUncompressed_Deprecated, uint nUncompressedVoiceDesiredSampleRate_Deprecated)
		{
			var r = GetAvailableVoice(out pcbCompressed);
			int samples = pcbCompressed == 0 ? 0 : ((int)pcbCompressed - VoiceEngine.HeaderSize) * 2;
			uint rate = nUncompressedVoiceDesiredSampleRate_Deprecated == 0 ? (uint)VoiceEngine.OptimalRate : nUncompressedVoiceDesiredSampleRate_Deprecated;
			pcbUncompressed_Deprecated = (uint)((long)samples * rate / VoiceEngine.CaptureRate) * 2;
			return r;
		}

		public static EVoiceResult GetVoice(bool bWantCompressed, byte[] pDestBuffer, uint cbDestBufferSize, out uint nBytesWritten)
		{
			nBytesWritten = 0;
			if (!bWantCompressed) return EVoiceResult.k_EVoiceResultUnsupportedCodec;
			int written;
			int r = VoiceEngine.GetVoice(pDestBuffer, ClampInt(cbDestBufferSize), out written);
			nBytesWritten = (uint)written;
			return MapVoice(r);
		}

		public static EVoiceResult GetVoice(bool bWantCompressed, byte[] pDestBuffer, uint cbDestBufferSize, out uint nBytesWritten,
			bool bWantUncompressed_Deprecated, byte[] pUncompressedDestBuffer_Deprecated, uint cbUncompressedDestBufferSize_Deprecated,
			out uint nUncompressBytesWritten_Deprecated, uint nUncompressedVoiceDesiredSampleRate_Deprecated)
		{
			nUncompressBytesWritten_Deprecated = 0;
			return GetVoice(bWantCompressed, pDestBuffer, cbDestBufferSize, out nBytesWritten);
		}

		public static EVoiceResult DecompressVoice(byte[] pCompressed, uint cbCompressed, byte[] pDestBuffer, uint cbDestBufferSize, out uint nBytesWritten, uint nDesiredSampleRate)
		{
			int written;
			int r = VoiceEngine.Decompress(pCompressed, ClampInt(cbCompressed), pDestBuffer, ClampInt(cbDestBufferSize), ClampInt(nDesiredSampleRate), out written);
			nBytesWritten = (uint)written;
			return MapVoice(r);
		}

		public static uint GetVoiceOptimalSampleRate() { return (uint)VoiceEngine.OptimalRate; }
	}

	public static partial class SteamFriends
	{
		public static void SetInGameVoiceSpeaking(CSteamID steamIDUser, bool bSpeaking) { }

		public static void SetPlayedWith(CSteamID steamIDUserPlayedWith) { }

		public static void ActivateGameOverlayToStore(AppId_t nAppID, EOverlayToStoreFlag eFlag)
		{
			Debug.Log("[Webworks] ActivateGameOverlayToStore(" + nAppID + ") -- no Steam store in the web shim");
		}
	}
}
