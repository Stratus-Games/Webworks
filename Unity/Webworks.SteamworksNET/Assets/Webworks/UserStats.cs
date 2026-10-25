using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	public static partial class SteamUserStats
	{
		class AchInfo { public string Name, Description; public bool Unlocked; public bool Hidden; public DateTime? UnlockTime; public int NumericId; }

		// Catalog order defines the numeric achievement index the Steamworks API uses.
		static readonly List<AchInfo> _catalog = new List<AchInfo>();
		static readonly Dictionary<string, int> _catalogIndex = new Dictionary<string, int>();

		static readonly Dictionary<string, double> _stats = new Dictionary<string, double>();
		static readonly Dictionary<string, AchInfo> _ach = new Dictionary<string, AchInfo>();
		static readonly HashSet<string> _dirtyStats = new HashSet<string>();
		static readonly HashSet<string> _dirtyUnlock = new HashSet<string>();
		static readonly HashSet<string> _dirtyClear = new HashSet<string>();
		static bool _statsReceived;

		public static bool RequestCurrentStats()
		{
			ApiClient.Fire(LoadAsync, "RequestCurrentStats");
			return true;
		}

		static async Task<string> LoadAsync()
		{
			var stats = WebworksJson.Map(WebworksJson.Obj(await ApiClient.GetAsync("/UserStats/Stats")), "stats");
			if (stats != null)
				foreach (var kv in stats)
					if (kv.Value is double && !_dirtyStats.Contains(kv.Key)) _stats[kv.Key] = (double)kv.Value;

			var list = WebworksJson.Arr(WebworksJson.Obj(await ApiClient.GetAsync("/UserStats/Achievements")), "achievements");
			if (list != null)
				foreach (var o in list)
				{
					var a = o as Dictionary<string, object>;
					var id = WebworksJson.Str(a, "id");
					if (id == null) continue;
					var info = Info(id);
					info.Name = WebworksJson.Str(a, "name") ?? id;
					info.Description = WebworksJson.Str(a, "description") ?? "";
					info.Hidden = WebworksJson.Bool(a, "hidden");
					info.NumericId = WebworksJson.Int(a, "numericId");
					if (!_dirtyUnlock.Contains(id) && !_dirtyClear.Contains(id)) info.Unlocked = WebworksJson.Bool(a, "unlocked");
					var ms = WebworksJson.Num(a, "unlockTime");
					info.UnlockTime = ms > 0 ? new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ms) : (DateTime?)null;
				}

			var catalog = WebworksJson.Arr(WebworksJson.Obj(await ApiClient.GetAsync("/UserStats/Achievements/Catalog")), "achievements");
			if (catalog != null)
			{
				_catalog.Clear();
				_catalogIndex.Clear();
				foreach (var o in catalog)
				{
					var a = o as Dictionary<string, object>;
					var key = WebworksJson.Str(a, "key");
					if (key == null) continue;
					var entry = Info(key);
					entry.Name = WebworksJson.Str(a, "name") ?? key;
					entry.Description = WebworksJson.Str(a, "description") ?? "";
					entry.Hidden = WebworksJson.Bool(a, "hidden");
					entry.NumericId = WebworksJson.Int(a, "id");
					_catalogIndex[key] = _catalog.Count;
					_catalog.Add(entry);
				}
			}

			_statsReceived = true;
			CallbackBus<UserStatsReceived_t>.Fire(new UserStatsReceived_t { m_nGameID = AppIdUlong(), m_eResult = EResult.k_EResultOK, m_steamIDUser = SteamUser.GetSteamID().m_SteamID });
			return null;
		}

		static ulong AppIdUlong() { uint v; return uint.TryParse(SessionState.AppId, out v) ? v : 0UL; }

		static AchInfo Info(string id)
		{
			AchInfo i;
			if (!_ach.TryGetValue(id, out i)) { i = new AchInfo { Name = id, Description = "" }; _ach[id] = i; }
			return i;
		}


		public static bool GetStat(string pchName, out int pData) { double v; pData = _stats.TryGetValue(pchName, out v) ? (int)v : 0; return _statsReceived; }
		public static bool GetStat(string pchName, out float pData) { double v; pData = _stats.TryGetValue(pchName, out v) ? (float)v : 0f; return _statsReceived; }
		public static bool SetStat(string pchName, int nData) { return Set(pchName, nData); }
		public static bool SetStat(string pchName, float fData) { return Set(pchName, fData); }
		public static bool UpdateAvgRateStat(string pchName, float flCountThisSession, double dSessionLength) { return Set(pchName, flCountThisSession); }

		static bool Set(string name, double value)
		{
			_stats[name] = value;
			_dirtyStats.Add(name);
			return true;
		}

		public static bool StoreStats()
		{
			var sb = new StringBuilder("{\"stats\":{");
			bool first = true;
			foreach (var name in _dirtyStats)
			{
				if (!first) sb.Append(',');
				first = false;
				sb.Append(WebworksJson.Quote(name)).Append(':').Append(_stats[name].ToString("R", CultureInfo.InvariantCulture));
			}
			sb.Append("},\"unlocked\":").Append(JsonArray(_dirtyUnlock));
			sb.Append(",\"cleared\":").Append(JsonArray(_dirtyClear)).Append('}');

			var body = sb.ToString();
			var newlyUnlocked = new List<string>(_dirtyUnlock);
			_dirtyStats.Clear(); _dirtyUnlock.Clear(); _dirtyClear.Clear();

			ApiClient.Fire(async () =>
			{
				await ApiClient.PostAsync("/UserStats/Stats", body);
				var gameId = AppIdUlong();
				foreach (var id in newlyUnlocked)
					CallbackBus<UserAchievementStored_t>.Fire(new UserAchievementStored_t { m_nGameID = gameId, m_bGroupAchievement = false, m_rgchAchievementName = id, m_nCurProgress = 0, m_nMaxProgress = 0 });
				CallbackBus<UserStatsStored_t>.Fire(new UserStatsStored_t { m_nGameID = gameId, m_eResult = EResult.k_EResultOK });
				return (string)null;
			}, "StoreStats");
			return true;
		}

		static string JsonArray(HashSet<string> items)
		{
			var sb = new StringBuilder("[");
			bool first = true;
			foreach (var s in items) { if (!first) sb.Append(','); first = false; sb.Append(WebworksJson.Quote(s)); }
			return sb.Append(']').ToString();
		}


		public static bool GetAchievement(string pchName, out bool pbAchieved) { pbAchieved = Info(pchName).Unlocked; return _statsReceived; }

		public static bool GetAchievementAndUnlockTime(string pchName, out bool pbAchieved, out uint punUnlockTime)
		{
			var info = Info(pchName);
			pbAchieved = info.Unlocked;
			punUnlockTime = info.UnlockTime.HasValue ? (uint)new DateTimeOffset(info.UnlockTime.Value).ToUnixTimeSeconds() : 0u;
			return _statsReceived;
		}

		public static string GetAchievementName(uint iAchievement)
		{
			return iAchievement < _catalog.Count ? _catalog[(int)iAchievement].Name : string.Empty;
		}

		public static string GetAchievementKey(uint iAchievement)
		{
			return iAchievement < _catalog.Count ? InfoKey(_catalog[(int)iAchievement]) : string.Empty;
		}

		static string InfoKey(AchInfo info)
		{
			foreach (var kv in _ach)
				if (ReferenceEquals(kv.Value, info)) return kv.Key;
			return info.Name;
		}

		public static bool SetAchievement(string pchName)
		{
			var i = Info(pchName);
			i.Unlocked = true;
			i.UnlockTime = DateTime.UtcNow;
			_dirtyUnlock.Add(pchName);
			_dirtyClear.Remove(pchName);
			return true;
		}

		public static bool ClearAchievement(string pchName)
		{
			var i = Info(pchName);
			i.Unlocked = false;
			i.UnlockTime = null;
			_dirtyClear.Add(pchName);
			_dirtyUnlock.Remove(pchName);
			return true;
		}

		public static bool IndicateAchievementProgress(string pchName, uint nCurProgress, uint nMaxProgress)
		{
			var body = "{\"name\":" + WebworksJson.Quote(pchName) + ",\"current\":" + nCurProgress + ",\"max\":" + nMaxProgress + "}";
			ApiClient.Fire(() => ApiClient.PostAsync("/UserStats/AchievementProgress", body), "IndicateAchievementProgress");
			if (nCurProgress >= nMaxProgress) Info(pchName).Unlocked = true;
			CallbackBus<UserAchievementStored_t>.Fire(new UserAchievementStored_t { m_nGameID = AppIdUlong(), m_bGroupAchievement = false, m_rgchAchievementName = pchName, m_nCurProgress = nCurProgress, m_nMaxProgress = nMaxProgress });
			return true;
		}

		public static uint GetNumAchievements()
		{
			return (uint)_catalog.Count;
		}

		public static string GetAchievementDisplayAttribute(string pchName, string pchKey)
		{
			var i = Info(pchName);
			if (pchKey == "name") return i.Name;
			if (pchKey == "desc") return i.Description;
			if (pchKey == "hidden") return i.Hidden ? "1" : "0";
			return "";
		}

		public static bool ResetAllStats(bool bAchievementsToo)
		{
			_stats.Clear(); _dirtyStats.Clear();
			if (bAchievementsToo) { _ach.Clear(); _dirtyUnlock.Clear(); _dirtyClear.Clear(); }
			ApiClient.Fire(() => ApiClient.PostAsync("/UserStats/Reset", "{}"), "ResetAllStats");
			return true;
		}


		static readonly Dictionary<string, double> _globalStats = new Dictionary<string, double>();
		static readonly Dictionary<string, double[]> _globalHistory = new Dictionary<string, double[]>();

		public static SteamAPICall_t RequestGlobalStats(int nHistoryDays)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.GetAsync("/UserStats/GlobalStats?days=" + nHistoryDays));
				if (d != null)
				{
					var stats = WebworksJson.Map(d, "stats");
					if (stats != null) foreach (var kv in stats) if (kv.Value is double) _globalStats[kv.Key] = (double)kv.Value;

					var hist = WebworksJson.Map(d, "history");
					if (hist != null)
						foreach (var kv in hist)
						{
							var arr = kv.Value as List<object>;
							if (arr == null) continue;
							var vals = new double[arr.Count];
							for (int i = 0; i < vals.Length; i++) vals[i] = arr[i] is double ? (double)arr[i] : 0;
							_globalHistory[kv.Key] = vals;
						}
				}
				ResultBus.Complete(call, new GlobalStatsReceived_t { m_nGameID = AppIdUlong(), m_eResult = d != null ? EResult.k_EResultOK : EResult.k_EResultFail });
				return (string)null;
			}, "RequestGlobalStats");
			return call;
		}

		public static bool GetGlobalStat(string pchStatName, out long pData) { double v; pData = _globalStats.TryGetValue(pchStatName, out v) ? (long)v : 0L; return true; }
		public static bool GetGlobalStat(string pchStatName, out double pData) { return _globalStats.TryGetValue(pchStatName, out pData); }

		public static int GetGlobalStatHistory(string pchStatName, long[] pData, uint cubData)
		{
			var h = History(pchStatName, (int)cubData);
			var n = Math.Min(h.Length, pData.Length);
			for (int i = 0; i < n; i++) pData[i] = (long)h[i];
			return n;
		}

		public static int GetGlobalStatHistory(string pchStatName, double[] pData, uint cubData)
		{
			var h = History(pchStatName, (int)cubData);
			var n = Math.Min(h.Length, pData.Length);
			Array.Copy(h, pData, n);
			return n;
		}

		static double[] History(string name, int days)
		{
			double[] h;
			if (!_globalHistory.TryGetValue(name, out h)) h = new double[0];
			if (days > 0 && h.Length > days) { var t = new double[days]; Array.Copy(h, t, days); return t; }
			return h;
		}


		class LeaderboardInfo { public string Name; public ELeaderboardSortMethod Sort; public ELeaderboardDisplayType Display; public int EntryCount; }
		static readonly Dictionary<ulong, LeaderboardInfo> _boards = new Dictionary<ulong, LeaderboardInfo>();
		static readonly Dictionary<string, ulong> _boardHandleByName = new Dictionary<string, ulong>();
		static ulong _nextBoardHandle = 1;

		static readonly Dictionary<ulong, LeaderboardEntry_t[]> _entryPages = new Dictionary<ulong, LeaderboardEntry_t[]>();
		static ulong _nextEntriesHandle = 1;

		public static SteamAPICall_t FindOrCreateLeaderboard(string pchLeaderboardName, ELeaderboardSortMethod eLeaderboardSortMethod, ELeaderboardDisplayType eLeaderboardDisplayType)
		{
			var call = ResultBus.NewCall();
			var body = "{\"name\":" + WebworksJson.Quote(pchLeaderboardName) + ",\"sort\":" + WebworksJson.Quote(SortToString(eLeaderboardSortMethod)) + ",\"display\":" + WebworksJson.Quote(DisplayToString(eLeaderboardDisplayType)) + "}";
			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.PostAsync("/UserStats/Leaderboard/FindOrCreate", body));
				CompleteFind(call, d);
				return (string)null;
			}, "FindOrCreateLeaderboard");
			return call;
		}

		public static SteamAPICall_t FindLeaderboard(string pchLeaderboardName)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.GetAsync("/UserStats/Leaderboard/Find?name=" + Uri.EscapeDataString(pchLeaderboardName)));
				CompleteFind(call, d);
				return (string)null;
			}, "FindLeaderboard");
			return call;
		}

		static void CompleteFind(SteamAPICall_t call, Dictionary<string, object> d)
		{
			bool found = d != null && WebworksJson.Bool(d, "found");
			ulong handle = 0;
			if (found)
			{
				var name = WebworksJson.Str(d, "name");
				if (!_boardHandleByName.TryGetValue(name, out handle))
				{
					handle = _nextBoardHandle++;
					_boardHandleByName[name] = handle;
				}
				_boards[handle] = new LeaderboardInfo
				{
					Name = name,
					Sort = ParseSort(WebworksJson.Str(d, "sort")),
					Display = ParseDisplay(WebworksJson.Str(d, "display")),
					EntryCount = WebworksJson.Int(d, "entryCount"),
				};
			}
			ResultBus.Complete(call, new LeaderboardFindResult_t { m_hSteamLeaderboard = handle, m_bLeaderboardFound = (byte)(found ? 1 : 0) });
		}

		public static string GetLeaderboardName(SteamLeaderboard_t hSteamLeaderboard) { LeaderboardInfo i; return _boards.TryGetValue(hSteamLeaderboard.m_SteamLeaderboard, out i) ? i.Name : ""; }
		public static int GetLeaderboardEntryCount(SteamLeaderboard_t hSteamLeaderboard) { LeaderboardInfo i; return _boards.TryGetValue(hSteamLeaderboard.m_SteamLeaderboard, out i) ? i.EntryCount : 0; }
		public static ELeaderboardSortMethod GetLeaderboardSortMethod(SteamLeaderboard_t hSteamLeaderboard) { LeaderboardInfo i; return _boards.TryGetValue(hSteamLeaderboard.m_SteamLeaderboard, out i) ? i.Sort : ELeaderboardSortMethod.k_ELeaderboardSortMethodNone; }
		public static ELeaderboardDisplayType GetLeaderboardDisplayType(SteamLeaderboard_t hSteamLeaderboard) { LeaderboardInfo i; return _boards.TryGetValue(hSteamLeaderboard.m_SteamLeaderboard, out i) ? i.Display : ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNone; }

		public static SteamAPICall_t UploadLeaderboardScore(SteamLeaderboard_t hSteamLeaderboard, ELeaderboardUploadScoreMethod eLeaderboardUploadScoreMethod, int nScore, int[] pScoreDetails, int cScoreDetailsCount)
		{
			var call = ResultBus.NewCall();
			LeaderboardInfo info;
			if (!_boards.TryGetValue(hSteamLeaderboard.m_SteamLeaderboard, out info))
			{
				ResultBus.Complete(call, new LeaderboardScoreUploaded_t { m_bSuccess = 0 });
				return call;
			}

			var body = new StringBuilder("{\"name\":").Append(WebworksJson.Quote(info.Name))
				.Append(",\"score\":").Append(nScore.ToString(CultureInfo.InvariantCulture))
				.Append(",\"forceUpdate\":").Append(eLeaderboardUploadScoreMethod == ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodForceUpdate ? "true" : "false")
				.Append(",\"details\":[");
			if (pScoreDetails != null)
				for (int i = 0; i < cScoreDetailsCount && i < pScoreDetails.Length; i++)
					body.Append(i > 0 ? "," : "").Append(pScoreDetails[i].ToString(CultureInfo.InvariantCulture));
			body.Append("]}");

			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.PostAsync("/UserStats/Leaderboard/Submit", body.ToString()));
				if (d == null || WebworksJson.Str(d, "error") != null)
				{
					ResultBus.Complete(call, new LeaderboardScoreUploaded_t { m_bSuccess = 0, m_hSteamLeaderboard = hSteamLeaderboard.m_SteamLeaderboard });
					return (string)null;
				}
				ResultBus.Complete(call, new LeaderboardScoreUploaded_t
				{
					m_bSuccess = 1,
					m_hSteamLeaderboard = hSteamLeaderboard.m_SteamLeaderboard,
					m_nScore = WebworksJson.Int(d, "score"),
					m_bScoreChanged = (byte)(WebworksJson.Bool(d, "changed") ? 1 : 0),
					m_nGlobalRankNew = WebworksJson.Int(d, "newGlobalRank"),
					m_nGlobalRankPrevious = WebworksJson.Int(d, "oldGlobalRank"),
				});
				return (string)null;
			}, "UploadLeaderboardScore");
			return call;
		}

		public static SteamAPICall_t DownloadLeaderboardEntries(SteamLeaderboard_t hSteamLeaderboard, ELeaderboardDataRequest eLeaderboardDataRequest, int nRangeStart, int nRangeEnd)
		{
			var call = ResultBus.NewCall();
			LeaderboardInfo info;
			if (!_boards.TryGetValue(hSteamLeaderboard.m_SteamLeaderboard, out info))
			{
				ResultBus.Complete(call, new LeaderboardScoresDownloaded_t { m_hSteamLeaderboard = hSteamLeaderboard.m_SteamLeaderboard });
				return call;
			}

			string query;
			switch (eLeaderboardDataRequest)
			{
				case ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser: query = "&type=around&start=" + nRangeStart + "&end=" + nRangeEnd; break;
				case ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends: query = "&type=friends"; break;
				default: query = "&type=global&offset=" + nRangeStart + "&count=" + (nRangeEnd - nRangeStart + 1); break;
			}

			ApiClient.Fire(async () =>
			{
				var d = WebworksJson.Obj(await ApiClient.GetAsync("/UserStats/Leaderboard/Entries?name=" + Uri.EscapeDataString(info.Name) + query));
				var list = new List<LeaderboardEntry_t>();
				var arr = d != null ? WebworksJson.Arr(d, "entries") : null;
				if (arr != null)
					foreach (var o in arr)
					{
						var e = o as Dictionary<string, object>;
						if (e == null) continue;
						ulong id = WebworksJson.Ulong(e, "steamId");
						SNetState.Players.Set(id, WebworksJson.Str(e, "name"));
						list.Add(new LeaderboardEntry_t { m_steamIDUser = id, m_nGlobalRank = WebworksJson.Int(e, "globalRank"), m_nScore = WebworksJson.Int(e, "score"), m_cDetails = 0 });
					}

				var entriesHandle = _nextEntriesHandle++;
				_entryPages[entriesHandle] = list.ToArray();
				ResultBus.Complete(call, new LeaderboardScoresDownloaded_t { m_hSteamLeaderboard = hSteamLeaderboard.m_SteamLeaderboard, m_hSteamLeaderboardEntries = entriesHandle, m_cEntryCount = list.Count });
				return (string)null;
			}, "DownloadLeaderboardEntries");
			return call;
		}

		public static bool GetDownloadedLeaderboardEntry(SteamLeaderboardEntries_t hSteamLeaderboardEntries, int index, out LeaderboardEntry_t leaderboardEntry, int[] pDetails, int cDetailsMax)
		{
			LeaderboardEntry_t[] page;
			if (!_entryPages.TryGetValue(hSteamLeaderboardEntries.m_SteamLeaderboardEntries, out page) || index < 0 || index >= page.Length)
			{
				leaderboardEntry = default(LeaderboardEntry_t);
				return false;
			}
			leaderboardEntry = page[index];
			return true;
		}

		static string SortToString(ELeaderboardSortMethod s) { switch (s) { case ELeaderboardSortMethod.k_ELeaderboardSortMethodAscending: return "ascending"; case ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending: return "descending"; default: return "none"; } }
		static string DisplayToString(ELeaderboardDisplayType d) { switch (d) { case ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric: return "numeric"; case ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeSeconds: return "timeseconds"; case ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeMilliSeconds: return "timemilliseconds"; default: return "none"; } }
		static ELeaderboardSortMethod ParseSort(string s) { return s == "ascending" ? ELeaderboardSortMethod.k_ELeaderboardSortMethodAscending : s == "descending" ? ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending : ELeaderboardSortMethod.k_ELeaderboardSortMethodNone; }
		static ELeaderboardDisplayType ParseDisplay(string s) { switch (s) { case "numeric": return ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric; case "timeseconds": return ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeSeconds; case "timemilliseconds": return ELeaderboardDisplayType.k_ELeaderboardDisplayTypeTimeMilliSeconds; default: return ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNone; } }
	}
}
