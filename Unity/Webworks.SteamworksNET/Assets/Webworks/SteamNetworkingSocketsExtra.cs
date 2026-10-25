using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	// Poll groups, loopback pairs, relay tickets and fake IPs for the sockets interface.
	// The connection table, listeners and message plumbing live in SteamNetworkingSockets.cs.
	public static partial class SteamNetworkingSockets
	{
		static readonly Dictionary<uint, HashSet<uint>> _pollGroups = new Dictionary<uint, HashSet<uint>>();
		static readonly List<SteamDatagramRelayAuthTicket> _relayTickets = new List<SteamDatagramRelayAuthTicket>();
		static uint _nextPollGroup = 1;

		static uint _fakeIp;
		static ushort _fakePorts;
		static bool _fakeReady;

		static double NowMicroseconds()
		{
			return (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds * 1000.0;
		}

		// Pulls up to max messages out of one connection into the caller's array.
		static int DrainConnection(Conn conn, HSteamNetConnection handle, IntPtr[] ppOutMessages, int start, int max)
		{
			int delivered = 0;
			lock (conn.Inbox)
			{
				while (delivered < max && conn.Inbox.Count > 0 && start + delivered < ppOutMessages.Length)
				{
					var data = conn.Inbox.Dequeue();
					int channel;
					lock (conn.Channels) { channel = conn.Channels.Count > 0 ? conn.Channels.Dequeue() : 0; }

					var message = new SteamNetworkingMessage_t
					{
						m_pData = Marshal.AllocHGlobal(Math.Max(1, data.Length)),
						m_cbSize = data.Length,
						m_conn = handle,
						m_identityPeer = conn.RemoteIdentity,
						m_nConnUserData = conn.UserData,
						m_usecTimeReceived = (long)NowMicroseconds(),
						m_nMessageNumber = conn.NextMessage++,
						m_nChannel = channel,
						m_nFlags = SendReliable,
					};

					Marshal.Copy(data, 0, message.m_pData, data.Length);
					var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SteamNetworkingMessage_t)));
					Marshal.StructureToPtr(message, ptr, false);
					ppOutMessages[start + delivered] = ptr;
					delivered++;
				}
			}
			return delivered;
		}

		public static void SendMessages(int nMessages, IntPtr[] pMessages, long[] pOutMessageNumberOrResult)
		{
			if (pMessages == null) return;

			for (int i = 0; i < nMessages && i < pMessages.Length; i++)
			{
				long result;
				var ptr = pMessages[i];
				if (ptr == IntPtr.Zero)
				{
					result = (long)EResult.k_EResultInvalidParam;
				}
				else
				{
					var message = (SteamNetworkingMessage_t)Marshal.PtrToStructure(ptr, typeof(SteamNetworkingMessage_t));
					var conn = Find(message.m_conn);
					if (conn == null || message.m_pData == IntPtr.Zero || message.m_cbSize <= 0)
					{
						result = (long)EResult.k_EResultInvalidParam;
					}
					else
					{
						var buffer = new byte[message.m_cbSize];
						Marshal.Copy(message.m_pData, buffer, 0, message.m_cbSize);
						result = Send(conn, buffer);
					}
				}

				if (pOutMessageNumberOrResult != null && i < pOutMessageNumberOrResult.Length) pOutMessageNumberOrResult[i] = result;
			}
		}

		public static bool CreateSocketPair(out HSteamNetConnection pOutConnection1, out HSteamNetConnection pOutConnection2, bool bUseNetworkLoopback, ref SteamNetworkingIdentity pIdentity1, ref SteamNetworkingIdentity pIdentity2)
		{
			pOutConnection1 = HSteamNetConnection.Invalid;
			pOutConnection2 = HSteamNetConnection.Invalid;
			if (!SessionState.Initialized) return false;
			Hook();

			var first = new Conn
			{
				Id = _nextConn++,
				IsTunnel = false,
				State = ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected,
				RemoteIdentity = pIdentity2,
				RemoteAddress = "loopback",
			};
			var second = new Conn
			{
				Id = _nextConn++,
				IsTunnel = false,
				State = ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected,
				RemoteIdentity = pIdentity1,
				RemoteAddress = "loopback",
			};

			first.Loopback = second;
			second.Loopback = first;
			_conns[first.Id] = first;
			_conns[second.Id] = second;

			pOutConnection1 = new HSteamNetConnection(first.Id);
			pOutConnection2 = new HSteamNetConnection(second.Id);
			Raise(first);
			Raise(second);
			return true;
		}

		public static EResult ConfigureConnectionLanes(HSteamNetConnection hConn, int nNumLanes, int[] pLanePriorities, ushort[] pLaneWeights)
		{
			// Every connection is a single reliable lane in Webworks; accepting the call is
			// correct, there is simply nothing to prioritise.
			return Find(hConn) != null ? EResult.k_EResultOK : EResult.k_EResultInvalidParam;
		}

		public static HSteamNetPollGroup CreatePollGroup()
		{
			var id = _nextPollGroup++;
			_pollGroups[id] = new HashSet<uint>();
			return new HSteamNetPollGroup(id);
		}

		public static bool DestroyPollGroup(HSteamNetPollGroup hPollGroup)
		{
			return _pollGroups.Remove(hPollGroup.m_HSteamNetPollGroup);
		}

		public static bool SetConnectionPollGroup(HSteamNetConnection hConn, HSteamNetPollGroup hPollGroup)
		{
			if (Find(hConn) == null) return false;

			foreach (var group in _pollGroups.Values) group.Remove(hConn.m_HSteamNetConnection);
			if (hPollGroup.m_HSteamNetPollGroup == 0) return true;

			HashSet<uint> target;
			if (!_pollGroups.TryGetValue(hPollGroup.m_HSteamNetPollGroup, out target)) return false;
			target.Add(hConn.m_HSteamNetConnection);
			return true;
		}

		public static int ReceiveMessagesOnPollGroup(HSteamNetPollGroup hPollGroup, IntPtr[] ppOutMessages, int nMaxMessages)
		{
			if (ppOutMessages == null || nMaxMessages <= 0) return 0;

			HashSet<uint> group;
			if (!_pollGroups.TryGetValue(hPollGroup.m_HSteamNetPollGroup, out group)) return 0;

			int delivered = 0;
			foreach (var id in group)
			{
				if (delivered >= nMaxMessages) break;
				Conn conn;
				if (!_conns.TryGetValue(id, out conn)) continue;
				delivered += DrainConnection(conn, new HSteamNetConnection(id), ppOutMessages, delivered, nMaxMessages - delivered);
			}
			return delivered;
		}

		public static bool ReceivedRelayAuthTicket(IntPtr pvTicket, int cbTicket, out SteamDatagramRelayAuthTicket pOutParsedTicket)
		{
			pOutParsedTicket = default(SteamDatagramRelayAuthTicket);
			if (pvTicket == IntPtr.Zero || cbTicket <= 0) return false;

			try
			{
				var raw = new byte[cbTicket];
				Marshal.Copy(pvTicket, raw, 0, cbTicket);
				var text = Encoding.UTF8.GetString(raw).Trim();

				// The server hands out base64("steamId.lobbyId.expiresAt.signature").
				var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(text));
				var parts = decoded.Split('.');
				if (parts.Length < 3) return false;

				ulong steamId;
				long expiresAt;
				if (!ulong.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out steamId)) return false;
				if (!long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out expiresAt)) return false;

				pOutParsedTicket.m_identityAuthorizedClient.SetSteamID64(steamId);
				pOutParsedTicket.m_rtimeTicketExpiry = (uint)Math.Max(0, Math.Min(uint.MaxValue, expiresAt / 1000L));
				_relayTickets.Add(pOutParsedTicket);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static int FindRelayAuthTicketForServer(ref SteamNetworkingIdentity identityGameServer, int nRemoteVirtualPort, out SteamDatagramRelayAuthTicket pOutParsedTicket)
		{
			pOutParsedTicket = default(SteamDatagramRelayAuthTicket);
			var wanted = identityGameServer.GetSteamID64();

			for (int i = 0; i < _relayTickets.Count; i++)
			{
				var ticket = _relayTickets[i];
				if (wanted != 0 && ticket.m_identityGameserver.GetSteamID64() == wanted)
				{
					pOutParsedTicket = ticket;
					return i + 1;
				}
			}
			return 0;
		}

		public static HSteamNetConnection ConnectToHostedDedicatedServer(ref SteamNetworkingIdentity identityTarget, int nRemoteVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			return ConnectP2P(ref identityTarget, nRemoteVirtualPort, nOptions, pOptions);
		}

		public static ushort GetHostedDedicatedServerPort() { return 0; }

		public static SteamNetworkingPOPID GetHostedDedicatedServerPOPID() { return default(SteamNetworkingPOPID); }

		public static EResult GetHostedDedicatedServerAddress(out SteamDatagramHostedAddress pRouting)
		{
			pRouting = default(SteamDatagramHostedAddress);
			return EResult.k_EResultInvalidState;
		}

		public static HSteamListenSocket CreateHostedDedicatedServerListenSocket(int nLocalVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			return CreateListenSocketP2P(nLocalVirtualPort, nOptions, pOptions);
		}

		public static EResult GetGameCoordinatorServerLogin(IntPtr pLoginInfo, out int pcbSignedBlob, IntPtr pBlob)
		{
			pcbSignedBlob = 0;
			return EResult.k_EResultInvalidState;
		}

		public static HSteamNetConnection ConnectP2PCustomSignaling(out ISteamNetworkingConnectionSignaling pSignaling, ref SteamNetworkingIdentity pPeerIdentity, int nRemoteVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			// Custom signaling needs a transport the caller owns; Webworks routes through its
			// own relay, so there is no signaling object to hand back.
			pSignaling = default(ISteamNetworkingConnectionSignaling);
			return HSteamNetConnection.Invalid;
		}

		public static bool ReceivedP2PCustomSignal(IntPtr pMsg, int cbMsg, out ISteamNetworkingSignalingRecvContext pContext)
		{
			pContext = default(ISteamNetworkingSignalingRecvContext);
			return false;
		}

		public static bool GetCertificateRequest(out int pcbBlob, IntPtr pBlob, out SteamNetworkingErrMsg errMsg)
		{
			pcbBlob = 0;
			errMsg = new SteamNetworkingErrMsg { m_rgchErrorMsg = "certificates are not used by Webworks" };
			return false;
		}

		public static bool SetCertificate(IntPtr pCertificate, int cbCertificate, out SteamNetworkingErrMsg errMsg)
		{
			errMsg = new SteamNetworkingErrMsg { m_rgchErrorMsg = "certificates are not used by Webworks" };
			return false;
		}

		public static void ResetIdentity(ref SteamNetworkingIdentity pIdentity)
		{
			pIdentity.Clear();
		}

		public static bool BeginAsyncRequestFakeIP(int nNumPorts)
		{
			if (!SessionState.Initialized) return false;
			_fakeIp = 0x0A000001; // 10.0.0.1, the first address of the fake network
			_fakePorts = (ushort)Math.Max(1, Math.Min(32, nNumPorts));
			_fakeReady = true;
			Hook();
			return true;
		}

		public static void GetFakeIP(int idxFirstPort, out SteamNetworkingFakeIPResult_t pInfo)
		{
			pInfo = new SteamNetworkingFakeIPResult_t
			{
				m_eResult = _fakeReady ? EResult.k_EResultOK : EResult.k_EResultInvalidState,
				m_unIP = _fakeIp,
				m_unPorts = _fakePorts,
			};
			SteamNetworkingIdentity identity;
			if (GetIdentity(out identity)) pInfo.m_identity = identity;
		}

		public static HSteamListenSocket CreateListenSocketP2PFakeIP(int idxFakePort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			if (!_fakeReady) return HSteamListenSocket.Invalid;

			var address = new SteamNetworkingIPAddr();
			address.SetIPv4(_fakeIp, (ushort)Math.Abs(idxFakePort));
			return CreateListenSocketIP(ref address, nOptions, pOptions);
		}

		public static EResult GetRemoteFakeIPForConnection(HSteamNetConnection hConn, out SteamNetworkingIPAddr pOutAddr)
		{
			pOutAddr = default(SteamNetworkingIPAddr);
			var conn = Find(hConn);
			if (conn == null || string.IsNullOrEmpty(conn.RemoteAddress)) return EResult.k_EResultInvalidParam;
			if (conn.RemoteAddress == "loopback")
			{
				pOutAddr.SetIPv4(0x7F000001, 0);
				return EResult.k_EResultOK;
			}
			return pOutAddr.ParseString(conn.RemoteAddress) ? EResult.k_EResultOK : EResult.k_EResultInvalidParam;
		}

		public static IntPtr CreateFakeUDPPort(int idxFakeServerPort)
		{
			// Raw UDP fake ports need a native socket; Webworks proxies UDP through its
			// tunnel instead, so there is no object to hand out here.
			return IntPtr.Zero;
		}
	}
}
