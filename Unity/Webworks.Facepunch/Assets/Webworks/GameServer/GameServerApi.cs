#if UNITY_WEBGL && !UNITY_EDITOR
#error Webworks/GameServer is for dedicated-server builds only -- it must not be part of a WebGL player build. See Webworks/GameServer/README.md.
#endif
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Webworks.GameServer
{
	internal static class GameServerApi
	{
		static readonly HttpClient _http = new HttpClient();
		static bool _configured;

		public static string SessionToken;

		static void EnsureConfigured()
		{
			if (_configured) return;
			Webworks.Internal.WebworksConfig.Load();
			_http.Timeout = TimeSpan.FromMilliseconds(Webworks.Internal.WebworksConfig.RequestTimeoutMs);
			_configured = true;
		}

		static string FullUrl(string relativePath)
		{
			return Webworks.Internal.WebworksConfig.ServerUrl + "/api/" + Webworks.Internal.WebworksConfig.EnginePath + relativePath;
		}

		public static Task<string> GetAsync(string path) { return Send("GET", path, null); }
		public static Task<string> PostAsync(string path, string jsonBody) { return Send("POST", path, jsonBody ?? "{}"); }

		public static async void Fire(Func<Task<string>> call, string what)
		{
			try { await call(); }
			catch (Exception ex) { UnityEngine.Debug.LogWarning("[Webworks] GameServer " + what + " failed: " + ex.Message); }
		}

		static async Task<string> Send(string method, string path, string body)
		{
			EnsureConfigured();
			var req = new HttpRequestMessage(new HttpMethod(method), FullUrl(path));
			if (body != null) req.Content = new StringContent(body, Encoding.UTF8, "application/json");
			if (!string.IsNullOrEmpty(SessionToken))
			{
				req.Headers.Add("x-webworks-session", SessionToken);
				req.Headers.Add("x-webeam-session", SessionToken); // pre-rename servers only know this name
			}
			var resp = await _http.SendAsync(req);
			return await resp.Content.ReadAsStringAsync();
		}
	}
}
