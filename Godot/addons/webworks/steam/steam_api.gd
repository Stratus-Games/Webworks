@tool
extends Node

## The Webworks stand-in for GodotSteam's `Steam` singleton.
##
## Generated from GodotSteam 4.23.2 (doc_classes/Steam.xml); do not edit by hand.
## Regenerate with Tools/generate-godotsteam-api.py.
##
## Every method keeps GodotSteam's name, arguments and defaults, so game code that
## called the Steam extension keeps compiling after the extension is removed. The
## work happens in steam_bridge.gd: this file only forwards calls.

const API_VERSION := "4.23.2"

## Method name -> declared GodotSteam return type, so a call made before the
## bridge is ready still answers with the right empty value.
const RETURN_TYPES := {
	"acceptConnection": "int",
	"acceptP2PSessionWithUser": "bool",
	"acceptSessionWithUser": "bool",
	"activateActionSet": "void",
	"activateActionSetLayer": "void",
	"activateGameOverlay": "void",
	"activateGameOverlayInviteDialog": "void",
	"activateGameOverlayInviteDialogConnectString": "void",
	"activateGameOverlayRemotePlayTogetherInviteDialog": "void",
	"activateGameOverlayToStore": "void",
	"activateGameOverlayToUser": "void",
	"activateGameOverlayToWebPage": "void",
	"addAppDependency": "void",
	"addContentDescriptor": "bool",
	"addDependency": "void",
	"addExcludedTag": "bool",
	"addFavoriteGame": "int",
	"addGamePhaseTag": "void",
	"addHeader": "void",
	"addInstantaneousTimelineEvent": "int",
	"addItemKeyValueTag": "bool",
	"addItemPreviewFile": "bool",
	"addItemPreviewVideo": "bool",
	"addItemToFavorites": "void",
	"addPromoItem": "int",
	"addPromoItems": "int",
	"addRangeTimelineEvent": "int",
	"addRequestLobbyListDistanceFilter": "void",
	"addRequestLobbyListFilterSlotsAvailable": "void",
	"addRequestLobbyListNearValueFilter": "void",
	"addRequestLobbyListNumericalFilter": "void",
	"addRequestLobbyListResultCountFilter": "void",
	"addRequestLobbyListStringFilter": "void",
	"addRequiredKeyValueTag": "bool",
	"addRequiredTag": "bool",
	"addRequiredTagGroup": "bool",
	"addScreenshotToLibrary": "int",
	"addVRScreenshotToLibrary": "int",
	"advertiseGame": "void",
	"allowP2PPacketRelay": "bool",
	"allowStartRequest": "void",
	"attachLeaderboardUGC": "void",
	"beginAsyncRequestFakeIP": "bool",
	"beginAuthSession": "int",
	"beginFileWriteBatch": "bool",
	"cancelAuthTicket": "void",
	"cancelQuery": "void",
	"cancelReservation": "void",
	"cancelServerQuery": "void",
	"changeNumOpenSlots": "void",
	"checkFileSignature": "void",
	"checkPingDataUpToDate": "bool",
	"checkResultSteamID": "bool",
	"clearAchievement": "bool",
	"clearRichPresence": "void",
	"clearTimelineTooltip": "void",
	"closeChannelWithUser": "bool",
	"closeClanChatWindowInSteam": "bool",
	"closeConnection": "bool",
	"closeListenSocket": "bool",
	"closeP2PChannelWithUser": "bool",
	"closeP2PSessionWithUser": "bool",
	"closeSessionWithUser": "bool",
	"configureConnectionLanes": "int",
	"connectByIPAddress": "int",
	"connectP2P": "int",
	"connectToHostedDedicatedServer": "int",
	"consumeItem": "int",
	"convertPingLocationToString": "String",
	"copyToClipboard": "void",
	"createBeacon": "void",
	"createBrowser": "void",
	"createCookieContainer": "int",
	"createFakeUDPPort": "void",
	"createHTTPRequest": "int",
	"createItem": "void",
	"createListenSocketIP": "int",
	"createListenSocketP2P": "int",
	"createListenSocketP2PFakeIP": "int",
	"createLobby": "void",
	"createPollGroup": "int",
	"createQueryAllUGCRequestCursor": "int",
	"createQueryAllUGCRequestPage": "int",
	"createQueryUGCDetailsRequest": "int",
	"createQueryUserUGCRequest": "int",
	"createSocketPair": "Dictionary",
	"deactivateActionSetLayer": "void",
	"deactivateAllActionSetLayers": "void",
	"decompressVoice": "Dictionary",
	"deferHTTPRequest": "bool",
	"deleteItem": "void",
	"deleteLobbyData": "bool",
	"deserializeResult": "int",
	"destroyBeacon": "bool",
	"destroyPollGroup": "bool",
	"destroyResult": "void",
	"disableRemotePlayTogetherDirectInput": "void",
	"dismissFloatingGamepadTextInput": "bool",
	"dismissGamepadTextInput": "bool",
	"doesEventRecordingExist": "void",
	"doesGamePhaseRecordingExist": "void",
	"downloadClanActivityCounts": "void",
	"downloadItem": "bool",
	"downloadLeaderboardEntries": "void",
	"downloadLeaderboardEntriesForUsers": "void",
	"enableActionEventCallbacks": "void",
	"enableDeviceCallbacks": "void",
	"enableRemotePlayTogetherDirectInput": "bool",
	"endAuthSession": "void",
	"endFileWriteBatch": "bool",
	"endGamePhase": "void",
	"endRangeTimelineEvent": "void",
	"enumerateFollowingList": "void",
	"estimatePingTimeBetweenTwoLocations": "int",
	"estimatePingTimeFromLocalHost": "int",
	"exchangeItems": "int",
	"executeJavascript": "void",
	"fileDelete": "bool",
	"fileExists": "bool",
	"fileForget": "bool",
	"filePersisted": "bool",
	"fileRead": "Dictionary",
	"fileReadAsync": "void",
	"fileShare": "void",
	"fileWrite": "bool",
	"fileWriteAsync": "void",
	"fileWriteStreamCancel": "bool",
	"fileWriteStreamClose": "bool",
	"fileWriteStreamOpen": "int",
	"fileWriteStreamWriteChunk": "bool",
	"filterText": "String",
	"find": "void",
	"findLeaderboard": "void",
	"findOrCreateLeaderboard": "void",
	"flushMessagesOnConnection": "int",
	"generateItems": "int",
	"getAPICallFailureReason": "int",
	"getAchievement": "Dictionary",
	"getAchievementAchievedPercent": "Dictionary",
	"getAchievementAndUnlockTime": "Dictionary",
	"getAchievementDisplayAttribute": "String",
	"getAchievementIcon": "int",
	"getAchievementName": "String",
	"getAchievementProgressLimitsFloat": "Dictionary",
	"getAchievementProgressLimitsInt": "Dictionary",
	"getActionOriginFromXboxOrigin": "int",
	"getActionSetHandle": "int",
	"getActiveActionSetLayers": "Array",
	"getAllItems": "int",
	"getAllLobbyData": "Dictionary",
	"getAnalogActionData": "Dictionary",
	"getAnalogActionHandle": "int",
	"getAnalogActionOrigins": "Array",
	"getAppBuildId": "int",
	"getAppDependencies": "void",
	"getAppID": "int",
	"getAppInstallDir": "String",
	"getAppOwner": "int",
	"getAuthSessionTicket": "Dictionary",
	"getAuthTicketForWebApi": "int",
	"getAuthenticationStatus": "int",
	"getAvailableBeaconLocations": "Array",
	"getAvailableGameLanguages": "String",
	"getAvailableP2PPacketSize": "int",
	"getAvailableVoice": "Dictionary",
	"getBeaconByIndex": "int",
	"getBeaconDetails": "Dictionary",
	"getBeaconLocationData": "String",
	"getBetaInfo": "Dictionary",
	"getCachedUGCCount": "int",
	"getCachedUGCHandle": "int",
	"getCertificateRequest": "Dictionary",
	"getChatMemberByIndex": "int",
	"getClanActivityCounts": "Dictionary",
	"getClanByIndex": "int",
	"getClanChatMemberCount": "int",
	"getClanCount": "int",
	"getClanName": "String",
	"getClanOfficerByIndex": "int",
	"getClanOfficerCount": "int",
	"getClanOwner": "int",
	"getClanTag": "String",
	"getConfigValue": "Dictionary",
	"getConfigValueInfo": "Dictionary",
	"getConnectedControllers": "Array",
	"getConnectedUniverse": "int",
	"getConnectionInfo": "Dictionary",
	"getConnectionName": "String",
	"getConnectionRealTimeStatus": "Dictionary",
	"getConnectionUserData": "int",
	"getControllerForGamepadIndex": "int",
	"getCoplayFriend": "int",
	"getCoplayFriendCount": "int",
	"getCurrentActionSet": "int",
	"getCurrentBatteryPower": "int",
	"getCurrentBetaName": "String",
	"getCurrentGameLanguage": "String",
	"getDLCCount": "int",
	"getDLCData": "Array",
	"getDLCDataByIndex": "Dictionary",
	"getDLCDownloadProgress": "Dictionary",
	"getDetailedConnectionStatus": "Dictionary",
	"getDeviceBindingRevision": "Array",
	"getDigitalActionData": "Dictionary",
	"getDigitalActionHandle": "int",
	"getDigitalActionOrigins": "Array",
	"getDirectPingToPOP": "int",
	"getDownloadedItems": "PackedInt64Array",
	"getDurationControl": "void",
	"getEarliestPurchaseUnixTime": "int",
	"getEncryptedAppTicket": "Dictionary",
	"getFakeIP": "Dictionary",
	"getFavoriteGames": "Array",
	"getFileCount": "int",
	"getFileDetails": "void",
	"getFileNameAndSize": "Dictionary",
	"getFileSize": "int",
	"getFileTimestamp": "int",
	"getFollowerCount": "void",
	"getFriendByIndex": "int",
	"getFriendCoplayGame": "int",
	"getFriendCoplayTime": "int",
	"getFriendCount": "int",
	"getFriendCountFromSource": "int",
	"getFriendFromSourceByIndex": "int",
	"getFriendGamePlayed": "Dictionary",
	"getFriendPersonaName": "String",
	"getFriendPersonaNameHistory": "String",
	"getFriendPersonaState": "int",
	"getFriendRelationship": "int",
	"getFriendRichPresence": "String",
	"getFriendRichPresenceKeyByIndex": "String",
	"getFriendRichPresenceKeyCount": "int",
	"getFriendSteamLevel": "int",
	"getFriendsGroupCount": "int",
	"getFriendsGroupIDByIndex": "int",
	"getFriendsGroupMembersCount": "int",
	"getFriendsGroupMembersList": "Array",
	"getFriendsGroupName": "String",
	"getGameBadgeLevel": "int",
	"getGamepadIndexForController": "int",
	"getGlobalStatFloat": "float",
	"getGlobalStatFloatHistory": "PackedFloat64Array",
	"getGlobalStatInt": "int",
	"getGlobalStatIntHistory": "PackedInt64Array",
	"getGlyphForActionOrigin": "String",
	"getGlyphForXboxOrigin": "String",
	"getGlyphPNGForActionOrigin": "String",
	"getGlyphSVGForActionOrigin": "String",
	"getHTTPDownloadProgressPct": "float",
	"getHTTPRequestWasTimedOut": "bool",
	"getHTTPResponseBodyData": "PackedByteArray",
	"getHTTPResponseBodySize": "int",
	"getHTTPResponseHeaderSize": "int",
	"getHTTPResponseHeaderValue": "PackedByteArray",
	"getHTTPStreamingResponseBodyData": "PackedByteArray",
	"getHostedDedicatedServerPOPId": "int",
	"getHostedDedicatedServerPort": "int",
	"getIPCCallCount": "int",
	"getIPCountry": "String",
	"getIPv4FakeIPType": "int",
	"getIPv6ConnectivityState": "int",
	"getImageRGBA": "Dictionary",
	"getImageSize": "Dictionary",
	"getInput": "Array",
	"getInputTypeForHandle": "int",
	"getInstalledDepots": "Array",
	"getItemDefinitionProperty": "Dictionary",
	"getItemDownloadInfo": "Dictionary",
	"getItemInstallInfo": "Dictionary",
	"getItemPrice": "Dictionary",
	"getItemState": "int",
	"getItemUpdateProgress": "Dictionary",
	"getItemsByID": "int",
	"getItemsWithPrices": "Array",
	"getLargeFriendAvatar": "int",
	"getLargeSessionAvatar": "int",
	"getLaunchCommandLine": "String",
	"getLaunchQueryParam": "String",
	"getLeaderboardDisplayType": "int",
	"getLeaderboardEntryCount": "int",
	"getLeaderboardName": "String",
	"getLeaderboardSortMethod": "int",
	"getLinkAtPosition": "void",
	"getListenSocketAddress": "String",
	"getLobbyData": "String",
	"getLobbyGameServer": "Dictionary",
	"getLobbyMemberByIndex": "int",
	"getLobbyMemberData": "String",
	"getLobbyMemberLimit": "int",
	"getLobbyOwner": "int",
	"getLocalFileChange": "Dictionary",
	"getLocalFileChangeCount": "int",
	"getLocalPingLocation": "Dictionary",
	"getLocalTimestamp": "int",
	"getMediumFriendAvatar": "int",
	"getMediumSessionAvatar": "int",
	"getMostAchievedAchievementInfo": "Dictionary",
	"getMotionData": "Dictionary",
	"getNextMostAchievedAchievementInfo": "Dictionary",
	"getNumAchievements": "int",
	"getNumActiveBeacons": "int",
	"getNumAvailableBeaconLocations": "int",
	"getNumBetas": "Dictionary",
	"getNumDownloadedItems": "int",
	"getNumLobbyMembers": "int",
	"getNumSubscribedItems": "int",
	"getNumSupportedGameVersions": "int",
	"getNumberOfCurrentPlayers": "void",
	"getOPFSettings": "void",
	"getOPFStringForApp": "String",
	"getP2PSessionState": "Dictionary",
	"getPOPCount": "int",
	"getPOPList": "Array",
	"getPersonaName": "String",
	"getPersonaState": "int",
	"getPingToDataCenter": "Dictionary",
	"getPlaybackStatus": "int",
	"getPlayerAvatar": "void",
	"getPlayerNickname": "String",
	"getPlayerSteamLevel": "int",
	"getProfileItemPropertyInt": "int",
	"getProfileItemPropertyString": "String",
	"getQueryUGCAdditionalPreview": "Dictionary",
	"getQueryUGCChildren": "Dictionary",
	"getQueryUGCContentDescriptors": "Dictionary",
	"getQueryUGCKeyValueTag": "Dictionary",
	"getQueryUGCMetadata": "String",
	"getQueryUGCNumAdditionalPreviews": "int",
	"getQueryUGCNumKeyValueTags": "int",
	"getQueryUGCNumTags": "int",
	"getQueryUGCPreviewURL": "String",
	"getQueryUGCResult": "Dictionary",
	"getQueryUGCStatistic": "Dictionary",
	"getQueryUGCTag": "String",
	"getQueryUGCTagDisplayName": "String",
	"getQuota": "Dictionary",
	"getRealIdentityForFakeIP": "Dictionary",
	"getRecentPlayers": "Array",
	"getRelayNetworkStatus": "int",
	"getRemoteFakeIPForConnection": "Dictionary",
	"getRemotePlaySessionID": "int",
	"getResultItemProperty": "String",
	"getResultItems": "Array",
	"getResultStatus": "int",
	"getResultTimestamp": "int",
	"getSecondsSinceAppActive": "int",
	"getSecondsSinceComputerActive": "int",
	"getServerCount": "int",
	"getServerDetails": "Dictionary",
	"getServerRealTime": "int",
	"getSessionClientFormFactor": "int",
	"getSessionClientName": "String",
	"getSessionClientResolution": "Dictionary",
	"getSessionConnectionInfo": "Dictionary",
	"getSessionCount": "int",
	"getSessionGuestID": "int",
	"getSessionID": "int",
	"getSessionInputConfigurationSettings": "int",
	"getSessionSteamID": "int",
	"getSmallFriendAvatar": "int",
	"getSmallSessionAvatar": "int",
	"getStatFloat": "float",
	"getStatInt": "int",
	"getSteamHardwareDefaultConfig": "int",
	"getSteamID": "int",
	"getSteamID32": "int",
	"getSteamUILanguage": "String",
	"getStringForActionOrigin": "String",
	"getStringForAnalogActionName": "String",
	"getStringForDigitalActionName": "String",
	"getStringForXboxOrigin": "String",
	"getSubscribedItems": "Array",
	"getSupportedGameVersionData": "Dictionary",
	"getSyncPlatforms": "int",
	"getUGCDetails": "Dictionary",
	"getUGCDownloadProgress": "Dictionary",
	"getUserAchievement": "Dictionary",
	"getUserAchievementAndUnlockTime": "Dictionary",
	"getUserContentDescriptorPreferences": "Array",
	"getUserFriendsGroups": "Array",
	"getUserItemVote": "void",
	"getUserStatFloat": "float",
	"getUserStatInt": "int",
	"getUserSteamFriends": "Array",
	"getUserSteamGroups": "Array",
	"getVideoURL": "void",
	"getVoice": "Dictionary",
	"getVoiceOptimalSampleRate": "int",
	"getWorkshopEULAStatus": "void",
	"get_godotsteam_version": "String",
	"get_server_list_request": "int",
	"get_steam_init_result": "Dictionary",
	"goBack": "void",
	"goForward": "void",
	"grantPromoItems": "int",
	"hasEquippedProfileItem": "bool",
	"hasFriend": "bool",
	"hookScreenshots": "void",
	"htmlInit": "bool",
	"htmlShutdown": "bool",
	"indicateAchievementProgress": "bool",
	"initAuthentication": "int",
	"initFilterText": "bool",
	"initRelayNetworkAccess": "void",
	"initWorkshopForGameServer": "bool",
	"initiateGameConnection": "Dictionary",
	"inputInit": "bool",
	"inputShutdown": "bool",
	"installDLC": "void",
	"inviteUserToGame": "bool",
	"inviteUserToLobby": "bool",
	"isAPICallCompleted": "Dictionary",
	"isAnonAccount": "bool",
	"isAnonUserAccount": "bool",
	"isAppBlocked": "bool",
	"isAppInBlockList": "bool",
	"isAppInstalled": "bool",
	"isBehindNAT": "bool",
	"isBroadcasting": "Dictionary",
	"isChatAccount": "bool",
	"isClanAccount": "bool",
	"isClanChatAdmin": "bool",
	"isClanChatWindowOpenInSteam": "bool",
	"isClanOfficialGameGroup": "bool",
	"isClanPublic": "bool",
	"isCloudEnabledForAccount": "bool",
	"isCloudEnabledForApp": "bool",
	"isConsoleUserAccount": "bool",
	"isCybercafe": "bool",
	"isDLCInstalled": "bool",
	"isFakeIPv4": "bool",
	"isFeatureBlocked": "bool",
	"isFeatureInBlockList": "bool",
	"isFollowing": "void",
	"isIndividualAccount": "bool",
	"isLobby": "bool",
	"isLowViolence": "bool",
	"isOverlayEnabled": "bool",
	"isParentalLockEnabled": "bool",
	"isParentalLockLocked": "bool",
	"isPhoneIdentifying": "bool",
	"isPhoneRequiringVerification": "bool",
	"isPhoneVerified": "bool",
	"isRefreshing": "bool",
	"isRunningOnSteamHardware": "int",
	"isRunningUnderProton": "bool",
	"isScreenshotsHooked": "bool",
	"isSteamChinaLauncher": "bool",
	"isSteamInBigPictureMode": "bool",
	"isSteamRunning": "bool",
	"isSteamRunningInVR": "bool",
	"isSubscribed": "bool",
	"isSubscribedApp": "bool",
	"isSubscribedFromFamilySharing": "bool",
	"isSubscribedFromFreeWeekend": "bool",
	"isTimedTrial": "Dictionary",
	"isTwoFactorEnabled": "bool",
	"isUserInSource": "bool",
	"isVACBanned": "bool",
	"isVRHeadsetStreamingEnabled": "bool",
	"iterateGenericEditableConfigValues": "int",
	"joinClanChatRoom": "void",
	"joinLobby": "void",
	"joinParty": "void",
	"jsDialogResponse": "void",
	"keyChar": "void",
	"keyDown": "void",
	"keyUp": "void",
	"leaveClanChatRoom": "bool",
	"leaveLobby": "void",
	"loadItemDefinitions": "bool",
	"loadURL": "void",
	"loggedOn": "bool",
	"markContentCorrupt": "bool",
	"markDownloadedItemAsUnused": "bool",
	"mouseDoubleClick": "void",
	"mouseDown": "void",
	"mouseMove": "void",
	"mouseUp": "void",
	"mouseWheel": "void",
	"musicGetVolume": "float",
	"musicIsEnabled": "bool",
	"musicIsPlaying": "bool",
	"musicPause": "void",
	"musicPlay": "void",
	"musicPlayNext": "void",
	"musicPlayPrev": "void",
	"musicSetVolume": "void",
	"newDataAvailable": "bool",
	"onReservationCompleted": "void",
	"openClanChatWindowInSteam": "bool",
	"openDeveloperTools": "void",
	"openOverlayToGamePhase": "void",
	"openOverlayToTimelineEvent": "void",
	"overlayNeedsPresent": "bool",
	"parsePingLocationString": "Dictionary",
	"pasteFromClipboard": "void",
	"pingServer": "int",
	"playerDetails": "int",
	"prioritizeHTTPRequest": "bool",
	"readP2PPacket": "Dictionary",
	"receiveMessagesOnChannel": "Array",
	"receiveMessagesOnConnection": "Array",
	"receiveMessagesOnPollGroup": "Array",
	"refreshQuery": "void",
	"refreshServer": "void",
	"registerProtocolInOverlayBrowser": "bool",
	"releaseCookieContainer": "bool",
	"releaseCurrentThreadMemory": "void",
	"releaseHTTPRequest": "bool",
	"releaseQueryUGCRequest": "bool",
	"releaseRequest": "void",
	"reload": "void",
	"removeAllItemKeyValueTags": "bool",
	"removeAppDependency": "void",
	"removeBrowser": "void",
	"removeContentDescriptor": "bool",
	"removeDependency": "void",
	"removeFavoriteGame": "bool",
	"removeItemFromFavorites": "void",
	"removeItemKeyValueTags": "bool",
	"removeItemPreview": "bool",
	"removeProperty": "bool",
	"removeTimelineEvent": "void",
	"replyToFriendMessage": "bool",
	"requestClanOfficerList": "void",
	"requestEligiblePromoItemDefinitionsIDs": "void",
	"requestEncryptedAppTicket": "void",
	"requestEquippedProfileItems": "void",
	"requestFavoritesServerList": "int",
	"requestFriendRichPresence": "void",
	"requestFriendsServerList": "int",
	"requestGlobalAchievementPercentages": "void",
	"requestGlobalStats": "void",
	"requestHistoryServerList": "int",
	"requestInternetServerList": "int",
	"requestLANServerList": "int",
	"requestLobbyData": "bool",
	"requestLobbyList": "void",
	"requestPrices": "void",
	"requestSpectatorServerList": "int",
	"requestStoreAuthURL": "void",
	"requestUserInformation": "bool",
	"requestUserStats": "void",
	"resetAllStats": "bool",
	"resetIdentity": "void",
	"restartAppIfNecessary": "bool",
	"runFrame": "void",
	"runNetworkingCallbacks": "void",
	"run_callbacks": "void",
	"sendClanChatMessage": "bool",
	"sendHTTPRequest": "bool",
	"sendHTTPRequestAndStreamResponse": "bool",
	"sendLobbyChatMsg": "bool",
	"sendMessageToConnection": "Dictionary",
	"sendMessageToUser": "int",
	"sendMessages": "PackedInt64Array",
	"sendP2PPacket": "bool",
	"sendQueryUGCRequest": "void",
	"sendRemotePlayTogetherInvite": "bool",
	"serializeResult": "PackedByteArray",
	"serverFriends": "int",
	"serverRules": "int",
	"sessionRemotePlayTogether": "bool",
	"setAchievement": "bool",
	"setActiveBeta": "bool",
	"setAdminQuery": "bool",
	"setAllowCachedResponse": "bool",
	"setAllowLegacyUpload": "bool",
	"setBackgroundMode": "void",
	"setCertificate": "Dictionary",
	"setCloudEnabledForApp": "void",
	"setCloudFileNameFilter": "bool",
	"setConnectionConfigValueFloat": "bool",
	"setConnectionConfigValueInt32": "bool",
	"setConnectionConfigValueString": "bool",
	"setConnectionName": "void",
	"setConnectionPollGroup": "bool",
	"setConnectionUserData": "bool",
	"setCookie": "void",
	"setDLCContext": "bool",
	"setDPIScalingFactor": "void",
	"setDurationControlOnlineState": "bool",
	"setGameLauncherMode": "void",
	"setGamePerformanceSettings": "void",
	"setGamePhaseAttribute": "void",
	"setGamePhaseID": "void",
	"setGameRenderResolution": "void",
	"setGlobalConfigValueFloat": "bool",
	"setGlobalConfigValueInt32": "bool",
	"setGlobalConfigValueString": "bool",
	"setHTTPCookie": "bool",
	"setHTTPRequestAbsoluteTimeoutMS": "bool",
	"setHTTPRequestContextValue": "bool",
	"setHTTPRequestCookieContainer": "bool",
	"setHTTPRequestGetOrPostParameter": "bool",
	"setHTTPRequestHeaderValue": "bool",
	"setHTTPRequestNetworkActivityTimeout": "bool",
	"setHTTPRequestRawPostBody": "bool",
	"setHTTPRequestRequiresVerifiedCertificate": "bool",
	"setHTTPRequestUserAgentInfo": "bool",
	"setHorizontalScroll": "void",
	"setInGameVoiceSpeaking": "void",
	"setInputActionManifestFilePath": "bool",
	"setItemContent": "bool",
	"setItemDescription": "bool",
	"setItemMetadata": "bool",
	"setItemPreview": "bool",
	"setItemTags": "bool",
	"setItemTitle": "bool",
	"setItemUpdateLanguage": "bool",
	"setItemVisibility": "bool",
	"setItemsDisabledLocally": "bool",
	"setKeyFocus": "void",
	"setLEDColor": "void",
	"setLanguage": "bool",
	"setListenForFriendsMessages": "bool",
	"setLobbyData": "bool",
	"setLobbyGameServer": "void",
	"setLobbyJoinable": "bool",
	"setLobbyMemberData": "void",
	"setLobbyMemberLimit": "bool",
	"setLobbyOwner": "bool",
	"setLobbyType": "bool",
	"setLocation": "bool",
	"setMatchAnyTag": "bool",
	"setMouseCursor": "void",
	"setMousePosition": "void",
	"setMouseVisibility": "void",
	"setOverlayNotificationInset": "void",
	"setOverlayNotificationPosition": "void",
	"setPageScaleFactor": "void",
	"setPlayedWith": "void",
	"setPropertyBool": "bool",
	"setPropertyFloat": "bool",
	"setPropertyInt": "bool",
	"setPropertyString": "bool",
	"setRankedByTrendDays": "bool",
	"setRequiredGameVersions": "bool",
	"setReturnAdditionalPreviews": "bool",
	"setReturnChildren": "bool",
	"setReturnKeyValueTags": "bool",
	"setReturnLongDescription": "bool",
	"setReturnMetadata": "bool",
	"setReturnOnlyIDs": "bool",
	"setReturnPlaytimeStats": "bool",
	"setReturnTotalOnly": "bool",
	"setRichPresence": "bool",
	"setSearchText": "bool",
	"setSize": "void",
	"setStatFloat": "bool",
	"setStatInt": "bool",
	"setSubscriptionsLoadOrder": "bool",
	"setSyncPlatforms": "bool",
	"setTimeCreatedDateRange": "bool",
	"setTimeUpdatedDateRange": "bool",
	"setTimelineGameMode": "void",
	"setTimelineTooltip": "void",
	"setUserItemVote": "void",
	"setVRHeadsetStreamingEnabled": "void",
	"setVerticalScroll": "void",
	"set_server_list_request": "void",
	"showBindingPanel": "bool",
	"showFloatingGamepadTextInput": "bool",
	"showGamepadTextInput": "bool",
	"showRemotePlayTogetherUI": "bool",
	"showWorkshopEULA": "bool",
	"startGamePhase": "void",
	"startItemUpdate": "int",
	"startPlaytimeTracking": "void",
	"startPurchase": "void",
	"startRangeTimelineEvent": "int",
	"startUpdateProperties": "int",
	"startVRDashboard": "void",
	"startVoiceRecording": "void",
	"steamInit": "bool",
	"steamInitEx": "Dictionary",
	"steamShutdown": "void",
	"stopAnalogActionMomentum": "void",
	"stopFind": "void",
	"stopLoad": "void",
	"stopPlaytimeTracking": "void",
	"stopPlaytimeTrackingForAllItems": "void",
	"stopVoiceRecording": "void",
	"storeStats": "bool",
	"submitItemUpdate": "void",
	"submitUpdateProperties": "int",
	"subscribeItem": "void",
	"suspendDownloads": "void",
	"tagPublishedFile": "bool",
	"tagUser": "bool",
	"terminateGameConnection": "void",
	"transferItemQuantity": "int",
	"translateActionOrigin": "int",
	"triggerHapticPulse": "void",
	"triggerItemDrop": "int",
	"triggerRepeatedHapticPulse": "void",
	"triggerScreenshot": "void",
	"triggerSimpleHapticEvent": "void",
	"triggerVibration": "void",
	"triggerVibrationExtended": "void",
	"ugcDownload": "void",
	"ugcDownloadToLocation": "void",
	"ugcRead": "PackedByteArray",
	"uninstallDLC": "void",
	"unsubscribeItem": "void",
	"updateAvgRateStat": "bool",
	"updateItemPreviewFile": "bool",
	"updateItemPreviewVideo": "bool",
	"updateRangeTimelineEvent": "void",
	"uploadLeaderboardScore": "void",
	"userHasLicenseForApp": "int",
	"viewSource": "void",
	"waitForData": "bool",
	"writeScreenshot": "int",
}

## Mirrors GodotSteam's exposed properties.
var current_app_id: int = 0
var current_browser_handle: int = 0
var current_clan_id: int = 0
var current_steam_id: int = 0
var inventory_handle: int = 0
var inventory_update_handle: int = 0
var leaderboard_details_max: int = 64
var leaderboard_entries_array: Array = []
var leaderboard_handle: int = 0
var leaderboard_ugc_handle: int = 0

## GodotSteam's callbacks. The bridge emits these as Webworks answers arrive.
signal active_beacons_updated()
signal add_app_dependency_result(result: int, file_id: int, app_id: int)
signal add_friend_to_list(friend_id: int, friend_name: String, currently_connected: bool)
signal add_ugc_dependency_result(result: int, file_id: int, child_id: int)
signal app_resuming_from_suspend()
signal available_beacon_locations_updated()
signal avatar_image_loaded(avatar_id: int, avatar_index: int, width: int, height: int)
signal avatar_loaded(avatar_id: int, size: int, data: PackedByteArray)
signal broadcast_upload_start(is_rtmp: bool)
signal broadcast_upload_stop(result: int)
signal change_num_open_slots(result: int)
signal change_server_requested(server: String, password: String)
signal check_file_signature(signature: int)
signal clan_activity_downloaded(activity: Dictionary)
signal client_game_server_deny(app_id: int, server_ip: String, server_port: int, secure: int, reason: int)
signal connected_chat_join(chat_id: int, steam_id: int)
signal connected_chat_leave(chat_id: int, steam_id: int, kicked: bool, dropped: bool)
signal connected_clan_chat_message(clan_chat_id: int, message_index: int, message_text: String, type: int, chatter: int)
signal connected_friend_chat_message(steam_id: int, message_index: int, message_text: int, type: int)
signal create_beacon(result: int, beacon_id: int)
signal dlc_installed(app: int)
signal download_ugc_result(result: int, download_data: Dictionary)
signal duration_control(result: int, duration: Dictionary)
signal encrypted_app_ticket_response(result: int)
signal end_game_result(result: int, game_id: int)
signal enumerate_following_list(message: String, following: PackedInt64Array)
signal equipped_profile_items(result: int, steam_id: int, profile_data: Dictionary)
signal equipped_profile_items_changed(steam_id: int)
signal fake_ip_result(result: int, remote_fake_steam_id: int, fake_ip: String, port_list: PackedInt32Array)
signal favorites_list_accounts_updated(result: int)
signal favorites_list_changed(favorite: Dictionary)
signal file_details_result(result: int, file_size: int, file_hash: int, flags: int)
signal file_read_async_complete(file_read: Dictionary)
signal file_share_result(result: int, handle: int, name: String)
signal file_write_async_complete(result: int)
signal filter_text_dictionary_changed(language: int)
signal floating_gamepad_text_input_dismissed()
signal friend_rich_presence_update(steam_id: int, app_id: int)
signal friends_failed_to_respond()
signal friends_refresh_complete()
signal game_web_callback(url: String)
signal gamepad_text_input_dismissed(submitted: bool, entered_text: String, app_id: int)
signal get_app_dependencies_result(result: int, file_id: int, app_dependencies: int, total_app_dependencies: int, app_ids: PackedInt32Array)
signal get_auth_session_ticket_response(auth_ticket: int, result: int)
signal get_follower_count(result: int, steam_id: int, count: int)
signal get_item_vote_result(result: int, file_id: int, vote_up: bool, vote_down: bool, vote_skipped: bool)
signal get_opf_settings_result(result: int, app_id: int)
signal get_ticket_for_web_api(auth_ticket: int, result: int, ticket_size: int, ticket_buffer: PackedByteArray)
signal get_video_result(result: int, app_id: int, url: String)
signal global_achievement_percentages_ready(game_id: int, result: int)
signal global_stats_received(game_id: int, result: int)
signal html_browser_ready(browser_handle: int)
signal html_can_go_backandforward(browser_handle: int, go_back: bool, go_forward: bool)
signal html_changed_title(browser_handle: int, title: String)
signal html_close_browser(browser_handle: int)
signal html_file_open_dialog(browser_handle: int, title: String, initial_file: String)
signal html_finished_request(browser_handle: int, url: String, title: String)
signal html_hide_tooltip(browser_handle: int)
signal html_horizontal_scroll(browser_handle: int, scroll_data: Dictionary)
signal html_js_alert(browser_handle: int, message: String)
signal html_js_confirm(browser_handle: int, message: String)
signal html_link_at_position(browser_handle: int, link_data: Dictionary)
signal html_needs_paint(browser_handle: int, page_data: Dictionary)
signal html_new_window(browser_handle: int, window_data: Dictionary)
signal html_open_link_in_new_tab(browser_handle: int, url: String)
signal html_search_results(browser_handle: int, results: int, current_match: int)
signal html_set_cursor(browser_handle: int, mouse_cursor: int)
signal html_show_tooltip(browser_handle: int, message: String)
signal html_start_request(browser_handle: int, url: String, target: String, post_data: String, redirect: bool)
signal html_status_text(browser_handle: int, message: String)
signal html_update_tooltip(browser_handle: int, message: String)
signal html_url_changed(browser_handle: int, url_data: Dictionary)
signal html_vertical_scroll(browser_handle: int, scroll_data: Dictionary)
signal http_request_completed(request_handle: int, context_value: int, request_success: bool, status_code: int, body_size: int)
signal http_request_data_received(request_handle: int, context_value: int, offset: int, bytes_received: int)
signal http_request_headers_received(request_handle: int, context_value: int)
signal input_action_event(input_handle: int, event_type: int, action_handle: int, is_active: bool, action_data: Dictionary)
signal input_configuration_loaded(app_id: int, device_handle: int, config_data: Dictionary)
signal input_device_connected(input_handle: int)
signal input_device_disconnected(input_handle: int)
signal input_gamepad_slot_change(app_id: int, device_handle: int, device_type: int, old_gamepad_slot: int, new_gamepad_slot: int)
signal inventory_definition_update(definitions: Array)
signal inventory_eligible_promo_item(result: int, cached: bool, definitions: Array)
signal inventory_full_update(inventory_handle: int)
signal inventory_request_prices_result(result: int, currency: String)
signal inventory_result_ready(result: int, inventory_handle: int)
signal inventory_start_purchase_result(result: String, order_id: int, transaction_id: int)
signal ip_country()
signal ipc_failure(type: int)
signal is_following(result: int, steam_id: int, following: bool)
signal item_created(result: int, file_id: int, accept_tos: bool)
signal item_deleted(result: int, file_id: int)
signal item_downloaded(result: int, file_id: int, app_id: int)
signal item_installed(app_id: int, file_id: int)
signal item_updated(result: int, need_to_accept_tos: bool, file_id: int)
signal join_clan_chat_complete(chat_id: int, response: int)
signal join_game_requested(user: int, connect: String)
signal join_party(result: int, beacon_id: int, steam_id: int, connect_string: String)
signal join_requested(lobby_id: int, steam_id: int)
signal leaderboard_find_result(leaderboard_handle: int, found: int)
signal leaderboard_score_uploaded(success: bool, this_handle: int, this_score: Dictionary)
signal leaderboard_scores_downloaded(message: String, leaderboard_handle: int, leaderboard_entries: Array)
signal leaderboard_ugc_set(leaderboard_handle: int, result: int)
signal licenses_updated()
signal lobby_chat_update(lobby_id: int, changed_id: int, making_change_id: int, chat_state: int)
signal lobby_created(connect: int, lobby_id: int)
signal lobby_data_update(success: int, lobby_id: int, member_id: int)
signal lobby_game_created(lobby_id: int, server_id: int, server_ip: String, port: int)
signal lobby_invite(inviter: int, lobby: int, game: int)
signal lobby_joined(lobby: int, permissions: int, locked: bool, response: int)
signal lobby_kicked(lobby_id: int, admin_id: int, due_to_disconnect: int)
signal lobby_match_list(lobbies: Array)
signal lobby_message(lobby_id: int, user: int, message: String, chat_type: int)
signal local_file_changed()
signal low_power(power: int)
signal market_eligibility_response(is_allowed: bool, disallow_reason: int, allowed_at_time: int, steam_guard_required_days: int, new_device_cooldown: int)
signal microtransaction_auth_response(app_id: int, order_id: int, authorized: bool)
signal music_playback_status_has_changed()
signal music_volume_has_changed(new_volume: float)
signal network_authentication_status(available: int, debug_message: String)
signal network_connection_status_changed(connect_handle: int, connection: Dictionary, old_state: int)
signal network_messages_session_failed(reason: int, remote_steam_id: int, connection_state: int, debug_message: String)
signal network_messages_session_request(remote_steam_id: int)
signal new_launch_url_parameters()
signal number_of_current_players(success: int, players: int)
signal overlay_browser_protocol(uri: String)
signal overlay_toggled(active: bool, user_initiated: bool, app_id: int)
signal p2p_session_connect_fail(remote_steam_id: int, session_error: int)
signal p2p_session_request(remote_steam_id: int)
signal parental_setting_changed()
signal persona_state_change(steam_id: int, flags: int)
signal ping_server_failed_to_respond()
signal ping_server_responded(server_details: Dictionary)
signal player_details_failed_to_respond()
signal player_details_player_added(name: String, score: int, time_played: float)
signal player_details_refresh_complete()
signal published_file_subscribed(file_id: int, app_id: int)
signal published_file_unsubscribed(file_id: int, app_id: int)
signal relay_network_status(available: int, ping_measurement: int, available_config: int, available_relay: int, debug_message: String)
signal remote_play_guest_invite(invite_url: String)
signal remote_play_session_avatar_loaded(session_id: int, avatar_index: int, width: int, height: int)
signal remote_play_session_connected(session_id: int)
signal remote_play_session_disconnected(session_id: int)
signal remove_app_dependency_result(result: int, file_id: int, app_id: int)
signal remove_ugc_dependency_result(result: int, file_id: int, child_id: int)
signal request_clan_officer_list(success: bool, officer_list: Array)
signal request_players_for_game_final_result(result: int, search_id: int, game_id: int)
signal request_players_for_game_progress(result: int, search_id: int)
signal request_players_for_game_result(result: int, search_id: int, player_data: Dictionary)
signal request_server_list_refresh_complete(request_handle: int, response: int)
signal request_server_list_server_failed_to_respond(request_handle: int, server: int)
signal request_server_list_server_responded(request_handle: int, server: int)
signal reservation_notification(beacon_id: int, steam_id: int)
signal screenshot_ready(handle: int, result: int)
signal screenshot_requested()
signal search_for_game_progress(result: int, search_id: int, search_progress: Dictionary)
signal search_for_game_result(result: int, search_id: int, search_result: Dictionary)
signal server_rules_failed_to_respond()
signal server_rules_refresh_complete()
signal server_rules_responded(rule: String, value: String)
signal set_user_item_vote(result: int, file_id: int, vote_up: bool)
signal start_playtime_tracking(result: int)
signal steam_api_call_completed(async_call: int, callback: int, parameter: int)
signal steam_server_connect_failed(result: int, retrying: bool)
signal steam_server_connected()
signal steam_server_disconnected(result: int)
signal steam_shutdown()
signal stop_playtime_tracking(result: int)
signal store_auth_url_response(url: String)
signal submit_player_result(result: int, game_id: int, player_id: int)
signal subscribe_item(result: int, file_id: int)
signal timed_trial_status(app_id: int, is_offline: bool, seconds_allowed: int, seconds_played: int)
signal timeline_event_recording_exists(event_id: int, recording_exists: bool)
signal timeline_game_phase_recording_exists(phase_id: String, recording_ms: int, longest_clip_ms: int, clip_count: int, screenshot_count: int)
signal ugc_query_completed(handle: int, result: int, results_returned: int, total_matching: int, cached: bool, next_cursor: String)
signal unread_chat_messages_changed()
signal unsubscribe_item(result: int, file_id: int)
signal user_achievement_icon_fetched(game_id: int, achievement_name: String, was_achieved: bool, icon_handle: int)
signal user_achievement_stored(game_id: int, group_achieve: bool, achievement_name: String, current_progress: int, max_progress: int)
signal user_favorite_items_list_changed(result: int, file_id: int, was_add_request: bool)
signal user_stats_received(game_id: int, result: int, user_id: int)
signal user_stats_stored(game_id: int, result: int)
signal user_stats_unloaded(user_id: int)
signal user_subscribed_items_list_changed(app_id: int)
signal validate_auth_ticket_response(auth_id: int, response: int, owner_id: int)
signal workshop_eula_status(result: int, app_id: int, eula_data: Dictionary)

## GodotSteam's enums, bit flags and sentinel values.
const ACCOUNT_ID_INVALID := 0
const API_CALL_INVALID := 0
const APP_ID_INVALID := 0
const AUTH_TICKET_INVALID := 0
const DEPOT_ID_INVALID := 0
const GAME_EXTRA_INFO_MAX := 64
const QUERY_PORT_ERROR := 65534
const QUERY_PORT_NOT_INITIALIZED := 65535
const STEAM_ACCOUNT_ID_MASK := 4294967295
const STEAM_ACCOUNT_INSTANCE_MASK := 1048575
const STEAM_BUFFER_SIZE := 255
const STEAM_LARGE_BUFFER_SIZE := 8160
const STEAM_MAX_ERROR_MESSAGE := 1024
const STEAM_USER_DEFAULT_INSTANCE := 1
const CHAT_METADATA_MAX := 8192
const ENUMERATED_FOLLOWERS_MAX := 50
const FRIEND_GAME_INFO_QUERY_PORT_ERROR := 65534
const FRIEND_GAME_INFO_QUERY_PORT_NOT_INITIALIZED := 65535
const FRIENDS_GROUP_LIMIT := 100
const INVALID_FRIEND_GROUP_ID := -1
const MAX_FRIENDS_GROUP_NAME := 64
const MAX_RICH_PRESENCE_KEY_LENGTH := 64
const MAX_RICH_PRESENCE_KEYS := 30
const MAX_RICH_PRESENCE_VALUE_LENTH := 256
const PERSONA_NAME_MAX_UTF8 := 128
const PERSONA_NAME_MAX_UTF16 := 32
const INVALID_HTMLBROWSER := 0
const HTTPCOOKIE_INVALID_HANDLE := 0
const HTTPREQUEST_INVALID_HANDLE := 0
const INPUT_HANDLE_ALL_CONTROLLERS := -1
const INPUT_MAX_ACTIVE_LAYERS := 16
const INPUT_MAX_ANALOG_ACTIONS := 24
const INPUT_MAX_ANALOG_ACTION_DATA := 1
const INPUT_MAX_COUNT := 16
const INPUT_MAX_DIGITAL_ACTIONS := 256
const INPUT_MAX_ORIGINS := 8
const INPUT_MIN_ANALOG_ACTION_DATA := -1
const SONY_PAD_TRIGGER_EFFECT_CONTROL_POINT_NUM := 10
const SONY_PAD_TRIGGER_EFFECT_PARAM_INDEX_FOR_R2 := 1
const SONY_PAD_TRIGGER_EFFECT_PARAM_INDEX_FOR_L2 := 0
const SONY_PAD_TRIGGER_EFFECT_TRIGGER_MASK_L2 := 1
const SONY_PAD_TRIGGER_EFFECT_TRIGGER_MASK_R2 := 2
const SONY_PAD_TRIGGER_EFFECT_TRIGGER_NUM := 2
const INVENTORY_RESULT_INVALID := -1
const ITEM_INSTANCE_ID_INVALID := -1
const FAVORITE_FLAG_FAVORITE := 1
const FAVORITE_FLAG_HISTORY := 2
const FAVORITE_FLAG_NONE := 0
const MAX_LOBBY_KEY_LENGTH := 255
const SERVER_QUERY_INVALID := -1
const MAX_GAME_SERVER_GAME_DATA := 2048
const MAX_GAME_SERVER_GAME_DESCRIPTION := 64
const MAX_GAME_SERVER_GAME_DIR := 32
const MAX_GAME_SERVER_MAP_NAME := 32
const MAX_GAME_SERVER_NAME := 64
const MAX_GAME_SERVER_TAGS := 128
const MAX_STEAM_PACKET_SIZE := 524288
const LISTEN_SOCKET_INVALID := 0
const MAX_NETWORKING_ERROR_MESSAGE := 1024
const MAX_NETWORKING_PING_LOCATION_STRING := 1024
const NETWORKING_CONFIG_P2P_TRANSPORT_ICE_DEFAULT := -1
const NETWORKING_CONFIG_P2P_TRANSPORT_ICE_DISABLE := 0
const NETWORKING_CONFIG_P2P_TRANSPORT_ICE_RELAY := 1
const NETWORKING_CONFIG_P2P_TRANSPORT_ICE_PRIVATE := 2
const NETWORKING_CONFIG_P2P_TRANSPORT_ICE_PUBLIC := 4
const NETWORKING_CONFIG_P2P_TRANSPORT_ICE_ALL := 2147483647
const NETWORKING_CONNECTION_INFO_FLAG_UNAUTHENTICATED := 1
const NETWORKING_CONNECTION_INFO_FLAG_UNENCRYPTED := 2
const NETWORKING_CONNECTION_INFO_FLAG_LOOPBACK_BUFFERS := 4
const NETWORKING_CONNECTION_INFO_FLAG_FAST := 8
const NETWORKING_CONNECTION_INFO_FLAG_RELAYED := 16
const NETWORKING_CONNECTION_INFO_FLAG_DUALWIFI := 32
const NETWORKING_CONNECTION_INVALID := 0
const NETWORKING_MAX_CONNECTION_APP_NAME := 32
const NETWORKING_MAX_CONNECTION_CLOSE_REASON := 128
const NETWORKING_MAX_CONNECTION_DESCRIPTION := 128
const NETWORKING_PING_FAILED := -1
const NETWORKING_PING_UNKNOWN := -2
const NETWORKING_SEND_UNRELIABLE := 0
const NETWORKING_SEND_NO_NAGLE := 1
const NETWORKING_SEND_URELIABLE_NO_NAGLE := 1
const NETWORKING_SEND_NO_DELAY := 4
const NETWORKING_SEND_UNRELIABLE_NO_DELAY := 5
const NETWORKING_SEND_RELIABLE := 8
const NETWORKING_SEND_RELIABLE_NO_NAGLE := 9
const NETWORKING_SEND_USE_CURRENT_THREAD := 16
const NETWORKING_SEND_AUTORESTART_BROKEN_SESSION := 32
const ENUMERATE_PUBLISHED_FILES_MAX_RESULTS := 50
const FILE_NAME_MAX := 260
const MAX_CLOUD_FILE_CHUNK_SIZE := 104857600
const PUBLISHED_DOCUMENT_CHANGE_DESCRIPTION_MAX := 8000
const PUBLISHED_DOCUMENT_DESCRIPTION_MAX := 8000
const PUBLISHED_DOCUMENT_TITLE_MAX := 129
const PUBLISHED_FILE_ID_INVALID := 0
const PUBLISHED_FILE_UPDATE_HANDLE_INVALID := -1
const PUBLISHED_FILE_URL_MAX := 256
const TAG_LIST_MAX := 1025
const UGC_FILE_STREAM_HANDLE_INVALID := -1
const UGC_HANDLE_INVALID := -1
const MAX_TAGGED_PUBLISHED_FILES := 32
const MAX_TAGGED_USERS := 32
const SCREENSHOT_INVALID_HANDLE := 0
const SCREENSHOT_THUMB_WIDTH := 200
const UFS_TAG_TYPE_MAX := 255
const UFS_TAG_VALUE_MAX := 255
const DEVELOPER_METADATA_MAX := 10000
const NUM_UGC_RESULTS_PER_PAGE := 50
const UGC_QUERY_HANDLE_INVALID := -1
const UGC_UPDATE_HANDLE_INVALID := -1
const LEADERBOARD_DETAILS_MAX := 64
const LEADERBOARD_NAME_MAX := 128
const STAT_NAME_MAX := 128
const ACCOUNT_TYPE_INVALID := 0
const ACCOUNT_TYPE_INDIVIDUAL := 1
const ACCOUNT_TYPE_MULTISEAT := 2
const ACCOUNT_TYPE_GAME_SERVER := 3
const ACCOUNT_TYPE_ANON_GAME_SERVER := 4
const ACCOUNT_TYPE_PENDING := 5
const ACCOUNT_TYPE_CONTENT_SERVER := 6
const ACCOUNT_TYPE_CLAN := 7
const ACCOUNT_TYPE_CHAT := 8
const ACCOUNT_TYPE_CONSOLE_USER := 9
const ACCOUNT_TYPE_ANON_USER := 10
const ACCOUNT_TYPE_MAX := 11
const STEAM_API_CALL_FAILURE_NONE := -1
const STEAM_API_CALL_FAILURE_STEAM_GONE := 0
const STEAM_API_CALL_FAILURE_NETWORK_FAILURE := 1
const STEAM_API_CALL_FAILURE_INVALID_HANDLE := 2
const STEAM_API_CALL_FAILURE_MISMATCHED_CALLBACK := 3
const AUDIO_PLAYBACK_UNDEFINED := 0
const AUDIO_PLAYBACK_PLAYING := 1
const AUDIO_PLAYBACK_PAUSED := 2
const AUDIO_PLAYBACK_IDLE := 3
const AUTH_SESSION_RESPONSE_OK := 0
const AUTH_SESSION_RESPONSE_USER_NOT_CONNECTED_TO_STEAM := 1
const AUTH_SESSION_RESPONSE_NO_LICENSE_OR_EXPIRED := 2
const AUTH_SESSION_RESPONSE_VAC_BANNED := 3
const AUTH_SESSION_RESPONSE_LOGGED_IN_ELSEWHERE := 4
const AUTH_SESSION_RESPONSE_VAC_CHECK_TIMED_OUT := 5
const AUTH_SESSION_RESPONSE_AUTH_TICKET_CANCELED := 6
const AUTH_SESSION_RESPONSE_AUTH_TICKET_INVALID_ALREADY_USED := 7
const AUTH_SESSION_RESPONSE_AUTH_TICKET_INVALID := 8
const AUTH_SESSION_RESPONSE_PUBLISHER_ISSUED_BAN := 9
const AUTH_SESSION_RESPONSE_AUTH_TICKET_NETWORK_IDENTITY_FAILURE := 10
const AVATAR_SMALL := 1
const AVATAR_MEDIUM := 2
const AVATAR_LARGE := 3
const BEGIN_AUTH_SESSION_RESULT_OK := 0
const BEGIN_AUTH_SESSION_RESULT_INVALID_TICKET := 1
const BEGIN_AUTH_SESSION_RESULT_DUPLICATE_REQUEST := 2
const BEGIN_AUTH_SESSION_RESULT_INVALID_VERSION := 3
const BEGIN_AUTH_SESSION_RESULT_GAME_MISMATCH := 4
const BEGIN_AUTH_SESSION_RESULT_EXPIRED_TICKET := 5
const BETA_BRANCH_NONE := 0
const BETA_BRANCH_DEFAULT := 1
const BETA_BRANCH_AVAILABLE := 2
const BETA_BRANCH_PRIVATE := 4
const BETA_BRANCH_SELECTED := 8
const BETA_BRANCH_INSTALLED := 16
const BROADCAST_UPLOAD_RESULT_NONE := 0
const BROADCAST_UPLOAD_RESULT_OK := 1
const BROADCAST_UPLOAD_RESULT_INIT_FAILED := 2
const BROADCAST_UPLOAD_RESULT_FRAME_FAILED := 3
const BROADCAST_UPLOAD_RESULT_TIME_OUT := 4
const BROADCAST_UPLOAD_RESULT_BANDWIDTH_EXCEEDED := 5
const BROADCAST_UPLOAD_RESULT_LOW_FPS := 6
const BROADCAST_UPLOAD_RESULT_MISSING_KEYFRAMES := 7
const BROADCAST_UPLOAD_RESULT_NO_CONNECTION := 8
const BROADCAST_UPLOAD_RESULT_RELAY_FAILED := 9
const BROADCAST_UPLOAD_RESULT_SETTINGS_CHANGED := 10
const BROADCAST_UPLOAD_RESULT_MISSING_AUDIO := 11
const BROADCAST_UPLOAD_RESULT_TOO_FAR_BEHIND := 12
const BROADCAST_UPLOAD_RESULT_TRANSCODE_BEHIND := 13
const BROADCAST_UPLOAD_RESULT_NOT_ALLOWED_TO_PLAY := 14
const BROADCAST_UPLOAD_RESULT_BUSY := 15
const BROADCAST_UPLOAD_RESULT_BANNED := 16
const BROADCAST_UPLOAD_RESULT_ALREADY_ACTIVE := 17
const BROADCAST_UPLOAD_RESULT_FORCED_OFF := 18
const BROADCAST_UPLOAD_RESULT_AUDIO_BEHIND := 19
const BROADCAST_UPLOAD_RESULT_SHUTDOWN := 20
const BROADCAST_UPLOAD_RESULT_DISCONNECT := 21
const BROADCAST_UPLOAD_RESULT_VIDEO_INIT_FAILED := 22
const BROADCAST_UPLOAD_RESULT_AUDIO_INIT_FAILED := 23
const CHAT_ENTRY_TYPE_INVALID := 0
const CHAT_ENTRY_TYPE_CHAT_MSG := 1
const CHAT_ENTRY_TYPE_TYPING := 2
const CHAT_ENTRY_TYPE_INVITE_GAME := 3
const CHAT_ENTRY_TYPE_EMOTE := 4
const CHAT_ENTRY_TYPE_LEFT_CONVERSATION := 6
const CHAT_ENTRY_TYPE_ENTERED := 7
const CHAT_ENTRY_TYPE_WAS_KICKED := 8
const CHAT_ENTRY_TYPE_WAS_BANNED := 9
const CHAT_ENTRY_TYPE_DISCONNECTED := 10
const CHAT_ENTRY_TYPE_HISTORICAL_CHAT := 11
const CHAT_ENTRY_TYPE_LINK_BLOCKED := 14
const CHAT_MEMBER_STATE_CHANGE_ENTERED := 1
const CHAT_MEMBER_STATE_CHANGE_LEFT := 2
const CHAT_MEMBER_STATE_CHANGE_DISCONNECTED := 4
const CHAT_MEMBER_STATE_CHANGE_KICKED := 8
const CHAT_MEMBER_STATE_CHANGE_BANNED := 16
const CHAT_ROOM_ENTER_RESPONSE_SUCCESS := 1
const CHAT_ROOM_ENTER_RESPONSE_DOESNT_EXIST := 2
const CHAT_ROOM_ENTER_RESPONSE_NOT_ALLOWED := 3
const CHAT_ROOM_ENTER_RESPONSE_FULL := 4
const CHAT_ROOM_ENTER_RESPONSE_ERROR := 5
const CHAT_ROOM_ENTER_RESPONSE_BANNED := 6
const CHAT_ROOM_ENTER_RESPONSE_LIMITED := 7
const CHAT_ROOM_ENTER_RESPONSE_CLAN_DISABLED := 8
const CHAT_ROOM_ENTER_RESPONSE_COMMUNITY_BAN := 9
const CHAT_ROOM_ENTER_RESPONSE_MEMBER_BLOCKED_YOU := 10
const CHAT_ROOM_ENTER_RESPONSE_YOU_BLOCKED_MEMBER := 11
const CHAT_ROOM_ENTER_RESPONSE_RATE_LIMIT_EXCEEDED := 15
const CHAT_ACCOUNT_INSTANCE_MASK := 4095
const CHAT_INSTANCE_FLAG_CLAN := 524288
const CHAT_INSTANCE_FLAG_LOBBY := 262144
const CHAT_INSTANCE_FLAG_MMS_LOBBY := 131072
const CHECK_FILE_SIGNATURE_INVALID_SIGNATURE := 0
const CHECK_FILE_SIGNATURE_VALID_SIGNATURE := 1
const CHECK_FILE_SIGNATURE_FILE_NOT_FOUND := 2
const CHECK_FILE_SIGNATURE_NO_SIGNATURES_FOUND_FOR_THIS_APP := 3
const CHECK_FILE_SIGNATURE_NO_SIGNATURES_FOUND_FOR_THIS_FILE := 4
const PROFILE_ITEM_TYPE_ANIMATED_AVATAR := 0
const PROFILE_ITEM_TYPE_AVATAR_FRAME := 1
const PROFILE_ITEM_TYPE_PROFILE_MODIFIER := 2
const PROFILE_ITEM_TYPE_PROFILE_BACKGROUND := 3
const PROFILE_ITEM_TYPE_MINI_PROFILE_BACKGROUND := 4
const PROFILE_ITEM_PROPERTY_IMAGE_SMALL := 0
const PROFILE_ITEM_PROPERTY_IMAGE_LARGE := 1
const PROFILE_ITEM_PROPERTY_INTERNAL_NAME := 2
const PROFILE_ITEM_PROPERTY_TITLE := 3
const PROFILE_ITEM_PROPERTY_DESCRIPTION := 4
const PROFILE_ITEM_PROPERTY_APP_ID := 5
const PROFILE_ITEM_PROPERTY_TYPE_ID := 6
const PROFILE_ITEM_PROPERTY_CLASS := 7
const PROFILE_ITEM_PROPERTY_MOVIE_WEBM := 8
const PROFILE_ITEM_PROPERTY_MOVIE_MP4 := 9
const PROFILE_ITEM_PROPERTY_MOVIE_WEBM_SMALL := 10
const PROFILE_ITEM_PROPERTY_MOVIE_MP4_SMALL := 11
const CONTROLLER_HAPTIC_LOCATION_LEFT := 1
const CONTROLLER_HAPTIC_LOCATION_RIGHT := 2
const CONTROLLER_HAPTIC_LOCATION_BOTH := 3
const CONTROLLER_HAPTIC_TYPE_OFF := 0
const CONTROLLER_HAPTIC_TYPE_TICK := 1
const CONTROLLER_HAPTIC_TYPE_CLICK := 2
const DENY_INVALID := 0
const DENY_INVALID_VERSION := 1
const DENY_GENERIC := 2
const DENY_NOT_LOGGED_ON := 3
const DENY_NO_LICENSE := 4
const DENY_CHEATER := 5
const DENY_LOGGED_IN_ELSEWHERE := 6
const DENY_UNKNOWN_TEXT := 7
const DENY_INCOMPATIBLE_ANTI_CHEAT := 8
const DENY_MEMORY_CORRUPTION := 9
const DENY_INCOMPATIBLE_SOFTWARE := 10
const DENY_STEAM_CONNECTION_LOST := 11
const DENY_STEAM_CONNECTION_ERROR := 12
const DENY_STEAM_RESPONSE_TIMED_OUT := 13
const DENY_STEAM_VALIDATION_STALLED := 14
const DENY_STEAM_OWNER_LEFT_GUEST_USER := 15
const FORM_FACTOR_UNKNOWN := 0
const FORM_FACTOR_PHONE := 1
const FORM_FACTOR_TABLET := 2
const FORM_FACTOR_COMPUTER := 3
const FORM_FACTOR_TV := 4
const FORM_FACTOR_VR_HEADSET := 5
const DURATION_CONTROL_NOTIFICATION_NONE := 0
const DURATION_CONTROL_NOTIFICATION_1_HOUR := 1
const DURATION_CONTROL_NOTIFICATION_3_HOURS := 2
const DURATION_CONTROL_NOTIFICATION_HALF_PROGRESS := 3
const DURATION_CONTROL_NOTIFICATION_NO_PROGRESS := 4
const DURATION_CONTROL_NOTIFICATION_EXIT_SOON_3H := 5
const DURATION_CONTROL_NOTIFICATION_EXIT_SOON_5H := 6
const DURATION_CONTROL_NOTIFICATION_EXIT_SOON_NIGHT := 7
const DURATION_CONTROL_ONLINE_STATE_INVALID := 0
const DURATION_CONTROL_ONLINE_STATE_OFFLINE := 1
const DURATION_CONTROL_ONLINE_STATE_ONLINE := 2
const DURATION_CONTROL_ONLINE_STATE_ONLINE_HIGH_PRIORITY := 3
const DURATION_CONTROL_PROGRESS_FULL := 0
const DURATION_CONTROL_PROGRESS_HALF := 1
const DURATION_CONTROL_PROGRESS_NONE := 2
const DURATION_CONTROL_EXIT_SOON_3H := 3
const DURATION_CONTROL_EXIT_SOON_5H := 4
const DURATION_CONTROL_EXIT_SOON_NIGHT := 5
const FAILURE_FLUSHED_CALLBACK_QUEUE := 0
const FAILURE_PIPE_FAIL := 1
const FILE_PATH_TYPE_INVALID := 0
const FILE_PATH_TYPE_ABSOLUTE := 1
const FILE_PATH_TYPE_API_FILENAME := 2
const FLOATING_GAMEPAD_TEXT_INPUT_MODE_SINGLE_LINE := 0
const FLOATING_GAMEPAD_TEXT_INPUT_MODE_MULTIPLE_LINES := 1
const FLOATING_GAMEPAD_TEXT_INPUT_MODE_EMAIL := 2
const FLOATING_GAMEPAD_TEXT_INPUT_MODE_NUMERIC := 3
const FRIEND_FLAG_NONE := 0
const FRIEND_FLAG_BLOCKED := 1
const FRIEND_FLAG_FRIENDSHIP_REQUESTED := 2
const FRIEND_FLAG_IMMEDIATE := 4
const FRIEND_FLAG_CLAN_MEMBER := 8
const FRIEND_FLAG_ON_GAME_SERVER := 16
const FRIEND_FLAG_REQUESTING_FRIENDSHIP := 128
const FRIEND_FLAG_REQUESTING_INFO := 256
const FRIEND_FLAG_IGNORED := 512
const FRIEND_FLAG_IGNORED_FRIEND := 1024
const FRIEND_FLAG_CHAT_MEMBER := 4096
const FRIEND_FLAG_ALL := 65535
const FRIEND_RELATION_NONE := 0
const FRIEND_RELATION_BLOCKED := 1
const FRIEND_RELATION_REQUEST_RECIPIENT := 2
const FRIEND_RELATION_FRIEND := 3
const FRIEND_RELATION_REQUEST_INITIATOR := 4
const FRIEND_RELATION_IGNORED := 5
const FRIEND_RELATION_IGNORED_FRIEND := 6
const FRIEND_RELATION_SUGGESTED := 7
const FRIEND_RELATION_MAX := 8
const GAME_TYPE_APP := 0
const GAME_TYPE_GAME_MOD := 1
const GAME_TYPE_SHORTCUT := 2
const GAMEPAD_TEXT_INPUT_LINE_MODE_SINGLE_LINE := 0
const GAMEPAD_TEXT_INPUT_LINE_MODE_MULTIPLE_LINES := 1
const GAMEPAD_TEXT_INPUT_MODE_NORMAL := 0
const GAMEPAD_TEXT_INPUT_MODE_PASSWORD := 1
const GAME_PERFORMANCE_SETTING_NOT_SET := 0
const GAME_PERFORMANCE_SETTING_LOW := 1
const GAME_PERFORMANCE_SETTING_MEDIUM := 2
const GAME_PERFORMANCE_SETTING_HIGH := 3
const GAME_PERFORMANCE_SETTING_ULTRA := 4
const GAME_PERFORMANCE_SETTING_CUSTOM := 5
const HTML_KEY_MODIFIER_NONE := 0
const HTML_KEY_MODIFIER_ALT_DOWN := 1
const HTML_KEY_MODIFIER_CTRL_DOWN := 2
const HTML_KEY_MODIFIER_SHIFT_DOWN := 4
const HTML_MOUSE_BUTTON_LEFT := 0
const HTML_MOUSE_BUTTON_RIGHT := 1
const HTML_MOUSE_BUTTON_MIDDLE := 2
const HTML_MOUSE_CURSOR_USER := 0
const HTML_MOUSE_CURSOR_NONE := 1
const HTML_MOUSE_CURSOR_ARROW := 2
const HTML_MOUSE_CURSOR_IBEAM := 3
const HTML_MOUSE_CURSOR_HOURGLASS := 4
const HTML_MOUSE_CURSOR_WAIT_ARROW := 5
const HTML_MOUSE_CURSOR_CROSSHAIR := 6
const HTML_MOUSE_CURSOR_UP := 7
const HTML_MOUSE_CURSOR_SIZE_NW := 8
const HTML_MOUSE_CURSOR_SIZE_SE := 9
const HTML_MOUSE_CURSOR_SIZE_NE := 10
const HTML_MOUSE_CURSOR_SIZE_SW := 11
const HTML_MOUSE_CURSOR_SIZE_W := 12
const HTML_MOUSE_CURSOR_SIZE_E := 13
const HTML_MOUSE_CURSOR_SIZE_N := 14
const HTML_MOUSE_CURSOR_SIZE_S := 15
const HTML_MOUSE_CURSOR_SIZE_WE := 16
const HTML_MOUSE_CURSOR_SIZE_NS := 17
const HTML_MOUSE_CURSOR_SIZE_ALL := 18
const HTML_MOUSE_CURSOR_CURSOR_NO := 19
const HTML_MOUSE_CURSOR_CURSOR_HAND := 20
const HTML_MOUSE_CURSOR_CURSOR_BLANK := 21
const HTML_MOUSE_CURSOR_MIDDLE_PAN := 22
const HTML_MOUSE_CURSOR_NORTH_PAN := 23
const HTML_MOUSE_CURSOR_NORTH_EAST_PAN := 24
const HTML_MOUSE_CURSOR_EAST_PAN := 25
const HTML_MOUSE_CURSOR_SOUTH_EAST_PAN := 26
const HTML_MOUSE_CURSOR_SOUTH_PAN := 27
const HTML_MOUSE_CURSOR_SOUTH_WEST_PAN := 28
const HTML_MOUSE_CURSOR_WEST_PAN := 29
const HTML_MOUSE_CURSOR_NORTH_WEST_PAN := 30
const HTML_MOUSE_CURSOR_ALIAS := 31
const HTML_MOUSE_CURSOR_CELL := 32
const HTML_MOUSE_CURSOR_COL_RESIZE := 33
const HTML_MOUSE_CURSOR_COPY_CUR := 34
const HTML_MOUSE_CURSOR_VERTICAL_TEXT := 35
const HTML_MOUSE_CURSOR_ROW_RESIZE := 36
const HTML_MOUSE_CURSOR_ZOOM_IN := 37
const HTML_MOUSE_CURSOR_ZOOM_OUT := 38
const HTML_MOUSE_CURSOR_HELP := 39
const HTML_MOUSE_CURSOR_CUSTOM := 40
const HTML_MOUSE_CURSOR_SIZE_NWSE := 41
const HTML_MOUSE_CURSOR_SIZE_NESW := 42
const HTML_MOUSE_CURSOR_LAST := 43
const HTTP_METHOD_INVALID := 0
const HTTP_METHOD_GET := 1
const HTTP_METHOD_HEAD := 2
const HTTP_METHOD_POST := 3
const HTTP_METHOD_PUT := 4
const HTTP_METHOD_DELETE := 5
const HTTP_METHOD_OPTIONS := 6
const HTTP_METHOD_PATCH := 7
const HTTP_STATUS_CODE_INVALID := 0
const HTTP_STATUS_CODE_100_CONTINUE := 100
const HTTP_STATUS_CODE_101_SWITCHING_PROTOCOLS := 101
const HTTP_STATUS_CODE_200_OK := 200
const HTTP_STATUS_CODE_201_CREATED := 201
const HTTP_STATUS_CODE_202_ACCEPTED := 202
const HTTP_STATUS_CODE_203_NON_AUTHORITATIVE := 203
const HTTP_STATUS_CODE_204_NO_CONTENT := 204
const HTTP_STATUS_CODE_205_RESET_CONTENT := 205
const HTTP_STATUS_CODE_206_PARTIAL_CONTENT := 206
const HTTP_STATUS_CODE_300_MULTIPLE_CHOICES := 300
const HTTP_STATUS_CODE_301_MOVED_PERMANENTLY := 301
const HTTP_STATUS_CODE_302_FOUND := 302
const HTTP_STATUS_CODE_303_SEE_OTHER := 303
const HTTP_STATUS_CODE_304_NOT_MODIFIED := 304
const HTTP_STATUS_CODE_305_USE_PROXY := 305
const HTTP_STATUS_CODE_307_TEMPORARY_REDIRECT := 307
const HTTP_STATUS_CODE_308_PERMANENT_REDIRECT := 308
const HTTP_STATUS_CODE_400_BAD_REQUEST := 400
const HTTP_STATUS_CODE_401_UNAUTHORIZED := 401
const HTTP_STATUS_CODE_402_PAYMENT_REQUIRED := 402
const HTTP_STATUS_CODE_403_FORBIDDEN := 403
const HTTP_STATUS_CODE_404_NOT_FOUND := 404
const HTTP_STATUS_CODE_405_METHOD_NOT_ALLOWED := 405
const HTTP_STATUS_CODE_406_NOT_ACCEPTABLE := 406
const HTTP_STATUS_CODE_407_PROXY_AUTH_REQUIRED := 407
const HTTP_STATUS_CODE_408_REQUEST_TIMEOUT := 408
const HTTP_STATUS_CODE_409_CONFLICT := 409
const HTTP_STATUS_CODE_410_GONE := 410
const HTTP_STATUS_CODE_411_LENGTH_REQUIRED := 411
const HTTP_STATUS_CODE_412_PRECONDITION_FAILED := 412
const HTTP_STATUS_CODE_413_REQUEST_ENTITY_TOO_LARGE := 413
const HTTP_STATUS_CODE_414_REQUEST_URI_TOO_LONG := 414
const HTTP_STATUS_CODE_415_UNSUPPORTED_MEDIA_TYPE := 415
const HTTP_STATUS_CODE_416_REQUESTED_RANGE_NOT_SATISFIABLE := 416
const HTTP_STATUS_CODE_417_EXPECTATION_FAILED := 417
const HTTP_STATUS_CODE_4XX_UNKNOWN := 418
const HTTP_STATUS_CODE_421_MISDIRECT_REQUEST := 421
const HTTP_STATUS_CODE_422_UNPROCESSABLE_CONTENT := 422
const HTTP_STATUS_CODE_423_LOCKED := 423
const HTTP_STATUS_CODE_424_FAILED_DEPENDENCY := 424
const HTTP_STATUS_CODE_425_TOO_EARLY := 425
const HTTP_STATUS_CODE_426_UPGRADE_REQUIRED := 426
const HTTP_STATUS_CODE_428_PRECONDITION_REQUIRED := 428
const HTTP_STATUS_CODE_429_TOO_MANY_REQUESTS := 429
const HTTP_STATUS_CODE_431_REQUEST_HEADER_FIELDS_TOO_LARGE := 431
const HTTP_STATUS_CODE_444_CONNECTION_CLOSED := 444
const HTTP_STATUS_CODE_451_UNAVAILABLE_FOR_LEGAL_AREASONS := 451
const HTTP_STATUS_CODE_500_INTERNAL_SERVER_ERROR := 500
const HTTP_STATUS_CODE_501_NOT_IMPLEMENTED := 501
const HTTP_STATUS_CODE_502_BAD_GATEWAY := 502
const HTTP_STATUS_CODE_503_SERVICE_UNAVAILABLE := 503
const HTTP_STATUS_CODE_504_GATEWAY_TIMEOUT := 504
const HTTP_STATUS_CODE_505_HTTP_VERSION_NOT_SUPPORTED := 505
const HTTP_STATUS_CODE_506_VARIANT_ALSO_NEGOTIATES := 506
const HTTP_STATUS_CODE_507_INSUFFICIENT_STORAGE := 507
const HTTP_STATUS_CODE_508_LOOP_DETECTED := 508
const HTTP_STATUS_CODE_510_NOT_EXTENDED := 510
const HTTP_STATUS_CODE_511_NETWORK_AUTHENTICATION_REQUIRED := 511
const HTTP_STATUS_CODE_5XX_UNKNOWN := 599
const INPUT_ACTION_EVENT_TYPE_DIGITAL_ACTION := 0
const INPUT_ACTION_EVENT_TYPE_ANALOG_ACTION := 1
const INPUT_ACTION_ORIGIN_NONE := 0
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_A := 1
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_B := 2
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_X := 3
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_Y := 4
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_BUMPER := 5
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RIGHT_BUMPER := 6
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_GRIP := 7
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RIGHT_GRIP := 8
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_START := 9
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_BACK := 10
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_PAD_TOUCH := 11
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_PAD_SWIPE := 12
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_PAD_CLICK := 13
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_PAD_DPAD_NORTH := 14
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_PAD_DPAD_SOUTH := 15
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_PAD_DPAD_WEST := 16
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_PAD_DPAD_EAST := 17
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RIGHT_PAD_TOUCH := 18
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RIGHT_PAD_SWIPE := 19
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RIGHT_PAD_CLICK := 20
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RIGHT_PAD_DPAD_NORTH := 21
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RIGHT_PAD_DPAD_SOUTH := 22
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RIGHT_PAD_DPAD_WEST := 23
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RIGHT_PAD_DPAD_EAST := 24
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_TRIGGER_PULL := 25
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_TRIGGER_CLICK := 26
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RIGHT_TRIGGER_PULL := 27
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RIGHT_TRIGGER_CLICK := 28
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_STICK_MOVE := 29
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_STICK_CLICK := 30
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_STICK_DPAD_NORTH := 31
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_STICK_DPAD_SOUTH := 32
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_STICK_DPAD_WEST := 33
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_LEFT_STICK_DPAD_EAST := 34
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_GYRO_MOVE := 35
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_GYRO_PITCH := 36
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_GYRO_YAW := 37
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_GYRO_ROLL := 38
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RESERVED0 := 39
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RESERVED1 := 40
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RESERVED2 := 41
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RESERVED3 := 42
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RESERVED4 := 43
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RESERVED5 := 44
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RESERVED6 := 45
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RESERVED7 := 46
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RESERVED8 := 47
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RESERVED9 := 48
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER_RESERVED10 := 49
const INPUT_ACTION_ORIGIN_PS4_X := 50
const INPUT_ACTION_ORIGIN_PS4_CIRCLE := 51
const INPUT_ACTION_ORIGIN_PS4_TRIANGLE := 52
const INPUT_ACTION_ORIGIN_PS4_SQUARE := 53
const INPUT_ACTION_ORIGIN_PS4_LEFT_BUMPER := 54
const INPUT_ACTION_ORIGIN_PS4_RIGHT_BUMPER := 55
const INPUT_ACTION_ORIGIN_PS4_OPTIONS := 56
const INPUT_ACTION_ORIGIN_PS4_SHARE := 57
const INPUT_ACTION_ORIGIN_PS4_LEFT_PAD_TOUCH := 58
const INPUT_ACTION_ORIGIN_PS4_LEFT_PAD_SWIPE := 59
const INPUT_ACTION_ORIGIN_PS4_LEFT_PAD_CLICK := 60
const INPUT_ACTION_ORIGIN_PS4_LEFT_PAD_DPAD_NORTH := 61
const INPUT_ACTION_ORIGIN_PS4_LEFT_PAD_DPAD_SOUTH := 62
const INPUT_ACTION_ORIGIN_PS4_LEFT_PAD_DPAD_WEST := 63
const INPUT_ACTION_ORIGIN_PS4_LEFT_PAD_DPAD_EAST := 64
const INPUT_ACTION_ORIGIN_PS4_RIGHT_PAD_TOUCH := 65
const INPUT_ACTION_ORIGIN_PS4_RIGHT_PAD_SWIPE := 66
const INPUT_ACTION_ORIGIN_PS4_RIGHT_PAD_CLICK := 67
const INPUT_ACTION_ORIGIN_PS4_RIGHT_PAD_DPAD_NORTH := 68
const INPUT_ACTION_ORIGIN_PS4_RIGHT_PAD_DPAD_SOUTH := 69
const INPUT_ACTION_ORIGIN_PS4_RIGHT_PAD_DPAD_WEST := 70
const INPUT_ACTION_ORIGIN_PS4_RIGHT_PAD_DPAD_EAST := 71
const INPUT_ACTION_ORIGIN_PS4_CENTER_PAD_TOUCH := 72
const INPUT_ACTION_ORIGIN_PS4_CENTER_PAD_SWIPE := 73
const INPUT_ACTION_ORIGIN_PS4_CENTER_PAD_CLICK := 74
const INPUT_ACTION_ORIGIN_PS4_CENTER_PAD_DPAD_NORTH := 75
const INPUT_ACTION_ORIGIN_PS4_CENTER_PAD_DPAD_SOUTH := 76
const INPUT_ACTION_ORIGIN_PS4_CENTER_PAD_DPAD_WEST := 77
const INPUT_ACTION_ORIGIN_PS4_CENTER_PAD_DPAD_EAST := 78
const INPUT_ACTION_ORIGIN_PS4_LEFT_TRIGGER_PULL := 79
const INPUT_ACTION_ORIGIN_PS4_LEFT_TRIGGER_CLICK := 80
const INPUT_ACTION_ORIGIN_PS4_RIGHT_TRIGGER_PULL := 81
const INPUT_ACTION_ORIGIN_PS4_RIGHT_TRIGGER_CLICK := 82
const INPUT_ACTION_ORIGIN_PS4_LEFT_STICK_MOVE := 83
const INPUT_ACTION_ORIGIN_PS4_LEFT_STICK_CLICK := 84
const INPUT_ACTION_ORIGIN_PS4_LEFT_STICK_DPAD_NORTH := 85
const INPUT_ACTION_ORIGIN_PS4_LEFT_STICK_DPAD_SOUTH := 86
const INPUT_ACTION_ORIGIN_PS4_LEFT_STICK_DPAD_WEST := 87
const INPUT_ACTION_ORIGIN_PS4_LEFT_STICK_DPAD_EAST := 88
const INPUT_ACTION_ORIGIN_PS4_RIGHT_STICK_MOVE := 89
const INPUT_ACTION_ORIGIN_PS4_RIGHT_STICK_CLICK := 90
const INPUT_ACTION_ORIGIN_PS4_RIGHT_STICK_DPAD_NORTH := 91
const INPUT_ACTION_ORIGIN_PS4_RIGHT_STICK_DPAD_SOUTH := 92
const INPUT_ACTION_ORIGIN_PS4_RIGHT_STICK_DPAD_WEST := 93
const INPUT_ACTION_ORIGIN_PS4_RIGHT_STICK_DPAD_EAST := 94
const INPUT_ACTION_ORIGIN_PS4_DPAD_NORTH := 95
const INPUT_ACTION_ORIGIN_PS4_DPAD_SOUTH := 96
const INPUT_ACTION_ORIGIN_PS4_DPAD_WEST := 97
const INPUT_ACTION_ORIGIN_PS4_DPAD_EAST := 98
const INPUT_ACTION_ORIGIN_PS4_GYRO_MOVE := 99
const INPUT_ACTION_ORIGIN_PS4_GYRO_PITCH := 100
const INPUT_ACTION_ORIGIN_PS4_GYRO_YAW := 101
const INPUT_ACTION_ORIGIN_PS4_GYRO_ROLL := 102
const INPUT_ACTION_ORIGIN_PS4_DPAD_MOVE := 103
const INPUT_ACTION_ORIGIN_PS4_RESERVED1 := 104
const INPUT_ACTION_ORIGIN_PS4_RESERVED2 := 105
const INPUT_ACTION_ORIGIN_PS4_RESERVED3 := 106
const INPUT_ACTION_ORIGIN_PS4_RESERVED4 := 107
const INPUT_ACTION_ORIGIN_PS4_RESERVED5 := 108
const INPUT_ACTION_ORIGIN_PS4_RESERVED6 := 109
const INPUT_ACTION_ORIGIN_PS4_RESERVED7 := 110
const INPUT_ACTION_ORIGIN_PS4_RESERVED8 := 111
const INPUT_ACTION_ORIGIN_PS4_RESERVED9 := 112
const INPUT_ACTION_ORIGIN_PS4_RESERVED10 := 113
const INPUT_ACTION_ORIGIN_XBOXONE_A := 114
const INPUT_ACTION_ORIGIN_XBOXONE_B := 115
const INPUT_ACTION_ORIGIN_XBOXONE_X := 116
const INPUT_ACTION_ORIGIN_XBOXONE_Y := 117
const INPUT_ACTION_ORIGIN_XBOXONE_LEFT_BUMPER := 118
const INPUT_ACTION_ORIGIN_XBOXONE_RIGHT_BUMPER := 119
const INPUT_ACTION_ORIGIN_XBOXONE_MENU := 120
const INPUT_ACTION_ORIGIN_XBOXONE_VIEW := 121
const INPUT_ACTION_ORIGIN_XBOXONE_LEFT_TRIGGER_PULL := 122
const INPUT_ACTION_ORIGIN_XBOXONE_LEFT_TRIGGER_CLICK := 123
const INPUT_ACTION_ORIGIN_XBOXONE_RIGHT_TRIGGER_PULL := 124
const INPUT_ACTION_ORIGIN_XBOXONE_RIGHT_TRIGGER_CLICK := 125
const INPUT_ACTION_ORIGIN_XBOXONE_LEFT_STICK_MOVE := 126
const INPUT_ACTION_ORIGIN_XBOXONE_LEFT_STICK_CLICK := 127
const INPUT_ACTION_ORIGIN_XBOXONE_LEFT_STICK_DPAD_NORTH := 128
const INPUT_ACTION_ORIGIN_XBOXONE_LEFT_STICK_DPAD_SOUTH := 129
const INPUT_ACTION_ORIGIN_XBOXONE_LEFT_STICK_DPAD_WEST := 130
const INPUT_ACTION_ORIGIN_XBOXONE_LEFT_STICK_DPAD_EAST := 131
const INPUT_ACTION_ORIGIN_XBOXONE_RIGHT_STICK_MOVE := 132
const INPUT_ACTION_ORIGIN_XBOXONE_RIGHT_STICK_CLICK := 133
const INPUT_ACTION_ORIGIN_XBOXONE_RIGHT_STICK_DPAD_NORTH := 134
const INPUT_ACTION_ORIGIN_XBOXONE_RIGHT_STICK_DPAD_SOUTH := 135
const INPUT_ACTION_ORIGIN_XBOXONE_RIGHT_STICK_DPAD_WEST := 136
const INPUT_ACTION_ORIGIN_XBOXONE_RIGHT_STICK_DPAD_EAST := 137
const INPUT_ACTION_ORIGIN_XBOXONE_DPAD_NORTH := 138
const INPUT_ACTION_ORIGIN_XBOXONE_DPAD_SOUTH := 139
const INPUT_ACTION_ORIGIN_XBOXONE_DPAD_WEST := 140
const INPUT_ACTION_ORIGIN_XBOXONE_DPAD_EAST := 141
const INPUT_ACTION_ORIGIN_XBOXONE_DPAD_MOVE := 142
const INPUT_ACTION_ORIGIN_XBOXONE_LEFT_GRIP_LOWER := 143
const INPUT_ACTION_ORIGIN_XBOXONE_LEFT_GRIP_UPPER := 144
const INPUT_ACTION_ORIGIN_XBOXONE_RIGHT_GRIP_LOWER := 145
const INPUT_ACTION_ORIGIN_XBOXONE_RIGHT_GRIP_UPPER := 146
const INPUT_ACTION_ORIGIN_XBOXONE_SHARE := 147
const INPUT_ACTION_ORIGIN_XBOXONE_RESERVED6 := 148
const INPUT_ACTION_ORIGIN_XBOXONE_RESERVED7 := 149
const INPUT_ACTION_ORIGIN_XBOXONE_RESERVED8 := 150
const INPUT_ACTION_ORIGIN_XBOXONE_RESERVED9 := 151
const INPUT_ACTION_ORIGIN_XBOXONE_RESERVED10 := 152
const INPUT_ACTION_ORIGIN_XBOX360_A := 153
const INPUT_ACTION_ORIGIN_XBOX360_B := 154
const INPUT_ACTION_ORIGIN_XBOX360_X := 155
const INPUT_ACTION_ORIGIN_XBOX360_Y := 156
const INPUT_ACTION_ORIGIN_XBOX360_LEFT_BUMPER := 157
const INPUT_ACTION_ORIGIN_XBOX360_RIGHT_BUMPER := 158
const INPUT_ACTION_ORIGIN_XBOX360_START := 159
const INPUT_ACTION_ORIGIN_XBOX360_BACK := 160
const INPUT_ACTION_ORIGIN_XBOX360_LEFT_TRIGGER_PULL := 161
const INPUT_ACTION_ORIGIN_XBOX360_LEFT_TRIGGER_CLICK := 162
const INPUT_ACTION_ORIGIN_XBOX360_RIGHT_TRIGGER_PULL := 163
const INPUT_ACTION_ORIGIN_XBOX360_RIGHT_TRIGGER_CLICK := 164
const INPUT_ACTION_ORIGIN_XBOX360_LEFT_STICK_MOVE := 165
const INPUT_ACTION_ORIGIN_XBOX360_LEFT_STICK_CLICK := 166
const INPUT_ACTION_ORIGIN_XBOX360_LEFT_STICK_DPAD_NORTH := 167
const INPUT_ACTION_ORIGIN_XBOX360_LEFT_STICK_DPAD_SOUTH := 168
const INPUT_ACTION_ORIGIN_XBOX360_LEFT_STICK_DPAD_WEST := 169
const INPUT_ACTION_ORIGIN_XBOX360_LEFT_STICK_DPAD_EAST := 170
const INPUT_ACTION_ORIGIN_XBOX360_RIGHT_STICK_MOVE := 171
const INPUT_ACTION_ORIGIN_XBOX360_RIGHT_STICK_CLICK := 172
const INPUT_ACTION_ORIGIN_XBOX360_RIGHT_STICK_DPAD_NORTH := 173
const INPUT_ACTION_ORIGIN_XBOX360_RIGHT_STICK_DPAD_SOUTH := 174
const INPUT_ACTION_ORIGIN_XBOX360_RIGHT_STICK_DPAD_WEST := 175
const INPUT_ACTION_ORIGIN_XBOX360_RIGHT_STICK_DPAD_EAST := 176
const INPUT_ACTION_ORIGIN_XBOX360_DPAD_NORTH := 177
const INPUT_ACTION_ORIGIN_XBOX360_DPAD_SOUTH := 178
const INPUT_ACTION_ORIGIN_XBOX360_DPAD_WEST := 179
const INPUT_ACTION_ORIGIN_XBOX360_DPAD_EAST := 180
const INPUT_ACTION_ORIGIN_XBOX360_DPAD_MOVE := 181
const INPUT_ACTION_ORIGIN_XBOX360_RESERVED1 := 182
const INPUT_ACTION_ORIGIN_XBOX360_RESERVED2 := 183
const INPUT_ACTION_ORIGIN_XBOX360_RESERVED3 := 184
const INPUT_ACTION_ORIGIN_XBOX360_RESERVED4 := 185
const INPUT_ACTION_ORIGIN_XBOX360_RESERVED5 := 186
const INPUT_ACTION_ORIGIN_XBOX360_RESERVED6 := 187
const INPUT_ACTION_ORIGIN_XBOX360_RESERVED7 := 188
const INPUT_ACTION_ORIGIN_XBOX360_RESERVED8 := 189
const INPUT_ACTION_ORIGIN_XBOX360_RESERVED9 := 190
const INPUT_ACTION_ORIGIN_XBOX360_RESERVED10 := 191
const INPUT_ACTION_ORIGIN_SWITCH_A := 192
const INPUT_ACTION_ORIGIN_SWITCH_B := 193
const INPUT_ACTION_ORIGIN_SWITCH_X := 194
const INPUT_ACTION_ORIGIN_SWITCH_Y := 195
const INPUT_ACTION_ORIGIN_SWITCH_LEFT_BUMPER := 196
const INPUT_ACTION_ORIGIN_SWITCH_RIGHT_BUMPER := 197
const INPUT_ACTION_ORIGIN_SWITCH_PLUS := 198
const INPUT_ACTION_ORIGIN_SWITCH_MINUS := 199
const INPUT_ACTION_ORIGIN_SWITCH_CAPTURE := 200
const INPUT_ACTION_ORIGIN_SWITCH_LEFT_TRIGGER_PULL := 201
const INPUT_ACTION_ORIGIN_SWITCH_LEFT_TRIGGER_CLICK := 202
const INPUT_ACTION_ORIGIN_SWITCH_RIGHT_TRIGGER_PULL := 203
const INPUT_ACTION_ORIGIN_SWITCH_RIGHT_TRIGGER_CLICK := 204
const INPUT_ACTION_ORIGIN_SWITCH_LEFT_STICK_MOVE := 205
const INPUT_ACTION_ORIGIN_SWITCH_LEFT_STICK_CLICK := 206
const INPUT_ACTION_ORIGIN_SWITCH_LEFT_STICK_DPAD_NORTH := 207
const INPUT_ACTION_ORIGIN_SWITCH_LEFT_STICK_DPAD_SOUTH := 208
const INPUT_ACTION_ORIGIN_SWITCH_LEFT_STICK_DPAD_WEST := 209
const INPUT_ACTION_ORIGIN_SWITCH_LEFT_STICK_DPAD_EAST := 210
const INPUT_ACTION_ORIGIN_SWITCH_RIGHT_STICK_MOVE := 211
const INPUT_ACTION_ORIGIN_SWITCH_RIGHT_STICK_CLICK := 212
const INPUT_ACTION_ORIGIN_SWITCH_RIGHT_STICK_DPAD_NORTH := 213
const INPUT_ACTION_ORIGIN_SWITCH_RIGHT_STICK_DPAD_SOUTH := 214
const INPUT_ACTION_ORIGIN_SWITCH_RIGHT_STICK_DPAD_WEST := 215
const INPUT_ACTION_ORIGIN_SWITCH_RIGHT_STICK_DPAD_EAST := 216
const INPUT_ACTION_ORIGIN_SWITCH_DPAD_NORTH := 217
const INPUT_ACTION_ORIGIN_SWITCH_DPAD_SOUTH := 218
const INPUT_ACTION_ORIGIN_SWITCH_DPAD_WEST := 219
const INPUT_ACTION_ORIGIN_SWITCH_DPAD_EAST := 220
const INPUT_ACTION_ORIGIN_SWITCH_PRO_GYRO_MOVE := 221
const INPUT_ACTION_ORIGIN_SWITCH_PRO_GYRO_PITCH := 222
const INPUT_ACTION_ORIGIN_SWITCH_PRO_GYRO_YAW := 223
const INPUT_ACTION_ORIGIN_SWITCH_PRO_GYRO_ROLL := 224
const INPUT_ACTION_ORIGIN_SWITCH_DPAD_MOVE := 225
const INPUT_ACTION_ORIGIN_SWITCH_RESERVED1 := 226
const INPUT_ACTION_ORIGIN_SWITCH_RESERVED2 := 227
const INPUT_ACTION_ORIGIN_SWITCH_RESERVED3 := 228
const INPUT_ACTION_ORIGIN_SWITCH_RESERVED4 := 229
const INPUT_ACTION_ORIGIN_SWITCH_RESERVED5 := 230
const INPUT_ACTION_ORIGIN_SWITCH_RESERVED6 := 231
const INPUT_ACTION_ORIGIN_SWITCH_RESERVED7 := 232
const INPUT_ACTION_ORIGIN_SWITCH_RESERVED8 := 233
const INPUT_ACTION_ORIGIN_SWITCH_RESERVED9 := 234
const INPUT_ACTION_ORIGIN_SWITCH_RESERVED10 := 235
const INPUT_ACTION_ORIGIN_SWITCH_RIGHT_GYRO_MOVE := 236
const INPUT_ACTION_ORIGIN_SWITCH_RIGHT_GYRO_PITCH := 237
const INPUT_ACTION_ORIGIN_SWITCH_RIGHT_GYRO_YAW := 238
const INPUT_ACTION_ORIGIN_SWITCH_RIGHT_GYRO_ROLL := 239
const INPUT_ACTION_ORIGIN_SWITCH_LEFT_GYRO_MOVE := 240
const INPUT_ACTION_ORIGIN_SWITCH_LEFT_GYRO_PITCH := 241
const INPUT_ACTION_ORIGIN_SWITCH_LEFT_GYRO_YAW := 242
const INPUT_ACTION_ORIGIN_SWITCH_LEFT_GYRO_ROLL := 243
const INPUT_ACTION_ORIGIN_SWITCH_LEFT_GRIP_LOWER := 244
const INPUT_ACTION_ORIGIN_SWITCH_LEFT_GRIP_UPPER := 245
const INPUT_ACTION_ORIGIN_SWITCH_RIGHT_GRIP_LOWER := 246
const INPUT_ACTION_ORIGIN_SWITCH_RIGHT_GRIP_UPPER := 247
const INPUT_ACTION_ORIGIN_SWITCH_JOYCON_BUTTON_N := 248
const INPUT_ACTION_ORIGIN_SWITCH_JOYCON_BUTTON_E := 249
const INPUT_ACTION_ORIGIN_SWITCH_JOYCON_BUTTON_S := 250
const INPUT_ACTION_ORIGIN_SWITCH_JOYCON_BUTTON_W := 251
const INPUT_ACTION_ORIGIN_SWITCH_RESERVED15 := 252
const INPUT_ACTION_ORIGIN_SWITCH_RESERVED16 := 253
const INPUT_ACTION_ORIGIN_SWITCH_RESERVED17 := 254
const INPUT_ACTION_ORIGIN_SWITCH_RESERVED18 := 255
const INPUT_ACTION_ORIGIN_SWITCH_RESERVED19 := 256
const INPUT_ACTION_ORIGIN_SWITCH_RESERVED20 := 257
const INPUT_ACTION_ORIGIN_PS5_X := 258
const INPUT_ACTION_ORIGIN_PS5_CIRCLE := 259
const INPUT_ACTION_ORIGIN_PS5_TRIANGLE := 260
const INPUT_ACTION_ORIGIN_PS5_SQUARE := 261
const INPUT_ACTION_ORIGIN_PS5_LEFT_BUMPER := 262
const INPUT_ACTION_ORIGIN_PS5_RIGHT_BUMPER := 263
const INPUT_ACTION_ORIGIN_PS5_OPTION := 264
const INPUT_ACTION_ORIGIN_PS5_CREATE := 265
const INPUT_ACTION_ORIGIN_PS5_MUTE := 266
const INPUT_ACTION_ORIGIN_PS5_LEFT_PAD_TOUCH := 267
const INPUT_ACTION_ORIGIN_PS5_LEFT_PAD_SWIPE := 268
const INPUT_ACTION_ORIGIN_PS5_LEFT_PAD_CLICK := 269
const INPUT_ACTION_ORIGIN_PS5_LEFT_PAD_DPAD_NORTH := 270
const INPUT_ACTION_ORIGIN_PS5_LEFT_PAD_DPAD_SOUTH := 271
const INPUT_ACTION_ORIGIN_PS5_LEFT_PAD_DPAD_WEST := 272
const INPUT_ACTION_ORIGIN_PS5_LEFT_PAD_DPAD_EAST := 273
const INPUT_ACTION_ORIGIN_PS5_RIGHT_PAD_TOUCH := 274
const INPUT_ACTION_ORIGIN_PS5_RIGHT_PAD_SWIPE := 275
const INPUT_ACTION_ORIGIN_PS5_RIGHT_PAD_CLICK := 276
const INPUT_ACTION_ORIGIN_PS5_RIGHT_PAD_DPAD_NORTH := 277
const INPUT_ACTION_ORIGIN_PS5_RIGHT_PAD_DPAD_SOUTH := 278
const INPUT_ACTION_ORIGIN_PS5_RIGHT_PAD_DPAD_WEST := 279
const INPUT_ACTION_ORIGIN_PS5_RIGHT_PAD_DPAD_EAST := 280
const INPUT_ACTION_ORIGIN_PS5_CENTER_PAD_TOUCH := 281
const INPUT_ACTION_ORIGIN_PS5_CENTER_PAD_SWIPE := 282
const INPUT_ACTION_ORIGIN_PS5_CENTER_PAD_CLICK := 283
const INPUT_ACTION_ORIGIN_PS5_CENTER_PAD_DPAD_NORTH := 284
const INPUT_ACTION_ORIGIN_PS5_CENTER_PAD_DPAD_SOUTH := 285
const INPUT_ACTION_ORIGIN_PS5_CENTER_PAD_DPAD_WEST := 286
const INPUT_ACTION_ORIGIN_PS5_CENTER_PAD_DPAD_EAST := 287
const INPUT_ACTION_ORIGIN_PS5_LEFT_TRIGGER_PULL := 288
const INPUT_ACTION_ORIGIN_PS5_LEFT_TRIGGER_CLICK := 289
const INPUT_ACTION_ORIGIN_PS5_RIGHT_TRIGGER_PULL := 290
const INPUT_ACTION_ORIGIN_PS5_RIGHT_TRIGGER_CLICK := 291
const INPUT_ACTION_ORIGIN_PS5_LEFT_STICK_MOVE := 292
const INPUT_ACTION_ORIGIN_PS5_LEFT_STICK_CLICK := 293
const INPUT_ACTION_ORIGIN_PS5_LEFT_STICK_DPAD_NORTH := 294
const INPUT_ACTION_ORIGIN_PS5_LEFT_STICK_DPAD_SOUTH := 295
const INPUT_ACTION_ORIGIN_PS5_LEFT_STICK_DPAD_WEST := 296
const INPUT_ACTION_ORIGIN_PS5_LEFT_STICK_DPAD_EAST := 297
const INPUT_ACTION_ORIGIN_PS5_RIGHT_STICK_MOVE := 298
const INPUT_ACTION_ORIGIN_PS5_RIGHT_STICK_CLICK := 299
const INPUT_ACTION_ORIGIN_PS5_RIGHT_STICK_DPAD_NORTH := 300
const INPUT_ACTION_ORIGIN_PS5_RIGHT_STICK_DPAD_SOUTH := 301
const INPUT_ACTION_ORIGIN_PS5_RIGHT_STICK_DPAD_WEST := 302
const INPUT_ACTION_ORIGIN_PS5_RIGHT_STICK_DPAD_EAST := 303
const INPUT_ACTION_ORIGIN_PS5_DPAD_NORTH := 304
const INPUT_ACTION_ORIGIN_PS5_DPAD_SOUTH := 305
const INPUT_ACTION_ORIGIN_PS5_DPAD_WEST := 306
const INPUT_ACTION_ORIGIN_PS5_DPAD_EAST := 307
const INPUT_ACTION_ORIGIN_PS5_GYRO_MOVE := 308
const INPUT_ACTION_ORIGIN_PS5_GYRO_PITCH := 309
const INPUT_ACTION_ORIGIN_PS5_GYRO_YAW := 310
const INPUT_ACTION_ORIGIN_PS5_GYRO_ROLL := 311
const INPUT_ACTION_ORIGIN_PS5_DPAD_MOVE := 312
const INPUT_ACTION_ORIGIN_PS5_LEFT_GRIP := 313
const INPUT_ACTION_ORIGIN_PS5_RIGHT_GRIP := 314
const INPUT_ACTION_ORIGIN_PS5_LEFT_FN := 315
const INPUT_ACTION_ORIGIN_PS5_RIGHT_FN := 316
const INPUT_ACTION_ORIGIN_PS5_RESERVED5 := 317
const INPUT_ACTION_ORIGIN_PS5_RESERVED6 := 318
const INPUT_ACTION_ORIGIN_PS5_RESERVED7 := 319
const INPUT_ACTION_ORIGIN_PS5_RESERVED8 := 320
const INPUT_ACTION_ORIGIN_PS5_RESERVED9 := 321
const INPUT_ACTION_ORIGIN_PS5_RESERVED10 := 322
const INPUT_ACTION_ORIGIN_PS5_RESERVED11 := 323
const INPUT_ACTION_ORIGIN_PS5_RESERVED12 := 324
const INPUT_ACTION_ORIGIN_PS5_RESERVED13 := 325
const INPUT_ACTION_ORIGIN_PS5_RESERVED14 := 326
const INPUT_ACTION_ORIGIN_PS5_RESERVED15 := 327
const INPUT_ACTION_ORIGIN_PS5_RESERVED16 := 328
const INPUT_ACTION_ORIGIN_PS5_RESERVED17 := 329
const INPUT_ACTION_ORIGIN_PS5_RESERVED18 := 330
const INPUT_ACTION_ORIGIN_PS5_RESERVED19 := 331
const INPUT_ACTION_ORIGIN_PS5_RESERVED20 := 332
const INPUT_ACTION_ORIGIN_STEAMDECK_A := 333
const INPUT_ACTION_ORIGIN_STEAMDECK_B := 334
const INPUT_ACTION_ORIGIN_STEAMDECK_X := 335
const INPUT_ACTION_ORIGIN_STEAMDECK_Y := 336
const INPUT_ACTION_ORIGIN_STEAMDECK_L1 := 337
const INPUT_ACTION_ORIGIN_STEAMDECK_R1 := 338
const INPUT_ACTION_ORIGIN_STEAMDECK_MENU := 339
const INPUT_ACTION_ORIGIN_STEAMDECK_VIEW := 340
const INPUT_ACTION_ORIGIN_STEAMDECK_LEFT_PAD_TOUCH := 341
const INPUT_ACTION_ORIGIN_STEAMDECK_LEFT_PAD_SWIPE := 342
const INPUT_ACTION_ORIGIN_STEAMDECK_LEFT_PAD_CLICK := 343
const INPUT_ACTION_ORIGIN_STEAMDECK_LEFT_PAD_DPAD_NORTH := 344
const INPUT_ACTION_ORIGIN_STEAMDECK_LEFT_PAD_DPAD_SOUTH := 345
const INPUT_ACTION_ORIGIN_STEAMDECK_LEFT_PAD_DPAD_WEST := 346
const INPUT_ACTION_ORIGIN_STEAMDECK_LEFT_PAD_DPAD_EAST := 347
const INPUT_ACTION_ORIGIN_STEAMDECK_RIGHT_PAD_TOUCH := 348
const INPUT_ACTION_ORIGIN_STEAMDECK_RIGHT_PAD_SWIPE := 349
const INPUT_ACTION_ORIGIN_STEAMDECK_RIGHT_PAD_CLICK := 350
const INPUT_ACTION_ORIGIN_STEAMDECK_RIGHT_PAD_DPAD_NORTH := 351
const INPUT_ACTION_ORIGIN_STEAMDECK_RIGHT_PAD_DPAD_SOUTH := 352
const INPUT_ACTION_ORIGIN_STEAMDECK_RIGHT_PAD_DPAD_WEST := 353
const INPUT_ACTION_ORIGIN_STEAMDECK_RIGHT_PAD_DPAD_EAST := 354
const INPUT_ACTION_ORIGIN_STEAMDECK_L2_SOFTPULL := 355
const INPUT_ACTION_ORIGIN_STEAMDECK_L2 := 356
const INPUT_ACTION_ORIGIN_STEAMDECK_R2_SOFTPULL := 357
const INPUT_ACTION_ORIGIN_STEAMDECK_R2 := 358
const INPUT_ACTION_ORIGIN_STEAMDECK_LEFT_STICK_MOVE := 359
const INPUT_ACTION_ORIGIN_STEAMDECK_L3 := 360
const INPUT_ACTION_ORIGIN_STEAMDECK_LEFT_STICK_DPAD_NORTH := 361
const INPUT_ACTION_ORIGIN_STEAMDECK_LEFT_STICK_DPAD_SOUTH := 362
const INPUT_ACTION_ORIGIN_STEAMDECK_LEFT_STICK_DPAD_WEST := 363
const INPUT_ACTION_ORIGIN_STEAMDECK_LEFT_STICK_DPAD_EAST := 364
const INPUT_ACTION_ORIGIN_STEAMDECK_LEFT_STICK_TOUCH := 365
const INPUT_ACTION_ORIGIN_STEAMDECK_RIGHT_STICK_MOVE := 366
const INPUT_ACTION_ORIGIN_STEAMDECK_R3 := 367
const INPUT_ACTION_ORIGIN_STEAMDECK_RIGHT_STICK_DPAD_NORTH := 368
const INPUT_ACTION_ORIGIN_STEAMDECK_RIGHT_STICK_DPAD_SOUTH := 369
const INPUT_ACTION_ORIGIN_STEAMDECK_RIGHT_STICK_DPAD_WEST := 370
const INPUT_ACTION_ORIGIN_STEAMDECK_RIGHT_STICK_DPAD_EAST := 371
const INPUT_ACTION_ORIGIN_STEAMDECK_RIGHT_STICK_TOUCH := 372
const INPUT_ACTION_ORIGIN_STEAMDECK_L4 := 373
const INPUT_ACTION_ORIGIN_STEAMDECK_R4 := 374
const INPUT_ACTION_ORIGIN_STEAMDECK_L5 := 375
const INPUT_ACTION_ORIGIN_STEAMDECK_R5 := 376
const INPUT_ACTION_ORIGIN_STEAMDECK_DPAD_MOVE := 377
const INPUT_ACTION_ORIGIN_STEAMDECK_DPAD_NORTH := 378
const INPUT_ACTION_ORIGIN_STEAMDECK_DPAD_SOUTH := 379
const INPUT_ACTION_ORIGIN_STEAMDECK_DPAD_WEST := 380
const INPUT_ACTION_ORIGIN_STEAMDECK_DPAD_EAST := 381
const INPUT_ACTION_ORIGIN_STEAMDECK_GYRO_MOVE := 382
const INPUT_ACTION_ORIGIN_STEAMDECK_GYRO_PITCH := 383
const INPUT_ACTION_ORIGIN_STEAMDECK_GYRO_YAW := 384
const INPUT_ACTION_ORIGIN_STEAMDECK_GYRO_ROLL := 385
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED1 := 386
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED2 := 387
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED3 := 388
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED4 := 389
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED5 := 390
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED6 := 391
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED7 := 392
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED8 := 393
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED9 := 394
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED10 := 395
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED11 := 396
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED12 := 397
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED13 := 398
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED14 := 399
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED15 := 400
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED16 := 401
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED17 := 402
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED18 := 403
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED19 := 404
const INPUT_ACTION_ORIGIN_STEAMDECK_RESERVED20 := 405
const INPUT_ACTION_ORIGIN_HORIPAD_M1 := 406
const INPUT_ACTION_ORIGIN_HORIPAD_M2 := 407
const INPUT_ACTION_ORIGIN_HORIPAD_L4 := 408
const INPUT_ACTION_ORIGIN_HORIPAD_R4 := 409
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_A := 410
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_B := 411
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_X := 412
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_Y := 413
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_LB := 414
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RB := 415
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_MENU := 416
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_VIEW := 417
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_LEFT_PAD_TOUCH := 418
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_LEFT_PAD_SWIPE := 419
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_LEFT_PAD_CLICK := 420
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_LEFT_PAD_DPAD_NORTH := 421
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_LEFT_PAD_DPAD_SOUTH := 422
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_LEFT_PAD_DPAD_WEST := 423
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_LEFT_PAD_DPAD_EAST := 424
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RIGHT_PAD_TOUCH := 425
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RIGHT_PAD_SWIPE := 426
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RIGHT_PAD_CLICK := 427
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RIGHT_PAD_DPAD_NORTH := 428
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RIGHT_PAD_DPAD_SOUTH := 429
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RIGHT_PAD_DPAD_WEST := 430
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RIGHT_PAD_DPAD_EAST := 431
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_LT_SOFTPULL := 432
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_LT := 433
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RT_SOFTPULL := 434
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RT := 435
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_LEFT_STICK_MOVE := 436
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_LS := 437
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_LEFT_STICK_DPAD_NORTH := 438
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_LEFT_STICK_DPAD_SOUTH := 439
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_LEFT_STICK_DPAD_WEST := 440
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_LEFT_STICK_DPAD_EAST := 441
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RIGHT_STICK_MOVE := 442
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RS := 443
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RIGHT_STICK_DPAD_NORTH := 444
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RIGHT_STICK_DPAD_SOUTH := 445
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RIGHT_STICK_DPAD_WEST := 446
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RIGHT_STICK_DPAD_EAST := 447
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_Y1 := 448
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_Y2 := 449
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_DPAD_MOVE := 450
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_DPAD_NORTH := 451
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_DPAD_SOUTH := 452
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_DPAD_WEST := 453
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_DPAD_EAST := 454
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_GYRO_MOVE := 455
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_GYRO_PITCH := 456
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_GYRO_YAW := 457
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_GYRO_ROLL := 458
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED1 := 459
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED2 := 460
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED3 := 461
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED4 := 462
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED5 := 463
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED6 := 464
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED7 := 465
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED8 := 466
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED9 := 467
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED10 := 468
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED11 := 469
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED12 := 470
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED13 := 471
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED14 := 472
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED15 := 473
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED16 := 474
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED17 := 475
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED18 := 476
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED19 := 477
const INPUT_ACTION_ORIGIN_LENOVO_LEGION_GO_RESERVED20 := 478
const INPUT_ACTION_ORIGIN_GENERIC_L4 := 479
const INPUT_ACTION_ORIGIN_GENERIC_R4 := 480
const INPUT_ACTION_ORIGIN_GENERIC_L5 := 481
const INPUT_ACTION_ORIGIN_GENERIC_R5 := 482
const INPUT_ACTION_ORIGIN_GENERIC_PL := 483
const INPUT_ACTION_ORIGIN_GENERIC_PR := 484
const INPUT_ACTION_ORIGIN_GENERIC_C := 485
const INPUT_ACTION_ORIGIN_GENERIC_Z := 486
const INPUT_ACTION_ORIGIN_GENERIC_MISC1 := 487
const INPUT_ACTION_ORIGIN_GENERIC_MISC2 := 488
const INPUT_ACTION_ORIGIN_GENERIC_MISC3 := 489
const INPUT_ACTION_ORIGIN_GENERIC_MISC4 := 490
const INPUT_ACTION_ORIGIN_GENERIC_MISC5 := 491
const INPUT_ACTION_ORIGIN_GENERIC_MISC6 := 492
const INPUT_ACTION_ORIGIN_GENERIC_MISC7 := 493
const INPUT_ACTION_ORIGIN_GENERIC_MISC8 := 494
const INPUT_ACTION_ORIGIN_SWITCH2_RIGHT_TRIGGER_PULL := 506
const INPUT_ACTION_ORIGIN_SWITCH2_RIGHT_TRIGGER_CLICK := 507
const INPUT_ACTION_ORIGIN_SWITCH2_LEFT_STICK_MOVE := 508
const INPUT_ACTION_ORIGIN_SWITCH2_LEFT_STICK_CLICK := 509
const INPUT_ACTION_ORIGIN_SWITCH2_LEFT_STICK_DPAD_NORTH := 510
const INPUT_ACTION_ORIGIN_SWITCH2_LEFT_STICK_DPAD_SOUTH := 511
const INPUT_ACTION_ORIGIN_SWITCH2_LEFT_STICK_DPAD_WEST := 512
const INPUT_ACTION_ORIGIN_SWITCH2_LEFT_STICK_DPAD_EAST := 513
const INPUT_ACTION_ORIGIN_SWITCH2_RIGHT_STICK_MOVE := 514
const INPUT_ACTION_ORIGIN_SWITCH2_RIGHT_STICK_CLICK := 515
const INPUT_ACTION_ORIGIN_SWITCH2_RIGHT_STICK_DPAD_NORTH := 516
const INPUT_ACTION_ORIGIN_SWITCH2_RIGHT_STICK_DPAD_SOUTH := 517
const INPUT_ACTION_ORIGIN_SWITCH2_RIGHT_STICK_DPAD_WEST := 518
const INPUT_ACTION_ORIGIN_SWITCH2_RIGHT_STICK_DPAD_EAST := 519
const INPUT_ACTION_ORIGIN_SWITCH2_DPAD_MOVE := 520
const INPUT_ACTION_ORIGIN_SWITCH2_DPAD_NORTH := 521
const INPUT_ACTION_ORIGIN_SWITCH2_DPAD_SOUTH := 522
const INPUT_ACTION_ORIGIN_SWITCH2_DPAD_WEST := 523
const INPUT_ACTION_ORIGIN_SWITCH2_DPAD_EAST := 524
const INPUT_ACTION_ORIGIN_SWITCH2_PRO_GYRO_MOVE := 525
const INPUT_ACTION_ORIGIN_SWITCH2_PRO_GYRO_PITCH := 526
const INPUT_ACTION_ORIGIN_SWITCH2_PRO_GYRO_YAW := 527
const INPUT_ACTION_ORIGIN_SWITCH2_PRO_GYRO_ROLL := 528
const INPUT_ACTION_ORIGIN_SWITCH2_GL := 529
const INPUT_ACTION_ORIGIN_SWITCH2_GR := 530
const INPUT_ACTION_ORIGIN_SWITCH2_C := 531
const INPUT_ACTION_ORIGIN_SWITCH2_RESERVED1 := 532
const INPUT_ACTION_ORIGIN_SWITCH2_RESERVED2 := 533
const INPUT_ACTION_ORIGIN_SWITCH2_RESERVED3 := 534
const INPUT_ACTION_ORIGIN_SWITCH2_RESERVED4 := 535
const INPUT_ACTION_ORIGIN_SWITCH2_RESERVED5 := 536
const INPUT_ACTION_ORIGIN_SWITCH2_RESERVED6 := 537
const INPUT_ACTION_ORIGIN_SWITCH2_RESERVED7 := 538
const INPUT_ACTION_ORIGIN_SWITCH2_RESERVED8 := 539
const INPUT_ACTION_ORIGIN_SWITCH2_RESERVED9 := 540
const INPUT_ACTION_ORIGIN_SWITCH2_RESERVED10 := 541
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_A := 542
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_B := 543
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_X := 544
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_Y := 545
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_L1 := 546
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_R1 := 547
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_MENU := 548
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_VIEW := 549
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_LEFT_PAD_TOUCH := 550
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_LEFT_PAD_SWIPE := 551
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_LEFT_PAD_CLICK := 552
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_LEFT_PAD_DPAD_NORTH := 553
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_LEFT_PAD_DPAD_SOUTH := 554
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_LEFT_PAD_DPAD_WEST := 555
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_LEFT_PAD_DPAD_EAST := 556
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RIGHT_PAD_TOUCH := 557
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RIGHT_PAD_SWIPE := 558
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RIGHT_PAD_CLICK := 559
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RIGHT_PAD_DPAD_NORTH := 560
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RIGHT_PAD_DPAD_SOUTH := 561
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RIGHT_PAD_DPAD_WEST := 562
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RIGHT_PAD_DPAD_EAST := 563
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_L2_SOFTPULL := 564
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_L2 := 565
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_R2_SOFTPULL := 566
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_R2 := 567
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_LEFT_STICK_MOVE := 568
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_L3 := 569
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_LEFT_STICK_DPAD_NORTH := 570
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_LEFT_STICK_DPAD_SOUTH := 571
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_LEFT_STICK_DPAD_WEST := 572
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_LEFT_STICK_DPAD_EAST := 573
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_LEFT_STICK_TOUCH := 574
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RIGHT_STICK_MOVE := 575
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_R3 := 576
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RIGHT_STICK_DPAD_NORTH := 577
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RIGHT_STICK_DPAD_SOUTH := 578
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RIGHT_STICK_DPAD_WEST := 579
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RIGHT_STICK_DPAD_EAST := 580
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RIGHT_STICK_TOUCH := 581
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_L4 := 582
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_R4 := 583
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_L5 := 584
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_R5 := 585
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_DPAD_MOVE := 586
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_DPAD_NORTH := 587
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_DPAD_SOUTH := 588
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_DPAD_WEST := 589
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_DPAD_EAST := 590
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_GYRO_MOVE := 591
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_GYRO_PITCH := 592
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_GYRO_YAW := 593
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_GYRO_ROLL := 594
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_LGRIP := 595
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RGRIP := 596
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED1 := 597
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED2 := 598
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED3 := 599
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED4 := 600
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED5 := 601
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED6 := 602
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED7 := 603
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED8 := 604
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED9 := 605
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED10 := 606
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED11 := 607
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED12 := 608
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED13 := 609
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED14 := 610
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED15 := 611
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED16 := 612
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED17 := 613
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED18 := 614
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED19 := 615
const INPUT_ACTION_ORIGIN_STEAMCONTROLLER2026_RESERVED20 := 616
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_A_CLICK := 617
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_A_TOUCH := 618
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_B_CLICK := 619
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_B_TOUCH := 620
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_X_CLICK := 621
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_X_TOUCH := 622
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_Y_CLICK := 623
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_Y_TOUCH := 624
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_TRIGGER_PULL := 625
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_TRIGGER_CLICK := 626
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_TRIGGER_TOUCH := 627
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_TRIGGER_PULL := 628
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_TRIGGER_CLICK := 629
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_TRIGGER_TOUCH := 630
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_MENU_CLICK := 631
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_MENU_TOUCH := 632
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_VIEW_CLICK := 633
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_VIEW_TOUCH := 634
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_BUMPER_CLICK := 635
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_BUMPER_TOUCH := 636
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_BUMPER_CLICK := 637
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_BUMPER_TOUCH := 638
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_STICK_MOVE := 639
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_STICK_CLICK := 640
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_STICK_DPAD_NORTH := 641
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_STICK_DPAD_SOUTH := 642
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_STICK_DPAD_WEST := 643
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_STICK_DPAD_EAST := 644
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_STICK_TOUCH := 645
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_STICK_MOVE := 646
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_STICK_CLICK := 647
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_STICK_DPAD_NORTH := 648
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_STICK_DPAD_SOUTH := 649
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_STICK_DPAD_WEST := 650
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_STICK_DPAD_EAST := 651
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_STICK_TOUCH := 652
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_DPAD_MOVE := 653
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_DPAD_NORTH := 654
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_DPAD_SOUTH := 655
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_DPAD_WEST := 656
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_DPAD_EAST := 657
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_DPAD_TOUCH := 658
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_GYRO_MOVE := 659
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_GYRO_PITCH := 660
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_GYRO_YAW := 661
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_GYRO_ROLL := 662
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_GYRO_MOVE := 663
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_GYRO_PITCH := 664
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_GYRO_YAW := 665
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_GYRO_ROLL := 666
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_GRIP_PULL := 667
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_GRIP_CLICK := 668
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_GRIP_TOUCH := 669
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_GRIP_PULL := 670
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_GRIP_CLICK := 671
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_GRIP_TOUCH := 672
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_LEFT_THUMBREST_TOUCH := 673
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RIGHT_THUMBREST_TOUCH := 674
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED1 := 675
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED2 := 676
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED3 := 677
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED4 := 678
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED5 := 679
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED6 := 680
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED7 := 681
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED8 := 682
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED9 := 683
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED10 := 684
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED11 := 685
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED12 := 686
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED13 := 687
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED14 := 688
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED15 := 689
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED16 := 690
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED17 := 691
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED18 := 692
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED19 := 693
const INPUT_ACTION_ORIGIN_STEAMFRAMECONTROLLER_RESERVED20 := 694
const INPUT_ACTION_ORIGIN_COUNT := 695
const INPUT_ACTION_ORIGIN_MAXIMUM_POSSIBLE_VALUE := 32767
const INPUT_CONFIGURATION_ENABLE_TYPE_NONE := 0
const INPUT_CONFIGURATION_ENABLE_TYPE_PLAYSTATION := 1
const INPUT_CONFIGURATION_ENABLE_TYPE_XBOX := 2
const INPUT_CONFIGURATION_ENABLE_TYPE_GENERIC := 4
const INPUT_CONFIGURATION_ENABLE_TYPE_SWITCH := 8
const INPUT_GLYPH_SIZE_SMALL := 0
const INPUT_GLYPH_SIZE_MEDIUM := 1
const INPUT_GLYPH_SIZE_LARGE := 2
const INPUT_GLYPH_SIZE_COUNT := 3
const INPUT_GLYPH_STYLE_KNOCKOUT := 0
const INPUT_GLYPH_STYLE_LIGHT := 1
const INPUT_GLYPH_STYLE_DARK := 2
const INPUT_GLYPH_STYLE_NEUTRAL_COLOR_ABXY := 16
const INPUT_GLYPH_STYLE_SOLID_ABXY := 32
const INPUT_LED_FLAG_SET_COLOR := 0
const INPUT_LED_FLAG_RESTORE_USER_DEFAULT := 1
const INPUT_SOURCE_MODE_NONE := 0
const INPUT_SOURCE_MODE_DPAD := 1
const INPUT_SOURCE_MODE_BUTTONS := 2
const INPUT_SOURCE_MODE_FOUR_BUTTONS := 3
const INPUT_SOURCE_MODE_ABSOLUTE_MOUSE := 4
const INPUT_SOURCE_MODE_RELATIVE_MOUSE := 5
const INPUT_SOURCE_MODE_JOYSTICK_MOVE := 6
const INPUT_SOURCE_MODE_JOYSTICK_MOUSE := 7
const INPUT_SOURCE_MODE_JOYSTICK_CAMERA := 8
const INPUT_SOURCE_MODE_SCROLL_WHEEL := 9
const INPUT_SOURCE_MODE_TRIGGER := 10
const INPUT_SOURCE_MODE_TOUCH_MENU := 11
const INPUT_SOURCE_MODE_MOUSE_JOYSTICK := 12
const INPUT_SOURCE_MODE_MOUSE_REGION := 13
const INPUT_SOURCE_MODE_RADIAL_MENU := 14
const INPUT_SOURCE_MODE_SINGLE_BUTTON := 15
const INPUT_SOURCE_MODE_SWITCH := 16
const INPUT_TYPE_UNKNOWN := 0
const INPUT_TYPE_STEAM_CONTROLLER := 1
const INPUT_TYPE_XBOX360_CONTROLLER := 2
const INPUT_TYPE_XBOXONE_CONTROLLER := 3
const INPUT_TYPE_GENERIC_XINPUT := 4
const INPUT_TYPE_PS4_CONTROLLER := 5
const INPUT_TYPE_APPLE_MFI_CONTROLLER := 6
const INPUT_TYPE_ANDROID_CONTROLLER := 7
const INPUT_TYPE_SWITCH_JOYCON_PAIR := 8
const INPUT_TYPE_SWITCH_JOYCON_SINGLE := 9
const INPUT_TYPE_SWITCH_PRO_CONTROLLER := 10
const INPUT_TYPE_MOBILE_TOUCH := 11
const INPUT_TYPE_PS3_CONTROLLER := 12
const INPUT_TYPE_PS5_CONTROLLER := 13
const INPUT_TYPE_STEAM_DECK_CONTROLLER := 14
const INPUT_TYPE_STEAM_OS_HANDHELD := 15
const INPUT_TYPE_SWITCH2_PRO_CONTROLLER := 16
const INPUT_TYPE_STEAM_CONTROLLER2 := 17
const INPUT_TYPE_STEAM_FRAME_CONTROLLER_PAIR := 18
const INPUT_TYPE_COUNT := 19
const INPUT_TYPE_MAXIMUM_POSSIBLE_VALUE := 255
const IP_TYPE_IPV4 := 0
const IP_TYPE_IPV6 := 1
const IPV6_CONNECTIVITY_PROTOCOL_INVALID := 0
const IPV6_CONNECTIVITY_PROTOCOL_HTTP := 1
const IPV6_CONNECTIVITY_PROTOCOL_UDP := 2
const IPV6_CONNECTIVITY_STATE_UNKNOWN := 0
const IPV6_CONNECTIVITY_STATE_GOOD := 1
const IPV6_CONNECTIVITY_STATE_BAD := 2
const STEAM_ITEM_NO_TRADE := 1
const STEAM_ITEM_REMOVED := 256
const STEAM_ITEM_CONSUMED := 512
const ITEM_PREVIEW_TYPE_IMAGE := 0
const ITEM_PREVIEW_TYPE_YOUTUBE_VIDEO := 1
const ITEM_PREVIEW_TYPE_SKETCHFAB := 2
const ITEM_PREVIEW_TYPE_ENVIRONMENTMAP_HORIZONTAL_CROSS := 3
const ITEM_PREVIEW_TYPE_ENVIRONMENTMAP_LAT_LONG := 4
const ITEM_PREVIEW_TYPE_CLIP := 5
const ITEM_PREVIEW_TYPE_RESERVED_MAX := 255
const ITEM_STATE_NONE := 0
const ITEM_STATE_SUBSCRIBED := 1
const ITEM_STATE_LEGACY_ITEM := 2
const ITEM_STATE_INSTALLED := 4
const ITEM_STATE_NEEDS_UPDATE := 8
const ITEM_STATE_DOWNLOADING := 16
const ITEM_STATE_DOWNLOAD_PENDING := 32
const ITEM_STATE_DISABLED_LOCALLY := 64
const ITEM_STATISTIC_NUM_SUBSCRIPTIONS := 0
const ITEM_STATISTIC_NUM_FAVORITES := 1
const ITEM_STATISTIC_NUM_FOLLOWERS := 2
const ITEM_STATISTIC_NUM_UNIQUE_SUBSCRIPTIONS := 3
const ITEM_STATISTIC_NUM_UNIQUE_FAVORITES := 4
const ITEM_STATISTIC_NUM_UNIQUE_FOLLOWERS := 5
const ITEM_STATISTIC_NUM_UNIQUE_WEBSITE_VIEWS := 6
const ITEM_STATISTIC_REPORT_SCORE := 7
const ITEM_STATISTIC_NUM_SECONDS_PLAYED := 8
const ITEM_STATISTIC_NUM_PLAYTIME_SESSIONS := 9
const ITEM_STATISTIC_NUM_COMMENTS := 10
const ITEM_STATISTIC_NUM_SECONDS_PLAYED_DURING_TIME_PERIOD := 11
const ITEM_STATISTIC_NUM_PLAYTIME_SESSIONS_DURING_TIME_PERIOD := 12
const ITEM_UPDATE_STATUS_INVALID := 0
const ITEM_UPDATE_STATUS_PREPARING_CONFIG := 1
const ITEM_UPDATE_STATUS_PREPARING_CONTENT := 2
const ITEM_UPDATE_STATUS_UPLOADING_CONTENT := 3
const ITEM_UPDATE_STATUS_UPLOADING_PREVIEW_FILE := 4
const ITEM_UPDATE_STATUS_COMMITTING_CHANGES := 5
const LEADERBOARD_DATA_REQUEST_GLOBAL := 0
const LEADERBOARD_DATA_REQUEST_GLOBAL_AROUND_USER := 1
const LEADERBOARD_DATA_REQUEST_FRIENDS := 2
const LEADERBOARD_DATA_REQUEST_USERS := 3
const LEADERBOARD_DISPLAY_TYPE_NONE := 0
const LEADERBOARD_DISPLAY_TYPE_NUMERIC := 1
const LEADERBOARD_DISPLAY_TYPE_TIME_SECONDS := 2
const LEADERBOARD_DISPLAY_TYPE_TIME_MILLISECONDS := 3
const LEADERBOARD_SORT_METHOD_NONE := 0
const LEADERBOARD_SORT_METHOD_ASCENDING := 1
const LEADERBOARD_SORT_METHOD_DESCENDING := 2
const LEADERBOARD_UPLOAD_SCORE_METHOD_NONE := 0
const LEADERBOARD_UPLOAD_SCORE_METHOD_KEEP_BEST := 1
const LEADERBOARD_UPLOAD_SCORE_METHOD_FORCE_UPDATE := 2
const LOBBY_COMPARISON_EQUAL_TO_OR_LESS_THAN := -2
const LOBBY_COMPARISON_LESS_THAN := -1
const LOBBY_COMPARISON_EQUAL := 0
const LOBBY_COMPARISON_GREATER_THAN := 1
const LOBBY_COMPARISON_EQUAL_TO_GREATER_THAN := 2
const LOBBY_COMPARISON_NOT_EQUAL := 3
const LOBBY_DISTANCE_FILTER_CLOSE := 0
const LOBBY_DISTANCE_FILTER_DEFAULT := 1
const LOBBY_DISTANCE_FILTER_FAR := 2
const LOBBY_DISTANCE_FILTER_WORLDWIDE := 3
const LOBBY_TYPE_PRIVATE := 0
const LOBBY_TYPE_FRIENDS_ONLY := 1
const LOBBY_TYPE_PUBLIC := 2
const LOBBY_TYPE_INVISIBLE := 3
const LOBBY_TYPE_PRIVATE_UNIQUE := 4
const LOCAL_FILE_CHANGE_INVALID := 0
const LOCAL_FILE_CHANGE_FILE_UPDATED := 1
const LOCAL_FILE_CHANGE_FILE_DELETED := 2
const MARKET_NOT_ALLOWED_REASON_NONE := 0
const MARKET_NOT_ALLOWED_REASON_TEMPORARY_FAILURE := 1
const MARKET_NOT_ALLOWED_REASON_ACCOUNT_DISABLED := 2
const MARKET_NOT_ALLOWED_REASON_ACCOUNT_LOCKED_DOWN := 4
const MARKET_NOT_ALLOWED_REASON_ACCOUNT_LIMITED := 8
const MARKET_NOT_ALLOWED_REASON_TRADE_BANNED := 16
const MARKET_NOT_ALLOWED_REASON_ACCOUNT_NOT_TRUSTED := 32
const MARKET_NOT_ALLOWED_REASON_STEAM_GUARD_NOT_ENABLED := 64
const MARKET_NOT_ALLOWED_REASON_STEAM_GUARD_ONLY_RECENTLY_ENABLED := 128
const MARKET_NOT_ALLOWED_REASON_RECENT_PASSWORD_RESET := 256
const MARKET_NOT_ALLOWED_REASON_NEW_PAYMENT_METHOD := 512
const MARKET_NOT_ALLOWED_REASON_INVALID_COOKIE := 1024
const MARKET_NOT_ALLOWED_REASON_USING_NEW_DEVICE := 2048
const MARKET_NOT_ALLOWED_REASON_RECENT_SELF_REFUND := 4096
const MARKET_NOT_ALLOWED_REASON_NEW_PAYMENT_METHOD_CANNOT_BE_VERIFIED := 8192
const MARKET_NOT_ALLOWED_REASON_NO_RECENT_PURCHASES := 16384
const MARKET_NOT_ALLOWED_REASON_ACCEPTED_WALLET_GIFT := 32768
const MARKET_NOT_ALLOWED_REASON_TRADE_COOLDOWN := 65536
const SERVER_RESPONDED := 0
const SERVER_FAILED_TO_RESPOND := 1
const NO_SERVERS_LISTED_ON_MASTER_SERVER := 2
const NETWORKING_AVAILABILITY_CANNOT_TRY := -102
const NETWORKING_AVAILABILITY_FAILED := -101
const NETWORKING_AVAILABILITY_PREVIOUSLY := -100
const NETWORKING_AVAILABILITY_RETRYING := -10
const NETWORKING_AVAILABILITY_NEVER_TRIED := 1
const NETWORKING_AVAILABILITY_WAITING := 2
const NETWORKING_AVAILABILITY_ATTEMPTING := 3
const NETWORKING_AVAILABILITY_CURRENT := 100
const NETWORKING_AVAILABILITY_UNKNOWN := 0
const NETWORKING_AVAILABILITY_FORCE_32BIT := 2147483647
const NETWORKING_CONFIG_TYPE_INT32 := 1
const NETWORKING_CONFIG_TYPE_INT64 := 2
const NETWORKING_CONFIG_TYPE_FLOAT := 3
const NETWORKING_CONFIG_TYPE_STRING := 4
const NETWORKING_CONFIG_TYPE_FUNCTION_PTR := 5
const NETWORKING_CONFIG_TYPE_FORCE_32BIT := 2147483647
const NETWORKING_CONFIG_SCOPE_GLOBAL := 1
const NETWORKING_CONFIG_SCOPE_SOCKETS_INTERFACE := 2
const NETWORKING_CONFIG_SCOPE_LISTEN_SOCKET := 3
const NETWORKING_CONFIG_SCOPE_CONNECTION := 4
const NETWORKING_CONFIG_SCOPE_FORCE_32BIT := 2147483647
const NETWORKING_CONFIG_INVALID := 0
const NETWORKING_CONFIG_FAKE_PACKET_LOSS_SEND := 2
const NETWORKING_CONFIG_FAKE_PACKET_LOSS_RECV := 3
const NETWORKING_CONFIG_FAKE_PACKET_LAG_SEND := 4
const NETWORKING_CONFIG_FAKE_PACKET_LAG_RECV := 5
const NETWORKING_CONFIG_FAKE_PACKET_JITTER_SEND_AVG := 53
const NETWORKING_CONFIG_FAKE_PACKET_JITTER_SEND_MAX := 54
const NETWORKING_CONFIG_FAKE_PACKET_JITTER_SEND_PCT := 55
const NETWORKING_CONFIG_FAKE_PACKET_JITTER_RECV_AVG := 56
const NETWORKING_CONFIG_FAKE_PACKET_JITTER_RECV_MAX := 57
const NETWORKING_CONFIG_FAKE_PACKET_JITTER_RECV_PCT := 58
const NETWORKING_CONFIG_FAKE_PACKET_REORDER_SEND := 6
const NETWORKING_CONFIG_FAKE_PACKET_REORDER_RECV := 7
const NETWORKING_CONFIG_FAKE_PACKET_REORDER_TIME := 8
const NETWORKING_CONFIG_FAKE_PACKET_DUP_SEND := 26
const NETWORKING_CONFIG_FAKE_PACKET_DUP_REVC := 27
const NETWORKING_CONFIG_FAKE_PACKET_DUP_TIME_MAX := 28
const NETWORKING_CONFIG_PACKET_TRACE_MAX_BYTES := 41
const NETWORKING_CONFIG_FAKE_RATE_LIMIT_SEND_RATE := 42
const NETWORKING_CONFIG_FAKE_RATE_LIMIT_SEND_BURST := 43
const NETWORKING_CONFIG_FAKE_RATE_LIMIT_RECV_RATE := 44
const NETWORKING_CONFIG_FAKE_RATE_LIMIT_RECV_BURST := 45
const NETWORKING_CONFIG_OUT_OF_ORDER_CORRECTION_WINDOW_MICROSECONDS := 51
const NETWORKING_CONFIG_CONNECTION_USER_DATA := 40
const NETWORKING_CONFIG_TIMEOUT_INITIAL := 24
const NETWORKING_CONFIG_TIMEOUT_CONNECTED := 25
const NETWORKING_CONFIG_SEND_BUFFER_SIZE := 9
const NETWORKING_CONFIG_RECV_BUFFER_SIZE := 47
const NETWORKING_CONFIG_RECV_BUFFER_MESSAGES := 48
const NETWORKING_CONFIG_RECV_MAX_MESSAGE_SIZE := 49
const NETWORKING_CONFIG_RECV_MAX_SEGMENTS_PER_PACKET := 50
const NETWORKING_CONFIG_SEND_RATE_MIN := 10
const NETWORKING_CONFIG_SEND_RATE_MAX := 11
const NETWORKING_CONFIG_NAGLE_TIME := 12
const NETWORKING_CONFIG_IP_ALLOW_WITHOUT_AUTH := 23
const NETWORKING_CONFIG_IP_LOCAL_HOST_ALLOW_WITHOUT_AUTH := 52
const NETWORKING_CONFIG_MTU_PACKET_SIZE := 32
const NETWORKING_CONFIG_MTU_DATA_SIZE := 33
const NETWORKING_CONFIG_UNENCRYPTED := 34
const NETWORKING_CONFIG_SYMMETRIC_CONNECT := 37
const NETWORKING_CONFIG_LOCAL_VIRTUAL_PORT := 38
const NETWORKING_CONFIG_DUAL_WIFI_ENABLE := 39
const NETWORKING_CONFIG_ENABLE_DIAGNOSTICS_UI := 46
const NETWORKING_CONFIG_SEND_TIME_SINCE_PREVIOUS_PACKET := 59
const NETWORKING_CONFIG_SDR_CLIENT_CONSEC_PING_TIMEOUT_FAIL_INITIAL := 19
const NETWORKING_CONFIG_SDR_CLIENT_CONSEC_PING_TIMEOUT_FAIL := 20
const NETWORKING_CONFIG_SDR_CLIENT_MIN_PINGS_BEFORE_PING_ACCURATE := 21
const NETWORKING_CONFIG_SDR_CLIENT_SINGLE_SOCKET := 22
const NETWORKING_CONFIG_SDR_CLIENT_FORCE_RELAY_CLUSTER := 29
const NETWORKING_CONFIG_SDR_CLIENT_DEV_TICKET := 30
const NETWORKING_CONFIG_SDR_CLIENT_FORCE_PROXY_ADDR := 31
const NETWORKING_CONFIG_SDR_CLIENT_FAKE_CLUSTER_PING := 36
const NETWORKING_CONFIG_SDR_CLIENT_LIMIT_PING_PROBES_TO_NEAREST_N := 60
const NETWORKING_CONFIG_LOG_LEVEL_ACK_RTT := 13
const NETWORKING_CONFIG_LOG_LEVEL_PACKET_DECODE := 14
const NETWORKING_CONFIG_LOG_LEVEL_MESSAGE := 15
const NETWORKING_CONFIG_LOG_LEVEL_PACKET_GAPS := 16
const NETWORKING_CONFIG_LOG_LEVEL_P2P_RENDEZVOUS := 17
const NETWORKING_CONFIG_LOG_LEVEL_SRD_RELAY_PINGS := 18
const NETWORKING_CONFIG_CALLBACK_CONNECTION_STATUS_CHANGED := 201
const NETWORKING_CONFIG_CALLBACK_AUTH_STATUS_CHANGED := 202
const NETWORKING_CONFIG_CALLBACK_RELAY_NETWORK_STATUS_CHANGED := 203
const NETWORKING_CONFIG_CALLBACK_MESSAGE_SESSION_REQUEST := 204
const NETWORKING_CONFIG_CALLBACK_MESSAGES_SESSION_FAILED := 205
const NETWORKING_CONFIG_CALLBACK_CREATE_CONNECTION_SIGNALING := 206
const NETWORKING_CONFIG_CALLBACK_FAKE_IP_RESULT := 207
const NETWORKING_CONFIG_P2P_STUN_SERVER_LIST := 103
const NETWORKING_CONFIG_P2P_TRANSPORT_ICE_ENABLE := 104
const NETWORKING_CONFIG_P2P_TRANSPORT_ICE_PENALTY := 105
const NETWORKING_CONFIG_P2P_TRANSPORT_SDR_PENALTY := 106
const NETWORKING_CONFIG_P2P_TURN_SERVER_LIST := 107
const NETWORKING_CONFIG_P2P_TURN_USER_LIST := 108
const NETWORKING_CONFIG_P2P_TURN_PASS_LIST := 109
const NETWORKING_CONFIG_P2P_TRANSPORT_ICE_IMPLEMENTATION := 110
const NETWORKING_CONFIG_ECN := 999
const NETWORKING_CONFIG_VALUE_FORCE32BIT := 2147483647
const CONNECTION_END_INVALID := 0
const CONNECTION_END_APP_MIN := 1000
const CONNECTION_END_APP_GENERIC := 1000
const CONNECTION_END_APP_MAX := 1999
const CONNECTION_END_APP_EXCEPTION_MIN := 2000
const CONNECTION_END_APP_EXCEPTION_GENERIC := 2000
const CONNECTION_END_APP_EXCEPTION_MAX := 2999
const CONNECTION_END_LOCAL_MIN := 3000
const CONNECTION_END_LOCAL_OFFLINE_MODE := 3001
const CONNECTION_END_LOCAL_MANY_RELAY_CONNECTIVITY := 3002
const CONNECTION_END_LOCAL_HOSTED_SERVER_PRIMARY_RELAY := 3003
const CONNECTION_END_LOCAL_NETWORK_CONFIG := 3004
const CONNECTION_END_LOCAL_RIGHTS := 3005
const CONNECTION_END_NO_PUBLIC_ADDRESS := 3006
const CONNECTION_END_LOCAL_MAX := 3999
const CONNECTION_END_REMOVE_MIN := 4000
const CONNECTION_END_REMOTE_TIMEOUT := 4001
const CONNECTION_END_REMOTE_BAD_CRYPT := 4002
const CONNECTION_END_REMOTE_BAD_CERT := 4003
const CONNECTION_END_BAD_PROTOCOL_VERSION := 4006
const CONNECTION_END_REMOTE_P2P_ICE_NO_PUBLIC_ADDRESSES := 4007
const CONNECTION_END_REMOTE_MAX := 4999
const CONNECTION_END_MISC_MIN := 5000
const CONNECTION_END_MISC_GENERIC := 5001
const CONNECTION_END_MISC_INTERNAL_ERROR := 5002
const CONNECTION_END_MISC_TIMEOUT := 5003
const CONNECTION_END_MISC_STEAM_CONNECTIVITY := 5005
const CONNECTION_END_MISC_NO_RELAY_SESSIONS_TO_CLIENT := 5006
const CONNECTION_END_MISC_P2P_RENDEZVOUS := 5008
const CONNECTION_END_MISC_P2P_NAT_FIREWALL := 5009
const CONNECTION_END_MISC_PEER_SENT_NO_CONNECTION := 5010
const CONNECTION_END_MISC_MAX := 5999
const CONNECTION_END_FORCE32BIT := 2147483647
const CONNECTION_STATE_NONE := 0
const CONNECTION_STATE_CONNECTING := 1
const CONNECTION_STATE_FINDING_ROUTE := 2
const CONNECTION_STATE_CONNECTED := 3
const CONNECTION_STATE_CLOSED_BY_PEER := 4
const CONNECTION_STATE_PROBLEM_DETECTED_LOCALLY := 5
const CONNECTION_STATE_FIN_WAIT := -1
const CONNECTION_STATE_LINGER := -2
const CONNECTION_STATE_DEAD := -3
const CONNECTION_STATE_FORCE_32BIT := 2147483647
const FAKE_IP_TYPE_INVALID := 0
const FAKE_IP_TYPE_NOT_FAKE := 1
const FAKE_IP_TYPE_GLOBAL_IPV4 := 2
const FAKE_IP_TYPE_LOCAL_IPV4 := 3
const FAKE_IP_TYPE_FORCE32BIT := 2147483647
const NETWORKING_GET_CONFIG_VALUE_BAD_VALUE := -1
const NETWORKING_GET_CONFIG_VALUE_BAD_SCOPE_OBJ := -2
const NETWORKING_GET_CONFIG_VALUE_BUFFER_TOO_SMALL := -3
const NETWORKING_GET_CONFIG_VALUE_OK := 1
const NETWORKING_GET_CONFIG_VALUE_OK_INHERITED := 2
const NETWORKING_GET_CONFIG_VALUE_FORCE_32BIT := 2147483647
const IDENTITY_TYPE_INVALID := 0
const IDENTITY_TYPE_STEAMID := 16
const IDENTITY_TYPE_IP_ADDRESS := 1
const IDENTITY_TYPE_GENERIC_STRING := 2
const IDENTITY_TYPE_GENERIC_BYTES := 3
const IDENTITY_TYPE_UNKNOWN_TYPE := 4
const IDENTITY_TYPE_XBOX_PAIRWISE := 17
const IDENTITY_TYPE_SONY_PSN := 18
const IDENTITY_TYPE_FORCE_32BIT := 2147483647
const NETWORKING_SOCKET_DEBUG_OUTPUT_TYPE_NONE := 0
const NETWORKING_SOCKET_DEBUG_OUTPUT_TYPE_BUG := 1
const NETWORKING_SOCKET_DEBUG_OUTPUT_TYPE_ERROR := 2
const NETWORKING_SOCKET_DEBUG_OUTPUT_TYPE_IMPORTANT := 3
const NETWORKING_SOCKET_DEBUG_OUTPUT_TYPE_WARNING := 4
const NETWORKING_SOCKET_DEBUG_OUTPUT_TYPE_MSG := 5
const NETWORKING_SOCKET_DEBUG_OUTPUT_TYPE_VERBOSE := 6
const NETWORKING_SOCKET_DEBUG_OUTPUT_TYPE_DEBUG := 7
const NETWORKING_SOCKET_DEBUG_OUTPUT_TYPE_EVERYTHING := 8
const NETWORKING_SOCKET_DEBUG_OUTPUT_TYPE_FORCE_32BIT := 2147483647
const POSITION_INVALID := -1
const POSITION_TOP_LEFT := 0
const POSITION_TOP_RIGHT := 1
const POSITION_BOTTOM_LEFT := 2
const POSITION_BOTTOM_RIGHT := 3
const OVERLAY_TO_STORE_FLAG_NONE := 0
const OVERLAY_TO_STORE_FLAG_ADD_TO_CART := 1
const OVERLAY_TO_STORE_FLAG_AND_TO_CART_AND_SHOW := 2
const OVERLAY_TO_WEB_PAGE_MODE_DEFAULT := 0
const OVERLAY_TO_WEB_PAGE_MODE_MODAL := 1
const P2P_SEND_UNRELIABLE := 0
const P2P_SEND_UNRELIABLE_NO_DELAY := 1
const P2P_SEND_RELIABLE := 2
const P2P_SEND_RELIABLE_WITH_BUFFERING := 3
const P2P_SESSION_ERROR_NONE := 0
const P2P_SESSION_ERROR_NOT_RUNNING_APP := 1
const P2P_SESSION_ERROR_NO_RIGHTS_TO_APP := 2
const P2P_SESSION_ERROR_DESTINATION_NOT_LOGGED_ON := 3
const P2P_SESSION_ERROR_TIMEOUT := 4
const P2P_SESSION_ERROR_MAX := 5
const FEATURE_INVALID := 0
const FEATURE_STORE := 1
const FEATURE_COMMUNITY := 2
const FEATURE_PROFILE := 3
const FEATURE_FRIENDS := 4
const FEATURE_NEWS := 5
const FEATURE_TRADING := 6
const FEATURE_SETTINGS := 7
const FEATURE_CONSOLE := 8
const FEATURE_BROWSER := 9
const FEATURE_PARENTAL_SETUP := 10
const FEATURE_LIBRARY := 11
const FEATURE_TEST := 12
const FEATURE_SITE_LICENSE := 13
const FEATURE_KIOSK_MODE := 14
const FEATURE_BLOCK_ALWAYS := 15
const FEATURE_DESKTOP := 16
const FEATURE_MAX := 17
const STEAM_PARTY_BEACON_LOCATION_DATA_INVALID := 0
const STEAM_PARTY_BEACON_LOCATION_DATA_NAME := 1
const STEAM_PARTY_BEACON_LOCATION_DATA_URL_SMALL := 2
const STEAM_PARTY_BEACON_LOCATION_DATA_URL_MEDIUM := 3
const STEAM_PARTY_BEACON_LOCATION_DATA_URL_LARGE := 4
const STEAM_PARTY_BEACON_LOCATION_TYPE_INVALID := 0
const STEAM_PARTY_BEACON_LOCATION_TYPE_CHAT_GROUP := 1
const STEAM_PARTY_BEACON_LOCATION_TYPE_MAX := 2
const PERSONA_CHANGE_NAME := 1
const PERSONA_CHANGE_STATUS := 2
const PERSONA_CHANGE_COME_ONLINE := 4
const PERSONA_CHANGE_GONE_OFFLINE := 8
const PERSONA_CHANGE_GAME_PLAYED := 16
const PERSONA_CHANGE_GAME_SERVER := 32
const PERSONA_CHANGE_AVATAR := 64
const PERSONA_CHANGE_JOINED_SOURCE := 128
const PERSONA_CHANGE_LEFT_SOURCE := 256
const PERSONA_CHANGE_RELATIONSHIP_CHANGED := 512
const PERSONA_CHANGE_NAME_FIRST_SET := 1024
const PERSONA_CHANGE_BROADCAST := 2048
const PERSONA_CHANGE_NICKNAME := 4096
const PERSONA_CHANGE_STEAM_LEVEL := 8192
const PERSONA_CHANGE_RICH_PRESENCE := 16384
const PERSONA_STATE_OFFLINE := 0
const PERSONA_STATE_ONLINE := 1
const PERSONA_STATE_BUSY := 2
const PERSONA_STATE_AWAY := 3
const PERSONA_STATE_SNOOZE := 4
const PERSONA_STATE_LOOKING_TO_TRADE := 5
const PERSONA_STATE_LOOKING_TO_PLAY := 6
const PERSONA_STATE_INVISIBLE := 7
const PERSONA_STATE_MAX := 8
const REMOTE_PLAY_INPUT_UNKNOWN := 0
const REMOTE_PLAY_INPUT_MOUSE_MOTION := 1
const REMOTE_PLAY_INPUT_MOUSE_BUTTON_DOWN := 2
const REMOTE_PLAY_INPUT_MOUSE_BUTTON_UP := 3
const REMOTE_PLAY_INPUT_MOUSE_WHEEL := 4
const REMOTE_PLAY_INPUT_KEY_DOWN := 5
const REMOTE_PLAY_INPUT_KEY_UP := 6
const REMOTE_PLAY_KEY_MODIFIER_NONE := 0
const REMOTE_PLAY_KEY_MODIFIER_LEFT_SHIFT := 1
const REMOTE_PLAY_KEY_MODIFIER_RIGHT_SHIFT := 2
const REMOTE_PLAY_KEY_MODIFIER_LEFT_CONTROL := 64
const REMOTE_PLAY_KEY_MODIFIER_RIGHT_CONTROL := 128
const REMOTE_PLAY_KEY_MODIFIER_LEFT_ALT := 256
const REMOTE_PLAY_KEY_MODIFIER_RIGHT_ALT := 512
const REMOTE_PLAY_KEY_MODIFIER_LEFT_GUI := 1024
const REMOTE_PLAY_KEY_MODIFIER_RIGHT_GUI := 2048
const REMOTE_PLAY_KEY_MODIFIER_NUM_LOCK := 4096
const REMOTE_PLAY_KEY_MODIFIER_CAPS_LOCK := 8192
const REMOTE_PLAY_KEY_MODIFIER_MASK := 65535
const REMOTE_PLAY_MOUSE_BUTTON_LEFT := 1
const REMOTE_PLAY_MOUSE_BUTTON_RIGHT := 2
const REMOTE_PLAY_MOUSE_BUTTON_MIDDLE := 16
const REMOTE_PLAY_MOUSE_BUTTON_X1 := 32
const REMOTE_PLAY_MOUSE_BUTTON_X2 := 64
const REMOTE_PLAY_MOUSE_WHEEL_UP := 1
const REMOTE_PLAY_MOUSE_WHEEL_DOWN := 2
const REMOTE_PLAY_MOUSE_WHEEL_LEFT := 3
const REMOTE_PLAY_MOUSE_WHEEL_RIGHT := 4
const REMOTE_PLAYER_SCANCODE_UNKNOWN := 0
const REMOTE_PLAYER_SCANCODE_A := 4
const REMOTE_PLAYER_SCANCODE_B := 5
const REMOTE_PLAYER_SCANCODE_C := 6
const REMOTE_PLAYER_SCANCODE_D := 7
const REMOTE_PLAYER_SCANCODE_E := 8
const REMOTE_PLAYER_SCANCODE_F := 9
const REMOTE_PLAYER_SCANCODE_G := 10
const REMOTE_PLAYER_SCANCODE_H := 11
const REMOTE_PLAYER_SCANCODE_I := 12
const REMOTE_PLAYER_SCANCODE_J := 13
const REMOTE_PLAYER_SCANCODE_K := 14
const REMOTE_PLAYER_SCANCODE_L := 15
const REMOTE_PLAYER_SCANCODE_M := 16
const REMOTE_PLAYER_SCANCODE_N := 17
const REMOTE_PLAYER_SCANCODE_O := 18
const REMOTE_PLAYER_SCANCODE_P := 19
const REMOTE_PLAYER_SCANCODE_Q := 20
const REMOTE_PLAYER_SCANCODE_R := 21
const REMOTE_PLAYER_SCANCODE_S := 22
const REMOTE_PLAYER_SCANCODE_T := 23
const REMOTE_PLAYER_SCANCODE_U := 24
const REMOTE_PLAYER_SCANCODE_V := 25
const REMOTE_PLAYER_SCANCODE_W := 26
const REMOTE_PLAYER_SCANCODE_X := 27
const REMOTE_PLAYER_SCANCODE_Y := 28
const REMOTE_PLAYER_SCANCODE_Z := 29
const REMOTE_PLAYER_SCANCODE_1 := 30
const REMOTE_PLAYER_SCANCODE_2 := 31
const REMOTE_PLAYER_SCANCODE_3 := 32
const REMOTE_PLAYER_SCANCODE_4 := 33
const REMOTE_PLAYER_SCANCODE_5 := 34
const REMOTE_PLAYER_SCANCODE_6 := 35
const REMOTE_PLAYER_SCANCODE_7 := 36
const REMOTE_PLAYER_SCANCODE_8 := 37
const REMOTE_PLAYER_SCANCODE_9 := 38
const REMOTE_PLAYER_SCANCODE_0 := 39
const REMOTE_PLAYER_SCANCODE_RETURN := 40
const REMOTE_PLAYER_SCANCODE_ESCAPE := 41
const REMOTE_PLAYER_SCANCODE_BACKSPACE := 42
const REMOTE_PLAYER_SCANCODE_TAB := 43
const REMOTE_PLAYER_SCANCODE_SPACE := 44
const REMOTE_PLAYER_SCANCODE_MINUS := 45
const REMOTE_PLAYER_SCANCODE_EQUALS := 46
const REMOTE_PLAYER_SCANCODE_LEFT_BRACKET := 47
const REMOTE_PLAYER_SCANCODE_RIGHT_BRACKET := 48
const REMOTE_PLAYER_SCANCODE_BACKSLASH := 49
const REMOTE_PLAYER_SCANCODE_SEMICOLON := 51
const REMOTE_PLAYER_SCANCODE_APOSTROPHE := 52
const REMOTE_PLAYER_SCANCODE_GRAVE := 53
const REMOTE_PLAYER_SCANCODE_COMMA := 54
const REMOTE_PLAYER_SCANCODE_PERIOD := 55
const REMOTE_PLAYER_SCANCODE_SLASH := 56
const REMOTE_PLAYER_SCANCODE_CAPSLOCK := 57
const REMOTE_PLAYER_SCANCODE_F1 := 58
const REMOTE_PLAYER_SCANCODE_F2 := 59
const REMOTE_PLAYER_SCANCODE_F3 := 60
const REMOTE_PLAYER_SCANCODE_F4 := 61
const REMOTE_PLAYER_SCANCODE_F5 := 62
const REMOTE_PLAYER_SCANCODE_F6 := 63
const REMOTE_PLAYER_SCANCODE_F7 := 64
const REMOTE_PLAYER_SCANCODE_F8 := 65
const REMOTE_PLAYER_SCANCODE_F9 := 66
const REMOTE_PLAYER_SCANCODE_F10 := 67
const REMOTE_PLAYER_SCANCODE_F11 := 68
const REMOTE_PLAYER_SCANCODE_F12 := 69
const REMOTE_PLAYER_SCANCODE_INSERT := 73
const REMOTE_PLAYER_SCANCODE_HOME := 74
const REMOTE_PLAYER_SCANCODE_PAGE_UP := 75
const REMOTE_PLAYER_SCANCODE_DELETE := 76
const REMOTE_PLAYER_SCANCODE_END := 77
const REMOTE_PLAYER_SCANCODE_PAGE_DOWN := 78
const REMOTE_PLAYER_SCANCODE_RIGHT := 79
const REMOTE_PLAYER_SCANCODE_LEFT := 80
const REMOTE_PLAYER_SCANCODE_DOWN := 81
const REMOTE_PLAYER_SCANCODE_UP := 82
const REMOTE_PLAYER_SCANCODE_KEYPAD_DIVIDE := 84
const REMOTE_PLAYER_SCANCODE_KEYPAD_MULTIPLY := 85
const REMOTE_PLAYER_SCANCODE_KEYPAD_MINUS := 86
const REMOTE_PLAYER_SCANCODE_KEYPAD_PLUS := 87
const REMOTE_PLAYER_SCANCODE_KEYPAD_ENTER := 88
const REMOTE_PLAYER_SCANCODE_KEYPAD_1 := 89
const REMOTE_PLAYER_SCANCODE_KEYPAD_2 := 90
const REMOTE_PLAYER_SCANCODE_KEYPAD_3 := 91
const REMOTE_PLAYER_SCANCODE_KEYPAD_4 := 92
const REMOTE_PLAYER_SCANCODE_KEYPAD_5 := 93
const REMOTE_PLAYER_SCANCODE_KEYPAD_6 := 94
const REMOTE_PLAYER_SCANCODE_KEYPAD_7 := 95
const REMOTE_PLAYER_SCANCODE_KEYPAD_8 := 96
const REMOTE_PLAYER_SCANCODE_KEYPAD_9 := 97
const REMOTE_PLAYER_SCANCODE_KEYPAD_0 := 98
const REMOTE_PLAYER_SCANCODE_KEYPAD_PERIOD := 99
const REMOTE_PLAYER_SCANCODE_LEFT_CONTROL := 224
const REMOTE_PLAYER_SCANCODE_LEFT_SHIFT := 225
const REMOTE_PLAYER_SCANCODE_LEFT_ALT := 226
const REMOTE_PLAYER_SCANCODE_LEFT_GUI := 227
const REMOTE_PLAYER_SCANCODE_RIGHT_CONTROL := 228
const REMOTE_PLAYER_SCANCODE_RIGHT_SHIFT := 229
const REMOTE_PLAYER_SCANCODE_RIGHT_ALT := 230
const REMOTE_PLAYER_SCANCODE_RIGHT_GUI := 231
const REMOTE_STORAGE_PLATFORM_NONE := 0
const REMOTE_STORAGE_PLATFORM_WINDOWS := 1
const REMOTE_STORAGE_PLATFORM_OSX := 2
const REMOTE_STORAGE_PLATFORM_PS3 := 4
const REMOTE_STORAGE_PLATFORM_LINUX := 8
const REMOTE_STORAGE_PLATFORM_SWITCH := 16
const REMOTE_STORAGE_PLATFORM_ANDROID := 32
const REMOTE_STORAGE_PLATFORM_IOS := 64
const REMOTE_STORAGE_PLATFORM_ALL := 4294967295
const REMOTE_STORAGE_PUBLISHED_VISIBILITY_PUBLIC := 0
const REMOTE_STORAGE_PUBLISHED_VISIBILITY_FRIENDS_ONLY := 1
const REMOTE_STORAGE_PUBLISHED_VISIBILITY_PRIVATE := 2
const REMOTE_STORAGE_PUBLISHED_VISIBILITY_UNLISTED := 3
const RESULT_NONE := 0
const RESULT_OK := 1
const RESULT_FAIL := 2
const RESULT_NO_CONNECTION := 3
const RESULT_INVALID_PASSWORD := 5
const RESULT_LOGGED_IN_ELSEWHERE := 6
const RESULT_INVALID_PROTOCOL_VER := 7
const RESULT_INVALID_PARAM := 8
const RESULT_FILE_NOT_FOUND := 9
const RESULT_BUSY := 10
const RESULT_INVALID_STATE := 11
const RESULT_INVALID_NAME := 12
const RESULT_INVALID_EMAIL := 13
const RESULT_DUPLICATE_NAME := 14
const RESULT_ACCESS_DENIED := 15
const RESULT_TIMEOUT := 16
const RESULT_BANNED := 17
const RESULT_ACCOUNT_NOT_FOUND := 18
const RESULT_INVALID_STEAMID := 19
const RESULT_SERVICE_UNAVAILABLE := 20
const RESULT_NOT_LOGGED_ON := 21
const RESULT_PENDING := 22
const RESULT_ENCRYPTION_FAILURE := 23
const RESULT_INSUFFICIENT_PRIVILEGE := 24
const RESULT_LIMIT_EXCEEDED := 25
const RESULT_REVOKED := 26
const RESULT_EXPIRED := 27
const RESULT_ALREADY_REDEEMED := 28
const RESULT_DUPLICATE_REQUEST := 29
const RESULT_ALREADY_OWNED := 30
const RESULT_IP_NOT_FOUND := 31
const RESULT_PERSIST_FAILED := 32
const RESULT_LOCKING_FAILED := 33
const RESULT_LOG_ON_SESSION_REPLACED := 34
const RESULT_CONNECT_FAILED := 35
const RESULT_HANDSHAKE_FAILED := 36
const RESULT_IO_FAILURE := 37
const RESULT_REMOTE_DISCONNECT := 38
const RESULT_SHOPPING_CART_NOT_FOUND := 39
const RESULT_BLOCKED := 40
const RESULT_IGNORED := 41
const RESULT_NO_MATCH := 42
const RESULT_ACCOUNT_DISABLED := 43
const RESULT_SERVICE_READ_ONLY := 44
const RESULT_ACCOUNT_NOT_FEATURED := 45
const RESULT_ADMINISTRATOR_OK := 46
const RESULT_CONTENT_VERSION := 47
const RESULT_TRY_ANOTHER_CM := 48
const RESULT_PASSWORD_REQUIRED_TO_KICK_SESSION := 49
const RESULT_ALREADY_LOGGED_IN_ELSEWHERE := 50
const RESULT_SUSPENDED := 51
const RESULT_CANCELLED := 52
const RESULT_DATA_CORRUPTION := 53
const RESULT_DISK_FULL := 54
const RESULT_REMOTE_CALL_FAILED := 55
const RESULT_PASSWORD_UNSET := 56
const RESULT_EXTERNAL_ACCOUNT_UNLINKED := 57
const RESULT_PSN_TICKET_INVALID := 58
const RESULT_EXTERNAL_ACCOUNT_ALREADY_LINKED := 59
const RESULT_REMOTE_FILE_CONFLICT := 60
const RESULT_ILLEGAL_PASSWORD := 61
const RESULT_SAME_AS_PREVIOUS_VALUE := 62
const RESULT_ACCOUNT_LOG_ON_DENIED := 63
const RESULT_CANNOT_USE_OLD_PASSWORD := 64
const RESULT_INVALID_LOG_IN_AUTH_CODE := 65
const RESULT_ACCOUNT_LOG_ON_DENIED_NO_MAIL := 66
const RESULT_HARDWARE_NOT_CAPABLE_OF_IPT := 67
const RESULT_IPT_INIT_ERROR := 68
const RESULT_PARENTAL_CONTROL_RESTRICTED := 69
const RESULT_FACEBOOK_QUERY_ERROR := 70
const RESULT_EXPIRED_LOGIN_AUTH_CODE := 71
const RESULT_IP_LOGIN_RESTRICTION_FAILED := 72
const RESULT_ACCOUNT_LOCKED_DOWN := 73
const RESULT_ACCOUNT_LOG_ON_DENIED_VERIFIED_EMAIL_REQUIRED := 74
const RESULT_NO_MATCHING_URL := 75
const RESULT_BAD_RESPONSE := 76
const RESULT_REQUIRE_PASSWORD_REENTRY := 77
const RESULT_VALUE_OUT_OF_RANGE := 78
const RESULT_UNEXPECTED_ERROR := 79
const RESULT_DISABLED := 80
const RESULT_INVALID_CEG_SUBMISSION := 81
const RESULT_RESTRICTED_DEVICE := 82
const RESULT_REGION_LOCKED := 83
const RESULT_RATE_LIMIT_EXCEEDED := 84
const RESULT_ACCOUNT_LOGIN_DENIED_NEED_TWO_FACTOR := 85
const RESULT_ITEM_DELETED := 86
const RESULT_ACCOUNT_LOGIN_DENIED_THROTTLE := 87
const RESULT_TWO_FACTOR_CODE_MISMATCH := 88
const RESULT_TWO_FACTOR_ACTIVATION_CODE_MISMATCH := 89
const RESULT_ACCOUNT_ASSOCIATED_TO_MULTIPLE_PARTNERS := 90
const RESULT_NOT_MODIFIED := 91
const RESULT_NO_MOBILE_DEVICE := 92
const RESULT_TIME_NOT_SYNCED := 93
const RESULT_SMS_CODE_FAILED := 94
const RESULT_ACCOUNT_LIMIT_EXCEEDED := 95
const RESULT_ACCOUNT_ACTIVITY_LIMIT_EXCEEDED := 96
const RESULT_PHONE_ACTIVITY_LIMIT_EXCEEDED := 97
const RESULT_REFUND_TO_WALLET := 98
const RESULT_EMAIL_SEND_FAILURE := 99
const RESULT_NOT_SETTLED := 100
const RESULT_NEED_CAPTCHA := 101
const RESULT_GSLT_DENIED := 102
const RESULT_GS_OWNER_DENIED := 103
const RESULT_INVALID_ITEM_TYPE := 104
const RESULT_IP_BANNED := 105
const RESULT_GSLT_EXPIRED := 106
const RESULT_INSUFFICIENT_FUNDS := 107
const RESULT_TOO_MANY_PENDING := 108
const RESULT_NO_SITE_LICENSES_FOUND := 109
const RESULT_WG_NETWORK_SEND_EXCEEDED := 110
const RESULT_ACCOUNT_NOT_FRIENDS := 111
const RESULT_LIMITED_USER_ACCOUNT := 112
const RESULT_CANT_REMOVE_ITEM := 113
const RESULT_ACCOUNT_DELETED := 114
const RESULT_EXISTING_USER_CANCELLED_LICENSE := 115
const RESULT_COMMUNITY_COOLDOWN := 116
const RESULT_NO_LAUNCHER_SPECIFIED := 117
const RESULT_MUST_AGREE_TO_SSA := 118
const RESULT_LAUNCHER_MIGRATED := 119
const RESULT_STEAM_REALM_MISMATCH := 120
const RESULT_INVALID_SIGNATURE := 121
const RESULT_PARSE_FAILURE := 122
const RESULT_NO_VERIFIED_PHONE := 123
const RESULT_INSUFFICIENT_BATTERY := 124
const RESULT_CHARGER_REQUIRED := 125
const RESULT_CACHED_CREDENTIAL_INVALID := 126
const RESULT_PHONE_NUMBER_IS_VOIP := 127
const RESULT_NOT_SUPPORTED := 128
const RESULT_FAMILY_SIZE_LIMIT_EXCEEDED := 129
const RESULT_OFFLINE_APP_CACHE_INVALID := 130
const RESULT_TRY_LATER := 131
const PAD_TRIGGER_EFFECT_MODE_OFF := 0
const PAD_TRIGGER_EFFECT_MODE_FEEDBACK := 1
const PAD_TRIGGER_EFFECT_MODE_WEAPON := 2
const PAD_TRIGGER_EFFECT_MODE_VIBRATION := 3
const PAD_TRIGGER_EFFECT_MODE_MULTIPLE_POSITION_FEEDBACK := 4
const PAD_TRIGGER_EFFECT_MODE_SLOPE_FEEDBACK := 5
const PAD_TRIGGER_EFFECT_MODE_MULTIPLE_POSITION_VIBRATION := 6
const NET_SOCKET_CONNECTION_TYPE_NOT_CONNECTED := 0
const NET_SOCKET_CONNECTION_TYPE_UDP := 1
const NET_SOCKET_CONNECTION_TYPE_UDP_RELAY := 2
const NET_SOCKET_STATE_INVALID := 0
const NET_SOCKET_STATE_CONNECTED := 1
const NET_SOCKET_STATE_INITIATED := 10
const NET_SOCKET_STATE_LOCAL_CANDIDATE_FOUND := 11
const NET_SOCKET_STATE_RECEIVED_REMOTE_CANDIDATES := 12
const NET_SOCKET_STATE_CHALLENGE_HANDSHAKE := 15
const NET_SOCKET_STATE_DISCONNECTING := 21
const NET_SOCKET_STATE_LOCAL_DISCONNECT := 22
const NET_SOCKET_STATE_TIMEOUT_DURING_CONNECT := 23
const NET_SOCKET_STATE_REMOTE_END_DISCONNECTED := 24
const NET_SOCKET_STATE_BROKEN := 25
const STEAM_API_INIT_RESULT_OK := 0
const STEAM_API_INIT_RESULT_FAILED_GENERIC := 1
const STEAM_API_INIT_RESULT_NO_STEAM_CLIENT := 2
const STEAM_API_INIT_RESULT_VERSION_MISMATCH := 3
const STEAM_CONTROLLER_PAD_LEFT := 0
const STEAM_CONTROLLER_PAD_RIGHT := 1
const STEAM_CONTROLLER_PAD_BOTH := 2
const STEAM_CONTROLLER_GRIP_LEFT := 3
const STEAM_CONTROLLER_GRIP_RIGHT := 4
const STEAM_CONTROLLER_GRIP_BOTH := 5
const STEAM_HARDWARE_TYPE_NONE := 0
const STEAM_HARDWARE_TYPE_STEAM_DECK := 1
const STEAM_HARDWARE_TYPE_STEAM_MACHINE := 2
const STEAM_HARDWARE_TYPE_STEAM_FRAME := 3
const STEAM_HARDWARE_DEFAULT_CONFIG_NONE := 0
const STEAM_HARDWARE_DEFAULT_CONFIG_LOW := 1
const STEAM_HARDWARE_DEFAULT_CONFIG_MEDIUM := 2
const STEAM_HARDWARE_DEFAULT_CONFIG_HIGH := 3
const STEAM_HARDWARE_DEFAULT_CONFIG_MAX := 4
const STEAM_HARDWARE_DEFAULT_CONFIG_STEAM_DECK := 5
const STEAM_HARDWARE_DEFAULT_CONFIG_STEAM_MACHINE := 6
const STEAM_HARDWARE_DEFAULT_CONFIG_STEAM_FRAME := 7
const TEXT_FILTERING_CONTEXT_UNKNOWN := 0
const TEXT_FILTERING_CONTEXT_GAME_CONTENT := 1
const TEXT_FILTERING_CONTEXT_CHAT := 2
const TEXT_FILTERING_CONTEXT_NAME := 3
const TIMELINE_GAME_MODE_INVALID := 0
const TIMELINE_GAME_MODE_PLAYING := 1
const TIMELINE_GAME_MODE_STAGING := 2
const TIMELINE_GAME_MODE_MENUS := 3
const TIMELINE_GAME_MODE_LOADING_SCREEN := 4
const TIMELINE_GAME_MODE_MAX := 5
const TIMELINE_EVENT_CLIP_PRIORITY_INVALID := 0
const TIMELINE_EVENT_CLIP_PRIORITY_NONE := 1
const TIMELINE_EVENT_CLIP_PRIORITY_STANDARD := 2
const TIMELINE_EVENT_CLIP_PRIORITY_FEATURED := 3
const UNIVERSE_INVALID := 0
const UNIVERSE_PUBLIC := 1
const UNIVERSE_BETA := 2
const UNIVERSE_INTERNAL := 3
const UNIVERSE_DEV := 4
const UNIVERSE_MAX := 5
const UGC_CONTENT_DESCRIPTOR_NUDITY_OR_SEXUAL_CONTENT := 1
const UGC_CONTENT_DESCRIPTOR_FREQUENT_VIOLENCE_OR_GORE := 2
const UGC_CONTENT_DESCRIPTOR_ADULT_ONLY_SEXUAL_CONTENT := 3
const UGC_CONTENT_DESCRIPTOR_GRATUITOUS_SEXUAL_CONTENT := 4
const UGC_CONTENT_DESCRIPTOR_ANY_MATURE_CONTENT := 5
const UGC_MATCHING_UGC_TYPE_ITEMS := 0
const UGC_MATCHING_UGC_TYPE_ITEMS_MTX := 1
const UGC_MATCHING_UGC_TYPE_ITEMS_READY_TO_USE := 2
const UGC_MATCHING_UGC_TYPE_COLLECTIONS := 3
const UGC_MATCHING_UGC_TYPE_ARTWORK := 4
const UGC_MATCHING_UGC_TYPE_VIDEOS := 5
const UGC_MATCHING_UGC_TYPE_SCREENSHOTS := 6
const UGC_MATCHING_UGC_TYPE_ALL_GUIDES := 7
const UGC_MATCHING_UGC_TYPE_WEB_GUIDES := 8
const UGC_MATCHING_UGC_TYPE_INTEGRATED_GUIDES := 9
const UGC_MATCHING_UGC_TYPE_USABLE_IN_GAME := 10
const UGC_MATCHING_UGC_TYPE_CONTROLLER_BINDINGS := 11
const UGC_MATCHING_UGC_TYPE_GAME_MANAGED_ITEMS := 12
const UGC_MATCHING_UGC_TYPE_ALL := -1
const UGC_QUERY_RANKED_BY_VOTE := 0
const UGC_QUERY_RANKED_BY_PUBLICATION_DATE := 1
const UGC_QUERY_ACCEPTED_FOR_GAME_RANKED_BY_ACCEPTANCE_DATE := 2
const UGC_QUERY_RANKED_BY_TREND := 3
const UGC_QUERY_FAVORITED_BY_FRIENDS_RANKED_BY_PUBLICATION_DATE := 4
const UGC_QUERY_CREATED_BY_FRIENDS_RANKED_BY_PUBLICATION_DATE := 5
const UGC_QUERY_RANKED_BY_NUM_TIMES_REPORTED := 6
const UGC_QUERY_CREATED_BY_FOLLOWED_USERS_RANKED_BY_PUBLICATION_DATE := 7
const UGC_QUERY_NOT_YET_RATED := 8
const UGC_QUERY_RANKED_BY_TOTAL_VOTES_ASC := 9
const UGC_QUERY_RANKED_BY_VOTES_UP := 10
const UGC_QUERY_RANKED_BY_TEXT_SEARCH := 11
const UGC_QUERY_RANKED_BY_TOTAL_UNIQUE_SUBSCRIPTIONS := 12
const UGC_QUERY_RANKED_BY_PLAYTIME_TREND := 13
const UGC_QUERY_RANKED_BY_TOTAL_PLAYTIME := 14
const UGC_QUERY_RANKED_BY_AVERAGE_PLAYTIME_TREND := 15
const UGC_QUERY_RANKED_BY_LIFETIME_AVERAGE_PLAYTIME := 16
const UGC_QUERY_RANKED_BY_PLAYTIME_SESSIONS_TREND := 17
const UGC_QUERY_RANKED_BY_LIFETIME_PLAYTIME_SESSIONS := 18
const UGC_QUERY_RANKED_BY_LAST_UPDATED_DATE := 19
const UGC_QUERY_RANKED_BY_NUM_PARENT_ITEMS := 20
const UGC_QUERY_RANKED_BY_NUM_PARENT_COLLECTIONS := 21
const UGC_READ_CONTINUE_READING_UNTIL_FINISHED := 0
const UGC_READ_CONTINUE_READING := 1
const UGC_READ_CLOSE := 2
const USER_HAS_LICENSE_RESULT_HAS_LICENSE := 0
const USER_HAS_LICENSE_RESULT_DOES_NOT_HAVE_LICENSE := 1
const USER_HAS_LICENSE_RESULT_NO_AUTH := 2
const USER_UGC_LIST_PUBLISHED := 0
const USER_UGC_LIST_VOTED_ON := 1
const USER_UGC_LIST_VOTED_UP := 2
const USER_UGC_LIST_VOTED_DOWN := 3
const USER_UGC_LIST_WILL_VOTE_LATER := 4
const USER_UGC_LIST_FAVORITED := 5
const USER_UGC_LIST_SUBSCRIBED := 6
const USER_UGC_LIST_USED_OR_PLAYED := 7
const USER_UGC_LIST_FOLLOWED := 8
const USER_UGC_LIST_SORT_ORDER_CREATION_ORDER_DESC := 0
const USER_UGC_LIST_SORT_ORDER_CREATION_ORDER_ASC := 1
const USER_UGC_LIST_SORT_ORDER_TITLE_ASC := 2
const USER_UGC_LIST_SORT_ORDER_LAST_UPDATED_DESC := 3
const USER_UGC_LIST_SORT_ORDER_SUBSCRIPTION_DATE_DESC := 4
const USER_UGC_LIST_SORT_ORDER_VOTE_SCORE_DESC := 5
const USER_UGC_LIST_SORT_ORDER_FOR_MODERATION := 6
const VOICE_RESULT_OK := 0
const VOICE_RESULT_NOT_INITIALIZED := 1
const VOICE_RESULT_NOT_RECORDING := 2
const VOICE_RESULT_NO_DATA := 3
const VOICE_RESULT_BUFFER_TOO_SMALL := 4
const VOICE_RESULT_DATA_CORRUPTED := 5
const VOICE_RESULT_RESTRICTED := 6
const VOICE_RESULT_UNSUPPORTED_CODEC := 7
const VOICE_RESULT_RECEIVER_OUT_OF_DATE := 8
const VOICE_RESULT_RECEIVER_DID_NOT_ANSWER := 9
const VR_SCREENSHOT_TYPE_NONE := 0
const VR_SCREENSHOT_TYPE_MONO := 1
const VR_SCREENSHOT_TYPE_STEREO := 2
const VR_SCREENSHOT_TYPE_MONO_CUBE_MAP := 3
const VR_SCREENSHOT_TYPE_MONO_PANORAMA := 4
const VR_SCREENSHOT_TYPE_STEREO_PANORAMA := 5
const WORKSHOP_ENUMERATION_TYPE_RANKED_BY_VOTE := 0
const WORKSHOP_ENUMERATION_TYPE_RECENT := 1
const WORKSHOP_ENUMERATION_TYPE_TRENDING := 2
const WORKSHOP_ENUMERATION_TYPE_FAVORITES_OF_FRIENDS := 3
const WORKSHOP_ENUMERATION_TYPE_VOTED_BY_FRIENDS := 4
const WORKSHOP_ENUMERATION_TYPE_CONTENT_BY_FRIENDS := 5
const WORKSHOP_ENUMERATION_TYPE_RECENT_FROM_FOLLOWED_USERS := 6
const WORKSHOP_FILE_ACTION_PLAYED := 0
const WORKSHOP_FILE_ACTION_COMPLETED := 1
const WORKSHOP_FILE_TYPE_FIRST := 0
const WORKSHOP_FILE_TYPE_COMMUNITY := 0
const WORKSHOP_FILE_TYPE_MICROTRANSACTION := 1
const WORKSHOP_FILE_TYPE_COLLECTION := 2
const WORKSHOP_FILE_TYPE_ART := 3
const WORKSHOP_FILE_TYPE_VIDEO := 4
const WORKSHOP_FILE_TYPE_SCREENSHOT := 5
const WORKSHOP_FILE_TYPE_GAME := 6
const WORKSHOP_FILE_TYPE_SOFTWARE := 7
const WORKSHOP_FILE_TYPE_CONCEPT := 8
const WORKSHOP_FILE_TYPE_WEB_GUIDE := 9
const WORKSHOP_FILE_TYPE_INTEGRATED_GUIDE := 10
const WORKSHOP_FILE_TYPE_MERCH := 11
const WORKSHOP_FILE_TYPE_CONTROLLER_BINDING := 12
const WORKSHOP_FILE_TYPE_STEAMWORKS_ACCESS_INVITE := 13
const WORKSHOP_FILE_TYPE_STEAM_VIDEO := 14
const WORKSHOP_FILE_TYPE_GAME_MANAGED_ITEM := 15
const WORKSHOP_FILE_TYPE_CLIP := 16
const WORKSHOP_FILE_TYPE_MAX := 17
const WORKSHOP_VIDEO_PROVIDER_NONE := 0
const WORKSHOP_VIDEO_PROVIDER_YOUTUBE := 1
const WORKSHOP_VOTE_UNVOTED := 0
const WORKSHOP_VOTE_FOR := 1
const WORKSHOP_VOTE_AGAINST := 2
const WORKSHOP_VOTE_LATER := 3
const XBOX_ORIGIN_A := 0
const XBOX_ORIGIN_B := 1
const XBOX_ORIGIN_X := 2
const XBOX_ORIGIN_Y := 3
const XBOX_ORIGIN_LEFT_BUMPER := 4
const XBOX_ORIGIN_RIGHT_BUMPER := 5
const XBOX_ORIGIN_MENU := 6
const XBOX_ORIGIN_VIEW := 7
const XBOX_ORIGIN_LEFT_TRIGGER_PULL := 8
const XBOX_ORIGIN_LEFT_TRIGGER_CLICK := 9
const XBOX_ORIGIN_RIGHT_TRIGGER_PULL := 10
const XBOX_ORIGIN_RIGHT_TRIGGER_CLICK := 11
const XBOX_ORIGIN_LEFT_STICK_MOVE := 12
const XBOX_ORIGIN_LEFT_STICK_CLICK := 13
const XBOX_ORIGIN_LEFT_STICK_DPAD_NORTH := 14
const XBOX_ORIGIN_LEFT_STICK_DPAD_SOUTH := 15
const XBOX_ORIGIN_LEFT_STICK_DPAD_WEST := 16
const XBOX_ORIGIN_LEFT_STICK_DPAD_EAST := 17
const XBOX_ORIGIN_RIGHT_STICK_MOVE := 18
const XBOX_ORIGIN_RIGHT_STICK_CLICK := 19
const XBOX_ORIGIN_RIGHT_STICK_DPAD_NORTH := 20
const XBOX_ORIGIN_RIGHT_STICK_DPAD_SOUTH := 21
const XBOX_ORIGIN_RIGHT_STICK_DPAD_WEST := 22
const XBOX_ORIGIN_RIGHT_STICK_DPAD_EAST := 23
const XBOX_ORIGIN_DPAD_NORTH := 24
const XBOX_ORIGIN_DPAD_SOUTH := 25
const XBOX_ORIGIN_DPAD_WEST := 26
const XBOX_ORIGIN_DPAD_EAST := 27
const XBOX_ORIGIN_COUNT := 28

## Set by steam.gd to the bridge that answers these calls.
var bridge = null


func _steam_call(method: String, args: Array):
	if bridge != null:
		return bridge.dispatch(method, args)
	return _empty(method)


## The value GodotSteam would have returned for an unanswered call.
func _empty(method: String):
	match RETURN_TYPES.get(method, "void"):
		"bool":
			return false
		"int":
			return 0
		"float":
			return 0.0
		"String":
			return ""
		"Dictionary":
			return {}
		"Array":
			return []
		"PackedInt32Array":
			return PackedInt32Array()
		"PackedInt64Array":
			return PackedInt64Array()
		"PackedFloat64Array":
			return PackedFloat64Array()
		"PackedByteArray":
			return PackedByteArray()
	return null


# Property accessors, named the way GodotSteam names them.
func get_current_app_id() -> int:
	return current_app_id

func set_current_app_id(value: int) -> void:
	current_app_id = value

func get_current_browser_handle() -> int:
	return current_browser_handle

func set_current_browser_handle(value: int) -> void:
	current_browser_handle = value

func get_current_clan_id() -> int:
	return current_clan_id

func set_current_clan_id(value: int) -> void:
	current_clan_id = value

func get_current_steam_id() -> int:
	return current_steam_id

func set_current_steam_id(value: int) -> void:
	current_steam_id = value

func get_inventory_handle() -> int:
	return inventory_handle

func set_inventory_handle(value: int) -> void:
	inventory_handle = value

func get_inventory_update_handle() -> int:
	return inventory_update_handle

func set_inventory_update_handle(value: int) -> void:
	inventory_update_handle = value

func get_leaderboard_details_max() -> int:
	return leaderboard_details_max

func set_leaderboard_details_max(value: int) -> void:
	leaderboard_details_max = value

func get_leaderboard_entries_array() -> Array:
	return leaderboard_entries_array

func set_leaderboard_entries_array(value: Array) -> void:
	leaderboard_entries_array = value

func get_leaderboard_handle() -> int:
	return leaderboard_handle

func set_leaderboard_handle(value: int) -> void:
	leaderboard_handle = value

func get_leaderboard_ugc_handle() -> int:
	return leaderboard_ugc_handle

func set_leaderboard_ugc_handle(value: int) -> void:
	leaderboard_ugc_handle = value


# ------------------------------------------------------------------ GodotSteam surface

func acceptConnection(connection_handle: int):
	return _steam_call("acceptConnection", [connection_handle])

func acceptP2PSessionWithUser(remote_steam_id: int):
	return _steam_call("acceptP2PSessionWithUser", [remote_steam_id])

func acceptSessionWithUser(remote_steam_id: int):
	return _steam_call("acceptSessionWithUser", [remote_steam_id])

func activateActionSet(input_handle: int, action_set_handle: int):
	return _steam_call("activateActionSet", [input_handle, action_set_handle])

func activateActionSetLayer(input_handle: int, action_set_layer_handle: int):
	return _steam_call("activateActionSetLayer", [input_handle, action_set_layer_handle])

func activateGameOverlay(type: String = ""):
	return _steam_call("activateGameOverlay", [type])

func activateGameOverlayInviteDialog(lobby_id: int):
	return _steam_call("activateGameOverlayInviteDialog", [lobby_id])

func activateGameOverlayInviteDialogConnectString(connect_string: String):
	return _steam_call("activateGameOverlayInviteDialogConnectString", [connect_string])

func activateGameOverlayRemotePlayTogetherInviteDialog(lobby_id: int):
	return _steam_call("activateGameOverlayRemotePlayTogetherInviteDialog", [lobby_id])

func activateGameOverlayToStore(app_id: int, store_flag: int = 0):
	return _steam_call("activateGameOverlayToStore", [app_id, store_flag])

func activateGameOverlayToUser(type: String = "", steam_id: int = 0):
	return _steam_call("activateGameOverlayToUser", [type, steam_id])

func activateGameOverlayToWebPage(url: String, webpage_mode: int = 0):
	return _steam_call("activateGameOverlayToWebPage", [url, webpage_mode])

func addAppDependency(published_file_id: int, app_id: int):
	return _steam_call("addAppDependency", [published_file_id, app_id])

func addContentDescriptor(update_handle: int, descriptor_id: int):
	return _steam_call("addContentDescriptor", [update_handle, descriptor_id])

func addDependency(published_file_id: int, child_published_file_id: int):
	return _steam_call("addDependency", [published_file_id, child_published_file_id])

func addExcludedTag(query_handle: int, tag_name: String):
	return _steam_call("addExcludedTag", [query_handle, tag_name])

func addFavoriteGame(ip: String, port: int, query_port: int, flags: int, last_played: int):
	return _steam_call("addFavoriteGame", [ip, port, query_port, flags, last_played])

func addGamePhaseTag(tag_name: String, tag_icon: String, tag_group: String, priority: int):
	return _steam_call("addGamePhaseTag", [tag_name, tag_icon, tag_group, priority])

func addHeader(key: String, value: String, browser_handle: int = 0):
	return _steam_call("addHeader", [key, value, browser_handle])

func addInstantaneousTimelineEvent(title: String, description: String, icon: String, icon_priority: int, start_offset_seconds: float, possible_clip: int = 1):
	return _steam_call("addInstantaneousTimelineEvent", [title, description, icon, icon_priority, start_offset_seconds, possible_clip])

func addItemKeyValueTag(update_handle: int, key: String, value: String):
	return _steam_call("addItemKeyValueTag", [update_handle, key, value])

func addItemPreviewFile(update_handle: int, preview_file: String, preview_type: int):
	return _steam_call("addItemPreviewFile", [update_handle, preview_file, preview_type])

func addItemPreviewVideo(update_handle: int, video_id: String):
	return _steam_call("addItemPreviewVideo", [update_handle, video_id])

func addItemToFavorites(app_id: int, published_file_id: int):
	return _steam_call("addItemToFavorites", [app_id, published_file_id])

func addPromoItem(item: int):
	return _steam_call("addPromoItem", [item])

func addPromoItems(items: PackedInt64Array):
	return _steam_call("addPromoItems", [items])

func addRangeTimelineEvent(title: String, description: String, icon: String, icon_priority: int, start_offset_seconds: float, duration: float, possible_clip: int = 1):
	return _steam_call("addRangeTimelineEvent", [title, description, icon, icon_priority, start_offset_seconds, duration, possible_clip])

func addRequestLobbyListDistanceFilter(distance_filter: int):
	return _steam_call("addRequestLobbyListDistanceFilter", [distance_filter])

func addRequestLobbyListFilterSlotsAvailable(slots_available: int):
	return _steam_call("addRequestLobbyListFilterSlotsAvailable", [slots_available])

func addRequestLobbyListNearValueFilter(key_to_match: String, value_to_be_close_to: int):
	return _steam_call("addRequestLobbyListNearValueFilter", [key_to_match, value_to_be_close_to])

func addRequestLobbyListNumericalFilter(key_to_match: String, value_to_match: int, comparison_type: int):
	return _steam_call("addRequestLobbyListNumericalFilter", [key_to_match, value_to_match, comparison_type])

func addRequestLobbyListResultCountFilter(max_results: int):
	return _steam_call("addRequestLobbyListResultCountFilter", [max_results])

func addRequestLobbyListStringFilter(key_to_match: String, value_to_match: String, comparison_type: int):
	return _steam_call("addRequestLobbyListStringFilter", [key_to_match, value_to_match, comparison_type])

func addRequiredKeyValueTag(query_handle: int, key: String, value: String):
	return _steam_call("addRequiredKeyValueTag", [query_handle, key, value])

func addRequiredTag(query_handle: int, tag_name: String):
	return _steam_call("addRequiredTag", [query_handle, tag_name])

func addRequiredTagGroup(query_handle: int, tag_array: Array):
	return _steam_call("addRequiredTagGroup", [query_handle, tag_array])

func addScreenshotToLibrary(filename: String, thumbnail_filename: String, width: int, height: int):
	return _steam_call("addScreenshotToLibrary", [filename, thumbnail_filename, width, height])

func addVRScreenshotToLibrary(type: int, filename: String, vr_filename: String):
	return _steam_call("addVRScreenshotToLibrary", [type, filename, vr_filename])

func advertiseGame(server_ip: String = "", port: int = 0):
	return _steam_call("advertiseGame", [server_ip, port])

func allowP2PPacketRelay(allow: bool):
	return _steam_call("allowP2PPacketRelay", [allow])

func allowStartRequest(allowed: bool, browser_handle: int = 0):
	return _steam_call("allowStartRequest", [allowed, browser_handle])

func attachLeaderboardUGC(ugc_handle: int, this_leaderboard: int = 0):
	return _steam_call("attachLeaderboardUGC", [ugc_handle, this_leaderboard])

func beginAsyncRequestFakeIP(num_ports: int):
	return _steam_call("beginAsyncRequestFakeIP", [num_ports])

func beginAuthSession(ticket: PackedByteArray, ticket_size: int, steam_id: int):
	return _steam_call("beginAuthSession", [ticket, ticket_size, steam_id])

func beginFileWriteBatch():
	return _steam_call("beginFileWriteBatch", [])

func cancelAuthTicket(auth_ticket: int):
	return _steam_call("cancelAuthTicket", [auth_ticket])

func cancelQuery(server_list_request: int):
	return _steam_call("cancelQuery", [server_list_request])

func cancelReservation(beacon_id: int, steam_id: int):
	return _steam_call("cancelReservation", [beacon_id, steam_id])

func cancelServerQuery(server_query: int):
	return _steam_call("cancelServerQuery", [server_query])

func changeNumOpenSlots(beacon_id: int, open_slots: int):
	return _steam_call("changeNumOpenSlots", [beacon_id, open_slots])

func checkFileSignature(_unnamed_arg0: String):
	return _steam_call("checkFileSignature", [_unnamed_arg0])

func checkPingDataUpToDate(max_age_in_seconds: float):
	return _steam_call("checkPingDataUpToDate", [max_age_in_seconds])

func checkResultSteamID(steam_id_expected: int, this_inventory_handle: int = 0):
	return _steam_call("checkResultSteamID", [steam_id_expected, this_inventory_handle])

func clearAchievement(achievement_name: String):
	return _steam_call("clearAchievement", [achievement_name])

func clearRichPresence():
	return _steam_call("clearRichPresence", [])

func clearTimelineTooltip(time_delta: float):
	return _steam_call("clearTimelineTooltip", [time_delta])

func closeChannelWithUser(remote_steam_id: int, channel: int):
	return _steam_call("closeChannelWithUser", [remote_steam_id, channel])

func closeClanChatWindowInSteam(chat_id: int):
	return _steam_call("closeClanChatWindowInSteam", [chat_id])

func closeConnection(connection_handle: int, reason: int, debug_message: String, linger: bool):
	return _steam_call("closeConnection", [connection_handle, reason, debug_message, linger])

func closeListenSocket(socket: int):
	return _steam_call("closeListenSocket", [socket])

func closeP2PChannelWithUser(remote_steam_id: int, channel: int):
	return _steam_call("closeP2PChannelWithUser", [remote_steam_id, channel])

func closeP2PSessionWithUser(remote_steam_id: int):
	return _steam_call("closeP2PSessionWithUser", [remote_steam_id])

func closeSessionWithUser(remote_steam_id: int):
	return _steam_call("closeSessionWithUser", [remote_steam_id])

func configureConnectionLanes(connection: int, lanes: int, priorities: Array, weights: Array):
	return _steam_call("configureConnectionLanes", [connection, lanes, priorities, weights])

func connectByIPAddress(ip_address_with_port: String, options: Dictionary):
	return _steam_call("connectByIPAddress", [ip_address_with_port, options])

func connectP2P(remote_steam_id: int, virtual_port: int, options: Dictionary):
	return _steam_call("connectP2P", [remote_steam_id, virtual_port, options])

func connectToHostedDedicatedServer(remote_steam_id: int, virtual_port: int, options: Dictionary):
	return _steam_call("connectToHostedDedicatedServer", [remote_steam_id, virtual_port, options])

func consumeItem(item_consume: int, quantity: int):
	return _steam_call("consumeItem", [item_consume, quantity])

func convertPingLocationToString(location: PackedByteArray):
	return _steam_call("convertPingLocationToString", [location])

func copyToClipboard(browser_handle: int = 0):
	return _steam_call("copyToClipboard", [browser_handle])

func createBeacon(open_slots: int, location_id: int, location_type: int, connect_string: String, beacon_metadata: String):
	return _steam_call("createBeacon", [open_slots, location_id, location_type, connect_string, beacon_metadata])

func createBrowser(user_agent: String = "", user_css: String = ""):
	return _steam_call("createBrowser", [user_agent, user_css])

func createCookieContainer(allow_response_to_modify: bool):
	return _steam_call("createCookieContainer", [allow_response_to_modify])

func createFakeUDPPort(fake_server_port: int):
	return _steam_call("createFakeUDPPort", [fake_server_port])

func createHTTPRequest(request_method: int, absolute_url: String):
	return _steam_call("createHTTPRequest", [request_method, absolute_url])

func createItem(app_id: int, file_type: int):
	return _steam_call("createItem", [app_id, file_type])

func createListenSocketIP(ip_reference: String, options: Dictionary):
	return _steam_call("createListenSocketIP", [ip_reference, options])

func createListenSocketP2P(virtual_port: int, options: Dictionary):
	return _steam_call("createListenSocketP2P", [virtual_port, options])

func createListenSocketP2PFakeIP(fake_port: int, options: Dictionary):
	return _steam_call("createListenSocketP2PFakeIP", [fake_port, options])

func createLobby(lobby_type: int, max_members: int = 2):
	return _steam_call("createLobby", [lobby_type, max_members])

func createPollGroup():
	return _steam_call("createPollGroup", [])

func createQueryAllUGCRequestCursor(query_type: int, matching_type: int, creator_id: int, consumer_id: int, cursor: String):
	return _steam_call("createQueryAllUGCRequestCursor", [query_type, matching_type, creator_id, consumer_id, cursor])

func createQueryAllUGCRequestPage(query_type: int, matching_type: int, creator_id: int, consumer_id: int, page: int):
	return _steam_call("createQueryAllUGCRequestPage", [query_type, matching_type, creator_id, consumer_id, page])

func createQueryUGCDetailsRequest(published_file_id_array: Array):
	return _steam_call("createQueryUGCDetailsRequest", [published_file_id_array])

func createQueryUserUGCRequest(account_id: int, list_type: int, matching_ugc_type: int, sort_order: int, creator_id: int, consumer_id: int, page: int):
	return _steam_call("createQueryUserUGCRequest", [account_id, list_type, matching_ugc_type, sort_order, creator_id, consumer_id, page])

func createSocketPair(loopback: bool, remote_steam_id1: int, remote_steam_id2: int):
	return _steam_call("createSocketPair", [loopback, remote_steam_id1, remote_steam_id2])

func deactivateActionSetLayer(input_handle: int, action_set_handle: int):
	return _steam_call("deactivateActionSetLayer", [input_handle, action_set_handle])

func deactivateAllActionSetLayers(input_handle: int):
	return _steam_call("deactivateAllActionSetLayers", [input_handle])

func decompressVoice(voice_data: PackedByteArray, sample_rate: int = 11025, buffer_size: int = 20480):
	return _steam_call("decompressVoice", [voice_data, sample_rate, buffer_size])

func deferHTTPRequest(request_handle: int):
	return _steam_call("deferHTTPRequest", [request_handle])

func deleteItem(published_file_id: int):
	return _steam_call("deleteItem", [published_file_id])

func deleteLobbyData(steam_lobby_id: int, key: String):
	return _steam_call("deleteLobbyData", [steam_lobby_id, key])

func deserializeResult(buffer: PackedByteArray):
	return _steam_call("deserializeResult", [buffer])

func destroyBeacon(beacon_id: int):
	return _steam_call("destroyBeacon", [beacon_id])

func destroyPollGroup(poll_group: int):
	return _steam_call("destroyPollGroup", [poll_group])

func destroyResult(this_inventory_handle: int = 0):
	return _steam_call("destroyResult", [this_inventory_handle])

func disableRemotePlayTogetherDirectInput():
	return _steam_call("disableRemotePlayTogetherDirectInput", [])

func dismissFloatingGamepadTextInput():
	return _steam_call("dismissFloatingGamepadTextInput", [])

func dismissGamepadTextInput():
	return _steam_call("dismissGamepadTextInput", [])

func doesEventRecordingExist(timeline_event_handle: int):
	return _steam_call("doesEventRecordingExist", [timeline_event_handle])

func doesGamePhaseRecordingExist(phase_id: String):
	return _steam_call("doesGamePhaseRecordingExist", [phase_id])

func downloadClanActivityCounts(clan_id_array: PackedInt64Array):
	return _steam_call("downloadClanActivityCounts", [clan_id_array])

func downloadItem(published_file_id: int, high_priority: bool):
	return _steam_call("downloadItem", [published_file_id, high_priority])

func downloadLeaderboardEntries(start: int, end: int, type: int = 0, this_leaderboard: int = 0):
	return _steam_call("downloadLeaderboardEntries", [start, end, type, this_leaderboard])

func downloadLeaderboardEntriesForUsers(users_id: Array, this_leaderboard: int = 0):
	return _steam_call("downloadLeaderboardEntriesForUsers", [users_id, this_leaderboard])

func enableActionEventCallbacks():
	return _steam_call("enableActionEventCallbacks", [])

func enableDeviceCallbacks():
	return _steam_call("enableDeviceCallbacks", [])

func enableRemotePlayTogetherDirectInput():
	return _steam_call("enableRemotePlayTogetherDirectInput", [])

func endAuthSession(steam_id: int):
	return _steam_call("endAuthSession", [steam_id])

func endFileWriteBatch():
	return _steam_call("endFileWriteBatch", [])

func endGamePhase():
	return _steam_call("endGamePhase", [])

func endRangeTimelineEvent(timeline_event_handle: int, end_offset_seconds: float):
	return _steam_call("endRangeTimelineEvent", [timeline_event_handle, end_offset_seconds])

func enumerateFollowingList(start_index: int):
	return _steam_call("enumerateFollowingList", [start_index])

func estimatePingTimeBetweenTwoLocations(location1: PackedByteArray, location2: PackedByteArray):
	return _steam_call("estimatePingTimeBetweenTwoLocations", [location1, location2])

func estimatePingTimeFromLocalHost(location: PackedByteArray):
	return _steam_call("estimatePingTimeFromLocalHost", [location])

func exchangeItems(output_items: PackedInt64Array, output_quantity: PackedInt32Array, input_items: PackedInt64Array, input_quantity: PackedInt32Array):
	return _steam_call("exchangeItems", [output_items, output_quantity, input_items, input_quantity])

func executeJavascript(script: String, browser_handle: int = 0):
	return _steam_call("executeJavascript", [script, browser_handle])

func fileDelete(file: String):
	return _steam_call("fileDelete", [file])

func fileExists(file: String):
	return _steam_call("fileExists", [file])

func fileForget(file: String):
	return _steam_call("fileForget", [file])

func filePersisted(file: String):
	return _steam_call("filePersisted", [file])

func fileRead(file: String, data_to_read: int):
	return _steam_call("fileRead", [file, data_to_read])

func fileReadAsync(file: String, offset: int, data_to_read: int):
	return _steam_call("fileReadAsync", [file, offset, data_to_read])

func fileShare(file: String):
	return _steam_call("fileShare", [file])

func fileWrite(file: String, data: PackedByteArray, size: int = 0):
	return _steam_call("fileWrite", [file, data, size])

func fileWriteAsync(file: String, data: PackedByteArray, size: int = 0):
	return _steam_call("fileWriteAsync", [file, data, size])

func fileWriteStreamCancel(write_handle: int):
	return _steam_call("fileWriteStreamCancel", [write_handle])

func fileWriteStreamClose(write_handle: int):
	return _steam_call("fileWriteStreamClose", [write_handle])

func fileWriteStreamOpen(file: String):
	return _steam_call("fileWriteStreamOpen", [file])

func fileWriteStreamWriteChunk(write_handle: int, data: PackedByteArray):
	return _steam_call("fileWriteStreamWriteChunk", [write_handle, data])

func filterText(context: int, steam_id: int, message: String):
	return _steam_call("filterText", [context, steam_id, message])

func find(search: String, currently_in_find: bool, reverse: bool, browser_handle: int = 0):
	return _steam_call("find", [search, currently_in_find, reverse, browser_handle])

func findLeaderboard(leaderboard_name: String):
	return _steam_call("findLeaderboard", [leaderboard_name])

func findOrCreateLeaderboard(leaderboard_name: String, sort_method: int, display_type: int):
	return _steam_call("findOrCreateLeaderboard", [leaderboard_name, sort_method, display_type])

func flushMessagesOnConnection(connection_handle: int):
	return _steam_call("flushMessagesOnConnection", [connection_handle])

func generateItems(items: PackedInt64Array, quantity: PackedInt32Array):
	return _steam_call("generateItems", [items, quantity])

func getAPICallFailureReason():
	return _steam_call("getAPICallFailureReason", [])

func getAchievement(achievement_name: String):
	return _steam_call("getAchievement", [achievement_name])

func getAchievementAchievedPercent(achievement_name: String):
	return _steam_call("getAchievementAchievedPercent", [achievement_name])

func getAchievementAndUnlockTime(achievement_name: String):
	return _steam_call("getAchievementAndUnlockTime", [achievement_name])

func getAchievementDisplayAttribute(achievement_name: String, key: String):
	return _steam_call("getAchievementDisplayAttribute", [achievement_name, key])

func getAchievementIcon(achievement_name: String):
	return _steam_call("getAchievementIcon", [achievement_name])

func getAchievementName(achievement_index: int):
	return _steam_call("getAchievementName", [achievement_index])

func getAchievementProgressLimitsFloat(achievement_name: String):
	return _steam_call("getAchievementProgressLimitsFloat", [achievement_name])

func getAchievementProgressLimitsInt(achievement_name: String):
	return _steam_call("getAchievementProgressLimitsInt", [achievement_name])

func getActionOriginFromXboxOrigin(input_handle: int, origin: int):
	return _steam_call("getActionOriginFromXboxOrigin", [input_handle, origin])

func getActionSetHandle(action_set_name: String):
	return _steam_call("getActionSetHandle", [action_set_name])

func getActiveActionSetLayers(input_handle: int):
	return _steam_call("getActiveActionSetLayers", [input_handle])

func getAllItems():
	return _steam_call("getAllItems", [])

func getAllLobbyData(steam_lobby_id: int):
	return _steam_call("getAllLobbyData", [steam_lobby_id])

func getAnalogActionData(input_handle: int, analog_action_handle: int):
	return _steam_call("getAnalogActionData", [input_handle, analog_action_handle])

func getAnalogActionHandle(action_name: String):
	return _steam_call("getAnalogActionHandle", [action_name])

func getAnalogActionOrigins(input_handle: int, action_set_handle: int, analog_action_handle: int):
	return _steam_call("getAnalogActionOrigins", [input_handle, action_set_handle, analog_action_handle])

func getAppBuildId():
	return _steam_call("getAppBuildId", [])

func getAppDependencies(published_file_id: int):
	return _steam_call("getAppDependencies", [published_file_id])

func getAppID():
	return _steam_call("getAppID", [])

func getAppInstallDir(app_id: int):
	return _steam_call("getAppInstallDir", [app_id])

func getAppOwner():
	return _steam_call("getAppOwner", [])

func getAuthSessionTicket(remote_steam_id: int = 0):
	return _steam_call("getAuthSessionTicket", [remote_steam_id])

func getAuthTicketForWebApi(service_identity: String = ""):
	return _steam_call("getAuthTicketForWebApi", [service_identity])

func getAuthenticationStatus():
	return _steam_call("getAuthenticationStatus", [])

func getAvailableBeaconLocations(max_locations: int):
	return _steam_call("getAvailableBeaconLocations", [max_locations])

func getAvailableGameLanguages():
	return _steam_call("getAvailableGameLanguages", [])

func getAvailableP2PPacketSize(channel: int = 0):
	return _steam_call("getAvailableP2PPacketSize", [channel])

func getAvailableVoice():
	return _steam_call("getAvailableVoice", [])

func getBeaconByIndex(index: int):
	return _steam_call("getBeaconByIndex", [index])

func getBeaconDetails(beacon_id: int):
	return _steam_call("getBeaconDetails", [beacon_id])

func getBeaconLocationData(location_id: int, location_type: int, location_data: int):
	return _steam_call("getBeaconLocationData", [location_id, location_type, location_data])

func getBetaInfo():
	return _steam_call("getBetaInfo", [])

func getCachedUGCCount():
	return _steam_call("getCachedUGCCount", [])

func getCachedUGCHandle(content: int):
	return _steam_call("getCachedUGCHandle", [content])

func getCertificateRequest():
	return _steam_call("getCertificateRequest", [])

func getChatMemberByIndex(clan_id: int, user: int):
	return _steam_call("getChatMemberByIndex", [clan_id, user])

func getClanActivityCounts(clan_id: int):
	return _steam_call("getClanActivityCounts", [clan_id])

func getClanByIndex(clan_index: int):
	return _steam_call("getClanByIndex", [clan_index])

func getClanChatMemberCount(clan_id: int):
	return _steam_call("getClanChatMemberCount", [clan_id])

func getClanCount():
	return _steam_call("getClanCount", [])

func getClanName(clan_id: int):
	return _steam_call("getClanName", [clan_id])

func getClanOfficerByIndex(clan_id: int, officer_index: int):
	return _steam_call("getClanOfficerByIndex", [clan_id, officer_index])

func getClanOfficerCount(clan_id: int):
	return _steam_call("getClanOfficerCount", [clan_id])

func getClanOwner(clan_id: int):
	return _steam_call("getClanOwner", [clan_id])

func getClanTag(clan_id: int):
	return _steam_call("getClanTag", [clan_id])

func getConfigValue(config_value: int, scope_type: int, connection_handle: int):
	return _steam_call("getConfigValue", [config_value, scope_type, connection_handle])

func getConfigValueInfo(config_value: int):
	return _steam_call("getConfigValueInfo", [config_value])

func getConnectedControllers():
	return _steam_call("getConnectedControllers", [])

func getConnectedUniverse():
	return _steam_call("getConnectedUniverse", [])

func getConnectionInfo(connection_handle: int):
	return _steam_call("getConnectionInfo", [connection_handle])

func getConnectionName(connection_handle: int):
	return _steam_call("getConnectionName", [connection_handle])

func getConnectionRealTimeStatus(connection_handle: int, lanes: int, get_status: bool = true):
	return _steam_call("getConnectionRealTimeStatus", [connection_handle, lanes, get_status])

func getConnectionUserData(connection_handle: int):
	return _steam_call("getConnectionUserData", [connection_handle])

func getControllerForGamepadIndex(index: int):
	return _steam_call("getControllerForGamepadIndex", [index])

func getCoplayFriend(friend_index: int):
	return _steam_call("getCoplayFriend", [friend_index])

func getCoplayFriendCount():
	return _steam_call("getCoplayFriendCount", [])

func getCurrentActionSet(input_handle: int):
	return _steam_call("getCurrentActionSet", [input_handle])

func getCurrentBatteryPower():
	return _steam_call("getCurrentBatteryPower", [])

func getCurrentBetaName():
	return _steam_call("getCurrentBetaName", [])

func getCurrentGameLanguage():
	return _steam_call("getCurrentGameLanguage", [])

func getDLCCount():
	return _steam_call("getDLCCount", [])

func getDLCData():
	return _steam_call("getDLCData", [])

func getDLCDataByIndex(this_dlc_index: int):
	return _steam_call("getDLCDataByIndex", [this_dlc_index])

func getDLCDownloadProgress(dlc_id: int):
	return _steam_call("getDLCDownloadProgress", [dlc_id])

func getDetailedConnectionStatus(connection_handle: int):
	return _steam_call("getDetailedConnectionStatus", [connection_handle])

func getDeviceBindingRevision(input_handle: int):
	return _steam_call("getDeviceBindingRevision", [input_handle])

func getDigitalActionData(input_handle: int, digital_action_handle: int):
	return _steam_call("getDigitalActionData", [input_handle, digital_action_handle])

func getDigitalActionHandle(action_name: String):
	return _steam_call("getDigitalActionHandle", [action_name])

func getDigitalActionOrigins(input_handle: int, action_set_handle: int, digital_action_handle: int):
	return _steam_call("getDigitalActionOrigins", [input_handle, action_set_handle, digital_action_handle])

func getDirectPingToPOP(pop_id: int):
	return _steam_call("getDirectPingToPOP", [pop_id])

func getDownloadedItems(max_entries: int):
	return _steam_call("getDownloadedItems", [max_entries])

func getDurationControl():
	return _steam_call("getDurationControl", [])

func getEarliestPurchaseUnixTime(app_id: int):
	return _steam_call("getEarliestPurchaseUnixTime", [app_id])

func getEncryptedAppTicket():
	return _steam_call("getEncryptedAppTicket", [])

func getFakeIP(first_port: int = 0):
	return _steam_call("getFakeIP", [first_port])

func getFavoriteGames():
	return _steam_call("getFavoriteGames", [])

func getFileCount():
	return _steam_call("getFileCount", [])

func getFileDetails(filename: String):
	return _steam_call("getFileDetails", [filename])

func getFileNameAndSize(file: int):
	return _steam_call("getFileNameAndSize", [file])

func getFileSize(file: String):
	return _steam_call("getFileSize", [file])

func getFileTimestamp(file: String):
	return _steam_call("getFileTimestamp", [file])

func getFollowerCount(steam_id: int):
	return _steam_call("getFollowerCount", [steam_id])

func getFriendByIndex(friend_number: int, friend_flags: int):
	return _steam_call("getFriendByIndex", [friend_number, friend_flags])

func getFriendCoplayGame(friend_id: int):
	return _steam_call("getFriendCoplayGame", [friend_id])

func getFriendCoplayTime(friend_id: int):
	return _steam_call("getFriendCoplayTime", [friend_id])

func getFriendCount(friend_flags: int = 65535):
	return _steam_call("getFriendCount", [friend_flags])

func getFriendCountFromSource(source_id: int):
	return _steam_call("getFriendCountFromSource", [source_id])

func getFriendFromSourceByIndex(source_id: int, friend_number: int):
	return _steam_call("getFriendFromSourceByIndex", [source_id, friend_number])

func getFriendGamePlayed(steam_id: int):
	return _steam_call("getFriendGamePlayed", [steam_id])

func getFriendPersonaName(steam_id: int):
	return _steam_call("getFriendPersonaName", [steam_id])

func getFriendPersonaNameHistory(steam_id: int, name_history: int):
	return _steam_call("getFriendPersonaNameHistory", [steam_id, name_history])

func getFriendPersonaState(steam_id: int):
	return _steam_call("getFriendPersonaState", [steam_id])

func getFriendRelationship(steam_id: int):
	return _steam_call("getFriendRelationship", [steam_id])

func getFriendRichPresence(friend_id: int, key: String):
	return _steam_call("getFriendRichPresence", [friend_id, key])

func getFriendRichPresenceKeyByIndex(friend_id: int, key_index: int):
	return _steam_call("getFriendRichPresenceKeyByIndex", [friend_id, key_index])

func getFriendRichPresenceKeyCount(friend_id: int):
	return _steam_call("getFriendRichPresenceKeyCount", [friend_id])

func getFriendSteamLevel(steam_id: int):
	return _steam_call("getFriendSteamLevel", [steam_id])

func getFriendsGroupCount():
	return _steam_call("getFriendsGroupCount", [])

func getFriendsGroupIDByIndex(friend_group: int):
	return _steam_call("getFriendsGroupIDByIndex", [friend_group])

func getFriendsGroupMembersCount(friend_group: int):
	return _steam_call("getFriendsGroupMembersCount", [friend_group])

func getFriendsGroupMembersList(friend_group: int, member_count: int):
	return _steam_call("getFriendsGroupMembersList", [friend_group, member_count])

func getFriendsGroupName(friend_group: int):
	return _steam_call("getFriendsGroupName", [friend_group])

func getGameBadgeLevel(series: int, foil: bool):
	return _steam_call("getGameBadgeLevel", [series, foil])

func getGamepadIndexForController(input_handle: int):
	return _steam_call("getGamepadIndexForController", [input_handle])

func getGlobalStatFloat(stat_name: String):
	return _steam_call("getGlobalStatFloat", [stat_name])

func getGlobalStatFloatHistory(stat_name: String):
	return _steam_call("getGlobalStatFloatHistory", [stat_name])

func getGlobalStatInt(stat_name: String):
	return _steam_call("getGlobalStatInt", [stat_name])

func getGlobalStatIntHistory(stat_name: String):
	return _steam_call("getGlobalStatIntHistory", [stat_name])

func getGlyphForActionOrigin(origin: int):
	return _steam_call("getGlyphForActionOrigin", [origin])

func getGlyphForXboxOrigin(origin: int):
	return _steam_call("getGlyphForXboxOrigin", [origin])

func getGlyphPNGForActionOrigin(origin: int, size: int, flags: int):
	return _steam_call("getGlyphPNGForActionOrigin", [origin, size, flags])

func getGlyphSVGForActionOrigin(origin: int, flags: int):
	return _steam_call("getGlyphSVGForActionOrigin", [origin, flags])

func getHTTPDownloadProgressPct(request_handle: int):
	return _steam_call("getHTTPDownloadProgressPct", [request_handle])

func getHTTPRequestWasTimedOut(request_handle: int):
	return _steam_call("getHTTPRequestWasTimedOut", [request_handle])

func getHTTPResponseBodyData(request_handle: int, buffer_size: int):
	return _steam_call("getHTTPResponseBodyData", [request_handle, buffer_size])

func getHTTPResponseBodySize(request_handle: int):
	return _steam_call("getHTTPResponseBodySize", [request_handle])

func getHTTPResponseHeaderSize(request_handle: int, header_name: String):
	return _steam_call("getHTTPResponseHeaderSize", [request_handle, header_name])

func getHTTPResponseHeaderValue(request_handle: int, header_name: String, buffer_size: int):
	return _steam_call("getHTTPResponseHeaderValue", [request_handle, header_name, buffer_size])

func getHTTPStreamingResponseBodyData(request_handle: int, offset: int, buffer_size: int):
	return _steam_call("getHTTPStreamingResponseBodyData", [request_handle, offset, buffer_size])

func getHostedDedicatedServerPOPId():
	return _steam_call("getHostedDedicatedServerPOPId", [])

func getHostedDedicatedServerPort():
	return _steam_call("getHostedDedicatedServerPort", [])

func getIPCCallCount():
	return _steam_call("getIPCCallCount", [])

func getIPCountry():
	return _steam_call("getIPCountry", [])

func getIPv4FakeIPType(ipv4: String):
	return _steam_call("getIPv4FakeIPType", [ipv4])

func getIPv6ConnectivityState(protocol: int):
	return _steam_call("getIPv6ConnectivityState", [protocol])

func getImageRGBA(image_handle: int):
	return _steam_call("getImageRGBA", [image_handle])

func getImageSize(image_handle: int):
	return _steam_call("getImageSize", [image_handle])

func getInput(max_events: int):
	return _steam_call("getInput", [max_events])

func getInputTypeForHandle(input_handle: int):
	return _steam_call("getInputTypeForHandle", [input_handle])

func getInstalledDepots(app_id: int):
	return _steam_call("getInstalledDepots", [app_id])

func getItemDefinitionProperty(definition: int, name: String):
	return _steam_call("getItemDefinitionProperty", [definition, name])

func getItemDownloadInfo(published_file_id: int):
	return _steam_call("getItemDownloadInfo", [published_file_id])

func getItemInstallInfo(published_file_id: int):
	return _steam_call("getItemInstallInfo", [published_file_id])

func getItemPrice(definition: int):
	return _steam_call("getItemPrice", [definition])

func getItemState(published_file_id: int):
	return _steam_call("getItemState", [published_file_id])

func getItemUpdateProgress(update_handle: int):
	return _steam_call("getItemUpdateProgress", [update_handle])

func getItemsByID(id_array: PackedInt64Array):
	return _steam_call("getItemsByID", [id_array])

func getItemsWithPrices():
	return _steam_call("getItemsWithPrices", [])

func getLargeFriendAvatar(steam_id: int):
	return _steam_call("getLargeFriendAvatar", [steam_id])

func getLargeSessionAvatar(session_id: int):
	return _steam_call("getLargeSessionAvatar", [session_id])

func getLaunchCommandLine():
	return _steam_call("getLaunchCommandLine", [])

func getLaunchQueryParam(key: String):
	return _steam_call("getLaunchQueryParam", [key])

func getLeaderboardDisplayType(this_leaderboard: int = 0):
	return _steam_call("getLeaderboardDisplayType", [this_leaderboard])

func getLeaderboardEntryCount(this_leaderboard: int = 0):
	return _steam_call("getLeaderboardEntryCount", [this_leaderboard])

func getLeaderboardName(this_leaderboard: int = 0):
	return _steam_call("getLeaderboardName", [this_leaderboard])

func getLeaderboardSortMethod(this_leaderboard: int = 0):
	return _steam_call("getLeaderboardSortMethod", [this_leaderboard])

func getLinkAtPosition(x: int, y: int, browser_handle: int = 0):
	return _steam_call("getLinkAtPosition", [x, y, browser_handle])

func getListenSocketAddress(socket: int, with_port: bool = true):
	return _steam_call("getListenSocketAddress", [socket, with_port])

func getLobbyData(steam_lobby_id: int, key: String):
	return _steam_call("getLobbyData", [steam_lobby_id, key])

func getLobbyGameServer(steam_lobby_id: int):
	return _steam_call("getLobbyGameServer", [steam_lobby_id])

func getLobbyMemberByIndex(steam_lobby_id: int, member: int):
	return _steam_call("getLobbyMemberByIndex", [steam_lobby_id, member])

func getLobbyMemberData(steam_lobby_id: int, steam_id_user: int, key: String):
	return _steam_call("getLobbyMemberData", [steam_lobby_id, steam_id_user, key])

func getLobbyMemberLimit(steam_lobby_id: int):
	return _steam_call("getLobbyMemberLimit", [steam_lobby_id])

func getLobbyOwner(steam_lobby_id: int):
	return _steam_call("getLobbyOwner", [steam_lobby_id])

func getLocalFileChange(file: int):
	return _steam_call("getLocalFileChange", [file])

func getLocalFileChangeCount():
	return _steam_call("getLocalFileChangeCount", [])

func getLocalPingLocation():
	return _steam_call("getLocalPingLocation", [])

func getLocalTimestamp():
	return _steam_call("getLocalTimestamp", [])

func getMediumFriendAvatar(steam_id: int):
	return _steam_call("getMediumFriendAvatar", [steam_id])

func getMediumSessionAvatar(session_id: int):
	return _steam_call("getMediumSessionAvatar", [session_id])

func getMostAchievedAchievementInfo():
	return _steam_call("getMostAchievedAchievementInfo", [])

func getMotionData(input_handle: int):
	return _steam_call("getMotionData", [input_handle])

func getNextMostAchievedAchievementInfo(iterator: int):
	return _steam_call("getNextMostAchievedAchievementInfo", [iterator])

func getNumAchievements():
	return _steam_call("getNumAchievements", [])

func getNumActiveBeacons():
	return _steam_call("getNumActiveBeacons", [])

func getNumAvailableBeaconLocations():
	return _steam_call("getNumAvailableBeaconLocations", [])

func getNumBetas():
	return _steam_call("getNumBetas", [])

func getNumDownloadedItems():
	return _steam_call("getNumDownloadedItems", [])

func getNumLobbyMembers(steam_lobby_id: int):
	return _steam_call("getNumLobbyMembers", [steam_lobby_id])

func getNumSubscribedItems(include_locally_disabled: bool = false):
	return _steam_call("getNumSubscribedItems", [include_locally_disabled])

func getNumSupportedGameVersions(query_handle: int, index: int):
	return _steam_call("getNumSupportedGameVersions", [query_handle, index])

func getNumberOfCurrentPlayers():
	return _steam_call("getNumberOfCurrentPlayers", [])

func getOPFSettings(app_id: int):
	return _steam_call("getOPFSettings", [app_id])

func getOPFStringForApp(app_id: int):
	return _steam_call("getOPFStringForApp", [app_id])

func getP2PSessionState(remote_steam_id: int):
	return _steam_call("getP2PSessionState", [remote_steam_id])

func getPOPCount():
	return _steam_call("getPOPCount", [])

func getPOPList():
	return _steam_call("getPOPList", [])

func getPersonaName():
	return _steam_call("getPersonaName", [])

func getPersonaState():
	return _steam_call("getPersonaState", [])

func getPingToDataCenter(pop_id: int):
	return _steam_call("getPingToDataCenter", [pop_id])

func getPlaybackStatus():
	return _steam_call("getPlaybackStatus", [])

func getPlayerAvatar(size: int = 2, steam_id: int = 0):
	return _steam_call("getPlayerAvatar", [size, steam_id])

func getPlayerNickname(steam_id: int):
	return _steam_call("getPlayerNickname", [steam_id])

func getPlayerSteamLevel():
	return _steam_call("getPlayerSteamLevel", [])

func getProfileItemPropertyInt(steam_id: int, item_type: int, item_property: int):
	return _steam_call("getProfileItemPropertyInt", [steam_id, item_type, item_property])

func getProfileItemPropertyString(steam_id: int, item_type: int, item_property: int):
	return _steam_call("getProfileItemPropertyString", [steam_id, item_type, item_property])

func getQueryUGCAdditionalPreview(query_handle: int, index: int, preview_index: int):
	return _steam_call("getQueryUGCAdditionalPreview", [query_handle, index, preview_index])

func getQueryUGCChildren(query_handle: int, index: int, child_count: int):
	return _steam_call("getQueryUGCChildren", [query_handle, index, child_count])

func getQueryUGCContentDescriptors(query_handle: int, index: int, max_entries: int = 5):
	return _steam_call("getQueryUGCContentDescriptors", [query_handle, index, max_entries])

func getQueryUGCKeyValueTag(query_handle: int, index: int, key_value_tag_index: int):
	return _steam_call("getQueryUGCKeyValueTag", [query_handle, index, key_value_tag_index])

func getQueryUGCMetadata(query_handle: int, index: int):
	return _steam_call("getQueryUGCMetadata", [query_handle, index])

func getQueryUGCNumAdditionalPreviews(query_handle: int, index: int):
	return _steam_call("getQueryUGCNumAdditionalPreviews", [query_handle, index])

func getQueryUGCNumKeyValueTags(query_handle: int, index: int):
	return _steam_call("getQueryUGCNumKeyValueTags", [query_handle, index])

func getQueryUGCNumTags(query_handle: int, index: int):
	return _steam_call("getQueryUGCNumTags", [query_handle, index])

func getQueryUGCPreviewURL(query_handle: int, index: int):
	return _steam_call("getQueryUGCPreviewURL", [query_handle, index])

func getQueryUGCResult(query_handle: int, index: int):
	return _steam_call("getQueryUGCResult", [query_handle, index])

func getQueryUGCStatistic(query_handle: int, index: int, stat_type: int):
	return _steam_call("getQueryUGCStatistic", [query_handle, index, stat_type])

func getQueryUGCTag(query_handle: int, index: int, tag_index: int):
	return _steam_call("getQueryUGCTag", [query_handle, index, tag_index])

func getQueryUGCTagDisplayName(query_handle: int, index: int, tag_index: int):
	return _steam_call("getQueryUGCTagDisplayName", [query_handle, index, tag_index])

func getQuota():
	return _steam_call("getQuota", [])

func getRealIdentityForFakeIP(fake_ip: String):
	return _steam_call("getRealIdentityForFakeIP", [fake_ip])

func getRecentPlayers():
	return _steam_call("getRecentPlayers", [])

func getRelayNetworkStatus():
	return _steam_call("getRelayNetworkStatus", [])

func getRemoteFakeIPForConnection(connection: int):
	return _steam_call("getRemoteFakeIPForConnection", [connection])

func getRemotePlaySessionID(input_handle: int):
	return _steam_call("getRemotePlaySessionID", [input_handle])

func getResultItemProperty(index: int, name: String, this_inventory_handle: int = 0):
	return _steam_call("getResultItemProperty", [index, name, this_inventory_handle])

func getResultItems(this_inventory_handle: int = 0):
	return _steam_call("getResultItems", [this_inventory_handle])

func getResultStatus(this_inventory_handle: int = 0):
	return _steam_call("getResultStatus", [this_inventory_handle])

func getResultTimestamp(this_inventory_handle: int = 0):
	return _steam_call("getResultTimestamp", [this_inventory_handle])

func getSecondsSinceAppActive():
	return _steam_call("getSecondsSinceAppActive", [])

func getSecondsSinceComputerActive():
	return _steam_call("getSecondsSinceComputerActive", [])

func getServerCount(server_list_request: int):
	return _steam_call("getServerCount", [server_list_request])

func getServerDetails(server: int, server_list_request: int):
	return _steam_call("getServerDetails", [server, server_list_request])

func getServerRealTime():
	return _steam_call("getServerRealTime", [])

func getSessionClientFormFactor(session_id: int):
	return _steam_call("getSessionClientFormFactor", [session_id])

func getSessionClientName(session_id: int):
	return _steam_call("getSessionClientName", [session_id])

func getSessionClientResolution(session_id: int):
	return _steam_call("getSessionClientResolution", [session_id])

func getSessionConnectionInfo(remote_steam_id: int, get_connection: bool, get_status: bool):
	return _steam_call("getSessionConnectionInfo", [remote_steam_id, get_connection, get_status])

func getSessionCount():
	return _steam_call("getSessionCount", [])

func getSessionGuestID(session_id: int):
	return _steam_call("getSessionGuestID", [session_id])

func getSessionID(index: int):
	return _steam_call("getSessionID", [index])

func getSessionInputConfigurationSettings():
	return _steam_call("getSessionInputConfigurationSettings", [])

func getSessionSteamID(session_id: int):
	return _steam_call("getSessionSteamID", [session_id])

func getSmallFriendAvatar(steam_id: int):
	return _steam_call("getSmallFriendAvatar", [steam_id])

func getSmallSessionAvatar(session_id: int):
	return _steam_call("getSmallSessionAvatar", [session_id])

func getStatFloat(stat_name: String):
	return _steam_call("getStatFloat", [stat_name])

func getStatInt(stat_name: String):
	return _steam_call("getStatInt", [stat_name])

func getSteamHardwareDefaultConfig():
	return _steam_call("getSteamHardwareDefaultConfig", [])

func getSteamID():
	return _steam_call("getSteamID", [])

func getSteamID32(steam_id: int):
	return _steam_call("getSteamID32", [steam_id])

func getSteamUILanguage():
	return _steam_call("getSteamUILanguage", [])

func getStringForActionOrigin(origin: int):
	return _steam_call("getStringForActionOrigin", [origin])

func getStringForAnalogActionName(action_handle: int):
	return _steam_call("getStringForAnalogActionName", [action_handle])

func getStringForDigitalActionName(action_handle: int):
	return _steam_call("getStringForDigitalActionName", [action_handle])

func getStringForXboxOrigin(origin: int):
	return _steam_call("getStringForXboxOrigin", [origin])

func getSubscribedItems(include_locally_disabled: bool = false):
	return _steam_call("getSubscribedItems", [include_locally_disabled])

func getSupportedGameVersionData(query_handle: int, index: int, version_index: int):
	return _steam_call("getSupportedGameVersionData", [query_handle, index, version_index])

func getSyncPlatforms(file: String):
	return _steam_call("getSyncPlatforms", [file])

func getUGCDetails(content: int):
	return _steam_call("getUGCDetails", [content])

func getUGCDownloadProgress(content: int):
	return _steam_call("getUGCDownloadProgress", [content])

func getUserAchievement(steam_id: int, achievement_name: String):
	return _steam_call("getUserAchievement", [steam_id, achievement_name])

func getUserAchievementAndUnlockTime(steam_id: int, achievement_name: String):
	return _steam_call("getUserAchievementAndUnlockTime", [steam_id, achievement_name])

func getUserContentDescriptorPreferences(max_entries: int = 5):
	return _steam_call("getUserContentDescriptorPreferences", [max_entries])

func getUserFriendsGroups():
	return _steam_call("getUserFriendsGroups", [])

func getUserItemVote(published_file_id: int):
	return _steam_call("getUserItemVote", [published_file_id])

func getUserStatFloat(steam_id: int, stat_name: String):
	return _steam_call("getUserStatFloat", [steam_id, stat_name])

func getUserStatInt(steam_id: int, stat_name: String):
	return _steam_call("getUserStatInt", [steam_id, stat_name])

func getUserSteamFriends():
	return _steam_call("getUserSteamFriends", [])

func getUserSteamGroups():
	return _steam_call("getUserSteamGroups", [])

func getVideoURL(app_id: int):
	return _steam_call("getVideoURL", [app_id])

func getVoice(buffer_size: int = 1024):
	return _steam_call("getVoice", [buffer_size])

func getVoiceOptimalSampleRate():
	return _steam_call("getVoiceOptimalSampleRate", [])

func getWorkshopEULAStatus():
	return _steam_call("getWorkshopEULAStatus", [])

func get_godotsteam_version():
	return _steam_call("get_godotsteam_version", [])

func get_server_list_request():
	return _steam_call("get_server_list_request", [])

func get_steam_init_result():
	return _steam_call("get_steam_init_result", [])

func goBack(browser_handle: int = 0):
	return _steam_call("goBack", [browser_handle])

func goForward(browser_handle: int = 0):
	return _steam_call("goForward", [browser_handle])

func grantPromoItems():
	return _steam_call("grantPromoItems", [])

func hasEquippedProfileItem(steam_id: int, friend_flags: int):
	return _steam_call("hasEquippedProfileItem", [steam_id, friend_flags])

func hasFriend(steam_id: int, friend_flags: int):
	return _steam_call("hasFriend", [steam_id, friend_flags])

func hookScreenshots(hook: bool):
	return _steam_call("hookScreenshots", [hook])

func htmlInit():
	return _steam_call("htmlInit", [])

func htmlShutdown():
	return _steam_call("htmlShutdown", [])

func indicateAchievementProgress(achievement_name: String, current_progress: int, max_progress: int):
	return _steam_call("indicateAchievementProgress", [achievement_name, current_progress, max_progress])

func initAuthentication():
	return _steam_call("initAuthentication", [])

func initFilterText():
	return _steam_call("initFilterText", [])

func initRelayNetworkAccess():
	return _steam_call("initRelayNetworkAccess", [])

func initWorkshopForGameServer(workshop_depot_id: int, folder: String):
	return _steam_call("initWorkshopForGameServer", [workshop_depot_id, folder])

func initiateGameConnection(server_id: int, server_ip: String, server_port: int, secure: bool):
	return _steam_call("initiateGameConnection", [server_id, server_ip, server_port, secure])

func inputInit(explicitly_call_runframe: bool = false):
	return _steam_call("inputInit", [explicitly_call_runframe])

func inputShutdown():
	return _steam_call("inputShutdown", [])

func installDLC(dlc_id: int):
	return _steam_call("installDLC", [dlc_id])

func inviteUserToGame(friend_id: int, connect_string: String):
	return _steam_call("inviteUserToGame", [friend_id, connect_string])

func inviteUserToLobby(steam_lobby_id: int, steam_id_invitee: int):
	return _steam_call("inviteUserToLobby", [steam_lobby_id, steam_id_invitee])

func isAPICallCompleted():
	return _steam_call("isAPICallCompleted", [])

func isAnonAccount(steam_id: int):
	return _steam_call("isAnonAccount", [steam_id])

func isAnonUserAccount(steam_id: int):
	return _steam_call("isAnonUserAccount", [steam_id])

func isAppBlocked(app_id: int):
	return _steam_call("isAppBlocked", [app_id])

func isAppInBlockList(app_id: int):
	return _steam_call("isAppInBlockList", [app_id])

func isAppInstalled(app_id: int):
	return _steam_call("isAppInstalled", [app_id])

func isBehindNAT():
	return _steam_call("isBehindNAT", [])

func isBroadcasting():
	return _steam_call("isBroadcasting", [])

func isChatAccount(steam_id: int):
	return _steam_call("isChatAccount", [steam_id])

func isClanAccount(steam_id: int):
	return _steam_call("isClanAccount", [steam_id])

func isClanChatAdmin(chat_id: int, steam_id: int):
	return _steam_call("isClanChatAdmin", [chat_id, steam_id])

func isClanChatWindowOpenInSteam(chat_id: int):
	return _steam_call("isClanChatWindowOpenInSteam", [chat_id])

func isClanOfficialGameGroup(clan_id: int):
	return _steam_call("isClanOfficialGameGroup", [clan_id])

func isClanPublic(clan_id: int):
	return _steam_call("isClanPublic", [clan_id])

func isCloudEnabledForAccount():
	return _steam_call("isCloudEnabledForAccount", [])

func isCloudEnabledForApp():
	return _steam_call("isCloudEnabledForApp", [])

func isConsoleUserAccount(steam_id: int):
	return _steam_call("isConsoleUserAccount", [steam_id])

func isCybercafe():
	return _steam_call("isCybercafe", [])

func isDLCInstalled(dlc_id: int):
	return _steam_call("isDLCInstalled", [dlc_id])

func isFakeIPv4(ip_address: String):
	return _steam_call("isFakeIPv4", [ip_address])

func isFeatureBlocked(feature: int):
	return _steam_call("isFeatureBlocked", [feature])

func isFeatureInBlockList(feature: int):
	return _steam_call("isFeatureInBlockList", [feature])

func isFollowing(steam_id: int):
	return _steam_call("isFollowing", [steam_id])

func isIndividualAccount(steam_id: int):
	return _steam_call("isIndividualAccount", [steam_id])

func isLobby(steam_id: int):
	return _steam_call("isLobby", [steam_id])

func isLowViolence():
	return _steam_call("isLowViolence", [])

func isOverlayEnabled():
	return _steam_call("isOverlayEnabled", [])

func isParentalLockEnabled():
	return _steam_call("isParentalLockEnabled", [])

func isParentalLockLocked():
	return _steam_call("isParentalLockLocked", [])

func isPhoneIdentifying():
	return _steam_call("isPhoneIdentifying", [])

func isPhoneRequiringVerification():
	return _steam_call("isPhoneRequiringVerification", [])

func isPhoneVerified():
	return _steam_call("isPhoneVerified", [])

func isRefreshing(server_list_request: int):
	return _steam_call("isRefreshing", [server_list_request])

func isRunningOnSteamHardware():
	return _steam_call("isRunningOnSteamHardware", [])

func isRunningUnderProton():
	return _steam_call("isRunningUnderProton", [])

func isScreenshotsHooked():
	return _steam_call("isScreenshotsHooked", [])

func isSteamChinaLauncher():
	return _steam_call("isSteamChinaLauncher", [])

func isSteamInBigPictureMode():
	return _steam_call("isSteamInBigPictureMode", [])

func isSteamRunning():
	return _steam_call("isSteamRunning", [])

func isSteamRunningInVR():
	return _steam_call("isSteamRunningInVR", [])

func isSubscribed():
	return _steam_call("isSubscribed", [])

func isSubscribedApp(app_id: int):
	return _steam_call("isSubscribedApp", [app_id])

func isSubscribedFromFamilySharing():
	return _steam_call("isSubscribedFromFamilySharing", [])

func isSubscribedFromFreeWeekend():
	return _steam_call("isSubscribedFromFreeWeekend", [])

func isTimedTrial():
	return _steam_call("isTimedTrial", [])

func isTwoFactorEnabled():
	return _steam_call("isTwoFactorEnabled", [])

func isUserInSource(steam_id: int, source_id: int):
	return _steam_call("isUserInSource", [steam_id, source_id])

func isVACBanned():
	return _steam_call("isVACBanned", [])

func isVRHeadsetStreamingEnabled():
	return _steam_call("isVRHeadsetStreamingEnabled", [])

func iterateGenericEditableConfigValues(current_value: int, enumerate_dev_vars: bool):
	return _steam_call("iterateGenericEditableConfigValues", [current_value, enumerate_dev_vars])

func joinClanChatRoom(clan_id: int):
	return _steam_call("joinClanChatRoom", [clan_id])

func joinLobby(steam_lobby_id: int):
	return _steam_call("joinLobby", [steam_lobby_id])

func joinParty(beacon_id: int):
	return _steam_call("joinParty", [beacon_id])

func jsDialogResponse(result: bool, browser_handle: int = 0):
	return _steam_call("jsDialogResponse", [result, browser_handle])

func keyChar(unicode_char: int, key_modifiers: int, browser_handle: int = 0):
	return _steam_call("keyChar", [unicode_char, key_modifiers, browser_handle])

func keyDown(native_key_code: int, key_modifiers: int, browser_handle: int = 0, is_system_key: bool = false):
	return _steam_call("keyDown", [native_key_code, key_modifiers, browser_handle, is_system_key])

func keyUp(native_key_code: int, key_modifiers: int, browser_handle: int = 0):
	return _steam_call("keyUp", [native_key_code, key_modifiers, browser_handle])

func leaveClanChatRoom(clan_id: int):
	return _steam_call("leaveClanChatRoom", [clan_id])

func leaveLobby(steam_lobby_id: int):
	return _steam_call("leaveLobby", [steam_lobby_id])

func loadItemDefinitions():
	return _steam_call("loadItemDefinitions", [])

func loadURL(url: String, post_data: String, browser_handle: int = 0):
	return _steam_call("loadURL", [url, post_data, browser_handle])

func loggedOn():
	return _steam_call("loggedOn", [])

func markContentCorrupt(missing_files_only: bool):
	return _steam_call("markContentCorrupt", [missing_files_only])

func markDownloadedItemAsUnused(published_file_id: int):
	return _steam_call("markDownloadedItemAsUnused", [published_file_id])

func mouseDoubleClick(mouse_button: int, browser_handle: int = 0):
	return _steam_call("mouseDoubleClick", [mouse_button, browser_handle])

func mouseDown(mouse_button: int, browser_handle: int = 0):
	return _steam_call("mouseDown", [mouse_button, browser_handle])

func mouseMove(x: int, y: int, browser_handle: int = 0):
	return _steam_call("mouseMove", [x, y, browser_handle])

func mouseUp(mouse_button: int, browser_handle: int = 0):
	return _steam_call("mouseUp", [mouse_button, browser_handle])

func mouseWheel(delta: int, browser_handle: int = 0):
	return _steam_call("mouseWheel", [delta, browser_handle])

func musicGetVolume():
	return _steam_call("musicGetVolume", [])

func musicIsEnabled():
	return _steam_call("musicIsEnabled", [])

func musicIsPlaying():
	return _steam_call("musicIsPlaying", [])

func musicPause():
	return _steam_call("musicPause", [])

func musicPlay():
	return _steam_call("musicPlay", [])

func musicPlayNext():
	return _steam_call("musicPlayNext", [])

func musicPlayPrev():
	return _steam_call("musicPlayPrev", [])

func musicSetVolume(volume: float):
	return _steam_call("musicSetVolume", [volume])

func newDataAvailable():
	return _steam_call("newDataAvailable", [])

func onReservationCompleted(beacon_id: int, steam_id: int):
	return _steam_call("onReservationCompleted", [beacon_id, steam_id])

func openClanChatWindowInSteam(chat_id: int):
	return _steam_call("openClanChatWindowInSteam", [chat_id])

func openDeveloperTools(browser_handle: int = 0):
	return _steam_call("openDeveloperTools", [browser_handle])

func openOverlayToGamePhase(phase_id: String):
	return _steam_call("openOverlayToGamePhase", [phase_id])

func openOverlayToTimelineEvent(timeline_event_handle: int):
	return _steam_call("openOverlayToTimelineEvent", [timeline_event_handle])

func overlayNeedsPresent():
	return _steam_call("overlayNeedsPresent", [])

func parsePingLocationString(string: String):
	return _steam_call("parsePingLocationString", [string])

func pasteFromClipboard(browser_handle: int = 0):
	return _steam_call("pasteFromClipboard", [browser_handle])

func pingServer(ip: String, port: int):
	return _steam_call("pingServer", [ip, port])

func playerDetails(ip: String, port: int):
	return _steam_call("playerDetails", [ip, port])

func prioritizeHTTPRequest(request_handle: int):
	return _steam_call("prioritizeHTTPRequest", [request_handle])

func readP2PPacket(packet_size: int, channel: int = 0):
	return _steam_call("readP2PPacket", [packet_size, channel])

func receiveMessagesOnChannel(channel: int, max_messages: int):
	return _steam_call("receiveMessagesOnChannel", [channel, max_messages])

func receiveMessagesOnConnection(connection_handle: int, max_messages: int):
	return _steam_call("receiveMessagesOnConnection", [connection_handle, max_messages])

func receiveMessagesOnPollGroup(poll_group: int, max_messages: int):
	return _steam_call("receiveMessagesOnPollGroup", [poll_group, max_messages])

func refreshQuery(server_list_request: int):
	return _steam_call("refreshQuery", [server_list_request])

func refreshServer(server: int, server_list_request: int):
	return _steam_call("refreshServer", [server, server_list_request])

func registerProtocolInOverlayBrowser(protocol: String):
	return _steam_call("registerProtocolInOverlayBrowser", [protocol])

func releaseCookieContainer(cookie_handle: int):
	return _steam_call("releaseCookieContainer", [cookie_handle])

func releaseCurrentThreadMemory():
	return _steam_call("releaseCurrentThreadMemory", [])

func releaseHTTPRequest(request_handle: int):
	return _steam_call("releaseHTTPRequest", [request_handle])

func releaseQueryUGCRequest(query_handle: int):
	return _steam_call("releaseQueryUGCRequest", [query_handle])

func releaseRequest(server_list_request: int):
	return _steam_call("releaseRequest", [server_list_request])

func reload(browser_handle: int = 0):
	return _steam_call("reload", [browser_handle])

func removeAllItemKeyValueTags(update_handle: int):
	return _steam_call("removeAllItemKeyValueTags", [update_handle])

func removeAppDependency(published_file_id: int, app_id: int):
	return _steam_call("removeAppDependency", [published_file_id, app_id])

func removeBrowser(browser_handle: int = 0):
	return _steam_call("removeBrowser", [browser_handle])

func removeContentDescriptor(update_handle: int, descriptor_id: int):
	return _steam_call("removeContentDescriptor", [update_handle, descriptor_id])

func removeDependency(published_file_id: int, child_published_file_id: int):
	return _steam_call("removeDependency", [published_file_id, child_published_file_id])

func removeFavoriteGame(app_id: int, ip: String, port: int, query_port: int, flags: int):
	return _steam_call("removeFavoriteGame", [app_id, ip, port, query_port, flags])

func removeItemFromFavorites(app_id: int, published_file_id: int):
	return _steam_call("removeItemFromFavorites", [app_id, published_file_id])

func removeItemKeyValueTags(update_handle: int, key: String):
	return _steam_call("removeItemKeyValueTags", [update_handle, key])

func removeItemPreview(update_handle: int, index: int):
	return _steam_call("removeItemPreview", [update_handle, index])

func removeProperty(item_id: int, name: String, this_inventory_update_handle: int = 0):
	return _steam_call("removeProperty", [item_id, name, this_inventory_update_handle])

func removeTimelineEvent(timeline_event_handle: int):
	return _steam_call("removeTimelineEvent", [timeline_event_handle])

func replyToFriendMessage(steam_id: int, message: String):
	return _steam_call("replyToFriendMessage", [steam_id, message])

func requestClanOfficerList(clan_id: int):
	return _steam_call("requestClanOfficerList", [clan_id])

func requestEligiblePromoItemDefinitionsIDs(steam_id: int):
	return _steam_call("requestEligiblePromoItemDefinitionsIDs", [steam_id])

func requestEncryptedAppTicket(secret: String):
	return _steam_call("requestEncryptedAppTicket", [secret])

func requestEquippedProfileItems(steam_id: int):
	return _steam_call("requestEquippedProfileItems", [steam_id])

func requestFavoritesServerList(app_id: int, filters: Array):
	return _steam_call("requestFavoritesServerList", [app_id, filters])

func requestFriendRichPresence(friend_id: int):
	return _steam_call("requestFriendRichPresence", [friend_id])

func requestFriendsServerList(app_id: int, filters: Array):
	return _steam_call("requestFriendsServerList", [app_id, filters])

func requestGlobalAchievementPercentages():
	return _steam_call("requestGlobalAchievementPercentages", [])

func requestGlobalStats(history_days: int):
	return _steam_call("requestGlobalStats", [history_days])

func requestHistoryServerList(app_id: int, filters: Array):
	return _steam_call("requestHistoryServerList", [app_id, filters])

func requestInternetServerList(app_id: int, filters: Array):
	return _steam_call("requestInternetServerList", [app_id, filters])

func requestLANServerList(app_id: int):
	return _steam_call("requestLANServerList", [app_id])

func requestLobbyData(steam_lobby_id: int):
	return _steam_call("requestLobbyData", [steam_lobby_id])

func requestLobbyList():
	return _steam_call("requestLobbyList", [])

func requestPrices():
	return _steam_call("requestPrices", [])

func requestSpectatorServerList(app_id: int, filters: Array):
	return _steam_call("requestSpectatorServerList", [app_id, filters])

func requestStoreAuthURL(redirect_url: String):
	return _steam_call("requestStoreAuthURL", [redirect_url])

func requestUserInformation(steam_id: int, require_name_only: bool):
	return _steam_call("requestUserInformation", [steam_id, require_name_only])

func requestUserStats(steam_id: int):
	return _steam_call("requestUserStats", [steam_id])

func resetAllStats(achievements_too: bool):
	return _steam_call("resetAllStats", [achievements_too])

func resetIdentity(remote_steam_id: int):
	return _steam_call("resetIdentity", [remote_steam_id])

func restartAppIfNecessary(app_id: int):
	return _steam_call("restartAppIfNecessary", [app_id])

func runFrame(reserved_value: bool = true):
	return _steam_call("runFrame", [reserved_value])

func runNetworkingCallbacks():
	return _steam_call("runNetworkingCallbacks", [])

func run_callbacks():
	return _steam_call("run_callbacks", [])

func sendClanChatMessage(chat_id: int, text: String):
	return _steam_call("sendClanChatMessage", [chat_id, text])

func sendHTTPRequest(request_handle: int):
	return _steam_call("sendHTTPRequest", [request_handle])

func sendHTTPRequestAndStreamResponse(request_handle: int):
	return _steam_call("sendHTTPRequestAndStreamResponse", [request_handle])

func sendLobbyChatMsg(steam_lobby_id: int, message_body: String):
	return _steam_call("sendLobbyChatMsg", [steam_lobby_id, message_body])

func sendMessageToConnection(connection_handle: int, message: PackedByteArray, flags: int):
	return _steam_call("sendMessageToConnection", [connection_handle, message, flags])

func sendMessageToUser(remote_steam_id: int, data: PackedByteArray, flags: int, channel: int):
	return _steam_call("sendMessageToUser", [remote_steam_id, data, flags, channel])

func sendMessages(connection_handle: int, messages: Array, flags: int, delete_failed_messages: bool):
	return _steam_call("sendMessages", [connection_handle, messages, flags, delete_failed_messages])

func sendP2PPacket(remote_steam_id: int, data: PackedByteArray, send_type: int, channel: int = 0):
	return _steam_call("sendP2PPacket", [remote_steam_id, data, send_type, channel])

func sendQueryUGCRequest(query_handle: int):
	return _steam_call("sendQueryUGCRequest", [query_handle])

func sendRemotePlayTogetherInvite(friend_id: int):
	return _steam_call("sendRemotePlayTogetherInvite", [friend_id])

func serializeResult(this_inventory_handle: int = 0):
	return _steam_call("serializeResult", [this_inventory_handle])

func serverFriends(ip: String, port: int):
	return _steam_call("serverFriends", [ip, port])

func serverRules(ip: String, port: int):
	return _steam_call("serverRules", [ip, port])

func sessionRemotePlayTogether(session_id: int):
	return _steam_call("sessionRemotePlayTogether", [session_id])

func setAchievement(achievement_name: String):
	return _steam_call("setAchievement", [achievement_name])

func setActiveBeta(beta_name: String):
	return _steam_call("setActiveBeta", [beta_name])

func setAdminQuery(query_handle: int, admin_query: bool):
	return _steam_call("setAdminQuery", [query_handle, admin_query])

func setAllowCachedResponse(update_handle: int, max_age_seconds: int):
	return _steam_call("setAllowCachedResponse", [update_handle, max_age_seconds])

func setAllowLegacyUpload(update_handle: int, allow_legacy_upload: bool):
	return _steam_call("setAllowLegacyUpload", [update_handle, allow_legacy_upload])

func setBackgroundMode(background_mode: bool, browser_handle: int = 0):
	return _steam_call("setBackgroundMode", [background_mode, browser_handle])

func setCertificate(certificate: PackedByteArray):
	return _steam_call("setCertificate", [certificate])

func setCloudEnabledForApp(enabled: bool):
	return _steam_call("setCloudEnabledForApp", [enabled])

func setCloudFileNameFilter(query_handle: int, match_cloud_filename: String):
	return _steam_call("setCloudFileNameFilter", [query_handle, match_cloud_filename])

func setConnectionConfigValueFloat(connection_handle: int, config: int, value: float):
	return _steam_call("setConnectionConfigValueFloat", [connection_handle, config, value])

func setConnectionConfigValueInt32(connection_handle: int, config: int, value: int):
	return _steam_call("setConnectionConfigValueInt32", [connection_handle, config, value])

func setConnectionConfigValueString(connection_handle: int, config: int, value: String):
	return _steam_call("setConnectionConfigValueString", [connection_handle, config, value])

func setConnectionName(connection_handle: int, name: String):
	return _steam_call("setConnectionName", [connection_handle, name])

func setConnectionPollGroup(connection_handle: int, poll_group: int):
	return _steam_call("setConnectionPollGroup", [connection_handle, poll_group])

func setConnectionUserData(connection_handle: int, user_data: int):
	return _steam_call("setConnectionUserData", [connection_handle, user_data])

func setCookie(hostname: String, key: String, value: String, path: String, expires: int, secure: bool, http_only: bool):
	return _steam_call("setCookie", [hostname, key, value, path, expires, secure, http_only])

func setDLCContext(app_id: int):
	return _steam_call("setDLCContext", [app_id])

func setDPIScalingFactor(dpi_scaling: float, browser_handle: int):
	return _steam_call("setDPIScalingFactor", [dpi_scaling, browser_handle])

func setDurationControlOnlineState(new_state: int):
	return _steam_call("setDurationControlOnlineState", [new_state])

func setGameLauncherMode(mode: bool):
	return _steam_call("setGameLauncherMode", [mode])

func setGamePerformanceSettings(setting: int):
	return _steam_call("setGamePerformanceSettings", [setting])

func setGamePhaseAttribute(attribute_group: String, attribute_value: String, priority: int):
	return _steam_call("setGamePhaseAttribute", [attribute_group, attribute_value, priority])

func setGamePhaseID(phase_id: String):
	return _steam_call("setGamePhaseID", [phase_id])

func setGameRenderResolution(width: int, height: int):
	return _steam_call("setGameRenderResolution", [width, height])

func setGlobalConfigValueFloat(config: int, value: float):
	return _steam_call("setGlobalConfigValueFloat", [config, value])

func setGlobalConfigValueInt32(config: int, value: int):
	return _steam_call("setGlobalConfigValueInt32", [config, value])

func setGlobalConfigValueString(config: int, value: String):
	return _steam_call("setGlobalConfigValueString", [config, value])

func setHTTPCookie(cookie_handle: int, host: String, url: String, cookie_name: String):
	return _steam_call("setHTTPCookie", [cookie_handle, host, url, cookie_name])

func setHTTPRequestAbsoluteTimeoutMS(request_handle: int, milliseconds: int):
	return _steam_call("setHTTPRequestAbsoluteTimeoutMS", [request_handle, milliseconds])

func setHTTPRequestContextValue(request_handle: int, context_value: int):
	return _steam_call("setHTTPRequestContextValue", [request_handle, context_value])

func setHTTPRequestCookieContainer(request_handle: int, cookie_handle: int):
	return _steam_call("setHTTPRequestCookieContainer", [request_handle, cookie_handle])

func setHTTPRequestGetOrPostParameter(request_handle: int, param_name: String, param_value: String):
	return _steam_call("setHTTPRequestGetOrPostParameter", [request_handle, param_name, param_value])

func setHTTPRequestHeaderValue(request_handle: int, header_name: String, header_value: String):
	return _steam_call("setHTTPRequestHeaderValue", [request_handle, header_name, header_value])

func setHTTPRequestNetworkActivityTimeout(request_handle: int, timeout_seconds: int):
	return _steam_call("setHTTPRequestNetworkActivityTimeout", [request_handle, timeout_seconds])

func setHTTPRequestRawPostBody(request_handle: int, content_type: String, body: String):
	return _steam_call("setHTTPRequestRawPostBody", [request_handle, content_type, body])

func setHTTPRequestRequiresVerifiedCertificate(request_handle: int, require_verified_certificate: bool):
	return _steam_call("setHTTPRequestRequiresVerifiedCertificate", [request_handle, require_verified_certificate])

func setHTTPRequestUserAgentInfo(request_handle: int, user_agent_info: String):
	return _steam_call("setHTTPRequestUserAgentInfo", [request_handle, user_agent_info])

func setHorizontalScroll(absolute_pixel_scroll: int, browser_handle: int = 0):
	return _steam_call("setHorizontalScroll", [absolute_pixel_scroll, browser_handle])

func setInGameVoiceSpeaking(steam_id: int, speaking: bool):
	return _steam_call("setInGameVoiceSpeaking", [steam_id, speaking])

func setInputActionManifestFilePath(manifest_path: String):
	return _steam_call("setInputActionManifestFilePath", [manifest_path])

func setItemContent(update_handle: int, content_folder: String):
	return _steam_call("setItemContent", [update_handle, content_folder])

func setItemDescription(update_handle: int, description: String):
	return _steam_call("setItemDescription", [update_handle, description])

func setItemMetadata(update_handle: int, ugc_metadata: String):
	return _steam_call("setItemMetadata", [update_handle, ugc_metadata])

func setItemPreview(update_handle: int, preview_file: String):
	return _steam_call("setItemPreview", [update_handle, preview_file])

func setItemTags(update_handle: int, tag_array: Array, allow_admin_tags: bool = false):
	return _steam_call("setItemTags", [update_handle, tag_array, allow_admin_tags])

func setItemTitle(update_handle: int, title: String):
	return _steam_call("setItemTitle", [update_handle, title])

func setItemUpdateLanguage(update_handle: int, language: String):
	return _steam_call("setItemUpdateLanguage", [update_handle, language])

func setItemVisibility(update_handle: int, visibility: int):
	return _steam_call("setItemVisibility", [update_handle, visibility])

func setItemsDisabledLocally(file_ids: PackedInt64Array, disabled_locally: bool):
	return _steam_call("setItemsDisabledLocally", [file_ids, disabled_locally])

func setKeyFocus(has_key_focus: bool, browser_handle: int = 0):
	return _steam_call("setKeyFocus", [has_key_focus, browser_handle])

func setLEDColor(input_handle: int, color_r: int, color_g: int, color_b: int, flags: int):
	return _steam_call("setLEDColor", [input_handle, color_r, color_g, color_b, flags])

func setLanguage(query_handle: int, language: String):
	return _steam_call("setLanguage", [query_handle, language])

func setListenForFriendsMessages(intercept_enabled: bool):
	return _steam_call("setListenForFriendsMessages", [intercept_enabled])

func setLobbyData(steam_lobby_id: int, key: String, value: String):
	return _steam_call("setLobbyData", [steam_lobby_id, key, value])

func setLobbyGameServer(steam_lobby_id: int, server_ip: String = "0", server_port: int = 0, steam_id_game_server: int = 0):
	return _steam_call("setLobbyGameServer", [steam_lobby_id, server_ip, server_port, steam_id_game_server])

func setLobbyJoinable(steam_lobby_id: int, joinable: bool):
	return _steam_call("setLobbyJoinable", [steam_lobby_id, joinable])

func setLobbyMemberData(steam_lobby_id: int, key: String, value: String):
	return _steam_call("setLobbyMemberData", [steam_lobby_id, key, value])

func setLobbyMemberLimit(steam_lobby_id: int, max_members: int):
	return _steam_call("setLobbyMemberLimit", [steam_lobby_id, max_members])

func setLobbyOwner(steam_lobby_id: int, steam_id_new_owner: int):
	return _steam_call("setLobbyOwner", [steam_lobby_id, steam_id_new_owner])

func setLobbyType(steam_lobby_id: int, lobby_type: int):
	return _steam_call("setLobbyType", [steam_lobby_id, lobby_type])

func setLocation(screenshot: int, location: String):
	return _steam_call("setLocation", [screenshot, location])

func setMatchAnyTag(query_handle: int, match_any_tag: bool):
	return _steam_call("setMatchAnyTag", [query_handle, match_any_tag])

func setMouseCursor(session_id: int, cursor_id: int):
	return _steam_call("setMouseCursor", [session_id, cursor_id])

func setMousePosition(session_id: int, normalized_x: float, normalized_y: float):
	return _steam_call("setMousePosition", [session_id, normalized_x, normalized_y])

func setMouseVisibility(session_id: int, visible: bool):
	return _steam_call("setMouseVisibility", [session_id, visible])

func setOverlayNotificationInset(horizontal: int, vertical: int):
	return _steam_call("setOverlayNotificationInset", [horizontal, vertical])

func setOverlayNotificationPosition(position: int):
	return _steam_call("setOverlayNotificationPosition", [position])

func setPageScaleFactor(zoom: float, point_x: int, point_y: int, browser_handle: int = 0):
	return _steam_call("setPageScaleFactor", [zoom, point_x, point_y, browser_handle])

func setPlayedWith(steam_id: int):
	return _steam_call("setPlayedWith", [steam_id])

func setPropertyBool(item_id: int, name: String, value: bool, this_inventory_update_handle: int = 0):
	return _steam_call("setPropertyBool", [item_id, name, value, this_inventory_update_handle])

func setPropertyFloat(item_id: int, name: String, value: float, this_inventory_update_handle: int = 0):
	return _steam_call("setPropertyFloat", [item_id, name, value, this_inventory_update_handle])

func setPropertyInt(item_id: int, name: String, value: int, this_inventory_update_handle: int = 0):
	return _steam_call("setPropertyInt", [item_id, name, value, this_inventory_update_handle])

func setPropertyString(item_id: int, name: String, value: String, this_inventory_update_handle: int = 0):
	return _steam_call("setPropertyString", [item_id, name, value, this_inventory_update_handle])

func setRankedByTrendDays(query_handle: int, days: int):
	return _steam_call("setRankedByTrendDays", [query_handle, days])

func setRequiredGameVersions(query_handle: int, game_branch_min: String, game_branch_max: String):
	return _steam_call("setRequiredGameVersions", [query_handle, game_branch_min, game_branch_max])

func setReturnAdditionalPreviews(query_handle: int, return_additional_previews: bool):
	return _steam_call("setReturnAdditionalPreviews", [query_handle, return_additional_previews])

func setReturnChildren(query_handle: int, return_children: bool):
	return _steam_call("setReturnChildren", [query_handle, return_children])

func setReturnKeyValueTags(query_handle: int, return_key_value_tags: bool):
	return _steam_call("setReturnKeyValueTags", [query_handle, return_key_value_tags])

func setReturnLongDescription(query_handle: int, return_long_description: bool):
	return _steam_call("setReturnLongDescription", [query_handle, return_long_description])

func setReturnMetadata(query_handle: int, return_metadata: bool):
	return _steam_call("setReturnMetadata", [query_handle, return_metadata])

func setReturnOnlyIDs(query_handle: int, return_only_ids: bool):
	return _steam_call("setReturnOnlyIDs", [query_handle, return_only_ids])

func setReturnPlaytimeStats(query_handle: int, days: int):
	return _steam_call("setReturnPlaytimeStats", [query_handle, days])

func setReturnTotalOnly(query_handle: int, return_total_only: bool):
	return _steam_call("setReturnTotalOnly", [query_handle, return_total_only])

func setRichPresence(key: String, value: String):
	return _steam_call("setRichPresence", [key, value])

func setSearchText(query_handle: int, search_text: String):
	return _steam_call("setSearchText", [query_handle, search_text])

func setSize(width: int, height: int, browser_handle: int = 0):
	return _steam_call("setSize", [width, height, browser_handle])

func setStatFloat(stat_name: String, value: float):
	return _steam_call("setStatFloat", [stat_name, value])

func setStatInt(stat_name: String, value: int):
	return _steam_call("setStatInt", [stat_name, value])

func setSubscriptionsLoadOrder(published_file_ids: PackedInt64Array):
	return _steam_call("setSubscriptionsLoadOrder", [published_file_ids])

func setSyncPlatforms(file: String, platform: int):
	return _steam_call("setSyncPlatforms", [file, platform])

func setTimeCreatedDateRange(update_handle: int, start: int, end: int):
	return _steam_call("setTimeCreatedDateRange", [update_handle, start, end])

func setTimeUpdatedDateRange(update_handle: int, start: int, end: int):
	return _steam_call("setTimeUpdatedDateRange", [update_handle, start, end])

func setTimelineGameMode(mode: int):
	return _steam_call("setTimelineGameMode", [mode])

func setTimelineTooltip(description: String, time_delta: float):
	return _steam_call("setTimelineTooltip", [description, time_delta])

func setUserItemVote(published_file_id: int, vote_up: bool):
	return _steam_call("setUserItemVote", [published_file_id, vote_up])

func setVRHeadsetStreamingEnabled(enabled: bool = true):
	return _steam_call("setVRHeadsetStreamingEnabled", [enabled])

func setVerticalScroll(absolute_pixel_scroll: int, browser_handle: int = 0):
	return _steam_call("setVerticalScroll", [absolute_pixel_scroll, browser_handle])

func set_server_list_request(new_server_list_request: int):
	return _steam_call("set_server_list_request", [new_server_list_request])

func showBindingPanel(input_handle: int):
	return _steam_call("showBindingPanel", [input_handle])

func showFloatingGamepadTextInput(input_mode: int, text_field_x_position: int, text_field_y_position: int, text_field_width: int, text_field_height: int):
	return _steam_call("showFloatingGamepadTextInput", [input_mode, text_field_x_position, text_field_y_position, text_field_width, text_field_height])

func showGamepadTextInput(input_mode: int, line_input_mode: int, description: String, max_text: int, preset_text: String):
	return _steam_call("showGamepadTextInput", [input_mode, line_input_mode, description, max_text, preset_text])

func showRemotePlayTogetherUI():
	return _steam_call("showRemotePlayTogetherUI", [])

func showWorkshopEULA():
	return _steam_call("showWorkshopEULA", [])

func startGamePhase():
	return _steam_call("startGamePhase", [])

func startItemUpdate(app_id: int, file_id: int):
	return _steam_call("startItemUpdate", [app_id, file_id])

func startPlaytimeTracking(published_file_ids: Array):
	return _steam_call("startPlaytimeTracking", [published_file_ids])

func startPurchase(items: PackedInt64Array, quantity: PackedInt32Array):
	return _steam_call("startPurchase", [items, quantity])

func startRangeTimelineEvent(title: String, description: String, icon: String, priority: int, start_offset_seconds: float, possible_clip: int = 1):
	return _steam_call("startRangeTimelineEvent", [title, description, icon, priority, start_offset_seconds, possible_clip])

func startUpdateProperties():
	return _steam_call("startUpdateProperties", [])

func startVRDashboard():
	return _steam_call("startVRDashboard", [])

func startVoiceRecording():
	return _steam_call("startVoiceRecording", [])

func steamInit(app_id: int = 0, embed_callbacks: bool = false):
	return _steam_call("steamInit", [app_id, embed_callbacks])

func steamInitEx(app_id: int = 0, embed_callbacks: bool = false):
	return _steam_call("steamInitEx", [app_id, embed_callbacks])

func steamShutdown():
	return _steam_call("steamShutdown", [])

func stopAnalogActionMomentum(input_handle: int, action: int):
	return _steam_call("stopAnalogActionMomentum", [input_handle, action])

func stopFind(browser_handle: int = 0):
	return _steam_call("stopFind", [browser_handle])

func stopLoad(browser_handle: int = 0):
	return _steam_call("stopLoad", [browser_handle])

func stopPlaytimeTracking(published_file_ids: Array):
	return _steam_call("stopPlaytimeTracking", [published_file_ids])

func stopPlaytimeTrackingForAllItems():
	return _steam_call("stopPlaytimeTrackingForAllItems", [])

func stopVoiceRecording():
	return _steam_call("stopVoiceRecording", [])

func storeStats():
	return _steam_call("storeStats", [])

func submitItemUpdate(update_handle: int, change_note: String = ""):
	return _steam_call("submitItemUpdate", [update_handle, change_note])

func submitUpdateProperties(this_inventory_update_handle: int = 0):
	return _steam_call("submitUpdateProperties", [this_inventory_update_handle])

func subscribeItem(published_file_id: int):
	return _steam_call("subscribeItem", [published_file_id])

func suspendDownloads(suspend: bool):
	return _steam_call("suspendDownloads", [suspend])

func tagPublishedFile(screenshot: int, file_id: int):
	return _steam_call("tagPublishedFile", [screenshot, file_id])

func tagUser(screenshot: int, steam_id: int):
	return _steam_call("tagUser", [screenshot, steam_id])

func terminateGameConnection(server_ip: String, server_port: int):
	return _steam_call("terminateGameConnection", [server_ip, server_port])

func transferItemQuantity(item_id: int, quantity: int, item_destination: int, split: bool):
	return _steam_call("transferItemQuantity", [item_id, quantity, item_destination, split])

func translateActionOrigin(destination_input: int, source_origin: int):
	return _steam_call("translateActionOrigin", [destination_input, source_origin])

func triggerHapticPulse(input_handle: int, target_pad: int, duration: int):
	return _steam_call("triggerHapticPulse", [input_handle, target_pad, duration])

func triggerItemDrop(definition: int):
	return _steam_call("triggerItemDrop", [definition])

func triggerRepeatedHapticPulse(input_handle: int, target_pad: int, duration: int, offset: int, repeat: int, flags: int):
	return _steam_call("triggerRepeatedHapticPulse", [input_handle, target_pad, duration, offset, repeat, flags])

func triggerScreenshot():
	return _steam_call("triggerScreenshot", [])

func triggerSimpleHapticEvent(input_handle: int, haptic_location: int, intensity: int, gain_db: String, other_intensity: int, other_gain_db: String):
	return _steam_call("triggerSimpleHapticEvent", [input_handle, haptic_location, intensity, gain_db, other_intensity, other_gain_db])

func triggerVibration(input_handle: int, left_speed: int, right_speed: int):
	return _steam_call("triggerVibration", [input_handle, left_speed, right_speed])

func triggerVibrationExtended(input_handle: int, left_speed: int, right_speed: int, left_trigger_speed: int, right_trigger_speed: int):
	return _steam_call("triggerVibrationExtended", [input_handle, left_speed, right_speed, left_trigger_speed, right_trigger_speed])

func ugcDownload(content: int, priority: int):
	return _steam_call("ugcDownload", [content, priority])

func ugcDownloadToLocation(content: int, location: String, priority: int):
	return _steam_call("ugcDownloadToLocation", [content, location, priority])

func ugcRead(content: int, data_size: int, offset: int, action: int):
	return _steam_call("ugcRead", [content, data_size, offset, action])

func uninstallDLC(dlc_id: int):
	return _steam_call("uninstallDLC", [dlc_id])

func unsubscribeItem(published_file_id: int):
	return _steam_call("unsubscribeItem", [published_file_id])

func updateAvgRateStat(stat_name: String, this_session: float, session_length: float):
	return _steam_call("updateAvgRateStat", [stat_name, this_session, session_length])

func updateItemPreviewFile(update_handle: int, index: int, preview_file: String):
	return _steam_call("updateItemPreviewFile", [update_handle, index, preview_file])

func updateItemPreviewVideo(update_handle: int, index: int, video_id: String):
	return _steam_call("updateItemPreviewVideo", [update_handle, index, video_id])

func updateRangeTimelineEvent(timeline_event_handle: int, title: String, description: String, icon: String, priority: int, possible_clip: int = 1):
	return _steam_call("updateRangeTimelineEvent", [timeline_event_handle, title, description, icon, priority, possible_clip])

func uploadLeaderboardScore(score: int, keep_best: bool = true, details: PackedInt32Array = PackedInt32Array(), this_leaderboard: int = 0):
	return _steam_call("uploadLeaderboardScore", [score, keep_best, details, this_leaderboard])

func userHasLicenseForApp(steam_id: int, app_id: int):
	return _steam_call("userHasLicenseForApp", [steam_id, app_id])

func viewSource(browser_handle: int = 0):
	return _steam_call("viewSource", [browser_handle])

func waitForData(wait_forever: bool, timeout: int):
	return _steam_call("waitForData", [wait_forever, timeout])

func writeScreenshot(rgb: PackedByteArray, width: int, height: int):
	return _steam_call("writeScreenshot", [rgb, width, height])
