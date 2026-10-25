using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	public static partial class SteamNetworkingUtils
	{
		static readonly Dictionary<int, double> _numericConfig = new Dictionary<int, double>();
		static readonly Dictionary<int, string> _stringConfig = new Dictionary<int, string>();
		static FSteamNetworkingSocketsDebugOutput _debugOutput;
		static ESteamNetworkingSocketsDebugOutputType _debugLevel = ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_None;
		static bool _relayInitialized;
		static readonly DateTime _epoch = DateTime.UtcNow;

		public static IntPtr AllocateMessage(int cbAllocateBuffer)
		{
			return cbAllocateBuffer > 0 ? Marshal.AllocHGlobal(cbAllocateBuffer) : IntPtr.Zero;
		}

		public static void InitRelayNetworkAccess()
		{
			_relayInitialized = true;
			var status = new SteamRelayNetworkStatus_t
			{
				m_eAvail = ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current,
				m_bPingMeasurementInProgress = 0,
				m_eAvailNetworkConfig = ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current,
				m_eAvailAnyRelay = ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current,
				m_debugMsg = "relay available over the websocket transport",
			};
			CallbackBus<SteamRelayNetworkStatus_t>.Fire(status);
		}

		public static ESteamNetworkingAvailability GetRelayNetworkStatus(out SteamRelayNetworkStatus_t pDetails)
		{
			pDetails = new SteamRelayNetworkStatus_t
			{
				m_eAvail = SessionState.Initialized
					? (_relayInitialized ? ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current : ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Attempting)
					: ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_CannotTry,
				m_bPingMeasurementInProgress = 0,
				m_eAvailNetworkConfig = SessionState.Initialized
					? ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current
					: ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_CannotTry,
				m_eAvailAnyRelay = SessionState.Initialized
					? ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current
					: ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_CannotTry,
				m_debugMsg = SessionState.Initialized ? "websocket relay" : "not initialized",
			};
			return pDetails.m_eAvail;
		}

		public static float GetLocalPingLocation(out SteamNetworkPingLocation_t result)
		{
			var stamp = (ulong)(DateTime.UtcNow - _epoch).TotalMilliseconds;
			result = new SteamNetworkPingLocation_t { m_data = BitConverter.GetBytes(stamp) };
			return 0f;
		}

		public static int EstimatePingTimeBetweenTwoLocations(ref SteamNetworkPingLocation_t location1, ref SteamNetworkPingLocation_t location2)
		{
			return 1;
		}

		public static int EstimatePingTimeFromLocalHost(ref SteamNetworkPingLocation_t remoteLocation)
		{
			return 1;
		}

		public static void ConvertPingLocationToString(ref SteamNetworkPingLocation_t location, out string pszBuf, int cchBufSize)
		{
			var data = location.m_data ?? new byte[0];
			pszBuf = Convert.ToBase64String(data);
			if (cchBufSize > 0 && pszBuf.Length > cchBufSize) pszBuf = pszBuf.Substring(0, cchBufSize);
		}

		public static bool ParsePingLocationString(string pszString, out SteamNetworkPingLocation_t result)
		{
			result = default(SteamNetworkPingLocation_t);
			if (string.IsNullOrEmpty(pszString)) return false;
			try
			{
				result = new SteamNetworkPingLocation_t { m_data = Convert.FromBase64String(pszString) };
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static bool CheckPingDataUpToDate(float flMaxAgeSeconds)
		{
			return SessionState.Initialized && flMaxAgeSeconds > 0f;
		}

		public static int GetPingToDataCenter(SteamNetworkingPOPID popID, out SteamNetworkingPOPID pViaRelayPoP)
		{
			pViaRelayPoP = default(SteamNetworkingPOPID);
			return 0;
		}

		public static int GetDirectPingToPOP(SteamNetworkingPOPID popID) { return 0; }
		public static int GetPOPCount() { return 0; }

		public static int GetPOPList(out SteamNetworkingPOPID list, int nListSz)
		{
			list = default(SteamNetworkingPOPID);
			return 0;
		}

		public static SteamNetworkingMicroseconds GetLocalTimestamp()
		{
			return new SteamNetworkingMicroseconds((long)(DateTime.UtcNow - _epoch).TotalMilliseconds * 1000);
		}

		public static void SetDebugOutputFunction(ESteamNetworkingSocketsDebugOutputType eDetailLevel, FSteamNetworkingSocketsDebugOutput pfnFunc)
		{
			_debugLevel = eDetailLevel;
			_debugOutput = pfnFunc;
		}

		internal static void DebugOutput(ESteamNetworkingSocketsDebugOutputType level, string message)
		{
			if (_debugOutput == null || (int)level > (int)_debugLevel) return;
			try
			{
				_debugOutput(level, new StringBuilder(message));
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Webworks] networking debug hook threw: " + ex.Message);
			}
		}

		public static bool IsFakeIPv4(uint nIPv4) { return false; }
		public static ESteamNetworkingFakeIPType GetIPv4FakeIPType(uint nIPv4) { return ESteamNetworkingFakeIPType.k_ESteamNetworkingFakeIPType_NotFake; }

		public static EResult GetRealIdentityForFakeIP(ref SteamNetworkingIPAddr fakeIP, out SteamNetworkingIdentity pOutRealIdentity)
		{
			pOutRealIdentity = default(SteamNetworkingIdentity);
			pOutRealIdentity.Clear();
			return EResult.k_EResultInvalidParam;
		}

		public static bool SetConfigValue(ESteamNetworkingConfigValue eValue, ESteamNetworkingConfigScope eScopeType, IntPtr scopeObj, ESteamNetworkingConfigDataType eDataType, IntPtr pArg)
		{
			if (pArg == IntPtr.Zero) return false;
			int key = (int)eValue;

			switch (eDataType)
			{
				case ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32:
					_numericConfig[key] = Marshal.ReadInt32(pArg);
					return true;
				case ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int64:
					_numericConfig[key] = Marshal.ReadInt64(pArg);
					return true;
				case ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Float:
					_numericConfig[key] = BitConverter.ToSingle(BitConverter.GetBytes(Marshal.ReadInt32(pArg)), 0);
					return true;
				case ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_String:
					var textPtr = Marshal.ReadIntPtr(pArg);
					_stringConfig[key] = textPtr == IntPtr.Zero ? "" : Marshal.PtrToStringAnsi(textPtr);
					return true;
				case ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Ptr:
					_numericConfig[key] = (double)pArg.ToInt64();
					return true;
				default:
					return false;
			}
		}

		public static ESteamNetworkingGetConfigValueResult GetConfigValue(ESteamNetworkingConfigValue eValue, ESteamNetworkingConfigScope eScopeType, IntPtr scopeObj, out ESteamNetworkingConfigDataType pOutDataType, IntPtr pResult, ref ulong cbResult)
		{
			pOutDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32;
			int key = (int)eValue;

			string text;
			if (_stringConfig.TryGetValue(key, out text))
			{
				pOutDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_String;
				if (pResult == IntPtr.Zero || cbResult < (ulong)(text.Length + 1))
				{
					cbResult = (ulong)(text.Length + 1);
					return ESteamNetworkingGetConfigValueResult.k_ESteamNetworkingGetConfigValue_BufferTooSmall;
				}
				var bytes = Encoding.UTF8.GetBytes(text + "\0");
				Marshal.Copy(bytes, 0, pResult, Math.Min(bytes.Length, (int)cbResult));
				cbResult = (ulong)bytes.Length;
				return ESteamNetworkingGetConfigValueResult.k_ESteamNetworkingGetConfigValue_OK;
			}

			double numeric;
			if (!_numericConfig.TryGetValue(key, out numeric)) return ESteamNetworkingGetConfigValueResult.k_ESteamNetworkingGetConfigValue_BadValue;
			if (pResult == IntPtr.Zero) return ESteamNetworkingGetConfigValueResult.k_ESteamNetworkingGetConfigValue_BadScopeObj;

			Marshal.WriteInt32(pResult, (int)numeric);
			cbResult = 4;
			return ESteamNetworkingGetConfigValueResult.k_ESteamNetworkingGetConfigValue_OK;
		}

		public static string GetConfigValueInfo(ESteamNetworkingConfigValue eValue, out ESteamNetworkingConfigDataType pOutDataType, out ESteamNetworkingConfigScope pOutScope)
		{
			pOutDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32;
			pOutScope = ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global;

			if (_stringConfig.ContainsKey((int)eValue)) pOutDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_String;
			if (_numericConfig.ContainsKey((int)eValue) && pOutDataType != ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_String)
				pOutDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32;

			var name = Enum.GetName(typeof(ESteamNetworkingConfigValue), eValue);
			return name != null ? name.Replace("k_ESteamNetworkingConfig_", "") : eValue.ToString();
		}

		public static ESteamNetworkingConfigValue IterateGenericEditableConfigValues(ESteamNetworkingConfigValue eCurrent, bool bEnumerateDevVars)
		{
			var next = (int)eCurrent + 1;
			if (!Enum.IsDefined(typeof(ESteamNetworkingConfigValue), next)) return ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_Invalid;
			return (ESteamNetworkingConfigValue)next;
		}

		public static void SteamNetworkingIPAddr_ToString(ref SteamNetworkingIPAddr addr, out string buf, uint cbBuf, bool bWithPort)
		{
			buf = addr.ToString(bWithPort);
			if (cbBuf > 0 && buf.Length >= (int)cbBuf) buf = buf.Substring(0, (int)cbBuf - 1);
		}

		public static bool SteamNetworkingIPAddr_ParseString(out SteamNetworkingIPAddr pAddr, string pszStr)
		{
			pAddr = default(SteamNetworkingIPAddr);
			return pAddr.ParseString(pszStr);
		}

		public static ESteamNetworkingFakeIPType SteamNetworkingIPAddr_GetFakeIPType(ref SteamNetworkingIPAddr addr)
		{
			return addr.GetFakeIPType();
		}

		public static void SteamNetworkingIdentity_ToString(ref SteamNetworkingIdentity identity, out string buf, uint cbBuf)
		{
			buf = identity.ToString();
			if (cbBuf > 0 && buf.Length >= (int)cbBuf) buf = buf.Substring(0, (int)cbBuf - 1);
		}

		public static bool SteamNetworkingIdentity_ParseString(out SteamNetworkingIdentity pIdentity, string pszStr)
		{
			pIdentity = default(SteamNetworkingIdentity);
			return pIdentity.ParseString(pszStr);
		}

		internal static void Reset()
		{
			_numericConfig.Clear();
			_stringConfig.Clear();
			_relayInitialized = false;
		}
	}
}
