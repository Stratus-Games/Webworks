using System;
using System.Globalization;
using Webworks.SNetInternal;

namespace Steamworks
{
	// Server-side stat writes go to the same per-game stat store as the client, using the
	// game-server session token so a dedicated server can author player stats.
	public static partial class SteamGameServerStats
	{
		static void Post(CSteamID user, string name, object value, bool achievement = false, bool clear = false)
		{
			if (user.m_SteamID == 0 || string.IsNullOrEmpty(name) || !SteamGameServer.BLoggedOn()) return;

			var body = "{\"steamId\":" + WebworksJson.Quote(user.m_SteamID.ToString(CultureInfo.InvariantCulture));
			if (achievement)
			{
				body += clear ? ",\"cleared\":[" : ",\"unlocked\":[";
				body += WebworksJson.Quote(name) + "]";
			}
			else
			{
				body += ",\"stats\":{" + WebworksJson.Quote(name) + ":" + Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture) + "}";
			}
			body += "}";

			ApiClient.Fire(() => ApiClient.PostAsync("/GameServer/Stats", body), "server player stat");
		}

		public static SteamAPICall_t RequestUserStats(CSteamID steamIDUser)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/GameServer/Stats?steamId=" + Uri.EscapeDataString(steamIDUser.m_SteamID.ToString(CultureInfo.InvariantCulture))));
				var ok = payload != null && WebworksJson.Str(payload, "error") == null;
				ResultBus.Complete(call, new GSStatsReceived_t
				{
					m_eResult = ok ? EResult.k_EResultOK : EResult.k_EResultFail,
					m_steamIDUser = steamIDUser,
				});
				return (string)null;
			}, "RequestUserStats");
			return call;
		}

		public static bool GetUserStat(CSteamID steamIDUser, string pchName, out int pData)
		{
			pData = 0;
			var cached = StatsCache.Get(steamIDUser, pchName);
			if (!cached.HasValue) return false;
			pData = (int)cached.Value;
			return true;
		}

		public static bool GetUserStat(CSteamID steamIDUser, string pchName, out float pData)
		{
			pData = 0f;
			var cached = StatsCache.Get(steamIDUser, pchName);
			if (!cached.HasValue) return false;
			pData = (float)cached.Value;
			return true;
		}

		public static bool GetUserAchievement(CSteamID steamIDUser, string pchName, out bool pbAchieved)
		{
			pbAchieved = StatsCache.Achievement(steamIDUser, pchName);
			return true;
		}

		public static bool GetUserAchievementAndUnlockTime(CSteamID steamIDUser, string pchName, out bool pbAchieved, out uint punUnlockTime)
		{
			pbAchieved = StatsCache.Achievement(steamIDUser, pchName);
			punUnlockTime = pbAchieved ? (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds() : 0u;
			return true;
		}

		public static bool SetUserStat(CSteamID steamIDUser, string pchName, int nData)
		{
			StatsCache.Set(steamIDUser, pchName, nData);
			Post(steamIDUser, pchName, nData);
			return true;
		}

		public static bool SetUserStat(CSteamID steamIDUser, string pchName, float fData)
		{
			StatsCache.Set(steamIDUser, pchName, fData);
			Post(steamIDUser, pchName, fData);
			return true;
		}

		public static bool UpdateUserAvgRateStat(CSteamID steamIDUser, string pchName, float flCountThisSession, double dSessionLength)
		{
			if (dSessionLength <= 0) return false;
			var value = (float)(flCountThisSession / dSessionLength);
			StatsCache.Set(steamIDUser, pchName, value);
			Post(steamIDUser, pchName, value);
			return true;
		}

		public static bool SetUserAchievement(CSteamID steamIDUser, string pchName)
		{
			StatsCache.SetAchievement(steamIDUser, pchName, true);
			Post(steamIDUser, pchName, 1, achievement: true);
			return true;
		}

		public static bool ClearUserAchievement(CSteamID steamIDUser, string pchName)
		{
			StatsCache.SetAchievement(steamIDUser, pchName, false);
			Post(steamIDUser, pchName, 0, achievement: true, clear: true);
			return true;
		}

		public static SteamAPICall_t StoreUserStats(CSteamID steamIDUser)
		{
			var call = ResultBus.NewCall();
			ResultBus.Complete(call, new GSStatsStored_t { m_eResult = EResult.k_EResultOK, m_steamIDUser = steamIDUser });
			return call;
		}

		internal static class StatsCache
		{
			static readonly System.Collections.Generic.Dictionary<string, double> _stats = new System.Collections.Generic.Dictionary<string, double>();
			static readonly System.Collections.Generic.Dictionary<string, bool> _achievements = new System.Collections.Generic.Dictionary<string, bool>();

			static string Key(CSteamID user, string name) { return user.m_SteamID + "\u0000" + (name ?? ""); }

			public static void Set(CSteamID user, string name, double value) { _stats[Key(user, name)] = value; }

			public static double? Get(CSteamID user, string name)
			{
				double value;
				return _stats.TryGetValue(Key(user, name), out value) ? value : (double?)null;
			}

			public static void SetAchievement(CSteamID user, string name, bool unlocked) { _achievements[Key(user, name)] = unlocked; }

			public static bool Achievement(CSteamID user, string name)
			{
				bool value;
				return _achievements.TryGetValue(Key(user, name), out value) && value;
			}
		}
	}
}
