#pragma warning disable CS0067
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Steamworks.Data;
using Webworks.Internal;

namespace Steamworks
{
	public static partial class SteamUserStats
	{
		class AchInfo { public string Name, Description; public bool Unlocked; public DateTime? UnlockTime; }

		static readonly Dictionary<string, double> _stats = new Dictionary<string, double>();
		static readonly Dictionary<string, AchInfo> _ach = new Dictionary<string, AchInfo>();
		static readonly HashSet<string> _dirtyStats = new HashSet<string>();
		static readonly HashSet<string> _dirtyUnlock = new HashSet<string>();
		static readonly HashSet<string> _dirtyClear = new HashSet<string>();

		public static event Action<SteamId, Result> OnUserStatsReceived;
		public static event Action<Result> OnUserStatsStored;
		public static event Action<Achievement, int, int> OnAchievementProgress;
		public static event Action<SteamId> OnUserStatsUnloaded;
		public static event Action<string, bool, int, int> OnAchievementIconFetched;

		public static IEnumerable<Achievement> Achievements
		{
			get
			{
				var list = new List<Achievement>();
				foreach (var k in _ach.Keys) list.Add(new Achievement(k));
				return list;
			}
		}

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
					if (!_dirtyUnlock.Contains(id) && !_dirtyClear.Contains(id)) info.Unlocked = WebworksJson.Bool(a, "unlocked");
					var ms = WebworksJson.Num(a, "unlockTime");
					info.UnlockTime = ms > 0 ? new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ms) : (DateTime?)null;
				}

			_statsReceived = true;
			Callbacks.Raise(OnUserStatsReceived, SteamClient.SteamId, Result.OK);
			return null;
		}

		static AchInfo Info(string id)
		{
			AchInfo i;
			if (!_ach.TryGetValue(id, out i)) { i = new AchInfo { Name = id, Description = "" }; _ach[id] = i; }
			return i;
		}

		static readonly Dictionary<string, double> _globalStats = new Dictionary<string, double>();
		static readonly Dictionary<string, double[]> _globalHistory = new Dictionary<string, double[]>();

		public static async Task<Result> RequestGlobalStatsAsync(int days)
		{
			var d = WebworksJson.Obj(await ApiClient.GetAsync("/UserStats/GlobalStats?days=" + days));
			if (d == null) return Result.Fail;

			var stats = WebworksJson.Map(d, "stats");
			if (stats != null)
				foreach (var kv in stats)
					if (kv.Value is double) _globalStats[kv.Key] = (double)kv.Value;

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
			return Result.OK;
		}

		public static long GetGlobalStatInt(string name) { double v; return _globalStats.TryGetValue(name, out v) ? (long)v : 0L; }
		public static double GetGlobalStatFloat(string name) { double v; return _globalStats.TryGetValue(name, out v) ? v : 0.0; }

		public static long[] GetGlobalStatIntHistory(string name, int days = 60)
		{
			var h = History(name, days);
			var r = new long[h.Length];
			for (int i = 0; i < h.Length; i++) r[i] = (long)h[i];
			return r;
		}

		public static double[] GetGlobalStatFloatHistory(string name, int days = 60) { return History(name, days); }

		static double[] History(string name, int days)
		{
			double[] h;
			if (!_globalHistory.TryGetValue(name, out h)) h = new double[0];
			if (days > 0 && h.Length > days) { var t = new double[days]; Array.Copy(h, t, days); return t; }
			return h;
		}


		public static int GetStatInt(string name) { double v; return _stats.TryGetValue(name, out v) ? (int)v : 0; }
		public static float GetStatFloat(string name) { double v; return _stats.TryGetValue(name, out v) ? (float)v : 0f; }

		public static bool SetStat(string name, int value) { return Set(name, value); }
		public static bool SetStat(string name, float value) { return Set(name, value); }
		public static bool AddStat(string name, int amount = 1) { return Set(name, GetStatInt(name) + amount); }
		public static bool AddStat(string name, float amount) { return Set(name, GetStatFloat(name) + amount); }

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
				foreach (var id in newlyUnlocked)
					Callbacks.Raise(OnAchievementProgress, new Achievement(id), 0, 0);
				Callbacks.Raise(OnUserStatsStored, Result.OK);
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

		public static bool IndicateAchievementProgress(string achievementName, int current, int max)
		{
			var body = "{\"name\":" + WebworksJson.Quote(achievementName) + ",\"current\":" + current + ",\"max\":" + max + "}";
			ApiClient.Fire(() => ApiClient.PostAsync("/UserStats/AchievementProgress", body), "IndicateAchievementProgress");
			if (current >= max) Info(achievementName).Unlocked = true;
			Callbacks.Raise(OnAchievementProgress, new Achievement(achievementName), current, max);
			return true;
		}

		public static bool ResetAll(bool includeAchievements)
		{
			_stats.Clear(); _dirtyStats.Clear();
			if (includeAchievements) { _ach.Clear(); _dirtyUnlock.Clear(); _dirtyClear.Clear(); }
			ApiClient.Fire(() => ApiClient.PostAsync("/UserStats/Reset", "{}"), "ResetAll");
			return true;
		}

		public static Task<string> GetLeaderboardRawAsync(string name)
		{
			return ApiClient.GetAsync("/UserStats/Leaderboard?name=" + Uri.EscapeDataString(name));
		}


		internal static bool IsUnlocked(string id) { AchInfo i; return _ach.TryGetValue(id, out i) && i.Unlocked; }
		internal static string AchName(string id) { AchInfo i; return _ach.TryGetValue(id, out i) ? i.Name : id; }
		internal static string AchDescription(string id) { AchInfo i; return _ach.TryGetValue(id, out i) ? i.Description : ""; }
		internal static DateTime? AchUnlockTime(string id) { AchInfo i; return _ach.TryGetValue(id, out i) ? i.UnlockTime : null; }

		internal static void SetAchievement(string id, bool unlocked)
		{
			var i = Info(id);
			i.Unlocked = unlocked;
			if (unlocked) { i.UnlockTime = DateTime.UtcNow; _dirtyUnlock.Add(id); _dirtyClear.Remove(id); }
			else { i.UnlockTime = null; _dirtyClear.Add(id); _dirtyUnlock.Remove(id); }
		}
	}
}

namespace Steamworks.Data
{
	public struct Achievement
	{
		readonly string _id;

		public Achievement(string name) { _id = name; }

		public string Identifier { get { return _id; } }
		public string Name { get { return SteamUserStats.AchName(_id); } }
		public string Description { get { return SteamUserStats.AchDescription(_id); } }
		public bool State { get { return SteamUserStats.IsUnlocked(_id); } }
		public DateTime? UnlockTime { get { return SteamUserStats.AchUnlockTime(_id); } }
		public float GlobalUnlocked { get { return 0f; } }

		public bool Trigger(bool apply = true)
		{
			SteamUserStats.SetAchievement(_id, true);
			if (apply) SteamUserStats.StoreStats();
			return true;
		}

		public bool Clear()
		{
			SteamUserStats.SetAchievement(_id, false);
			return true;
		}

		public Image? GetIcon() { return Image.Placeholder(_id); }
		public Task<Image?> GetIconAsync(int timeout = 10000) { return Task.FromResult<Image?>(GetIcon()); }
	}

	public struct PixelColor { public byte r, g, b, a; }

	public struct Image
	{
		public uint Width;
		public uint Height;
		public byte[] Data;

		public PixelColor GetPixel(int x, int y)
		{
			var i = (int)((y * Width + x) * 4);
			return new PixelColor { r = Data[i], g = Data[i + 1], b = Data[i + 2], a = Data[i + 3] };
		}

		internal static Image Placeholder(string seed)
		{
			const int n = 32;
			int h = seed == null ? 0 : seed.GetHashCode();
			byte r = (byte)(64 + ((h >> 16) & 0x7F)), g = (byte)(64 + ((h >> 8) & 0x7F)), b = (byte)(64 + (h & 0x7F));
			var data = new byte[n * n * 4];
			for (int y = 0; y < n; y++)
				for (int x = 0; x < n; x++)
				{
					bool border = x < 2 || y < 2 || x >= n - 2 || y >= n - 2;
					int i = (y * n + x) * 4;
					data[i] = border ? (byte)255 : r; data[i + 1] = border ? (byte)255 : g; data[i + 2] = border ? (byte)255 : b; data[i + 3] = 255;
				}
			return new Image { Width = n, Height = n, Data = data };
		}
	}

	public struct AuthTicket : IDisposable
	{
		public byte[] Data;
		public uint Handle;
		public void Cancel() { }
		public void Dispose() { Cancel(); }
	}

	public struct P2Packet
	{
		public SteamId SteamId;
		public byte[] Data;
	}
}
