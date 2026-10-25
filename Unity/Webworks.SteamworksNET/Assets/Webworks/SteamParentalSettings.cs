using System.Collections.Generic;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	// Parental controls are a local feature: Webworks has no account-level parental
	// service, so the shim keeps the lock state and the block lists itself.
	public static partial class SteamParentalSettings
	{
		static bool _enabled;
		static bool _locked;
		static readonly HashSet<uint> _blockedApps = new HashSet<uint>();
		static readonly HashSet<uint> _appBlockList = new HashSet<uint>();
		static readonly HashSet<int> _blockedFeatures = new HashSet<int>();
		static readonly HashSet<int> _featureBlockList = new HashSet<int>();

		const string PrefKey = "webworks.parental";

		static SteamParentalSettings()
		{
			Load();
		}

		static void Load()
		{
			try
			{
				var raw = PlayerPrefs.GetString(PrefKey, "");
				if (string.IsNullOrEmpty(raw)) return;
				var obj = WebworksJson.Obj(raw);
				if (obj == null) return;
				_enabled = WebworksJson.Bool(obj, "enabled");
				_locked = WebworksJson.Bool(obj, "locked");
				ReadSet(WebworksJson.Str(obj, "apps"), _blockedApps);
				ReadSet(WebworksJson.Str(obj, "appList"), _appBlockList);
				ReadFeatures(WebworksJson.Str(obj, "features"), _blockedFeatures);
				ReadFeatures(WebworksJson.Str(obj, "featureList"), _featureBlockList);
			}
			catch
			{
			}
		}

		static void ReadSet(string csv, HashSet<uint> target)
		{
			target.Clear();
			if (string.IsNullOrEmpty(csv)) return;
			foreach (var part in csv.Split(','))
			{
				uint value;
				if (uint.TryParse(part, out value)) target.Add(value);
			}
		}

		static void ReadFeatures(string csv, HashSet<int> target)
		{
			target.Clear();
			if (string.IsNullOrEmpty(csv)) return;
			foreach (var part in csv.Split(','))
			{
				int value;
				if (int.TryParse(part, out value)) target.Add(value);
			}
		}

		static string JoinSet(IEnumerable<uint> values)
		{
			return string.Join(",", new List<uint>(values).ConvertAll(v => v.ToString()).ToArray());
		}

		static string JoinFeatures(IEnumerable<int> values)
		{
			return string.Join(",", new List<int>(values).ConvertAll(v => v.ToString()).ToArray());
		}

		static void Save()
		{
			var body = "{\"enabled\":" + (_enabled ? "true" : "false")
				+ ",\"locked\":" + (_locked ? "true" : "false")
				+ ",\"apps\":\"" + JoinSet(_blockedApps) + "\""
				+ ",\"appList\":\"" + JoinSet(_appBlockList) + "\""
				+ ",\"features\":\"" + JoinFeatures(_blockedFeatures) + "\""
				+ ",\"featureList\":\"" + JoinFeatures(_featureBlockList) + "\"}";
			try { PlayerPrefs.SetString(PrefKey, body); PlayerPrefs.Save(); } catch { }
		}

		// Settings the host application (or a parental UI) can drive.
		public static void Configure(bool enabled, bool locked)
		{
			_enabled = enabled;
			_locked = locked;
			Save();
		}

		public static void SetAppBlocked(AppId_t appId, bool blocked)
		{
			if (blocked) _blockedApps.Add(appId.m_AppId); else _blockedApps.Remove(appId.m_AppId);
			Save();
		}

		public static void SetFeatureBlocked(EParentalFeature feature, bool blocked)
		{
			if (blocked) _blockedFeatures.Add((int)feature); else _blockedFeatures.Remove((int)feature);
			Save();
		}

		public static bool BIsParentalLockEnabled() { return _enabled; }
		public static bool BIsParentalLockLocked() { return _enabled && _locked; }

		public static bool BIsAppBlocked(AppId_t nAppID)
		{
			return _blockedApps.Contains(nAppID.m_AppId) || _appBlockList.Contains(nAppID.m_AppId);
		}

		public static bool BIsAppInBlockList(AppId_t nAppID)
		{
			return _appBlockList.Contains(nAppID.m_AppId);
		}

		public static bool BIsFeatureBlocked(EParentalFeature eFeature)
		{
			return _blockedFeatures.Contains((int)eFeature) || _featureBlockList.Contains((int)eFeature);
		}

		public static bool BIsFeatureInBlockList(EParentalFeature eFeature)
		{
			return _featureBlockList.Contains((int)eFeature);
		}
	}
}
