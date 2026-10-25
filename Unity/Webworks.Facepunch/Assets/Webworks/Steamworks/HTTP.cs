using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine.Networking;
#else
using System.Net.Http;
#endif

namespace Steamworks
{
	public enum HttpMethod { GET, HEAD, POST, PUT, DELETE, OPTIONS, PATCH }

	public struct HttpResponse
	{
		public bool Success;
		public int StatusCode;
		public byte[] Body;
		public Dictionary<string, string> Headers;

		public string BodyString { get { return Body == null ? "" : Encoding.UTF8.GetString(Body); } }
	}

	public class HttpRequest
	{
		internal HttpMethod _method;
		internal string _url;
		internal readonly Dictionary<string, string> _headers = new Dictionary<string, string>();
		internal readonly Dictionary<string, string> _postParams = new Dictionary<string, string>();
		internal byte[] _rawBody;
		internal string _rawBodyContentType;
		internal int _timeoutSeconds = 30;

		public HttpRequest(HttpMethod method, string url) { _method = method; _url = url; }

		public HttpRequest SetHeader(string name, string value) { _headers[name] = value; return this; }
		public HttpRequest SetTimeout(int seconds) { _timeoutSeconds = seconds; return this; }

		public HttpRequest SetParam(string name, string value) { _postParams[name] = value; return this; }

		public HttpRequest SetBody(byte[] data, string contentType = "application/octet-stream") { _rawBody = data; _rawBodyContentType = contentType; return this; }
		public HttpRequest SetBody(string json) { return SetBody(Encoding.UTF8.GetBytes(json), "application/json"); }

		public Task<HttpResponse> SendAsync() { return HTTP.SendAsync(this); }
	}

	public static class HTTP
	{
		public static HttpRequest CreateRequest(HttpMethod method, string url) { return new HttpRequest(method, url); }
		public static HttpRequest Get(string url) { return new HttpRequest(HttpMethod.GET, url); }
		public static HttpRequest Post(string url) { return new HttpRequest(HttpMethod.POST, url); }

		public static async Task<HttpResponse> SendAsync(HttpRequest req)
		{
			var url = BuildUrl(req);
#if UNITY_WEBGL && !UNITY_EDITOR
			return await SendViaUnityWebRequest(req, url);
#else
			return await SendViaHttpClient(req, url);
#endif
		}

		static string BuildUrl(HttpRequest req)
		{
			if (req._method != HttpMethod.GET && req._method != HttpMethod.HEAD) return req._url;
			if (req._postParams.Count == 0) return req._url;

			var sb = new StringBuilder(req._url);
			sb.Append(req._url.IndexOf('?') >= 0 ? '&' : '?');
			bool first = true;
			foreach (var kv in req._postParams)
			{
				if (!first) sb.Append('&');
				first = false;
				sb.Append(Uri.EscapeDataString(kv.Key)).Append('=').Append(Uri.EscapeDataString(kv.Value));
			}
			return sb.ToString();
		}

#if UNITY_WEBGL && !UNITY_EDITOR
		static async Task<HttpResponse> SendViaUnityWebRequest(HttpRequest req, string url)
		{
			var tcs = new TaskCompletionSource<HttpResponse>();
			MainThreadDispatcherRun(SendCoroutine(req, url, tcs));
			return await tcs.Task;
		}

		static void MainThreadDispatcherRun(IEnumerator routine) { Webworks.Internal.MainThreadDispatcher.Run(routine); }

		static IEnumerator SendCoroutine(HttpRequest req, string url, TaskCompletionSource<HttpResponse> tcs)
		{
			UnityWebRequest uwr;
			if (req._method == HttpMethod.GET || req._method == HttpMethod.HEAD)
			{
				uwr = UnityWebRequest.Get(url);
			}
			else
			{
				byte[] body = req._rawBody;
				string contentType = req._rawBodyContentType;
				if (body == null && req._postParams.Count > 0)
				{
					var form = new WWWForm();
					foreach (var kv in req._postParams) form.AddField(kv.Key, kv.Value);
					body = form.data;
					contentType = "application/x-www-form-urlencoded";
				}
				uwr = new UnityWebRequest(url, req._method.ToString());
				uwr.uploadHandler = body != null ? new UploadHandlerRaw(body) : null;
				if (uwr.uploadHandler != null) uwr.uploadHandler.contentType = contentType;
			}
			uwr.downloadHandler = new DownloadHandlerBuffer();
			uwr.timeout = req._timeoutSeconds;
			foreach (var kv in req._headers) uwr.SetRequestHeader(kv.Key, kv.Value);

			yield return uwr.SendWebRequest();

			var resp = new HttpResponse
			{
				Success = uwr.result == UnityWebRequest.Result.Success,
				StatusCode = (int)uwr.responseCode,
				Body = uwr.downloadHandler.data,
				Headers = uwr.GetResponseHeaders() ?? new Dictionary<string, string>(),
			};
			tcs.SetResult(resp);
		}
#else
		static readonly HttpClient _http = new HttpClient();

		static async Task<HttpResponse> SendViaHttpClient(HttpRequest req, string url)
		{
			try
			{
				var message = new System.Net.Http.HttpRequestMessage(new System.Net.Http.HttpMethod(req._method.ToString()), url);
				foreach (var kv in req._headers) message.Headers.TryAddWithoutValidation(kv.Key, kv.Value);

				if (req._method != HttpMethod.GET && req._method != HttpMethod.HEAD)
				{
					if (req._rawBody != null)
					{
						message.Content = new ByteArrayContent(req._rawBody);
						message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(req._rawBodyContentType ?? "application/octet-stream");
					}
					else if (req._postParams.Count > 0)
					{
						message.Content = new FormUrlEncodedContent(req._postParams);
					}
				}

				using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(req._timeoutSeconds)))
				{
					var response = await _http.SendAsync(message, cts.Token);
					var bytes = await response.Content.ReadAsByteArrayAsync();
					var headers = new Dictionary<string, string>();
					foreach (var h in response.Headers) headers[h.Key] = string.Join(",", h.Value);

					return new HttpResponse
					{
						Success = response.IsSuccessStatusCode,
						StatusCode = (int)response.StatusCode,
						Body = bytes,
						Headers = headers,
					};
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Webworks] HTTP request to " + url + " failed: " + ex.Message);
				return new HttpResponse { Success = false, StatusCode = 0, Body = new byte[0], Headers = new Dictionary<string, string>() };
			}
		}
#endif
	}
}
