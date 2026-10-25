using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	[StructLayout(LayoutKind.Sequential)]
	public struct SteamNetworkingMessage_t
	{
		public IntPtr m_pData;
		public int m_cbSize;
		public HSteamNetConnection m_conn;
		public SteamNetworkingIdentity m_identityPeer;
		public long m_nConnUserData;
		public long m_usecTimeReceived;
		public long m_nMessageNumber;
		public IntPtr m_pfnFreeData;
		public IntPtr m_pfnRelease;
		public int m_nChannel;
		public int m_nFlags;
		public long m_nUserData;

		public void Release()
		{
			if (m_pData != IntPtr.Zero) Marshal.FreeHGlobal(m_pData);
			m_pData = IntPtr.Zero;
		}

		public static void Release(IntPtr message)
		{
			if (message == IntPtr.Zero) return;
			try
			{
				var msg = (SteamNetworkingMessage_t)Marshal.PtrToStructure(message, typeof(SteamNetworkingMessage_t));
				if (msg.m_pData != IntPtr.Zero) Marshal.FreeHGlobal(msg.m_pData);
			}
			finally
			{
				Marshal.FreeHGlobal(message);
			}
		}
	}

	public static partial class SteamNetworkingSockets
	{
		const int RelayChannel = 90001;
		const int SendReliable = 8;

		class Conn
		{
			public uint Id;
			public ulong Peer;
			public uint PeerId;
			public int VirtualPort;
			public int TunnelStream;
			public bool Incoming;
			public bool IsTunnel;
			public ESteamNetworkingConnectionState State;
			public HSteamListenSocket ListenSocket;
			public string Name = "";
			public long UserData;
			public long NextMessage = 1;
			public SteamNetworkingIdentity RemoteIdentity;
			public string RemoteAddress = "";
			public Conn Loopback;
			public readonly Queue<byte[]> Inbox = new Queue<byte[]>();
			public readonly Queue<int> Channels = new Queue<int>();
		}

		class Listener
		{
			public uint Id;
			public int VirtualPort;
			public int TunnelStream;
			public int Port;
			public bool IsTunnel;
			public SteamNetworkingIPAddr Address;
		}

		static readonly Dictionary<uint, Conn> _conns = new Dictionary<uint, Conn>();
		static readonly Dictionary<uint, Listener> _listeners = new Dictionary<uint, Listener>();
		static uint _nextConn = 1;
		static uint _nextListener = 1;
		static bool _hooked;

		static void Hook()
		{
			if (_hooked) return;
			_hooked = true;

			TunnelClient.OnOpened += (stream, protocol, ok) =>
			{
				var conn = ConnForStream(stream);
				if (conn == null) return;
				conn.State = ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected;
				Raise(conn);
			};

			TunnelClient.OnListening += (stream, port) =>
			{
				foreach (var listener in _listeners.Values)
				{
					if (listener.TunnelStream != stream) continue;
					listener.Port = port;
					break;
				}
			};

			TunnelClient.OnAccepted += (stream, host, port) =>
			{
				var listener = ListenerForStream(stream);
				if (listener == null)
				{
					TunnelClient.Close(stream);
					return;
				}

				var conn = new Conn
				{
					Id = _nextConn++,
					TunnelStream = stream,
					Incoming = true,
					IsTunnel = true,
					State = ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting,
					ListenSocket = new HSteamListenSocket(listener.Id),
					RemoteAddress = host + ":" + port,
				};
				_conns[conn.Id] = conn;
				Raise(conn);
			};

			TunnelClient.OnData += (stream, bytes) =>
			{
				var conn = ConnForStream(stream);
				if (conn == null) return;
				lock (conn.Inbox)
				{
					if (conn.Inbox.Count > 4096) conn.Inbox.Dequeue();
					conn.Inbox.Enqueue(bytes);
					conn.Channels.Enqueue(0);
				}
			};

			TunnelClient.OnClosed += (stream, error) =>
			{
				var conn = ConnForStream(stream);
				if (conn != null) CloseLocally(conn, ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer, 0);
			};

			TunnelClient.EnsureConnectedAsync().Forget("tunnel warmup");
		}

		static Conn ConnForStream(int stream)
		{
			foreach (var conn in _conns.Values) if (conn.TunnelStream == stream) return conn;
			return null;
		}

		static Listener ListenerForStream(int stream)
		{
			foreach (var listener in _listeners.Values) if (listener.TunnelStream == stream) return listener;
			return null;
		}

		static string HostOf(SteamNetworkingIPAddr address)
		{
			if (address.IsIPv4())
			{
				var ip = address.GetIPv4();
				return ((ip >> 24) & 0xff) + "." + ((ip >> 16) & 0xff) + "." + ((ip >> 8) & 0xff) + "." + (ip & 0xff);
			}
			if (address.m_ipv6 == null || address.m_ipv6.Length < 16) return "127.0.0.1";
			var sb = new StringBuilder();
			for (int i = 0; i < 16; i += 2)
			{
				if (i > 0) sb.Append(':');
				sb.Append(address.m_ipv6[i].ToString("x2", CultureInfo.InvariantCulture));
				sb.Append(address.m_ipv6[i + 1].ToString("x2", CultureInfo.InvariantCulture));
			}
			return sb.ToString();
		}

		public static HSteamListenSocket CreateListenSocketIP(ref SteamNetworkingIPAddr localAddress, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			if (!SessionState.Initialized) return HSteamListenSocket.Invalid;
			Hook();

			var listener = new Listener
			{
				Id = _nextListener++,
				TunnelStream = TunnelClient.NextStream(),
				IsTunnel = true,
				Address = localAddress,
			};
			_listeners[listener.Id] = listener;
			TunnelClient.Listen(listener.TunnelStream, localAddress.m_port);
			return new HSteamListenSocket(listener.Id);
		}

		public static HSteamListenSocket CreateListenSocketP2P(int nLocalVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			if (!SessionState.Initialized) return HSteamListenSocket.Invalid;
			Hook();

			foreach (var existing in _listeners.Values)
			{
				if (existing.VirtualPort == nLocalVirtualPort && !existing.IsTunnel)
				{
					Debug.LogWarning("[Webworks] a P2P listen socket is already using virtual port " + nLocalVirtualPort);
					return HSteamListenSocket.Invalid;
				}
			}

			var listener = new Listener { Id = _nextListener++, VirtualPort = nLocalVirtualPort };
			_listeners[listener.Id] = listener;
			return new HSteamListenSocket(listener.Id);
		}

		public static HSteamNetConnection ConnectByIPAddress(ref SteamNetworkingIPAddr address, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			if (!SessionState.Initialized) return HSteamNetConnection.Invalid;
			Hook();

			var conn = new Conn
			{
				Id = _nextConn++,
				IsTunnel = true,
				TunnelStream = TunnelClient.NextStream(),
				State = ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting,
				RemoteAddress = HostOf(address) + ":" + address.m_port,
			};
			_conns[conn.Id] = conn;

			TunnelClient.Open(conn.TunnelStream, false, HostOf(address), address.m_port);
			Raise(conn);
			return new HSteamNetConnection(conn.Id);
		}

		public static HSteamNetConnection ConnectP2P(ref SteamNetworkingIdentity identityRemote, int nRemoteVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			if (!SessionState.Initialized) return HSteamNetConnection.Invalid;
			Hook();

			var peer = identityRemote.GetSteamID64();
			if (peer == 0) return HSteamNetConnection.Invalid;

			var conn = new Conn
			{
				Id = _nextConn++,
				Peer = peer,
				VirtualPort = nRemoteVirtualPort,
				State = ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting,
				RemoteIdentity = identityRemote,
			};
			_conns[conn.Id] = conn;

			Relay(peer, "{\"op\":\"connect\",\"vp\":" + nRemoteVirtualPort + ",\"cid\":" + conn.Id + "}");
			Raise(conn);
			return new HSteamNetConnection(conn.Id);
		}

		static Conn Find(HSteamNetConnection handle)
		{
			Conn conn;
			return _conns.TryGetValue(handle.m_HSteamNetConnection, out conn) ? conn : null;
		}

		public static EResult AcceptConnection(HSteamNetConnection hConn)
		{
			var conn = Find(hConn);
			if (conn == null) return EResult.k_EResultInvalidParam;
			if (!conn.Incoming || conn.State != ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting) return EResult.k_EResultInvalidState;

			conn.State = ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected;
			if (!conn.IsTunnel) Relay(conn.Peer, "{\"op\":\"accept\",\"cid\":" + conn.PeerId + ",\"pcid\":" + conn.Id + "}");
			Raise(conn);
			return EResult.k_EResultOK;
		}

		public static bool CloseConnection(HSteamNetConnection hPeer, int nReason, string pszDebug, bool bEnableLinger)
		{
			var conn = Find(hPeer);
			if (conn == null) return false;

			if (conn.IsTunnel) TunnelClient.Close(conn.TunnelStream);
			else Relay(conn.Peer, "{\"op\":\"close\",\"cid\":" + conn.PeerId + ",\"pcid\":" + conn.Id + ",\"reason\":" + nReason + "}");

			_conns.Remove(conn.Id);
			conn.State = ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer;
			Raise(conn);
			return true;
		}

		public static bool CloseListenSocket(HSteamListenSocket hSocket)
		{
			Listener listener;
			if (!_listeners.TryGetValue(hSocket.m_HSteamListenSocket, out listener)) return false;

			if (listener.IsTunnel) TunnelClient.Close(listener.TunnelStream);
			_listeners.Remove(listener.Id);

			var owned = new List<uint>();
			foreach (var conn in _conns.Values) if (conn.ListenSocket.m_HSteamListenSocket == listener.Id) owned.Add(conn.Id);
			foreach (var id in owned) CloseConnection(new HSteamNetConnection(id), 0, "listen socket closed", false);
			return true;
		}

		public static bool SetConnectionUserData(HSteamNetConnection hPeer, long nUserData)
		{
			var conn = Find(hPeer);
			if (conn == null) return false;
			conn.UserData = nUserData;
			return true;
		}

		public static long GetConnectionUserData(HSteamNetConnection hPeer)
		{
			var conn = Find(hPeer);
			return conn != null ? conn.UserData : -1;
		}

		public static void SetConnectionName(HSteamNetConnection hPeer, string pszName)
		{
			var conn = Find(hPeer);
			if (conn != null) conn.Name = pszName ?? "";
		}

		public static bool GetConnectionName(HSteamNetConnection hPeer, out string pszName, int nMaxLen)
		{
			var conn = Find(hPeer);
			pszName = conn != null ? conn.Name : "";
			return conn != null;
		}

		public static EResult SendMessageToConnection(HSteamNetConnection hConn, IntPtr pData, uint cbData, int nSendFlags, out long pOutMessageNumber)
		{
			pOutMessageNumber = 0;
			var conn = Find(hConn);
			if (conn == null) return EResult.k_EResultInvalidParam;
			if (conn.State != ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected) return EResult.k_EResultInvalidState;
			if (pData == IntPtr.Zero || cbData == 0) return EResult.k_EResultInvalidParam;
			if (cbData > 256 * 1024) return EResult.k_EResultLimitExceeded;

			var buffer = new byte[cbData];
			Marshal.Copy(pData, buffer, 0, (int)cbData);
			pOutMessageNumber = Send(conn, buffer);
			return EResult.k_EResultOK;
		}

		static long Send(Conn conn, byte[] data)
		{
			var number = conn.NextMessage++;
			if (conn.Loopback != null)
			{
				// Socket pairs deliver synchronously into the peer's inbox.
				lock (conn.Loopback.Inbox)
				{
					if (conn.Loopback.Inbox.Count > 4096) conn.Loopback.Inbox.Dequeue();
					conn.Loopback.Inbox.Enqueue(data);
					conn.Loopback.Channels.Enqueue(0);
				}
			}
			else if (conn.IsTunnel)
			{
				TunnelClient.Send(conn.TunnelStream, data, 0, data.Length);
			}
			else
			{
				Relay(conn.Peer, "{\"op\":\"data\",\"cid\":" + conn.PeerId + ",\"ch\":0,\"d\":\"" + Convert.ToBase64String(data) + "\"}");
			}
			return number;
		}

		public static EResult FlushMessagesOnConnection(HSteamNetConnection hConn)
		{
			return Find(hConn) != null ? EResult.k_EResultOK : EResult.k_EResultInvalidParam;
		}

		public static int ReceiveMessagesOnConnection(HSteamNetConnection hConn, IntPtr[] ppOutMessages, int nMaxMessages)
		{
			var conn = Find(hConn);
			if (conn == null || ppOutMessages == null || nMaxMessages <= 0) return 0;

			int delivered = 0;
			lock (conn.Inbox)
			{
				while (delivered < nMaxMessages && conn.Inbox.Count > 0 && delivered < ppOutMessages.Length)
				{
					var data = conn.Inbox.Dequeue();
					int channel;
					lock (conn.Channels) { channel = conn.Channels.Count > 0 ? conn.Channels.Dequeue() : 0; }

					var message = new SteamNetworkingMessage_t
					{
						m_pData = Marshal.AllocHGlobal(Math.Max(1, data.Length)),
						m_cbSize = data.Length,
						m_conn = hConn,
						m_identityPeer = conn.RemoteIdentity,
						m_nConnUserData = conn.UserData,
						m_usecTimeReceived = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds * 1000L,
						m_nMessageNumber = conn.NextMessage++,
						m_nChannel = channel,
						m_nFlags = SendReliable,
					};

					Marshal.Copy(data, 0, message.m_pData, data.Length);
					var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SteamNetworkingMessage_t)));
					Marshal.StructureToPtr(message, ptr, false);
					ppOutMessages[delivered++] = ptr;
				}
			}
			return delivered;
		}

		public static bool GetConnectionInfo(HSteamNetConnection hConn, out SteamNetConnectionInfo_t pInfo)
		{
			pInfo = default(SteamNetConnectionInfo_t);
			var conn = Find(hConn);
			if (conn == null) return false;

			pInfo.m_identityRemote = conn.RemoteIdentity;
			pInfo.m_nUserData = conn.UserData;
			pInfo.m_hListenSocket = conn.ListenSocket;
			pInfo.m_eState = conn.State;
			pInfo.m_szConnectionDescription = conn.RemoteAddress;
			pInfo.m_szEndDebug = "";
			if (!string.IsNullOrEmpty(conn.RemoteAddress))
			{
				var parts = conn.RemoteAddress.Split(':');
				ushort port;
				ushort.TryParse(parts.Length > 1 ? parts[1] : "0", out port);
				pInfo.m_addrRemote.m_port = port;
			}
			return true;
		}

		public static EResult GetConnectionRealTimeStatus(HSteamNetConnection hConn, ref SteamNetConnectionRealTimeStatus_t pStatus, int nLanes, ref SteamNetConnectionRealTimeLaneStatus_t pLanes)
		{
			var conn = Find(hConn);
			if (conn == null) return EResult.k_EResultInvalidParam;
			pStatus.m_eState = conn.State;
			pStatus.m_nPing = 0;
			pStatus.m_flConnectionQualityLocal = 1f;
			pStatus.m_flConnectionQualityRemote = 1f;
			return EResult.k_EResultOK;
		}

		public static int GetDetailedConnectionStatus(HSteamNetConnection hConn, out string pszBuf, int cbBuf)
		{
			var conn = Find(hConn);
			pszBuf = conn == null ? "no connection" : conn.State + " " + conn.RemoteAddress;
			return psZLength(pszBuf);
		}

		static int psZLength(string text) { return string.IsNullOrEmpty(text) ? 0 : text.Length + 1; }

		public static bool GetListenSocketAddress(HSteamListenSocket hSocket, out SteamNetworkingIPAddr address)
		{
			address = default(SteamNetworkingIPAddr);
			Listener listener;
			if (!_listeners.TryGetValue(hSocket.m_HSteamListenSocket, out listener)) return false;
			address.m_port = (ushort)listener.Port;
			address.SetIPv4(0, (ushort)listener.Port);
			return true;
		}

		public static bool GetIdentity(out SteamNetworkingIdentity pIdentity)
		{
			pIdentity = default(SteamNetworkingIdentity);
			ulong id;
			ulong.TryParse(SessionState.SteamId ?? "0", NumberStyles.Integer, CultureInfo.InvariantCulture, out id);
			pIdentity.SetSteamID64(id);
			return id != 0;
		}

		public static ESteamNetworkingAvailability InitAuthentication()
		{
			return ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current;
		}

		public static ESteamNetworkingAvailability GetAuthenticationStatus(out SteamNetAuthenticationStatus_t pDetails)
		{
			pDetails = new SteamNetAuthenticationStatus_t
			{
				m_eAvail = ESteamNetworkingAvailability.k_ESteamNetworkingAvailability_Current,
				m_debugMsg = "",
			};
			return pDetails.m_eAvail;
		}

		public static void RunCallbacks() { }

		static void Raise(Conn conn)
		{
			Hook();
			var info = new SteamNetConnectionInfo_t
			{
				m_identityRemote = conn.RemoteIdentity,
				m_nUserData = conn.UserData,
				m_hListenSocket = conn.ListenSocket,
				m_eState = conn.State,
				m_szConnectionDescription = conn.RemoteAddress,
			};
			CallbackBus<SteamNetConnectionStatusChangedCallback_t>.Fire(new SteamNetConnectionStatusChangedCallback_t
			{
				m_hConn = new HSteamNetConnection(conn.Id),
				m_info = info,
				m_eOldState = conn.State,
			});
		}

		static void CloseLocally(Conn conn, ESteamNetworkingConnectionState state, int reason)
		{
			_conns.Remove(conn.Id);
			conn.State = state;
			Raise(conn);
		}

		static void Relay(ulong to, string payload)
		{
			if (SNetState.Realtime == null) return;
			SNetState.Realtime.SendAsync("{\"type\":\"Relay\",\"to\":\"" + to + "\",\"payload\":" + payload + "}").Forget("sockets relay");
		}

		internal static void DispatchRelay(string json)
		{
			Hook();
			var d = WebworksJson.Obj(json);
			var payload = d != null ? WebworksJson.Map(d, "payload") : null;
			if (payload == null) return;

			ulong sender = WebworksJson.Ulong(d, "from");
			var op = WebworksJson.Str(payload, "op");

			// Legacy ISteamNetworking sockets share this relay with their own op vocabulary.
			if (SteamNetworking.TryDispatchLegacyRelay(sender, payload)) return;

			if (op == "connect")
			{
				var virtualPort = WebworksJson.Int(payload, "vp");
				var peerId = (uint)WebworksJson.Int(payload, "cid");

				Listener listener = null;
				foreach (var candidate in _listeners.Values)
					if (!candidate.IsTunnel && candidate.VirtualPort == virtualPort) { listener = candidate; break; }

				if (listener == null)
				{
					Relay(sender, "{\"op\":\"close\",\"cid\":0,\"pcid\":" + peerId + ",\"reason\":0}");
					return;
				}

				var conn = new Conn
				{
					Id = _nextConn++,
					Peer = sender,
					PeerId = peerId,
					VirtualPort = virtualPort,
					Incoming = true,
					State = ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting,
					ListenSocket = new HSteamListenSocket(listener.Id),
				};
				conn.RemoteIdentity.SetSteamID64(sender);
				_conns[conn.Id] = conn;
				Raise(conn);
				return;
			}

			if (op == "accept")
			{
				var conn = Find(new HSteamNetConnection((uint)WebworksJson.Int(payload, "cid")));
				if (conn == null) return;
				conn.PeerId = (uint)WebworksJson.Int(payload, "pcid");
				conn.State = ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected;
				Raise(conn);
				return;
			}

			if (op == "data")
			{
				var conn = Find(new HSteamNetConnection((uint)WebworksJson.Int(payload, "cid")));
				if (conn == null) return;

				byte[] data;
				try { data = Convert.FromBase64String(WebworksJson.Str(payload, "d") ?? ""); }
				catch (FormatException) { return; }

				lock (conn.Inbox)
				{
					if (conn.Inbox.Count > 4096) conn.Inbox.Dequeue();
					conn.Inbox.Enqueue(data);
					conn.Channels.Enqueue(WebworksJson.Int(payload, "ch"));
				}
				return;
			}

			if (op == "close")
			{
				Conn conn = null;
				var id = (uint)WebworksJson.Int(payload, "cid");
				if (id != 0) conn = Find(new HSteamNetConnection(id));
				if (conn == null)
				{
					var theirs = (uint)WebworksJson.Int(payload, "pcid");
					foreach (var candidate in _conns.Values)
						if (candidate.Peer == sender && candidate.PeerId == theirs) { conn = candidate; break; }
				}
				if (conn != null) CloseLocally(conn, ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer, WebworksJson.Int(payload, "reason"));
			}
		}

		internal static void DispatchPeerDisconnected(string json)
		{
			var d = WebworksJson.Obj(json);
			if (d == null) return;
			ulong sender = WebworksJson.Ulong(d, "from");
			if (sender == 0) return;

			foreach (var conn in new List<Conn>(_conns.Values))
				if (conn.Peer == sender) CloseLocally(conn, ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally, 0);
		}

		public static void Reset()
		{
			_conns.Clear();
			_listeners.Clear();
			TunnelClient.Reset();
			_hooked = false;
		}
	}

	public struct SteamNetConnectionStatusChangedCallback_t
	{
		public HSteamNetConnection m_hConn;
		public SteamNetConnectionInfo_t m_info;
		public ESteamNetworkingConnectionState m_eOldState;
	}
}
