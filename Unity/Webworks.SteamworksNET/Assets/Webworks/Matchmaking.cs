using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	public static partial class SteamMatchmaking
	{

		public static SteamAPICall_t CreateLobby(ELobbyType eLobbyType, int cMaxMembers)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var raw = await ApiClient.PostAsync("/Matchmaking/CreateLobby", "{\"maxMembers\":" + cMaxMembers + "}");
				var st = SNetState.Lobbies.Ingest(WebworksJson.Obj(raw));
				if (st == null)
				{
					Debug.LogWarning("[Webworks] CreateLobby failed, server said: " + raw);
					ResultBus.Complete(call, new LobbyCreated_t { m_eResult = EResult.k_EResultFail, m_ulSteamIDLobby = 0 });
					return (string)null;
				}

				if (eLobbyType != ELobbyType.k_ELobbyTypePublic) SetVisibility(st.Id, VisibilityFor(eLobbyType));
				Debug.Log("[Webworks] Lobby created: id=" + st.Id + " owner=" + st.OwnerId + " members=" + st.Members.Count);
				ResultBus.Complete(call, new LobbyCreated_t { m_eResult = EResult.k_EResultOK, m_ulSteamIDLobby = st.Id });
				CallbackBus<LobbyEnter_t>.Fire(new LobbyEnter_t { m_ulSteamIDLobby = st.Id, m_rgfChatPermissions = 0, m_bLocked = false, m_EChatRoomEnterResponse = (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess });
				return (string)null;
			}, "CreateLobby");
			return call;
		}

		public static SteamAPICall_t JoinLobby(CSteamID steamIDLobby)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var raw = await ApiClient.PostAsync("/Matchmaking/JoinLobby", "{\"lobbyId\":" + WebworksJson.Quote(steamIDLobby.m_SteamID.ToString()) + "}");
				var st = SNetState.Lobbies.Ingest(WebworksJson.Obj(raw));
				var response = st != null ? EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess : EChatRoomEnterResponse.k_EChatRoomEnterResponseDoesntExist;
				ResultBus.Complete(call, new LobbyEnter_t { m_ulSteamIDLobby = steamIDLobby.m_SteamID, m_rgfChatPermissions = 0, m_bLocked = false, m_EChatRoomEnterResponse = (uint)response });
				return (string)null;
			}, "JoinLobby");
			return call;
		}

		public static void LeaveLobby(CSteamID steamIDLobby)
		{
			Send("/Matchmaking/LeaveLobby", steamIDLobby.m_SteamID, "");
			SNetState.Lobbies.Remove(steamIDLobby.m_SteamID);
		}

		public static void RequestLobbyData(CSteamID steamIDLobby)
		{
			var id = steamIDLobby.m_SteamID;
			ApiClient.Fire(async () =>
			{
				SNetState.Lobbies.Ingest(WebworksJson.Obj(await ApiClient.GetAsync("/Matchmaking/Lobby?lobbyId=" + id)));
				CallbackBus<LobbyDataUpdate_t>.Fire(new LobbyDataUpdate_t { m_ulSteamIDLobby = id, m_ulSteamIDMember = id, m_bSuccess = 1 });
				return (string)null;
			}, "RequestLobbyData");
		}


		public static CSteamID GetLobbyOwner(CSteamID steamIDLobby)
		{
			var s = SNetState.Lobbies.Get(steamIDLobby.m_SteamID);
			return new CSteamID(s == null ? 0UL : s.OwnerId);
		}

		public static int GetNumLobbyMembers(CSteamID steamIDLobby)
		{
			var s = SNetState.Lobbies.Get(steamIDLobby.m_SteamID);
			return s == null ? 0 : s.Members.Count;
		}

		public static CSteamID GetLobbyMemberByIndex(CSteamID steamIDLobby, int iMember)
		{
			var s = SNetState.Lobbies.Get(steamIDLobby.m_SteamID);
			if (s == null || iMember < 0 || iMember >= s.Members.Count) return CSteamID.Nil;
			return new CSteamID(s.Members[iMember]);
		}

		public static int GetLobbyMemberLimit(CSteamID steamIDLobby)
		{
			var s = SNetState.Lobbies.Get(steamIDLobby.m_SteamID);
			return s == null ? 0 : s.MaxMembers;
		}

		public static bool SetLobbyMemberLimit(CSteamID steamIDLobby, int cMaxMembers)
		{
			var s = SNetState.Lobbies.Get(steamIDLobby.m_SteamID);
			if (s == null) return false;
			s.MaxMembers = cMaxMembers;
			Send("/Matchmaking/LobbyMaxMembers", steamIDLobby.m_SteamID, "\"maxMembers\":" + cMaxMembers);
			return true;
		}


		public static string GetLobbyData(CSteamID steamIDLobby, string pchKey)
		{
			var s = SNetState.Lobbies.Get(steamIDLobby.m_SteamID);
			string v;
			return s != null && s.Data.TryGetValue(pchKey, out v) ? v : "";
		}

		public static bool SetLobbyData(CSteamID steamIDLobby, string pchKey, string pchValue)
		{
			var s = SNetState.Lobbies.Get(steamIDLobby.m_SteamID);
			if (s == null) return false;
			s.Data[pchKey] = pchValue;
			Send("/Matchmaking/LobbyData", steamIDLobby.m_SteamID, "\"key\":" + WebworksJson.Quote(pchKey) + ",\"value\":" + WebworksJson.Quote(pchValue));
			return true;
		}

		public static bool DeleteLobbyData(CSteamID steamIDLobby, string pchKey)
		{
			var s = SNetState.Lobbies.Get(steamIDLobby.m_SteamID);
			if (s == null) return false;
			s.Data.Remove(pchKey);
			Send("/Matchmaking/LobbyData", steamIDLobby.m_SteamID, "\"key\":" + WebworksJson.Quote(pchKey), "DELETE");
			return true;
		}

		public static int GetLobbyDataCount(CSteamID steamIDLobby)
		{
			var s = SNetState.Lobbies.Get(steamIDLobby.m_SteamID);
			return s == null ? 0 : s.Data.Count;
		}

		public static bool GetLobbyDataByIndex(CSteamID steamIDLobby, int iLobbyData, out string pchKey, int cchKeyBufferSize, out string pchValue, int cchValueBufferSize)
		{
			var s = SNetState.Lobbies.Get(steamIDLobby.m_SteamID);
			pchKey = ""; pchValue = "";
			if (s == null || iLobbyData < 0 || iLobbyData >= s.Data.Count) return false;
			int i = 0;
			foreach (var kv in s.Data)
			{
				if (i++ == iLobbyData) { pchKey = kv.Key; pchValue = kv.Value; return true; }
			}
			return false;
		}

		public static string GetLobbyMemberData(CSteamID steamIDLobby, CSteamID steamIDUser, string pchKey)
		{
			var s = SNetState.Lobbies.Get(steamIDLobby.m_SteamID);
			Dictionary<string, string> m; string v;
			return s != null && s.MemberData.TryGetValue(steamIDUser.m_SteamID, out m) && m.TryGetValue(pchKey, out v) ? v : "";
		}

		public static void SetLobbyMemberData(CSteamID steamIDLobby, string pchKey, string pchValue)
		{
			var s = SNetState.Lobbies.Get(steamIDLobby.m_SteamID);
			if (s == null) return;
			s.MemberMap(SteamUser.GetSteamID().m_SteamID)[pchKey] = pchValue;
			Send("/Matchmaking/MemberData", steamIDLobby.m_SteamID, "\"key\":" + WebworksJson.Quote(pchKey) + ",\"value\":" + WebworksJson.Quote(pchValue));
		}


		public static bool SetLobbyType(CSteamID steamIDLobby, ELobbyType eLobbyType) { return SetVisibility(steamIDLobby.m_SteamID, VisibilityFor(eLobbyType)); }

		public static bool SetLobbyJoinable(CSteamID steamIDLobby, bool bLobbyJoinable)
		{
			var s = SNetState.Lobbies.Get(steamIDLobby.m_SteamID);
			if (s == null) return false;
			s.Joinable = bLobbyJoinable;
			Send("/Matchmaking/LobbyJoinable", steamIDLobby.m_SteamID, "\"joinable\":" + (bLobbyJoinable ? "true" : "false"));
			return true;
		}

		static bool SetVisibility(ulong lobbyId, string mode)
		{
			var s = SNetState.Lobbies.Get(lobbyId);
			if (s == null) return false;
			s.Visibility = mode;
			Send("/Matchmaking/LobbyVisibility", lobbyId, "\"mode\":" + WebworksJson.Quote(mode));
			return true;
		}

		static string VisibilityFor(ELobbyType t)
		{
			switch (t)
			{
				case ELobbyType.k_ELobbyTypePublic: return "public";
				case ELobbyType.k_ELobbyTypeFriendsOnly: return "friends_only";
				case ELobbyType.k_ELobbyTypeInvisible: return "invisible";
				default: return "private";
			}
		}


		public static void SetLobbyGameServer(CSteamID steamIDLobby, uint unGameServerIP, ushort unGameServerPort, CSteamID steamIDGameServer)
		{
			var s = SNetState.Lobbies.Get(steamIDLobby.m_SteamID);
			if (s == null) return;
			s.GameServerIp = unGameServerIP; s.GameServerPort = unGameServerPort; s.GameServerId = steamIDGameServer.m_SteamID;
			Send("/Matchmaking/LobbyGameServer", steamIDLobby.m_SteamID, "\"ip\":" + unGameServerIP + ",\"port\":" + unGameServerPort + ",\"serverId\":" + WebworksJson.Quote(steamIDGameServer.m_SteamID.ToString()));
		}

		public static bool GetLobbyGameServer(CSteamID steamIDLobby, out uint punGameServerIP, out ushort punGameServerPort, out CSteamID psteamIDGameServer)
		{
			var s = SNetState.Lobbies.Get(steamIDLobby.m_SteamID);
			punGameServerIP = 0; punGameServerPort = 0; psteamIDGameServer = CSteamID.Nil;
			if (s == null || (s.GameServerIp == 0 && s.GameServerId == 0)) return false;
			punGameServerIP = s.GameServerIp; punGameServerPort = s.GameServerPort; psteamIDGameServer = new CSteamID(s.GameServerId);
			return true;
		}


		public static bool InviteUserToLobby(CSteamID steamIDLobby, CSteamID steamIDInvitee)
		{
			Send("/Matchmaking/InviteFriend", steamIDLobby.m_SteamID, "\"friendSteamId\":" + WebworksJson.Quote(steamIDInvitee.m_SteamID.ToString()));
			return true;
		}


		class ChatEntry { public ulong Sender; public byte[] Data; public EChatEntryType Type = EChatEntryType.k_EChatEntryTypeChatMsg; }
		static readonly Dictionary<ulong, Dictionary<uint, ChatEntry>> _chatLog = new Dictionary<ulong, Dictionary<uint, ChatEntry>>();
		static readonly Dictionary<ulong, uint> _lastChatId = new Dictionary<ulong, uint>();
		const int MaxChatBytes = 4096;

		public static bool SendLobbyChatMsg(CSteamID steamIDLobby, byte[] pvMsgBody, int cubMsgBody)
		{
			if (pvMsgBody == null || cubMsgBody <= 0 || cubMsgBody > MaxChatBytes) return false;
			if (SNetState.Lobbies.Get(steamIDLobby.m_SteamID) == null) return false;
			var bytes = new byte[Math.Min(cubMsgBody, pvMsgBody.Length)];
			Array.Copy(pvMsgBody, bytes, bytes.Length);
			var b64 = Convert.ToBase64String(bytes);

			if (SNetState.Realtime != null)
			{
				var json = "{\"type\":\"LobbyChat\",\"lobbyId\":" + WebworksJson.Quote(steamIDLobby.m_SteamID.ToString()) + ",\"data\":\"" + b64 + "\"}";
				SNetState.Realtime.SendAsync(json).Forget("SendLobbyChatMsg");
			}
			else Send("/Matchmaking/ChatSend", steamIDLobby.m_SteamID, "\"data\":\"" + b64 + "\"");
			return true;
		}

		public static int GetLobbyChatEntry(CSteamID steamIDLobby, int iChatID, out CSteamID pSteamIDUser, byte[] pvData, int cubData, out EChatEntryType peChatEntryType)
		{
			Dictionary<uint, ChatEntry> log;
			ChatEntry entry;
			pSteamIDUser = CSteamID.Nil;
			peChatEntryType = EChatEntryType.k_EChatEntryTypeInvalid;
			if (iChatID < 0 || !_chatLog.TryGetValue(steamIDLobby.m_SteamID, out log) || !log.TryGetValue((uint)iChatID, out entry)) return 0;

			pSteamIDUser = new CSteamID(entry.Sender);
			peChatEntryType = entry.Type;
			var n = Math.Min(entry.Data.Length, cubData);
			if (pvData != null) Array.Copy(entry.Data, pvData, Math.Min(n, pvData.Length));
			return n;
		}


		static readonly Dictionary<string, string> _kv = new Dictionary<string, string>();
		static readonly List<string> _numeric = new List<string>();
		static readonly List<string> _near = new List<string>();
		static int _slots, _max = 50;
		static CSteamID[] _lastSearchResults = new CSteamID[0];

		public static void AddRequestLobbyListStringFilter(string pchKeyToMatch, string pchValueToMatch, ELobbyComparison eComparisonType) { _kv[pchKeyToMatch] = pchValueToMatch; }
		public static void AddRequestLobbyListNumericalFilter(string pchKeyToMatch, int nValueToMatch, ELobbyComparison eComparisonType) { _numeric.Add("{\"key\":" + WebworksJson.Quote(pchKeyToMatch) + ",\"op\":\"" + OpFor(eComparisonType) + "\",\"value\":" + nValueToMatch.ToString(CultureInfo.InvariantCulture) + "}"); }
		public static void AddRequestLobbyListNearValueFilter(string pchKeyToMatch, int nValueToBeCloseTo) { _near.Add("{\"key\":" + WebworksJson.Quote(pchKeyToMatch) + ",\"value\":" + nValueToBeCloseTo.ToString(CultureInfo.InvariantCulture) + "}"); }
		public static void AddRequestLobbyListFilterSlotsAvailable(int nSlotsAvailable) { _slots = nSlotsAvailable; }
		public static void AddRequestLobbyListResultCountFilter(int cMaxResults) { _max = cMaxResults; }
		public static void AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter eLobbyDistanceFilter) { }

		static string OpFor(ELobbyComparison c)
		{
			switch (c)
			{
				case ELobbyComparison.k_ELobbyComparisonEqual: return "eq";
				case ELobbyComparison.k_ELobbyComparisonNotEqual: return "ne";
				case ELobbyComparison.k_ELobbyComparisonEqualToOrLessThan: case ELobbyComparison.k_ELobbyComparisonLessThan: return "lte";
				case ELobbyComparison.k_ELobbyComparisonEqualToOrGreaterThan: case ELobbyComparison.k_ELobbyComparisonGreaterThan: return "gte";
				default: return "eq";
			}
		}

		public static SteamAPICall_t RequestLobbyList()
		{
			var call = ResultBus.NewCall();
			var sb = new StringBuilder("{\"slots\":" + _slots + ",\"max\":" + _max + ",\"filters\":{");
			bool first = true;
			foreach (var kv in _kv) { if (!first) sb.Append(','); first = false; sb.Append(WebworksJson.Quote(kv.Key)).Append(':').Append(WebworksJson.Quote(kv.Value)); }
			sb.Append("},\"numeric\":[").Append(string.Join(",", _numeric.ToArray()));
			sb.Append("],\"near\":[").Append(string.Join(",", _near.ToArray())).Append("]");
			if (!string.IsNullOrEmpty(_compatibleLobby)) sb.Append(",\"compatible\":").Append(WebworksJson.Quote(_compatibleLobby));
			sb.Append('}');
			_kv.Clear(); _numeric.Clear(); _near.Clear(); _slots = 0; _max = 50; _compatibleLobby = "";

			ApiClient.Fire(async () =>
			{
				Dictionary<string, object> d;
				try { d = WebworksJson.Obj(await ApiClient.PostAsync("/Matchmaking/LobbyList", sb.ToString())); }
				catch (Exception ex)
				{
					Debug.LogWarning("[Webworks] lobby search failed: " + ex.Message);
					_lastSearchResults = new CSteamID[0];
					ResultBus.Complete(call, new LobbyMatchList_t { m_nLobbiesMatching = 0 });
					return (string)null;
				}

				var result = new List<CSteamID>();
				var arr = d == null ? null : WebworksJson.Arr(d, "lobbies");
				if (arr != null)
					foreach (var o in arr)
					{
						var st = SNetState.Lobbies.Ingest(o as Dictionary<string, object>);
						if (st != null) result.Add(new CSteamID(st.Id));
					}
				_lastSearchResults = result.ToArray();
				ResultBus.Complete(call, new LobbyMatchList_t { m_nLobbiesMatching = (uint)result.Count });
				return (string)null;
			}, "RequestLobbyList");
			return call;
		}

		public static CSteamID GetLobbyByIndex(int iLobby) { return iLobby >= 0 && iLobby < _lastSearchResults.Length ? _lastSearchResults[iLobby] : CSteamID.Nil; }


		internal static void DispatchLobbyEvent(string type, string json)
		{
			var d = WebworksJson.Obj(json);
			if (d == null) return;

			var lobbyId = WebworksJson.Ulong(d, "lobbyId");
			var st = SNetState.Lobbies.Get(lobbyId);
			if (st == null) return;

			var who = WebworksJson.Ulong(d, "steamId");
			SNetState.Players.Set(who, WebworksJson.Str(d, "name"));

			switch (type)
			{
				case "LobbyMemberJoined":
				{
					if (!st.Members.Contains(who)) st.Members.Add(who);
					st.MemberMap(who);
					CallbackBus<LobbyChatUpdate_t>.Fire(new LobbyChatUpdate_t { m_ulSteamIDLobby = lobbyId, m_ulSteamIDUserChanged = who, m_ulSteamIDMakingChange = who, m_rgfChatMemberStateChange = (uint)EChatMemberStateChange.k_EChatMemberStateChangeEntered });
					break;
				}
				case "LobbyMemberLeave":
				case "LobbyMemberDisconnected":
				{
					st.Members.Remove(who);
					st.MemberData.Remove(who);
					var newOwner = WebworksJson.Ulong(d, "ownerId");
					if (newOwner != 0) st.OwnerId = newOwner;

					var flag = type == "LobbyMemberLeave" ? EChatMemberStateChange.k_EChatMemberStateChangeLeft : EChatMemberStateChange.k_EChatMemberStateChangeDisconnected;
					if (who == SteamUser.GetSteamID().m_SteamID) { SNetState.Lobbies.Remove(lobbyId); break; }
					CallbackBus<LobbyChatUpdate_t>.Fire(new LobbyChatUpdate_t { m_ulSteamIDLobby = lobbyId, m_ulSteamIDUserChanged = who, m_ulSteamIDMakingChange = who, m_rgfChatMemberStateChange = (uint)flag });
					break;
				}
				case "LobbyDataChanged":
				{
					var key = WebworksJson.Str(d, "key");
					if (key != null)
					{
						object v;
						if (d.TryGetValue("value", out v) && v != null) st.Data[key] = v as string ?? "";
						else st.Data.Remove(key);
					}
					var owner = WebworksJson.Ulong(d, "ownerId");
					if (owner != 0) st.OwnerId = owner;
					CallbackBus<LobbyDataUpdate_t>.Fire(new LobbyDataUpdate_t { m_ulSteamIDLobby = lobbyId, m_ulSteamIDMember = lobbyId, m_bSuccess = 1 });
					break;
				}
				case "LobbyMemberDataChanged":
				{
					var key = WebworksJson.Str(d, "key");
					if (key != null)
					{
						object v;
						if (d.TryGetValue("value", out v) && v != null) st.MemberMap(who)[key] = v as string ?? "";
						else st.MemberMap(who).Remove(key);
					}
					CallbackBus<LobbyDataUpdate_t>.Fire(new LobbyDataUpdate_t { m_ulSteamIDLobby = lobbyId, m_ulSteamIDMember = who, m_bSuccess = 1 });
					break;
				}
				case "LobbyGameCreated":
				{
					st.GameServerIp = (uint)WebworksJson.Num(d, "ip");
					st.GameServerPort = (ushort)WebworksJson.Int(d, "port");
					st.GameServerId = WebworksJson.Ulong(d, "serverId");
					CallbackBus<LobbyGameCreated_t>.Fire(new LobbyGameCreated_t { m_ulSteamIDLobby = lobbyId, m_ulSteamIDGameServer = st.GameServerId, m_unIP = st.GameServerIp, m_usPort = st.GameServerPort });
					break;
				}
				case "LobbyChat":
				{
					Dictionary<uint, ChatEntry> log;
					if (!_chatLog.TryGetValue(lobbyId, out log)) { log = new Dictionary<uint, ChatEntry>(); _chatLog[lobbyId] = log; }
					byte[] bytes;
					var b64 = WebworksJson.Str(d, "data");
					try { bytes = !string.IsNullOrEmpty(b64) ? Convert.FromBase64String(b64) : Encoding.UTF8.GetBytes(WebworksJson.Str(d, "message") ?? ""); }
					catch (FormatException) { break; }

					uint id;
					if (!_lastChatId.TryGetValue(lobbyId, out id)) id = 0;
					var serverId = (uint)WebworksJson.Num(d, "chatId");
					id = serverId > id ? serverId : id + 1;
					_lastChatId[lobbyId] = id;

					var entryType = WebworksJson.Int(d, "entryType", (int)EChatEntryType.k_EChatEntryTypeChatMsg);
					log[id] = new ChatEntry { Sender = who, Data = bytes, Type = (EChatEntryType)entryType };
					if (id > 256) log.Remove(id - 256);
					CallbackBus<LobbyChatMsg_t>.Fire(new LobbyChatMsg_t { m_ulSteamIDLobby = lobbyId, m_ulSteamIDUser = who, m_eChatEntryType = (byte)entryType, m_iChatID = id });
					break;
				}
			}
		}

		static void Send(string path, ulong lobbyId, string fields, string method = "POST")
		{
			var body = "{\"lobbyId\":" + WebworksJson.Quote(lobbyId.ToString()) + (fields.Length > 0 ? "," + fields : "") + "}";
			if (method == "DELETE") ApiClient.Fire(() => ApiClient.DeleteAsync(path, body), "Lobby " + path);
			else ApiClient.Fire(() => ApiClient.PostAsync(path, body), "Lobby " + path);
		}
	}
}
