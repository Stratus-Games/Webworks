using System;
using System.Collections.Generic;
using System.Globalization;

namespace Webworks.SNetInternal
{
	internal static class SNetState
	{
		public static readonly SNetPlayerDirectory Players = new SNetPlayerDirectory();
		public static readonly SNetFriendDirectory Friends = new SNetFriendDirectory();
		public static readonly SNetLobbyCache Lobbies = new SNetLobbyCache();

		public static RealtimeClient Realtime;
	}

	internal class SNetPlayerDirectory
	{
		readonly Dictionary<ulong, string> _names = new Dictionary<ulong, string>();
		public void Set(ulong id, string name) { if (id != 0 && !string.IsNullOrEmpty(name)) _names[id] = name; }
		public string Get(ulong id) { string n; return _names.TryGetValue(id, out n) ? n : null; }
	}

	internal class SNetFriendDirectory
	{
		internal class Entry { public Steamworks.EPersonaState PersonaState = Steamworks.EPersonaState.k_EPersonaStateOffline; }
		readonly Dictionary<ulong, Entry> _friends = new Dictionary<ulong, Entry>();

		public void ReplaceAll(IEnumerable<ulong> ids)
		{
			var keep = new HashSet<ulong>(ids);
			foreach (var id in new List<ulong>(_friends.Keys))
				if (!keep.Contains(id)) _friends.Remove(id);
			foreach (var id in keep)
				if (!_friends.ContainsKey(id)) _friends[id] = new Entry();
		}

		public void Upsert(ulong id, Steamworks.EPersonaState state)
		{
			Entry e;
			if (!_friends.TryGetValue(id, out e)) { e = new Entry(); _friends[id] = e; }
			e.PersonaState = state;
		}

		public bool IsFriend(ulong id) { return _friends.ContainsKey(id); }
		public Steamworks.EPersonaState StateOf(ulong id) { Entry e; return _friends.TryGetValue(id, out e) ? e.PersonaState : Steamworks.EPersonaState.k_EPersonaStateOffline; }
		public IEnumerable<ulong> AllIds { get { return _friends.Keys; } }
	}

	internal class SNetLobbyState
	{
		public ulong Id, OwnerId;
		public int MaxMembers = 100;
		public bool Joinable = true;
		public string Visibility = "public";
		public uint GameServerIp;
		public ushort GameServerPort;
		public ulong GameServerId;
		public Dictionary<string, string> Data = new Dictionary<string, string>();
		public List<ulong> Members = new List<ulong>();
		public Dictionary<ulong, Dictionary<string, string>> MemberData = new Dictionary<ulong, Dictionary<string, string>>();

		public void Apply(Dictionary<string, object> d)
		{
			Id = WebworksJson.Ulong(d, "id");
			OwnerId = WebworksJson.Ulong(d, "ownerId");
			MaxMembers = WebworksJson.Int(d, "maxMembers", MaxMembers);
			Joinable = WebworksJson.Bool(d, "joinable", Joinable);
			Visibility = WebworksJson.Str(d, "visibility") ?? Visibility;
			Data = StrMap(WebworksJson.Map(d, "data"));

			var gs = WebworksJson.Map(d, "gameServer");
			if (gs != null)
			{
				GameServerIp = (uint)WebworksJson.Num(gs, "ip");
				GameServerPort = (ushort)WebworksJson.Int(gs, "port");
				GameServerId = WebworksJson.Ulong(gs, "serverId");
			}

			var members = WebworksJson.Arr(d, "members");
			if (members != null)
			{
				Members.Clear();
				foreach (var m in members)
				{
					var md = m as Dictionary<string, object>;
					var id = WebworksJson.Ulong(md, "steamId");
					if (id == 0) continue;
					Members.Add(id);
					SNetState.Players.Set(id, WebworksJson.Str(md, "name"));
				}
			}

			var mdata = WebworksJson.Map(d, "memberData");
			if (mdata != null)
			{
				MemberData.Clear();
				foreach (var kv in mdata)
				{
					ulong mid;
					if (ulong.TryParse(kv.Key, out mid)) MemberData[mid] = StrMap(kv.Value as Dictionary<string, object>);
				}
			}
		}

		public Dictionary<string, string> MemberMap(ulong id)
		{
			Dictionary<string, string> m;
			if (!MemberData.TryGetValue(id, out m)) { m = new Dictionary<string, string>(); MemberData[id] = m; }
			return m;
		}

		static Dictionary<string, string> StrMap(Dictionary<string, object> src)
		{
			var r = new Dictionary<string, string>();
			if (src == null) return r;
			foreach (var kv in src)
				r[kv.Key] = kv.Value as string ?? (kv.Value == null ? null : Convert.ToString(kv.Value, CultureInfo.InvariantCulture));
			return r;
		}
	}

	internal class SNetLobbyCache
	{
		readonly Dictionary<ulong, SNetLobbyState> _lobbies = new Dictionary<ulong, SNetLobbyState>();

		public SNetLobbyState Get(ulong id) { SNetLobbyState s; return _lobbies.TryGetValue(id, out s) ? s : null; }

		public SNetLobbyState Ingest(Dictionary<string, object> snapshot)
		{
			if (snapshot == null) return null;
			var id = WebworksJson.Ulong(snapshot, "id");
			if (id == 0) return null;
			SNetLobbyState s;
			if (!_lobbies.TryGetValue(id, out s)) { s = new SNetLobbyState(); _lobbies[id] = s; }
			s.Apply(snapshot);
			return s;
		}

		public void Remove(ulong id) { _lobbies.Remove(id); }

		public IEnumerable<SNetLobbyState> All { get { return _lobbies.Values; } }
	}
}
