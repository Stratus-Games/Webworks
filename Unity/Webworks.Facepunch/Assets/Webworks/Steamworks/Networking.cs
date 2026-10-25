using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks.Data;
using Webworks.Internal;

namespace Steamworks
{
	public static class SteamNetworking
	{
		internal static string HostSessionId;

		public static Action<SteamId> OnP2PSessionRequest;
		public static Action<SteamId, P2PSessionError> OnP2PConnectionFailed;

		static RelayClient _relay;

		public static async Task<bool> EnsureRelay(SteamId lobbyId)
		{
			if (_relay != null && _relay.IsReady) return true;

			var body = "{\"lobbyId\":" + WebworksJson.Quote(lobbyId.Value.ToString()) + "}";
			var d = WebworksJson.Obj(await ApiClient.PostAsync("/Networking/RelayTicket", body));
			if (d == null || WebworksJson.Str(d, "error") != null) return false;

			var wsUrl = WebworksJson.Str(d, "wsUrl");
			var ticket = WebworksJson.Str(d, "ticket");
			if (wsUrl == null || ticket == null) return false;

			var relay = new RelayClient();
			relay.OnPacket += EnqueuePacket;
			relay.OnDisconnected += () => { if (_relay == relay) _relay = null; };

			var ok = await relay.ConnectAsync(wsUrl, ticket);
			if (ok) { _relay = relay; SteamNetworkingSockets.AttachSharedRelay(relay); }
			return ok;
		}

		public static void CloseRelay()
		{
			_relay?.Close();
			_relay = null;
		}

		internal static RelayClient SharedRelay { get { return _relay; } }

		static readonly object _lock = new object();
		static readonly Dictionary<int, Queue<P2Packet>> _incoming = new Dictionary<int, Queue<P2Packet>>();

		public static bool AllowP2PPacketRelay(bool allow) { return true; }

		public static bool IsP2PPacketAvailable(int channel = 0)
		{
			lock (_lock)
			{
				Queue<P2Packet> q;
				return _incoming.TryGetValue(channel, out q) && q.Count > 0;
			}
		}

		public static bool IsP2PPacketAvailable(out uint msgSize, int channel = 0)
		{
			lock (_lock)
			{
				msgSize = 0;
				Queue<P2Packet> q;
				if (!_incoming.TryGetValue(channel, out q) || q.Count == 0) return false;
				msgSize = (uint)q.Peek().Data.Length;
				return true;
			}
		}

		public static P2Packet? ReadP2PPacket(int channel = 0)
		{
			lock (_lock)
			{
				Queue<P2Packet> q;
				if (!_incoming.TryGetValue(channel, out q) || q.Count == 0) return null;
				return q.Dequeue();
			}
		}

		public static bool ReadP2PPacket(byte[] buffer, ref uint size, ref SteamId steamid, int channel = 0)
		{
			var p = ReadP2PPacket(channel);
			if (!p.HasValue) return false;
			var data = p.Value.Data;
			var n = Math.Min((uint)data.Length, (uint)buffer.Length);
			Array.Copy(data, buffer, n);
			size = n;
			steamid = p.Value.SteamId;
			return true;
		}

		public static bool SendP2PPacket(SteamId steamid, byte[] data, int length = -1, int nChannel = 0, P2PSend sendType = P2PSend.Reliable)
		{
			if (data == null) return false;

			byte[] payload = data;
			if (length >= 0 && length < data.Length)
			{
				payload = new byte[length];
				Array.Copy(data, payload, length);
			}

			bool reliable = sendType != P2PSend.Unreliable && sendType != P2PSend.UnreliableNoDelay;

			if (_relay != null && _relay.IsReady)
			{
				_relay.Send(steamid.Value, payload, nChannel, reliable);
				return true;
			}

			if (SteamClient.Realtime == null) return false;
			var json = "{\"type\":\"P2PPacket\",\"to\":\"" + steamid + "\",\"channel\":" + nChannel + ",\"data\":\"" + Convert.ToBase64String(payload) + "\"}";
			SteamClient.Realtime.SendAsync(json).Forget("SendP2PPacket to " + steamid);
			return true;
		}

		public static bool AcceptP2PSessionWithUser(SteamId user)
		{
			ApiClient.Fire(() => ApiClient.PostAsync("/Networking/AcceptSession", "{\"peerSteamId\":\"" + user + "\"}"), "AcceptP2PSession");
			return true;
		}

		public static bool CloseP2PSessionWithUser(SteamId user)
		{
			ApiClient.Fire(() => ApiClient.PostAsync("/Networking/CloseSession", "{\"peerSteamId\":\"" + user + "\"}"), "CloseP2PSession");
			return true;
		}

		internal static void EnqueuePacket(ulong from, byte[] data, int channel, bool reliable)
		{
			lock (_lock)
			{
				Queue<P2Packet> q;
				if (!_incoming.TryGetValue(channel, out q)) { q = new Queue<P2Packet>(); _incoming[channel] = q; }
				q.Enqueue(new P2Packet { SteamId = from, Data = data });
			}
		}

		internal static void DispatchNetworkingEvent(string type, string json)
		{
			var d = WebworksJson.Obj(json);
			if (d == null) return;
			var sender = WebworksJson.Ulong(d, "from");
			if (sender == 0) return;

			switch (type)
			{
				case "P2PPacket":
				{
					var b64 = WebworksJson.Str(d, "data");
					if (b64 == null) return;
					var channel = WebworksJson.Int(d, "channel");
					EnqueuePacket(sender, Convert.FromBase64String(b64), channel, reliable: true);
					break;
				}
				case "P2PSessionRequest":
					if (OnP2PSessionRequest != null) OnP2PSessionRequest(sender);
					break;
				case "P2PConnectionFailed":
					if (OnP2PConnectionFailed != null) OnP2PConnectionFailed(sender, P2PSessionError.Timeout);
					break;
				case "PeerDisconnected":
					if (OnP2PConnectionFailed != null) OnP2PConnectionFailed(sender, P2PSessionError.DestinationNotLoggedIn);
					break;
			}
		}
	}
}
