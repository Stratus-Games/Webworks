using System;
using System.Text;
using System.Runtime.InteropServices;

namespace Steamworks
{
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct DlcInstalled_t
	{
		public const int k_iCallback = Constants.k_iSteamAppsCallbacks + 5;
		public AppId_t m_nAppID;		// AppID of the DLC

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 1)]
	public struct NewUrlLaunchParameters_t
	{
		public const int k_iCallback = Constants.k_iSteamAppsCallbacks + 14;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct AppProofOfPurchaseKeyResponse_t
	{
		public const int k_iCallback = Constants.k_iSteamAppsCallbacks + 21;
		public EResult m_eResult;
		public uint m_nAppID;
		public uint m_cchKeyLength;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_cubAppProofOfPurchaseKeyMax)]
		private byte[] m_rgchKey_;
		public string m_rgchKey
		{
		get { return InteropHelp.ByteArrayToStringUTF8(m_rgchKey_); }
		set { InteropHelp.StringToByteArrayUTF8(value, m_rgchKey_, Constants.k_cubAppProofOfPurchaseKeyMax); }
		}

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct TimedTrialStatus_t
	{
		public const int k_iCallback = Constants.k_iSteamAppsCallbacks + 30;
		public AppId_t m_unAppID;			// appID
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bIsOffline;		// if true, time allowed / played refers to offline time, not total time
		public uint m_unSecondsAllowed;	// how many seconds the app can be played in total
		public uint m_unSecondsPlayed;	// how many seconds the app was already played

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct AvatarImageLoaded_t
	{
		public const int k_iCallback = Constants.k_iSteamFriendsCallbacks + 34;
		public CSteamID m_steamID; // steamid the avatar has been loaded for
		public int m_iImage; // the image index of the now loaded image
		public int m_iWide; // width of the loaded image
		public int m_iTall; // height of the loaded image

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct ClanOfficerListResponse_t
	{
		public const int k_iCallback = Constants.k_iSteamFriendsCallbacks + 35;
		public CSteamID m_steamIDClan;
		public int m_cOfficers;
		public byte m_bSuccess;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct FriendRichPresenceUpdate_t
	{
		public const int k_iCallback = Constants.k_iSteamFriendsCallbacks + 36;
		public CSteamID m_steamIDFriend;	// friend who's rich presence has changed
		public AppId_t m_nAppID;			// the appID of the game (should always be the current game)

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GameConnectedClanChatMsg_t
	{
		public const int k_iCallback = Constants.k_iSteamFriendsCallbacks + 38;
		public CSteamID m_steamIDClanChat;
		public CSteamID m_steamIDUser;
		public int m_iMessageID;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GameConnectedChatJoin_t
	{
		public const int k_iCallback = Constants.k_iSteamFriendsCallbacks + 39;
		public CSteamID m_steamIDClanChat;
		public CSteamID m_steamIDUser;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct GameConnectedChatLeave_t
	{
		public const int k_iCallback = Constants.k_iSteamFriendsCallbacks + 40;
		public CSteamID m_steamIDClanChat;
		public CSteamID m_steamIDUser;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bKicked;		// true if admin kicked
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bDropped;	// true if Steam connection dropped

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct DownloadClanActivityCountsResult_t
	{
		public const int k_iCallback = Constants.k_iSteamFriendsCallbacks + 41;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bSuccess;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct JoinClanChatRoomCompletionResult_t
	{
		public const int k_iCallback = Constants.k_iSteamFriendsCallbacks + 42;
		public CSteamID m_steamIDClanChat;
		public EChatRoomEnterResponse m_eChatRoomEnterResponse;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GameConnectedFriendChatMsg_t
	{
		public const int k_iCallback = Constants.k_iSteamFriendsCallbacks + 43;
		public CSteamID m_steamIDUser;
		public int m_iMessageID;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct FriendsGetFollowerCount_t
	{
		public const int k_iCallback = Constants.k_iSteamFriendsCallbacks + 44;
		public EResult m_eResult;
		public CSteamID m_steamID;
		public int m_nCount;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct FriendsIsFollowing_t
	{
		public const int k_iCallback = Constants.k_iSteamFriendsCallbacks + 45;
		public EResult m_eResult;
		public CSteamID m_steamID;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bIsFollowing;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct FriendsEnumerateFollowingList_t
	{
		public const int k_iCallback = Constants.k_iSteamFriendsCallbacks + 46;
		public EResult m_eResult;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_cEnumerateFollowersMax)]
		public CSteamID[] m_rgSteamID;
		public int m_nResultsReturned;
		public int m_nTotalResultCount;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 1)]
	public struct UnreadChatMessagesChanged_t
	{
		public const int k_iCallback = Constants.k_iSteamFriendsCallbacks + 48;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct OverlayBrowserProtocolNavigation_t
	{
		public const int k_iCallback = Constants.k_iSteamFriendsCallbacks + 49;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
		private byte[] rgchURI_;
		public string rgchURI
		{
		get { return InteropHelp.ByteArrayToStringUTF8(rgchURI_); }
		set { InteropHelp.StringToByteArrayUTF8(value, rgchURI_, 1024); }
		}

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct EquippedProfileItemsChanged_t
	{
		public const int k_iCallback = Constants.k_iSteamFriendsCallbacks + 50;
		public CSteamID m_steamID;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct EquippedProfileItems_t
	{
		public const int k_iCallback = Constants.k_iSteamFriendsCallbacks + 51;
		public EResult m_eResult;
		public CSteamID m_steamID;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bHasAnimatedAvatar;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bHasAvatarFrame;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bHasProfileModifier;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bHasProfileBackground;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bHasMiniProfileBackground;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bFromCache;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GCMessageAvailable_t
	{
		public const int k_iCallback = Constants.k_iSteamGameCoordinatorCallbacks + 1;
		public uint m_nMessageSize;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 1)]
	public struct GCMessageFailed_t
	{
		public const int k_iCallback = Constants.k_iSteamGameCoordinatorCallbacks + 2;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GSClientApprove_t
	{
		public const int k_iCallback = Constants.k_iSteamGameServerCallbacks + 1;
		public CSteamID m_SteamID;			// SteamID of approved player
		public CSteamID m_OwnerSteamID;	// SteamID of original owner for game license

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GSClientDeny_t
	{
		public const int k_iCallback = Constants.k_iSteamGameServerCallbacks + 2;
		public CSteamID m_SteamID;
		public EDenyReason m_eDenyReason;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
		private byte[] m_rgchOptionalText_;
		public string m_rgchOptionalText
		{
		get { return InteropHelp.ByteArrayToStringUTF8(m_rgchOptionalText_); }
		set { InteropHelp.StringToByteArrayUTF8(value, m_rgchOptionalText_, 128); }
		}

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GSClientKick_t
	{
		public const int k_iCallback = Constants.k_iSteamGameServerCallbacks + 3;
		public CSteamID m_SteamID;
		public EDenyReason m_eDenyReason;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GSClientAchievementStatus_t
	{
		public const int k_iCallback = Constants.k_iSteamGameServerCallbacks + 6;
		public ulong m_SteamID;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
		private byte[] m_pchAchievement_;
		public string m_pchAchievement
		{
		get { return InteropHelp.ByteArrayToStringUTF8(m_pchAchievement_); }
		set { InteropHelp.StringToByteArrayUTF8(value, m_pchAchievement_, 128); }
		}
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bUnlocked;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GSPolicyResponse_t
	{
		public const int k_iCallback = Constants.k_iSteamUserCallbacks + 15;
		public byte m_bSecure;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GSGameplayStats_t
	{
		public const int k_iCallback = Constants.k_iSteamGameServerCallbacks + 7;
		public EResult m_eResult;					// Result of the call
		public int m_nRank;					// Overall rank of the server (0-based)
		public uint m_unTotalConnects;			// Total number of clients who have ever connected to the server
		public uint m_unTotalMinutesPlayed;		// Total number of minutes ever played on the server

	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct GSClientGroupStatus_t
	{
		public const int k_iCallback = Constants.k_iSteamGameServerCallbacks + 8;
		public CSteamID m_SteamIDUser;
		public CSteamID m_SteamIDGroup;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bMember;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bOfficer;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GSReputation_t
	{
		public const int k_iCallback = Constants.k_iSteamGameServerCallbacks + 9;
		public EResult m_eResult;				// Result of the call;
		public uint m_unReputationScore;	// The reputation score for the game server
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bBanned;				// True if the server is banned from the Steam
		// master servers

		// The following members are only filled out if m_bBanned is true. They will all
		// be set to zero otherwise. Master server bans are by IP so it is possible to be
		// banned even when the score is good high if there is a bad server on another port.
		// This information can be used to determine which server is bad.

		public uint m_unBannedIP;		// The IP of the banned server
		public ushort m_usBannedPort;		// The port of the banned server
		public ulong m_ulBannedGameID;	// The game ID the banned server is serving
		public uint m_unBanExpires;		// Time the ban expires, expressed in the Unix epoch (seconds since 1/1/1970)

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct AssociateWithClanResult_t
	{
		public const int k_iCallback = Constants.k_iSteamGameServerCallbacks + 10;
		public EResult m_eResult;				// Result of the call;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct ComputeNewPlayerCompatibilityResult_t
	{
		public const int k_iCallback = Constants.k_iSteamGameServerCallbacks + 11;
		public EResult m_eResult;				// Result of the call;
		public int m_cPlayersThatDontLikeCandidate;
		public int m_cPlayersThatCandidateDoesntLike;
		public int m_cClanPlayersThatDontLikeCandidate;
		public CSteamID m_SteamIDCandidate;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GSStatsReceived_t
	{
		public const int k_iCallback = Constants.k_iSteamGameServerStatsCallbacks;
		public EResult m_eResult;		// Success / error fetching the stats
		public CSteamID m_steamIDUser;	// The user for whom the stats are retrieved for

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GSStatsStored_t
	{
		public const int k_iCallback = Constants.k_iSteamGameServerStatsCallbacks + 1;
		public EResult m_eResult;		// success / error
		public CSteamID m_steamIDUser;	// The user for whom the stats were stored

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GSStatsUnloaded_t
	{
		public const int k_iCallback = Constants.k_iSteamUserStatsCallbacks + 8;
		public CSteamID m_steamIDUser;	// User whose stats have been unloaded

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_NeedsPaint_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 2;
		public HHTMLBrowser unBrowserHandle; // the browser that needs the paint
		public IntPtr pBGRA; // a pointer to the B8G8R8A8 data for this surface, valid until SteamAPI_RunCallbacks is next called
		public uint unWide; // the total width of the pBGRA texture
		public uint unTall; // the total height of the pBGRA texture
		public uint unUpdateX; // the offset in X for the damage rect for this update
		public uint unUpdateY; // the offset in Y for the damage rect for this update
		public uint unUpdateWide; // the width of the damage rect for this update
		public uint unUpdateTall; // the height of the damage rect for this update
		public uint unScrollX; // the page scroll the browser was at when this texture was rendered
		public uint unScrollY; // the page scroll the browser was at when this texture was rendered
		public float flPageScale; // the page scale factor on this page when rendered
		public uint unPageSerial; // incremented on each new page load, you can use this to reject draws while navigating to new pages

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_CloseBrowser_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 4;
		public HHTMLBrowser unBrowserHandle; // the handle of the surface

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_OpenLinkInNewTab_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 7;
		public HHTMLBrowser unBrowserHandle; // the handle of the surface
		public string pchURL; //

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_ChangedTitle_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 8;
		public HHTMLBrowser unBrowserHandle; // the handle of the surface
		public string pchTitle; //

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_SearchResults_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 9;
		public HHTMLBrowser unBrowserHandle; // the handle of the surface
		public uint unResults; //
		public uint unCurrentMatch; //

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_HorizontalScroll_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 11;
		public HHTMLBrowser unBrowserHandle; // the handle of the surface
		public uint unScrollMax; //
		public uint unScrollCurrent; //
		public float flPageScale; //
		[MarshalAs(UnmanagedType.I1)]
		public bool bVisible; //
		public uint unPageSize; //

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_VerticalScroll_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 12;
		public HHTMLBrowser unBrowserHandle; // the handle of the surface
		public uint unScrollMax; //
		public uint unScrollCurrent; //
		public float flPageScale; //
		[MarshalAs(UnmanagedType.I1)]
		public bool bVisible; //
		public uint unPageSize; //

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_JSAlert_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 14;
		public HHTMLBrowser unBrowserHandle; // the handle of the surface
		public string pchMessage; //

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_JSConfirm_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 15;
		public HHTMLBrowser unBrowserHandle; // the handle of the surface
		public string pchMessage; //

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_FileOpenDialog_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 16;
		public HHTMLBrowser unBrowserHandle; // the handle of the surface
		public string pchTitle; //
		public string pchInitialFile; //

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_NewWindow_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 21;
		public HHTMLBrowser unBrowserHandle; // the handle of the current surface
		public string pchURL; // the page to load
		public uint unX; // the x pos into the page to display the popup
		public uint unY; // the y pos into the page to display the popup
		public uint unWide; // the total width of the pBGRA texture
		public uint unTall; // the total height of the pBGRA texture
		public HHTMLBrowser unNewWindow_BrowserHandle_IGNORE;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_SetCursor_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 22;
		public HHTMLBrowser unBrowserHandle; // the handle of the surface
		public uint eMouseCursor; // the EHTMLMouseCursor to display

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_StatusText_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 23;
		public HHTMLBrowser unBrowserHandle; // the handle of the surface
		public string pchMsg; // the message text

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_ShowToolTip_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 24;
		public HHTMLBrowser unBrowserHandle; // the handle of the surface
		public string pchMsg; // the tooltip text

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_UpdateToolTip_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 25;
		public HHTMLBrowser unBrowserHandle; // the handle of the surface
		public string pchMsg; // the new tooltip text

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_HideToolTip_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 26;
		public HHTMLBrowser unBrowserHandle; // the handle of the surface

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTML_BrowserRestarted_t
	{
		public const int k_iCallback = Constants.k_iSteamHTMLSurfaceCallbacks + 27;
		public HHTMLBrowser unBrowserHandle; // this is the new browser handle after the restart
		public HHTMLBrowser unOldBrowserHandle; // the handle for the browser before the restart, if your handle was this then switch to using unBrowserHandle for API calls

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTTPRequestCompleted_t
	{
		public const int k_iCallback = Constants.k_iSteamHTTPCallbacks + 1;

		// Handle value for the request that has completed.
		public HTTPRequestHandle m_hRequest;

		// Context value that the user defined on the request that this callback is associated with, 0 if
		// no context value was set.
		public ulong m_ulContextValue;

		// This will be true if we actually got any sort of response from the server (even an error).
		// It will be false if we failed due to an internal error or client side network failure.
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bRequestSuccessful;

		// Will be the HTTP status code value returned by the server, k_EHTTPStatusCode200OK is the normal
		// OK response, if you get something else you probably need to treat it as a failure.
		public EHTTPStatusCode m_eStatusCode;

		public uint m_unBodySize; // Same as GetHTTPResponseBodySize()

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTTPRequestHeadersReceived_t
	{
		public const int k_iCallback = Constants.k_iSteamHTTPCallbacks + 2;

		// Handle value for the request that has received headers.
		public HTTPRequestHandle m_hRequest;

		// Context value that the user defined on the request that this callback is associated with, 0 if
		// no context value was set.
		public ulong m_ulContextValue;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct HTTPRequestDataReceived_t
	{
		public const int k_iCallback = Constants.k_iSteamHTTPCallbacks + 3;

		// Handle value for the request that has received data.
		public HTTPRequestHandle m_hRequest;

		// Context value that the user defined on the request that this callback is associated with, 0 if
		// no context value was set.
		public ulong m_ulContextValue;

		// Offset to provide to GetHTTPStreamingResponseBodyData to get this chunk of data
		public uint m_cOffset;

		// Size to provide to GetHTTPStreamingResponseBodyData to get this chunk of data
		public uint m_cBytesReceived;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SteamInputDeviceConnected_t
	{
		public const int k_iCallback = Constants.k_iSteamControllerCallbacks + 1;
		public InputHandle_t m_ulConnectedDeviceHandle;	// Handle for device

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SteamInputDeviceDisconnected_t
	{
		public const int k_iCallback = Constants.k_iSteamControllerCallbacks + 2;
		public InputHandle_t m_ulDisconnectedDeviceHandle;	// Handle for device

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SteamInputConfigurationLoaded_t
	{
		public const int k_iCallback = Constants.k_iSteamControllerCallbacks + 3;
		public AppId_t m_unAppID;
		public InputHandle_t m_ulDeviceHandle;		// Handle for device
		public CSteamID m_ulMappingCreator;		// May differ from local user when using
		// an unmodified community or official config
		public uint m_unMajorRevision;		// Binding revision from In-game Action File.
		// Same value as queried by GetDeviceBindingRevision
		public uint m_unMinorRevision;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bUsesSteamInputAPI;	// Does the configuration contain any Analog/Digital actions?
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bUsesGamepadAPI;		// Does the configuration contain any Xinput bindings?

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SteamInputGamepadSlotChange_t
	{
		public const int k_iCallback = Constants.k_iSteamControllerCallbacks + 4;
		public AppId_t m_unAppID;
		public InputHandle_t m_ulDeviceHandle;		// Handle for device
		public ESteamInputType m_eDeviceType;			// Type of device
		public int m_nOldGamepadSlot;		// Previous GamepadSlot - can be -1 controller doesn't uses gamepad bindings
		public int m_nNewGamepadSlot;		// New Gamepad Slot - can be -1 controller doesn't uses gamepad bindings

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SteamInventoryEligiblePromoItemDefIDs_t
	{
		public const int k_iCallback = Constants.k_iSteamInventoryCallbacks + 3;
		public EResult m_result;
		public CSteamID m_steamID;
		public int m_numEligiblePromoItemDefs;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bCachedData;	// indicates that the data was retrieved from the cache and not the server

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SteamInventoryStartPurchaseResult_t
	{
		public const int k_iCallback = Constants.k_iSteamInventoryCallbacks + 4;
		public EResult m_result;
		public ulong m_ulOrderID;
		public ulong m_ulTransID;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SteamInventoryRequestPricesResult_t
	{
		public const int k_iCallback = Constants.k_iSteamInventoryCallbacks + 5;
		public EResult m_result;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		private byte[] m_rgchCurrency_;
		public string m_rgchCurrency
		{
		get { return InteropHelp.ByteArrayToStringUTF8(m_rgchCurrency_); }
		set { InteropHelp.StringToByteArrayUTF8(value, m_rgchCurrency_, 4); }
		}

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct FavoritesListChanged_t
	{
		public const int k_iCallback = Constants.k_iSteamMatchmakingCallbacks + 2;
		public uint m_nIP; // an IP of 0 means reload the whole list, any other value means just one server
		public uint m_nQueryPort;
		public uint m_nConnPort;
		public uint m_nAppID;
		public uint m_nFlags;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bAdd; // true if this is adding the entry, otherwise it is a remove
		public AccountID_t m_unAccountId;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct LobbyKicked_t
	{
		public const int k_iCallback = Constants.k_iSteamMatchmakingCallbacks + 12;
		public ulong m_ulSteamIDLobby;			// Lobby
		public ulong m_ulSteamIDAdmin;			// User who kicked you - possibly the ID of the lobby itself
		public byte m_bKickedDueToDisconnect;		// true if you were kicked from the lobby due to the user losing connection to Steam (currently always true)

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct FavoritesListAccountsUpdated_t
	{
		public const int k_iCallback = Constants.k_iSteamMatchmakingCallbacks + 16;

		public EResult m_eResult;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct ReservationNotificationCallback_t
	{
		public const int k_iCallback = Constants.k_iSteamPartiesCallbacks + 3;

		public PartyBeaconID_t m_ulBeaconID;
		public CSteamID m_steamIDJoiner;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct ChangeNumOpenSlotsCallback_t
	{
		public const int k_iCallback = Constants.k_iSteamPartiesCallbacks + 4;

		public EResult m_eResult;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 1)]
	public struct AvailableBeaconLocationsUpdated_t
	{
		public const int k_iCallback = Constants.k_iSteamPartiesCallbacks + 5;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 1)]
	public struct ActiveBeaconsUpdated_t
	{
		public const int k_iCallback = Constants.k_iSteamPartiesCallbacks + 6;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 1)]
	public struct PlaybackStatusHasChanged_t
	{
		public const int k_iCallback = Constants.k_iSteamMusicCallbacks + 1;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct VolumeHasChanged_t
	{
		public const int k_iCallback = Constants.k_iSteamMusicCallbacks + 2;
		public float m_flNewVolume;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SocketStatusCallback_t
	{
		public const int k_iCallback = Constants.k_iSteamNetworkingCallbacks + 1;
		public SNetSocket_t m_hSocket;				// the socket used to send/receive data to the remote host
		public SNetListenSocket_t m_hListenSocket;	// this is the server socket that we were listening on; NULL if this was an outgoing connection
		public CSteamID m_steamIDRemote;			// remote steamID we have connected to, if it has one
		public int m_eSNetSocketState;				// socket state, ESNetSocketState

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SteamNetworkingMessagesSessionRequest_t
	{
		public const int k_iCallback = Constants.k_iSteamNetworkingMessagesCallbacks + 1;
		public SteamNetworkingIdentity m_identityRemote;			// user who wants to talk to us

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SteamNetworkingMessagesSessionFailed_t
	{
		public const int k_iCallback = Constants.k_iSteamNetworkingMessagesCallbacks + 2;

		/// Detailed info about the session that failed.
		/// SteamNetConnectionInfo_t::m_identityRemote indicates who this session
		/// was with.
		public SteamNetConnectionInfo_t m_info;

	}

	public struct SteamParentalSettingsChanged_t
	{
		public const int k_iCallback = Constants.k_ISteamParentalSettingsCallbacks + 1;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SteamRemotePlaySessionConnected_t
	{
		public const int k_iCallback = Constants.k_iSteamRemotePlayCallbacks + 1;
		public RemotePlaySessionID_t m_unSessionID;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SteamRemotePlaySessionDisconnected_t
	{
		public const int k_iCallback = Constants.k_iSteamRemotePlayCallbacks + 2;
		public RemotePlaySessionID_t m_unSessionID;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SteamRemotePlayTogetherGuestInvite_t
	{
		public const int k_iCallback = Constants.k_iSteamRemotePlayCallbacks + 3;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
		private byte[] m_szConnectURL_;
		public string m_szConnectURL
		{
		get { return InteropHelp.ByteArrayToStringUTF8(m_szConnectURL_); }
		set { InteropHelp.StringToByteArrayUTF8(value, m_szConnectURL_, 1024); }
		}

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SteamRemotePlaySessionAvatarLoaded_t
	{
		public const int k_iCallback = Constants.k_iSteamRemotePlayCallbacks + 4;
		public RemotePlaySessionID_t m_unSessionID; // sessionID the avatar has been loaded for
		public int m_iImage; // the image index of the now loaded image
		public int m_iWide; // width of the loaded image
		public int m_iTall; // height of the loaded image

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStorageFileShareResult_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 7;
		public EResult m_eResult;			// The result of the operation
		public UGCHandle_t m_hFile;		// The handle that can be shared with users and features
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_cchFilenameMax)]
		private byte[] m_rgchFilename_;
		public string m_rgchFilename // The name of the file that was shared
		{
		get { return InteropHelp.ByteArrayToStringUTF8(m_rgchFilename_); }
		set { InteropHelp.StringToByteArrayUTF8(value, m_rgchFilename_, Constants.k_cchFilenameMax); }
		}

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStoragePublishFileResult_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 9;
		public EResult m_eResult;				// The result of the operation.
		public PublishedFileId_t m_nPublishedFileId;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bUserNeedsToAcceptWorkshopLegalAgreement;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStorageDeletePublishedFileResult_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 11;
		public EResult m_eResult;				// The result of the operation.
		public PublishedFileId_t m_nPublishedFileId;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStorageEnumerateUserPublishedFilesResult_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 12;
		public EResult m_eResult;				// The result of the operation.
		public int m_nResultsReturned;
		public int m_nTotalResultCount;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_unEnumeratePublishedFilesMaxResults)]
		public PublishedFileId_t[] m_rgPublishedFileId;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStorageEnumerateUserSubscribedFilesResult_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 14;
		public EResult m_eResult;				// The result of the operation.
		public int m_nResultsReturned;
		public int m_nTotalResultCount;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_unEnumeratePublishedFilesMaxResults)]
		public PublishedFileId_t[] m_rgPublishedFileId;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_unEnumeratePublishedFilesMaxResults)]
		public uint[] m_rgRTimeSubscribed;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStorageUpdatePublishedFileResult_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 16;
		public EResult m_eResult;				// The result of the operation.
		public PublishedFileId_t m_nPublishedFileId;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bUserNeedsToAcceptWorkshopLegalAgreement;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStorageDownloadUGCResult_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 17;
		public EResult m_eResult;				// The result of the operation.
		public UGCHandle_t m_hFile;			// The handle to the file that was attempted to be downloaded.
		public AppId_t m_nAppID;				// ID of the app that created this file.
		public int m_nSizeInBytes;			// The size of the file that was downloaded, in bytes.
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_cchFilenameMax)]
		private byte[] m_pchFileName_;
		public string m_pchFileName		// The name of the file that was downloaded.
		{
		get { return InteropHelp.ByteArrayToStringUTF8(m_pchFileName_); }
		set { InteropHelp.StringToByteArrayUTF8(value, m_pchFileName_, Constants.k_cchFilenameMax); }
		}
		public ulong m_ulSteamIDOwner;		// Steam ID of the user who created this content.

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStorageGetPublishedFileDetailsResult_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 18;
		public EResult m_eResult;				// The result of the operation.
		public PublishedFileId_t m_nPublishedFileId;
		public AppId_t m_nCreatorAppID;		// ID of the app that created this file.
		public AppId_t m_nConsumerAppID;		// ID of the app that will consume this file.
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_cchPublishedDocumentTitleMax)]
		private byte[] m_rgchTitle_;
		public string m_rgchTitle		// title of document
		{
		get { return InteropHelp.ByteArrayToStringUTF8(m_rgchTitle_); }
		set { InteropHelp.StringToByteArrayUTF8(value, m_rgchTitle_, Constants.k_cchPublishedDocumentTitleMax); }
		}
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_cchPublishedDocumentDescriptionMax)]
		private byte[] m_rgchDescription_;
		public string m_rgchDescription	// description of document
		{
		get { return InteropHelp.ByteArrayToStringUTF8(m_rgchDescription_); }
		set { InteropHelp.StringToByteArrayUTF8(value, m_rgchDescription_, Constants.k_cchPublishedDocumentDescriptionMax); }
		}
		public UGCHandle_t m_hFile;			// The handle of the primary file
		public UGCHandle_t m_hPreviewFile;		// The handle of the preview file
		public ulong m_ulSteamIDOwner;		// Steam ID of the user who created this content.
		public uint m_rtimeCreated;			// time when the published file was created
		public uint m_rtimeUpdated;			// time when the published file was last updated
		public ERemoteStoragePublishedFileVisibility m_eVisibility;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bBanned;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_cchTagListMax)]
		private byte[] m_rgchTags_;
		public string m_rgchTags	// comma separated list of all tags associated with this file
		{
		get { return InteropHelp.ByteArrayToStringUTF8(m_rgchTags_); }
		set { InteropHelp.StringToByteArrayUTF8(value, m_rgchTags_, Constants.k_cchTagListMax); }
		}
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bTagsTruncated;			// whether the list of tags was too long to be returned in the provided buffer
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_cchFilenameMax)]
		private byte[] m_pchFileName_;
		public string m_pchFileName		// The name of the primary file
		{
		get { return InteropHelp.ByteArrayToStringUTF8(m_pchFileName_); }
		set { InteropHelp.StringToByteArrayUTF8(value, m_pchFileName_, Constants.k_cchFilenameMax); }
		}
		public int m_nFileSize;				// Size of the primary file
		public int m_nPreviewFileSize;		// Size of the preview file
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_cchPublishedFileURLMax)]
		private byte[] m_rgchURL_;
		public string m_rgchURL	// URL (for a video or a website)
		{
		get { return InteropHelp.ByteArrayToStringUTF8(m_rgchURL_); }
		set { InteropHelp.StringToByteArrayUTF8(value, m_rgchURL_, Constants.k_cchPublishedFileURLMax); }
		}
		public EWorkshopFileType m_eFileType;	// Type of the file
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bAcceptedForUse;			// developer has specifically flagged this item as accepted in the Workshop

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStorageEnumerateWorkshopFilesResult_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 19;
		public EResult m_eResult;
		public int m_nResultsReturned;
		public int m_nTotalResultCount;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_unEnumeratePublishedFilesMaxResults)]
		public PublishedFileId_t[] m_rgPublishedFileId;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_unEnumeratePublishedFilesMaxResults)]
		public float[] m_rgScore;
		public AppId_t m_nAppId;
		public uint m_unStartIndex;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStorageGetPublishedItemVoteDetailsResult_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 20;
		public EResult m_eResult;
		public PublishedFileId_t m_unPublishedFileId;
		public int m_nVotesFor;
		public int m_nVotesAgainst;
		public int m_nReports;
		public float m_fScore;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStoragePublishedFileSubscribed_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 21;
		public PublishedFileId_t m_nPublishedFileId;	// The published file id
		public AppId_t m_nAppID;						// ID of the app that will consume this file.

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStoragePublishedFileUnsubscribed_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 22;
		public PublishedFileId_t m_nPublishedFileId;	// The published file id
		public AppId_t m_nAppID;						// ID of the app that will consume this file.

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStoragePublishedFileDeleted_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 23;
		public PublishedFileId_t m_nPublishedFileId;	// The published file id
		public AppId_t m_nAppID;						// ID of the app that will consume this file.

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStorageUpdateUserPublishedItemVoteResult_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 24;
		public EResult m_eResult;				// The result of the operation.
		public PublishedFileId_t m_nPublishedFileId;	// The published file id

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStorageUserVoteDetails_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 25;
		public EResult m_eResult;				// The result of the operation.
		public PublishedFileId_t m_nPublishedFileId;	// The published file id
		public EWorkshopVote m_eVote;			// what the user voted

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStorageEnumerateUserSharedWorkshopFilesResult_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 26;
		public EResult m_eResult;				// The result of the operation.
		public int m_nResultsReturned;
		public int m_nTotalResultCount;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_unEnumeratePublishedFilesMaxResults)]
		public PublishedFileId_t[] m_rgPublishedFileId;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStorageSetUserPublishedFileActionResult_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 27;
		public EResult m_eResult;				// The result of the operation.
		public PublishedFileId_t m_nPublishedFileId;	// The published file id
		public EWorkshopFileAction m_eAction;	// the action that was attempted

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStorageEnumeratePublishedFilesByUserActionResult_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 28;
		public EResult m_eResult;				// The result of the operation.
		public EWorkshopFileAction m_eAction;	// the action that was filtered on
		public int m_nResultsReturned;
		public int m_nTotalResultCount;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_unEnumeratePublishedFilesMaxResults)]
		public PublishedFileId_t[] m_rgPublishedFileId;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_unEnumeratePublishedFilesMaxResults)]
		public uint[] m_rgRTimeUpdated;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStoragePublishFileProgress_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 29;
		public double m_dPercentFile;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bPreview;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoteStoragePublishedFileUpdated_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 30;
		public PublishedFileId_t m_nPublishedFileId;	// The published file id
		public AppId_t m_nAppID;						// ID of the app that will consume this file.
		public ulong m_ulUnused;						// not used anymore

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 1)]
	public struct RemoteStorageLocalFileChange_t
	{
		public const int k_iCallback = Constants.k_iSteamRemoteStorageCallbacks + 33;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct ScreenshotReady_t
	{
		public const int k_iCallback = Constants.k_iSteamScreenshotsCallbacks + 1;
		public ScreenshotHandle m_hLocal;
		public EResult m_eResult;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 1)]
	public struct ScreenshotRequested_t
	{
		public const int k_iCallback = Constants.k_iSteamScreenshotsCallbacks + 2;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SteamTimelineGamePhaseRecordingExists_t
	{
		public const int k_iCallback = Constants.k_iSteamTimelineCallbacks + 1;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_cchMaxPhaseIDLength)]
		private byte[] m_rgchPhaseID_;
		public string m_rgchPhaseID
		{
		get { return InteropHelp.ByteArrayToStringUTF8(m_rgchPhaseID_); }
		set { InteropHelp.StringToByteArrayUTF8(value, m_rgchPhaseID_, Constants.k_cchMaxPhaseIDLength); }
		}
		public ulong m_ulRecordingMS;
		public ulong m_ulLongestClipMS;
		public uint m_unClipCount;
		public uint m_unScreenshotCount;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SteamTimelineEventRecordingExists_t
	{
		public const int k_iCallback = Constants.k_iSteamTimelineCallbacks + 2;
		public ulong m_ulEventID;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bRecordingExists;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct StartPlaytimeTrackingResult_t
	{
		public const int k_iCallback = Constants.k_iSteamUGCCallbacks + 10;
		public EResult m_eResult;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct StopPlaytimeTrackingResult_t
	{
		public const int k_iCallback = Constants.k_iSteamUGCCallbacks + 11;
		public EResult m_eResult;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct AddUGCDependencyResult_t
	{
		public const int k_iCallback = Constants.k_iSteamUGCCallbacks + 12;
		public EResult m_eResult;
		public PublishedFileId_t m_nPublishedFileId;
		public PublishedFileId_t m_nChildPublishedFileId;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoveUGCDependencyResult_t
	{
		public const int k_iCallback = Constants.k_iSteamUGCCallbacks + 13;
		public EResult m_eResult;
		public PublishedFileId_t m_nPublishedFileId;
		public PublishedFileId_t m_nChildPublishedFileId;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct AddAppDependencyResult_t
	{
		public const int k_iCallback = Constants.k_iSteamUGCCallbacks + 14;
		public EResult m_eResult;
		public PublishedFileId_t m_nPublishedFileId;
		public AppId_t m_nAppID;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct RemoveAppDependencyResult_t
	{
		public const int k_iCallback = Constants.k_iSteamUGCCallbacks + 15;
		public EResult m_eResult;
		public PublishedFileId_t m_nPublishedFileId;
		public AppId_t m_nAppID;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GetAppDependenciesResult_t
	{
		public const int k_iCallback = Constants.k_iSteamUGCCallbacks + 16;
		public EResult m_eResult;
		public PublishedFileId_t m_nPublishedFileId;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public AppId_t[] m_rgAppIDs;
		public uint m_nNumAppDependencies;		// number returned in this struct
		public uint m_nTotalNumAppDependencies;	// total found

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct DeleteItemResult_t
	{
		public const int k_iCallback = Constants.k_iSteamUGCCallbacks + 17;
		public EResult m_eResult;
		public PublishedFileId_t m_nPublishedFileId;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct UserSubscribedItemsListChanged_t
	{
		public const int k_iCallback = Constants.k_iSteamUGCCallbacks + 18;
		public AppId_t m_nAppID;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct WorkshopEULAStatus_t
	{
		public const int k_iCallback = Constants.k_iSteamUGCCallbacks + 20;
		public EResult m_eResult;
		public AppId_t m_nAppID;
		public uint m_unVersion;
		public RTime32 m_rtAction;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bAccepted;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bNeedsAction;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct ClientGameServerDeny_t
	{
		public const int k_iCallback = Constants.k_iSteamUserCallbacks + 13;

		public uint m_uAppID;
		public uint m_unGameServerIP;
		public ushort m_usGameServerPort;
		public ushort m_bSecure;
		public uint m_uReason;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct IPCFailure_t
	{
		public const int k_iCallback = Constants.k_iSteamUserCallbacks + 17;
		public byte m_eFailureType;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 1)]
	public struct LicensesUpdated_t
	{
		public const int k_iCallback = Constants.k_iSteamUserCallbacks + 25;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct MicroTxnAuthorizationResponse_t
	{
		public const int k_iCallback = Constants.k_iSteamUserCallbacks + 52;

		public uint m_unAppID;			// AppID for this microtransaction
		public ulong m_ulOrderID;			// OrderID provided for the microtransaction
		public byte m_bAuthorized;		// if user authorized transaction

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct EncryptedAppTicketResponse_t
	{
		public const int k_iCallback = Constants.k_iSteamUserCallbacks + 54;

		public EResult m_eResult;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GameWebCallback_t
	{
		public const int k_iCallback = Constants.k_iSteamUserCallbacks + 64;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
		private byte[] m_szURL_;
		public string m_szURL
		{
		get { return InteropHelp.ByteArrayToStringUTF8(m_szURL_); }
		set { InteropHelp.StringToByteArrayUTF8(value, m_szURL_, 256); }
		}

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct StoreAuthURLResponse_t
	{
		public const int k_iCallback = Constants.k_iSteamUserCallbacks + 65;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
		private byte[] m_szURL_;
		public string m_szURL
		{
		get { return InteropHelp.ByteArrayToStringUTF8(m_szURL_); }
		set { InteropHelp.StringToByteArrayUTF8(value, m_szURL_, 512); }
		}

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct MarketEligibilityResponse_t
	{
		public const int k_iCallback = Constants.k_iSteamUserCallbacks + 66;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bAllowed;
		public EMarketNotAllowedReasonFlags m_eNotAllowedReason;
		public RTime32 m_rtAllowedAtTime;

		public int m_cdaySteamGuardRequiredDays; // The number of days any user is required to have had Steam Guard before they can use the market
		public int m_cdayNewDeviceCooldown; // The number of days after initial device authorization a user must wait before using the market on that device

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct DurationControl_t
	{
		public const int k_iCallback = Constants.k_iSteamUserCallbacks + 67;

		public EResult m_eResult;								// result of call (always k_EResultOK for asynchronous timer-based notifications)
		public AppId_t m_appid;								// appid generating playtime

		[MarshalAs(UnmanagedType.I1)]
		public bool m_bApplicable;							// is duration control applicable to user + game combination
		public int m_csecsLast5h;							// playtime since most recent 5 hour gap in playtime, only counting up to regulatory limit of playtime, in seconds

		public EDurationControlProgress m_progress;			// recommended progress (either everything is fine, or please exit game)
		public EDurationControlNotification m_notification;	// notification to show, if any (always k_EDurationControlNotification_None for API calls)

		public int m_csecsToday;							// playtime on current calendar day
		public int m_csecsRemaining;						// playtime remaining until the user hits a regulatory limit

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GetTicketForWebApiResponse_t
	{
		public const int k_iCallback = Constants.k_iSteamUserCallbacks + 68;
		public HAuthTicket m_hAuthTicket;
		public EResult m_eResult;
		public int m_cubTicket;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.k_nCubTicketMaxLength)]
		public byte[] m_rgubTicket;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct NumberOfCurrentPlayers_t
	{
		public const int k_iCallback = Constants.k_iSteamUserStatsCallbacks + 7;
		public byte m_bSuccess;			// 1 if the call was successful
		public int m_cPlayers;			// Number of players currently playing

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct UserStatsUnloaded_t
	{
		public const int k_iCallback = Constants.k_iSteamUserStatsCallbacks + 8;
		public CSteamID m_steamIDUser;	// User whose stats have been unloaded

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GlobalAchievementPercentagesReady_t
	{
		public const int k_iCallback = Constants.k_iSteamUserStatsCallbacks + 10;

		public ulong m_nGameID;				// Game this is for
		public EResult m_eResult;				// Result of the operation

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct LeaderboardUGCSet_t
	{
		public const int k_iCallback = Constants.k_iSteamUserStatsCallbacks + 11;
		public EResult m_eResult;				// The result of the operation
		public SteamLeaderboard_t m_hSteamLeaderboard;	// the leaderboard handle that was

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 1)]
	public struct IPCountry_t
	{
		public const int k_iCallback = Constants.k_iSteamUtilsCallbacks + 1;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct LowBatteryPower_t
	{
		public const int k_iCallback = Constants.k_iSteamUtilsCallbacks + 2;
		public byte m_nMinutesBatteryLeft;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct SteamAPICallCompleted_t
	{
		public const int k_iCallback = Constants.k_iSteamUtilsCallbacks + 3;
		public SteamAPICall_t m_hAsyncCall;
		public int m_iCallback;
		public uint m_cubParam;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 1)]
	public struct SteamShutdown_t
	{
		public const int k_iCallback = Constants.k_iSteamUtilsCallbacks + 4;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct CheckFileSignature_t
	{
		public const int k_iCallback = Constants.k_iSteamUtilsCallbacks + 5;
		public ECheckFileSignature m_eCheckFileSignature;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 1)]
	public struct AppResumingFromSuspend_t
	{
		public const int k_iCallback = Constants.k_iSteamUtilsCallbacks + 36;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct FilterTextDictionaryChanged_t
	{
		public const int k_iCallback = Constants.k_iSteamUtilsCallbacks + 39;
		public int m_eLanguage;	// One of ELanguage, or k_LegallyRequiredFiltering

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GetOPFSettingsResult_t
	{
		public const int k_iCallback = Constants.k_iSteamVideoCallbacks + 24;
		public EResult m_eResult;
		public AppId_t m_unVideoAppID;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct BroadcastUploadStart_t
	{
		public const int k_iCallback = Constants.k_iSteamVideoCallbacks + 4;
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bIsRTMP;

	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct BroadcastUploadStop_t
	{
		public const int k_iCallback = Constants.k_iSteamVideoCallbacks + 5;
		public EBroadcastUploadResult m_eResult;

	}
}
