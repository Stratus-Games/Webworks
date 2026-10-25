#pragma warning disable CS0067
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Steamworks.Data;
using Webworks.Internal;

namespace Steamworks
{
	public static partial class SteamMatchmaking
	{
		public static event Action<Lobby> OnLobbyEntered;
		public static event Action<Result, Lobby> OnLobbyCreated;
		public static event Action<Lobby, uint, ushort, SteamId> OnLobbyGameCreated;
		public static event Action<Friend, Lobby> OnLobbyInvite;
		public static event Action<Lobby> OnLobbyDataChanged;
		public static event Action<Lobby, Friend> OnLobbyMemberDataChanged;
		public static event Action<Lobby, Friend> OnLobbyMemberJoined;
		public static event Action<Lobby, Friend> OnLobbyMemberLeave;
		public static event Action<Lobby, Friend> OnLobbyMemberDisconnected;
		public static event Action<Lobby, Friend, Friend> OnLobbyMemberKicked;
		public static event Action<Lobby, Friend, Friend> OnLobbyMemberBanned;
		public static event Action<Lobby, Friend, string> OnChatMessage;

		public static LobbyQuery LobbyList { get { return new LobbyQuery(); } }

		public static async Task<Lobby?> CreateLobbyAsync(int maxMembers = 100)
		{
			var raw = await ApiClient.PostAsync("/Matchmaking/CreateLobby", "{\"maxMembers\":" + maxMembers + "}");
			var st = LobbyCache.Ingest(WebworksJson.Obj(raw));
			if (st == null)
			{
				UnityEngine.Debug.LogWarning("[Webworks] CreateLobby failed, server said: " + raw);
				Callbacks.Raise(OnLobbyCreated, Result.Fail, default(Lobby));
				return null;
			}

			UnityEngine.Debug.Log("[Webworks] Lobby created: id=" + st.Id + " owner=" + st.OwnerId + " members=" + st.Members.Count);
			var lobby = new Lobby(st.Id);
			Callbacks.Raise(OnLobbyCreated, Result.OK, lobby);
			Callbacks.Raise(OnLobbyEntered, lobby);
			return lobby;
		}

		public static async Task<Lobby?> JoinLobbyAsync(SteamId lobbyId)
		{
			var st = LobbyCache.Ingest(WebworksJson.Obj(await ApiClient.PostAsync("/Matchmaking/JoinLobby", "{\"lobbyId\":" + WebworksJson.Quote(lobbyId.ToString()) + "}")));
			if (st == null) return null;

			var lobby = new Lobby(st.Id);
			Callbacks.Raise(OnLobbyEntered, lobby);
			return lobby;
		}

		internal static async Task<RoomEnter> JoinAsync(SteamId id)
		{
			var l = await JoinLobbyAsync(id);
			return l.HasValue ? RoomEnter.Success : RoomEnter.DoesntExist;
		}

		internal static void DispatchLobbyEvent(string type, string json)
		{
			var d = WebworksJson.Obj(json);
			if (d == null) return;

			var lobbyId = WebworksJson.Ulong(d, "lobbyId");
			var st = LobbyCache.Get(lobbyId);
			if (st == null) return;

			var lobby = new Lobby(lobbyId);
			var who = WebworksJson.Ulong(d, "steamId");
			PlayerDirectory.Set(who, WebworksJson.Str(d, "name"));
			var friend = new Friend(who);

			switch (type)
			{
				case "LobbyMemberJoined":
				{
					if (!st.Members.Contains(who)) st.Members.Add(who);
					st.MemberMap(who);
					Callbacks.RaiseNow(OnLobbyMemberJoined, lobby, friend);
					break;
				}
				case "LobbyMemberLeave":
				case "LobbyMemberDisconnected":
				{
					st.Members.Remove(who);
					st.MemberData.Remove(who);
					var newOwner = WebworksJson.Ulong(d, "ownerId");
					if (newOwner != 0) st.OwnerId = newOwner;

					if (who == SteamClient.SteamId.Value) { LobbyCache.Remove(lobbyId); break; }
					if (type == "LobbyMemberLeave") Callbacks.RaiseNow(OnLobbyMemberLeave, lobby, friend);
					else Callbacks.RaiseNow(OnLobbyMemberDisconnected, lobby, friend);
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
					Callbacks.RaiseNow(OnLobbyDataChanged, lobby);
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
					Callbacks.RaiseNow(OnLobbyMemberDataChanged, lobby, friend);
					break;
				}
				case "LobbyGameCreated":
				{
					st.GameServerIp = (uint)WebworksJson.Num(d, "ip");
					st.GameServerPort = (ushort)WebworksJson.Int(d, "port");
					st.GameServerId = WebworksJson.Ulong(d, "serverId");
					Callbacks.RaiseNow(OnLobbyGameCreated, lobby, st.GameServerIp, st.GameServerPort, (SteamId)st.GameServerId);
					break;
				}
				case "LobbyChat":
				{
					Callbacks.RaiseNow(OnChatMessage, lobby, friend, WebworksJson.Str(d, "message"));
					break;
				}
			}
		}
	}
}

namespace Steamworks.Data
{
	public struct Lobby : IEquatable<Lobby>
	{
		public SteamId Id { get; internal set; }

		internal Lobby(SteamId id) { Id = id; }

		internal LobbyState S { get { return LobbyCache.Get(Id.Value); } }

		public Friend Owner
		{
			get { var s = S; return new Friend(s == null ? 0UL : s.OwnerId); }
			set
			{
				var s = S;
				if (s == null) return;
				s.OwnerId = value.Id.Value;
				Send("/Matchmaking/LobbyOwner", Id.Value, "\"ownerId\":" + WebworksJson.Quote(value.Id.ToString()));
			}
		}

		public int MemberCount { get { var s = S; return s == null ? 0 : s.Members.Count; } }

		public int MaxMembers
		{
			get { var s = S; return s == null ? 0 : s.MaxMembers; }
			set { var s = S; if (s == null) return; s.MaxMembers = value; Send("/Matchmaking/LobbyMaxMembers", Id.Value, "\"maxMembers\":" + value); }
		}

		public IEnumerable<Friend> Members
		{
			get
			{
				var list = new List<Friend>();
				var s = S;
				if (s != null) foreach (var m in s.Members) list.Add(new Friend(m));
				return list;
			}
		}

		public IEnumerable<KeyValuePair<string, string>> Data
		{
			get
			{
				var list = new List<KeyValuePair<string, string>>();
				var s = S;
				if (s != null) foreach (var kv in s.Data) list.Add(kv);
				return list;
			}
		}

		public string GetData(string key)
		{
			var s = S; string v;
			return s != null && s.Data.TryGetValue(key, out v) ? v : null;
		}

		public bool SetData(string key, string value)
		{
			var s = S;
			if (s == null) return false;
			s.Data[key] = value;
			Send("/Matchmaking/LobbyData", Id.Value, "\"key\":" + WebworksJson.Quote(key) + ",\"value\":" + WebworksJson.Quote(value));
			return true;
		}

		public bool DeleteData(string key)
		{
			var s = S;
			if (s == null) return false;
			s.Data.Remove(key);
			Send("/Matchmaking/LobbyData", Id.Value, "\"key\":" + WebworksJson.Quote(key), "DELETE");
			return true;
		}

		public string GetMemberData(Friend member, string key)
		{
			var s = S; Dictionary<string, string> m; string v;
			return s != null && s.MemberData.TryGetValue(member.Id.Value, out m) && m.TryGetValue(key, out v) ? v : null;
		}

		public void SetMemberData(string key, string value)
		{
			var s = S;
			if (s == null) return;
			s.MemberMap(SteamClient.SteamId.Value)[key] = value;
			Send("/Matchmaking/MemberData", Id.Value, "\"key\":" + WebworksJson.Quote(key) + ",\"value\":" + WebworksJson.Quote(value));
		}

		public bool SendChatString(string message)
		{
			if (S == null) return false;
			Send("/Matchmaking/ChatSend", Id.Value, "\"message\":" + WebworksJson.Quote(message));
			return true;
		}

		public bool SetPublic() { return SetVisibility("public"); }
		public bool SetPrivate() { return SetVisibility("private"); }
		public bool SetInvisible() { return SetVisibility("invisible"); }
		public bool SetFriendsOnly() { return SetVisibility("friends_only"); }

		public bool SetType(LobbyType type)
		{
			switch (type)
			{
				case LobbyType.Public: return SetPublic();
				case LobbyType.FriendsOnly: return SetFriendsOnly();
				case LobbyType.Invisible: return SetInvisible();
				default: return SetPrivate();
			}
		}

		bool SetVisibility(string mode)
		{
			var s = S;
			if (s == null) return false;
			s.Visibility = mode;
			Send("/Matchmaking/LobbyVisibility", Id.Value, "\"mode\":" + WebworksJson.Quote(mode));
			return true;
		}

		public bool SetJoinable(bool b)
		{
			var s = S;
			if (s == null) return false;
			s.Joinable = b;
			Send("/Matchmaking/LobbyJoinable", Id.Value, "\"joinable\":" + (b ? "true" : "false"));
			return true;
		}

		public bool SetGameServer(string ip, ushort port)
		{
			IPAddress addr;
			if (!IPAddress.TryParse(ip, out addr)) return false;
			return SetGameServer(addr, port);
		}

		public bool SetGameServer(IPAddress ip, ushort port)
		{
			var b = ip.GetAddressBytes();
			if (b.Length != 4) return false;
			uint v = ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
			return SendGameServer(v, port, 0UL);
		}

		public bool SetGameServer(SteamId steamServer) { return SendGameServer(0u, 0, steamServer.Value); }

		public bool GetGameServer(ref uint ip, ref ushort port, ref SteamId serverId)
		{
			var s = S;
			if (s == null || (s.GameServerIp == 0 && s.GameServerId == 0)) return false;
			ip = s.GameServerIp; port = s.GameServerPort; serverId = s.GameServerId;
			return true;
		}

		bool SendGameServer(uint ip, ushort port, ulong serverId)
		{
			var s = S;
			if (s == null) return false;
			s.GameServerIp = ip; s.GameServerPort = port; s.GameServerId = serverId;
			Send("/Matchmaking/LobbyGameServer", Id.Value,
				"\"ip\":" + ip + ",\"port\":" + port + ",\"serverId\":" + WebworksJson.Quote(serverId.ToString()));
			return true;
		}

		public bool InviteFriend(SteamId steamid)
		{
			Send("/Matchmaking/InviteFriend", Id.Value, "\"friendSteamId\":" + WebworksJson.Quote(steamid.ToString()));
			return true;
		}

		public async Task<LobbyInvite?> CreateInviteAsync(TimeSpan? ttl = null)
		{
			var body = "{\"lobbyId\":" + WebworksJson.Quote(Id.ToString());
			if (ttl.HasValue) body += ",\"ttlSeconds\":" + (int)ttl.Value.TotalSeconds;
			body += "}";

			var d = WebworksJson.Obj(await ApiClient.PostAsync("/Matchmaking/CreateInvite", body));
			var token = WebworksJson.Str(d, "token");
			if (string.IsNullOrEmpty(token)) return null;

			return new LobbyInvite
			{
				LobbyId = Id,
				Token = token,
				Url = WebworksJson.Str(d, "url"),
				ExpiresAtUnixMs = (long)WebworksJson.Num(d, "expiresAt"),
			};
		}

		public bool Refresh()
		{
			var id = Id.Value;
			ApiClient.Fire(async () =>
			{
				LobbyCache.Ingest(WebworksJson.Obj(await ApiClient.GetAsync("/Matchmaking/Lobby?lobbyId=" + id)));
				return (string)null;
			}, "Lobby.Refresh");
			return true;
		}

		public Task<RoomEnter> Join() { return SteamMatchmaking.JoinAsync(Id); }

		public void Leave()
		{
			Send("/Matchmaking/LeaveLobby", Id.Value, "");
			LobbyCache.Remove(Id.Value);
		}

		static void Send(string path, ulong lobbyId, string fields, string method = "POST")
		{
			var body = "{\"lobbyId\":" + WebworksJson.Quote(lobbyId.ToString()) + (fields.Length > 0 ? "," + fields : "") + "}";
			if (method == "DELETE") ApiClient.Fire(() => ApiClient.DeleteAsync(path, body), "Lobby " + path);
			else ApiClient.Fire(() => ApiClient.PostAsync(path, body), "Lobby " + path);
		}

		public override int GetHashCode() { return Id.GetHashCode(); }
		public override bool Equals(object obj) { return obj is Lobby && ((Lobby)obj).Id == Id; }
		public bool Equals(Lobby other) { return Id == other.Id; }
		public static bool operator ==(Lobby a, Lobby b) { return a.Id == b.Id; }
		public static bool operator !=(Lobby a, Lobby b) { return a.Id != b.Id; }
	}

	public struct LobbyInvite
	{
		public SteamId LobbyId;
		public string Token;
		public string Url;
		public long ExpiresAtUnixMs;

		public DateTime ExpiresAtUtc { get { return DateTimeOffset.FromUnixTimeMilliseconds(ExpiresAtUnixMs).UtcDateTime; } }
		public bool IsExpired { get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > ExpiresAtUnixMs; } }
	}

	public class LobbyQuery
	{
		readonly Dictionary<string, string> _kv = new Dictionary<string, string>();
		readonly List<string> _numeric = new List<string>();
		readonly List<string> _near = new List<string>();
		int _slots = 0, _max = 50;

		public LobbyQuery FilterDistanceClose() { return this; }
		public LobbyQuery FilterDistanceFar() { return this; }
		public LobbyQuery FilterDistanceWorldwide() { return this; }

		public LobbyQuery WithKeyValue(string key, string value) { _kv[key] = value; return this; }
		public LobbyQuery WithSlotsAvailable(int minSlots) { _slots = minSlots; return this; }
		public LobbyQuery WithMaxResults(int max) { _max = max; return this; }

		public LobbyQuery WithEqual(string key, int value) { return Numeric(key, "eq", value); }
		public LobbyQuery WithNotEqual(string key, int value) { return Numeric(key, "ne", value); }
		public LobbyQuery WithLower(string key, int value) { return Numeric(key, "lte", value); }
		public LobbyQuery WithHigher(string key, int value) { return Numeric(key, "gte", value); }

		public LobbyQuery OrderByNear(string key, int value)
		{
			_near.Add("{\"key\":" + WebworksJson.Quote(key) + ",\"value\":" + value.ToString(CultureInfo.InvariantCulture) + "}");
			return this;
		}

		public LobbyQuery WithNear(string key, int value) { return OrderByNear(key, value); }

		LobbyQuery Numeric(string key, string op, int value)
		{
			_numeric.Add("{\"key\":" + WebworksJson.Quote(key) + ",\"op\":\"" + op + "\",\"value\":" + value.ToString(CultureInfo.InvariantCulture) + "}");
			return this;
		}

		public async Task<Lobby[]> RequestAsync()
		{
			var sb = new StringBuilder("{\"slots\":" + _slots + ",\"max\":" + _max + ",\"filters\":{");
			bool first = true;
			foreach (var kv in _kv)
			{
				if (!first) sb.Append(',');
				first = false;
				sb.Append(WebworksJson.Quote(kv.Key)).Append(':').Append(WebworksJson.Quote(kv.Value));
			}
			sb.Append("},\"numeric\":[").Append(string.Join(",", _numeric.ToArray()));
			sb.Append("],\"near\":[").Append(string.Join(",", _near.ToArray())).Append("]}");

			Dictionary<string, object> d;
			try
			{
				d = WebworksJson.Obj(await ApiClient.PostAsync("/Matchmaking/LobbyList", sb.ToString()));
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogWarning("[Webworks] lobby search failed: " + ex.Message);
				return new Lobby[0];
			}

			var result = new List<Lobby>();
			var arr = d == null ? null : WebworksJson.Arr(d, "lobbies");
			if (arr != null)
				foreach (var o in arr)
				{
					var st = LobbyCache.Ingest(o as Dictionary<string, object>);
					if (st != null) result.Add(new Lobby(st.Id));
				}
			return result.ToArray();
		}
	}
}
