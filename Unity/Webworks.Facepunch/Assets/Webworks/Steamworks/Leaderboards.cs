using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Webworks.Internal;

namespace Steamworks.Data
{
	public enum LeaderboardSort { None = 0, Ascending = 1, Descending = 2 }
	public enum LeaderboardDisplay { None = 0, Numeric = 1, TimeSeconds = 2, TimeMilliSeconds = 3 }

	public struct LeaderboardEntry
	{
		public Friend User;
		public int GlobalRank;
		public int Score;
		public int[] Details;
	}

	public struct LeaderboardUpdate
	{
		public int Score;
		public bool Changed;
		public int NewGlobalRank;
		public int OldGlobalRank;
		public int RankChange { get { return NewGlobalRank - OldGlobalRank; } }
	}

	public struct Leaderboard
	{
		internal string _name;
		internal LeaderboardSort _sort;
		internal LeaderboardDisplay _display;
		internal int _entryCount;

		public string Name { get { return _name; } }
		public LeaderboardSort Sort { get { return _sort; } }
		public LeaderboardDisplay Display { get { return _display; } }
		public int EntryCount { get { return _entryCount; } }

		public Task<LeaderboardUpdate?> SubmitScoreAsync(int score, int[] details = null)
		{
			return SteamUserStats.SubmitLeaderboardScoreAsync(_name, score, details, false);
		}

		public Task<LeaderboardUpdate?> ReplaceScore(int score, int[] details = null)
		{
			return SteamUserStats.SubmitLeaderboardScoreAsync(_name, score, details, true);
		}

		public Task<LeaderboardEntry[]> GetScoresAsync(int count, int offset = 1)
		{
			return SteamUserStats.GetLeaderboardEntriesAsync(_name, "&type=global&offset=" + offset + "&count=" + count);
		}

		public Task<LeaderboardEntry[]> GetScoresAroundUserAsync(int start = -10, int end = 10)
		{
			return SteamUserStats.GetLeaderboardEntriesAsync(_name, "&type=around&start=" + start + "&end=" + end);
		}

		public Task<LeaderboardEntry[]> GetScoresFromFriendsAsync()
		{
			return SteamUserStats.GetLeaderboardEntriesAsync(_name, "&type=friends");
		}
	}
}

namespace Steamworks
{
	using Steamworks.Data;

	public static partial class SteamUserStats
	{
		public static async Task<Leaderboard?> FindOrCreateLeaderboardAsync(string name, LeaderboardSort sort, LeaderboardDisplay display)
		{
			var body = "{\"name\":" + WebworksJson.Quote(name) + ",\"sort\":" + WebworksJson.Quote(SortToString(sort)) + ",\"display\":" + WebworksJson.Quote(DisplayToString(display)) + "}";
			return ToLeaderboard(WebworksJson.Obj(await ApiClient.PostAsync("/UserStats/Leaderboard/FindOrCreate", body)));
		}

		public static async Task<Leaderboard?> FindLeaderboardAsync(string name)
		{
			return ToLeaderboard(WebworksJson.Obj(await ApiClient.GetAsync("/UserStats/Leaderboard/Find?name=" + Uri.EscapeDataString(name))));
		}

		internal static async Task<LeaderboardUpdate?> SubmitLeaderboardScoreAsync(string name, int score, int[] details, bool forceUpdate)
		{
			var body = new StringBuilder("{\"name\":").Append(WebworksJson.Quote(name))
				.Append(",\"score\":").Append(score.ToString(CultureInfo.InvariantCulture))
				.Append(",\"forceUpdate\":").Append(forceUpdate ? "true" : "false")
				.Append(",\"details\":[");
			if (details != null)
				for (int i = 0; i < details.Length; i++)
					body.Append(i > 0 ? "," : "").Append(details[i].ToString(CultureInfo.InvariantCulture));
			body.Append("]}");

			var d = WebworksJson.Obj(await ApiClient.PostAsync("/UserStats/Leaderboard/Submit", body.ToString()));
			if (d == null || WebworksJson.Str(d, "error") != null) return null;

			return new LeaderboardUpdate
			{
				Score = WebworksJson.Int(d, "score"),
				Changed = WebworksJson.Bool(d, "changed"),
				NewGlobalRank = WebworksJson.Int(d, "newGlobalRank"),
				OldGlobalRank = WebworksJson.Int(d, "oldGlobalRank"),
			};
		}

		internal static async Task<LeaderboardEntry[]> GetLeaderboardEntriesAsync(string name, string query)
		{
			var d = WebworksJson.Obj(await ApiClient.GetAsync("/UserStats/Leaderboard/Entries?name=" + Uri.EscapeDataString(name) + query));
			var list = new List<LeaderboardEntry>();
			var arr = d != null ? WebworksJson.Arr(d, "entries") : null;
			if (arr == null) return list.ToArray();

			foreach (var o in arr)
			{
				var e = o as Dictionary<string, object>;
				if (e == null) continue;

				ulong id = WebworksJson.Ulong(e, "steamId");
				PlayerDirectory.Set(id, WebworksJson.Str(e, "name"));
				var details = new List<int>();
				var raw = WebworksJson.Arr(e, "details");
				if (raw != null) foreach (var x in raw) if (x is double) details.Add((int)(double)x);

				list.Add(new LeaderboardEntry
				{
					User = new Friend(id),
					GlobalRank = WebworksJson.Int(e, "globalRank"),
					Score = WebworksJson.Int(e, "score"),
					Details = details.ToArray(),
				});
			}
			return list.ToArray();
		}

		static Leaderboard? ToLeaderboard(Dictionary<string, object> d)
		{
			if (d == null || !WebworksJson.Bool(d, "found")) return null;
			return new Leaderboard
			{
				_name = WebworksJson.Str(d, "name"),
				_sort = ParseSort(WebworksJson.Str(d, "sort")),
				_display = ParseDisplay(WebworksJson.Str(d, "display")),
				_entryCount = WebworksJson.Int(d, "entryCount"),
			};
		}

		static string SortToString(LeaderboardSort s)
		{
			switch (s) { case LeaderboardSort.Ascending: return "ascending"; case LeaderboardSort.Descending: return "descending"; default: return "none"; }
		}

		static string DisplayToString(LeaderboardDisplay d)
		{
			switch (d) { case LeaderboardDisplay.Numeric: return "numeric"; case LeaderboardDisplay.TimeSeconds: return "timeseconds"; case LeaderboardDisplay.TimeMilliSeconds: return "timemilliseconds"; default: return "none"; }
		}

		static LeaderboardSort ParseSort(string s)
		{
			return s == "ascending" ? LeaderboardSort.Ascending : s == "descending" ? LeaderboardSort.Descending : LeaderboardSort.None;
		}

		static LeaderboardDisplay ParseDisplay(string s)
		{
			switch (s) { case "numeric": return LeaderboardDisplay.Numeric; case "timeseconds": return LeaderboardDisplay.TimeSeconds; case "timemilliseconds": return LeaderboardDisplay.TimeMilliSeconds; default: return LeaderboardDisplay.None; }
		}
	}
}
