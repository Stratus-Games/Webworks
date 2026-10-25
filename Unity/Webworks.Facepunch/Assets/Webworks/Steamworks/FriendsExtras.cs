#pragma warning disable CS0067
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Steamworks.Data;
using Webworks.Internal;

namespace Steamworks
{
	public static partial class SteamClient
	{
		public static FriendState State { get { return SessionState.Initialized ? FriendState.Online : FriendState.Offline; } }
	}

	public static partial class SteamFriends
	{
		public static event Action<Friend, string, string> OnChatMessage;

		static readonly Dictionary<string, string> _myRichPresence = new Dictionary<string, string>();

		internal static void RememberRichPresence(string key, string value) { if (key != null) _myRichPresence[key] = value; }
		internal static void ForgetRichPresence() { _myRichPresence.Clear(); }

		public static string GetRichPresence(string key)
		{
			string v;
			return key != null && _myRichPresence.TryGetValue(key, out v) ? v : null;
		}

		public static bool ListenForFriendsMessages(bool intercept = true) { return true; }
		public static bool RequestUserInformation(SteamId steamid, bool nameonly = false) { return false; }
		public static void SetPlayedWith(SteamId steamid) { }

		public static IEnumerable<Friend> GetFriendsRequested() { return new Friend[0]; }
		public static IEnumerable<Friend> GetFriendsRequestingFriendship() { return new Friend[0]; }
		public static IEnumerable<Friend> GetFriendsClanMembers() { return new Friend[0]; }
		public static IEnumerable<Friend> GetFriendsClanMembers(SteamId clan) { return new Friend[0]; }
		public static IEnumerable<Friend> GetFriendsOnGameServer(SteamId server) { return new Friend[0]; }
		public static IEnumerable<Friend> GetFromSource(SteamId source) { return new Friend[0]; }

		public static Task<int> GetFollowerCount(SteamId steamid) { return Task.FromResult(0); }
		public static Task<SteamId[]> GetFollowingList() { return Task.FromResult(new SteamId[0]); }
		public static Task<bool> IsFollowing(SteamId steamid) { return Task.FromResult(false); }

		internal static bool SendFriendMessage(SteamId to, string message)
		{
			if (SteamClient.Realtime == null || string.IsNullOrEmpty(message)) return false;
			SteamClient.Realtime.SendAsync("{\"type\":\"FriendMessage\",\"to\":" + WebworksJson.Quote(to.ToString()) + ",\"message\":" + WebworksJson.Quote(message) + "}").Forget("SendMessage");
			return true;
		}

		internal static void DispatchFriendMessage(string json)
		{
			var d = WebworksJson.Obj(json);
			if (d == null) return;
			ulong sender = WebworksJson.Ulong(d, "from");
			PlayerDirectory.Set(sender, WebworksJson.Str(d, "fromName"));
			Callbacks.RaiseNow(OnChatMessage, new Friend(sender), "chat", WebworksJson.Str(d, "message"));
		}
	}

	public partial struct Friend
	{
		public bool IsBlocked { get { return false; } }
		public bool IsSnoozing { get { return false; } }
		public FriendState State { get { return IsMe ? FriendState.Online : FriendDirectory.StateOf(Id.Value); } }
		public Relationship Relationship { get { return IsFriend ? Steamworks.Relationship.Friend : Steamworks.Relationship.None; } }
		public int SteamLevel { get { return 1; } }
		public IEnumerable<string> NameHistory { get { return new[] { Name }; } }

		public bool GetAchievement(string name) { return IsMe && SteamUserStats.IsUnlocked(name); }
		public DateTime? GetAchievementUnlockTime(string name) { return IsMe ? SteamUserStats.AchUnlockTime(name) : null; }
		public int GetStatInt(string name) { return IsMe ? SteamUserStats.GetStatInt(name) : 0; }
		public float GetStatFloat(string name) { return IsMe ? SteamUserStats.GetStatFloat(name) : 0f; }

		public bool InviteToGame(string connectString) { return false; }
		public bool IsIn(SteamId group_or_room) { return false; }
		public Task<bool> RequestInfoAsync() { return Task.FromResult(true); }
		public Task<bool> RequestUserStatsAsync() { return Task.FromResult(true); }

		public bool SendMessage(string message) { return SteamFriends.SendFriendMessage(Id, message); }
	}

	public static partial class SteamUser
	{
		public static event Action OnSteamServersConnected;
		public static event Action<Result, bool> OnSteamServerConnectFailure;
		public static event Action<Result> OnSteamServersDisconnected;
		public static event Action OnLicensesUpdated;
		public static event Action<SteamId, SteamId, AuthResponse> OnValidateAuthTicketResponse;
		public static event Action<AppId, ulong, bool> OnMicroTxnAuthorizationResponse;
		public static event Action<string> OnGameWebCallback;
		public static event Action<DurationControl> OnDurationControl;
		public static event Action<uint, uint, ushort, uint> OnClientGameServerDeny;

		static uint _sampleRate = 48000;

		public static bool HasVoiceData { get { return false; } }
		public static bool IsBehindNAT { get { return false; } }
		public static bool IsPhoneIdentifying { get { return false; } }
		public static bool IsPhoneRequiringVerification { get { return false; } }
		public static bool IsPhoneVerified { get { return false; } }
		public static bool IsTwoFactorEnabled { get { return false; } }
		public static int SteamLevel { get { return 1; } }
		public static uint OptimalSampleRate { get { return 48000; } }
		public static uint SampleRate { get { return _sampleRate; } set { _sampleRate = value; } }
		public static bool VoiceRecord { get; set; }

		public static int ReadVoiceData(Stream stream) { return 0; }
		public static byte[] ReadVoiceDataBytes() { return new byte[0]; }
		public static int DecompressVoice(Stream input, int length, Stream output) { return 0; }
		public static int DecompressVoice(byte[] compressed, Stream output) { return 0; }

		public static Task<DurationControl?> GetDurationControl() { return Task.FromResult<DurationControl?>(null); }
		public static Task<string> GetStoreAuthUrlAsync(string redirectUrl) { return Task.FromResult(redirectUrl); }

		public static async Task<byte[]> RequestEncryptedAppTicketAsync(byte[] dataToInclude = null)
		{
			var ticket = await GetAuthSessionTicketAsync();
			return ticket.Data;
		}

		internal static void RaiseValidateAuthTicketResponse(SteamId user, AuthResponse response)
		{
			Callbacks.Raise(OnValidateAuthTicketResponse, user, user, response);
		}
	}
}

namespace Steamworks.Data
{
	public struct DurationControl
	{
		public AppId Appid;
		public bool Applicable;
		public int SecondsInLastFiveHours;
		public DurationControlProgress Progress;
	}
}
