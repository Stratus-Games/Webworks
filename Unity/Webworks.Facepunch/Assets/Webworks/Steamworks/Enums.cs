namespace Steamworks
{
	public enum FriendState { Offline = 0, Online = 1, Busy = 2, Away = 3, Snooze = 4, LookingToTrade = 5, LookingToPlay = 6, Invisible = 7, Max = 8 }
	public enum Relationship { None = 0, Blocked = 1, RequestRecipient = 2, Friend = 3, RequestInitiator = 4, Ignored = 5, IgnoredFriend = 6, Suggested = 7, Max = 8 }
	public enum Universe { Invalid = 0, Public = 1, Beta = 2, Internal = 3, Dev = 4, Max = 5 }

	public enum AuthResponse
	{
		OK = 0, UserNotConnectedToSteam = 1, NoLicenseOrExpired = 2, VACBanned = 3, LoggedInElseWhere = 4, VACCheckTimedOut = 5,
		AuthTicketCanceled = 6, AuthTicketInvalidAlreadyUsed = 7, AuthTicketInvalid = 8, PublisherIssuedBan = 9
	}

	public enum BeginAuthResult { OK = 0, InvalidTicket = 1, DuplicateRequest = 2, InvalidVersion = 3, GameMismatch = 4, ExpiredTicket = 5 }
	public enum UserHasLicenseForAppResult { HasLicense = 0, DoesNotHaveLicense = 1, NoAuth = 2 }
	public enum DurationControlProgress { Full = 0, Half = 1, None = 2, ExitSoon_3h = 3, ExitSoon_5h = 4, ExitSoon_Night = 5 }

	public enum NotificationPosition { TopLeft = 0, TopRight = 1, BottomLeft = 2, BottomRight = 3 }
	public enum GamepadTextInputMode { Normal = 0, Password = 1 }
	public enum GamepadTextInputLineMode { SingleLine = 0, MultipleLines = 1 }
	public enum CheckFileSignature { InvalidSignature = 0, ValidSignature = 1, FileNotFound = 2, NoSignaturesFoundForThisApp = 3, NoSignaturesFoundForThisFile = 4 }

	public enum MusicStatus { Undefined = 0, Playing = 1, Paused = 2, Idle = 3 }
	public enum ParentalFeature { Invalid = 0, Store = 1, Community = 2, Profile = 3, Friends = 4, News = 5, Trading = 6, Settings = 7, Console = 8, Browser = 9, ParentalSetup = 10, Library = 11, Test = 12, Max = 13 }
	public enum SteamDeviceFormFactor { Unknown = 0, Phone = 1, Tablet = 2, Computer = 3, TV = 4 }
	public enum BroadcastUploadResult { None = 0, OK = 1, InitFailed = 2, FrameFailed = 3, Timeout = 4, BandwidthExceeded = 5, LowFPS = 6, MissingKeyFrames = 7, NoConnection = 8, RelayFailed = 9, SettingsChanged = 10, MissingAudio = 11, TooFarBehind = 12, TranscodeBehind = 13, NotAllowedToPlay = 14, Busy = 15, Banned = 16, AlreadyActive = 17, ForcedOff = 18, AudioBehind = 19, Shutdown = 20, Disconnect = 21, VideoInitFailed = 22, AudioInitFailed = 23 }

	public enum UgcType { All = -1, Items = 0, ItemsMtx = 1, ItemsReadyToUse = 2, Collections = 3, Artwork = 4, Videos = 5, Screenshots = 6, AllGuides = 7, WebGuides = 8, IntegratedGuides = 9, UsableInGame = 10, ControllerBindings = 11, GameManagedItems = 12 }

	public enum InputType { Unknown = 0, SteamController = 1, XBox360Controller = 2, XBoxOneController = 3, GenericGamepad = 4, PS4Controller = 5, AppleMFiController = 6, AndroidController = 7, SwitchJoyConPair = 8, SwitchJoyConSingle = 9, SwitchProController = 10, MobileTouch = 11, PS3Controller = 12, PS5Controller = 13, SteamDeckController = 14 }
	public enum InputSourceMode { None = 0, Dpad = 1, Buttons = 2, FourButtons = 3, AbsoluteMouse = 4, RelativeMouse = 5, JoystickMove = 6, JoystickMouse = 7, JoystickCamera = 8, ScrollWheel = 9, Trigger = 10, TouchMenu = 11, MouseJoystick = 12, MouseRegion = 13, RadialMenu = 14, SingleButton = 15, Switches = 16 }

	public enum ConnectionState { None = 0, Connecting = 1, FindingRoute = 2, Connected = 3, ClosedByPeer = 4, ProblemDetectedLocally = 5, FinWait = -1, Linger = -2, Dead = -3 }
	public enum NetDebugOutput { None = 0, Bug = 1, Error = 2, Important = 3, Warning = 4, Msg = 5, Verbose = 6, Debug = 7, Everything = 8 }
	public enum SteamNetworkingAvailability { CannotTry = -102, Failed = -101, Previously = -100, Retrying = -10, NeverTried = 1, Waiting = 2, Attempting = 3, Current = 100, Unknown = 0 }

	public enum NetConnectionEnd
	{
		Invalid = 0, App_Generic = 1000, AppException_Generic = 2000, Local_Generic = 3000,
		Remote_Generic = 4000, Remote_Timeout = 4001, Misc_Generic = 5001, Misc_InternalError = 5002, Misc_Timeout = 5003,
		Misc_SteamConnectivity = 5005, Misc_NoRelaySessionsToClient = 5006, Misc_P2P_Rendezvous = 5008, Misc_P2P_NAT_Firewall = 5009, Misc_PeerSentNoConnection = 5010
	}

	public enum CallbackType { Unknown = 0 }
}

namespace Steamworks.Data
{
	public enum SendType { Unreliable = 0, NoNagle = 1, NoDelay = 4, Reliable = 8 }
}
