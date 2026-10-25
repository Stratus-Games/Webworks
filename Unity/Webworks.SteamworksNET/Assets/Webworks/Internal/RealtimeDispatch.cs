using UnityEngine;

namespace Webworks.SNetInternal
{
	internal static class RealtimeDispatch
	{
		public static void HandleMessage(string json)
		{
			string type = ExtractType(json);

			switch (type)
			{
				case "LobbyMemberJoined":
				case "LobbyMemberLeave":
				case "LobbyMemberDisconnected":
				case "LobbyDataChanged":
				case "LobbyMemberDataChanged":
				case "LobbyChat":
				case "LobbyGameCreated":
					Steamworks.SteamMatchmaking.DispatchLobbyEvent(type, json);
					break;

				case "LobbyChatError":
					Debug.LogWarning("[Webworks] Lobby chat message was rejected by the server: " + json);
					break;

				case "GameInvite":
					Steamworks.SteamFriends.DispatchGameInvite(json);
					break;

				case "FriendMessage":
					Steamworks.SteamFriends.DispatchFriendMessage(json);
					break;

				case "PersonaStateChange":
					Steamworks.SteamFriends.DispatchPersonaStateChange(json);
					break;

				case "LobbyInvite":
					Steamworks.SteamFriends.DispatchLobbyInvite(json);
					break;

				case "Relay":
					Steamworks.SteamNetworkingSockets.DispatchRelay(json);
					break;

				case "P2PPacket":
				case "P2PSessionAccepted":
				case "P2PSessionClosed":
				case "P2PSessionRequest":
				case "P2PConnectionFailed":
					Steamworks.SteamNetworking.DispatchNetworkingEvent(type, json);
					break;

				case "PeerDisconnected":
					Steamworks.SteamNetworking.DispatchNetworkingEvent(type, json);
					Steamworks.SteamNetworkingSockets.DispatchPeerDisconnected(json);
					break;

				case "MusicStateChanged":
				{
					var d = WebworksJson.Obj(json);
					Steamworks.SteamMusic.ApplyServerState(
						WebworksJson.Str(d, "status"),
						(float)WebworksJson.Num(d, "volume"));
					break;
				}

				case "BroadcastStarted":
				case "BroadcastStopped":
				case "BroadcastViewers":
				case "PartyChanged":
				case "PartyInvite":
				case "RemotePlaySessionStarted":
				case "RemotePlaySessionEnded":
				case "RemotePlayInvite":
				case "RemotePlayInput":
					MediaPump.Poll();
					break;

				default:
					Debug.LogWarning($"[Webworks] Unhandled realtime message type: {type}");
					break;
			}
		}

		static string ExtractType(string json)
		{
			int idx = json.IndexOf("\"type\"");
			if (idx < 0) return null;
			int firstQuote = json.IndexOf('"', idx + 6);
			int secondQuote = json.IndexOf('"', firstQuote + 1);
			return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
		}
	}
}
