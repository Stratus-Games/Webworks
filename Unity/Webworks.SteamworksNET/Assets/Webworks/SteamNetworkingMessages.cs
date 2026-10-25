using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Webworks.SNetInternal;

namespace Steamworks
{
	// Connectionless-style messaging layered on the relay sockets: each remote identity
	// gets one relay connection, and channel numbers are carried in the message header.
	public static partial class SteamNetworkingMessages
	{
		const int ChannelBytes = 4;

		class Session
		{
			public string Key;
			public HSteamNetConnection Connection = HSteamNetConnection.Invalid;
			public readonly List<byte[]> Inbox = new List<byte[]>();
			public readonly object Gate = new object();
		}

		static readonly Dictionary<string, Session> _sessions = new Dictionary<string, Session>();

		static string KeyOf(ref SteamNetworkingIdentity identity)
		{
			var steamId = identity.GetSteamID64();
			if (steamId != 0) return "steam:" + steamId;
			var generic = identity.GetGenericString();
			if (!string.IsNullOrEmpty(generic)) return "str:" + generic;
			return identity.ToString();
		}

		internal static void Deliver(string remoteKey, byte[] payload)
		{
			Session session;
			if (!_sessions.TryGetValue(remoteKey, out session)) return;
			lock (session.Gate) session.Inbox.Add(payload);
		}

		static Session Ensure(ref SteamNetworkingIdentity identity)
		{
			var key = KeyOf(ref identity);
			Session session;
			if (_sessions.TryGetValue(key, out session)) return session;

			session = new Session { Key = key };
			_sessions[key] = session;

			SteamNetworkingConfigValue_t[] none = null;
			session.Connection = SteamNetworkingSockets.ConnectP2P(ref identity, 0, 0, none);
			return session;
		}

		public static EResult SendMessageToUser(ref SteamNetworkingIdentity identityRemote, IntPtr pubData, uint cubData, int nSendFlags, int nRemoteChannel)
		{
			if (pubData == IntPtr.Zero || cubData == 0) return EResult.k_EResultInvalidParam;
			if (!SessionState.Initialized) return EResult.k_EResultNoConnection;
			if (cubData > 256 * 1024) return EResult.k_EResultLimitExceeded;

			var session = Ensure(ref identityRemote);
			if (session.Connection == HSteamNetConnection.Invalid) return EResult.k_EResultNoConnection;

			var payload = new byte[cubData + ChannelBytes];
			payload[0] = (byte)(nRemoteChannel & 0xff);
			payload[1] = (byte)((nRemoteChannel >> 8) & 0xff);
			payload[2] = (byte)((nRemoteChannel >> 16) & 0xff);
			payload[3] = (byte)((nRemoteChannel >> 24) & 0xff);
			Marshal.Copy(pubData, payload, ChannelBytes, (int)cubData);

			var native = Marshal.AllocHGlobal(payload.Length);
			try
			{
				Marshal.Copy(payload, 0, native, payload.Length);
				long messageNumber;
				return SteamNetworkingSockets.SendMessageToConnection(session.Connection, native, (uint)payload.Length, nSendFlags, out messageNumber);
			}
			finally
			{
				Marshal.FreeHGlobal(native);
			}
		}

		public static int ReceiveMessagesOnChannel(int nLocalChannel, IntPtr[] ppOutMessages, int nMaxMessages)
		{
			if (ppOutMessages == null || nMaxMessages <= 0) return 0;

			int delivered = 0;
			foreach (var session in new List<Session>(_sessions.Values))
			{
				lock (session.Gate)
				{
					for (int i = session.Inbox.Count - 1; i >= 0 && delivered < nMaxMessages; i--)
					{
						var payload = session.Inbox[i];
						if (payload.Length < ChannelBytes) { session.Inbox.RemoveAt(i); continue; }
						int channel = payload[0] | (payload[1] << 8) | (payload[2] << 16) | (payload[3] << 24);
						if (channel != nLocalChannel) continue;

						var size = payload.Length - ChannelBytes;
						var buffer = Marshal.AllocHGlobal(size);
						Marshal.Copy(payload, ChannelBytes, buffer, size);
						ppOutMessages[delivered++] = buffer;
						session.Inbox.RemoveAt(i);
					}
				}
			}
			return delivered;
		}

		public static bool AcceptSessionWithUser(ref SteamNetworkingIdentity identityRemote)
		{
			if (!SessionState.Initialized) return false;
			var session = Ensure(ref identityRemote);
			return session.Connection != HSteamNetConnection.Invalid &&
				SteamNetworkingSockets.AcceptConnection(session.Connection) == EResult.k_EResultOK;
		}

		public static bool CloseSessionWithUser(ref SteamNetworkingIdentity identityRemote)
		{
			Session session;
			if (!_sessions.TryGetValue(KeyOf(ref identityRemote), out session)) return false;
			if (session.Connection != HSteamNetConnection.Invalid)
				SteamNetworkingSockets.CloseConnection(session.Connection, 0, "", false);
			_sessions.Remove(session.Key);
			return true;
		}

		public static bool CloseChannelWithUser(ref SteamNetworkingIdentity identityRemote, int nLocalChannel)
		{
			Session session;
			if (!_sessions.TryGetValue(KeyOf(ref identityRemote), out session)) return false;
			lock (session.Gate)
			{
				session.Inbox.RemoveAll((payload) =>
				{
					if (payload.Length < ChannelBytes) return true;
					int channel = payload[0] | (payload[1] << 8) | (payload[2] << 16) | (payload[3] << 24);
					return channel == nLocalChannel;
				});
			}
			return true;
		}

		public static ESteamNetworkingConnectionState GetSessionConnectionInfo(ref SteamNetworkingIdentity identityRemote, out SteamNetConnectionInfo_t pConnectionInfo, out SteamNetConnectionRealTimeStatus_t pQuickStatus)
		{
			Session session;
			if (!_sessions.TryGetValue(KeyOf(ref identityRemote), out session) || session.Connection == HSteamNetConnection.Invalid)
			{
				pConnectionInfo = default(SteamNetConnectionInfo_t);
				pQuickStatus = default(SteamNetConnectionRealTimeStatus_t);
				return ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None;
			}
			pQuickStatus = default(SteamNetConnectionRealTimeStatus_t);
			if (!SteamNetworkingSockets.GetConnectionInfo(session.Connection, out pConnectionInfo))
				return ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None;

			pQuickStatus.m_eState = pConnectionInfo.m_eState;
			return pConnectionInfo.m_eState;
		}

		internal static void Reset() { _sessions.Clear(); }
	}
}
