using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	// HTTP rides the server proxy: the page/standalone client never opens a raw socket,
	// it posts the request to Webworks and gets status, headers and body back.
	public static partial class SteamHTTP
	{
		class CookieJar
		{
			public bool AllowResponses;
			public readonly List<string[]> Cookies = new List<string[]>();
		}

		class Request
		{
			public EHTTPMethod Method;
			public string Url;
			public readonly Dictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			public readonly Dictionary<string, string> Params = new Dictionary<string, string>();
			public byte[] Body;
			public string ContentType;
			public ulong ContextValue;
			public uint TimeoutMs = 15000;
			public bool TimedOut;
			public float Progress;

			public bool Done;
			public bool Success;
			public int Status;
			public byte[] ResponseBody = new byte[0];
			public readonly Dictionary<string, string> ResponseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}

		static readonly Dictionary<uint, Request> _requests = new Dictionary<uint, Request>();
		static readonly Dictionary<uint, CookieJar> _jars = new Dictionary<uint, CookieJar>();
		static uint _nextHandle = 1;

		static Request Find(HTTPRequestHandle handle)
		{
			Request request;
			return _requests.TryGetValue(handle.m_HTTPRequestHandle, out request) ? request : null;
		}

		static string Base64(byte[] data, int count)
		{
			if (data == null || count <= 0) return "";
			return Convert.ToBase64String(data, 0, Math.Min(count, data.Length));
		}

		static string HeaderJson(Dictionary<string, string> headers)
		{
			var body = new StringBuilder("{");
			int index = 0;
			foreach (var kv in headers)
			{
				if (index++ > 0) body.Append(',');
				body.Append(WebworksJson.Quote(kv.Key)).Append(':').Append(WebworksJson.Quote(kv.Value));
			}
			return body.Append('}').ToString();
		}

		static string MethodName(EHTTPMethod method)
		{
			switch (method)
			{
				case EHTTPMethod.k_EHTTPMethodHEAD: return "HEAD";
				case EHTTPMethod.k_EHTTPMethodPOST: return "POST";
				case EHTTPMethod.k_EHTTPMethodPUT: return "PUT";
				case EHTTPMethod.k_EHTTPMethodDELETE: return "DELETE";
				case EHTTPMethod.k_EHTTPMethodOPTIONS: return "OPTIONS";
				case EHTTPMethod.k_EHTTPMethodPATCH: return "PATCH";
				default: return "GET";
			}
		}

		static string BuildUrl(Request request)
		{
			if (request.Params.Count == 0) return request.Url;
			var builder = new StringBuilder(request.Url);
			builder.Append(request.Url.Contains("?") ? '&' : '?');
			int index = 0;
			foreach (var kv in request.Params)
			{
				if (index++ > 0) builder.Append('&');
				builder.Append(Uri.EscapeDataString(kv.Key)).Append('=').Append(Uri.EscapeDataString(kv.Value ?? ""));
			}
			return builder.ToString();
		}

		static void Dispatch(uint handleId, SteamAPICall_t call)
		{
			var request = _requests[handleId];
			var payload = "{\"method\":" + WebworksJson.Quote(MethodName(request.Method))
				+ ",\"url\":" + WebworksJson.Quote(BuildUrl(request))
				+ ",\"headers\":" + HeaderJson(request.Headers)
				+ ",\"bodyBase64\":" + WebworksJson.Quote(Base64(request.Body, request.Body != null ? request.Body.Length : 0))
				+ ",\"timeoutMs\":" + request.TimeoutMs + "}";

			ApiClient.Fire(async () =>
			{
				var started = DateTime.UtcNow;
				var raw = await ApiClient.PostAsync("/HTTP/Request", payload);
				var obj = WebworksJson.Obj(raw);
				request.Done = true;

				if (obj == null)
				{
					request.Success = false;
					request.Status = 0;
				}
				else
				{
					request.Success = WebworksJson.Bool(obj, "ok") && !WebworksJson.Bool(obj, "timedOut");
					request.Status = (int)WebworksJson.Num(obj, "status");
					request.TimedOut = WebworksJson.Bool(obj, "timedOut");

					var body = WebworksJson.Str(obj, "bodyBase64");
					try { request.ResponseBody = string.IsNullOrEmpty(body) ? new byte[0] : Convert.FromBase64String(body); }
					catch { request.ResponseBody = new byte[0]; }

					var headers = WebworksJson.Map(obj, "headers");
					if (headers != null)
					{
						foreach (var pair in headers)
						{
							if (pair.Value is string text) request.ResponseHeaders[pair.Key] = text;
						}
					}
				}

				request.Progress = 1f;
				request.TimedOut = request.TimedOut || (DateTime.UtcNow - started).TotalMilliseconds > request.TimeoutMs;

				var completed = new HTTPRequestCompleted_t
				{
					m_hRequest = new HTTPRequestHandle(handleId),
					m_ulContextValue = request.ContextValue,
					m_bRequestSuccessful = request.Success,
					m_eStatusCode = (EHTTPStatusCode)request.Status,
					m_unBodySize = (uint)request.ResponseBody.Length,
				};

				ResultBus.Complete(call, completed, !request.Success);
				CallbackBus<HTTPRequestCompleted_t>.Fire(completed);

				if (request.ResponseBody.Length > 0)
				{
					CallbackBus<HTTPRequestDataReceived_t>.Fire(new HTTPRequestDataReceived_t
					{
						m_hRequest = new HTTPRequestHandle(handleId),
						m_ulContextValue = request.ContextValue,
						m_cOffset = 0,
						m_cBytesReceived = (uint)request.ResponseBody.Length,
					});
				}

				return "";
			}, "http request");
		}

		public static HTTPRequestHandle CreateHTTPRequest(EHTTPMethod eHTTPRequestMethod, string pchAbsoluteURL)
		{
			if (string.IsNullOrEmpty(pchAbsoluteURL)) return HTTPRequestHandle.Invalid;
			var request = new Request { Method = eHTTPRequestMethod, Url = pchAbsoluteURL };
			var id = _nextHandle++;
			_requests[id] = request;
			return new HTTPRequestHandle(id);
		}

		public static bool SetHTTPRequestContextValue(HTTPRequestHandle hRequest, ulong ulContextValue)
		{
			var request = Find(hRequest);
			if (request == null) return false;
			request.ContextValue = ulContextValue;
			return true;
		}

		public static bool SetHTTPRequestNetworkActivityTimeout(HTTPRequestHandle hRequest, uint unTimeoutSeconds)
		{
			var request = Find(hRequest);
			if (request == null) return false;
			request.TimeoutMs = Math.Max(1000u, unTimeoutSeconds * 1000u);
			return true;
		}

		public static bool SetHTTPRequestHeaderValue(HTTPRequestHandle hRequest, string pchHeaderName, string pchHeaderValue)
		{
			var request = Find(hRequest);
			if (request == null || string.IsNullOrEmpty(pchHeaderName)) return false;
			request.Headers[pchHeaderName] = pchHeaderValue ?? "";
			return true;
		}

		public static bool SetHTTPRequestGetOrPostParameter(HTTPRequestHandle hRequest, string pchParamName, string pchParamValue)
		{
			var request = Find(hRequest);
			if (request == null || string.IsNullOrEmpty(pchParamName)) return false;
			request.Params[pchParamName] = pchParamValue ?? "";
			return true;
		}

		public static bool SendHTTPRequest(HTTPRequestHandle hRequest, out SteamAPICall_t pCallHandle)
		{
			pCallHandle = default(SteamAPICall_t);
			var request = Find(hRequest);
			if (request == null) return false;
			var call = ResultBus.NewCall();
			pCallHandle = call;
			Dispatch(hRequest.m_HTTPRequestHandle, call);
			return true;
		}

		public static bool SendHTTPRequestAndStreamResponse(HTTPRequestHandle hRequest, out SteamAPICall_t pCallHandle)
		{
			// The server proxy answers in one piece, so streaming degrades to a normal send
			// with the data-received callback fired at the end.
			return SendHTTPRequest(hRequest, out pCallHandle);
		}

		public static bool DeferHTTPRequest(HTTPRequestHandle hRequest) { return Find(hRequest) != null; }
		public static bool PrioritizeHTTPRequest(HTTPRequestHandle hRequest) { return Find(hRequest) != null; }

		public static bool GetHTTPResponseHeaderSize(HTTPRequestHandle hRequest, string pchHeaderName, out uint unResponseHeaderSize)
		{
			unResponseHeaderSize = 0;
			var request = Find(hRequest);
			string value;
			if (request == null || !request.ResponseHeaders.TryGetValue(pchHeaderName ?? "", out value)) return false;
			unResponseHeaderSize = (uint)Encoding.UTF8.GetByteCount(value) + 1;
			return true;
		}

		public static bool GetHTTPResponseHeaderValue(HTTPRequestHandle hRequest, string pchHeaderName, byte[] pHeaderValueBuffer, uint unBufferSize)
		{
			var request = Find(hRequest);
			string value;
			if (request == null || pHeaderValueBuffer == null || !request.ResponseHeaders.TryGetValue(pchHeaderName ?? "", out value)) return false;
			var bytes = Encoding.UTF8.GetBytes(value + "\0");
			Array.Copy(bytes, 0, pHeaderValueBuffer, 0, Math.Min(bytes.Length, (int)unBufferSize));
			return true;
		}

		public static bool GetHTTPResponseBodySize(HTTPRequestHandle hRequest, out uint unBodySize)
		{
			unBodySize = 0;
			var request = Find(hRequest);
			if (request == null) return false;
			unBodySize = (uint)request.ResponseBody.Length;
			return true;
		}

		public static bool GetHTTPResponseBodyData(HTTPRequestHandle hRequest, byte[] pBodyDataBuffer, uint unBufferSize)
		{
			var request = Find(hRequest);
			if (request == null || pBodyDataBuffer == null) return false;
			var count = Math.Min(request.ResponseBody.Length, (int)unBufferSize);
			Array.Copy(request.ResponseBody, 0, pBodyDataBuffer, 0, count);
			return true;
		}

		public static bool GetHTTPStreamingResponseBodyData(HTTPRequestHandle hRequest, uint cOffset, byte[] pBodyDataBuffer, uint unBufferSize)
		{
			var request = Find(hRequest);
			if (request == null || pBodyDataBuffer == null) return false;
			var from = (int)Math.Min(cOffset, (uint)request.ResponseBody.Length);
			var count = Math.Min(request.ResponseBody.Length - from, (int)unBufferSize);
			Array.Copy(request.ResponseBody, from, pBodyDataBuffer, 0, count);
			return true;
		}

		public static bool ReleaseHTTPRequest(HTTPRequestHandle hRequest)
		{
			return _requests.Remove(hRequest.m_HTTPRequestHandle);
		}

		public static bool GetHTTPDownloadProgressPct(HTTPRequestHandle hRequest, out float pflPercentOut)
		{
			pflPercentOut = 0f;
			var request = Find(hRequest);
			if (request == null) return false;
			pflPercentOut = request.Progress;
			return true;
		}

		public static bool SetHTTPRequestRawPostBody(HTTPRequestHandle hRequest, string pchContentType, byte[] pubBody, uint unBodyLen)
		{
			var request = Find(hRequest);
			if (request == null) return false;
			request.ContentType = pchContentType ?? "application/octet-stream";
			request.Headers["Content-Type"] = request.ContentType;
			request.Body = new byte[Math.Min(unBodyLen, (uint)(pubBody != null ? pubBody.Length : 0))];
			if (request.Body.Length > 0) Array.Copy(pubBody, request.Body, request.Body.Length);
			return true;
		}

		public static HTTPCookieContainerHandle CreateCookieContainer(bool bAllowResponsesToModify)
		{
			var id = _nextHandle++;
			_jars[id] = new CookieJar { AllowResponses = bAllowResponsesToModify };
			return new HTTPCookieContainerHandle((uint)id);
		}

		public static bool ReleaseCookieContainer(HTTPCookieContainerHandle hCookieContainer)
		{
			return _jars.Remove(hCookieContainer.m_HTTPCookieContainerHandle);
		}

		public static bool SetCookie(HTTPCookieContainerHandle hCookieContainer, string pchHost, string pchUrl, string pchCookie)
		{
			CookieJar jar;
			if (!_jars.TryGetValue(hCookieContainer.m_HTTPCookieContainerHandle, out jar) || string.IsNullOrEmpty(pchCookie)) return false;
			jar.Cookies.Add(new[] { pchHost ?? "", pchUrl ?? "", pchCookie });
			return true;
		}

		public static bool SetHTTPRequestCookieContainer(HTTPRequestHandle hRequest, HTTPCookieContainerHandle hCookieContainer)
		{
			var request = Find(hRequest);
			CookieJar jar;
			if (request == null || !_jars.TryGetValue(hCookieContainer.m_HTTPCookieContainerHandle, out jar)) return false;

			if (jar.Cookies.Count > 0)
			{
				var pairs = new List<string>();
				foreach (var cookie in jar.Cookies)
				{
					var value = cookie[2];
					var semicolon = value.IndexOf(';');
					pairs.Add(semicolon >= 0 ? value.Substring(0, semicolon) : value);
				}
				string existing;
				request.Headers["Cookie"] = request.Headers.TryGetValue("Cookie", out existing)
					? existing + "; " + string.Join("; ", pairs.ToArray())
					: string.Join("; ", pairs.ToArray());
			}
			return true;
		}

		public static bool SetHTTPRequestUserAgentInfo(HTTPRequestHandle hRequest, string pchUserAgentInfo)
		{
			var request = Find(hRequest);
			if (request == null) return false;
			request.Headers["User-Agent"] = string.IsNullOrEmpty(pchUserAgentInfo) ? "Webworks" : pchUserAgentInfo;
			return true;
		}

		public static bool SetHTTPRequestRequiresVerifiedCertificate(HTTPRequestHandle hRequest, bool bRequireVerifiedCertificate)
		{
			// The proxy only speaks https with normal certificate validation, so this is a no-op
			// that always succeeds when the request exists.
			return Find(hRequest) != null;
		}

		public static bool SetHTTPRequestAbsoluteTimeoutMS(HTTPRequestHandle hRequest, uint unMilliseconds)
		{
			var request = Find(hRequest);
			if (request == null) return false;
			request.TimeoutMs = Math.Max(1000u, unMilliseconds);
			return true;
		}

		public static bool GetHTTPRequestWasTimedOut(HTTPRequestHandle hRequest, out bool pbWasTimedOut)
		{
			pbWasTimedOut = false;
			var request = Find(hRequest);
			if (request == null) return false;
			pbWasTimedOut = request.TimedOut;
			return true;
		}

		internal static void Reset()
		{
			_requests.Clear();
			_jars.Clear();
		}
	}
}
