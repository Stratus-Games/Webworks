using System;
using System.Collections.Generic;
using System.IO;

namespace Webworks.SNetInternal
{
	public static class WebworksConfig
	{
		public static string ServerUrl { get; private set; } = "http://localhost:3000";
		public static string RealtimeUrl { get; private set; } = "ws://localhost:3000/realtime";
		public static string IdentityMode { get; private set; } = "guest";
		public static string AppId { get; private set; } = "000000";
		public static int RequestTimeoutMs { get; private set; } = 8000;

		public static string AccountsWorkerUrl { get; private set; } = "";

		public static string AccountsApiBase { get { return ServerUrl + "/api/" + EnginePath + "/Accounts"; } }

		public static string TunnelUrl { get { return ToWs(ServerUrl) + "/tunnel"; } }

		public static bool PersistentId { get; private set; }

		public static string DeviceKey { get; private set; }

		static bool _realtimeExplicit;

		/// <summary>Random per-device key stored in PlayerPrefs (only read when PersistentId is on).</summary>
		public static void PrepareDeviceKey()
		{
			Load();
			if (!PersistentId || DeviceKey != null) return;
			const string key = "webworks_device_key";
			var v = UnityEngine.PlayerPrefs.GetString(key, "");
			if (string.IsNullOrEmpty(v))
			{
				v = Guid.NewGuid().ToString("N");
				UnityEngine.PlayerPrefs.SetString(key, v);
				UnityEngine.PlayerPrefs.Save();
			}
			DeviceKey = v;
		}

		public static string EnginePath { get; private set; } = "Unity/SteamworksNET";

		/// <summary>
		/// Every engine prefix this project has ever mounted routes under, most relevant first.
		/// ApiClient walks this list when a request comes back as an unsupported route, which is
		/// what makes this shim work against an older server without editing shim.config.txt.
		/// The empty string is the wrapper-less /api mount.
		/// </summary>
		static readonly string[] KnownEnginePaths =
		{
			"Unity/SteamworksNET",
			"Unity/Steamworks.net",
			"Unity/SteamworksNet",
			"Unity/Steamworks",
			"Unity/Facepunch",
			"Unity/Webeam",
			"Unity/Weeam",
			"Unity/Webworks",
			"Unity",
			"Unreal/Steam",
			"Unreal/Steamworks",
			"Unreal",
			"Godot/GodotSteam",
			"Godot",
			"",
		};

		public static string[] EngineCandidates()
		{
			var list = new List<string>();
			AddEngine(list, EnginePath);
			foreach (var engine in KnownEnginePaths) AddEngine(list, engine);
			return list.ToArray();
		}

		static void AddEngine(List<string> list, string engine)
		{
			var normalized = NormalizeEngine(engine);
			if (!list.Contains(normalized)) list.Add(normalized);
		}

		static bool _loaded;

		public static string ResolveAppId(uint fromInit)
		{
			Load();
			if (!string.IsNullOrEmpty(AppId) && AppId != "000000" && AppId != "0") return AppId;
			if (fromInit != 0) return fromInit.ToString();
#if !(UNITY_WEBGL && !UNITY_EDITOR)
			try
			{
				foreach (var dir in new[] { AppDomain.CurrentDomain.BaseDirectory, Directory.GetCurrentDirectory() })
				{
					var file = Path.Combine(dir, "steam_appid.txt");
					uint id;
					if (File.Exists(file) && uint.TryParse(File.ReadAllText(file).Trim(), out id) && id != 0) return id.ToString();
				}
			}
			catch (Exception) {  }
#endif
			return "0";
		}

		/// <summary>
		/// Reads a key in the current spelling, then the Webworks-era alias, then the
		/// pre-rename Webeam/Weeam spelling (webeam_server_url, webeam_app_id, ...).
		/// </summary>
		static string Value(Dictionary<string, string> values, string key)
		{
			string v;
			if (values.TryGetValue(key, out v)) return v;
			if (values.TryGetValue("webworks_" + key, out v)) return v;
			if (values.TryGetValue("webeam_" + key, out v)) return v;
			return null;
		}

		public static void Load(string explicitPath = null)
		{
			if (_loaded) return;
			_loaded = true;

			string[] lines = null;
#if UNITY_WEBGL && !UNITY_EDITOR
			string url = UnityEngine.Application.streamingAssetsPath + "/shim.config.txt";
			string text = WebworksNative.Webworks_HttpSync("GET", url, null, null);
			if (!string.IsNullOrEmpty(text)) lines = text.Split('\n');
			else UnityEngine.Debug.LogWarning("[Webworks] shim.config.txt not found at '" + url + "', using defaults (server " + ServerUrl + ").");
#else
			string path = explicitPath;
			if (string.IsNullOrEmpty(path))
			{
				string exeDir = AppDomain.CurrentDomain.BaseDirectory;
				string candidate = Path.Combine(exeDir, "shim.config.txt");
				path = File.Exists(candidate)
					? candidate
					: Path.Combine(UnityEngine.Application.streamingAssetsPath, "shim.config.txt");
			}

			if (File.Exists(path)) lines = File.ReadAllLines(path);
			else UnityEngine.Debug.LogWarning("[Webworks] shim.config.txt not found at '" + path + "', using defaults.");
#endif
			if (lines == null) { Normalize(false); return; }

			var values = new Dictionary<string, string>();
			foreach (var rawLine in lines)
			{
				var line = rawLine.Trim();
				if (line.Length == 0 || line.StartsWith("#")) continue;

				int eq = line.IndexOf('=');
				if (eq < 0) continue;

				values[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
			}

			string v;
			int tms;
			v = Value(values, "server_url");
			if (v != null) ServerUrl = v;
			v = Value(values, "realtime_url");
			if (v != null && v.Length > 0) { RealtimeUrl = v; _realtimeExplicit = true; }
			v = Value(values, "persistent_id");
			if (v != null) PersistentId = v == "true" || v == "1";
			v = Value(values, "identity_mode");
			if (v != null) IdentityMode = v;
			v = Value(values, "app_id");
			if (v != null) AppId = v;
			v = Value(values, "accounts_worker_url");
			if (v != null) AccountsWorkerUrl = v.TrimEnd('/');
			v = Value(values, "engine_path");
			bool engineExplicit = v != null;
			if (engineExplicit) EnginePath = NormalizeEngine(v);
			v = Value(values, "request_timeout_ms");
			if (v != null && int.TryParse(v, out tms)) RequestTimeoutMs = tms;
			Normalize(engineExplicit);
		}

		/// <summary>
		/// Older configs copied a fully-qualified API base into server_url
		/// (e.g. http://host:3000/api/Unity/Facepunch). Split it so the shim never doubles
		/// the /api prefix, and adopt the engine that was embedded there unless engine_path
		/// says otherwise.
		/// </summary>
		static void Normalize(bool engineExplicit)
		{
			ServerUrl = (ServerUrl ?? "").Trim();
			EnginePath = NormalizeEngine(EnginePath);
			if (ServerUrl.Length == 0) ServerUrl = "http://localhost:3000";

			int api = ServerUrl.IndexOf("/api/", StringComparison.Ordinal);
			if (api > 0)
			{
				var embedded = ServerUrl.Substring(api + "/api/".Length).Trim('/');
				ServerUrl = ServerUrl.Substring(0, api);
				if (!engineExplicit && embedded.Length > 0) EnginePath = NormalizeEngine(embedded);
			}
			else if (ServerUrl.EndsWith("/api", StringComparison.Ordinal))
			{
				ServerUrl = ServerUrl.Substring(0, ServerUrl.Length - "/api".Length);
			}

			ServerUrl = ServerUrl.TrimEnd('/');
			if (_realtimeExplicit && RealtimeUrl == "ws://localhost:3000/realtime" && !ServerUrl.StartsWith("http://localhost:3000")) _realtimeExplicit = false;
			if (!_realtimeExplicit) RealtimeUrl = ToWs(ServerUrl) + "/realtime";
#if UNITY_WEBGL && !UNITY_EDITOR
			if (WebworksNative.Webworks_PageIsSecure() == 1)
			{
				ServerUrl = Upgrade(ServerUrl, "http://", "https://");
				RealtimeUrl = Upgrade(RealtimeUrl, "ws://", "wss://");
			}
#endif
		}

		static string NormalizeEngine(string engine)
		{
			var value = (engine ?? "").Trim().Trim('/');
			if (value.StartsWith("api/", StringComparison.Ordinal)) value = value.Substring(4);
			return value.Trim('/');
		}

		static string ToWs(string http)
		{
			if (http.StartsWith("https://")) return "wss://" + http.Substring(8);
			if (http.StartsWith("http://")) return "ws://" + http.Substring(7);
			return http;
		}

		static string Upgrade(string url, string insecureScheme, string secureScheme)
		{
			if (!url.StartsWith(insecureScheme)) return url;
			string rest = url.Substring(insecureScheme.Length);
			if (rest.StartsWith("localhost") || rest.StartsWith("127.0.0.1") || rest.StartsWith("[::1]")) return url;
			return secureScheme + rest;
		}
	}
}
