using System;
using System.Collections.Generic;
using Webworks.SNetInternal;

namespace Steamworks
{
	public static partial class SteamNetworking
	{
		static readonly object _lock = new object();
		static readonly Dictionary<int, Queue<KeyValuePair<ulong, byte[]>>> _incoming = new Dictionary<int, Queue<KeyValuePair<ulong, byte[]>>>();

		public static bool AllowP2PPacketRelay(bool bAllow) { return true; }

		public static bool IsP2PPacketAvailable(out uint pcubMsgSize, int nChannel = 0)
		{
			lock (_lock)
			{
				pcubMsgSize = 0;
				Queue<KeyValuePair<ulong, byte[]>> q;
				if (!_incoming.TryGetValue(nChannel, out q) || q.Count == 0) return false;
				pcubMsgSize = (uint)q.Peek().Value.Length;
				return true;
			}
		}

		public static bool ReadP2PPacket(byte[] pubDest, uint cubDest, out uint pcubMsgSize, out CSteamID psteamIDRemote, int nChannel = 0)
		{
			lock (_lock)
			{
				pcubMsgSize = 0;
				psteamIDRemote = CSteamID.Nil;
				Queue<KeyValuePair<ulong, byte[]>> q;
				if (!_incoming.TryGetValue(nChannel, out q) || q.Count == 0) return false;

				var p = q.Dequeue();
				var n = Math.Min((uint)p.Value.Length, cubDest);
				Array.Copy(p.Value, pubDest, n);
				pcubMsgSize = n;
				psteamIDRemote = new CSteamID(p.Key);
				return true;
			}
		}

		public static bool SendP2PPacket(CSteamID steamIDRemote, byte[] pubData, uint cubData, EP2PSend eP2PSendType, int nChannel = 0)
		{
			if (pubData == null) return false;
			byte[] payload = pubData;
			if (cubData < pubData.Length) { payload = new byte[cubData]; Array.Copy(pubData, payload, cubData); }

			if (SNetState.Realtime == null) return false;
			var json = "{\"type\":\"P2PPacket\",\"to\":\"" + steamIDRemote.m_SteamID + "\",\"channel\":" + nChannel + ",\"data\":\"" + Convert.ToBase64String(payload) + "\"}";
			SNetState.Realtime.SendAsync(json).Forget("SendP2PPacket to " + steamIDRemote);
			return true;
		}

		public static bool AcceptP2PSessionWithUser(CSteamID steamIDRemote)
		{
			ApiClient.Fire(() => ApiClient.PostAsync("/Networking/AcceptSession", "{\"peerSteamId\":\"" + steamIDRemote.m_SteamID + "\"}"), "AcceptP2PSession");
			return true;
		}

		public static bool CloseP2PSessionWithUser(CSteamID steamIDRemote)
		{
			ApiClient.Fire(() => ApiClient.PostAsync("/Networking/CloseSession", "{\"peerSteamId\":\"" + steamIDRemote.m_SteamID + "\"}"), "CloseP2PSession");
			return true;
		}

		public static bool CloseP2PChannelWithUser(CSteamID steamIDRemote, int nChannel)
		{
			lock (_lock) { _incoming.Remove(nChannel); }
			return true;
		}

		public static bool GetP2PSessionState(CSteamID steamIDRemote, out P2PSessionState_t pConnectionState)
		{
			pConnectionState = new P2PSessionState_t { m_bConnectionActive = 1, m_bConnecting = 0, m_bUsingRelay = 0 };
			return true;
		}

		internal static void EnqueuePacket(ulong from, byte[] data, int channel)
		{
			lock (_lock)
			{
				Queue<KeyValuePair<ulong, byte[]>> q;
				if (!_incoming.TryGetValue(channel, out q)) { q = new Queue<KeyValuePair<ulong, byte[]>>(); _incoming[channel] = q; }
				q.Enqueue(new KeyValuePair<ulong, byte[]>(from, data));
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
					EnqueuePacket(sender, Convert.FromBase64String(b64), channel);
					break;
				}
				case "P2PSessionRequest":
					CallbackBus<P2PSessionRequest_t>.Fire(new P2PSessionRequest_t { m_steamIDRemote = sender });
					break;
				case "P2PConnectionFailed":
					CallbackBus<P2PSessionConnectFail_t>.Fire(new P2PSessionConnectFail_t { m_steamIDRemote = sender, m_eP2PSessionError = (byte)EP2PSessionError.k_EP2PSessionErrorTimeout });
					break;
				case "PeerDisconnected":
					CallbackBus<P2PSessionConnectFail_t>.Fire(new P2PSessionConnectFail_t { m_steamIDRemote = sender, m_eP2PSessionError = (byte)EP2PSessionError.k_EP2PSessionErrorDestinationNotLoggedIn });
					break;
			}
		}
	}

}
