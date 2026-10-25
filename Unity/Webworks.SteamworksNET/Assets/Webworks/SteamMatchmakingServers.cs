using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	public static partial class SteamMatchmakingServers
	{
		class ServerList
		{
			public long Id;
			public List<gameserveritem_t> Servers = new List<gameserveritem_t>();
			public bool Refreshing;
			public ISteamMatchmakingServerListResponse Response;
			public ulong AppId;
			public Dictionary<string, string> Filters = new Dictionary<string, string>();
		}

		static readonly Dictionary<long, ServerList> _lists = new Dictionary<long, ServerList>();
		static long _nextList = 1;

		static uint IpToRaw(string ip)
		{
			if (string.IsNullOrEmpty(ip)) return 0;
			var parts = ip.Split('.');
			if (parts.Length != 4) return 0;
			uint value = 0;
			foreach (var part in parts)
			{
				byte octet;
				byte.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out octet);
				value = (value << 8) | octet;
			}
			return value;
		}

		static gameserveritem_t ToItem(Dictionary<string, object> entry, ulong appId)
		{
			var item = new gameserveritem_t();
			var gamePort = (ushort)WebworksJson.Int(entry, "gamePort");
			var queryPort = (ushort)WebworksJson.Int(entry, "queryPort");
			item.m_NetAdr.Init(IpToRaw(WebworksJson.Str(entry, "ip")), queryPort, gamePort);
			item.m_nPing = 0;
			item.m_bHadSuccessfulResponse = true;
			item.m_bDoNotRefresh = false;
			item.SetGameDir("");
			item.SetMap(WebworksJson.Str(entry, "map") ?? "");
			item.SetGameDescription("");
			item.m_nAppID = (uint)appId;
			item.m_nPlayers = WebworksJson.Int(entry, "playerCount");
			item.m_nMaxPlayers = WebworksJson.Int(entry, "maxPlayers");
			item.m_nBotPlayers = WebworksJson.Int(entry, "botCount");
			item.m_bPassword = WebworksJson.Bool(entry, "passworded");
			item.m_bSecure = WebworksJson.Bool(entry, "secure");
			item.m_ulTimeLastPlayed = 0;
			item.m_nServerVersion = 1;
			item.SetServerName(WebworksJson.Str(entry, "name") ?? "");
			item.SetGameTags(WebworksJson.Str(entry, "tags") ?? "");
			ulong id;
			ulong.TryParse(WebworksJson.Str(entry, "id") ?? "0", NumberStyles.Integer, CultureInfo.InvariantCulture, out id);
			item.m_steamID = new CSteamID(id);
			return item;
		}

		static string QueryString(ServerList list)
		{
			var query = "?";
			string map, tags, text;
			if (list.Filters.TryGetValue("map", out map)) query += "map=" + Uri.EscapeDataString(map) + "&";
			if (list.Filters.TryGetValue("gametagsand", out tags)) query += "tags=" + Uri.EscapeDataString(tags) + "&";
			if (list.Filters.TryGetValue("gamedata", out text)) query += "text=" + Uri.EscapeDataString(text) + "&";
			if (list.Filters.ContainsKey("notfull") || list.Filters.ContainsKey("empty")) query += "notFull=1&";
			if (list.Filters.ContainsKey("secure")) query += "secureOnly=1&";
			if (list.Filters.ContainsKey("password"))
			{
				var wanted = list.Filters["password"];
				if (wanted == "0") query += "noPassword=1&";
			}
			return query.TrimEnd('&');
		}

		static HServerListRequest Request(AppId_t app, MatchMakingKeyValuePair_t[] filters, uint count, ISteamMatchmakingServerListResponse response)
		{
			var list = new ServerList
			{
				Id = _nextList++,
				Response = response,
				AppId = app.m_AppId,
				Refreshing = true,
			};

			if (filters != null)
			{
				for (uint i = 0; i < count && i < filters.Length; i++)
				{
					if (string.IsNullOrEmpty(filters[i].m_szKey)) continue;
					list.Filters[filters[i].m_szKey.ToLowerInvariant()] = filters[i].m_szValue ?? "";
				}
			}

			_lists[list.Id] = list;
			var handle = new HServerListRequest(new IntPtr(list.Id));
			Refresh(list, handle);
			return handle;
		}

		static void Refresh(ServerList list, HServerListRequest handle)
		{
			list.Refreshing = true;
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/GameServer/List" + QueryString(list)));
				list.Servers.Clear();
				var entries = payload != null ? WebworksJson.Arr(payload, "entries") : null;
				if (entries != null)
				{
					foreach (var raw in entries)
					{
						var entry = raw as Dictionary<string, object>;
						if (entry == null) continue;
						list.Servers.Add(ToItem(entry, list.AppId));
					}
				}

				list.Refreshing = false;
				for (int i = 0; i < list.Servers.Count; i++)
				{
					try { list.Response?.ServerResponded(handle, i); } catch (Exception ex) { Debug.LogException(ex); }
				}
				try { list.Response?.RefreshComplete(handle, EMatchMakingServerResponse.eServerResponded); } catch (Exception ex) { Debug.LogException(ex); }
				return "";
			}, "server list");
		}

		public static HServerListRequest RequestInternetServerList(AppId_t iApp, MatchMakingKeyValuePair_t[] ppchFilters, uint nFilters, ISteamMatchmakingServerListResponse pRequestServersResponse)
		{
			return Request(iApp, ppchFilters, nFilters, pRequestServersResponse);
		}

		public static HServerListRequest RequestLANServerList(AppId_t iApp, ISteamMatchmakingServerListResponse pRequestServersResponse)
		{
			return Request(iApp, null, 0, pRequestServersResponse);
		}

		public static HServerListRequest RequestFriendsServerList(AppId_t iApp, MatchMakingKeyValuePair_t[] ppchFilters, uint nFilters, ISteamMatchmakingServerListResponse pRequestServersResponse)
		{
			return Request(iApp, ppchFilters, nFilters, pRequestServersResponse);
		}

		public static HServerListRequest RequestFavoritesServerList(AppId_t iApp, MatchMakingKeyValuePair_t[] ppchFilters, uint nFilters, ISteamMatchmakingServerListResponse pRequestServersResponse)
		{
			return Request(iApp, ppchFilters, nFilters, pRequestServersResponse);
		}

		public static HServerListRequest RequestHistoryServerList(AppId_t iApp, MatchMakingKeyValuePair_t[] ppchFilters, uint nFilters, ISteamMatchmakingServerListResponse pRequestServersResponse)
		{
			return Request(iApp, ppchFilters, nFilters, pRequestServersResponse);
		}

		public static HServerListRequest RequestSpectatorServerList(AppId_t iApp, MatchMakingKeyValuePair_t[] ppchFilters, uint nFilters, ISteamMatchmakingServerListResponse pRequestServersResponse)
		{
			return Request(iApp, ppchFilters, nFilters, pRequestServersResponse);
		}

		public static void ReleaseRequest(HServerListRequest hServerListRequest)
		{
			_lists.Remove(hServerListRequest.m_HServerListRequest.ToInt64());
		}

		static ServerList ListOf(HServerListRequest handle)
		{
			ServerList list;
			return _lists.TryGetValue(handle.m_HServerListRequest.ToInt64(), out list) ? list : null;
		}

		public static gameserveritem_t GetServerDetails(HServerListRequest hRequest, int iServer)
		{
			var list = ListOf(hRequest);
			if (list == null || iServer < 0 || iServer >= list.Servers.Count) return null;
			return list.Servers[iServer];
		}

		public static void CancelQuery(HServerListRequest hRequest)
		{
			var list = ListOf(hRequest);
			if (list != null) list.Refreshing = false;
		}

		public static bool IsRefreshing(HServerListRequest hRequest)
		{
			var list = ListOf(hRequest);
			return list != null && list.Refreshing;
		}

		public static int GetServerCount(HServerListRequest hRequest)
		{
			var list = ListOf(hRequest);
			return list != null ? list.Servers.Count : 0;
		}

		public static void RefreshServer(HServerListRequest hRequest, int iServer)
		{
			var list = ListOf(hRequest);
			if (list == null) return;
			if (iServer >= 0 && iServer < list.Servers.Count)
			{
				try { list.Response?.ServerResponded(hRequest, iServer); } catch (Exception ex) { Debug.LogException(ex); }
			}
			try { list.Response?.RefreshComplete(hRequest, EMatchMakingServerResponse.eServerResponded); } catch (Exception ex) { Debug.LogException(ex); }
		}

		public static HServerQuery PingServer(uint unIP, ushort usPort, ISteamMatchmakingPingResponse pRequestServersResponse)
		{
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/GameServer/List"));
				var entries = payload != null ? WebworksJson.Arr(payload, "entries") : null;
				gameserveritem_t found = null;
				if (entries != null)
				{
					foreach (var raw in entries)
					{
						var entry = raw as Dictionary<string, object>;
						if (entry == null) continue;
						var item = ToItem(entry, 0);
						if (item.m_NetAdr.GetIP() != unIP) continue;
						if (item.m_NetAdr.GetQueryPort() != usPort && item.m_NetAdr.GetConnectionPort() != usPort) continue;
						found = item;
						break;
					}
				}

				if (found != null) pRequestServersResponse?.ServerResponded(found);
				else pRequestServersResponse?.ServerFailedToRespond();
				return "";
			}, "PingServer");
			return new HServerQuery(1);
		}

		public static HServerQuery PlayerDetails(uint unIP, ushort usPort, ISteamMatchmakingPlayersResponse pRequestServersResponse)
		{
			pRequestServersResponse?.PlayersRefreshComplete();
			return HServerQuery.Invalid;
		}

		public static HServerQuery ServerRules(uint unIP, ushort usPort, ISteamMatchmakingRulesResponse pRequestServersResponse)
		{
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/GameServer/List"));
				var entries = payload != null ? WebworksJson.Arr(payload, "entries") : null;
				if (entries != null)
				{
					foreach (var raw in entries)
					{
						var entry = raw as Dictionary<string, object>;
						if (entry == null) continue;
						var item = ToItem(entry, 0);
						if (item.m_NetAdr.GetIP() != unIP) continue;
						if (item.m_NetAdr.GetQueryPort() != usPort && item.m_NetAdr.GetConnectionPort() != usPort) continue;

						var rules = WebworksJson.Map(entry, "rules");
						if (rules != null)
						{
							foreach (var kv in rules) pRequestServersResponse?.RulesResponded(kv.Key, kv.Value is string ? (string)kv.Value : "");
						}
						pRequestServersResponse?.RulesRefreshComplete();
						return "";
					}
				}
				pRequestServersResponse?.RulesFailedToRespond();
				return "";
			}, "ServerRules");
			return new HServerQuery(1);
		}

		public static void CancelServerQuery(HServerQuery hServerQuery) { }
	}
}
