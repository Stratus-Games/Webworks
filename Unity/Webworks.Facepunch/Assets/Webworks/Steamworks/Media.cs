#pragma warning disable CS0067
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Steamworks.Data;
using UnityEngine;
using Webworks.Internal;

namespace Steamworks
{
	public static class SteamMusic
	{
		public static event Action OnPlaybackChanged;
		public static event Action<float> OnVolumeChanged;

		static MusicStatus _status = MusicStatus.Idle;
		static float _volume = 1f;
		static string _track = "";
		static string _artist = "";

		public static bool IsEnabled { get { return true; } }
		public static bool IsPlaying { get { return _status == MusicStatus.Playing; } }
		public static MusicStatus Status { get { return _status; } }
		public static float Volume { get { return _volume; } set { SetVolume(value); } }

		public static string Track { get { return _track; } }
		public static string Artist { get { return _artist; } }

		public static void SetTrack(string track, string artist)
		{
			_track = track ?? "";
			_artist = artist ?? "";
			Publish();
		}

		public static void Play() { _status = MusicStatus.Playing; Publish(); }
		public static void Pause() { _status = MusicStatus.Paused; Publish(); }
		public static void PlayNext() { _status = MusicStatus.Playing; Publish(); }
		public static void PlayPrevious() { _status = MusicStatus.Playing; Publish(); }

		public static void SetVolume(float volume)
		{
			_volume = Mathf.Clamp01(volume);
			OnVolumeChanged?.Invoke(_volume);
			Publish();
		}

		static void Publish()
		{
			var body = new StringBuilder("{");
			body.Append("\"status\":").Append(WebworksJson.Quote(_status.ToString()));
			body.Append(",\"track\":").Append(WebworksJson.Quote(_track));
			body.Append(",\"artist\":").Append(WebworksJson.Quote(_artist));
			body.Append(",\"volume\":").Append(_volume.ToString("0.###", CultureInfo.InvariantCulture));
			body.Append('}');
			ApiClient.Fire(() => ApiClient.PostAsync("/Music/State", body.ToString()), "music state");
			OnPlaybackChanged?.Invoke();
		}

		internal static void ApplyRemote(string status, string track, string artist, float volume)
		{
			MusicStatus parsed;
			_status = Enum.TryParse(status, true, out parsed) ? parsed : MusicStatus.Idle;
			_track = track ?? "";
			_artist = artist ?? "";
			_volume = Mathf.Clamp01(volume);
			OnPlaybackChanged?.Invoke();
		}
	}

	public static class SteamParental
	{
		const string KeyEnabled = "webworks_parental_enabled";
		const string KeyLocked = "webworks_parental_locked";
		const string KeyApps = "webworks_parental_apps";
		const string KeyFeatures = "webworks_parental_features";

		public static event Action OnSettingsChanged;

		static bool _loaded;
		static bool _enabled;
		static bool _locked;
		static readonly HashSet<uint> _blockedApps = new HashSet<uint>();
		static readonly HashSet<int> _blockedFeatures = new HashSet<int>();

		static void Load()
		{
			if (_loaded) return;
			_loaded = true;
			_enabled = PlayerPrefs.GetString(KeyEnabled, "") == "1";
			_locked = PlayerPrefs.GetString(KeyLocked, "") == "1";
			foreach (var part in PlayerPrefs.GetString(KeyApps, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
			{
				uint app;
				if (uint.TryParse(part, out app)) _blockedApps.Add(app);
			}
			foreach (var part in PlayerPrefs.GetString(KeyFeatures, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
			{
				int feature;
				if (int.TryParse(part, out feature)) _blockedFeatures.Add(feature);
			}
		}

		static void Save()
		{
			PlayerPrefs.SetString(KeyEnabled, _enabled ? "1" : "0");
			PlayerPrefs.SetString(KeyLocked, _locked ? "1" : "0");
			PlayerPrefs.SetString(KeyApps, string.Join(",", _blockedApps));
			PlayerPrefs.SetString(KeyFeatures, string.Join(",", _blockedFeatures));
			PlayerPrefs.Save();
			OnSettingsChanged?.Invoke();
		}

		public static bool IsParentalLockEnabled { get { Load(); return _enabled; } }
		public static bool IsParentalLockLocked { get { Load(); return _enabled && _locked; } }

		public static void Enable(bool enabled, bool locked = true)
		{
			Load();
			_enabled = enabled;
			_locked = enabled && locked;
			Save();
		}

		public static void BlockApp(AppId app, bool blocked)
		{
			Load();
			if (blocked) _blockedApps.Add(app.Value); else _blockedApps.Remove(app.Value);
			Save();
		}

		public static void BlockFeature(ParentalFeature feature, bool blocked)
		{
			Load();
			if (blocked) _blockedFeatures.Add((int)feature); else _blockedFeatures.Remove((int)feature);
			Save();
		}

		public static bool BIsAppInBlockList(AppId app) { Load(); return _enabled && _blockedApps.Contains(app.Value); }
		public static bool BIsFeatureInBlockList(ParentalFeature feature) { Load(); return _enabled && _blockedFeatures.Contains((int)feature); }
		public static bool IsAppBlocked(AppId app) { return BIsAppInBlockList(app); }
		public static bool IsFeatureBlocked(ParentalFeature feature) { return BIsFeatureInBlockList(feature); }
	}

	public struct PartyBeacon
	{
		public static event Action<ulong, SteamId> OnReservationCompleted;

		public ulong Id;
		public string MetaData;
		public SteamId Owner;

		public void Destroy() { SteamParties.DestroyBeacon(Id); }
		public void CancelReservation(SteamId user) { SteamParties.CancelReservation(Id, user); }

		internal void RaiseReservation(SteamId user) { OnReservationCompleted?.Invoke(Id, user); }
	}

	public static class SteamParties
	{
		public static event Action OnBeaconLocationsUpdated;
		public static event Action OnActiveBeaconsUpdated;

		static readonly List<PartyBeacon> _beacons = new List<PartyBeacon>();
		static float _lastPoll = -999f;
		static bool _polling;
		static ulong _hostBeacon;

		public static uint ActiveBeaconCount { get { Poll(); return (uint)_beacons.Count; } }
		public static IEnumerable<PartyBeacon> ActiveBeacons { get { Poll(); return _beacons; } }

		public static ulong CreateBeacon(string metadata, int maxMembers = 8)
		{
			var body = "{\"metadata\":" + WebworksJson.Quote(metadata ?? "") + ",\"maxMembers\":" + Math.Max(1, maxMembers) + "}";
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/Party/Create", body));
				var id = payload != null ? WebworksJson.Str(payload, "beaconId") : null;
				ulong parsed;
				if (!string.IsNullOrEmpty(id) && ulong.TryParse(id, out parsed)) _hostBeacon = parsed;
				await PollAsync();
				return "";
			}, "CreateBeacon");
			return _hostBeacon;
		}

		public static bool JoinBeacon(ulong id)
		{
			var body = "{\"beaconId\":" + WebworksJson.Quote(id.ToString(CultureInfo.InvariantCulture)) + "}";
			ApiClient.Fire(async () =>
			{
				await ApiClient.PostAsync("/Party/Join", body);
				await PollAsync();
				return "";
			}, "JoinBeacon");
			return true;
		}

		public static void LeaveBeacon()
		{
			if (_hostBeacon == 0) return;
			var body = "{\"beaconId\":" + WebworksJson.Quote(_hostBeacon.ToString(CultureInfo.InvariantCulture)) + "}";
			ApiClient.Fire(() => ApiClient.PostAsync("/Party/Leave", body), "LeaveBeacon");
			_hostBeacon = 0;
		}

		public static bool Invite(SteamId friend)
		{
			var body = "{\"steamId\":" + WebworksJson.Quote(friend.Value.ToString(CultureInfo.InvariantCulture))
				+ ",\"beaconId\":" + WebworksJson.Quote(_hostBeacon.ToString(CultureInfo.InvariantCulture)) + "}";
			ApiClient.Fire(() => ApiClient.PostAsync("/Party/Invite", body), "party invite");
			return true;
		}

		internal static void DestroyBeacon(ulong id)
		{
			ApiClient.Fire(() => ApiClient.DeleteAsync("/Party/Beacon?id=" + id.ToString(CultureInfo.InvariantCulture)), "DestroyBeacon");
			if (_hostBeacon == id) _hostBeacon = 0;
		}

		internal static void CancelReservation(ulong id, SteamId user)
		{
			var body = "{\"beaconId\":" + WebworksJson.Quote(id.ToString(CultureInfo.InvariantCulture)) + "}";
			ApiClient.Fire(() => ApiClient.PostAsync("/Party/Leave", body), "CancelReservation");
		}

		static void Poll()
		{
			if (_polling || Time.realtimeSinceStartup - _lastPoll < 2f) return;
			_lastPoll = Time.realtimeSinceStartup;
			_polling = true;
			PollAsync().Forget("party poll");
		}

		static async Task PollAsync()
		{
			try
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/Party/Beacons"));
				var entries = payload != null ? WebworksJson.Arr(payload, "beacons") : null;
				if (entries == null) return;

				_beacons.Clear();
				foreach (var raw in entries)
				{
					var entry = raw as Dictionary<string, object>;
					if (entry == null) continue;

					ulong id, owner;
					ulong.TryParse(WebworksJson.Str(entry, "beaconId") ?? "0", out id);
					ulong.TryParse(WebworksJson.Str(entry, "ownerSteamId") ?? "0", out owner);
					_beacons.Add(new PartyBeacon
					{
						Id = id,
						Owner = owner,
						MetaData = WebworksJson.Str(entry, "metadata") ?? "",
					});
				}
				OnActiveBeaconsUpdated?.Invoke();
			}
			finally
			{
				_polling = false;
			}
		}
	}

	public static class SteamRemotePlay
	{
		public static event Action<RemotePlaySession> OnSessionConnected;
		public static event Action<RemotePlaySession> OnSessionDisconnected;

		static readonly List<RemotePlaySession> _sessions = new List<RemotePlaySession>();
		static float _lastPoll = -999f;
		static bool _polling;

		public static int SessionCount { get { Poll(); return _sessions.Count; } }

		public static RemotePlaySession GetSession(int index)
		{
			Poll();
			if (index < 0 || index >= _sessions.Count) return default(RemotePlaySession);
			return _sessions[index];
		}

		public static bool SendInvite(SteamId steamid)
		{
			var body = "{\"steamId\":" + WebworksJson.Quote(steamid.Value.ToString(CultureInfo.InvariantCulture));
			if (!string.IsNullOrEmpty(SteamNetworking.HostSessionId)) body += ",\"sessionId\":" + WebworksJson.Quote(SteamNetworking.HostSessionId);
			body += "}";
			ApiClient.Fire(() => ApiClient.PostAsync("/RemotePlay/Invite", body), "remote play invite");
			return true;
		}

		public static void StartHosting(string title = "")
		{
			var body = "{\"title\":" + WebworksJson.Quote(title) + ",\"maxGuests\":4}";
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/RemotePlay/Start", body));
				var session = payload != null ? WebworksJson.Map(payload, "session") : null;
				if (session != null) SteamNetworking.HostSessionId = WebworksJson.Str(session, "id");
				return "";
			}, "remote play host");
		}

		public static void StopHosting()
		{
			if (string.IsNullOrEmpty(SteamNetworking.HostSessionId)) return;
			var body = "{\"sessionId\":" + WebworksJson.Quote(SteamNetworking.HostSessionId) + "}";
			ApiClient.Fire(() => ApiClient.PostAsync("/RemotePlay/Stop", body), "remote play stop");
			SteamNetworking.HostSessionId = null;
			_sessions.Clear();
		}

		static void Poll()
		{
			if (_polling || Time.realtimeSinceStartup - _lastPoll < 2f) return;
			_lastPoll = Time.realtimeSinceStartup;
			_polling = true;
			ApiClient.Fire(async () =>
			{
				try
				{
					var payload = WebworksJson.Obj(await ApiClient.GetAsync("/RemotePlay/Sessions"));
					var entries = payload != null ? WebworksJson.Arr(payload, "sessions") : null;
					if (entries == null) return "";

					_sessions.Clear();
					foreach (var raw in entries)
					{
						var entry = raw as Dictionary<string, object>;
						if (entry == null) continue;
						ulong host;
						ulong.TryParse(WebworksJson.Str(entry, "hostSteamId") ?? "0", out host);
						_sessions.Add(new RemotePlaySession
						{
							Id = (uint)(_sessions.Count + 1),
							SteamId = host,
							ClientName = WebworksJson.Str(entry, "hostName") ?? "",
							FormFactor = SteamDeviceFormFactor.Computer,
						});
					}
					return "";
				}
				finally
				{
					_polling = false;
				}
			}, "remote play poll");
		}
	}

	public static class SteamVideo
	{
		public static event Action OnBroadcastStarted;
		public static event Action<BroadcastUploadResult> OnBroadcastStopped;

		static bool _broadcasting;
		static int _viewers;
		static string _url = "";
		static float _lastPoll = -999f;
		static bool _polling;

		public static bool IsBroadcasting { get { Poll(); return _broadcasting; } }
		public static int NumViewers { get { Poll(); return _viewers; } }
		public static string BroadcastUrl { get { return _url; } }

		public static void StartBroadcast(string url, string title)
		{
			var body = "{\"url\":" + WebworksJson.Quote(url ?? "") + ",\"title\":" + WebworksJson.Quote(title ?? "") + "}";
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/Video/Broadcast/Start", body));
				if (payload == null) return "";
				var broadcast = WebworksJson.Map(payload, "broadcast");
				if (broadcast == null) return "";
				_broadcasting = WebworksJson.Bool(broadcast, "broadcasting");
				_viewers = WebworksJson.Int(broadcast, "viewers");
				_url = WebworksJson.Str(broadcast, "url") ?? "";
				OnBroadcastStarted?.Invoke();
				return "";
			}, "start broadcast");
		}

		public static void StopBroadcast()
		{
			ApiClient.Fire(() => ApiClient.PostAsync("/Video/Broadcast/Stop", "{}"), "stop broadcast");
			_broadcasting = false;
			_viewers = 0;
			OnBroadcastStopped?.Invoke(BroadcastUploadResult.OK);
		}

		public static void StartWatching(SteamId broadcaster, bool watch = true)
		{
			var body = "{\"steamId\":" + WebworksJson.Quote(broadcaster.Value.ToString(CultureInfo.InvariantCulture)) + "}";
			ApiClient.Fire(() => ApiClient.PostAsync(watch ? "/Video/Watch" : "/Video/Unwatch", body), "watch broadcast");
		}

		static void Poll()
		{
			if (_polling || Time.realtimeSinceStartup - _lastPoll < 2f) return;
			_lastPoll = Time.realtimeSinceStartup;
			_polling = true;
			ApiClient.Fire(async () =>
			{
				try
				{
					var payload = WebworksJson.Obj(await ApiClient.GetAsync("/Video/Status"));
					if (payload != null)
					{
						_broadcasting = WebworksJson.Bool(payload, "broadcasting");
						_viewers = WebworksJson.Int(payload, "viewers");
						_url = WebworksJson.Str(payload, "url") ?? "";
					}
					return "";
				}
				finally
				{
					_polling = false;
				}
			}, "video status");
		}
	}

	public static class SteamScreenshots
	{
		public static event Action<Result> OnScreenshotFailed;
		public static event Action<Screenshot> OnScreenshotReady;
		public static event Action OnScreenshotRequested;

		public static bool Hooked { get; set; }
		static readonly Dictionary<ulong, string> _locations = new Dictionary<ulong, string>();
		static ulong _nextHandle = 1;

		static void Upload(byte[] png, int width, int height, ulong handle, string location)
		{
			var body = new StringBuilder("{");
			body.Append("\"data\":").Append(WebworksJson.Quote(Convert.ToBase64String(png)));
			body.Append(",\"width\":").Append(width);
			body.Append(",\"height\":").Append(height);
			body.Append(",\"location\":").Append(WebworksJson.Quote(location ?? ""));
			body.Append(",\"caption\":\"\"}");

			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/Screenshots/Add", body.ToString()));
				var ok = payload != null && WebworksJson.Str(payload, "error") == null;
				if (ok) OnScreenshotReady?.Invoke(new Screenshot { Handle = handle });
				else OnScreenshotFailed?.Invoke(Result.Fail);
				return "";
			}, "screenshot upload");
		}

		public static void TriggerScreenshot()
		{
			try
			{
				var texture = ScreenCapture.CaptureScreenshotAsTexture();
				var png = texture.EncodeToPNG();
				var handle = _nextHandle++;
				Upload(png, texture.width, texture.height, handle, null);
				UnityEngine.Object.Destroy(texture);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Webworks] screenshot failed: " + ex.Message);
				OnScreenshotFailed?.Invoke(Result.Fail);
			}
		}

		public static Screenshot? WriteScreenshot(byte[] data, int width, int height)
		{
			if (data == null || width <= 0 || height <= 0) return null;
			try
			{
				var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
				texture.LoadRawTextureData(data);
				texture.Apply();
				var png = texture.EncodeToPNG();
				UnityEngine.Object.Destroy(texture);

				var handle = _nextHandle++;
				Upload(png, width, height, handle, null);
				return new Screenshot { Handle = handle };
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Webworks] WriteScreenshot failed: " + ex.Message);
				return null;
			}
		}

		public static Screenshot? AddScreenshot(string filename, string thumbnail, int width, int height)
		{
			try
			{
				var bytes = File.ReadAllBytes(filename);
				var handle = _nextHandle++;
				Upload(bytes, width, height, handle, Path.GetFileName(filename));
				return new Screenshot { Handle = handle };
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Webworks] AddScreenshot failed: " + ex.Message);
				return null;
			}
		}

		internal static bool SetLocation(Screenshot screenshot, string location)
		{
			_locations[screenshot.Handle] = location ?? "";
			return true;
		}

		internal static bool TagUser(Screenshot screenshot, SteamId user) { return true; }
		internal static bool TagPublishedFile(Screenshot screenshot, PublishedFileId file) { return true; }
	}
}
