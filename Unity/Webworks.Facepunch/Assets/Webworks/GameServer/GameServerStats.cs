using System.Threading.Tasks;
using Webworks.Internal;
using Webworks.GameServer;

namespace Steamworks
{
	public static class SteamServerStats
	{
		public static bool GetAchievement(SteamId steamid, string name)
		{
			var v = _cache.TryGet(steamid, name);
			return v.HasValue && v.Value;
		}

		public static bool SetAchievement(SteamId steamid, string name) { return WriteUnlocked(steamid, name, true); }
		public static bool ClearAchievement(SteamId steamid, string name) { return WriteUnlocked(steamid, name, false); }

		static bool WriteUnlocked(SteamId steamid, string name, bool unlocked)
		{
			var body = "{\"steamId\":" + WebworksJson.Quote(steamid.Value.ToString()) + ",\"" + (unlocked ? "unlocked" : "cleared") + "\":[" + WebworksJson.Quote(name) + "]}";
			GameServerApi.Fire(() => GameServerApi.PostAsync("/GameServer/Stats", body), "SetAchievement");
			return true;
		}

		public static int GetInt(SteamId steamid, string name, int defaultValue = 0)
		{
			var record = _cache.TryGetStats(steamid);
			if (record != null && record.TryGetValue(name, out var v) && v is double d) return (int)d;
			return defaultValue;
		}

		public static float GetFloat(SteamId steamid, string name, float defaultValue = 0f)
		{
			var record = _cache.TryGetStats(steamid);
			if (record != null && record.TryGetValue(name, out var v) && v is double d) return (float)d;
			return defaultValue;
		}

		public static bool SetInt(SteamId steamid, string name, int stat) { return SetStat(steamid, name, stat); }
		public static bool SetFloat(SteamId steamid, string name, float stat) { return SetStat(steamid, name, stat); }

		static bool SetStat(SteamId steamid, string name, double value)
		{
			var body = "{\"steamId\":" + WebworksJson.Quote(steamid.Value.ToString()) + ",\"stats\":{" + WebworksJson.Quote(name) + ":" + value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}}";
			GameServerApi.Fire(() => GameServerApi.PostAsync("/GameServer/Stats", body), "SetStat");
			return true;
		}

		public static async Task<Result> RequestUserStatsAsync(SteamId steamid)
		{
			var d = WebworksJson.Obj(await GameServerApi.GetAsync("/GameServer/Stats?steamId=" + steamid.Value));
			if (d == null) return Result.Fail;
			_cache.Store(steamid, WebworksJson.Map(d, "stats"), WebworksJson.Map(d, "achievements"));
			return Result.OK;
		}

		public static Task<Result> StoreUserStats(SteamId steamid) { return Task.FromResult(Result.OK); }

		static readonly StatsCache _cache = new StatsCache();

		class StatsCache
		{
			readonly System.Collections.Generic.Dictionary<ulong, System.Collections.Generic.Dictionary<string, object>> _stats = new System.Collections.Generic.Dictionary<ulong, System.Collections.Generic.Dictionary<string, object>>();
			readonly System.Collections.Generic.Dictionary<ulong, System.Collections.Generic.Dictionary<string, object>> _achievements = new System.Collections.Generic.Dictionary<ulong, System.Collections.Generic.Dictionary<string, object>>();

			public void Store(SteamId id, System.Collections.Generic.Dictionary<string, object> stats, System.Collections.Generic.Dictionary<string, object> achievements)
			{
				_stats[id.Value] = stats ?? new System.Collections.Generic.Dictionary<string, object>();
				_achievements[id.Value] = achievements ?? new System.Collections.Generic.Dictionary<string, object>();
			}

			public System.Collections.Generic.Dictionary<string, object> TryGetStats(SteamId id)
			{
				return _stats.TryGetValue(id.Value, out var v) ? v : null;
			}

			public bool? TryGet(SteamId id, string achievementName)
			{
				if (!_achievements.TryGetValue(id.Value, out var a) || !a.TryGetValue(achievementName, out var entry)) return null;
				var d = entry as System.Collections.Generic.Dictionary<string, object>;
				return d != null && WebworksJson.Bool(d, "unlocked");
			}
		}
	}
}
