using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	public struct SteamUGCQueryCompleted_t
	{
		public ulong m_handle;
		public EResult m_eResult;
		public uint m_unNumResultsReturned;
		public uint m_unTotalMatchingResults;
		public bool m_bCachedData;
		public string m_rgchNextCursor;
	}

	public struct SteamUGCRequestUGCDetailsResult_t
	{
		public SteamUGCDetails_t m_details;
		public bool m_bCachedData;
	}

	public struct CreateItemResult_t
	{
		public EResult m_eResult;
		public PublishedFileId_t m_nPublishedFileId;
		public bool m_bUserNeedsToAcceptWorkshopLegalAgreement;
	}

	public struct SubmitItemUpdateResult_t
	{
		public EResult m_eResult;
		public bool m_bUserNeedsToAcceptWorkshopLegalAgreement;
		public PublishedFileId_t m_nPublishedFileId;
	}

	public struct DownloadItemResult_t
	{
		public AppId_t m_unAppID;
		public PublishedFileId_t m_nPublishedFileID;
		public EResult m_eResult;
	}

	public struct ItemInstalled_t
	{
		public AppId_t m_unAppID;
		public PublishedFileId_t m_nPublishedFileID;
	}

	public struct RemoteStorageSubscribePublishedFileResult_t
	{
		public EResult m_eResult;
		public PublishedFileId_t m_nPublishedFileID;
	}

	public struct RemoteStorageUnsubscribePublishedFileResult_t
	{
		public EResult m_eResult;
		public PublishedFileId_t m_nPublishedFileID;
	}

	public struct UserFavoriteItemsListChanged_t
	{
		public PublishedFileId_t m_nPublishedFileID;
		public EResult m_eResult;
		public bool m_bWasAddRequest;
	}

	public struct SetUserItemVoteResult_t
	{
		public PublishedFileId_t m_nPublishedFileID;
		public EResult m_eResult;
		public bool m_bVoteUp;
	}

	public struct GetUserItemVoteResult_t
	{
		public PublishedFileId_t m_nPublishedFileID;
		public EResult m_eResult;
		public bool m_bVotedUp;
		public bool m_bVotedDown;
		public bool m_bVoteSkipped;
	}

	public static partial class SteamUGC
	{
		class ItemView
		{
			public ulong Id;
			public string Title = "";
			public string Description = "";
			public ulong OwnerId;
			public string OwnerName = "";
			public string[] Tags = new string[0];
			public long Created;
			public long Updated;
			public int VotesUp;
			public int VotesDown;
			public float Score;
			public ulong SizeBytes;
			public string PreviewData;
			public string PreviewName;
		}

		class QueryState
		{
			public ulong Handle;
			public string Sort = "creationdate";
			public string Type;
			public string UserId;
			public string Search;
			public string Language = "english";
			public bool MatchAll;
			public bool ReturnOnlyIds;
			public bool ReturnKeyValueTags;
			public uint Page = 1;
			public List<string> Tags = new List<string>();
			public List<ItemView> Items = new List<ItemView>();
			public int Total;
		}

		class UpdateState
		{
			public ulong Handle;
			public ulong FileId;
			public bool IsDraft;
			public string Title;
			public string Description;
			public string ChangeNote;
			public string Metadata;
			public string ContentFolder;
			public string PreviewFile;
			public string Language;
			public ERemoteStoragePublishedFileVisibility Visibility = ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic;
			public readonly List<string> Tags = new List<string>();
			public readonly Dictionary<string, string> KeyValues = new Dictionary<string, string>();
			public ulong BytesTotal;
			public ulong BytesProcessed;
			public EItemUpdateStatus Status = EItemUpdateStatus.k_EItemUpdateStatusInvalid;
		}

		static readonly Dictionary<ulong, QueryState> _queries = new Dictionary<ulong, QueryState>();
		static readonly Dictionary<ulong, UpdateState> _updates = new Dictionary<ulong, UpdateState>();
		static readonly HashSet<ulong> _drafts = new HashSet<ulong>();
		static readonly List<ulong> _subscribed = new List<ulong>();
		static readonly HashSet<ulong> _downloading = new HashSet<ulong>();
		static ulong _nextHandle = 1;
		static ulong _nextDraft = 0x8000000000000001UL;
		static bool _warmed;

		internal static void WarmUp()
		{
			if (_warmed || !SessionState.Initialized) return;
			_warmed = true;
			ApiClient.Fire(RefreshSubscriptionsAsync, "workshop subscription refresh");
		}

		internal static void Reset()
		{
			_queries.Clear();
			_updates.Clear();
			_drafts.Clear();
			_subscribed.Clear();
			_downloading.Clear();
			_warmed = false;
		}

		static async Task<string> RefreshSubscriptionsAsync()
		{
			var body = "{\"page\":1,\"perPage\":50,\"type\":\"subscribed\",\"sort\":\"creationdate\"}";
			var d = WebworksJson.Obj(await ApiClient.PostAsync("/UGC/Query", body));
			if (d == null) return "";

			var entries = WebworksJson.Arr(d, "entries");
			if (entries == null) return "";

			_subscribed.Clear();
			foreach (var raw in entries)
			{
				var item = ParseItem(raw as Dictionary<string, object>);
				if (item == null) continue;
				_subscribed.Add(item.Id);
				WorkshopContent.SetSubscribed(item.Id, true);
			}
			return "";
		}

		static ItemView ParseItem(Dictionary<string, object> o)
		{
			if (o == null) return null;
			var view = new ItemView
			{
				Id = WebworksJson.Ulong(o, "id"),
				Title = WebworksJson.Str(o, "title") ?? "",
				Description = WebworksJson.Str(o, "description") ?? "",
				OwnerId = WebworksJson.Ulong(o, "ownerId"),
				OwnerName = WebworksJson.Str(o, "ownerName") ?? "",
				Created = (long)WebworksJson.Num(o, "created"),
				Updated = (long)WebworksJson.Num(o, "updated"),
				VotesUp = WebworksJson.Int(o, "votesUp"),
				VotesDown = WebworksJson.Int(o, "votesDown"),
				Score = (float)WebworksJson.Num(o, "score"),
				SizeBytes = (ulong)WebworksJson.Num(o, "sizeBytes"),
				PreviewData = WebworksJson.Str(o, "previewData"),
				PreviewName = WebworksJson.Str(o, "previewName"),
			};

			var tags = WebworksJson.Arr(o, "tags");
			if (tags != null)
			{
				var list = new List<string>();
				foreach (var t in tags) if (t is string) list.Add((string)t);
				view.Tags = list.ToArray();
			}
			return view;
		}

		static string SortFor(EUGCQuery queryType)
		{
			var name = queryType.ToString().ToLowerInvariant();
			if (name.Contains("vote")) return "vote";
			if (name.Contains("trend")) return "vote";
			if (name.Contains("publication")) return "creationdate";
			if (name.Contains("subscription")) return "subscriptions";
			return "creationdate";
		}

		static string TypeFor(EUserUGCList listType)
		{
			var name = listType.ToString().ToLowerInvariant();
			if (name.Contains("published")) return "published";
			if (name.Contains("subscribed")) return "subscribed";
			if (name.Contains("favorited")) return "favorited";
			if (name.Contains("votedup")) return "votedup";
			if (name.Contains("voteddown")) return "voteddown";
			if (name.Contains("votedon")) return "votedon";
			return null;
		}

		static UGCQueryHandle_t NewQuery(QueryState state)
		{
			state.Handle = _nextHandle++;
			_queries[state.Handle] = state;
			return new UGCQueryHandle_t(state.Handle);
		}

		public static UGCQueryHandle_t CreateQueryAllUGCRequest(EUGCQuery eQueryType, EUGCMatchingUGCType eMatchingeMatchingUGCTypeFileType, AppId_t nCreatorAppID, AppId_t nConsumerAppID, uint unPage)
		{
			var state = new QueryState { Sort = SortFor(eQueryType), Page = Math.Max(1, unPage) };
			return NewQuery(state);
		}

		public static UGCQueryHandle_t CreateQueryUserUGCRequest(AccountID_t unAccountID, EUserUGCList eListType, EUGCMatchingUGCType eMatchingUGCType, EUserUGCListSortOrder eSortOrder, AppId_t nCreatorAppID, AppId_t nConsumerAppID, uint unPage)
		{
			var state = new QueryState
			{
				Type = TypeFor(eListType),
				UserId = unAccountID.m_AccountID.ToString(CultureInfo.InvariantCulture),
				Sort = eSortOrder.ToString().ToLowerInvariant().Contains("vote") ? "vote" : "creationdate",
				Page = Math.Max(1, unPage),
			};
			return NewQuery(state);
		}

		public static UGCQueryHandle_t CreateQueryUGCDetailsRequest(PublishedFileId_t[] pvecPublishedFileID, uint unNumPublishedFileIDs)
		{
			var state = new QueryState();
			if (pvecPublishedFileID != null)
			{
				for (uint i = 0; i < unNumPublishedFileIDs && i < pvecPublishedFileID.Length; i++)
					state.Tags.Add(pvecPublishedFileID[i].m_PublishedFileId.ToString(CultureInfo.InvariantCulture));
			}
			state.Sort = "details";
			return NewQuery(state);
		}

		static QueryState Find(UGCQueryHandle_t handle)
		{
			QueryState state;
			return _queries.TryGetValue(handle.m_UGCQueryHandle, out state) ? state : null;
		}

		public static SteamAPICall_t SendQueryUGCRequest(UGCQueryHandle_t handle)
		{
			var call = ResultBus.NewCall();
			var state = Find(handle);
			if (state == null)
			{
				ResultBus.Complete<SteamUGCQueryCompleted_t>(call, new SteamUGCQueryCompleted_t { m_handle = handle.m_UGCQueryHandle, m_eResult = EResult.k_EResultInvalidParam });
				return call;
			}

			ApiClient.Fire(async () =>
			{
				var body = new StringBuilder("{\"page\":").Append(state.Page);
				body.Append(",\"perPage\":50,\"sort\":").Append(WebworksJson.Quote(state.Sort));
				if (!string.IsNullOrEmpty(state.Search)) body.Append(",\"searchText\":").Append(WebworksJson.Quote(state.Search));
				if (!string.IsNullOrEmpty(state.Type)) body.Append(",\"type\":").Append(WebworksJson.Quote(state.Type));
				if (!string.IsNullOrEmpty(state.UserId)) body.Append(",\"userId\":").Append(WebworksJson.Quote(state.UserId));
				body.Append(",\"matchAllTags\":").Append(state.MatchAll ? "true" : "false");
				body.Append(",\"tags\":[");
				for (int i = 0; i < state.Tags.Count; i++) body.Append(i > 0 ? "," : "").Append(WebworksJson.Quote(state.Tags[i]));
				body.Append("]}");

				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/UGC/Query", body.ToString()));
				if (payload == null)
				{
					ResultBus.Complete<SteamUGCQueryCompleted_t>(call, new SteamUGCQueryCompleted_t
					{
						m_handle = handle.m_UGCQueryHandle,
						m_eResult = EResult.k_EResultNoConnection,
					});
					return "";
				}

				state.Items.Clear();
				var entries = WebworksJson.Arr(payload, "entries");
				if (entries != null)
				{
					foreach (var raw in entries)
					{
						var item = ParseItem(raw as Dictionary<string, object>);
						if (item != null) state.Items.Add(item);
					}
				}
				state.Total = WebworksJson.Int(payload, "totalCount");

				ResultBus.Complete<SteamUGCQueryCompleted_t>(call, new SteamUGCQueryCompleted_t
				{
					m_handle = handle.m_UGCQueryHandle,
					m_eResult = EResult.k_EResultOK,
					m_unNumResultsReturned = (uint)state.Items.Count,
					m_unTotalMatchingResults = (uint)state.Total,
					m_bCachedData = false,
				});
				return "";
			}, "SendQueryUGCRequest");

			return call;
		}

		static bool Fill(QueryState state, uint index, out ItemView item)
		{
			item = null;
			if (state == null || index >= state.Items.Count) return false;
			item = state.Items[(int)index];
			return true;
		}

		public static bool GetQueryUGCResult(UGCQueryHandle_t handle, uint index, out SteamUGCDetails_t pDetails)
		{
			pDetails = default(SteamUGCDetails_t);
			ItemView item;
			if (!Fill(Find(handle), index, out item)) return false;

			pDetails.m_nPublishedFileId = new PublishedFileId_t(item.Id);
			pDetails.m_eResult = EResult.k_EResultOK;
			pDetails.m_eFileType = EWorkshopFileType.k_EWorkshopFileTypeCommunity;
			pDetails.m_nCreatorAppID = new AppId_t(0);
			pDetails.m_nConsumerAppID = new AppId_t(0);
			pDetails.m_rgchTitle = item.Title;
			pDetails.m_rgchDescription = item.Description;
			pDetails.m_ulSteamIDOwner = item.OwnerId;
			pDetails.m_rtimeCreated = (uint)(item.Created / 1000);
			pDetails.m_rtimeUpdated = (uint)(item.Updated / 1000);
			pDetails.m_eVisibility = ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic;
			pDetails.m_rgchTags = string.Join(",", item.Tags);
			pDetails.m_pchFileName = "";
			pDetails.m_nFileSize = (int)Math.Min(int.MaxValue, item.SizeBytes);
			pDetails.m_unVotesUp = (uint)Math.Max(0, item.VotesUp);
			pDetails.m_unVotesDown = (uint)Math.Max(0, item.VotesDown);
			pDetails.m_flScore = item.Score;
			pDetails.m_rgchURL = "";
			return true;
		}

		public static uint GetQueryUGCNumTags(UGCQueryHandle_t handle, uint index)
		{
			ItemView item;
			return Fill(Find(handle), index, out item) ? (uint)item.Tags.Length : 0;
		}

		public static bool GetQueryUGCTag(UGCQueryHandle_t handle, uint index, uint indexTag, out string pchValue, uint cchValueSize)
		{
			pchValue = "";
			ItemView item;
			if (!Fill(Find(handle), index, out item) || indexTag >= item.Tags.Length) return false;
			pchValue = item.Tags[(int)indexTag];
			return true;
		}

		public static bool GetQueryUGCTagDisplayName(UGCQueryHandle_t handle, uint index, uint indexTag, out string pchValue, uint cchValueSize)
		{
			return GetQueryUGCTag(handle, index, indexTag, out pchValue, cchValueSize);
		}

		public static bool GetQueryUGCPreviewURL(UGCQueryHandle_t handle, uint index, out string pchURL, uint cchURLSize)
		{
			pchURL = "";
			ItemView item;
			if (!Fill(Find(handle), index, out item) || string.IsNullOrEmpty(item.PreviewData)) return false;
			pchURL = DataUrl(item.PreviewName, item.PreviewData);
			return true;
		}

		static string DataUrl(string name, string base64)
		{
			if (string.IsNullOrEmpty(base64)) return "";
			var n = (name ?? "").ToLowerInvariant();
			var mime = n.EndsWith(".png") ? "image/png" : (n.EndsWith(".jpg") || n.EndsWith(".jpeg")) ? "image/jpeg" : "application/octet-stream";
			return "data:" + mime + ";base64," + base64;
		}

		public static bool GetQueryUGCMetadata(UGCQueryHandle_t handle, uint index, out string pchMetadata, uint cchMetadatasize)
		{
			pchMetadata = "";
			return true;
		}

		public static bool GetQueryUGCChildren(UGCQueryHandle_t handle, uint index, PublishedFileId_t[] pvecPublishedFileID, uint cMaxEntries)
		{
			return false;
		}

		public static bool GetQueryUGCStatistic(UGCQueryHandle_t handle, uint index, EItemStatistic eStatType, out ulong pStatValue)
		{
			pStatValue = 0;
			ItemView item;
			if (!Fill(Find(handle), index, out item)) return false;
			if (eStatType == EItemStatistic.k_EItemStatistic_NumSubscriptions) pStatValue = 0;
			else if (eStatType == EItemStatistic.k_EItemStatistic_ReportScore) pStatValue = (ulong)Math.Max(0, item.VotesUp - item.VotesDown);
			else pStatValue = 0;
			return true;
		}

		public static uint GetQueryUGCNumAdditionalPreviews(UGCQueryHandle_t handle, uint index) { return 0; }

		public static bool GetQueryUGCAdditionalPreview(UGCQueryHandle_t handle, uint index, uint previewIndex, out string pchURLOrVideoID, uint cchURLSize, out string pchOriginalFileName, uint cchOriginalFileNameSize, out EItemPreviewType pPreviewType)
		{
			pchURLOrVideoID = "";
			pchOriginalFileName = "";
			pPreviewType = EItemPreviewType.k_EItemPreviewType_Image;
			return false;
		}

		public static uint GetQueryUGCNumKeyValueTags(UGCQueryHandle_t handle, uint index)
		{
			ItemView item;
			if (!Fill(Find(handle), index, out item)) return 0;
			uint count = 0;
			foreach (var tag in item.Tags) if (tag.IndexOf(':') > 0) count++;
			return count;
		}

		public static bool GetQueryUGCKeyValueTag(UGCQueryHandle_t handle, uint index, uint keyValueTagIndex, out string pchKey, uint cchKeySize, out string pchValue, uint cchValueSize)
		{
			pchKey = "";
			pchValue = "";
			ItemView item;
			if (!Fill(Find(handle), index, out item)) return false;

			uint seen = 0;
			foreach (var tag in item.Tags)
			{
				int split = tag.IndexOf(':');
				if (split <= 0) continue;
				if (seen++ != keyValueTagIndex) continue;
				pchKey = tag.Substring(0, split);
				pchValue = tag.Substring(split + 1);
				return true;
			}
			return false;
		}

		public static bool GetQueryUGCKeyValueTag(UGCQueryHandle_t handle, uint index, string pchKey, out string pchValue, uint cchValueSize)
		{
			pchValue = "";
			ItemView item;
			if (!Fill(Find(handle), index, out item)) return false;
			foreach (var tag in item.Tags)
			{
				int split = tag.IndexOf(':');
				if (split <= 0) continue;
				if (!string.Equals(tag.Substring(0, split), pchKey, StringComparison.OrdinalIgnoreCase)) continue;
				pchValue = tag.Substring(split + 1);
				return true;
			}
			return false;
		}

		public static uint GetNumSupportedGameVersions(UGCQueryHandle_t handle, uint index) { return 0; }

		public static bool GetSupportedGameVersionData(UGCQueryHandle_t handle, uint index, uint versionIndex, out string pchGameBranchMin, out string pchGameBranchMax, uint cchGameBranchSize)
		{
			pchGameBranchMin = "";
			pchGameBranchMax = "";
			return false;
		}

		public static uint GetQueryUGCContentDescriptors(UGCQueryHandle_t handle, uint index, EUGCContentDescriptorID[] pvecDescriptors, uint cMaxEntries) { return 0; }

		public static bool ReleaseQueryUGCRequest(UGCQueryHandle_t handle)
		{
			return _queries.Remove(handle.m_UGCQueryHandle);
		}

		static QueryState For(UGCQueryHandle_t handle) { return Find(handle); }

		public static bool AddRequiredTag(UGCQueryHandle_t handle, string pTagName)
		{
			var state = For(handle);
			if (state == null || string.IsNullOrEmpty(pTagName)) return false;
			if (!state.Tags.Contains(pTagName)) state.Tags.Add(pTagName);
			return true;
		}

		public static bool AddRequiredTagGroup(UGCQueryHandle_t handle, System.Collections.Generic.IList<string> pTagGroups)
		{
			if (pTagGroups == null) return false;
			foreach (var tag in pTagGroups) AddRequiredTag(handle, tag);
			return true;
		}

		public static bool AddExcludedTag(UGCQueryHandle_t handle, string pTagName) { return For(handle) != null; }
		public static bool SetReturnOnlyIDs(UGCQueryHandle_t handle, bool bReturnOnlyIDs) { var s = For(handle); if (s == null) return false; s.ReturnOnlyIds = bReturnOnlyIDs; return true; }
		public static bool SetReturnKeyValueTags(UGCQueryHandle_t handle, bool bReturnKeyValueTags) { var s = For(handle); if (s == null) return false; s.ReturnKeyValueTags = bReturnKeyValueTags; return true; }
		public static bool SetReturnLongDescription(UGCQueryHandle_t handle, bool bReturnLongDescription) { return For(handle) != null; }
		public static bool SetReturnMetadata(UGCQueryHandle_t handle, bool bReturnMetadata) { return For(handle) != null; }
		public static bool SetReturnChildren(UGCQueryHandle_t handle, bool bReturnChildren) { return For(handle) != null; }
		public static bool SetReturnAdditionalPreviews(UGCQueryHandle_t handle, bool bReturnAdditionalPreviews) { return For(handle) != null; }
		public static bool SetReturnTotalOnly(UGCQueryHandle_t handle, bool bReturnTotalOnly) { return For(handle) != null; }
		public static bool SetReturnPlaytimeStats(UGCQueryHandle_t handle, uint unDays) { return For(handle) != null; }

		public static bool SetLanguage(UGCQueryHandle_t handle, string pchLanguage)
		{
			var state = For(handle);
			if (state == null) return false;
			state.Language = pchLanguage;
			return true;
		}

		public static bool SetAllowCachedResponse(UGCQueryHandle_t handle, uint unMaxAgeSeconds) { return For(handle) != null; }
		public static bool SetAdminQuery(UGCUpdateHandle_t handle, bool bAdminQuery) { return false; }
		public static bool SetCloudFileNameFilter(UGCQueryHandle_t handle, string pMatchCloudFileName) { return For(handle) != null; }

		public static bool SetMatchAnyTag(UGCQueryHandle_t handle, bool bMatchAnyTag)
		{
			var state = For(handle);
			if (state == null) return false;
			state.MatchAll = !bMatchAnyTag;
			return true;
		}

		public static bool SetSearchText(UGCQueryHandle_t handle, string pSearchText)
		{
			var state = For(handle);
			if (state == null) return false;
			state.Search = pSearchText;
			return true;
		}

		public static bool SetRankedByTrendDays(UGCQueryHandle_t handle, uint unDays) { return For(handle) != null; }
		public static bool SetTimeCreatedDateRange(UGCQueryHandle_t handle, uint rtStart, uint rtEnd) { return For(handle) != null; }
		public static bool SetTimeUpdatedDateRange(UGCQueryHandle_t handle, uint rtStart, uint rtEnd) { return For(handle) != null; }

		public static bool AddRequiredKeyValueTag(UGCQueryHandle_t handle, string pKey, string pValue)
		{
			return AddRequiredTag(handle, pKey + ":" + pValue);
		}

		public static SteamAPICall_t RequestUGCDetails(PublishedFileId_t nPublishedFileID, uint unMaxAgeSeconds)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/UGC/Item?id=" + nPublishedFileID.m_PublishedFileId.ToString(CultureInfo.InvariantCulture)));
				var result = new SteamUGCRequestUGCDetailsResult_t
				{
					m_details = new SteamUGCDetails_t { m_eResult = EResult.k_EResultFileNotFound },
				};

				var item = payload != null && WebworksJson.Bool(payload, "found") ? ParseItem(WebworksJson.Map(payload, "item")) : null;
				if (item != null)
				{
					result.m_details.m_nPublishedFileId = new PublishedFileId_t(item.Id);
					result.m_details.m_eResult = EResult.k_EResultOK;
					result.m_details.m_rgchTitle = item.Title;
					result.m_details.m_rgchDescription = item.Description;
					result.m_details.m_ulSteamIDOwner = item.OwnerId;
					result.m_details.m_rtimeCreated = (uint)(item.Created / 1000);
					result.m_details.m_rtimeUpdated = (uint)(item.Updated / 1000);
					result.m_details.m_rgchTags = string.Join(",", item.Tags);
					result.m_details.m_nFileSize = (int)Math.Min(int.MaxValue, item.SizeBytes);
					result.m_details.m_unVotesUp = (uint)Math.Max(0, item.VotesUp);
					result.m_details.m_unVotesDown = (uint)Math.Max(0, item.VotesDown);
					result.m_details.m_flScore = item.Score;
				}

				ResultBus.Complete<SteamUGCRequestUGCDetailsResult_t>(call, result);
				return "";
			}, "RequestUGCDetails");
			return call;
		}

		public static SteamAPICall_t CreateItem(AppId_t nConsumerAppId, EWorkshopFileType eFileType)
		{
			var call = ResultBus.NewCall();
			var id = _nextDraft;
			_drafts.Add(id);
			var result = new CreateItemResult_t { m_eResult = EResult.k_EResultOK, m_nPublishedFileId = new PublishedFileId_t(id) };
			ResultBus.Complete<CreateItemResult_t>(call, result);
			return call;
		}

		public static UGCUpdateHandle_t StartItemUpdate(AppId_t nConsumerAppId, PublishedFileId_t nPublishedFileID)
		{
			var state = new UpdateState
			{
				Handle = _nextHandle++,
				FileId = nPublishedFileID.m_PublishedFileId,
				IsDraft = _drafts.Contains(nPublishedFileID.m_PublishedFileId),
			};
			_updates[state.Handle] = state;
			return new UGCUpdateHandle_t(state.Handle);
		}

		static UpdateState UpdateOf(UGCUpdateHandle_t handle)
		{
			UpdateState state;
			return _updates.TryGetValue(handle.m_UGCUpdateHandle, out state) ? state : null;
		}

		public static bool SetItemTitle(UGCUpdateHandle_t handle, string pchTitle)
		{
			var state = UpdateOf(handle);
			if (state == null) return false;
			state.Title = pchTitle;
			return true;
		}

		public static bool SetItemDescription(UGCUpdateHandle_t handle, string pchDescription)
		{
			var state = UpdateOf(handle);
			if (state == null) return false;
			state.Description = pchDescription;
			return true;
		}

		public static bool SetItemUpdateLanguage(UGCUpdateHandle_t handle, string pchLanguage)
		{
			var state = UpdateOf(handle);
			if (state == null) return false;
			state.Language = pchLanguage;
			return true;
		}

		public static bool SetItemMetadata(UGCUpdateHandle_t handle, string pchMetaData)
		{
			var state = UpdateOf(handle);
			if (state == null) return false;
			state.Metadata = pchMetaData;
			return true;
		}

		public static bool SetItemVisibility(UGCUpdateHandle_t handle, ERemoteStoragePublishedFileVisibility eVisibility)
		{
			var state = UpdateOf(handle);
			if (state == null) return false;
			state.Visibility = eVisibility;
			return true;
		}

		public static bool SetItemTags(UGCUpdateHandle_t updateHandle, System.Collections.Generic.IList<string> pTags, bool bAllowAdminTags = false)
		{
			var state = UpdateOf(updateHandle);
			if (state == null) return false;
			state.Tags.Clear();
			if (pTags != null) foreach (var tag in pTags) if (!string.IsNullOrEmpty(tag)) state.Tags.Add(tag);
			return true;
		}

		public static bool SetItemContent(UGCUpdateHandle_t handle, string pszContentFolder)
		{
			var state = UpdateOf(handle);
			if (state == null) return false;
			state.ContentFolder = pszContentFolder;
			return true;
		}

		public static bool SetItemPreview(UGCUpdateHandle_t handle, string pszPreviewFile)
		{
			var state = UpdateOf(handle);
			if (state == null) return false;
			state.PreviewFile = pszPreviewFile;
			return true;
		}

		public static bool SetAllowLegacyUpload(UGCUpdateHandle_t handle, bool bAllowLegacyUpload) { return UpdateOf(handle) != null; }
		public static bool RemoveAllItemKeyValueTags(UGCUpdateHandle_t handle) { var s = UpdateOf(handle); if (s == null) return false; s.KeyValues.Clear(); return true; }

		public static bool RemoveItemKeyValueTags(UGCUpdateHandle_t handle, string pchKey)
		{
			var state = UpdateOf(handle);
			if (state == null) return false;
			state.KeyValues.Remove(pchKey);
			return true;
		}

		public static bool AddItemKeyValueTag(UGCUpdateHandle_t handle, string pchKey, string pchValue)
		{
			var state = UpdateOf(handle);
			if (state == null) return false;
			state.KeyValues[pchKey] = pchValue;
			return true;
		}

		public static bool AddItemPreviewFile(UGCUpdateHandle_t handle, string pszPreviewFile, EItemPreviewType type) { return UpdateOf(handle) != null; }
		public static bool AddItemPreviewVideo(UGCUpdateHandle_t handle, string pszVideoID) { return UpdateOf(handle) != null; }
		public static bool UpdateItemPreviewFile(UGCUpdateHandle_t handle, uint index, string pszPreviewFile) { return UpdateOf(handle) != null; }
		public static bool UpdateItemPreviewVideo(UGCUpdateHandle_t handle, uint index, string pszVideoID) { return UpdateOf(handle) != null; }
		public static bool RemoveItemPreview(UGCUpdateHandle_t handle, uint index) { return UpdateOf(handle) != null; }
		public static bool AddContentDescriptor(UGCUpdateHandle_t handle, EUGCContentDescriptorID descid) { return UpdateOf(handle) != null; }
		public static bool RemoveContentDescriptor(UGCUpdateHandle_t handle, EUGCContentDescriptorID descid) { return UpdateOf(handle) != null; }
		public static bool SetRequiredGameVersions(UGCUpdateHandle_t handle, string pszGameBranchMin, string pszGameBranchMax) { return UpdateOf(handle) != null; }

		public static SteamAPICall_t SubmitItemUpdate(UGCUpdateHandle_t handle, string pchChangeNote)
		{
			var call = ResultBus.NewCall();
			var state = UpdateOf(handle);
			if (state == null)
			{
				ResultBus.Complete<SubmitItemUpdateResult_t>(call, new SubmitItemUpdateResult_t { m_eResult = EResult.k_EResultInvalidParam });
				return call;
			}

			state.ChangeNote = pchChangeNote;
			state.Status = EItemUpdateStatus.k_EItemUpdateStatusCommittingChanges;
			state.BytesProcessed = 0;
			state.BytesTotal = 0;

			ApiClient.Fire(async () =>
			{
				var content = WorkshopContent.ReadFolder(state.ContentFolder);
				foreach (var bytes in content.Values) state.BytesTotal += (ulong)bytes.Length;

				string previewName = null;
				byte[] previewBytes = null;
				if (!string.IsNullOrEmpty(state.PreviewFile))
				{
					try
					{
						previewBytes = File.ReadAllBytes(state.PreviewFile);
						previewName = Path.GetFileName(state.PreviewFile);
						state.BytesTotal += (ulong)previewBytes.Length;
					}
					catch (Exception ex)
					{
						Debug.LogWarning("[Webworks] workshop preview file could not be read: " + ex.Message);
					}
				}
				state.BytesProcessed = state.BytesTotal;

				var body = new StringBuilder("{");
				if (!state.IsDraft) body.Append("\"fileId\":").Append(WebworksJson.Quote(state.FileId.ToString(CultureInfo.InvariantCulture))).Append(",");
				if (!state.IsDraft) body.Append("\"replaceContent\":true,");
				body.Append("\"title\":").Append(WebworksJson.Quote(state.Title ?? "")).Append(",");
				body.Append("\"description\":").Append(WebworksJson.Quote(state.Description ?? "")).Append(",");
				if (state.ChangeNote != null) body.Append("\"changelog\":").Append(WebworksJson.Quote(state.ChangeNote)).Append(",");
				if (previewName != null)
				{
					body.Append("\"previewName\":").Append(WebworksJson.Quote(previewName)).Append(",");
					body.Append("\"previewData\":").Append(WebworksJson.Quote(Convert.ToBase64String(previewBytes))).Append(",");
				}

				body.Append("\"tags\":[");
				var tags = new List<string>(state.Tags);
				foreach (var kv in state.KeyValues) tags.Add(kv.Key + ":" + kv.Value);
				for (int i = 0; i < tags.Count; i++) body.Append(i > 0 ? "," : "").Append(WebworksJson.Quote(tags[i]));
				body.Append("],\"content\":{");

				int index = 0;
				foreach (var kv in content)
				{
					body.Append(index++ > 0 ? "," : "").Append(WebworksJson.Quote(kv.Key)).Append(":").Append(WebworksJson.Quote(Convert.ToBase64String(kv.Value)));
				}
				body.Append("}}");

				var payload = WebworksJson.Obj(await ApiClient.PostAsync(state.IsDraft ? "/UGC/Publish" : "/UGC/Update", body.ToString()));
				var error = payload != null ? WebworksJson.Str(payload, "error") : "no_response";
				ulong publishedId = state.FileId;

				if (error == null)
				{
					var returned = WebworksJson.Str(payload, "fileId");
					if (!string.IsNullOrEmpty(returned)) ulong.TryParse(returned, out publishedId);
					if (state.IsDraft)
					{
						_drafts.Remove(state.FileId);
						_drafts.Remove(publishedId);
					}
					WorkshopContent.SetSubscribed(publishedId, true);
					if (!_subscribed.Contains(publishedId)) _subscribed.Add(publishedId);
				}

				state.Status = error == null ? EItemUpdateStatus.k_EItemUpdateStatusInvalid : EItemUpdateStatus.k_EItemUpdateStatusInvalid;
				ResultBus.Complete<SubmitItemUpdateResult_t>(call, new SubmitItemUpdateResult_t
				{
					m_eResult = error == null ? EResult.k_EResultOK : EResult.k_EResultFail,
					m_nPublishedFileId = new PublishedFileId_t(publishedId),
				});
				return "";
			}, "SubmitItemUpdate");

			return call;
		}

		public static EItemUpdateStatus GetItemUpdateProgress(UGCUpdateHandle_t handle, out ulong punBytesProcessed, out ulong punBytesTotal)
		{
			var state = UpdateOf(handle);
			punBytesProcessed = state != null ? state.BytesProcessed : 0;
			punBytesTotal = state != null ? state.BytesTotal : 0;
			return state != null ? state.Status : EItemUpdateStatus.k_EItemUpdateStatusInvalid;
		}

		public static SteamAPICall_t SetUserItemVote(PublishedFileId_t nPublishedFileID, bool bVoteUp)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var body = "{\"fileId\":" + WebworksJson.Quote(nPublishedFileID.m_PublishedFileId.ToString(CultureInfo.InvariantCulture)) + ",\"up\":" + (bVoteUp ? "true" : "false") + "}";
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/UGC/Vote", body));
				var ok = payload != null && WebworksJson.Str(payload, "error") == null;
				ResultBus.Complete<SetUserItemVoteResult_t>(call, new SetUserItemVoteResult_t
				{
					m_nPublishedFileID = nPublishedFileID,
					m_eResult = ok ? EResult.k_EResultOK : EResult.k_EResultFail,
					m_bVoteUp = bVoteUp,
				});
				return "";
			}, "SetUserItemVote");
			return call;
		}

		public static SteamAPICall_t GetUserItemVote(PublishedFileId_t nPublishedFileID)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/UGC/Vote?fileId=" + nPublishedFileID.m_PublishedFileId.ToString(CultureInfo.InvariantCulture)));
				ResultBus.Complete<GetUserItemVoteResult_t>(call, new GetUserItemVoteResult_t
				{
					m_nPublishedFileID = nPublishedFileID,
					m_eResult = payload != null ? EResult.k_EResultOK : EResult.k_EResultFail,
					m_bVotedUp = payload != null && WebworksJson.Bool(payload, "votedUp"),
					m_bVotedDown = payload != null && WebworksJson.Bool(payload, "votedDown"),
				});
				return "";
			}, "GetUserItemVote");
			return call;
		}

		static SteamAPICall_t ToggleFavorite(PublishedFileId_t id, bool favorite)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var body = "{\"fileId\":" + WebworksJson.Quote(id.m_PublishedFileId.ToString(CultureInfo.InvariantCulture)) + "}";
				var payload = WebworksJson.Obj(await ApiClient.PostAsync(favorite ? "/UGC/Favorite" : "/UGC/Unfavorite", body));
				var ok = payload != null && WebworksJson.Str(payload, "error") == null;
				ResultBus.Complete<UserFavoriteItemsListChanged_t>(call, new UserFavoriteItemsListChanged_t
				{
					m_nPublishedFileID = id,
					m_eResult = ok ? EResult.k_EResultOK : EResult.k_EResultFail,
					m_bWasAddRequest = favorite,
				});
				return "";
			}, "favorite workshop item");
			return call;
		}

		public static SteamAPICall_t AddItemToFavorites(AppId_t nAppId, PublishedFileId_t nPublishedFileID) { return ToggleFavorite(nPublishedFileID, true); }
		public static SteamAPICall_t RemoveItemFromFavorites(AppId_t nAppId, PublishedFileId_t nPublishedFileID) { return ToggleFavorite(nPublishedFileID, false); }

		public static SteamAPICall_t SubscribeItem(PublishedFileId_t nPublishedFileID)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var body = "{\"fileId\":" + WebworksJson.Quote(nPublishedFileID.m_PublishedFileId.ToString(CultureInfo.InvariantCulture)) + "}";
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/UGC/Subscribe", body));
				var ok = payload != null && WebworksJson.Str(payload, "error") == null;
				if (ok)
				{
					WorkshopContent.SetSubscribed(nPublishedFileID.m_PublishedFileId, true);
					if (!_subscribed.Contains(nPublishedFileID.m_PublishedFileId)) _subscribed.Add(nPublishedFileID.m_PublishedFileId);
				}
				ResultBus.Complete<RemoteStorageSubscribePublishedFileResult_t>(call, new RemoteStorageSubscribePublishedFileResult_t
				{
					m_eResult = ok ? EResult.k_EResultOK : EResult.k_EResultFail,
					m_nPublishedFileID = nPublishedFileID,
				});
				return "";
			}, "SubscribeItem");
			return call;
		}

		public static SteamAPICall_t UnsubscribeItem(PublishedFileId_t nPublishedFileID)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var body = "{\"fileId\":" + WebworksJson.Quote(nPublishedFileID.m_PublishedFileId.ToString(CultureInfo.InvariantCulture)) + "}";
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/UGC/Unsubscribe", body));
				var ok = payload != null && WebworksJson.Str(payload, "error") == null;
				if (ok)
				{
					WorkshopContent.SetSubscribed(nPublishedFileID.m_PublishedFileId, false);
					_subscribed.Remove(nPublishedFileID.m_PublishedFileId);
				}
				ResultBus.Complete<RemoteStorageUnsubscribePublishedFileResult_t>(call, new RemoteStorageUnsubscribePublishedFileResult_t
				{
					m_eResult = ok ? EResult.k_EResultOK : EResult.k_EResultFail,
					m_nPublishedFileID = nPublishedFileID,
				});
				return "";
			}, "UnsubscribeItem");
			return call;
		}

		public static uint GetNumSubscribedItems(bool bIncludeLocallyDisabled = false)
		{
			WarmUp();
			return (uint)_subscribed.Count;
		}

		public static uint GetSubscribedItems(PublishedFileId_t[] pvecPublishedFileID, uint cMaxEntries, bool bIncludeLocallyDisabled = false)
		{
			WarmUp();
			if (pvecPublishedFileID == null) return 0;
			uint count = 0;
			for (int i = 0; i < _subscribed.Count && count < cMaxEntries && i < pvecPublishedFileID.Length; i++)
			{
				pvecPublishedFileID[count++] = new PublishedFileId_t(_subscribed[i]);
			}
			return count;
		}

		public static uint GetItemState(PublishedFileId_t nPublishedFileID)
		{
			uint state = 0;
			var id = nPublishedFileID.m_PublishedFileId;
			if (WorkshopContent.IsSubscribed(id) || _subscribed.Contains(id)) state |= 1;
			if (WorkshopContent.IsInstalled(id)) state |= 4;
			if (WorkshopContent.IsDownloading(id) || _downloading.Contains(id)) state |= 16;
			return state;
		}

		public static bool GetItemDownloadInfo(PublishedFileId_t nPublishedFileID, out ulong punBytesDownloaded, out ulong punBytesTotal)
		{
			punBytesDownloaded = 0;
			punBytesTotal = 0;
			if (!WorkshopContent.IsDownloading(nPublishedFileID.m_PublishedFileId)) return false;
			punBytesTotal = 1;
			return true;
		}

		public static bool DownloadItem(PublishedFileId_t nPublishedFileID, bool bHighPriority)
		{
			if (WorkshopContent.IsDownloading(nPublishedFileID.m_PublishedFileId)) return false;
			ApiClient.Fire(async () => { await DownloadAsync(nPublishedFileID); return ""; }, "DownloadItem");
			return true;
		}

		internal static async Task<bool> DownloadAsync(PublishedFileId_t id)
		{
			var fileId = id.m_PublishedFileId;
			WorkshopContent.BeginDownload(fileId);
			_downloading.Add(fileId);
			try
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/UGC/Download?id=" + fileId.ToString(CultureInfo.InvariantCulture)));
				var content = payload != null ? WebworksJson.Map(payload, "content") : null;
				var ok = content != null && WorkshopContent.WriteContent(fileId, content);
				var result = new DownloadItemResult_t
				{
					m_unAppID = new AppId_t(0),
					m_nPublishedFileID = id,
					m_eResult = ok ? EResult.k_EResultOK : EResult.k_EResultFail,
				};
				CallbackBus<DownloadItemResult_t>.Fire(result);
				if (ok) CallbackBus<ItemInstalled_t>.Fire(new ItemInstalled_t { m_unAppID = new AppId_t(0), m_nPublishedFileID = id });
				return ok;
			}
			finally
			{
				WorkshopContent.EndDownload(fileId);
				_downloading.Remove(fileId);
			}
		}

		public static bool GetItemInstallInfo(PublishedFileId_t nPublishedFileID, out ulong punSizeOnDisk, out string pchFolder, uint cchFolderSize, out uint punTimeStamp)
		{
			var fileId = nPublishedFileID.m_PublishedFileId;
			pchFolder = WorkshopContent.DirectoryFor(fileId);
			punSizeOnDisk = 0;
			punTimeStamp = 0;

			if (!WorkshopContent.IsInstalled(fileId)) return false;
			try
			{
				foreach (var file in Directory.GetFiles(pchFolder, "*", SearchOption.AllDirectories))
					punSizeOnDisk += (ulong)new FileInfo(file).Length;
			}
			catch {  }
			return true;
		}

		public static bool BInitWorkshopForGameServer(DepotId_t unWorkshopDepotID, string pszFolder) { return false; }
		public static void SuspendDownloads(bool bSuspend) { }

		public static SteamAPICall_t StartPlaytimeTracking(PublishedFileId_t[] pvecPublishedFileID, uint unNumPublishedFileIDs)
		{
			return ResultBus.NewCall();
		}

		public static SteamAPICall_t StopPlaytimeTracking(PublishedFileId_t[] pvecPublishedFileID, uint unNumPublishedFileIDs)
		{
			return ResultBus.NewCall();
		}

		public static SteamAPICall_t StopPlaytimeTrackingForAllItems() { return ResultBus.NewCall(); }

		static SteamAPICall_t Dependency(PublishedFileId_t parent, PublishedFileId_t child, bool add)
		{
			var call = ResultBus.NewCall();
			var payload = new SteamUGCDetails_t { m_nPublishedFileId = parent, m_eResult = add ? EResult.k_EResultOK : EResult.k_EResultOK };
			CallbackBus<SteamUGCRequestUGCDetailsResult_t>.Fire(new SteamUGCRequestUGCDetailsResult_t { m_details = payload });
			return call;
		}

		public static SteamAPICall_t AddDependency(PublishedFileId_t nParentPublishedFileID, PublishedFileId_t nChildPublishedFileID) { return Dependency(nParentPublishedFileID, nChildPublishedFileID, true); }
		public static SteamAPICall_t RemoveDependency(PublishedFileId_t nParentPublishedFileID, PublishedFileId_t nChildPublishedFileID) { return Dependency(nParentPublishedFileID, nChildPublishedFileID, false); }
		public static SteamAPICall_t AddAppDependency(PublishedFileId_t nPublishedFileID, AppId_t nAppID) { return Dependency(nPublishedFileID, new PublishedFileId_t(0), true); }
		public static SteamAPICall_t RemoveAppDependency(PublishedFileId_t nPublishedFileID, AppId_t nAppID) { return Dependency(nPublishedFileID, new PublishedFileId_t(0), false); }
		public static SteamAPICall_t GetAppDependencies(PublishedFileId_t nPublishedFileID) { return Dependency(nPublishedFileID, new PublishedFileId_t(0), true); }

		public static SteamAPICall_t DeleteItem(PublishedFileId_t nPublishedFileID)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var body = "{\"fileId\":" + WebworksJson.Quote(nPublishedFileID.m_PublishedFileId.ToString(CultureInfo.InvariantCulture)) + "}";
				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/UGC/Delete", body));
				var ok = payload != null && WebworksJson.Str(payload, "error") == null;
				if (ok)
				{
					_subscribed.Remove(nPublishedFileID.m_PublishedFileId);
					WorkshopContent.SetSubscribed(nPublishedFileID.m_PublishedFileId, false);
				}
				CallbackBus<SteamUGCRequestUGCDetailsResult_t>.Fire(new SteamUGCRequestUGCDetailsResult_t
				{
					m_details = new SteamUGCDetails_t
					{
						m_nPublishedFileId = nPublishedFileID,
						m_eResult = ok ? EResult.k_EResultOK : EResult.k_EResultFail,
					},
				});
				return "";
			}, "DeleteItem");
			return call;
		}

		public static bool ShowWorkshopEULA() { return true; }
		public static SteamAPICall_t GetWorkshopEULAStatus() { return ResultBus.NewCall(); }
		public static uint GetUserContentDescriptorPreferences(EUGCContentDescriptorID[] pvecDescriptors, uint cMaxEntries) { return 0; }
		public static bool SetItemsDisabledLocally(PublishedFileId_t[] pvecPublishedFileIDs, uint unNumPublishedFileIDs, bool bDisabledLocally) { return true; }
		public static bool SetSubscriptionsLoadOrder(PublishedFileId_t[] pvecPublishedFileIDs, uint unNumPublishedFileIDs) { return true; }
		public static bool MarkDownloadedItemAsUnused(PublishedFileId_t nPublishedFileID) { return false; }
		public static uint GetNumDownloadedItems() { return (uint)_subscribed.Count; }

		public static uint GetDownloadedItems(out PublishedFileId_t pvecPublishedFileIDs, uint cMaxEntries)
		{
			pvecPublishedFileIDs = new PublishedFileId_t(cMaxEntries > 0 && _subscribed.Count > 0 ? _subscribed[0] : 0);
			return _subscribed.Count > 0 && cMaxEntries > 0 ? 1u : 0u;
		}
	}
}
