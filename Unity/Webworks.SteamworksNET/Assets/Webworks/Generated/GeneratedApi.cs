using System;
using System.Collections.Generic;
using System.Text;

namespace Steamworks
{
	public static class SteamGameServerClient
	{
		public static HSteamPipe CreateSteamPipe()
		{
			return SteamClient.CreateSteamPipe();
		}

		public static bool BReleaseSteamPipe(HSteamPipe hSteamPipe)
		{
			return SteamClient.BReleaseSteamPipe(hSteamPipe);
		}

		public static HSteamUser ConnectToGlobalUser(HSteamPipe hSteamPipe)
		{
			return SteamClient.ConnectToGlobalUser(hSteamPipe);
		}

		public static HSteamUser CreateLocalUser(out HSteamPipe phSteamPipe, EAccountType eAccountType)
		{
			return SteamClient.CreateLocalUser(out phSteamPipe, eAccountType);
		}

		public static void ReleaseUser(HSteamPipe hSteamPipe, HSteamUser hUser)
		{
			SteamClient.ReleaseUser(hSteamPipe, hUser);
		}

		public static IntPtr GetISteamUser(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamUser(hSteamUser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamGameServer(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamGameServer(hSteamUser, hSteamPipe, pchVersion);
		}

		public static void SetLocalIPBinding(ref SteamIPAddress_t unIP, ushort usPort)
		{
			SteamClient.SetLocalIPBinding(ref unIP, usPort);
		}

		public static IntPtr GetISteamFriends(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamFriends(hSteamUser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamUtils(HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamUtils(hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamMatchmaking(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamMatchmaking(hSteamUser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamMatchmakingServers(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamMatchmakingServers(hSteamUser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamGenericInterface(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamGenericInterface(hSteamUser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamUserStats(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamUserStats(hSteamUser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamGameServerStats(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamGameServerStats(hSteamuser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamApps(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamApps(hSteamUser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamNetworking(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamNetworking(hSteamUser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamRemoteStorage(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamRemoteStorage(hSteamuser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamScreenshots(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamScreenshots(hSteamuser, hSteamPipe, pchVersion);
		}

		public static uint GetIPCCallCount()
		{
			return SteamClient.GetIPCCallCount();
		}

		public static void SetWarningMessageHook(SteamAPIWarningMessageHook_t pFunction)
		{
			SteamClient.SetWarningMessageHook(pFunction);
		}

		public static bool BShutdownIfAllPipesClosed()
		{
			return SteamClient.BShutdownIfAllPipesClosed();
		}

		public static IntPtr GetISteamHTTP(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamHTTP(hSteamuser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamController(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamController(hSteamUser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamUGC(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamUGC(hSteamUser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamMusic(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamMusic(hSteamuser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamHTMLSurface(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamHTMLSurface(hSteamuser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamInventory(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamInventory(hSteamuser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamVideo(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamVideo(hSteamuser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamParentalSettings(HSteamUser hSteamuser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamParentalSettings(hSteamuser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamInput(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamInput(hSteamUser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamParties(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamParties(hSteamUser, hSteamPipe, pchVersion);
		}

		public static IntPtr GetISteamRemotePlay(HSteamUser hSteamUser, HSteamPipe hSteamPipe, string pchVersion)
		{
			return SteamClient.GetISteamRemotePlay(hSteamUser, hSteamPipe, pchVersion);
		}
	}

	public static class SteamGameServerHTTP
	{
		public static HTTPRequestHandle CreateHTTPRequest(EHTTPMethod eHTTPRequestMethod, string pchAbsoluteURL)
		{
			return SteamHTTP.CreateHTTPRequest(eHTTPRequestMethod, pchAbsoluteURL);
		}

		public static bool SetHTTPRequestContextValue(HTTPRequestHandle hRequest, ulong ulContextValue)
		{
			return SteamHTTP.SetHTTPRequestContextValue(hRequest, ulContextValue);
		}

		public static bool SetHTTPRequestNetworkActivityTimeout(HTTPRequestHandle hRequest, uint unTimeoutSeconds)
		{
			return SteamHTTP.SetHTTPRequestNetworkActivityTimeout(hRequest, unTimeoutSeconds);
		}

		public static bool SetHTTPRequestHeaderValue(HTTPRequestHandle hRequest, string pchHeaderName, string pchHeaderValue)
		{
			return SteamHTTP.SetHTTPRequestHeaderValue(hRequest, pchHeaderName, pchHeaderValue);
		}

		public static bool SetHTTPRequestGetOrPostParameter(HTTPRequestHandle hRequest, string pchParamName, string pchParamValue)
		{
			return SteamHTTP.SetHTTPRequestGetOrPostParameter(hRequest, pchParamName, pchParamValue);
		}

		public static bool SendHTTPRequest(HTTPRequestHandle hRequest, out SteamAPICall_t pCallHandle)
		{
			return SteamHTTP.SendHTTPRequest(hRequest, out pCallHandle);
		}

		public static bool SendHTTPRequestAndStreamResponse(HTTPRequestHandle hRequest, out SteamAPICall_t pCallHandle)
		{
			return SteamHTTP.SendHTTPRequestAndStreamResponse(hRequest, out pCallHandle);
		}

		public static bool DeferHTTPRequest(HTTPRequestHandle hRequest)
		{
			return SteamHTTP.DeferHTTPRequest(hRequest);
		}

		public static bool PrioritizeHTTPRequest(HTTPRequestHandle hRequest)
		{
			return SteamHTTP.PrioritizeHTTPRequest(hRequest);
		}

		public static bool GetHTTPResponseHeaderSize(HTTPRequestHandle hRequest, string pchHeaderName, out uint unResponseHeaderSize)
		{
			return SteamHTTP.GetHTTPResponseHeaderSize(hRequest, pchHeaderName, out unResponseHeaderSize);
		}

		public static bool GetHTTPResponseHeaderValue(HTTPRequestHandle hRequest, string pchHeaderName, byte[] pHeaderValueBuffer, uint unBufferSize)
		{
			return SteamHTTP.GetHTTPResponseHeaderValue(hRequest, pchHeaderName, pHeaderValueBuffer, unBufferSize);
		}

		public static bool GetHTTPResponseBodySize(HTTPRequestHandle hRequest, out uint unBodySize)
		{
			return SteamHTTP.GetHTTPResponseBodySize(hRequest, out unBodySize);
		}

		public static bool GetHTTPResponseBodyData(HTTPRequestHandle hRequest, byte[] pBodyDataBuffer, uint unBufferSize)
		{
			return SteamHTTP.GetHTTPResponseBodyData(hRequest, pBodyDataBuffer, unBufferSize);
		}

		public static bool GetHTTPStreamingResponseBodyData(HTTPRequestHandle hRequest, uint cOffset, byte[] pBodyDataBuffer, uint unBufferSize)
		{
			return SteamHTTP.GetHTTPStreamingResponseBodyData(hRequest, cOffset, pBodyDataBuffer, unBufferSize);
		}

		public static bool ReleaseHTTPRequest(HTTPRequestHandle hRequest)
		{
			return SteamHTTP.ReleaseHTTPRequest(hRequest);
		}

		public static bool GetHTTPDownloadProgressPct(HTTPRequestHandle hRequest, out float pflPercentOut)
		{
			return SteamHTTP.GetHTTPDownloadProgressPct(hRequest, out pflPercentOut);
		}

		public static bool SetHTTPRequestRawPostBody(HTTPRequestHandle hRequest, string pchContentType, byte[] pubBody, uint unBodyLen)
		{
			return SteamHTTP.SetHTTPRequestRawPostBody(hRequest, pchContentType, pubBody, unBodyLen);
		}

		public static HTTPCookieContainerHandle CreateCookieContainer(bool bAllowResponsesToModify)
		{
			return SteamHTTP.CreateCookieContainer(bAllowResponsesToModify);
		}

		public static bool ReleaseCookieContainer(HTTPCookieContainerHandle hCookieContainer)
		{
			return SteamHTTP.ReleaseCookieContainer(hCookieContainer);
		}

		public static bool SetCookie(HTTPCookieContainerHandle hCookieContainer, string pchHost, string pchUrl, string pchCookie)
		{
			return SteamHTTP.SetCookie(hCookieContainer, pchHost, pchUrl, pchCookie);
		}

		public static bool SetHTTPRequestCookieContainer(HTTPRequestHandle hRequest, HTTPCookieContainerHandle hCookieContainer)
		{
			return SteamHTTP.SetHTTPRequestCookieContainer(hRequest, hCookieContainer);
		}

		public static bool SetHTTPRequestUserAgentInfo(HTTPRequestHandle hRequest, string pchUserAgentInfo)
		{
			return SteamHTTP.SetHTTPRequestUserAgentInfo(hRequest, pchUserAgentInfo);
		}

		public static bool SetHTTPRequestRequiresVerifiedCertificate(HTTPRequestHandle hRequest, bool bRequireVerifiedCertificate)
		{
			return SteamHTTP.SetHTTPRequestRequiresVerifiedCertificate(hRequest, bRequireVerifiedCertificate);
		}

		public static bool SetHTTPRequestAbsoluteTimeoutMS(HTTPRequestHandle hRequest, uint unMilliseconds)
		{
			return SteamHTTP.SetHTTPRequestAbsoluteTimeoutMS(hRequest, unMilliseconds);
		}

		public static bool GetHTTPRequestWasTimedOut(HTTPRequestHandle hRequest, out bool pbWasTimedOut)
		{
			return SteamHTTP.GetHTTPRequestWasTimedOut(hRequest, out pbWasTimedOut);
		}
	}

	public static class SteamGameServerInventory
	{
		public static EResult GetResultStatus(SteamInventoryResult_t resultHandle)
		{
			return SteamInventory.GetResultStatus(resultHandle);
		}

		public static bool GetResultItems(SteamInventoryResult_t resultHandle, SteamItemDetails_t[] pOutItemsArray, ref uint punOutItemsArraySize)
		{
			return SteamInventory.GetResultItems(resultHandle, pOutItemsArray, ref punOutItemsArraySize);
		}

		public static bool GetResultItemProperty(SteamInventoryResult_t resultHandle, uint unItemIndex, string pchPropertyName, out string pchValueBuffer, ref uint punValueBufferSizeOut)
		{
			return SteamInventory.GetResultItemProperty(resultHandle, unItemIndex, pchPropertyName, out pchValueBuffer, ref punValueBufferSizeOut);
		}

		public static uint GetResultTimestamp(SteamInventoryResult_t resultHandle)
		{
			return SteamInventory.GetResultTimestamp(resultHandle);
		}

		public static bool CheckResultSteamID(SteamInventoryResult_t resultHandle, CSteamID steamIDExpected)
		{
			return SteamInventory.CheckResultSteamID(resultHandle, steamIDExpected);
		}

		public static void DestroyResult(SteamInventoryResult_t resultHandle)
		{
			SteamInventory.DestroyResult(resultHandle);
		}

		public static bool GetAllItems(out SteamInventoryResult_t pResultHandle)
		{
			return SteamInventory.GetAllItems(out pResultHandle);
		}

		public static bool GetItemsByID(out SteamInventoryResult_t pResultHandle, SteamItemInstanceID_t[] pInstanceIDs, uint unCountInstanceIDs)
		{
			return SteamInventory.GetItemsByID(out pResultHandle, pInstanceIDs, unCountInstanceIDs);
		}

		public static bool SerializeResult(SteamInventoryResult_t resultHandle, byte[] pOutBuffer, out uint punOutBufferSize)
		{
			return SteamInventory.SerializeResult(resultHandle, pOutBuffer, out punOutBufferSize);
		}

		public static bool DeserializeResult(out SteamInventoryResult_t pOutResultHandle, byte[] pBuffer, uint unBufferSize, bool bRESERVED_MUST_BE_FALSE = false)
		{
			return SteamInventory.DeserializeResult(out pOutResultHandle, pBuffer, unBufferSize, bRESERVED_MUST_BE_FALSE);
		}

		public static bool GenerateItems(out SteamInventoryResult_t pResultHandle, SteamItemDef_t[] pArrayItemDefs, uint[] punArrayQuantity, uint unArrayLength)
		{
			return SteamInventory.GenerateItems(out pResultHandle, pArrayItemDefs, punArrayQuantity, unArrayLength);
		}

		public static bool GrantPromoItems(out SteamInventoryResult_t pResultHandle)
		{
			return SteamInventory.GrantPromoItems(out pResultHandle);
		}

		public static bool AddPromoItem(out SteamInventoryResult_t pResultHandle, SteamItemDef_t itemDef)
		{
			return SteamInventory.AddPromoItem(out pResultHandle, itemDef);
		}

		public static bool AddPromoItems(out SteamInventoryResult_t pResultHandle, SteamItemDef_t[] pArrayItemDefs, uint unArrayLength)
		{
			return SteamInventory.AddPromoItems(out pResultHandle, pArrayItemDefs, unArrayLength);
		}

		public static bool ConsumeItem(out SteamInventoryResult_t pResultHandle, SteamItemInstanceID_t itemConsume, uint unQuantity)
		{
			return SteamInventory.ConsumeItem(out pResultHandle, itemConsume, unQuantity);
		}

		public static bool ExchangeItems(out SteamInventoryResult_t pResultHandle, SteamItemDef_t[] pArrayGenerate, uint[] punArrayGenerateQuantity, uint unArrayGenerateLength, SteamItemInstanceID_t[] pArrayDestroy, uint[] punArrayDestroyQuantity, uint unArrayDestroyLength)
		{
			return SteamInventory.ExchangeItems(out pResultHandle, pArrayGenerate, punArrayGenerateQuantity, unArrayGenerateLength, pArrayDestroy, punArrayDestroyQuantity, unArrayDestroyLength);
		}

		public static bool TransferItemQuantity(out SteamInventoryResult_t pResultHandle, SteamItemInstanceID_t itemIdSource, uint unQuantity, SteamItemInstanceID_t itemIdDest)
		{
			return SteamInventory.TransferItemQuantity(out pResultHandle, itemIdSource, unQuantity, itemIdDest);
		}

		public static void SendItemDropHeartbeat()
		{
			SteamInventory.SendItemDropHeartbeat();
		}

		public static bool TriggerItemDrop(out SteamInventoryResult_t pResultHandle, SteamItemDef_t dropListDefinition)
		{
			return SteamInventory.TriggerItemDrop(out pResultHandle, dropListDefinition);
		}

		public static bool TradeItems(out SteamInventoryResult_t pResultHandle, CSteamID steamIDTradePartner, SteamItemInstanceID_t[] pArrayGive, uint[] pArrayGiveQuantity, uint nArrayGiveLength, SteamItemInstanceID_t[] pArrayGet, uint[] pArrayGetQuantity, uint nArrayGetLength)
		{
			return SteamInventory.TradeItems(out pResultHandle, steamIDTradePartner, pArrayGive, pArrayGiveQuantity, nArrayGiveLength, pArrayGet, pArrayGetQuantity, nArrayGetLength);
		}

		public static bool LoadItemDefinitions()
		{
			return SteamInventory.LoadItemDefinitions();
		}

		public static bool GetItemDefinitionIDs(SteamItemDef_t[] pItemDefIDs, ref uint punItemDefIDsArraySize)
		{
			return SteamInventory.GetItemDefinitionIDs(pItemDefIDs, ref punItemDefIDsArraySize);
		}

		public static bool GetItemDefinitionProperty(SteamItemDef_t iDefinition, string pchPropertyName, out string pchValueBuffer, ref uint punValueBufferSizeOut)
		{
			return SteamInventory.GetItemDefinitionProperty(iDefinition, pchPropertyName, out pchValueBuffer, ref punValueBufferSizeOut);
		}

		public static SteamAPICall_t RequestEligiblePromoItemDefinitionsIDs(CSteamID steamID)
		{
			return SteamInventory.RequestEligiblePromoItemDefinitionsIDs(steamID);
		}

		public static bool GetEligiblePromoItemDefinitionIDs(CSteamID steamID, SteamItemDef_t[] pItemDefIDs, ref uint punItemDefIDsArraySize)
		{
			return SteamInventory.GetEligiblePromoItemDefinitionIDs(steamID, pItemDefIDs, ref punItemDefIDsArraySize);
		}

		public static SteamAPICall_t StartPurchase(SteamItemDef_t[] pArrayItemDefs, uint[] punArrayQuantity, uint unArrayLength)
		{
			return SteamInventory.StartPurchase(pArrayItemDefs, punArrayQuantity, unArrayLength);
		}

		public static SteamAPICall_t RequestPrices()
		{
			return SteamInventory.RequestPrices();
		}

		public static uint GetNumItemsWithPrices()
		{
			return SteamInventory.GetNumItemsWithPrices();
		}

		public static bool GetItemsWithPrices(SteamItemDef_t[] pArrayItemDefs, ulong[] pCurrentPrices, ulong[] pBasePrices, uint unArrayLength)
		{
			return SteamInventory.GetItemsWithPrices(pArrayItemDefs, pCurrentPrices, pBasePrices, unArrayLength);
		}

		public static bool GetItemPrice(SteamItemDef_t iDefinition, out ulong pCurrentPrice, out ulong pBasePrice)
		{
			return SteamInventory.GetItemPrice(iDefinition, out pCurrentPrice, out pBasePrice);
		}

		public static SteamInventoryUpdateHandle_t StartUpdateProperties()
		{
			return SteamInventory.StartUpdateProperties();
		}

		public static bool RemoveProperty(SteamInventoryUpdateHandle_t handle, SteamItemInstanceID_t nItemID, string pchPropertyName)
		{
			return SteamInventory.RemoveProperty(handle, nItemID, pchPropertyName);
		}

		public static bool SetProperty(SteamInventoryUpdateHandle_t handle, SteamItemInstanceID_t nItemID, string pchPropertyName, string pchPropertyValue)
		{
			return SteamInventory.SetProperty(handle, nItemID, pchPropertyName, pchPropertyValue);
		}

		public static bool SubmitUpdateProperties(SteamInventoryUpdateHandle_t handle, out SteamInventoryResult_t pResultHandle)
		{
			return SteamInventory.SubmitUpdateProperties(handle, out pResultHandle);
		}

		public static bool InspectItem(out SteamInventoryResult_t pResultHandle, string pchItemToken)
		{
			return SteamInventory.InspectItem(out pResultHandle, pchItemToken);
		}
	}

	public static class SteamGameServerNetworking
	{
		public static bool SendP2PPacket(CSteamID steamIDRemote, byte[] pubData, uint cubData, EP2PSend eP2PSendType, int nChannel = 0)
		{
			return SteamNetworking.SendP2PPacket(steamIDRemote, pubData, cubData, eP2PSendType, nChannel);
		}

		public static bool IsP2PPacketAvailable(out uint pcubMsgSize, int nChannel = 0)
		{
			return SteamNetworking.IsP2PPacketAvailable(out pcubMsgSize, nChannel);
		}

		public static bool ReadP2PPacket(byte[] pubDest, uint cubDest, out uint pcubMsgSize, out CSteamID psteamIDRemote, int nChannel = 0)
		{
			return SteamNetworking.ReadP2PPacket(pubDest, cubDest, out pcubMsgSize, out psteamIDRemote, nChannel);
		}

		public static bool AcceptP2PSessionWithUser(CSteamID steamIDRemote)
		{
			return SteamNetworking.AcceptP2PSessionWithUser(steamIDRemote);
		}

		public static bool CloseP2PSessionWithUser(CSteamID steamIDRemote)
		{
			return SteamNetworking.CloseP2PSessionWithUser(steamIDRemote);
		}

		public static bool CloseP2PChannelWithUser(CSteamID steamIDRemote, int nChannel)
		{
			return SteamNetworking.CloseP2PChannelWithUser(steamIDRemote, nChannel);
		}

		public static bool GetP2PSessionState(CSteamID steamIDRemote, out P2PSessionState_t pConnectionState)
		{
			return SteamNetworking.GetP2PSessionState(steamIDRemote, out pConnectionState);
		}

		public static bool AllowP2PPacketRelay(bool bAllow)
		{
			return SteamNetworking.AllowP2PPacketRelay(bAllow);
		}

		public static SNetListenSocket_t CreateListenSocket(int nVirtualP2PPort, SteamIPAddress_t nIP, ushort nPort, bool bAllowUseOfPacketRelay)
		{
			return SteamNetworking.CreateListenSocket(nVirtualP2PPort, nIP, nPort, bAllowUseOfPacketRelay);
		}

		public static SNetSocket_t CreateP2PConnectionSocket(CSteamID steamIDTarget, int nVirtualPort, int nTimeoutSec, bool bAllowUseOfPacketRelay)
		{
			return SteamNetworking.CreateP2PConnectionSocket(steamIDTarget, nVirtualPort, nTimeoutSec, bAllowUseOfPacketRelay);
		}

		public static SNetSocket_t CreateConnectionSocket(SteamIPAddress_t nIP, ushort nPort, int nTimeoutSec)
		{
			return SteamNetworking.CreateConnectionSocket(nIP, nPort, nTimeoutSec);
		}

		public static bool DestroySocket(SNetSocket_t hSocket, bool bNotifyRemoteEnd)
		{
			return SteamNetworking.DestroySocket(hSocket, bNotifyRemoteEnd);
		}

		public static bool DestroyListenSocket(SNetListenSocket_t hSocket, bool bNotifyRemoteEnd)
		{
			return SteamNetworking.DestroyListenSocket(hSocket, bNotifyRemoteEnd);
		}

		public static bool SendDataOnSocket(SNetSocket_t hSocket, byte[] pubData, uint cubData, bool bReliable)
		{
			return SteamNetworking.SendDataOnSocket(hSocket, pubData, cubData, bReliable);
		}

		public static bool IsDataAvailableOnSocket(SNetSocket_t hSocket, out uint pcubMsgSize)
		{
			return SteamNetworking.IsDataAvailableOnSocket(hSocket, out pcubMsgSize);
		}

		public static bool RetrieveDataFromSocket(SNetSocket_t hSocket, byte[] pubDest, uint cubDest, out uint pcubMsgSize)
		{
			return SteamNetworking.RetrieveDataFromSocket(hSocket, pubDest, cubDest, out pcubMsgSize);
		}

		public static bool IsDataAvailable(SNetListenSocket_t hListenSocket, out uint pcubMsgSize, out SNetSocket_t phSocket)
		{
			return SteamNetworking.IsDataAvailable(hListenSocket, out pcubMsgSize, out phSocket);
		}

		public static bool RetrieveData(SNetListenSocket_t hListenSocket, byte[] pubDest, uint cubDest, out uint pcubMsgSize, out SNetSocket_t phSocket)
		{
			return SteamNetworking.RetrieveData(hListenSocket, pubDest, cubDest, out pcubMsgSize, out phSocket);
		}

		public static bool GetSocketInfo(SNetSocket_t hSocket, out CSteamID pSteamIDRemote, out int peSocketStatus, out SteamIPAddress_t punIPRemote, out ushort punPortRemote)
		{
			return SteamNetworking.GetSocketInfo(hSocket, out pSteamIDRemote, out peSocketStatus, out punIPRemote, out punPortRemote);
		}

		public static bool GetListenSocketInfo(SNetListenSocket_t hListenSocket, out SteamIPAddress_t pnIP, out ushort pnPort)
		{
			return SteamNetworking.GetListenSocketInfo(hListenSocket, out pnIP, out pnPort);
		}

		public static ESNetSocketConnectionType GetSocketConnectionType(SNetSocket_t hSocket)
		{
			return SteamNetworking.GetSocketConnectionType(hSocket);
		}

		public static int GetMaxPacketSize(SNetSocket_t hSocket)
		{
			return SteamNetworking.GetMaxPacketSize(hSocket);
		}
	}

	public static class SteamGameServerNetworkingMessages
	{
		public static EResult SendMessageToUser(ref SteamNetworkingIdentity identityRemote, IntPtr pubData, uint cubData, int nSendFlags, int nRemoteChannel)
		{
			return SteamNetworkingMessages.SendMessageToUser(ref identityRemote, pubData, cubData, nSendFlags, nRemoteChannel);
		}

		public static int ReceiveMessagesOnChannel(int nLocalChannel, IntPtr[] ppOutMessages, int nMaxMessages)
		{
			return SteamNetworkingMessages.ReceiveMessagesOnChannel(nLocalChannel, ppOutMessages, nMaxMessages);
		}

		public static bool AcceptSessionWithUser(ref SteamNetworkingIdentity identityRemote)
		{
			return SteamNetworkingMessages.AcceptSessionWithUser(ref identityRemote);
		}

		public static bool CloseSessionWithUser(ref SteamNetworkingIdentity identityRemote)
		{
			return SteamNetworkingMessages.CloseSessionWithUser(ref identityRemote);
		}

		public static bool CloseChannelWithUser(ref SteamNetworkingIdentity identityRemote, int nLocalChannel)
		{
			return SteamNetworkingMessages.CloseChannelWithUser(ref identityRemote, nLocalChannel);
		}

		public static ESteamNetworkingConnectionState GetSessionConnectionInfo(ref SteamNetworkingIdentity identityRemote, out SteamNetConnectionInfo_t pConnectionInfo, out SteamNetConnectionRealTimeStatus_t pQuickStatus)
		{
			return SteamNetworkingMessages.GetSessionConnectionInfo(ref identityRemote, out pConnectionInfo, out pQuickStatus);
		}
	}

	public static class SteamGameServerNetworkingSockets
	{
		public static HSteamListenSocket CreateListenSocketIP(ref SteamNetworkingIPAddr localAddress, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			return SteamNetworkingSockets.CreateListenSocketIP(ref localAddress, nOptions, pOptions);
		}

		public static HSteamNetConnection ConnectByIPAddress(ref SteamNetworkingIPAddr address, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			return SteamNetworkingSockets.ConnectByIPAddress(ref address, nOptions, pOptions);
		}

		public static HSteamListenSocket CreateListenSocketP2P(int nLocalVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			return SteamNetworkingSockets.CreateListenSocketP2P(nLocalVirtualPort, nOptions, pOptions);
		}

		public static HSteamNetConnection ConnectP2P(ref SteamNetworkingIdentity identityRemote, int nRemoteVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			return SteamNetworkingSockets.ConnectP2P(ref identityRemote, nRemoteVirtualPort, nOptions, pOptions);
		}

		public static EResult AcceptConnection(HSteamNetConnection hConn)
		{
			return SteamNetworkingSockets.AcceptConnection(hConn);
		}

		public static bool CloseConnection(HSteamNetConnection hPeer, int nReason, string pszDebug, bool bEnableLinger)
		{
			return SteamNetworkingSockets.CloseConnection(hPeer, nReason, pszDebug, bEnableLinger);
		}

		public static bool CloseListenSocket(HSteamListenSocket hSocket)
		{
			return SteamNetworkingSockets.CloseListenSocket(hSocket);
		}

		public static bool SetConnectionUserData(HSteamNetConnection hPeer, long nUserData)
		{
			return SteamNetworkingSockets.SetConnectionUserData(hPeer, nUserData);
		}

		public static long GetConnectionUserData(HSteamNetConnection hPeer)
		{
			return SteamNetworkingSockets.GetConnectionUserData(hPeer);
		}

		public static void SetConnectionName(HSteamNetConnection hPeer, string pszName)
		{
			SteamNetworkingSockets.SetConnectionName(hPeer, pszName);
		}

		public static bool GetConnectionName(HSteamNetConnection hPeer, out string pszName, int nMaxLen)
		{
			return SteamNetworkingSockets.GetConnectionName(hPeer, out pszName, nMaxLen);
		}

		public static EResult SendMessageToConnection(HSteamNetConnection hConn, IntPtr pData, uint cbData, int nSendFlags, out long pOutMessageNumber)
		{
			return SteamNetworkingSockets.SendMessageToConnection(hConn, pData, cbData, nSendFlags, out pOutMessageNumber);
		}

		public static void SendMessages(int nMessages, IntPtr[] pMessages, long[] pOutMessageNumberOrResult)
		{
			SteamNetworkingSockets.SendMessages(nMessages, pMessages, pOutMessageNumberOrResult);
		}

		public static EResult FlushMessagesOnConnection(HSteamNetConnection hConn)
		{
			return SteamNetworkingSockets.FlushMessagesOnConnection(hConn);
		}

		public static int ReceiveMessagesOnConnection(HSteamNetConnection hConn, IntPtr[] ppOutMessages, int nMaxMessages)
		{
			return SteamNetworkingSockets.ReceiveMessagesOnConnection(hConn, ppOutMessages, nMaxMessages);
		}

		public static bool GetConnectionInfo(HSteamNetConnection hConn, out SteamNetConnectionInfo_t pInfo)
		{
			return SteamNetworkingSockets.GetConnectionInfo(hConn, out pInfo);
		}

		public static EResult GetConnectionRealTimeStatus(HSteamNetConnection hConn, ref SteamNetConnectionRealTimeStatus_t pStatus, int nLanes, ref SteamNetConnectionRealTimeLaneStatus_t pLanes)
		{
			return SteamNetworkingSockets.GetConnectionRealTimeStatus(hConn, ref pStatus, nLanes, ref pLanes);
		}

		public static int GetDetailedConnectionStatus(HSteamNetConnection hConn, out string pszBuf, int cbBuf)
		{
			return SteamNetworkingSockets.GetDetailedConnectionStatus(hConn, out pszBuf, cbBuf);
		}

		public static bool GetListenSocketAddress(HSteamListenSocket hSocket, out SteamNetworkingIPAddr address)
		{
			return SteamNetworkingSockets.GetListenSocketAddress(hSocket, out address);
		}

		public static bool CreateSocketPair(out HSteamNetConnection pOutConnection1, out HSteamNetConnection pOutConnection2, bool bUseNetworkLoopback, ref SteamNetworkingIdentity pIdentity1, ref SteamNetworkingIdentity pIdentity2)
		{
			return SteamNetworkingSockets.CreateSocketPair(out pOutConnection1, out pOutConnection2, bUseNetworkLoopback, ref pIdentity1, ref pIdentity2);
		}

		public static EResult ConfigureConnectionLanes(HSteamNetConnection hConn, int nNumLanes, int[] pLanePriorities, ushort[] pLaneWeights)
		{
			return SteamNetworkingSockets.ConfigureConnectionLanes(hConn, nNumLanes, pLanePriorities, pLaneWeights);
		}

		public static bool GetIdentity(out SteamNetworkingIdentity pIdentity)
		{
			return SteamNetworkingSockets.GetIdentity(out pIdentity);
		}

		public static ESteamNetworkingAvailability InitAuthentication()
		{
			return SteamNetworkingSockets.InitAuthentication();
		}

		public static ESteamNetworkingAvailability GetAuthenticationStatus(out SteamNetAuthenticationStatus_t pDetails)
		{
			return SteamNetworkingSockets.GetAuthenticationStatus(out pDetails);
		}

		public static HSteamNetPollGroup CreatePollGroup()
		{
			return SteamNetworkingSockets.CreatePollGroup();
		}

		public static bool DestroyPollGroup(HSteamNetPollGroup hPollGroup)
		{
			return SteamNetworkingSockets.DestroyPollGroup(hPollGroup);
		}

		public static bool SetConnectionPollGroup(HSteamNetConnection hConn, HSteamNetPollGroup hPollGroup)
		{
			return SteamNetworkingSockets.SetConnectionPollGroup(hConn, hPollGroup);
		}

		public static int ReceiveMessagesOnPollGroup(HSteamNetPollGroup hPollGroup, IntPtr[] ppOutMessages, int nMaxMessages)
		{
			return SteamNetworkingSockets.ReceiveMessagesOnPollGroup(hPollGroup, ppOutMessages, nMaxMessages);
		}

		public static bool ReceivedRelayAuthTicket(IntPtr pvTicket, int cbTicket, out SteamDatagramRelayAuthTicket pOutParsedTicket)
		{
			return SteamNetworkingSockets.ReceivedRelayAuthTicket(pvTicket, cbTicket, out pOutParsedTicket);
		}

		public static int FindRelayAuthTicketForServer(ref SteamNetworkingIdentity identityGameServer, int nRemoteVirtualPort, out SteamDatagramRelayAuthTicket pOutParsedTicket)
		{
			return SteamNetworkingSockets.FindRelayAuthTicketForServer(ref identityGameServer, nRemoteVirtualPort, out pOutParsedTicket);
		}

		public static HSteamNetConnection ConnectToHostedDedicatedServer(ref SteamNetworkingIdentity identityTarget, int nRemoteVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			return SteamNetworkingSockets.ConnectToHostedDedicatedServer(ref identityTarget, nRemoteVirtualPort, nOptions, pOptions);
		}

		public static ushort GetHostedDedicatedServerPort()
		{
			return SteamNetworkingSockets.GetHostedDedicatedServerPort();
		}

		public static SteamNetworkingPOPID GetHostedDedicatedServerPOPID()
		{
			return SteamNetworkingSockets.GetHostedDedicatedServerPOPID();
		}

		public static EResult GetHostedDedicatedServerAddress(out SteamDatagramHostedAddress pRouting)
		{
			return SteamNetworkingSockets.GetHostedDedicatedServerAddress(out pRouting);
		}

		public static HSteamListenSocket CreateHostedDedicatedServerListenSocket(int nLocalVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			return SteamNetworkingSockets.CreateHostedDedicatedServerListenSocket(nLocalVirtualPort, nOptions, pOptions);
		}

		public static EResult GetGameCoordinatorServerLogin(IntPtr pLoginInfo, out int pcbSignedBlob, IntPtr pBlob)
		{
			return SteamNetworkingSockets.GetGameCoordinatorServerLogin(pLoginInfo, out pcbSignedBlob, pBlob);
		}

		public static HSteamNetConnection ConnectP2PCustomSignaling(out ISteamNetworkingConnectionSignaling pSignaling, ref SteamNetworkingIdentity pPeerIdentity, int nRemoteVirtualPort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			return SteamNetworkingSockets.ConnectP2PCustomSignaling(out pSignaling, ref pPeerIdentity, nRemoteVirtualPort, nOptions, pOptions);
		}

		public static bool ReceivedP2PCustomSignal(IntPtr pMsg, int cbMsg, out ISteamNetworkingSignalingRecvContext pContext)
		{
			return SteamNetworkingSockets.ReceivedP2PCustomSignal(pMsg, cbMsg, out pContext);
		}

		public static bool GetCertificateRequest(out int pcbBlob, IntPtr pBlob, out SteamNetworkingErrMsg errMsg)
		{
			return SteamNetworkingSockets.GetCertificateRequest(out pcbBlob, pBlob, out errMsg);
		}

		public static bool SetCertificate(IntPtr pCertificate, int cbCertificate, out SteamNetworkingErrMsg errMsg)
		{
			return SteamNetworkingSockets.SetCertificate(pCertificate, cbCertificate, out errMsg);
		}

		public static void ResetIdentity(ref SteamNetworkingIdentity pIdentity)
		{
			SteamNetworkingSockets.ResetIdentity(ref pIdentity);
		}

		public static void RunCallbacks()
		{
			SteamNetworkingSockets.RunCallbacks();
		}

		public static bool BeginAsyncRequestFakeIP(int nNumPorts)
		{
			return SteamNetworkingSockets.BeginAsyncRequestFakeIP(nNumPorts);
		}

		public static void GetFakeIP(int idxFirstPort, out SteamNetworkingFakeIPResult_t pInfo)
		{
			SteamNetworkingSockets.GetFakeIP(idxFirstPort, out pInfo);
		}

		public static HSteamListenSocket CreateListenSocketP2PFakeIP(int idxFakePort, int nOptions, SteamNetworkingConfigValue_t[] pOptions)
		{
			return SteamNetworkingSockets.CreateListenSocketP2PFakeIP(idxFakePort, nOptions, pOptions);
		}

		public static EResult GetRemoteFakeIPForConnection(HSteamNetConnection hConn, out SteamNetworkingIPAddr pOutAddr)
		{
			return SteamNetworkingSockets.GetRemoteFakeIPForConnection(hConn, out pOutAddr);
		}

		public static IntPtr CreateFakeUDPPort(int idxFakeServerPort)
		{
			return SteamNetworkingSockets.CreateFakeUDPPort(idxFakeServerPort);
		}
	}

	public static class SteamGameServerNetworkingUtils
	{
		public static IntPtr AllocateMessage(int cbAllocateBuffer)
		{
			return SteamNetworkingUtils.AllocateMessage(cbAllocateBuffer);
		}

		public static void InitRelayNetworkAccess()
		{
			SteamNetworkingUtils.InitRelayNetworkAccess();
		}

		public static ESteamNetworkingAvailability GetRelayNetworkStatus(out SteamRelayNetworkStatus_t pDetails)
		{
			return SteamNetworkingUtils.GetRelayNetworkStatus(out pDetails);
		}

		public static float GetLocalPingLocation(out SteamNetworkPingLocation_t result)
		{
			return SteamNetworkingUtils.GetLocalPingLocation(out result);
		}

		public static int EstimatePingTimeBetweenTwoLocations(ref SteamNetworkPingLocation_t location1, ref SteamNetworkPingLocation_t location2)
		{
			return SteamNetworkingUtils.EstimatePingTimeBetweenTwoLocations(ref location1, ref location2);
		}

		public static int EstimatePingTimeFromLocalHost(ref SteamNetworkPingLocation_t remoteLocation)
		{
			return SteamNetworkingUtils.EstimatePingTimeFromLocalHost(ref remoteLocation);
		}

		public static void ConvertPingLocationToString(ref SteamNetworkPingLocation_t location, out string pszBuf, int cchBufSize)
		{
			SteamNetworkingUtils.ConvertPingLocationToString(ref location, out pszBuf, cchBufSize);
		}

		public static bool ParsePingLocationString(string pszString, out SteamNetworkPingLocation_t result)
		{
			return SteamNetworkingUtils.ParsePingLocationString(pszString, out result);
		}

		public static bool CheckPingDataUpToDate(float flMaxAgeSeconds)
		{
			return SteamNetworkingUtils.CheckPingDataUpToDate(flMaxAgeSeconds);
		}

		public static int GetPingToDataCenter(SteamNetworkingPOPID popID, out SteamNetworkingPOPID pViaRelayPoP)
		{
			return SteamNetworkingUtils.GetPingToDataCenter(popID, out pViaRelayPoP);
		}

		public static int GetDirectPingToPOP(SteamNetworkingPOPID popID)
		{
			return SteamNetworkingUtils.GetDirectPingToPOP(popID);
		}

		public static int GetPOPCount()
		{
			return SteamNetworkingUtils.GetPOPCount();
		}

		public static int GetPOPList(out SteamNetworkingPOPID list, int nListSz)
		{
			return SteamNetworkingUtils.GetPOPList(out list, nListSz);
		}

		public static SteamNetworkingMicroseconds GetLocalTimestamp()
		{
			return SteamNetworkingUtils.GetLocalTimestamp();
		}

		public static void SetDebugOutputFunction(ESteamNetworkingSocketsDebugOutputType eDetailLevel, FSteamNetworkingSocketsDebugOutput pfnFunc)
		{
			SteamNetworkingUtils.SetDebugOutputFunction(eDetailLevel, pfnFunc);
		}

		public static bool IsFakeIPv4(uint nIPv4)
		{
			return SteamNetworkingUtils.IsFakeIPv4(nIPv4);
		}

		public static ESteamNetworkingFakeIPType GetIPv4FakeIPType(uint nIPv4)
		{
			return SteamNetworkingUtils.GetIPv4FakeIPType(nIPv4);
		}

		public static EResult GetRealIdentityForFakeIP(ref SteamNetworkingIPAddr fakeIP, out SteamNetworkingIdentity pOutRealIdentity)
		{
			return SteamNetworkingUtils.GetRealIdentityForFakeIP(ref fakeIP, out pOutRealIdentity);
		}

		public static bool SetConfigValue(ESteamNetworkingConfigValue eValue, ESteamNetworkingConfigScope eScopeType, IntPtr scopeObj, ESteamNetworkingConfigDataType eDataType, IntPtr pArg)
		{
			return SteamNetworkingUtils.SetConfigValue(eValue, eScopeType, scopeObj, eDataType, pArg);
		}

		public static ESteamNetworkingGetConfigValueResult GetConfigValue(ESteamNetworkingConfigValue eValue, ESteamNetworkingConfigScope eScopeType, IntPtr scopeObj, out ESteamNetworkingConfigDataType pOutDataType, IntPtr pResult, ref ulong cbResult)
		{
			return SteamNetworkingUtils.GetConfigValue(eValue, eScopeType, scopeObj, out pOutDataType, pResult, ref cbResult);
		}

		public static string GetConfigValueInfo(ESteamNetworkingConfigValue eValue, out ESteamNetworkingConfigDataType pOutDataType, out ESteamNetworkingConfigScope pOutScope)
		{
			return SteamNetworkingUtils.GetConfigValueInfo(eValue, out pOutDataType, out pOutScope);
		}

		public static ESteamNetworkingConfigValue IterateGenericEditableConfigValues(ESteamNetworkingConfigValue eCurrent, bool bEnumerateDevVars)
		{
			return SteamNetworkingUtils.IterateGenericEditableConfigValues(eCurrent, bEnumerateDevVars);
		}

		public static void SteamNetworkingIPAddr_ToString(ref SteamNetworkingIPAddr addr, out string buf, uint cbBuf, bool bWithPort)
		{
			SteamNetworkingUtils.SteamNetworkingIPAddr_ToString(ref addr, out buf, cbBuf, bWithPort);
		}

		public static bool SteamNetworkingIPAddr_ParseString(out SteamNetworkingIPAddr pAddr, string pszStr)
		{
			return SteamNetworkingUtils.SteamNetworkingIPAddr_ParseString(out pAddr, pszStr);
		}

		public static ESteamNetworkingFakeIPType SteamNetworkingIPAddr_GetFakeIPType(ref SteamNetworkingIPAddr addr)
		{
			return SteamNetworkingUtils.SteamNetworkingIPAddr_GetFakeIPType(ref addr);
		}

		public static void SteamNetworkingIdentity_ToString(ref SteamNetworkingIdentity identity, out string buf, uint cbBuf)
		{
			SteamNetworkingUtils.SteamNetworkingIdentity_ToString(ref identity, out buf, cbBuf);
		}

		public static bool SteamNetworkingIdentity_ParseString(out SteamNetworkingIdentity pIdentity, string pszStr)
		{
			return SteamNetworkingUtils.SteamNetworkingIdentity_ParseString(out pIdentity, pszStr);
		}
	}

	public static class SteamGameServerUGC
	{
		public static UGCQueryHandle_t CreateQueryUserUGCRequest(AccountID_t unAccountID, EUserUGCList eListType, EUGCMatchingUGCType eMatchingUGCType, EUserUGCListSortOrder eSortOrder, AppId_t nCreatorAppID, AppId_t nConsumerAppID, uint unPage)
		{
			return SteamUGC.CreateQueryUserUGCRequest(unAccountID, eListType, eMatchingUGCType, eSortOrder, nCreatorAppID, nConsumerAppID, unPage);
		}

		public static UGCQueryHandle_t CreateQueryAllUGCRequest(EUGCQuery eQueryType, EUGCMatchingUGCType eMatchingeMatchingUGCTypeFileType, AppId_t nCreatorAppID, AppId_t nConsumerAppID, uint unPage)
		{
			return SteamUGC.CreateQueryAllUGCRequest(eQueryType, eMatchingeMatchingUGCTypeFileType, nCreatorAppID, nConsumerAppID, unPage);
		}

		public static UGCQueryHandle_t CreateQueryUGCDetailsRequest(PublishedFileId_t[] pvecPublishedFileID, uint unNumPublishedFileIDs)
		{
			return SteamUGC.CreateQueryUGCDetailsRequest(pvecPublishedFileID, unNumPublishedFileIDs);
		}

		public static SteamAPICall_t SendQueryUGCRequest(UGCQueryHandle_t handle)
		{
			return SteamUGC.SendQueryUGCRequest(handle);
		}

		public static bool GetQueryUGCResult(UGCQueryHandle_t handle, uint index, out SteamUGCDetails_t pDetails)
		{
			return SteamUGC.GetQueryUGCResult(handle, index, out pDetails);
		}

		public static uint GetQueryUGCNumTags(UGCQueryHandle_t handle, uint index)
		{
			return SteamUGC.GetQueryUGCNumTags(handle, index);
		}

		public static bool GetQueryUGCTag(UGCQueryHandle_t handle, uint index, uint indexTag, out string pchValue, uint cchValueSize)
		{
			return SteamUGC.GetQueryUGCTag(handle, index, indexTag, out pchValue, cchValueSize);
		}

		public static bool GetQueryUGCTagDisplayName(UGCQueryHandle_t handle, uint index, uint indexTag, out string pchValue, uint cchValueSize)
		{
			return SteamUGC.GetQueryUGCTagDisplayName(handle, index, indexTag, out pchValue, cchValueSize);
		}

		public static bool GetQueryUGCPreviewURL(UGCQueryHandle_t handle, uint index, out string pchURL, uint cchURLSize)
		{
			return SteamUGC.GetQueryUGCPreviewURL(handle, index, out pchURL, cchURLSize);
		}

		public static bool GetQueryUGCMetadata(UGCQueryHandle_t handle, uint index, out string pchMetadata, uint cchMetadatasize)
		{
			return SteamUGC.GetQueryUGCMetadata(handle, index, out pchMetadata, cchMetadatasize);
		}

		public static bool GetQueryUGCChildren(UGCQueryHandle_t handle, uint index, PublishedFileId_t[] pvecPublishedFileID, uint cMaxEntries)
		{
			return SteamUGC.GetQueryUGCChildren(handle, index, pvecPublishedFileID, cMaxEntries);
		}

		public static bool GetQueryUGCStatistic(UGCQueryHandle_t handle, uint index, EItemStatistic eStatType, out ulong pStatValue)
		{
			return SteamUGC.GetQueryUGCStatistic(handle, index, eStatType, out pStatValue);
		}

		public static uint GetQueryUGCNumAdditionalPreviews(UGCQueryHandle_t handle, uint index)
		{
			return SteamUGC.GetQueryUGCNumAdditionalPreviews(handle, index);
		}

		public static bool GetQueryUGCAdditionalPreview(UGCQueryHandle_t handle, uint index, uint previewIndex, out string pchURLOrVideoID, uint cchURLSize, out string pchOriginalFileName, uint cchOriginalFileNameSize, out EItemPreviewType pPreviewType)
		{
			return SteamUGC.GetQueryUGCAdditionalPreview(handle, index, previewIndex, out pchURLOrVideoID, cchURLSize, out pchOriginalFileName, cchOriginalFileNameSize, out pPreviewType);
		}

		public static uint GetQueryUGCNumKeyValueTags(UGCQueryHandle_t handle, uint index)
		{
			return SteamUGC.GetQueryUGCNumKeyValueTags(handle, index);
		}

		public static bool GetQueryUGCKeyValueTag(UGCQueryHandle_t handle, uint index, uint keyValueTagIndex, out string pchKey, uint cchKeySize, out string pchValue, uint cchValueSize)
		{
			return SteamUGC.GetQueryUGCKeyValueTag(handle, index, keyValueTagIndex, out pchKey, cchKeySize, out pchValue, cchValueSize);
		}

		public static bool GetQueryUGCKeyValueTag(UGCQueryHandle_t handle, uint index, string pchKey, out string pchValue, uint cchValueSize)
		{
			return SteamUGC.GetQueryUGCKeyValueTag(handle, index, pchKey, out pchValue, cchValueSize);
		}

		public static uint GetNumSupportedGameVersions(UGCQueryHandle_t handle, uint index)
		{
			return SteamUGC.GetNumSupportedGameVersions(handle, index);
		}

		public static bool GetSupportedGameVersionData(UGCQueryHandle_t handle, uint index, uint versionIndex, out string pchGameBranchMin, out string pchGameBranchMax, uint cchGameBranchSize)
		{
			return SteamUGC.GetSupportedGameVersionData(handle, index, versionIndex, out pchGameBranchMin, out pchGameBranchMax, cchGameBranchSize);
		}

		public static uint GetQueryUGCContentDescriptors(UGCQueryHandle_t handle, uint index, EUGCContentDescriptorID[] pvecDescriptors, uint cMaxEntries)
		{
			return SteamUGC.GetQueryUGCContentDescriptors(handle, index, pvecDescriptors, cMaxEntries);
		}

		public static bool ReleaseQueryUGCRequest(UGCQueryHandle_t handle)
		{
			return SteamUGC.ReleaseQueryUGCRequest(handle);
		}

		public static bool AddRequiredTag(UGCQueryHandle_t handle, string pTagName)
		{
			return SteamUGC.AddRequiredTag(handle, pTagName);
		}

		public static bool AddRequiredTagGroup(UGCQueryHandle_t handle, System.Collections.Generic.IList<string> pTagGroups)
		{
			return SteamUGC.AddRequiredTagGroup(handle, pTagGroups);
		}

		public static bool AddExcludedTag(UGCQueryHandle_t handle, string pTagName)
		{
			return SteamUGC.AddExcludedTag(handle, pTagName);
		}

		public static bool SetReturnOnlyIDs(UGCQueryHandle_t handle, bool bReturnOnlyIDs)
		{
			return SteamUGC.SetReturnOnlyIDs(handle, bReturnOnlyIDs);
		}

		public static bool SetReturnKeyValueTags(UGCQueryHandle_t handle, bool bReturnKeyValueTags)
		{
			return SteamUGC.SetReturnKeyValueTags(handle, bReturnKeyValueTags);
		}

		public static bool SetReturnLongDescription(UGCQueryHandle_t handle, bool bReturnLongDescription)
		{
			return SteamUGC.SetReturnLongDescription(handle, bReturnLongDescription);
		}

		public static bool SetReturnMetadata(UGCQueryHandle_t handle, bool bReturnMetadata)
		{
			return SteamUGC.SetReturnMetadata(handle, bReturnMetadata);
		}

		public static bool SetReturnChildren(UGCQueryHandle_t handle, bool bReturnChildren)
		{
			return SteamUGC.SetReturnChildren(handle, bReturnChildren);
		}

		public static bool SetReturnAdditionalPreviews(UGCQueryHandle_t handle, bool bReturnAdditionalPreviews)
		{
			return SteamUGC.SetReturnAdditionalPreviews(handle, bReturnAdditionalPreviews);
		}

		public static bool SetReturnTotalOnly(UGCQueryHandle_t handle, bool bReturnTotalOnly)
		{
			return SteamUGC.SetReturnTotalOnly(handle, bReturnTotalOnly);
		}

		public static bool SetReturnPlaytimeStats(UGCQueryHandle_t handle, uint unDays)
		{
			return SteamUGC.SetReturnPlaytimeStats(handle, unDays);
		}

		public static bool SetLanguage(UGCQueryHandle_t handle, string pchLanguage)
		{
			return SteamUGC.SetLanguage(handle, pchLanguage);
		}

		public static bool SetAllowCachedResponse(UGCQueryHandle_t handle, uint unMaxAgeSeconds)
		{
			return SteamUGC.SetAllowCachedResponse(handle, unMaxAgeSeconds);
		}

		public static bool SetAdminQuery(UGCUpdateHandle_t handle, bool bAdminQuery)
		{
			return SteamUGC.SetAdminQuery(handle, bAdminQuery);
		}

		public static bool SetCloudFileNameFilter(UGCQueryHandle_t handle, string pMatchCloudFileName)
		{
			return SteamUGC.SetCloudFileNameFilter(handle, pMatchCloudFileName);
		}

		public static bool SetMatchAnyTag(UGCQueryHandle_t handle, bool bMatchAnyTag)
		{
			return SteamUGC.SetMatchAnyTag(handle, bMatchAnyTag);
		}

		public static bool SetSearchText(UGCQueryHandle_t handle, string pSearchText)
		{
			return SteamUGC.SetSearchText(handle, pSearchText);
		}

		public static bool SetRankedByTrendDays(UGCQueryHandle_t handle, uint unDays)
		{
			return SteamUGC.SetRankedByTrendDays(handle, unDays);
		}

		public static bool SetTimeCreatedDateRange(UGCQueryHandle_t handle, uint rtStart, uint rtEnd)
		{
			return SteamUGC.SetTimeCreatedDateRange(handle, rtStart, rtEnd);
		}

		public static bool SetTimeUpdatedDateRange(UGCQueryHandle_t handle, uint rtStart, uint rtEnd)
		{
			return SteamUGC.SetTimeUpdatedDateRange(handle, rtStart, rtEnd);
		}

		public static bool AddRequiredKeyValueTag(UGCQueryHandle_t handle, string pKey, string pValue)
		{
			return SteamUGC.AddRequiredKeyValueTag(handle, pKey, pValue);
		}

		public static SteamAPICall_t RequestUGCDetails(PublishedFileId_t nPublishedFileID, uint unMaxAgeSeconds)
		{
			return SteamUGC.RequestUGCDetails(nPublishedFileID, unMaxAgeSeconds);
		}

		public static SteamAPICall_t CreateItem(AppId_t nConsumerAppId, EWorkshopFileType eFileType)
		{
			return SteamUGC.CreateItem(nConsumerAppId, eFileType);
		}

		public static UGCUpdateHandle_t StartItemUpdate(AppId_t nConsumerAppId, PublishedFileId_t nPublishedFileID)
		{
			return SteamUGC.StartItemUpdate(nConsumerAppId, nPublishedFileID);
		}

		public static bool SetItemTitle(UGCUpdateHandle_t handle, string pchTitle)
		{
			return SteamUGC.SetItemTitle(handle, pchTitle);
		}

		public static bool SetItemDescription(UGCUpdateHandle_t handle, string pchDescription)
		{
			return SteamUGC.SetItemDescription(handle, pchDescription);
		}

		public static bool SetItemUpdateLanguage(UGCUpdateHandle_t handle, string pchLanguage)
		{
			return SteamUGC.SetItemUpdateLanguage(handle, pchLanguage);
		}

		public static bool SetItemMetadata(UGCUpdateHandle_t handle, string pchMetaData)
		{
			return SteamUGC.SetItemMetadata(handle, pchMetaData);
		}

		public static bool SetItemVisibility(UGCUpdateHandle_t handle, ERemoteStoragePublishedFileVisibility eVisibility)
		{
			return SteamUGC.SetItemVisibility(handle, eVisibility);
		}

		public static bool SetItemTags(UGCUpdateHandle_t updateHandle, System.Collections.Generic.IList<string> pTags, bool bAllowAdminTags = false)
		{
			return SteamUGC.SetItemTags(updateHandle, pTags, bAllowAdminTags);
		}

		public static bool SetItemContent(UGCUpdateHandle_t handle, string pszContentFolder)
		{
			return SteamUGC.SetItemContent(handle, pszContentFolder);
		}

		public static bool SetItemPreview(UGCUpdateHandle_t handle, string pszPreviewFile)
		{
			return SteamUGC.SetItemPreview(handle, pszPreviewFile);
		}

		public static bool SetAllowLegacyUpload(UGCUpdateHandle_t handle, bool bAllowLegacyUpload)
		{
			return SteamUGC.SetAllowLegacyUpload(handle, bAllowLegacyUpload);
		}

		public static bool RemoveAllItemKeyValueTags(UGCUpdateHandle_t handle)
		{
			return SteamUGC.RemoveAllItemKeyValueTags(handle);
		}

		public static bool RemoveItemKeyValueTags(UGCUpdateHandle_t handle, string pchKey)
		{
			return SteamUGC.RemoveItemKeyValueTags(handle, pchKey);
		}

		public static bool AddItemKeyValueTag(UGCUpdateHandle_t handle, string pchKey, string pchValue)
		{
			return SteamUGC.AddItemKeyValueTag(handle, pchKey, pchValue);
		}

		public static bool AddItemPreviewFile(UGCUpdateHandle_t handle, string pszPreviewFile, EItemPreviewType type)
		{
			return SteamUGC.AddItemPreviewFile(handle, pszPreviewFile, type);
		}

		public static bool AddItemPreviewVideo(UGCUpdateHandle_t handle, string pszVideoID)
		{
			return SteamUGC.AddItemPreviewVideo(handle, pszVideoID);
		}

		public static bool UpdateItemPreviewFile(UGCUpdateHandle_t handle, uint index, string pszPreviewFile)
		{
			return SteamUGC.UpdateItemPreviewFile(handle, index, pszPreviewFile);
		}

		public static bool UpdateItemPreviewVideo(UGCUpdateHandle_t handle, uint index, string pszVideoID)
		{
			return SteamUGC.UpdateItemPreviewVideo(handle, index, pszVideoID);
		}

		public static bool RemoveItemPreview(UGCUpdateHandle_t handle, uint index)
		{
			return SteamUGC.RemoveItemPreview(handle, index);
		}

		public static bool AddContentDescriptor(UGCUpdateHandle_t handle, EUGCContentDescriptorID descid)
		{
			return SteamUGC.AddContentDescriptor(handle, descid);
		}

		public static bool RemoveContentDescriptor(UGCUpdateHandle_t handle, EUGCContentDescriptorID descid)
		{
			return SteamUGC.RemoveContentDescriptor(handle, descid);
		}

		public static bool SetRequiredGameVersions(UGCUpdateHandle_t handle, string pszGameBranchMin, string pszGameBranchMax)
		{
			return SteamUGC.SetRequiredGameVersions(handle, pszGameBranchMin, pszGameBranchMax);
		}

		public static SteamAPICall_t SubmitItemUpdate(UGCUpdateHandle_t handle, string pchChangeNote)
		{
			return SteamUGC.SubmitItemUpdate(handle, pchChangeNote);
		}

		public static EItemUpdateStatus GetItemUpdateProgress(UGCUpdateHandle_t handle, out ulong punBytesProcessed, out ulong punBytesTotal)
		{
			return SteamUGC.GetItemUpdateProgress(handle, out punBytesProcessed, out punBytesTotal);
		}

		public static SteamAPICall_t SetUserItemVote(PublishedFileId_t nPublishedFileID, bool bVoteUp)
		{
			return SteamUGC.SetUserItemVote(nPublishedFileID, bVoteUp);
		}

		public static SteamAPICall_t GetUserItemVote(PublishedFileId_t nPublishedFileID)
		{
			return SteamUGC.GetUserItemVote(nPublishedFileID);
		}

		public static SteamAPICall_t AddItemToFavorites(AppId_t nAppId, PublishedFileId_t nPublishedFileID)
		{
			return SteamUGC.AddItemToFavorites(nAppId, nPublishedFileID);
		}

		public static SteamAPICall_t RemoveItemFromFavorites(AppId_t nAppId, PublishedFileId_t nPublishedFileID)
		{
			return SteamUGC.RemoveItemFromFavorites(nAppId, nPublishedFileID);
		}

		public static SteamAPICall_t SubscribeItem(PublishedFileId_t nPublishedFileID)
		{
			return SteamUGC.SubscribeItem(nPublishedFileID);
		}

		public static SteamAPICall_t UnsubscribeItem(PublishedFileId_t nPublishedFileID)
		{
			return SteamUGC.UnsubscribeItem(nPublishedFileID);
		}

		public static uint GetNumSubscribedItems(bool bIncludeLocallyDisabled = false)
		{
			return SteamUGC.GetNumSubscribedItems(bIncludeLocallyDisabled);
		}

		public static uint GetSubscribedItems(PublishedFileId_t[] pvecPublishedFileID, uint cMaxEntries, bool bIncludeLocallyDisabled = false)
		{
			return SteamUGC.GetSubscribedItems(pvecPublishedFileID, cMaxEntries, bIncludeLocallyDisabled);
		}

		public static uint GetItemState(PublishedFileId_t nPublishedFileID)
		{
			return SteamUGC.GetItemState(nPublishedFileID);
		}

		public static bool GetItemInstallInfo(PublishedFileId_t nPublishedFileID, out ulong punSizeOnDisk, out string pchFolder, uint cchFolderSize, out uint punTimeStamp)
		{
			return SteamUGC.GetItemInstallInfo(nPublishedFileID, out punSizeOnDisk, out pchFolder, cchFolderSize, out punTimeStamp);
		}

		public static bool GetItemDownloadInfo(PublishedFileId_t nPublishedFileID, out ulong punBytesDownloaded, out ulong punBytesTotal)
		{
			return SteamUGC.GetItemDownloadInfo(nPublishedFileID, out punBytesDownloaded, out punBytesTotal);
		}

		public static bool DownloadItem(PublishedFileId_t nPublishedFileID, bool bHighPriority)
		{
			return SteamUGC.DownloadItem(nPublishedFileID, bHighPriority);
		}

		public static bool BInitWorkshopForGameServer(DepotId_t unWorkshopDepotID, string pszFolder)
		{
			return SteamUGC.BInitWorkshopForGameServer(unWorkshopDepotID, pszFolder);
		}

		public static void SuspendDownloads(bool bSuspend)
		{
			SteamUGC.SuspendDownloads(bSuspend);
		}

		public static SteamAPICall_t StartPlaytimeTracking(PublishedFileId_t[] pvecPublishedFileID, uint unNumPublishedFileIDs)
		{
			return SteamUGC.StartPlaytimeTracking(pvecPublishedFileID, unNumPublishedFileIDs);
		}

		public static SteamAPICall_t StopPlaytimeTracking(PublishedFileId_t[] pvecPublishedFileID, uint unNumPublishedFileIDs)
		{
			return SteamUGC.StopPlaytimeTracking(pvecPublishedFileID, unNumPublishedFileIDs);
		}

		public static SteamAPICall_t StopPlaytimeTrackingForAllItems()
		{
			return SteamUGC.StopPlaytimeTrackingForAllItems();
		}

		public static SteamAPICall_t AddDependency(PublishedFileId_t nParentPublishedFileID, PublishedFileId_t nChildPublishedFileID)
		{
			return SteamUGC.AddDependency(nParentPublishedFileID, nChildPublishedFileID);
		}

		public static SteamAPICall_t RemoveDependency(PublishedFileId_t nParentPublishedFileID, PublishedFileId_t nChildPublishedFileID)
		{
			return SteamUGC.RemoveDependency(nParentPublishedFileID, nChildPublishedFileID);
		}

		public static SteamAPICall_t AddAppDependency(PublishedFileId_t nPublishedFileID, AppId_t nAppID)
		{
			return SteamUGC.AddAppDependency(nPublishedFileID, nAppID);
		}

		public static SteamAPICall_t RemoveAppDependency(PublishedFileId_t nPublishedFileID, AppId_t nAppID)
		{
			return SteamUGC.RemoveAppDependency(nPublishedFileID, nAppID);
		}

		public static SteamAPICall_t GetAppDependencies(PublishedFileId_t nPublishedFileID)
		{
			return SteamUGC.GetAppDependencies(nPublishedFileID);
		}

		public static SteamAPICall_t DeleteItem(PublishedFileId_t nPublishedFileID)
		{
			return SteamUGC.DeleteItem(nPublishedFileID);
		}

		public static bool ShowWorkshopEULA()
		{
			return SteamUGC.ShowWorkshopEULA();
		}

		public static SteamAPICall_t GetWorkshopEULAStatus()
		{
			return SteamUGC.GetWorkshopEULAStatus();
		}

		public static uint GetUserContentDescriptorPreferences(EUGCContentDescriptorID[] pvecDescriptors, uint cMaxEntries)
		{
			return SteamUGC.GetUserContentDescriptorPreferences(pvecDescriptors, cMaxEntries);
		}

		public static bool SetItemsDisabledLocally(PublishedFileId_t[] pvecPublishedFileIDs, uint unNumPublishedFileIDs, bool bDisabledLocally)
		{
			return SteamUGC.SetItemsDisabledLocally(pvecPublishedFileIDs, unNumPublishedFileIDs, bDisabledLocally);
		}

		public static bool SetSubscriptionsLoadOrder(PublishedFileId_t[] pvecPublishedFileIDs, uint unNumPublishedFileIDs)
		{
			return SteamUGC.SetSubscriptionsLoadOrder(pvecPublishedFileIDs, unNumPublishedFileIDs);
		}

		public static bool MarkDownloadedItemAsUnused(PublishedFileId_t nPublishedFileID)
		{
			return SteamUGC.MarkDownloadedItemAsUnused(nPublishedFileID);
		}

		public static uint GetNumDownloadedItems()
		{
			return SteamUGC.GetNumDownloadedItems();
		}

		public static uint GetDownloadedItems(out PublishedFileId_t pvecPublishedFileIDs, uint cMaxEntries)
		{
			return SteamUGC.GetDownloadedItems(out pvecPublishedFileIDs, cMaxEntries);
		}
	}

	public static class SteamGameServerUtils
	{
		public static uint GetSecondsSinceAppActive()
		{
			return SteamUtils.GetSecondsSinceAppActive();
		}

		public static uint GetSecondsSinceComputerActive()
		{
			return SteamUtils.GetSecondsSinceComputerActive();
		}

		public static EUniverse GetConnectedUniverse()
		{
			return SteamUtils.GetConnectedUniverse();
		}

		public static uint GetServerRealTime()
		{
			return SteamUtils.GetServerRealTime();
		}

		public static string GetIPCountry()
		{
			return SteamUtils.GetIPCountry();
		}

		public static bool GetImageSize(int iImage, out uint pnWidth, out uint pnHeight)
		{
			return SteamUtils.GetImageSize(iImage, out pnWidth, out pnHeight);
		}

		public static bool GetImageRGBA(int iImage, byte[] pubDest, int nDestBufferSize)
		{
			return SteamUtils.GetImageRGBA(iImage, pubDest, nDestBufferSize);
		}

		public static byte GetCurrentBatteryPower()
		{
			return SteamUtils.GetCurrentBatteryPower();
		}

		public static AppId_t GetAppID()
		{
			return SteamUtils.GetAppID();
		}

		public static void SetOverlayNotificationPosition(ENotificationPosition eNotificationPosition)
		{
			SteamUtils.SetOverlayNotificationPosition(eNotificationPosition);
		}

		public static bool IsAPICallCompleted(SteamAPICall_t hSteamAPICall, out bool pbFailed)
		{
			return SteamUtils.IsAPICallCompleted(hSteamAPICall, out pbFailed);
		}

		public static ESteamAPICallFailure GetAPICallFailureReason(SteamAPICall_t hSteamAPICall)
		{
			return SteamUtils.GetAPICallFailureReason(hSteamAPICall);
		}

		public static bool GetAPICallResult(SteamAPICall_t hSteamAPICall, IntPtr pCallback, int cubCallback, int iCallbackExpected, out bool pbFailed)
		{
			return SteamUtils.GetAPICallResult(hSteamAPICall, pCallback, cubCallback, iCallbackExpected, out pbFailed);
		}

		public static uint GetIPCCallCount()
		{
			return SteamUtils.GetIPCCallCount();
		}

		public static void SetWarningMessageHook(SteamAPIWarningMessageHook_t pFunction)
		{
			SteamUtils.SetWarningMessageHook(pFunction);
		}

		public static bool IsOverlayEnabled()
		{
			return SteamUtils.IsOverlayEnabled();
		}

		public static bool BOverlayNeedsPresent()
		{
			return SteamUtils.BOverlayNeedsPresent();
		}

		public static SteamAPICall_t CheckFileSignature(string szFileName)
		{
			return SteamUtils.CheckFileSignature(szFileName);
		}

		public static bool ShowGamepadTextInput(EGamepadTextInputMode eInputMode, EGamepadTextInputLineMode eLineInputMode, string pchDescription, uint unCharMax, string pchExistingText)
		{
			return SteamUtils.ShowGamepadTextInput(eInputMode, eLineInputMode, pchDescription, unCharMax, pchExistingText);
		}

		public static uint GetEnteredGamepadTextLength()
		{
			return SteamUtils.GetEnteredGamepadTextLength();
		}

		public static bool GetEnteredGamepadTextInput(out string pchText, uint cchText)
		{
			return SteamUtils.GetEnteredGamepadTextInput(out pchText, cchText);
		}

		public static string GetSteamUILanguage()
		{
			return SteamUtils.GetSteamUILanguage();
		}

		public static bool IsSteamRunningInVR()
		{
			return SteamUtils.IsSteamRunningInVR();
		}

		public static void SetOverlayNotificationInset(int nHorizontalInset, int nVerticalInset)
		{
			SteamUtils.SetOverlayNotificationInset(nHorizontalInset, nVerticalInset);
		}

		public static bool IsSteamInBigPictureMode()
		{
			return SteamUtils.IsSteamInBigPictureMode();
		}

		public static void StartVRDashboard()
		{
			SteamUtils.StartVRDashboard();
		}

		public static bool IsVRHeadsetStreamingEnabled()
		{
			return SteamUtils.IsVRHeadsetStreamingEnabled();
		}

		public static void SetVRHeadsetStreamingEnabled(bool bEnabled)
		{
			SteamUtils.SetVRHeadsetStreamingEnabled(bEnabled);
		}

		public static bool IsSteamChinaLauncher()
		{
			return SteamUtils.IsSteamChinaLauncher();
		}

		public static bool InitFilterText(uint unFilterOptions = 0)
		{
			return SteamUtils.InitFilterText(unFilterOptions);
		}

		public static int FilterText(ETextFilteringContext eContext, CSteamID sourceSteamID, string pchInputMessage, out string pchOutFilteredText, uint nByteSizeOutFilteredText)
		{
			return SteamUtils.FilterText(eContext, sourceSteamID, pchInputMessage, out pchOutFilteredText, nByteSizeOutFilteredText);
		}

		public static ESteamIPv6ConnectivityState GetIPv6ConnectivityState(ESteamIPv6ConnectivityProtocol eProtocol)
		{
			return SteamUtils.GetIPv6ConnectivityState(eProtocol);
		}

		public static bool IsSteamRunningOnSteamDeck()
		{
			return SteamUtils.IsSteamRunningOnSteamDeck();
		}

		public static bool ShowFloatingGamepadTextInput(EFloatingGamepadTextInputMode eKeyboardMode, int nTextFieldXPosition, int nTextFieldYPosition, int nTextFieldWidth, int nTextFieldHeight)
		{
			return SteamUtils.ShowFloatingGamepadTextInput(eKeyboardMode, nTextFieldXPosition, nTextFieldYPosition, nTextFieldWidth, nTextFieldHeight);
		}

		public static void SetGameLauncherMode(bool bLauncherMode)
		{
			SteamUtils.SetGameLauncherMode(bLauncherMode);
		}

		public static bool DismissFloatingGamepadTextInput()
		{
			return SteamUtils.DismissFloatingGamepadTextInput();
		}

		public static bool DismissGamepadTextInput()
		{
			return SteamUtils.DismissGamepadTextInput();
		}
	}

	public static class SteamInput
	{
		public static bool Init(bool bExplicitlyCallRunFrame)
		{
			return false;
		}

		public static bool Shutdown()
		{
			return false;
		}

		public static bool SetInputActionManifestFilePath(string pchInputActionManifestAbsolutePath)
		{
			return false;
		}

		public static void RunFrame(bool bReservedValue = true)
		{
			
		}

		public static bool BWaitForData(bool bWaitForever, uint unTimeout)
		{
			return false;
		}

		public static bool BNewDataAvailable()
		{
			return false;
		}

		public static int GetConnectedControllers(InputHandle_t[] handlesOut)
		{
			return 0;
		}

		public static void EnableDeviceCallbacks()
		{
			
		}

		public static void EnableActionEventCallbacks(SteamInputActionEventCallbackPointer pCallback)
		{
			
		}

		public static InputActionSetHandle_t GetActionSetHandle(string pszActionSetName)
		{
			return default(InputActionSetHandle_t);
		}

		public static void ActivateActionSet(InputHandle_t inputHandle, InputActionSetHandle_t actionSetHandle)
		{
			
		}

		public static InputActionSetHandle_t GetCurrentActionSet(InputHandle_t inputHandle)
		{
			return default(InputActionSetHandle_t);
		}

		public static void ActivateActionSetLayer(InputHandle_t inputHandle, InputActionSetHandle_t actionSetLayerHandle)
		{
			
		}

		public static void DeactivateActionSetLayer(InputHandle_t inputHandle, InputActionSetHandle_t actionSetLayerHandle)
		{
			
		}

		public static void DeactivateAllActionSetLayers(InputHandle_t inputHandle)
		{
			
		}

		public static int GetActiveActionSetLayers(InputHandle_t inputHandle, InputActionSetHandle_t[] handlesOut)
		{
			return 0;
		}

		public static InputDigitalActionHandle_t GetDigitalActionHandle(string pszActionName)
		{
			return default(InputDigitalActionHandle_t);
		}

		public static InputDigitalActionData_t GetDigitalActionData(InputHandle_t inputHandle, InputDigitalActionHandle_t digitalActionHandle)
		{
			return default(InputDigitalActionData_t);
		}

		public static int GetDigitalActionOrigins(InputHandle_t inputHandle, InputActionSetHandle_t actionSetHandle, InputDigitalActionHandle_t digitalActionHandle, EInputActionOrigin[] originsOut)
		{
			return 0;
		}

		public static string GetStringForDigitalActionName(InputDigitalActionHandle_t eActionHandle)
		{
			return string.Empty;
		}

		public static InputAnalogActionHandle_t GetAnalogActionHandle(string pszActionName)
		{
			return default(InputAnalogActionHandle_t);
		}

		public static InputAnalogActionData_t GetAnalogActionData(InputHandle_t inputHandle, InputAnalogActionHandle_t analogActionHandle)
		{
			return default(InputAnalogActionData_t);
		}

		public static int GetAnalogActionOrigins(InputHandle_t inputHandle, InputActionSetHandle_t actionSetHandle, InputAnalogActionHandle_t analogActionHandle, EInputActionOrigin[] originsOut)
		{
			return 0;
		}

		public static string GetGlyphPNGForActionOrigin(EInputActionOrigin eOrigin, ESteamInputGlyphSize eSize, uint unFlags)
		{
			return string.Empty;
		}

		public static string GetGlyphSVGForActionOrigin(EInputActionOrigin eOrigin, uint unFlags)
		{
			return string.Empty;
		}

		public static string GetGlyphForActionOrigin_Legacy(EInputActionOrigin eOrigin)
		{
			return string.Empty;
		}

		public static string GetStringForActionOrigin(EInputActionOrigin eOrigin)
		{
			return string.Empty;
		}

		public static string GetStringForAnalogActionName(InputAnalogActionHandle_t eActionHandle)
		{
			return string.Empty;
		}

		public static void StopAnalogActionMomentum(InputHandle_t inputHandle, InputAnalogActionHandle_t eAction)
		{
			
		}

		public static InputMotionData_t GetMotionData(InputHandle_t inputHandle)
		{
			return default(InputMotionData_t);
		}

		public static void TriggerVibration(InputHandle_t inputHandle, ushort usLeftSpeed, ushort usRightSpeed)
		{
			
		}

		public static void TriggerVibrationExtended(InputHandle_t inputHandle, ushort usLeftSpeed, ushort usRightSpeed, ushort usLeftTriggerSpeed, ushort usRightTriggerSpeed)
		{
			
		}

		public static void TriggerSimpleHapticEvent(InputHandle_t inputHandle, EControllerHapticLocation eHapticLocation, byte nIntensity, char nGainDB, byte nOtherIntensity, char nOtherGainDB)
		{
			
		}

		public static void SetLEDColor(InputHandle_t inputHandle, byte nColorR, byte nColorG, byte nColorB, uint nFlags)
		{
			
		}

		public static void Legacy_TriggerHapticPulse(InputHandle_t inputHandle, ESteamControllerPad eTargetPad, ushort usDurationMicroSec)
		{
			
		}

		public static void Legacy_TriggerRepeatedHapticPulse(InputHandle_t inputHandle, ESteamControllerPad eTargetPad, ushort usDurationMicroSec, ushort usOffMicroSec, ushort unRepeat, uint nFlags)
		{
			
		}

		public static bool ShowBindingPanel(InputHandle_t inputHandle)
		{
			return false;
		}

		public static ESteamInputType GetInputTypeForHandle(InputHandle_t inputHandle)
		{
			return default(ESteamInputType);
		}

		public static InputHandle_t GetControllerForGamepadIndex(int nIndex)
		{
			return default(InputHandle_t);
		}

		public static int GetGamepadIndexForController(InputHandle_t ulinputHandle)
		{
			return 0;
		}

		public static string GetStringForXboxOrigin(EXboxOrigin eOrigin)
		{
			return string.Empty;
		}

		public static string GetGlyphForXboxOrigin(EXboxOrigin eOrigin)
		{
			return string.Empty;
		}

		public static EInputActionOrigin GetActionOriginFromXboxOrigin(InputHandle_t inputHandle, EXboxOrigin eOrigin)
		{
			return default(EInputActionOrigin);
		}

		public static EInputActionOrigin TranslateActionOrigin(ESteamInputType eDestinationInputType, EInputActionOrigin eSourceOrigin)
		{
			return default(EInputActionOrigin);
		}

		public static bool GetDeviceBindingRevision(InputHandle_t inputHandle, out int pMajor, out int pMinor)
		{
			pMajor = 0;
			pMinor = 0;
			return false;
		}

		public static uint GetRemotePlaySessionID(InputHandle_t inputHandle)
		{
			return 0;
		}

		public static ushort GetSessionInputConfigurationSettings()
		{
			return 0;
		}

		public static void SetDualSenseTriggerEffect(InputHandle_t inputHandle, IntPtr pParam)
		{
			
		}
	}

	public static partial class SteamMatchmakingServers
	{
		public static void RefreshQuery(HServerListRequest hRequest)
		{
			
		}
	}

	public static partial class SteamRemoteStorage
	{
		public static SteamAPICall_t FileShare(string pchFile)
		{
			return default(SteamAPICall_t);
		}

		public static SteamAPICall_t UGCDownload(UGCHandle_t hContent, uint unPriority)
		{
			return default(SteamAPICall_t);
		}

		public static UGCHandle_t GetCachedUGCHandle(int iCachedContent)
		{
			return default(UGCHandle_t);
		}

		public static SteamAPICall_t PublishVideo(EWorkshopVideoProvider eVideoProvider, string pchVideoAccount, string pchVideoIdentifier, string pchPreviewFile, AppId_t nConsumerAppId, string pchTitle, string pchDescription, ERemoteStoragePublishedFileVisibility eVisibility, System.Collections.Generic.IList<string> pTags)
		{
			return default(SteamAPICall_t);
		}

		public static SteamAPICall_t SetUserPublishedFileAction(PublishedFileId_t unPublishedFileId, EWorkshopFileAction eAction)
		{
			return default(SteamAPICall_t);
		}

		public static SteamAPICall_t EnumeratePublishedWorkshopFiles(EWorkshopEnumerationType eEnumerationType, uint unStartIndex, uint unCount, uint unDays, System.Collections.Generic.IList<string> pTags, System.Collections.Generic.IList<string> pUserTags)
		{
			return default(SteamAPICall_t);
		}

		public static SteamAPICall_t UGCDownloadToLocation(UGCHandle_t hContent, string pchLocation, uint unPriority)
		{
			return default(SteamAPICall_t);
		}
	}

	public static partial class SteamUserStats
	{
		public static int GetNextMostAchievedAchievementInfo(int iIteratorPrevious, out string pchName, uint unNameBufLen, out float pflPercent, out bool pbAchieved)
		{
			pchName = string.Empty;
			pflPercent = 0;
			pbAchieved = false;
			return 0;
		}
	}
}
