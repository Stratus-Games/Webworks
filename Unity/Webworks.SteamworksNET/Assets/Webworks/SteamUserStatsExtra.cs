using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	// The rest of the stats surface: other players' stats, global achievement percentages,
	// progress limits and leaderboards filtered to a set of users. Data comes from the
	// same /UserStats routes the rest of the shim uses.
	public static partial class SteamUserStats
	{
		static readonly Dictionary<ulong, Dictionary<string, double>> _userStats = new Dictionary<ulong, Dictionary<string, double>>();
		static readonly Dictionary<ulong, Dictionary<string, KeyValuePair<bool, uint>>> _userAchievements = new Dictionary<ulong, Dictionary<string, KeyValuePair<bool, uint>>>();
		static readonly Dictionary<string, float> _achievementPercent = new Dictionary<string, float>();
		static readonly Dictionary<string, int> _achievementMaxProgress = new Dictionary<string, int>();
		static readonly List<string> _percentOrder = new List<string>();

		static double NumberOf(object value)
		{
			if (value == null) return 0;
			if (value is double) return (double)value;
			if (value is int) return (int)value;
			if (value is long) return (long)value;
			if (value is bool) return (bool)value ? 1 : 0;
			double parsed;
			return double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ? parsed : 0;
		}

		public static int GetAchievementIcon(string pchName)
		{
			// Webworks has no achievement art service, so there is no icon handle to hand
			// out; callers get 0 and can fall back to their own local art.
			return 0;
		}

		public static SteamAPICall_t RequestUserStats(CSteamID steamIDUser)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.GetAsync("/UserStats/UserStats?steamId=" + steamIDUser.m_SteamID));
				var stats = new Dictionary<string, double>();
				var achievements = new Dictionary<string, KeyValuePair<bool, uint>>();

				if (d != null)
				{
					var statMap = WebworksJson.Map(d, "stats");
					if (statMap != null)
						foreach (var kv in statMap) stats[kv.Key] = NumberOf(kv.Value);

					var achMap = WebworksJson.Map(d, "achievements");
					if (achMap != null)
						foreach (var kv in achMap)
						{
							var entry = kv.Value as Dictionary<string, object>;
							achievements[kv.Key] = new KeyValuePair<bool, uint>(
								WebworksJson.Bool(entry, "unlocked"),
								(uint)WebworksJson.Num(entry, "unlockTime"));
						}
				}

				_userStats[steamIDUser.m_SteamID] = stats;
				_userAchievements[steamIDUser.m_SteamID] = achievements;

				ResultBus.Complete(call, new UserStatsReceived_t
				{
					m_nGameID = AppIdUlong(),
					m_eResult = d != null ? EResult.k_EResultOK : EResult.k_EResultFail,
					m_steamIDUser = steamIDUser.m_SteamID,
				});
				return "";
			}, "RequestUserStats");
			return call;
		}

		public static bool GetUserStat(CSteamID steamIDUser, string pchName, out int pData)
		{
			pData = 0;
			Dictionary<string, double> stats;
			if (pchName == null || !_userStats.TryGetValue(steamIDUser.m_SteamID, out stats) || !stats.ContainsKey(pchName)) return false;
			pData = (int)stats[pchName];
			return true;
		}

		public static bool GetUserStat(CSteamID steamIDUser, string pchName, out float pData)
		{
			pData = 0f;
			Dictionary<string, double> stats;
			if (pchName == null || !_userStats.TryGetValue(steamIDUser.m_SteamID, out stats) || !stats.ContainsKey(pchName)) return false;
			pData = (float)stats[pchName];
			return true;
		}

		static bool FriendAchievement(CSteamID steamIDUser, string pchName, out KeyValuePair<bool, uint> value)
		{
			value = default(KeyValuePair<bool, uint>);
			Dictionary<string, KeyValuePair<bool, uint>> achievements;
			return pchName != null
				&& _userAchievements.TryGetValue(steamIDUser.m_SteamID, out achievements)
				&& achievements.TryGetValue(pchName, out value);
		}

		public static bool GetUserAchievement(CSteamID steamIDUser, string pchName, out bool pbAchieved)
		{
			KeyValuePair<bool, uint> value;
			pbAchieved = false;
			if (!FriendAchievement(steamIDUser, pchName, out value)) return false;
			pbAchieved = value.Key;
			return true;
		}

		public static bool GetUserAchievementAndUnlockTime(CSteamID steamIDUser, string pchName, out bool pbAchieved, out uint punUnlockTime)
		{
			KeyValuePair<bool, uint> value;
			pbAchieved = false;
			punUnlockTime = 0;
			if (!FriendAchievement(steamIDUser, pchName, out value)) return false;
			pbAchieved = value.Key;
			punUnlockTime = value.Value;
			return true;
		}

		public static SteamAPICall_t DownloadLeaderboardEntriesForUsers(SteamLeaderboard_t hSteamLeaderboard, CSteamID[] prgUsers, int cUsers)
		{
			var call = ResultBus.NewCall();
			LeaderboardInfo info;
			if (!_boards.TryGetValue(hSteamLeaderboard.m_SteamLeaderboard, out info))
			{
				MainThreadDispatcher.Enqueue(() => ResultBus.Complete(call, new LeaderboardScoresDownloaded_t { m_hSteamLeaderboard = hSteamLeaderboard.m_SteamLeaderboard }));
				return call;
			}

			var users = new StringBuilder();
			if (prgUsers != null)
				for (int i = 0; i < cUsers && i < prgUsers.Length; i++)
				{
					if (users.Length > 0) users.Append(',');
					users.Append(prgUsers[i].m_SteamID.ToString(CultureInfo.InvariantCulture));
				}

			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.GetAsync(
					"/UserStats/Leaderboard/EntriesForUsers?name=" + Uri.EscapeDataString(info.Name) + "&users=" + users));
				var list = new List<LeaderboardEntry_t>();
				var arr = d != null ? WebworksJson.Arr(d, "entries") : null;
				if (arr != null)
					foreach (var o in arr)
					{
						var e = o as Dictionary<string, object>;
						if (e == null) continue;
						ulong id = WebworksJson.Ulong(e, "steamId");
						SNetState.Players.Set(id, WebworksJson.Str(e, "name"));
						list.Add(new LeaderboardEntry_t
						{
							m_steamIDUser = id,
							m_nGlobalRank = WebworksJson.Int(e, "globalRank"),
							m_nScore = WebworksJson.Int(e, "score"),
						});
					}

				var handle = _nextEntriesHandle++;
				_entryPages[handle] = list.ToArray();
				ResultBus.Complete(call, new LeaderboardScoresDownloaded_t
				{
					m_hSteamLeaderboard = hSteamLeaderboard.m_SteamLeaderboard,
					m_hSteamLeaderboardEntries = handle,
					m_cEntryCount = list.Count,
				});
				return "";
			}, "DownloadLeaderboardEntriesForUsers");
			return call;
		}

		public static SteamAPICall_t AttachLeaderboardUGC(SteamLeaderboard_t hSteamLeaderboard, UGCHandle_t hUGC)
		{
			var call = ResultBus.NewCall();
			LeaderboardInfo info;
			if (!_boards.TryGetValue(hSteamLeaderboard.m_SteamLeaderboard, out info))
			{
				MainThreadDispatcher.Enqueue(() => ResultBus.Complete(call, new LeaderboardUGCSet_t
				{
					m_eResult = EResult.k_EResultInvalidParam,
					m_hSteamLeaderboard = hSteamLeaderboard,
				}));
				return call;
			}

			var body = "{\"name\":" + WebworksJson.Quote(info.Name) + ",\"ugcHandle\":\"" + hUGC.m_UGCHandle.ToString(CultureInfo.InvariantCulture) + "\"}";
			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.PostAsync("/UserStats/Leaderboard/AttachUGC", body));
				ResultBus.Complete(call, new LeaderboardUGCSet_t
				{
					m_eResult = d != null && WebworksJson.Str(d, "error") == null ? EResult.k_EResultOK : EResult.k_EResultFail,
					m_hSteamLeaderboard = hSteamLeaderboard,
				});
				return "";
			}, "AttachLeaderboardUGC");
			return call;
		}

		public static SteamAPICall_t GetNumberOfCurrentPlayers()
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.GetAsync("/UserStats/PlayerCount"));
				ResultBus.Complete(call, new NumberOfCurrentPlayers_t
				{
					m_bSuccess = (byte)(d != null ? 1 : 0),
					m_cPlayers = d != null ? WebworksJson.Int(d, "count") : 0,
				});
				return "";
			}, "GetNumberOfCurrentPlayers");
			return call;
		}

		public static SteamAPICall_t RequestGlobalAchievementPercentages()
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.GetAsync("/UserStats/Achievements/Percentages"));

				_achievementPercent.Clear();
				_achievementMaxProgress.Clear();
				_percentOrder.Clear();

				var arr = d != null ? WebworksJson.Arr(d, "achievements") : null;
				if (arr != null)
					foreach (var o in arr)
					{
						var entry = o as Dictionary<string, object>;
						var key = WebworksJson.Str(entry, "key");
						if (string.IsNullOrEmpty(key)) continue;
						_achievementPercent[key] = (float)WebworksJson.Num(entry, "percent");
						_achievementMaxProgress[key] = WebworksJson.Int(entry, "maxProgress");
						_percentOrder.Add(key);
					}

				_percentOrder.Sort((a, b) => _achievementPercent[b].CompareTo(_achievementPercent[a]));

				ResultBus.Complete(call, new GlobalAchievementPercentagesReady_t
				{
					m_nGameID = AppIdUlong(),
					m_eResult = d != null ? EResult.k_EResultOK : EResult.k_EResultFail,
				});
				return "";
			}, "RequestGlobalAchievementPercentages");
			return call;
		}

		public static int GetMostAchievedAchievementInfo(out string pchName, uint unNameBufLen, out float pflPercent, out bool pbAchieved)
		{
			return PercentInfo(0, out pchName, out pflPercent, out pbAchieved);
		}

		public static int GetNextMostAchievedAchievementInfo(int iIteratorPrevious, out string pchName, out float pflPercent, out bool pbAchieved)
		{
			return PercentInfo(iIteratorPrevious + 1, out pchName, out pflPercent, out pbAchieved);
		}

		static int PercentInfo(int index, out string pchName, out float pflPercent, out bool pbAchieved)
		{
			pchName = string.Empty;
			pflPercent = 0f;
			pbAchieved = false;
			if (index < 0 || index >= _percentOrder.Count) return -1;

			pchName = _percentOrder[index];
			pflPercent = _achievementPercent[pchName];
			pbAchieved = Info(pchName).Unlocked;
			return index + 1 < _percentOrder.Count ? index : -1;
		}

		public static bool GetAchievementAchievedPercent(string pchName, out float pflPercent)
		{
			pflPercent = 0f;
			if (pchName == null || !_achievementPercent.TryGetValue(pchName, out pflPercent)) return false;
			return true;
		}

		public static bool GetAchievementProgressLimits(string pchName, out int pnMinProgress, out int pnMaxProgress)
		{
			pnMinProgress = 0;
			pnMaxProgress = 0;
			int max;
			if (pchName == null || !_achievementMaxProgress.TryGetValue(pchName, out max) || max <= 0) return false;
			pnMaxProgress = max;
			return true;
		}
	}
}
