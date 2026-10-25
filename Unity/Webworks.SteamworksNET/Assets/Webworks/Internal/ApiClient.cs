using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
#else
using System.Net.Http;
#endif

namespace Webworks.SNetInternal
{
	/// <summary>
	/// HTTP wrapper for the /api/Unity/... routes. Every Facepunch/Steamworks.NET-shaped static
	/// class (SteamMatchmaking, SteamUser, ...) calls through here instead of the real Steamworks
	/// native bindings.
	///
	/// Two compatibility behaviours live here so no call site or game has to care:
	/// - the session token is sent as both x-webworks-session (current) and x-webeam-session
	///   (the pre-rename Weeam/Webeam builds' header name), so old servers authenticate us;
	/// - if a route comes back as "no such route" the request is retried under the other engine
	///   prefixes older servers mounted (Unity/Facepunch, Unity/Steamworks.net, ...), so this
	///   shim keeps working against an older deployment without config changes. A route that
	///   no mount has is only skipped for a few minutes, so upgrading the server (or one that
	///   gains inventory/parties/media later) starts working without restarting the game.
	/// </summary>
	public static class ApiClient
	{
		struct Reply
		{
			public string Body;
			public int Status;
		}

		/// <summary>Engine prefix that last answered successfully; tried first on the next call.</summary>
		static string _workingEngine;

		/// <summary>Routes no mounted engine has, and when to try them again. Negative results are
		/// cached so a game polling an endpoint the server simply does not have costs one walk, not
		/// one request per historical prefix -- but only for a while, so a server that is upgraded
		/// mid-session is picked up instead of staying invisible until the game restarts.</summary>
		static readonly Dictionary<string, DateTime> _unsupportedRoutes = new Dictionary<string, DateTime>();

		static readonly TimeSpan UnsupportedTtl = TimeSpan.FromMinutes(5);

		static bool UnsupportedCached(string key)
		{
			DateTime until;
			if (!_unsupportedRoutes.TryGetValue(key, out until)) return false;
			if (DateTime.UtcNow < until) return true;
			_unsupportedRoutes.Remove(key);
			return false;
		}

		static void RememberUnsupported(string key)
		{
			// A game that polls many missing endpoints must not grow this without bound.
			if (_unsupportedRoutes.Count >= 512) _unsupportedRoutes.Clear();
			_unsupportedRoutes[key] = DateTime.UtcNow.Add(UnsupportedTtl);
		}

		/// <summary>Background write for the sync-looking Steamworks calls (SetData, SetStat, ...).
		/// Failures are logged instead of vanishing inside an un-awaited Task.</summary>
		public static async void Fire(Func<Task<string>> call, string what)
		{
			try { await call(); }
			catch (Exception ex) { UnityEngine.Debug.LogWarning("[Webworks] " + what + " failed: " + ex.Message); }
		}

		public static Task<string> GetAsync(string path) { return SendAsync("GET", path, null); }
		public static Task<string> PostAsync(string path, string jsonBody) { return SendAsync("POST", path, jsonBody ?? "{}"); }
		public static Task<string> DeleteAsync(string path, string jsonBody = null) { return SendAsync("DELETE", path, jsonBody); }

		/// <summary>
		/// Callers pass paths relative to the engine mount, e.g. "/Matchmaking/CreateLobby" -- not the
		/// full "/api/Unity/Facepunch/Matchmaking/CreateLobby". The engine/wrapper prefix is centralized
		/// here (driven by WebworksConfig.EnginePath) instead of being hardcoded in every call site.
		/// </summary>
		internal static string FullUrl(string relativePath)
		{
			return UrlFor(WebworksConfig.EnginePath, relativePath);
		}

		static string UrlFor(string engine, string relativePath)
		{
			if (string.IsNullOrEmpty(engine)) return WebworksConfig.ServerUrl + "/api" + relativePath;
			return WebworksConfig.ServerUrl + "/api/" + engine + relativePath;
		}

		// A route that does not exist comes back as Express' HTML 404/405 page; a route that exists
		// but did not find the resource sends our own JSON {error}. Only the former means "this
		// engine prefix isn't mounted here".
		static bool Unsupported(Reply reply)
		{
			if (reply.Status != 404 && reply.Status != 405) return false;
			var body = (reply.Body ?? string.Empty).TrimStart();
			return body.Length == 0 || body[0] != '{';
		}

		static async Task<string> SendAsync(string method, string path, string body)
		{
			var key = method + " " + path;
			if (UnsupportedCached(key) && !string.IsNullOrEmpty(_workingEngine))
			{
				// The cheap path still verifies the mount we settled on. If the deployment
				// changed underneath us (a different build, a proxy in front), fall through and
				// walk the prefixes again instead of returning a 404 forever.
				var cached = await SendOnce(method, UrlFor(_workingEngine, path), body);
				if (!Unsupported(cached)) return cached.Body;
				_unsupportedRoutes.Remove(key);
			}

			Reply last = default(Reply);
			foreach (var engine in Candidates())
			{
				last = await SendOnce(method, UrlFor(engine, path), body);
				if (Unsupported(last)) continue;
				_workingEngine = engine;
				return last.Body;
			}

			RememberUnsupported(key);
			return last.Body;
		}

		static string[] Candidates()
		{
			var ordered = new List<string>();
			if (!string.IsNullOrEmpty(_workingEngine)) ordered.Add(_workingEngine);
			foreach (var engine in WebworksConfig.EngineCandidates())
				if (!ordered.Contains(engine)) ordered.Add(engine);
			return ordered.ToArray();
		}

#if UNITY_WEBGL && !UNITY_EDITOR
		static Task<Reply> SendOnce(string method, string url, string body)
		{
			WebworksConfig.Load();
			var tcs = new TaskCompletionSource<Reply>();
			MainThreadDispatcher.Run(WebRequest(method, url, body, tcs));
			return tcs.Task;
		}

		static IEnumerator WebRequest(string method, string url, string body, TaskCompletionSource<Reply> tcs)
		{
			var req = new UnityWebRequest(url, method);
			req.downloadHandler = new DownloadHandlerBuffer();
			if (body != null)
			{
				req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
				req.SetRequestHeader("Content-Type", "application/json");
			}
			if (!string.IsNullOrEmpty(SessionState.SessionToken))
			{
				req.SetRequestHeader("x-webworks-session", SessionState.SessionToken);
				req.SetRequestHeader("x-webeam-session", SessionState.SessionToken);
			}
			req.timeout = Mathf.Max(1, WebworksConfig.RequestTimeoutMs / 1000);

#if UNITY_2017_2_OR_NEWER
			yield return req.SendWebRequest();
#else
			yield return req.Send();
#endif

			// Like the HttpClient path, HTTP error statuses still hand back the body (the server sends
			// JSON errors); only "no response at all" (responseCode 0: network error / CORS / timeout)
			// is an exception.
			if (req.responseCode == 0) tcs.TrySetException(new Exception(string.IsNullOrEmpty(req.error) ? "network error (server down or CORS blocked?)" : req.error));
			else tcs.TrySetResult(new Reply { Body = req.downloadHandler.text, Status = (int)req.responseCode });
			req.Dispose();
		}
#else
		static readonly HttpClient _http = new HttpClient();
		static bool _configured;

		static void EnsureConfigured()
		{
			if (_configured) return;
			WebworksConfig.Load();
			_http.Timeout = TimeSpan.FromMilliseconds(WebworksConfig.RequestTimeoutMs);
			_configured = true;
		}

		static async Task<Reply> SendOnce(string method, string url, string body)
		{
			EnsureConfigured();
			var req = new HttpRequestMessage(new HttpMethod(method), url);
			if (body != null) req.Content = new StringContent(body, Encoding.UTF8, "application/json");
			if (!string.IsNullOrEmpty(SessionState.SessionToken))
			{
				req.Headers.Add("x-webworks-session", SessionState.SessionToken);
				req.Headers.Add("x-webeam-session", SessionState.SessionToken);
			}
			var resp = await _http.SendAsync(req);
			return new Reply { Body = await resp.Content.ReadAsStringAsync(), Status = (int)resp.StatusCode };
		}
#endif
	}
}
