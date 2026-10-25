using System;
using System.Threading.Tasks;
using Steamworks.Data;
using Webworks.Internal;

namespace Steamworks
{
	public static partial class SteamUserStats
	{
		static bool _statsReceived;

		public static bool StatsRecieved { get { return _statsReceived; } }

		public static async Task<int> PlayerCountAsync()
		{
			var d = WebworksJson.Obj(await ApiClient.GetAsync("/UserStats/PlayerCount"));
			return d == null ? 0 : WebworksJson.Int(d, "count");
		}

		internal static async Task<long[]> GlobalIntDaysAsync(string name, int days)
		{
			await RequestGlobalStatsAsync(days);
			return GetGlobalStatIntHistory(name, days);
		}

		internal static async Task<double[]> GlobalFloatDaysAsync(string name, int days)
		{
			await RequestGlobalStatsAsync(days);
			return GetGlobalStatFloatHistory(name, days);
		}
	}
}

namespace Steamworks.Data
{
	public struct Stat
	{
		readonly string _name;
		readonly ulong _user;

		public Stat(string name) { _name = name; _user = 0; }
		public Stat(string name, SteamId user) { _name = name; _user = user.Value; }

		public string Name { get { return _name; } }
		public SteamId UserId { get { return _user; } }

		bool Mine { get { return _user == 0 || _user == SteamClient.SteamId.Value; } }

		public long GetInt() { return Mine ? SteamUserStats.GetStatInt(_name) : 0; }
		public double GetFloat() { return Mine ? SteamUserStats.GetStatFloat(_name) : 0.0; }
		public long GetGlobalInt() { return SteamUserStats.GetGlobalStatInt(_name); }
		public double GetGlobalFloat() { return SteamUserStats.GetGlobalStatFloat(_name); }
		public long[] GetGlobalIntDays(int days) { return SteamUserStats.GetGlobalStatIntHistory(_name, days); }
		public double[] GetGlobalFloatDays(int days) { return SteamUserStats.GetGlobalStatFloatHistory(_name, days); }
		public Task<long[]> GetGlobalIntDaysAsync(int days) { return SteamUserStats.GlobalIntDaysAsync(_name, days); }

		public bool Set(int value) { return SteamUserStats.SetStat(_name, value); }
		public bool Set(float value) { return SteamUserStats.SetStat(_name, value); }
		public bool Add(int amount = 1) { return SteamUserStats.AddStat(_name, amount); }
		public bool Add(float amount) { return SteamUserStats.AddStat(_name, amount); }
		public bool Store() { return SteamUserStats.StoreStats(); }
		public bool UpdateAverageRate(float count, float sessionlength) { return false; }
	}
}
