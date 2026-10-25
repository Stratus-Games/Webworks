using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	// The old ISteamNetworking socket API. Direct sockets use the raw tunnel; P2P sockets
	// use the same realtime relay the newer sockets interface uses, with a small
	// "legacy-*" op vocabulary so the two layers never confuse each other's messages.
	public static partial class SteamNetworking
	{
		class LegacySocket
		{
			public uint Id;
			public int Stream = -1;
			public uint ListenId;
			public bool IsRelay;
			public bool Incoming;
			public bool Connected;
			public bool Closed;
			public ulong Peer;
			public uint PeerSocketId;
			public int VirtualPort;
			public string RemoteHost = "";
			public ushort RemotePort;
			public readonly Queue<byte[]> Inbox = new Queue<byte[]>();
		}

		class LegacyListener
		{
			public uint Id;
			public int Stream = -1;
			public int VirtualPort;
			public ushort Port;
			public bool IsTunnel;
			public readonly Queue<uint> Pending = new Queue<uint>();
		}

		static readonly Dictionary<uint, LegacySocket> _sockets = new Dictionary<uint, LegacySocket>();
		static readonly Dictionary<uint, LegacyListener> _listeners = new Dictionary<uint, LegacyListener>();
		static uint _nextSocketId = 1;
		static uint _nextListenId = 1;
		static bool _legacyHooked;

		static void EnsureLegacyHooked()
		{
			if (_legacyHooked) return;
			_legacyHooked = true;

			TunnelClient.OnOpened += (stream, protocol, ok) =>
			{
				var socket = LegacySocketForStream(stream);
				if (socket == null) return;
				socket.Connected = ok;
			};

			TunnelClient.OnAccepted += (stream, host, port) =>
			{
				var listener = LegacyListenerForStream(stream);
				if (listener == null) return;

				var socket = new LegacySocket
				{
					Id = _nextSocketId++,
					Stream = stream,
					ListenId = listener.Id,
					Incoming = true,
					Connected = true,
					RemoteHost = host,
					RemotePort = port,
				};
				_sockets[socket.Id] = socket;
				listener.Pending.Enqueue(socket.Id);
			};

			TunnelClient.OnData += (stream, bytes) =>
			{
				var socket = LegacySocketForStream(stream);
				if (socket == null) return;
				lock (socket.Inbox)
				{
					if (socket.Inbox.Count > 4096) socket.Inbox.Dequeue();
					socket.Inbox.Enqueue(bytes);
				}
			};

			TunnelClient.OnClosed += (stream, error) =>
			{
				var socket = LegacySocketForStream(stream);
				if (socket != null) socket.Closed = true;
			};

			TunnelClient.EnsureConnectedAsync().Forget("legacy socket warmup");
		}

		static LegacySocket LegacySocketForStream(int stream)
		{
			foreach (var socket in _sockets.Values) if (socket.Stream == stream) return socket;
			return null;
		}

		static LegacyListener LegacyListenerForStream(int stream)
		{
			foreach (var listener in _listeners.Values) if (listener.Stream == stream) return listener;
			return null;
		}

		static LegacySocket Socket(SNetSocket_t handle)
		{
			LegacySocket socket;
			return _sockets.TryGetValue(handle.m_SNetSocket, out socket) ? socket : null;
		}

		static void RelayLegacy(ulong to, string payload)
		{
			if (SNetState.Realtime == null) return;
			SNetState.Realtime.SendAsync("{\"type\":\"Relay\",\"to\":\"" + to.ToString(CultureInfo.InvariantCulture) + "\",\"payload\":" + payload + "}").Forget("legacy socket relay");
		}

		public static SNetListenSocket_t CreateListenSocket(int nVirtualP2PPort, SteamIPAddress_t nIP, ushort nPort, bool bAllowUseOfPacketRelay)
		{
			if (!SessionState.Initialized) return new SNetListenSocket_t(0);
			EnsureLegacyHooked();

			var listener = new LegacyListener
			{
				Id = _nextListenId++,
				VirtualPort = nVirtualP2PPort,
				Stream = TunnelClient.NextStream(),
				Port = nPort,
				IsTunnel = true,
			};
			_listeners[listener.Id] = listener;
			TunnelClient.Listen(listener.Stream, nPort);
			return new SNetListenSocket_t(listener.Id);
		}

		public static SNetSocket_t CreateP2PConnectionSocket(CSteamID steamIDTarget, int nVirtualPort, int nTimeoutSec, bool bAllowUseOfPacketRelay)
		{
			if (!SessionState.Initialized || steamIDTarget.m_SteamID == 0) return new SNetSocket_t(0);
			EnsureLegacyHooked();

			var socket = new LegacySocket
			{
				Id = _nextSocketId++,
				IsRelay = true,
				Peer = steamIDTarget.m_SteamID,
				VirtualPort = nVirtualPort,
				PeerSocketId = _nextSocketId++,
			};
			_sockets[socket.Id] = socket;
			RelayLegacy(socket.Peer, "{\"op\":\"legacy-connect\",\"vp\":" + nVirtualPort + ",\"sid\":" + socket.PeerSocketId + "}");
			return new SNetSocket_t(socket.Id);
		}

		public static SNetSocket_t CreateConnectionSocket(SteamIPAddress_t nIP, ushort nPort, int nTimeoutSec)
		{
			if (!SessionState.Initialized) return new SNetSocket_t(0);
			EnsureLegacyHooked();

			var host = "127.0.0.1";
			try
			{
				var address = nIP.ToIPAddress();
				if (address != null) host = address.ToString();
			}
			catch
			{
				host = "127.0.0.1";
			}

			var socket = new LegacySocket
			{
				Id = _nextSocketId++,
				Stream = TunnelClient.NextStream(),
				RemoteHost = host,
				RemotePort = nPort,
			};
			_sockets[socket.Id] = socket;
			TunnelClient.Open(socket.Stream, false, host, nPort);
			return new SNetSocket_t(socket.Id);
		}

		public static bool DestroySocket(SNetSocket_t hSocket, bool bNotifyRemoteEnd)
		{
			var socket = Socket(hSocket);
			if (socket == null) return false;

			if (socket.IsRelay)
			{
				if (bNotifyRemoteEnd && socket.Peer != 0)
					RelayLegacy(socket.Peer, "{\"op\":\"legacy-close\",\"sid\":" + socket.PeerSocketId + "}");
			}
			else if (socket.Stream >= 0)
			{
				TunnelClient.Close(socket.Stream);
			}

			_sockets.Remove(socket.Id);
			socket.Closed = true;
			return true;
		}

		public static bool DestroyListenSocket(SNetListenSocket_t hSocket, bool bNotifyRemoteEnd)
		{
			LegacyListener listener;
			if (!_listeners.TryGetValue(hSocket.m_SNetListenSocket, out listener)) return false;

			if (listener.Stream >= 0) TunnelClient.Close(listener.Stream);
			_listeners.Remove(listener.Id);

			foreach (var socket in new List<LegacySocket>(_sockets.Values))
				if (socket.ListenId == listener.Id)
					DestroySocket(new SNetSocket_t(socket.Id), bNotifyRemoteEnd);
			return true;
		}

		public static bool SendDataOnSocket(SNetSocket_t hSocket, byte[] pubData, uint cubData, bool bReliable)
		{
			var socket = Socket(hSocket);
			if (socket == null || socket.Closed || pubData == null || cubData == 0) return false;

			byte[] payload = pubData;
			if (cubData < pubData.Length)
			{
				payload = new byte[cubData];
				Array.Copy(pubData, payload, cubData);
			}

			if (socket.IsRelay)
			{
				RelayLegacy(socket.Peer, "{\"op\":\"legacy-data\",\"sid\":" + socket.PeerSocketId + ",\"d\":\"" + Convert.ToBase64String(payload) + "\"}");
			}
			else
			{
				TunnelClient.Send(socket.Stream, payload, 0, payload.Length);
			}
			return true;
		}

		public static bool IsDataAvailableOnSocket(SNetSocket_t hSocket, out uint pcubMsgSize)
		{
			pcubMsgSize = 0;
			var socket = Socket(hSocket);
			if (socket == null) return false;

			lock (socket.Inbox)
			{
				if (socket.Inbox.Count == 0) return false;
				pcubMsgSize = (uint)socket.Inbox.Peek().Length;
				return true;
			}
		}

		public static bool RetrieveDataFromSocket(SNetSocket_t hSocket, byte[] pubDest, uint cubDest, out uint pcubMsgSize)
		{
			pcubMsgSize = 0;
			var socket = Socket(hSocket);
			if (socket == null || pubDest == null) return false;

			lock (socket.Inbox)
			{
				if (socket.Inbox.Count == 0) return false;
				var data = socket.Inbox.Dequeue();
				var count = (int)Math.Min(Math.Min(data.Length, pubDest.Length), cubDest);
				Array.Copy(data, pubDest, count);
				pcubMsgSize = (uint)count;
				return true;
			}
		}

		public static bool IsDataAvailable(SNetListenSocket_t hListenSocket, out uint pcubMsgSize, out SNetSocket_t phSocket)
		{
			pcubMsgSize = 0;
			phSocket = new SNetSocket_t(0);

			LegacyListener listener;
			if (!_listeners.TryGetValue(hListenSocket.m_SNetListenSocket, out listener) || listener.Pending.Count == 0) return false;

			phSocket = new SNetSocket_t(listener.Pending.Peek());
			LegacySocket socket;
			if (_sockets.TryGetValue(phSocket.m_SNetSocket, out socket))
			{
				lock (socket.Inbox) pcubMsgSize = socket.Inbox.Count > 0 ? (uint)socket.Inbox.Peek().Length : 0;
			}
			return true;
		}

		public static bool RetrieveData(SNetListenSocket_t hListenSocket, byte[] pubDest, uint cubDest, out uint pcubMsgSize, out SNetSocket_t phSocket)
		{
			pcubMsgSize = 0;
			phSocket = new SNetSocket_t(0);

			LegacyListener listener;
			if (!_listeners.TryGetValue(hListenSocket.m_SNetListenSocket, out listener) || listener.Pending.Count == 0) return false;

			var id = listener.Pending.Peek();
			LegacySocket socket;
			if (!_sockets.TryGetValue(id, out socket)) { listener.Pending.Dequeue(); return false; }

			lock (socket.Inbox)
			{
				if (socket.Inbox.Count > 0)
				{
					if (pubDest == null) return false;
					var data = socket.Inbox.Dequeue();
					var count = (int)Math.Min(Math.Min(data.Length, pubDest.Length), cubDest);
					Array.Copy(data, pubDest, count);
					pcubMsgSize = (uint)count;
				}
			}

			listener.Pending.Dequeue();
			phSocket = new SNetSocket_t(id);
			return true;
		}

		public static bool GetSocketInfo(SNetSocket_t hSocket, out CSteamID pSteamIDRemote, out int peSocketStatus, out SteamIPAddress_t punIPRemote, out ushort punPortRemote)
		{
			pSteamIDRemote = CSteamID.Nil;
			peSocketStatus = (int)ESNetSocketState.k_ESNetSocketStateInvalid;
			punIPRemote = default(SteamIPAddress_t);
			punPortRemote = 0;

			var socket = Socket(hSocket);
			if (socket == null) return false;

			pSteamIDRemote = socket.IsRelay ? new CSteamID(socket.Peer) : CSteamID.Nil;
			peSocketStatus = (int)(socket.Closed
				? ESNetSocketState.k_ESNetSocketStateRemoteEndDisconnected
				: socket.Connected
					? ESNetSocketState.k_ESNetSocketStateConnected
					: ESNetSocketState.k_ESNetSocketStateInitiated);

			punPortRemote = socket.RemotePort;
			if (!string.IsNullOrEmpty(socket.RemoteHost))
			{
				try { punIPRemote = new SteamIPAddress_t(System.Net.IPAddress.Parse(socket.RemoteHost)); }
				catch { }
			}
			return true;
		}

		public static bool GetListenSocketInfo(SNetListenSocket_t hListenSocket, out SteamIPAddress_t pnIP, out ushort pnPort)
		{
			pnIP = default(SteamIPAddress_t);
			pnPort = 0;

			LegacyListener listener;
			if (!_listeners.TryGetValue(hListenSocket.m_SNetListenSocket, out listener)) return false;
			pnPort = listener.Port;
			return true;
		}

		public static ESNetSocketConnectionType GetSocketConnectionType(SNetSocket_t hSocket)
		{
			var socket = Socket(hSocket);
			if (socket == null || !socket.Connected) return ESNetSocketConnectionType.k_ESNetSocketConnectionTypeNotConnected;
			return socket.IsRelay
				? ESNetSocketConnectionType.k_ESNetSocketConnectionTypeUDPRelay
				: ESNetSocketConnectionType.k_ESNetSocketConnectionTypeUDP;
		}

		public static int GetMaxPacketSize(SNetSocket_t hSocket)
		{
			return Socket(hSocket) != null ? 512 * 1024 : 0;
		}

		// Called from the newer sockets layer for ops it does not recognise.
		internal static bool TryDispatchLegacyRelay(ulong sender, Dictionary<string, object> payload)
		{
			if (payload == null) return false;
			var op = WebworksJson.Str(payload, "op");
			if (op == null || !op.StartsWith("legacy-", StringComparison.Ordinal)) return false;

			EnsureLegacyHooked();
			var sid = (uint)WebworksJson.Int(payload, "sid");

			if (op == "legacy-connect")
			{
				var virtualPort = WebworksJson.Int(payload, "vp");
				LegacyListener listener = null;
				foreach (var candidate in _listeners.Values)
					if (candidate.VirtualPort == virtualPort) { listener = candidate; break; }

				if (listener == null)
				{
					RelayLegacy(sender, "{\"op\":\"legacy-close\",\"sid\":" + sid + "}");
					return true;
				}

				var socket = new LegacySocket
				{
					Id = _nextSocketId++,
					IsRelay = true,
					Incoming = true,
					Connected = true,
					Peer = sender,
					PeerSocketId = sid,
					VirtualPort = virtualPort,
					ListenId = listener.Id,
				};
				_sockets[socket.Id] = socket;
				listener.Pending.Enqueue(socket.Id);
				RelayLegacy(sender, "{\"op\":\"legacy-accept\",\"sid\":" + sid + ",\"psid\":" + socket.Id + "}");
				return true;
			}

			LegacySocket target = null;
			foreach (var candidate in _sockets.Values)
				if (candidate.PeerSocketId == sid) { target = candidate; break; }

			if (op == "legacy-accept")
			{
				if (target != null) target.Connected = true;
				return true;
			}

			if (op == "legacy-data")
			{
				if (target == null) return true;
				byte[] data;
				try { data = Convert.FromBase64String(WebworksJson.Str(payload, "d") ?? ""); }
				catch (FormatException) { return true; }

				lock (target.Inbox)
				{
					if (target.Inbox.Count > 4096) target.Inbox.Dequeue();
					target.Inbox.Enqueue(data);
				}
				return true;
			}

			if (op == "legacy-close")
			{
				if (target != null) target.Closed = true;
				return true;
			}

			return true;
		}

		internal static void ResetLegacySockets()
		{
			_sockets.Clear();
			_listeners.Clear();
			_legacyHooked = false;
		}
	}
}
