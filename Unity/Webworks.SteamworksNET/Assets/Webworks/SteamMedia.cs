using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	public struct GetVideoURLResult_t
	{
		public EResult m_eResult;
		public AppId_t m_unVideoAppID;
		public string m_rgchURL;
	}

	public struct HTML_BrowserReady_t
	{
		public HHTMLBrowser unBrowserHandle;
	}

	public struct HTML_URLChanged_t
	{
		public HHTMLBrowser unBrowserHandle;
		public string pchURL;
		public string pchPostData;
		public bool bIsRedirect;
		public string pchPageTitle;
		public bool bNewNavigation;
	}

	public struct HTML_FinishedRequest_t
	{
		public HHTMLBrowser unBrowserHandle;
		public string pchURL;
		public string pchPageTitle;
	}

	public struct HTML_CanGoBackAndForward_t
	{
		public HHTMLBrowser unBrowserHandle;
		public bool bCanGoBack;
		public bool bCanGoForward;
	}

	public struct HTML_StartRequest_t
	{
		public HHTMLBrowser unBrowserHandle;
		public string pchURL;
		public string pchTarget;
		public string pchPostData;
		public bool bIsRedirect;
	}

	public struct HTML_LinkAtPosition_t
	{
		public HHTMLBrowser unBrowserHandle;
		public uint x;
		public uint y;
		public string pchURL;
		public bool bLiveLink;
	}

	public struct CreateBeaconCallback_t
	{
		public EResult m_eResult;
		public PartyBeaconID_t m_ulBeaconID;
	}

	public struct JoinPartyCallback_t
	{
		public EResult m_eResult;
		public PartyBeaconID_t m_ulBeaconID;
		public CSteamID m_SteamIDBeaconOwner;
		public string m_rgchConnectString;
	}

	public static partial class SteamMusic
	{
		static string _status = "Stopped";
		static float _volume = 1f;

		static void Publish(string status)
		{
			_status = status;
			var body = "{\"status\":" + WebworksJson.Quote(status)
				+ ",\"track\":" + WebworksJson.Quote(WebworksMusic.Track)
				+ ",\"artist\":" + WebworksJson.Quote(WebworksMusic.Artist)
				+ ",\"volume\":" + _volume.ToString("0.###", CultureInfo.InvariantCulture) + "}";
			ApiClient.Fire(() => ApiClient.PostAsync("/Music/State", body), "music state");
		}

		internal static void ApplyStatus(string status) { _status = status; }

		public static bool BIsEnabled() { return true; }
		public static bool BIsPlaying() { return _status == "Playing"; }
		public static AudioPlayback_Status GetPlaybackStatus()
		{
			if (_status == "Playing") return AudioPlayback_Status.AudioPlayback_Playing;
			if (_status == "Paused") return AudioPlayback_Status.AudioPlayback_Paused;
			return AudioPlayback_Status.AudioPlayback_Idle;
		}

		public static void Play() { Publish("Playing"); }
		public static void Pause() { Publish("Paused"); }
		public static void PlayPrevious() { WebworksMusic.Skip(-1); Publish("Playing"); }
		public static void PlayNext() { WebworksMusic.Skip(1); Publish("Playing"); }

		public static void SetVolume(float flVolume)
		{
			_volume = Mathf.Clamp01(flVolume);
			WebworksMusic.SetVolume(_volume);
			Publish(_status);
		}

		public static float GetVolume() { return _volume; }

		internal static void ApplyServerState(string status, float volume)
		{
			_status = status ?? "Stopped";
			_volume = Mathf.Clamp01(volume);
		}
	}

	public static class WebworksMusic
	{
		public static string Track = "";
		public static string Artist = "";
		static float _volume = 1f;

		static readonly List<string> _playlist = new List<string>();
		static int _index;

		public static IList<string> Playlist { get { return _playlist; } }
		public static int CurrentIndex { get { return _index; } }

		public static void Add(string track)
		{
			if (string.IsNullOrEmpty(track)) return;
			_playlist.Add(track);
			if (_playlist.Count == 1) Track = track;
		}

		public static void SetVolume(float volume) { _volume = volume; }

		public static void Skip(int direction)
		{
			if (_playlist.Count == 0) return;
			_index = (_index + direction + _playlist.Count) % _playlist.Count;
			Track = _playlist[_index];
		}
	}

	public static partial class SteamVideo
	{
		internal static bool Broadcasting;
		internal static int Viewers;
		internal static string BroadcastUrl = "";
		static float _lastPoll = -999f;
		static bool _polling;

		public static void GetVideoURL(AppId_t unVideoAppID)
		{
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/Video/URL?appId=" + unVideoAppID.m_AppId.ToString(CultureInfo.InvariantCulture)));
				var url = payload != null ? WebworksJson.Str(payload, "url") : null;
				CallbackBus<GetVideoURLResult_t>.Fire(new GetVideoURLResult_t
				{
					m_eResult = string.IsNullOrEmpty(url) ? EResult.k_EResultFileNotFound : EResult.k_EResultOK,
					m_unVideoAppID = unVideoAppID,
					m_rgchURL = url ?? "",
				});
				return "";
			}, "GetVideoURL");
		}

		public static bool IsBroadcasting(out int pnNumViewers)
		{
			Poll();
			pnNumViewers = Viewers;
			return Broadcasting;
		}

		public static void GetOPFSettings(AppId_t unVideoAppID) { }
		public static bool GetOPFStringForApp(AppId_t unVideoAppID, out string pchBuffer, ref int pnBufferSize)
		{
			pchBuffer = "";
			return false;
		}

		public static void StartBroadcast(string url, string title)
		{
			var body = "{\"url\":" + WebworksJson.Quote(url ?? "") + ",\"title\":" + WebworksJson.Quote(title ?? "") + "}";
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/Video/Broadcast/Start", body));
				var broadcast = payload != null ? WebworksJson.Map(payload, "broadcast") : null;
				if (broadcast == null) return "";
				Broadcasting = WebworksJson.Bool(broadcast, "broadcasting");
				Viewers = WebworksJson.Int(broadcast, "viewers");
				BroadcastUrl = WebworksJson.Str(broadcast, "url") ?? "";
				return "";
			}, "StartBroadcast");
		}

		public static void StopBroadcast()
		{
			ApiClient.Fire(() => ApiClient.PostAsync("/Video/Broadcast/Stop", "{}"), "StopBroadcast");
			Broadcasting = false;
			Viewers = 0;
		}

		internal static void Poll()
		{
			if (_polling || Time.realtimeSinceStartup - _lastPoll < 2f) return;
			_lastPoll = Time.realtimeSinceStartup;
			_polling = true;
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/Video/Status"));
				if (payload != null)
				{
					Broadcasting = WebworksJson.Bool(payload, "broadcasting");
					Viewers = WebworksJson.Int(payload, "viewers");
					BroadcastUrl = WebworksJson.Str(payload, "url") ?? "";
				}
				_polling = false;
				return "";
			}, "video status");
		}
	}

	public struct RemotePlaySession
	{
		public uint Id;
		public CSteamID SteamId;
		public string ClientName;
		public int ResolutionX;
		public int ResolutionY;
		public bool RemotePlayTogether;
	}

	public static partial class SteamRemotePlay
	{
		internal static string HostSessionId;
		internal static readonly List<RemotePlaySession> Sessions = new List<RemotePlaySession>();
		internal static readonly List<RemotePlayInput_t> PendingInput = new List<RemotePlayInput_t>();
		static float _lastPoll = -999f;
		static bool _polling;

		public static void StartHosting(string title = "")
		{
			var body = "{\"title\":" + WebworksJson.Quote(title) + ",\"maxGuests\":4}";
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/RemotePlay/Start", body));
				var session = payload != null ? WebworksJson.Map(payload, "session") : null;
				if (session != null) HostSessionId = WebworksJson.Str(session, "id");
				return "";
			}, "remote play host");
		}

		public static void StopHosting()
		{
			if (string.IsNullOrEmpty(HostSessionId)) return;
			var body = "{\"sessionId\":" + WebworksJson.Quote(HostSessionId) + "}";
			ApiClient.Fire(() => ApiClient.PostAsync("/RemotePlay/Stop", body), "remote play stop");
			HostSessionId = null;
			Sessions.Clear();
		}

		public static uint GetSessionCount() { Poll(); return (uint)Sessions.Count; }

		public static RemotePlaySessionID_t GetSessionID(int iSessionIndex)
		{
			if (iSessionIndex < 0 || iSessionIndex >= Sessions.Count) return new RemotePlaySessionID_t(0);
			return new RemotePlaySessionID_t(Sessions[iSessionIndex].Id);
		}

		static RemotePlaySession Session(uint id)
		{
			foreach (var session in Sessions) if (session.Id == id) return session;
			return new RemotePlaySession { Id = id };
		}

		public static bool BSessionRemotePlayTogether(RemotePlaySessionID_t unSessionID) { return Session(unSessionID.m_RemotePlaySessionID).RemotePlayTogether; }
		public static CSteamID GetSessionSteamID(RemotePlaySessionID_t unSessionID) { return Session(unSessionID.m_RemotePlaySessionID).SteamId; }
		public static uint GetSessionGuestID(RemotePlaySessionID_t unSessionID) { return 0; }
		public static int GetSmallSessionAvatar(RemotePlaySessionID_t unSessionID) { return 0; }
		public static int GetMediumSessionAvatar(RemotePlaySessionID_t unSessionID) { return 0; }
		public static int GetLargeSessionAvatar(RemotePlaySessionID_t unSessionID) { return 0; }
		public static string GetSessionClientName(RemotePlaySessionID_t unSessionID) { return Session(unSessionID.m_RemotePlaySessionID).ClientName ?? ""; }

		public static ESteamDeviceFormFactor GetSessionClientFormFactor(RemotePlaySessionID_t unSessionID)
		{
			return ESteamDeviceFormFactor.k_ESteamDeviceFormFactorComputer;
		}

		public static bool BGetSessionClientResolution(RemotePlaySessionID_t unSessionID, out int pnResolutionX, out int pnResolutionY)
		{
			var session = Session(unSessionID.m_RemotePlaySessionID);
			pnResolutionX = session.ResolutionX;
			pnResolutionY = session.ResolutionY;
			return session.ResolutionX > 0;
		}

		public static bool ShowRemotePlayTogetherUI() { return true; }

		public static bool BSendRemotePlayTogetherInvite(CSteamID steamIDFriend)
		{
			var body = "{\"steamId\":" + WebworksJson.Quote(steamIDFriend.m_SteamID.ToString(CultureInfo.InvariantCulture))
				+ ",\"sessionId\":" + WebworksJson.Quote(HostSessionId ?? "") + "}";
			ApiClient.Fire(() => ApiClient.PostAsync("/RemotePlay/Invite", body), "remote play invite");
			return true;
		}

		public static bool BEnableRemotePlayTogetherDirectInput() { return true; }
		public static void DisableRemotePlayTogetherDirectInput() { }

		public static uint GetInput(RemotePlayInput_t[] pInput, uint unMaxEvents)
		{
			if (pInput == null) return 0;
			uint count = 0;
			while (count < unMaxEvents && PendingInput.Count > 0 && count < pInput.Length)
			{
				pInput[count++] = PendingInput[0];
				PendingInput.RemoveAt(0);
			}
			return count;
		}

		public static void SetMouseVisibility(RemotePlaySessionID_t unSessionID, bool bVisible) { }
		public static void SetMousePosition(RemotePlaySessionID_t unSessionID, float flNormalizedX, float flNormalizedY) { }
		public static RemotePlayCursorID_t CreateMouseCursor(int nWidth, int nHeight, int nHotX, int nHotY, IntPtr pBGRA, int nPitch = 0) { return default(RemotePlayCursorID_t); }
		public static void SetMouseCursor(RemotePlaySessionID_t unSessionID, RemotePlayCursorID_t unCursorID) { }

		internal static void Poll()
		{
			if (_polling || Time.realtimeSinceStartup - _lastPoll < 2f) return;
			_lastPoll = Time.realtimeSinceStartup;
			_polling = true;

			ApiClient.Fire(async () =>
			{
				var list = WebworksJson.Obj(await ApiClient.GetAsync("/RemotePlay/Sessions"));
				Sessions.Clear();
				var entries = list != null ? WebworksJson.Arr(list, "sessions") : null;
				if (entries != null)
				{
					foreach (var raw in entries)
					{
						var entry = raw as Dictionary<string, object>;
						if (entry == null) continue;
						ulong hostId;
						ulong.TryParse(WebworksJson.Str(entry, "hostSteamId") ?? "0", NumberStyles.Integer, CultureInfo.InvariantCulture, out hostId);
						var session = new RemotePlaySession
						{
							Id = (uint)(Sessions.Count + 1),
							SteamId = new CSteamID(hostId),
							ClientName = WebworksJson.Str(entry, "hostName") ?? "",
							ResolutionX = WebworksJson.Int(entry, "resolutionX"),
							ResolutionY = WebworksJson.Int(entry, "resolutionY"),
							RemotePlayTogether = true,
						};
						Sessions.Add(session);
					}
				}

				if (!string.IsNullOrEmpty(HostSessionId))
				{
					var input = WebworksJson.Obj(await ApiClient.GetAsync("/RemotePlay/Input?sessionId=" + Uri.EscapeDataString(HostSessionId)));
					var events = input != null ? WebworksJson.Arr(input, "events") : null;
					if (events != null)
					{
						foreach (var raw in events)
						{
							var entry = raw as Dictionary<string, object>;
							if (entry == null) continue;
							PendingInput.Add(new RemotePlayInput_t
							{
								m_unSessionID = new RemotePlaySessionID_t(0),
								m_eType = ERemotePlayInputType.k_ERemotePlayInputMouseMotion,
							});
						}
					}
				}

				_polling = false;
				return "";
			}, "remote play poll");
		}
	}

	public static partial class SteamHTMLSurface
	{
		class Browser
		{
			public uint Id;
			public string Url = "";
			public string Title = "";
			public string Text = "";
			public readonly List<string> History = new List<string>();
			public int Index = -1;
			public uint Width;
			public uint Height;
		}

		static readonly Dictionary<uint, Browser> _browsers = new Dictionary<uint, Browser>();
		static uint _nextBrowser = 1;
		static bool _ready;

		public static bool Init() { _ready = true; return true; }

		public static bool Shutdown()
		{
			_browsers.Clear();
			_ready = false;
			return true;
		}

		public static SteamAPICall_t CreateBrowser(string pchUserAgent, string pchUserCSS)
		{
			var call = ResultBus.NewCall();
			var browser = new Browser { Id = _nextBrowser++ };
			_browsers[browser.Id] = browser;
			ResultBus.Complete<HTML_BrowserReady_t>(call, new HTML_BrowserReady_t { unBrowserHandle = new HHTMLBrowser(browser.Id) });
			return call;
		}

		static Browser Find(HHTMLBrowser handle)
		{
			Browser browser;
			return _browsers.TryGetValue(handle.m_HHTMLBrowser, out browser) ? browser : null;
		}

		public static void RemoveBrowser(HHTMLBrowser unBrowserHandle)
		{
			_browsers.Remove(unBrowserHandle.m_HHTMLBrowser);
			HtmlSurfaceNative.Close(unBrowserHandle.m_HHTMLBrowser);
		}

		public static void LoadURL(HHTMLBrowser unBrowserHandle, string pchURL, string pchPostData)
		{
			var browser = Find(unBrowserHandle);
			if (browser == null || string.IsNullOrEmpty(pchURL)) return;

			browser.History.RemoveRange(browser.Index + 1, browser.History.Count - browser.Index - 1);
			browser.History.Add(pchURL);
			browser.Index = browser.History.Count - 1;
			browser.Url = pchURL;

			HtmlSurfaceNative.Navigate(browser.Id, pchURL, browser.Width, browser.Height);
			CallbackBus<HTML_StartRequest_t>.Fire(new HTML_StartRequest_t
			{
				unBrowserHandle = unBrowserHandle,
				pchURL = pchURL,
				pchTarget = "",
				pchPostData = pchPostData ?? "",
			});

			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/HTML/Fetch?url=" + Uri.EscapeDataString(pchURL)));
				if (payload == null || WebworksJson.Str(payload, "error") != null) return "";

				browser.Url = WebworksJson.Str(payload, "url") ?? pchURL;
				browser.Title = WebworksJson.Str(payload, "title") ?? "";
				browser.Text = WebworksJson.Str(payload, "text") ?? "";

				CallbackBus<HTML_FinishedRequest_t>.Fire(new HTML_FinishedRequest_t
				{
					unBrowserHandle = unBrowserHandle,
					pchURL = browser.Url,
					pchPageTitle = browser.Title,
				});
				CallbackBus<HTML_URLChanged_t>.Fire(new HTML_URLChanged_t
				{
					unBrowserHandle = unBrowserHandle,
					pchURL = browser.Url,
					pchPageTitle = browser.Title,
				});
				CallbackBus<HTML_CanGoBackAndForward_t>.Fire(new HTML_CanGoBackAndForward_t
				{
					unBrowserHandle = unBrowserHandle,
					bCanGoBack = browser.Index > 0,
					bCanGoForward = browser.Index + 1 < browser.History.Count,
				});
				return "";
			}, "HTML LoadURL");
		}

		public static void SetSize(HHTMLBrowser unBrowserHandle, uint unWidth, uint unHeight)
		{
			var browser = Find(unBrowserHandle);
			if (browser == null) return;
			browser.Width = unWidth;
			browser.Height = unHeight;
			HtmlSurfaceNative.Resize(browser.Id, unWidth, unHeight);
		}

		public static void StopLoad(HHTMLBrowser unBrowserHandle) { }
		public static void Reload(HHTMLBrowser unBrowserHandle) { var b = Find(unBrowserHandle); if (b != null) LoadURL(unBrowserHandle, b.Url, ""); }

		public static void GoBack(HHTMLBrowser unBrowserHandle)
		{
			var browser = Find(unBrowserHandle);
			if (browser == null || browser.Index <= 0) return;
			browser.Index--;
			var url = browser.History[browser.Index];
			LoadURL(unBrowserHandle, url, "");
			browser.Index--;
		}

		public static void GoForward(HHTMLBrowser unBrowserHandle)
		{
			var browser = Find(unBrowserHandle);
			if (browser == null || browser.Index + 1 >= browser.History.Count) return;
			browser.Index++;
			LoadURL(unBrowserHandle, browser.History[browser.Index], "");
		}

		public static void AddHeader(HHTMLBrowser unBrowserHandle, string pchKey, string pchValue) { }
		public static void ExecuteJavascript(HHTMLBrowser unBrowserHandle, string pchScript) { var b = Find(unBrowserHandle); if (b != null) HtmlSurfaceNative.ExecuteJs(b.Id, pchScript); }
		public static void MouseUp(HHTMLBrowser unBrowserHandle, EHTMLMouseButton eMouseButton) { }
		public static void MouseDown(HHTMLBrowser unBrowserHandle, EHTMLMouseButton eMouseButton) { }
		public static void MouseDoubleClick(HHTMLBrowser unBrowserHandle, EHTMLMouseButton eMouseButton) { }
		public static void MouseMove(HHTMLBrowser unBrowserHandle, int x, int y) { }
		public static void MouseWheel(HHTMLBrowser unBrowserHandle, int nDelta) { }
		public static void KeyDown(HHTMLBrowser unBrowserHandle, uint nNativeKeyCode, EHTMLKeyModifiers eHTMLKeyModifiers, bool bIsSystemKey = false) { }
		public static void KeyUp(HHTMLBrowser unBrowserHandle, uint nNativeKeyCode, EHTMLKeyModifiers eHTMLKeyModifiers) { }
		public static void KeyChar(HHTMLBrowser unBrowserHandle, uint cUnicodeChar, EHTMLKeyModifiers eHTMLKeyModifiers) { }
		public static void SetHorizontalScroll(HHTMLBrowser unBrowserHandle, uint nAbsolutePixelScroll) { }
		public static void SetVerticalScroll(HHTMLBrowser unBrowserHandle, uint nAbsolutePixelScroll) { }
		public static void SetKeyFocus(HHTMLBrowser unBrowserHandle, bool bHasKeyFocus) { }

		public static void ViewSource(HHTMLBrowser unBrowserHandle)
		{
			var browser = Find(unBrowserHandle);
			if (browser != null && !string.IsNullOrEmpty(browser.Url)) Application.OpenURL("view-source:" + browser.Url);
		}

		public static void CopyToClipboard(HHTMLBrowser unBrowserHandle) { }
		public static void PasteFromClipboard(HHTMLBrowser unBrowserHandle) { }
		public static void Find(HHTMLBrowser unBrowserHandle, string pchSearchStr, bool bCurrentlyInFind, bool bReverse) { }
		public static void StopFind(HHTMLBrowser unBrowserHandle) { }

		public static void GetLinkAtPosition(HHTMLBrowser unBrowserHandle, int x, int y)
		{
			CallbackBus<HTML_LinkAtPosition_t>.Fire(new HTML_LinkAtPosition_t
			{
				unBrowserHandle = unBrowserHandle,
				x = (uint)Math.Max(0, x),
				y = (uint)Math.Max(0, y),
				pchURL = "",
			});
		}

		public static void SetCookie(string pchHostname, string pchKey, string pchValue, string pchPath = "/", uint nExpires = 0, bool bSecure = false, bool bHTTPOnly = false) { }
		public static void SetPageScaleFactor(HHTMLBrowser unBrowserHandle, float flZoom, int nPointX, int nPointY) { }
		public static void SetBackgroundMode(HHTMLBrowser unBrowserHandle, bool bBackgroundMode) { }
		public static void SetDPIScalingFactor(HHTMLBrowser unBrowserHandle, float flDPIScaling) { }
		public static void OpenDeveloperTools(HHTMLBrowser unBrowserHandle) { var b = Find(unBrowserHandle); if (b != null && !string.IsNullOrEmpty(b.Url)) Application.OpenURL(b.Url); }
		public static void AllowStartRequest(HHTMLBrowser unBrowserHandle, bool bAllowed) { }
		public static void JSDialogResponse(HHTMLBrowser unBrowserHandle, bool bResult) { }
		public static void FileLoadDialogResponse(HHTMLBrowser unBrowserHandle, IntPtr pchSelectedFiles) { }

		internal static string TextOf(HHTMLBrowser handle)
		{
			var browser = Find(handle);
			return browser != null ? browser.Text : "";
		}
	}

	internal static class HtmlSurfaceNative
	{
		public static void Navigate(uint id, string url, uint width, uint height)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			WebworksNative.Webworks_HTML_Open((int)id, url);
			WebworksNative.Webworks_HTML_Resize((int)id, (int)width, (int)height);
#else
			WebworksNativeNoop.Touch();
#endif
		}

		public static void Resize(uint id, uint width, uint height)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			WebworksNative.Webworks_HTML_Resize((int)id, (int)width, (int)height);
#endif
		}

		public static void ExecuteJs(uint id, string script)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			WebworksNative.Webworks_HTML_Execute((int)id, script);
#endif
		}

		public static void Close(uint id)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			WebworksNative.Webworks_HTML_Close((int)id);
#endif
		}
	}

	internal static class WebworksNativeNoop
	{
		public static void Touch() { }
	}

	public static partial class SteamParties
	{
		internal static readonly List<PartyBeacon> Beacons = new List<PartyBeacon>();
		static readonly Dictionary<ulong, string> _metadata = new Dictionary<ulong, string>();
		static ulong _nextLocalBeacon = 1;

		public static uint GetNumActiveBeacons() { return (uint)Beacons.Count; }

		public static PartyBeaconID_t GetBeaconByIndex(uint unIndex)
		{
			return unIndex < Beacons.Count ? Beacons[(int)unIndex].Id : PartyBeaconID_t.Invalid;
		}

		public static bool GetBeaconDetails(PartyBeaconID_t ulBeaconID, out CSteamID pSteamIDBeaconOwner, out SteamPartyBeaconLocation_t pLocation, out string pchMetadata, int cchMetadata)
		{
			pSteamIDBeaconOwner = CSteamID.Nil;
			pLocation = new SteamPartyBeaconLocation_t { m_eType = ESteamPartyBeaconLocationType.k_ESteamPartyBeaconLocationType_ChatGroup };
			pchMetadata = "";

			foreach (var beacon in Beacons)
			{
				if (beacon.Id.m_PartyBeaconID != ulBeaconID.m_PartyBeaconID) continue;
				pSteamIDBeaconOwner = beacon.Owner;
				pchMetadata = _metadata.ContainsKey(beacon.Id.m_PartyBeaconID) ? _metadata[beacon.Id.m_PartyBeaconID] : "";
				return true;
			}
			return false;
		}

		public static SteamAPICall_t JoinParty(PartyBeaconID_t ulBeaconID)
		{
			var call = ResultBus.NewCall();
			var body = "{\"beaconId\":" + WebworksJson.Quote(ulBeaconID.m_PartyBeaconID.ToString(CultureInfo.InvariantCulture)) + "}";
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/Party/Join", body));
				ResultBus.Complete<JoinPartyCallback_t>(call, new JoinPartyCallback_t
				{
					m_eResult = payload != null && WebworksJson.Str(payload, "error") == null ? EResult.k_EResultOK : EResult.k_EResultFail,
					m_ulBeaconID = ulBeaconID,
					m_rgchConnectString = "",
				});
				return "";
			}, "JoinParty");
			return call;
		}

		public static bool GetNumAvailableBeaconLocations(out uint puNumLocations)
		{
			puNumLocations = 1;
			return true;
		}

		public static bool GetAvailableBeaconLocations(SteamPartyBeaconLocation_t[] pLocationList, uint uMaxNumLocations)
		{
			if (pLocationList == null || uMaxNumLocations == 0) return false;
			pLocationList[0] = new SteamPartyBeaconLocation_t
			{
				m_eType = ESteamPartyBeaconLocationType.k_ESteamPartyBeaconLocationType_ChatGroup,
				m_ulLocationID = 0,
			};
			return true;
		}

		public static SteamAPICall_t CreateBeacon(uint unOpenSlots, ref SteamPartyBeaconLocation_t pBeaconLocation, string pchConnectString, string pchMetadata)
		{
			var call = ResultBus.NewCall();
			var body = "{\"maxMembers\":" + Math.Max(1, unOpenSlots) + ",\"metadata\":" + WebworksJson.Quote(pchMetadata ?? "") + "}";
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/Party/Create", body));
				var beaconId = payload != null ? WebworksJson.Str(payload, "beaconId") : null;

				ulong id = 0;
				if (!string.IsNullOrEmpty(beaconId)) ulong.TryParse(beaconId, out id);
				if (id == 0) id = _nextLocalBeacon++;

				_metadata[id] = pchMetadata ?? "";
				ResultBus.Complete<CreateBeaconCallback_t>(call, new CreateBeaconCallback_t
				{
					m_eResult = string.IsNullOrEmpty(beaconId) ? EResult.k_EResultFail : EResult.k_EResultOK,
					m_ulBeaconID = new PartyBeaconID_t(id),
				});
				return "";
			}, "CreateBeacon");
			return call;
		}

		public static void OnReservationCompleted(PartyBeaconID_t ulBeacon, CSteamID steamIDUser) { }

		public static void CancelReservation(PartyBeaconID_t ulBeacon, CSteamID steamIDUser)
		{
			var body = "{\"beaconId\":" + WebworksJson.Quote(ulBeacon.m_PartyBeaconID.ToString(CultureInfo.InvariantCulture)) + "}";
			ApiClient.Fire(() => ApiClient.PostAsync("/Party/Leave", body), "CancelReservation");
		}

		public static SteamAPICall_t ChangeNumOpenSlots(PartyBeaconID_t ulBeacon, uint unOpenSlots) { return ResultBus.NewCall(); }

		public static bool DestroyBeacon(PartyBeaconID_t ulBeacon)
		{
			ApiClient.Fire(() => ApiClient.DeleteAsync("/Party/Beacon?id=" + ulBeacon.m_PartyBeaconID.ToString(CultureInfo.InvariantCulture)), "DestroyBeacon");
			return true;
		}

		public static bool GetBeaconLocationData(SteamPartyBeaconLocation_t BeaconLocation, ESteamPartyBeaconLocationData eData, out string pchDataStringOut, int cchDataStringOut)
		{
			pchDataStringOut = "";
			return false;
		}

		internal static void Poll()
		{
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/Party/Beacons"));
				var entries = payload != null ? WebworksJson.Arr(payload, "beacons") : null;
				if (entries == null) return "";

				Beacons.Clear();
				foreach (var raw in entries)
				{
					var entry = raw as Dictionary<string, object>;
					if (entry == null) continue;

					ulong id;
					ulong.TryParse(WebworksJson.Str(entry, "beaconId") ?? "0", out id);
					ulong owner;
					ulong.TryParse(WebworksJson.Str(entry, "ownerSteamId") ?? "0", out owner);

					var metadata = WebworksJson.Str(entry, "metadata") ?? "";
					_metadata[id] = metadata;
					Beacons.Add(new PartyBeacon
					{
						Id = new PartyBeaconID_t(id),
						Owner = new CSteamID(owner),
						Metadata = metadata,
					});
				}
				return "";
			}, "party beacons");
		}
	}

	public struct PartyBeacon
	{
		public PartyBeaconID_t Id;
		public CSteamID Owner;
		public string Metadata;
		public void Destroy() { SteamParties.DestroyBeacon(Id); }
		public void CancelReservation(CSteamID user) { SteamParties.CancelReservation(Id, user); }
	}
}

namespace Webworks.SNetInternal
{
	internal static class MediaPump
	{
		public static void Poll()
		{
			Steamworks.SteamVideo.Poll();
			Steamworks.SteamRemotePlay.Poll();
			Steamworks.SteamParties.Poll();
		}
	}
}
