using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Steamworks.Data;
using Webworks.Internal;
using Webworks.GameServer;

namespace Steamworks.ServerList
{
	public abstract class Base : IDisposable
	{
		public AppId AppId { get; set; }
		public event Action OnChanges;
		public event Action<ServerInfo> OnResponsiveServer;
		public List<ServerInfo> Responsive = new List<ServerInfo>();
		public List<ServerInfo> Unresponsive = new List<ServerInfo>();
		public List<ServerInfo> Unqueried = new List<ServerInfo>();

		internal readonly Dictionary<string, string> Filters = new Dictionary<string, string>();
		public void AddFilter(string key, string value) { Filters[key] = value; }

		public void Cancel() { }

		public virtual async Task<bool> RunQueryAsync(float timeoutSeconds = 10f)
		{
			var q = "?";
			string map, tags;
			if (Filters.TryGetValue("map", out map)) q += "map=" + Uri.EscapeDataString(map) + "&";
			if (Filters.TryGetValue("gametagsand", out tags)) q += "tags=" + Uri.EscapeDataString(tags) + "&";
			if (Filters.ContainsKey("empty") || Filters.ContainsKey("notfull")) q += "notFull=1&";
			if (Filters.ContainsKey("secure")) q += "secureOnly=1&";
			if (Filters.ContainsKey("password")) q += "noPassword=" + (Filters["password"] == "0" ? "1" : "0") + "&";

			var d = WebworksJson.Obj(await GameServerApi.GetAsync("/GameServer/List" + q));
			var arr = d != null ? WebworksJson.Arr(d, "entries") : null;

			Responsive.Clear();
			Unresponsive.Clear();
			Unqueried.Clear();

			if (arr != null)
			{
				foreach (var o in arr)
				{
					var m = o as Dictionary<string, object>;
					if (m == null) continue;
					var info = ToServerInfo(m);
					if (Accept(info))
					{
						Responsive.Add(info);
						OnResponsiveServer?.Invoke(info);
					}
				}
			}

			OnChanges?.Invoke();
			return true;
		}

		internal virtual bool Accept(ServerInfo info) { return true; }

		public void UpdateResponsive() { }

		static ServerInfo ToServerInfo(Dictionary<string, object> m)
		{
			ulong id; ulong.TryParse(WebworksJson.Str(m, "id") ?? "0", out id);
			return new ServerInfo
			{
				Name = WebworksJson.Str(m, "name") ?? "",
				Map = WebworksJson.Str(m, "map") ?? "",
				GameDir = "",
				AppId = 0,
				Players = WebworksJson.Int(m, "playerCount"),
				MaxPlayers = WebworksJson.Int(m, "maxPlayers"),
				BotPlayers = WebworksJson.Int(m, "botCount"),
				Passworded = WebworksJson.Bool(m, "passworded"),
				Secure = WebworksJson.Bool(m, "secure"),
				TagString = WebworksJson.Str(m, "tags") ?? "",
				SteamId = id,
				AddressRaw = IpToRaw(WebworksJson.Str(m, "ip")),
				ConnectionPort = WebworksJson.Int(m, "gamePort"),
				QueryPort = WebworksJson.Int(m, "queryPort"),
				Ping = 0,
			};
		}

		static uint IpToRaw(string ip)
		{
			if (string.IsNullOrEmpty(ip)) return 0;
			var parts = ip.Split('.');
			if (parts.Length != 4) return 0;
			uint result = 0;
			foreach (var p in parts) { byte b; byte.TryParse(p, out b); result = (result << 8) | b; }
			return result;
		}

		public virtual void Dispose() { }
	}

	public class Internet : Base { }

	public class Favourites : Base
	{
		internal override bool Accept(ServerInfo info) { return LocalServerLists.Favourites.Contains(info.SteamId.Value); }
	}

	public class History : Base
	{
		internal override bool Accept(ServerInfo info) { return LocalServerLists.History.Contains(info.SteamId.Value); }
	}

	public class Friends : Base { }

	public class LocalNetwork : Base { }

	public class IpList : Base
	{
		public List<string> Ips = new List<string>();
		public IpList(IEnumerable<string> list) { if (list != null) Ips.AddRange(list); }
		public IpList(params string[] list) { if (list != null) Ips.AddRange(list); }

		public override async Task<bool> RunQueryAsync(float timeoutSeconds = 10f)
		{
			var ok = await base.RunQueryAsync(timeoutSeconds);
			Responsive = Responsive.Where(r => Ips.Contains(r.Address.ToString())).ToList();
			return ok;
		}
	}
}

namespace Steamworks
{
	using Steamworks.Data;
	using Steamworks.ServerList;

	public static partial class SteamMatchmaking
	{
		public static IEnumerable<ServerInfo> GetFavoriteServers() { return LocalServerLists.Favourites.All().Select(id => new ServerInfo { SteamId = id }); }
		public static IEnumerable<ServerInfo> GetHistoryServers() { return LocalServerLists.History.All().Select(id => new ServerInfo { SteamId = id }); }
	}
}
