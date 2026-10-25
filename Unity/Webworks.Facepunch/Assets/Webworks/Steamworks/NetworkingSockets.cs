#pragma warning disable CS0067
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks.Data;
using Webworks.Internal;

namespace Steamworks.Data
{
	public struct NetAddress
	{
		public IPAddress Address;
		public ushort Port;

		public bool IsIPv4 { get { return Address != null && Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork; } }
		public bool IsLocalHost { get { return Address != null && IPAddress.IsLoopback(Address); } }
		public bool IsIPv6AllZeros { get { return Address != null && Address.Equals(IPAddress.IPv6Any); } }

		public static NetAddress From(IPAddress address, ushort port) { return new NetAddress { Address = address, Port = port }; }
		public static NetAddress From(string address, ushort port)
		{
			IPAddress parsed;
			IPAddress.TryParse(address, out parsed);
			return new NetAddress { Address = parsed, Port = port };
		}
		public static NetAddress AnyIp(ushort port) { return From(IPAddress.IPv6Any, port); }
		public static NetAddress LocalHost(ushort port) { return From(IPAddress.Loopback, port); }
		public static NetAddress Cleared { get { return new NetAddress(); } }

		public override string ToString() { return (Address == null ? "" : Address.ToString()) + ":" + Port; }
	}

	public struct NetIdentity
	{
		internal ulong _steam;
		internal NetAddress _address;
		internal bool _isAddress;

		public static NetIdentity LocalHost { get { return new NetIdentity { _isAddress = true, _address = NetAddress.LocalHost(0) }; } }

		public bool IsSteamId { get { return _steam != 0; } }
		public bool IsIpAddress { get { return _isAddress; } }
		public bool IsLocalHost { get { return _isAddress && _address.IsLocalHost; } }
		public SteamId SteamId { get { return _steam; } }
		public NetAddress Address { get { return _address; } }

		public static implicit operator NetIdentity(SteamId value) { return new NetIdentity { _steam = value.Value }; }
		public static implicit operator NetIdentity(NetAddress address) { return new NetIdentity { _isAddress = true, _address = address }; }

		public override string ToString() { return _isAddress ? _address.ToString() : _steam.ToString(); }
	}

	public struct ConnectionInfo
	{
		public NetIdentity Identity;
		public NetAddress Address;
		public ConnectionState State;
		public NetConnectionEnd EndReason;
	}

	public struct Connection : IEquatable<Connection>
	{
		public uint Id;

		public string ConnectionName { get { return SteamNetworkingSockets.GetName(Id); } set { SteamNetworkingSockets.SetName(Id, value); } }
		public long UserData { get { return SteamNetworkingSockets.GetUserData(Id); } set { SteamNetworkingSockets.SetUserData(Id, value); } }

		public Result Accept() { return SteamNetworkingSockets.AcceptConnection(Id); }
		public bool Close(bool linger = false, int reasonCode = 0, string debugString = "Closing Connection") { return SteamNetworkingSockets.CloseConnection(Id, reasonCode); }
		public Result Flush() { return Result.OK; }
		public string DetailedStatus() { return ""; }

		public Result SendMessage(IntPtr ptr, int size, SendType sendType = SendType.Reliable)
		{
			if (ptr == IntPtr.Zero || size < 0) return Result.InvalidParam;
			var bytes = new byte[size];
			Marshal.Copy(ptr, bytes, 0, size);
			return SteamNetworkingSockets.SendOnConnection(Id, bytes, 0, size);
		}

		public Result SendMessage(byte[] data, SendType sendType = SendType.Reliable)
		{
			return SteamNetworkingSockets.SendOnConnection(Id, data, 0, data == null ? 0 : data.Length);
		}

		public Result SendMessage(byte[] data, int offset, int length, SendType sendType = SendType.Reliable)
		{
			return SteamNetworkingSockets.SendOnConnection(Id, data, offset, length);
		}

		public Result SendMessage(string text, SendType sendType = SendType.Reliable)
		{
			var bytes = Encoding.UTF8.GetBytes(text ?? "");
			return SteamNetworkingSockets.SendOnConnection(Id, bytes, 0, bytes.Length);
		}

		public override string ToString() { return Id.ToString(); }
		public override int GetHashCode() { return Id.GetHashCode(); }
		public override bool Equals(object obj) { return obj is Connection && ((Connection)obj).Id == Id; }
		public bool Equals(Connection other) { return Id == other.Id; }
		public static bool operator ==(Connection a, Connection b) { return a.Id == b.Id; }
		public static bool operator !=(Connection a, Connection b) { return a.Id != b.Id; }
	}

	public struct Socket
	{
		internal int _virtualPort;
		public SocketManager Manager;

		public bool Close() { return Manager != null && Manager.Close(); }
		public override string ToString() { return _virtualPort.ToString(); }
	}

	public struct NetPingLocation
	{
		public static NetPingLocation? TryParseFromString(string str) { return null; }
		public int EstimatePingTo(NetPingLocation target) { return 0; }
	}
}

namespace Steamworks
{
	public interface ISocketManager
	{
		void OnConnecting(Connection connection, ConnectionInfo data);
		void OnConnected(Connection connection, ConnectionInfo data);
		void OnDisconnected(Connection connection, ConnectionInfo data);
		void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel);
	}

	public interface IConnectionManager
	{
		void OnConnecting(ConnectionInfo info);
		void OnConnected(ConnectionInfo info);
		void OnDisconnected(ConnectionInfo info);
		void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel);
	}

	public class SocketManager : ISocketManager
	{
		public ISocketManager Interface { get; set; }
		public Socket Socket;
		public List<Connection> Connecting = new List<Connection>();
		public List<Connection> Connected = new List<Connection>();
		internal int VirtualPort;

		public SocketManager() { Interface = this; }

		public bool Close() { return SteamNetworkingSockets.CloseSocket(this); }

		public void Receive(int bufferSize = 32) { SteamNetworkingSockets.ReceiveForSocket(this, bufferSize); }

		public virtual void OnConnecting(Connection connection, ConnectionInfo data) { connection.Accept(); }
		public virtual void OnConnected(Connection connection, ConnectionInfo data) { }
		public virtual void OnDisconnected(Connection connection, ConnectionInfo data) { }
		public virtual void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel) { }
	}

	public class ConnectionManager : IConnectionManager
	{
		public IConnectionManager Interface { get; set; }
		public Connection Connection;
		public ConnectionInfo ConnectionInfo;
		public bool Connected { get; internal set; }
		public bool Connecting { get; internal set; }

		public ConnectionManager() { Interface = this; }

		public string ConnectionName { get { return Connection.ConnectionName; } set { Connection.ConnectionName = value; } }
		public long UserData { get { return Connection.UserData; } set { Connection.UserData = value; } }

		public bool Close(bool linger = false, int reasonCode = 0, string debugString = "Closing Connection") { return SteamNetworkingSockets.CloseConnection(Connection.Id, reasonCode); }

		public void Receive(int bufferSize = 32) { SteamNetworkingSockets.ReceiveForConnection(this, bufferSize); }

		public virtual void OnConnecting(ConnectionInfo info) { }
		public virtual void OnConnected(ConnectionInfo info) { }
		public virtual void OnDisconnected(ConnectionInfo info) { }
		public virtual void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel) { }
	}

	public static class SteamNetworkingSockets
	{
		public static event Action<Connection, ConnectionInfo> OnConnectionStatusChanged;

		class Msg { public byte[] Data; public long Number; public int Channel; public long Time; }

		class Conn
		{
			public uint Id;
			public ulong Peer;
			public uint PeerId;
			public int VirtualPort;
			public bool Incoming;
			public ConnectionState State;
			public SocketManager Server;
			public ConnectionManager Client;
			public string Name = "";
			public long UserData;
			public long NextMessage = 1;
			public int TunnelStream;
			public readonly Queue<Msg> Inbox = new Queue<Msg>();
		}

		static readonly Dictionary<uint, Conn> _conns = new Dictionary<uint, Conn>();
		static readonly Dictionary<int, SocketManager> _listeners = new Dictionary<int, SocketManager>();
		static uint _nextId = 1;


		public static T CreateRelaySocket<T>(int virtualport = 0) where T : SocketManager, new()
		{
			if (!SteamClient.IsValid) return null;
			if (_listeners.ContainsKey(virtualport))
			{
				Debug.LogWarning("[Webworks] a relay socket is already listening on virtual port " + virtualport);
				return null;
			}

			var manager = new T();
			manager.VirtualPort = virtualport;
			manager.Socket = new Socket { _virtualPort = virtualport, Manager = manager };
			_listeners[virtualport] = manager;
			return manager;
		}

		public static T ConnectRelay<T>(SteamId serverId, int virtualport = 0) where T : ConnectionManager, new()
		{
			if (!SteamClient.IsValid || SteamClient.Realtime == null) return null;

			var manager = new T();
			var conn = new Conn { Id = _nextId++, Peer = serverId.Value, VirtualPort = virtualport, State = ConnectionState.Connecting, Client = manager };
			_conns[conn.Id] = conn;

			manager.Connection = new Connection { Id = conn.Id };
			manager.Connecting = true;
			manager.ConnectionInfo = InfoOf(conn);

			SendRelay(conn.Peer, "{\"op\":\"connect\",\"vp\":" + virtualport + ",\"cid\":" + conn.Id + "}");

			Callbacks.Raise<ConnectionInfo>(manager.Interface.OnConnecting, manager.ConnectionInfo);
			RaiseStatus(manager.Connection, manager.ConnectionInfo);
			return manager;
		}

		static readonly Dictionary<int, Conn> _tunnelConns = new Dictionary<int, Conn>();
		static readonly Dictionary<int, SocketManager> _tunnelServers = new Dictionary<int, SocketManager>();
		static bool _tunnelHooked;

		static void HookTunnel()
		{
			if (_tunnelHooked) return;
			_tunnelHooked = true;

			TunnelClient.OnData += (stream, bytes) =>
			{
				Conn conn;
				if (!_tunnelConns.TryGetValue(stream, out conn)) return;
				if (conn.Inbox.Count >= 20000) conn.Inbox.Dequeue();
				conn.Inbox.Enqueue(new Msg { Data = bytes, Number = conn.NextMessage++, Channel = 0, Time = NowMicroseconds() });
			};

			TunnelClient.OnOpened += (stream, protocol, ok) =>
			{
				Conn conn;
				if (!_tunnelConns.TryGetValue(stream, out conn) || conn.Client == null) return;
				conn.State = ConnectionState.Connected;
				var manager = conn.Client;
				manager.Connecting = false;
				manager.Connected = true;
				manager.ConnectionInfo = InfoOf(conn);
				Callbacks.RaiseNow<ConnectionInfo>(manager.Interface.OnConnected, manager.ConnectionInfo);
				RaiseStatus(manager.Connection, manager.ConnectionInfo);
			};

			TunnelClient.OnClosed += (stream, error) =>
			{
				Conn conn;
				if (!_tunnelConns.TryGetValue(stream, out conn)) return;
				CloseLocally(conn, NetConnectionEnd.Misc_SteamConnectivity);
			};

			TunnelClient.OnListening += (stream, port) =>
			{
				SocketManager manager;
				if (_tunnelServers.TryGetValue(stream, out manager)) manager.Socket = new Socket { _virtualPort = port, Manager = manager };
			};

			TunnelClient.OnAccepted += (stream, host, port) =>
			{
				SocketManager manager;
				if (!_tunnelServers.TryGetValue(stream, out manager)) { TunnelClient.Close(stream); return; }

				var conn = new Conn
				{
					Id = _nextId++,
					Incoming = true,
					State = ConnectionState.Connecting,
					Server = manager,
					TunnelStream = stream,
				};
				_tunnelConns[stream] = conn;
				_conns[conn.Id] = conn;

				var connection = new Connection { Id = conn.Id };
				var info = InfoOf(conn);
				manager.Connecting.Add(connection);
				Callbacks.RaiseNow<Connection, ConnectionInfo>(manager.Interface.OnConnecting, connection, info);
				RaiseStatus(connection, info);
			};

			TunnelClient.EnsureConnectedAsync().Forget("tunnel warmup");
		}

		public static T CreateNormalSocket<T>(NetAddress address) where T : SocketManager, new()
		{
			if (!SteamClient.IsValid) return null;
			HookTunnel();

			var manager = new T();
			var stream = TunnelClient.NextStream();
			manager.VirtualPort = address.Port;
			manager.Socket = new Socket { _virtualPort = address.Port, Manager = manager };
			_tunnelServers[stream] = manager;
			TunnelClient.Listen(stream, address.Port);
			return manager;
		}

		public static T ConnectNormal<T>(NetAddress address) where T : ConnectionManager, new()
		{
			if (!SteamClient.IsValid) return null;
			HookTunnel();

			var manager = new T();
			var stream = TunnelClient.NextStream();
			var conn = new Conn
			{
				Id = _nextId++,
				State = ConnectionState.Connecting,
				Client = manager,
				TunnelStream = stream,
			};
			_tunnelConns[stream] = conn;
			_conns[conn.Id] = conn;

			manager.Connection = new Connection { Id = conn.Id };
			manager.Connecting = true;
			manager.ConnectionInfo = InfoOf(conn);

			TunnelClient.Open(stream, false, address.Address != null ? address.Address.ToString() : "127.0.0.1", address.Port);
			Callbacks.Raise<ConnectionInfo>(manager.Interface.OnConnecting, manager.ConnectionInfo);
			RaiseStatus(manager.Connection, manager.ConnectionInfo);
			return manager;
		}


		internal static string GetName(uint id) { Conn c; return _conns.TryGetValue(id, out c) ? c.Name : ""; }
		internal static void SetName(uint id, string name) { Conn c; if (_conns.TryGetValue(id, out c)) c.Name = name ?? ""; }
		internal static long GetUserData(uint id) { Conn c; return _conns.TryGetValue(id, out c) ? c.UserData : 0; }
		internal static void SetUserData(uint id, long value) { Conn c; if (_conns.TryGetValue(id, out c)) c.UserData = value; }

		internal static Result AcceptConnection(uint id)
		{
			Conn c;
			if (!_conns.TryGetValue(id, out c) || !c.Incoming || c.State != ConnectionState.Connecting) return Result.InvalidState;

			c.State = ConnectionState.Connected;
			SendRelay(c.Peer, "{\"op\":\"accept\",\"cid\":" + c.PeerId + ",\"pcid\":" + c.Id + "}");

			var connection = new Connection { Id = c.Id };
			var info = InfoOf(c);
			var manager = c.Server;
			manager.Connecting.Remove(connection);
			manager.Connected.Add(connection);
			Callbacks.Raise<Connection, ConnectionInfo>(manager.Interface.OnConnected, connection, info);
			RaiseStatus(connection, info);
			return Result.OK;
		}

		internal static bool CloseConnection(uint id, int reason)
		{
			Conn c;
			if (!_conns.TryGetValue(id, out c)) return false;

			_conns.Remove(id);
			if (c.TunnelStream != 0)
			{
				_tunnelConns.Remove(c.TunnelStream);
				TunnelClient.Close(c.TunnelStream);
			}
			else SendRelay(c.Peer, "{\"op\":\"close\",\"cid\":" + c.PeerId + ",\"pcid\":" + c.Id + ",\"reason\":" + reason + "}");
			Detach(c);
			return true;
		}

		internal static bool CloseSocket(SocketManager manager)
		{
			var mine = new List<uint>();
			foreach (var c in _conns.Values) if (c.Server == manager) mine.Add(c.Id);
			foreach (var id in mine) CloseConnection(id, 0);
			return _listeners.Remove(manager.VirtualPort);
		}

		internal static Result SendOnConnection(uint id, byte[] data, int offset, int length)
		{
			Conn c;
			if (!_conns.TryGetValue(id, out c)) return Result.NoConnection;
			if (c.State != ConnectionState.Connected) return Result.InvalidState;
			if (data == null || offset < 0 || length < 0 || offset + length > data.Length) return Result.InvalidParam;
			if (length > 512 * 1024) return Result.LimitExceeded;

			if (c.TunnelStream != 0)
			{
				TunnelClient.Send(c.TunnelStream, data, offset, length);
				return Result.OK;
			}

			SendRelay(c.Peer, "{\"op\":\"data\",\"cid\":" + c.PeerId + ",\"ch\":0,\"d\":\"" + Convert.ToBase64String(data, offset, length) + "\"}");
			return Result.OK;
		}

		internal static int ReceiveForSocket(SocketManager manager, int bufferSize)
		{
			int delivered = 0;
			foreach (var c in new List<Conn>(_conns.Values))
			{
				if (c.Server != manager) continue;
				while (delivered < bufferSize && c.Inbox.Count > 0)
				{
					var m = c.Inbox.Dequeue();
					SteamId sender = c.Peer;
					NetIdentity identity = sender;
					var connection = new Connection { Id = c.Id };
					Deliver(m, (ptr, size) => manager.Interface.OnMessage(connection, identity, ptr, size, m.Number, m.Time, m.Channel));
					delivered++;
				}
				if (delivered >= bufferSize) break;
			}
			return delivered;
		}

		internal static int ReceiveForConnection(ConnectionManager manager, int bufferSize)
		{
			Conn c;
			if (!_conns.TryGetValue(manager.Connection.Id, out c)) return 0;

			int delivered = 0;
			while (delivered < bufferSize && c.Inbox.Count > 0)
			{
				var m = c.Inbox.Dequeue();
				Deliver(m, (ptr, size) => manager.Interface.OnMessage(ptr, size, m.Number, m.Time, m.Channel));
				delivered++;
			}
			return delivered;
		}

		static void Deliver(Msg m, Action<IntPtr, int> handler)
		{
			IntPtr ptr = Marshal.AllocHGlobal(Math.Max(1, m.Data.Length));
			try
			{
				Marshal.Copy(m.Data, 0, ptr, m.Data.Length);
				handler(ptr, m.Data.Length);
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
			}
		}


		internal static void DispatchRelay(string json)
		{
			var d = WebworksJson.Obj(json);
			var p = d == null ? null : WebworksJson.Map(d, "payload");
			if (p == null) return;

			ulong sender = WebworksJson.Ulong(d, "from");
			PlayerDirectory.Set(sender, WebworksJson.Str(d, "fromName"));

			switch (WebworksJson.Str(p, "op"))
			{
				case "connect": OnConnectRequest(sender, p); break;
				case "accept": OnAccepted(p); break;
				case "data": OnData(p); break;
				case "close": OnClosedByPeer(sender, p); break;
			}
		}

		static void OnConnectRequest(ulong sender, Dictionary<string, object> p)
		{
			int virtualPort = WebworksJson.Int(p, "vp");
			uint peerId = (uint)WebworksJson.Int(p, "cid");

			SocketManager manager;
			if (!_listeners.TryGetValue(virtualPort, out manager))
			{
				SendRelay(sender, "{\"op\":\"close\",\"cid\":0,\"pcid\":" + peerId + ",\"reason\":" + (int)NetConnectionEnd.Misc_PeerSentNoConnection + "}");
				return;
			}

			var c = new Conn { Id = _nextId++, Peer = sender, PeerId = peerId, VirtualPort = virtualPort, Incoming = true, State = ConnectionState.Connecting, Server = manager };
			_conns[c.Id] = c;

			var connection = new Connection { Id = c.Id };
			var info = InfoOf(c);
			manager.Connecting.Add(connection);

			Callbacks.RaiseNow<Connection, ConnectionInfo>(manager.Interface.OnConnecting, connection, info);
			RaiseStatus(connection, info);
		}

		static void OnAccepted(Dictionary<string, object> p)
		{
			Conn c;
			if (!_conns.TryGetValue((uint)WebworksJson.Int(p, "cid"), out c) || c.Client == null) return;

			c.PeerId = (uint)WebworksJson.Int(p, "pcid");
			c.State = ConnectionState.Connected;

			var manager = c.Client;
			manager.Connecting = false;
			manager.Connected = true;
			manager.ConnectionInfo = InfoOf(c);
			Callbacks.RaiseNow<ConnectionInfo>(manager.Interface.OnConnected, manager.ConnectionInfo);
			RaiseStatus(manager.Connection, manager.ConnectionInfo);
		}

		static void OnData(Dictionary<string, object> p)
		{
			Conn c;
			if (!_conns.TryGetValue((uint)WebworksJson.Int(p, "cid"), out c)) return;

			byte[] data;
			try { data = Convert.FromBase64String(WebworksJson.Str(p, "d") ?? ""); }
			catch (FormatException) { return; }

			if (c.Inbox.Count >= 20000) c.Inbox.Dequeue();
			c.Inbox.Enqueue(new Msg { Data = data, Number = c.NextMessage++, Channel = WebworksJson.Int(p, "ch"), Time = NowMicroseconds() });
		}

		static void OnClosedByPeer(ulong sender, Dictionary<string, object> p)
		{
			Conn c = null;
			uint id = (uint)WebworksJson.Int(p, "cid");
			if (id != 0) _conns.TryGetValue(id, out c);
			else
			{
				uint theirs = (uint)WebworksJson.Int(p, "pcid");
				foreach (var candidate in _conns.Values)
					if (candidate.Peer == sender && candidate.PeerId == theirs) { c = candidate; break; }
			}
			if (c == null) return;

			CloseLocally(c, (NetConnectionEnd)WebworksJson.Int(p, "reason"));
		}

		internal static void DispatchPeerDisconnected(string json)
		{
			var d = WebworksJson.Obj(json);
			if (d == null) return;
			ulong sender = WebworksJson.Ulong(d, "from");
			if (sender == 0) return;

			foreach (var c in new List<Conn>(_conns.Values))
				if (c.Peer == sender) CloseLocally(c, NetConnectionEnd.Misc_SteamConnectivity);
		}

		static void CloseLocally(Conn c, NetConnectionEnd reason)
		{
			_conns.Remove(c.Id);
			var info = InfoOf(c);
			info.State = ConnectionState.ClosedByPeer;
			info.EndReason = reason;
			var connection = new Connection { Id = c.Id };

			if (c.Server != null)
			{
				c.Server.Connecting.Remove(connection);
				c.Server.Connected.Remove(connection);
				Callbacks.RaiseNow<Connection, ConnectionInfo>(c.Server.Interface.OnDisconnected, connection, info);
			}
			else if (c.Client != null)
			{
				c.Client.Connected = false;
				c.Client.Connecting = false;
				c.Client.ConnectionInfo = info;
				Callbacks.RaiseNow<ConnectionInfo>(c.Client.Interface.OnDisconnected, info);
			}
			RaiseStatus(connection, info);
		}


		static void Detach(Conn c)
		{
			var connection = new Connection { Id = c.Id };
			if (c.Server != null) { c.Server.Connecting.Remove(connection); c.Server.Connected.Remove(connection); }
			if (c.Client != null) { c.Client.Connected = false; c.Client.Connecting = false; }
		}

		static ConnectionInfo InfoOf(Conn c)
		{
			SteamId peer = c.Peer;
			NetIdentity identity = peer;
			return new ConnectionInfo { Identity = identity, State = c.State, EndReason = NetConnectionEnd.Invalid };
		}

		static void RaiseStatus(Connection connection, ConnectionInfo info)
		{
			Callbacks.Raise<Connection, ConnectionInfo>(OnConnectionStatusChanged, connection, info);
		}

		const int RelaySocketsChannel = 90000;
		static bool _sharedRelayHooked;

		internal static void AttachSharedRelay(RelayClient relay)
		{
			if (_sharedRelayHooked) return;
			_sharedRelayHooked = true;
			relay.OnPacket += (from, data, channel, reliable) =>
			{
				if (channel != RelaySocketsChannel) return;
				DispatchRelay("{\"from\":\"" + from + "\",\"payload\":" + Encoding.UTF8.GetString(data) + "}");
			};
		}

		static void SendRelay(ulong to, string payloadJson)
		{
			var relay = SteamNetworking.SharedRelay;
			if (relay != null && relay.IsReady)
			{
				relay.Send(to, Encoding.UTF8.GetBytes(payloadJson), RelaySocketsChannel, reliable: true);
				return;
			}

			if (SteamClient.Realtime == null) return;
			SteamClient.Realtime.SendAsync("{\"type\":\"Relay\",\"to\":\"" + to + "\",\"payload\":" + payloadJson + "}").Forget("relay send");
		}

		static long NowMicroseconds()
		{
			return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds * 1000L;
		}
	}

	public static class SteamNetworkingUtils
	{
		public static event Action<NetDebugOutput, string> OnDebugOutput;

		public static SteamNetworkingAvailability Status { get { return SteamNetworkingAvailability.Current; } }
		public static long LocalTimestamp { get { return (long)(Time.realtimeSinceStartup * 1000000.0); } }
		public static NetPingLocation? LocalPingLocation { get { return null; } }
		public static NetDebugOutput DebugLevel { get; set; }

		public static int ConnectionTimeout { get; set; }
		public static int Timeout { get; set; }
		public static int SendBufferSize { get; set; }
		public static float FakeSendPacketLoss { get; set; }
		public static float FakeRecvPacketLoss { get; set; }
		public static float FakeSendPacketLag { get; set; }
		public static float FakeRecvPacketLag { get; set; }

		public static void InitRelayNetworkAccess() { }
		public static Task WaitForPingDataAsync(float maxAgeInSeconds = 300f) { return Task.FromResult(0); }
		public static int EstimatePingTo(NetPingLocation target) { return 0; }
	}
}
