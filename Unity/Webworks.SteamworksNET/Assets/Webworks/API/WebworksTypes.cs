using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	[StructLayout(LayoutKind.Sequential)]
	public struct FriendGameInfo_t
	{
		public CGameID m_gameID;
		public uint m_unGameIP;
		public ushort m_usGamePort;
		public ushort m_usQueryPort;
		public CSteamID m_steamIDLobby;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct InputAnalogActionData_t
	{
		public EInputSourceMode eMode;
		public float x;
		public float y;
		public byte bActive;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct InputDigitalActionData_t
	{
		public byte bState;
		public byte bActive;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct InputMotionData_t
	{
		public float rotQuatX;
		public float rotQuatY;
		public float rotQuatZ;
		public float rotQuatW;
		public float posAccelX;
		public float posAccelY;
		public float posAccelZ;
		public float rotVelX;
		public float rotVelY;
		public float rotVelZ;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SteamItemDetails_t
	{
		public SteamItemInstanceID_t m_itemId;
		public SteamItemDef_t m_iDefinition;
		public ushort m_unQuantity;
		public ushort m_unFlags;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SteamPartyBeaconLocation_t
	{
		public ESteamPartyBeaconLocationType m_eType;
		public ulong m_ulLocationID;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SteamParamStringArray_t
	{
		public IntPtr m_ppStrings;
		public int m_nNumStrings;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SteamUGCDetails_t
	{
		public PublishedFileId_t m_nPublishedFileId;
		public EResult m_eResult;
		public EWorkshopFileType m_eFileType;
		public AppId_t m_nCreatorAppID;
		public AppId_t m_nConsumerAppID;
		public string m_rgchTitle;
		public string m_rgchDescription;
		public ulong m_ulSteamIDOwner;
		public uint m_rtimeCreated;
		public uint m_rtimeUpdated;
		public uint m_rtimeAddedToUserList;
		public ERemoteStoragePublishedFileVisibility m_eVisibility;
		public bool m_bBanned;
		public bool m_bAcceptedForUse;
		public bool m_bTagsTruncated;
		public string m_rgchTags;
		public UGCHandle_t m_hFile;
		public UGCHandle_t m_hPreviewFile;
		public string m_pchFileName;
		public int m_nFileSize;
		public int m_nPreviewFileSize;
		public string m_rgchURL;
		public uint m_unVotesUp;
		public uint m_unVotesDown;
		public float m_flScore;
		public uint m_unNumChildren;
		public ulong m_ulTotalFilesSize;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct MatchMakingKeyValuePair_t
	{
		public MatchMakingKeyValuePair_t(string strKey, string strValue)
		{
			m_szKey = strKey;
			m_szValue = strValue;
		}

		public string m_szKey;
		public string m_szValue;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct CallbackMsg_t
	{
		public int m_hSteamUser;
		public int m_iCallback;
		public IntPtr m_pubParam;
		public int m_cubParam;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SteamNetworkingIPAddr : IEquatable<SteamNetworkingIPAddr>
	{
		public byte[] m_ipv6;
		public ushort m_port;

		public void Clear() { m_ipv6 = new byte[16]; m_port = 0; }
		public bool IsIPv6AllZeros() { return m_ipv6 == null || Array.TrueForAll(m_ipv6, b => b == 0); }
		public void SetIPv6(byte[] ipv6, ushort nPort) { m_ipv6 = ipv6 ?? new byte[16]; m_port = nPort; }
		public void SetIPv4(uint nIP, ushort nPort) { m_ipv6 = new byte[16]; m_ipv6[10] = 0xff; m_ipv6[11] = 0xff; m_ipv6[12] = (byte)(nIP >> 24); m_ipv6[13] = (byte)(nIP >> 16); m_ipv6[14] = (byte)(nIP >> 8); m_ipv6[15] = (byte)nIP; m_port = nPort; }
		public uint GetIPv4() { return m_ipv6 == null || m_ipv6.Length < 16 ? 0u : (uint)((m_ipv6[12] << 24) | (m_ipv6[13] << 16) | (m_ipv6[14] << 8) | m_ipv6[15]); }
		public bool IsIPv4() { return m_ipv6 != null && m_ipv6.Length == 16 && m_ipv6[10] == 0xff && m_ipv6[11] == 0xff; }
		public string ToString(bool bWithPort = true) { return IsIPv4() ? string.Format("{0}.{1}.{2}.{3}{4}", m_ipv6[12], m_ipv6[13], m_ipv6[14], m_ipv6[15], bWithPort ? ":" + m_port : "") : (bWithPort ? ":" + m_port : ""); }
		public override string ToString() { return ToString(true); }
		public bool ParseString(string pszStr)
		{
			if (string.IsNullOrEmpty(pszStr)) return false;
			var text = pszStr.Trim();
			ushort port = 0;

			if (text.StartsWith("["))
			{
				int close = text.IndexOf(']');
				if (close < 0) return false;
				var tail = text.Substring(close + 1);
				if (tail.StartsWith(":") && !ushort.TryParse(tail.Substring(1), out port)) return false;
				Clear();
				m_port = port;
				return true;
			}

			int colon = text.LastIndexOf(':');
			if (colon > 0 && text.IndexOf(':') == colon)
			{
				if (!ushort.TryParse(text.Substring(colon + 1), out port)) return false;
				text = text.Substring(0, colon);
			}

			var parts = text.Split('.');
			if (parts.Length != 4) return false;
			uint ip = 0;
			foreach (var part in parts)
			{
				byte octet;
				if (!byte.TryParse(part, out octet)) return false;
				ip = (ip << 8) | octet;
			}
			SetIPv4(ip, port);
			return true;
		}
		public ESteamNetworkingFakeIPType GetFakeIPType() { return ESteamNetworkingFakeIPType.k_ESteamNetworkingFakeIPType_NotFake; }
		public bool IsFakeIP() { return false; }
		public bool Equals(SteamNetworkingIPAddr other) { return m_port == other.m_port && (m_ipv6 == other.m_ipv6); }
		public override bool Equals(object obj) { return obj is SteamNetworkingIPAddr && Equals((SteamNetworkingIPAddr)obj); }
		public override int GetHashCode() { return m_port ^ (m_ipv6 == null ? 0 : m_ipv6.GetHashCode()); }
		public static bool operator ==(SteamNetworkingIPAddr a, SteamNetworkingIPAddr b) { return a.Equals(b); }
		public static bool operator !=(SteamNetworkingIPAddr a, SteamNetworkingIPAddr b) { return !a.Equals(b); }
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SteamNetworkingErrMsg
	{
		public string m_rgchErrorMsg;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SteamDatagramRelayAuthTicket
	{
		public SteamNetworkingIdentity m_identityGameserver;
		public SteamNetworkingIdentity m_identityAuthorizedClient;
		public uint m_rtimeTicketExpiry;
		public SteamNetworkingPOPID m_idPOPRelay;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SteamDatagramHostedAddress
	{
		public byte[] m_data;
		public int m_cb;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SteamNetworkingFakeIPResult_t
	{
		public EResult m_eResult;
		public SteamNetworkingIdentity m_identity;
		public uint m_unIP;
		public ushort m_unPorts;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SteamNetAuthenticationStatus_t
	{
		public ESteamNetworkingAvailability m_eAvail;
		public string m_debugMsg;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SteamRelayNetworkStatus_t
	{
		public ESteamNetworkingAvailability m_eAvail;
		public int m_bPingMeasurementInProgress;
		public ESteamNetworkingAvailability m_eAvailNetworkConfig;
		public ESteamNetworkingAvailability m_eAvailAnyRelay;
		public string m_debugMsg;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SteamNetConnectionInfo_t
	{
		public SteamNetworkingIdentity m_identityRemote;
		public long m_nUserData;
		public HSteamListenSocket m_hListenSocket;
		public SteamNetworkingIPAddr m_addrRemote;
		public ushort m__pad1;
		public SteamNetworkingPOPID m_idPOPRemote;
		public SteamNetworkingPOPID m_idPOPRelay;
		public ESteamNetworkingConnectionState m_eState;
		public int m_eEndReason;
		public string m_szEndDebug;
		public string m_szConnectionDescription;
		public int m_nFlags;
		public uint[] reserved;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SteamNetConnectionRealTimeStatus_t
	{
		public ESteamNetworkingConnectionState m_eState;
		public int m_nPing;
		public float m_flConnectionQualityLocal;
		public float m_flConnectionQualityRemote;
		public float m_flOutPacketsPerSec;
		public float m_flOutBytesPerSec;
		public float m_flInPacketsPerSec;
		public float m_flInBytesPerSec;
		public int m_nSendRateBytesPerSecond;
		public int m_cbPendingUnreliable;
		public int m_cbPendingReliable;
		public int m_cbSentUnackedReliable;
		public SteamNetworkingMicroseconds m_usecQueueTime;
		public int m_usecMaxJitter;
		public uint[] reserved;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SteamNetConnectionRealTimeLaneStatus_t
	{
		public int m_cbPendingUnreliable;
		public int m_cbPendingReliable;
		public int m_cbSentUnackedReliable;
		public int _reservePad1;
		public SteamNetworkingMicroseconds m_usecQueueTime;
		public uint[] reserved;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SteamNetworkPingLocation_t
	{
		public byte[] m_data;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct ISteamNetworkingSignalingRecvContext
	{
		public IntPtr OnConnectRequest(HSteamNetConnection hConn, ref SteamNetworkingIdentity identityPeer, int nLocalVirtualPort) { return IntPtr.Zero; }
		public void SendRejectionSignal(ref SteamNetworkingIdentity identityPeer, IntPtr pMsg, int cbMsg) { }
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct ISteamNetworkingConnectionSignaling
	{
		public bool SendSignal(HSteamNetConnection hConn, ref SteamNetConnectionInfo_t info, IntPtr pMsg, int cbMsg) { return false; }
		public void Release() { }
	}

	public delegate void SteamInputActionEventCallbackPointer(IntPtr SteamInputActionEvent);

[StructLayout(LayoutKind.Sequential)]
public struct RemotePlayInput_t
{
	[StructLayout(LayoutKind.Explicit, Size = 56)]
	public struct OptionValue
	{
		[FieldOffset(0)] public ulong m_ulValue;
		[FieldOffset(0)] public float m_flValue;
	}

	public RemotePlaySessionID_t m_unSessionID;
	public ERemotePlayInputType m_eType;
	public OptionValue m_val;
}

public interface ISteamMatchmakingServerListResponse
	{
		void ServerResponded(HServerListRequest hRequest, int iServer);
		void ServerFailedToRespond(HServerListRequest hRequest, int iServer);
		void RefreshComplete(HServerListRequest hRequest, EMatchMakingServerResponse response);
	}

	public interface ISteamMatchmakingPingResponse
	{
		void ServerResponded(gameserveritem_t server);
		void ServerFailedToRespond();
	}

	public interface ISteamMatchmakingPlayersResponse
	{
		void AddPlayerToList(string pchName, int nScore, float flTimePlayed);
		void PlayersFailedToRespond();
		void PlayersRefreshComplete();
	}

	public interface ISteamMatchmakingRulesResponse
	{
		void RulesResponded(string pchRule, string pchValue);
		void RulesFailedToRespond();
		void RulesRefreshComplete();
	}

}
