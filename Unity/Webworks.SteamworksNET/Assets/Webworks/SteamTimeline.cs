using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Webworks.SNetInternal;

namespace Steamworks
{
	// Timeline events are kept locally and mirrored to the server, which is what the
	// Steam overlay timeline would read back.
	public static partial class SteamTimeline
	{
		class Event
		{
			public ulong Handle;
			public string Title;
			public string Description;
			public string Icon;
			public uint Priority;
			public float StartOffset;
			public float Duration;
			public bool Range;
			public bool Open;
		}

		static readonly Dictionary<ulong, Event> _events = new Dictionary<ulong, Event>();
		static ulong _nextHandle = 1;
		static ETimelineGameMode _mode = ETimelineGameMode.k_ETimelineGameMode_Playing;
		static string _phaseId = "";
		static bool _phaseOpen;
		static string _tooltip = "";
		static readonly List<string[]> _tags = new List<string[]>();

		static ulong Store(Event e)
		{
			e.Handle = _nextHandle++;
			_events[e.Handle] = e;
			Send("event", e);
			return e.Handle;
		}

		static void Send(string type, Event e)
		{
			if (!SessionState.Initialized || e == null) return;
			var body = new StringBuilder("{")
				.Append("\"type\":").Append(WebworksJson.Quote(type))
				.Append(",\"description\":").Append(WebworksJson.Quote(string.IsNullOrEmpty(e.Description) ? e.Title : e.Description))
				.Append(",\"icon\":").Append(WebworksJson.Quote(e.Icon ?? ""))
				.Append(",\"priority\":").Append(Math.Min(10u, e.Priority))
				.Append(",\"time\":").Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
				.Append('}');
			ApiClient.Fire(() => ApiClient.PostAsync("/Timeline/Event", body.ToString()), "timeline event");
		}

		public static void SetTimelineTooltip(string pchDescription, float flTimeDelta)
		{
			_tooltip = pchDescription ?? "";
			Send("tooltip", new Event { Title = _tooltip, StartOffset = flTimeDelta });
		}

		public static void ClearTimelineTooltip(float flTimeDelta)
		{
			_tooltip = "";
			Send("tooltip_clear", new Event { StartOffset = flTimeDelta });
		}

		public static void SetTimelineGameMode(ETimelineGameMode eMode)
		{
			_mode = eMode;
			Send("mode", new Event { Title = eMode.ToString(), Description = eMode.ToString() });
		}

		internal static ETimelineGameMode GameMode { get { return _mode; } }

		public static TimelineEventHandle_t AddInstantaneousTimelineEvent(string pchTitle, string pchDescription, string pchIcon, uint unIconPriority, float flStartOffsetSeconds, ETimelineEventClipPriority ePossibleClip)
		{
			return new TimelineEventHandle_t(Store(new Event
			{
				Title = pchTitle ?? "",
				Description = pchDescription ?? "",
				Icon = pchIcon ?? "",
				Priority = unIconPriority,
				StartOffset = flStartOffsetSeconds,
				Range = false,
			}));
		}

		public static TimelineEventHandle_t AddRangeTimelineEvent(string pchTitle, string pchDescription, string pchIcon, uint unIconPriority, float flStartOffsetSeconds, float flDuration, ETimelineEventClipPriority ePossibleClip)
		{
			return new TimelineEventHandle_t(Store(new Event
			{
				Title = pchTitle ?? "",
				Description = pchDescription ?? "",
				Icon = pchIcon ?? "",
				Priority = unIconPriority,
				StartOffset = flStartOffsetSeconds,
				Duration = flDuration,
				Range = true,
			}));
		}

		public static TimelineEventHandle_t StartRangeTimelineEvent(string pchTitle, string pchDescription, string pchIcon, uint unPriority, float flStartOffsetSeconds, ETimelineEventClipPriority ePossibleClip)
		{
			var handle = Store(new Event
			{
				Title = pchTitle ?? "",
				Description = pchDescription ?? "",
				Icon = pchIcon ?? "",
				Priority = unPriority,
				StartOffset = flStartOffsetSeconds,
				Range = true,
				Open = true,
			});
			return new TimelineEventHandle_t(handle);
		}

		public static void UpdateRangeTimelineEvent(TimelineEventHandle_t ulEvent, string pchTitle, string pchDescription, string pchIcon, uint unPriority, ETimelineEventClipPriority ePossibleClip)
		{
			Event e;
			if (!_events.TryGetValue(ulEvent.m_TimelineEventHandle, out e)) return;
			e.Title = pchTitle ?? e.Title;
			e.Description = pchDescription ?? e.Description;
			e.Icon = pchIcon ?? e.Icon;
			e.Priority = unPriority;
			Send("event_update", e);
		}

		public static void EndRangeTimelineEvent(TimelineEventHandle_t ulEvent, float flEndOffsetSeconds)
		{
			Event e;
			if (!_events.TryGetValue(ulEvent.m_TimelineEventHandle, out e)) return;
			e.Duration = flEndOffsetSeconds - e.StartOffset;
			e.Open = false;
			Send("event_end", e);
		}

		public static void RemoveTimelineEvent(TimelineEventHandle_t ulEvent)
		{
			if (_events.Remove(ulEvent.m_TimelineEventHandle))
				Send("event_remove", new Event { Title = "removed", Description = "removed" });
		}

		public static SteamAPICall_t DoesEventRecordingExist(TimelineEventHandle_t ulEvent)
		{
			var call = ResultBus.NewCall();
			var exists = _events.ContainsKey(ulEvent.m_TimelineEventHandle);
			ResultBus.Complete(call, new SteamTimelineEventRecordingExists_t
			{
				m_ulEventID = ulEvent.m_TimelineEventHandle,
				m_bRecordingExists = exists,
			});
			return call;
		}

		public static void StartGamePhase()
		{
			_phaseOpen = true;
			Send("phase_start", new Event { Title = _phaseId, Description = _phaseId });
		}

		public static void EndGamePhase()
		{
			_phaseOpen = false;
			Send("phase_end", new Event { Title = _phaseId, Description = _phaseId });
		}

		public static void SetGamePhaseID(string pchPhaseID)
		{
			_phaseId = pchPhaseID ?? "";
			Send("phase_id", new Event { Title = _phaseId, Description = _phaseId });
		}

		public static SteamAPICall_t DoesGamePhaseRecordingExist(string pchPhaseID)
		{
			var call = ResultBus.NewCall();
			ResultBus.Complete(call, new SteamTimelineGamePhaseRecordingExists_t
			{
				m_rgchPhaseID = pchPhaseID ?? "",
				m_ulRecordingMS = _phaseOpen && _phaseId == (pchPhaseID ?? "") ? 1UL : 0UL,
			});
			return call;
		}

		public static void AddGamePhaseTag(string pchTagName, string pchTagIcon, string pchTagGroup, uint unPriority)
		{
			_tags.Add(new[] { pchTagName ?? "", pchTagIcon ?? "", pchTagGroup ?? "", unPriority.ToString(CultureInfo.InvariantCulture) });
			Send("phase_tag", new Event { Title = pchTagName ?? "", Description = pchTagGroup ?? "", Icon = pchTagIcon ?? "", Priority = unPriority });
		}

		public static void SetGamePhaseAttribute(string pchAttributeGroup, string pchAttributeValue, uint unPriority)
		{
			Send("phase_attribute", new Event { Title = pchAttributeValue ?? "", Description = pchAttributeGroup ?? "", Priority = unPriority });
		}

		public static void OpenOverlayToGamePhase(string pchPhaseID)
		{
			SteamFriends.ActivateGameOverlayToWebPage("steam://timeline/" + Uri.EscapeDataString(pchPhaseID ?? ""));
		}

		public static void OpenOverlayToTimelineEvent(TimelineEventHandle_t ulEvent)
		{
			SteamFriends.ActivateGameOverlayToWebPage("steam://timeline/event/" + ulEvent.m_TimelineEventHandle);
		}

		internal static void Reset()
		{
			_events.Clear();
			_tags.Clear();
			_phaseId = "";
			_phaseOpen = false;
			_tooltip = "";
		}
	}
}
