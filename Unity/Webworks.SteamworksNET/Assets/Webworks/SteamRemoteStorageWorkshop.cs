using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Webworks.SNetInternal;

namespace Steamworks
{
	// The published-file half of ISteamRemoteStorage. Every call here lands on the same
	// /UGC routes SteamUGC uses, so the server has one workshop store no matter which
	// Steamworks interface (or which engine binding) published the item.
	public static partial class SteamRemoteStorage
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
			public int SubscriberCount;
			public int FavoriteCount;
		}

		const int MaxEnumeratedFiles = Constants.k_unEnumeratePublishedFilesMaxResults;

		static EResult ErrorFrom(string payload)
		{
			var body = WebworksJson.Obj(payload);
			// An empty body means the request never reached the server; reporting OK here
			// would leave callers believing a publish or a vote went through.
			if (body == null) return EResult.k_EResultNoConnection;
			var code = WebworksJson.Str(body, "error");
			switch (code)
			{
				case null:
				case "": return EResult.k_EResultOK;
				case "invalid_title": return EResult.k_EResultInvalidParam;
				case "quota_exceeded":
				case "too_many_items": return EResult.k_EResultLimitExceeded;
				case "not_owner": return EResult.k_EResultAccessDenied;
				case "not_found": return EResult.k_EResultFileNotFound;
				default: return EResult.k_EResultFail;
			}
		}

		static ItemView ParseItem(Dictionary<string, object> raw)
		{
			if (raw == null) return null;
			var item = new ItemView
			{
				Id = WebworksJson.Ulong(raw, "id"),
				Title = WebworksJson.Str(raw, "title") ?? "",
				Description = WebworksJson.Str(raw, "description") ?? "",
				OwnerId = WebworksJson.Ulong(raw, "ownerId"),
				OwnerName = WebworksJson.Str(raw, "ownerName") ?? "",
				Created = (long)WebworksJson.Num(raw, "created"),
				Updated = (long)WebworksJson.Num(raw, "updated"),
				VotesUp = WebworksJson.Int(raw, "votesUp"),
				VotesDown = WebworksJson.Int(raw, "votesDown"),
				Score = (float)WebworksJson.Num(raw, "score"),
				SizeBytes = (ulong)WebworksJson.Num(raw, "sizeBytes"),
				PreviewData = WebworksJson.Str(raw, "previewData"),
				PreviewName = WebworksJson.Str(raw, "previewName"),
				SubscriberCount = WebworksJson.Int(raw, "subscriberCount"),
				FavoriteCount = WebworksJson.Int(raw, "favoriteCount"),
			};

			var tags = WebworksJson.Arr(raw, "tags");
			if (tags != null)
			{
				var list = new List<string>();
				foreach (var tag in tags) if (tag is string) list.Add((string)tag);
				item.Tags = list.ToArray();
			}

			return item;
		}

		// Steam enumerates 50 ids per callback starting at an arbitrary index; the server
		// paginates at 50, so ask for the page that contains startIndex and fill from the
		// offset inside it.
		const int EnumerationPageSize = 50;

		static PublishedFileId_t[] Slice(ItemView[] items, int offset)
		{
			var ids = new PublishedFileId_t[MaxEnumeratedFiles];
			for (int i = 0; i < MaxEnumeratedFiles; i++)
			{
				var index = offset + i;
				ids[i] = index >= 0 && index < items.Length
					? new PublishedFileId_t(items[index].Id)
					: new PublishedFileId_t(0);
			}
			return ids;
		}

		static int Filled(ItemView[] items, int offset)
		{
			return Math.Min(MaxEnumeratedFiles, Math.Max(0, items.Length - offset));
		}

		static void Enumerate(string type, string sort, CSteamID userId, uint startIndex, Action<ItemView[], int, int> done)
		{
			var offset = (int)(startIndex % EnumerationPageSize);
			var page = (int)(startIndex / EnumerationPageSize) + 1;

			ApiClient.Fire(async () =>
			{
				var body = new StringBuilder("{\"page\":" + page + ",\"perPage\":" + EnumerationPageSize);
				body.Append(",\"type\":").Append(WebworksJson.Quote(type));
				if (!string.IsNullOrEmpty(sort)) body.Append(",\"sort\":").Append(WebworksJson.Quote(sort));
				if (userId.m_SteamID != 0) body.Append(",\"userId\":").Append(WebworksJson.Quote(userId.m_SteamID.ToString()));
				body.Append("}");

				var payload = WebworksJson.Obj(await ApiClient.PostAsync("/UGC/Query", body.ToString()));
				var list = new List<ItemView>();
				var entries = WebworksJson.Arr(payload, "entries");
				if (entries != null)
				{
					foreach (var raw in entries)
					{
						var item = ParseItem(raw as Dictionary<string, object>);
						if (item != null) list.Add(item);
					}
				}
				done(list.ToArray(), offset, WebworksJson.Int(payload, "totalCount"));
				return "";
			}, "RemoteStorageEnumerate");
		}

		static string ReadBase64(string path)
		{
			if (string.IsNullOrEmpty(path)) return null;
			try { return Convert.ToBase64String(File.ReadAllBytes(path)); }
			catch (Exception ex)
			{
				UnityEngine.Debug.LogWarning("[Webworks] could not read '" + path + "': " + ex.Message);
				return null;
			}
		}

		static string PickName(string path)
		{
			if (string.IsNullOrEmpty(path)) return null;
			try { return Path.GetFileName(path); }
			catch { return path; }
		}

		static string PublishBody(string pchFile, string pchPreviewFile, string title, string description, IList<string> tags, string changelog)
		{
			var body = new StringBuilder("{\"title\":").Append(WebworksJson.Quote(title));
			body.Append(",\"description\":").Append(WebworksJson.Quote(description ?? ""));

			body.Append(",\"tags\":[");
			if (tags != null)
			{
				var first = true;
				foreach (var tag in tags)
				{
					if (string.IsNullOrEmpty(tag)) continue;
					body.Append(first ? "" : ",").Append(WebworksJson.Quote(tag));
					first = false;
				}
			}
			body.Append("]");

			if (!string.IsNullOrEmpty(changelog)) body.Append(",\"changelog\":").Append(WebworksJson.Quote(changelog));

			var preview = ReadBase64(pchPreviewFile);
			if (preview != null)
			{
				body.Append(",\"previewName\":").Append(WebworksJson.Quote(PickName(pchPreviewFile) ?? "preview.png"));
				body.Append(",\"previewData\":").Append(WebworksJson.Quote(preview));
			}

			body.Append(",\"content\":{");
			var data = ReadBase64(pchFile);
			if (data != null)
			{
				body.Append(WebworksJson.Quote(PickName(pchFile) ?? "content.bin"));
				body.Append(":").Append(WebworksJson.Quote(data));
			}
			body.Append("}}");
			return body.ToString();
		}

		static SteamAPICall_t CompleteUpdate(SteamAPICall_t call, PublishedFileId_t fileId, string payload)
		{
			var result = ErrorFrom(payload);
			var body = WebworksJson.Obj(payload);
			var returned = WebworksJson.Ulong(body, "fileId");
			ResultBus.Complete<RemoteStorageUpdatePublishedFileResult_t>(call, new RemoteStorageUpdatePublishedFileResult_t
			{
				m_eResult = result,
				m_nPublishedFileId = returned != 0 ? new PublishedFileId_t(returned) : fileId,
				m_bUserNeedsToAcceptWorkshopLegalAgreement = false,
			}, result == EResult.k_EResultNoConnection);
			return call;
		}

		public static SteamAPICall_t PublishWorkshopFile(string pchFile, string pchPreviewFile, AppId_t nConsumerAppId, string pchTitle, string pchDescription, ERemoteStoragePublishedFileVisibility eVisibility, IList<string> pTags, EWorkshopFileType eWorkshopFileType)
		{
			var call = ResultBus.NewCall();
			if (string.IsNullOrEmpty(pchTitle))
			{
				ResultBus.Complete<RemoteStorageUpdatePublishedFileResult_t>(call, new RemoteStorageUpdatePublishedFileResult_t
				{
					m_eResult = EResult.k_EResultInvalidParam,
					m_nPublishedFileId = new PublishedFileId_t(0),
				});
				return call;
			}

			ApiClient.Fire(async () =>
			{
				var body = PublishBody(pchFile, pchPreviewFile, pchTitle, pchDescription, pTags, null);
				return CompleteUpdate(call, new PublishedFileId_t(0), await ApiClient.PostAsync("/UGC/Publish", body)).m_SteamAPICall.ToString();
			}, "PublishWorkshopFile");
			return call;
		}

		public static SteamAPICall_t CommitPublishedFileUpdate(PublishedFileUpdateHandle_t updateHandle)
		{
			var call = ResultBus.NewCall();
			Update update;
			if (!_updates.TryGetValue(updateHandle.m_PublishedFileUpdateHandle, out update))
			{
				ResultBus.Complete<RemoteStorageUpdatePublishedFileResult_t>(call, new RemoteStorageUpdatePublishedFileResult_t
				{
					m_eResult = EResult.k_EResultInvalidParam,
					m_nPublishedFileId = new PublishedFileId_t(0),
				});
				return call;
			}

			_updates.Remove(updateHandle.m_PublishedFileUpdateHandle);
			var fileId = new PublishedFileId_t(update.FileId);

			ApiClient.Fire(async () =>
			{
				var body = new StringBuilder("{\"fileId\":").Append(update.FileId);
				if (update.Title != null) body.Append(",\"title\":").Append(WebworksJson.Quote(update.Title));
				if (update.Description != null) body.Append(",\"description\":").Append(WebworksJson.Quote(update.Description));
				if (update.ChangeDescription != null) body.Append(",\"changelog\":").Append(WebworksJson.Quote(update.ChangeDescription));
				if (update.Tags != null)
				{
					body.Append(",\"tags\":[");
					for (int i = 0; i < update.Tags.Count; i++)
						body.Append(i > 0 ? "," : "").Append(WebworksJson.Quote(update.Tags[i]));
					body.Append("]");
				}

				var preview = ReadBase64(update.Preview);
				if (preview != null)
				{
					body.Append(",\"previewName\":").Append(WebworksJson.Quote(PickName(update.Preview) ?? "preview.png"));
					body.Append(",\"previewData\":").Append(WebworksJson.Quote(preview));
				}

				var data = ReadBase64(update.File);
				if (data != null)
				{
					body.Append(",\"content\":{").Append(WebworksJson.Quote(PickName(update.File) ?? "content.bin"));
					body.Append(":").Append(WebworksJson.Quote(data)).Append("}");
				}
				body.Append("}");

				return CompleteUpdate(call, fileId, await ApiClient.PostAsync("/UGC/Update", body.ToString())).m_SteamAPICall.ToString();
			}, "CommitPublishedFileUpdate");
			return call;
		}

		public static SteamAPICall_t DeletePublishedFile(PublishedFileId_t unPublishedFileId)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var body = "{\"fileId\":" + unPublishedFileId.m_PublishedFileId + "}";
				var payload = await ApiClient.PostAsync("/UGC/Delete", body);
				ResultBus.Complete<RemoteStorageDeletePublishedFileResult_t>(call, new RemoteStorageDeletePublishedFileResult_t
				{
					m_eResult = ErrorFrom(payload),
					m_nPublishedFileId = unPublishedFileId,
				});
				return "";
			}, "DeletePublishedFile");
			return call;
		}

		public static SteamAPICall_t GetPublishedFileDetails(PublishedFileId_t unPublishedFileId, uint unMaxSecondsOld)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/UGC/Item?id=" + unPublishedFileId.m_PublishedFileId));
				var item = WebworksJson.Bool(payload, "found") ? ParseItem(WebworksJson.Map(payload, "item")) : null;
				if (item == null)
				{
					ResultBus.Complete<RemoteStorageGetPublishedFileDetailsResult_t>(call, new RemoteStorageGetPublishedFileDetailsResult_t
					{
						m_eResult = EResult.k_EResultFileNotFound,
						m_nPublishedFileId = unPublishedFileId,
					});
					return "";
				}

				uint appId;
				uint.TryParse(SessionState.AppId, out appId);
				var details = new RemoteStorageGetPublishedFileDetailsResult_t
				{
					m_eResult = EResult.k_EResultOK,
					m_nPublishedFileId = new PublishedFileId_t(item.Id),
					m_nCreatorAppID = new AppId_t(appId),
					m_nConsumerAppID = new AppId_t(appId),
					m_rgchTitle = item.Title,
					m_rgchDescription = item.Description,
					m_ulSteamIDOwner = item.OwnerId,
					m_rtimeCreated = (uint)(item.Created / 1000),
					m_rtimeUpdated = (uint)(item.Updated / 1000),
					m_eVisibility = ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic,
					m_bBanned = false,
					m_bAcceptedForUse = true,
					m_bTagsTruncated = false,
					m_rgchTags = string.Join(",", item.Tags),
					m_hFile = new UGCHandle_t(item.Id),
					m_hPreviewFile = new UGCHandle_t(item.Id + 1),
					m_pchFileName = item.PreviewName ?? "",
					m_nFileSize = (int)Math.Min(int.MaxValue, item.SizeBytes),
					m_nPreviewFileSize = 0,
					m_rgchURL = "",
					m_eFileType = EWorkshopFileType.k_EWorkshopFileTypeCommunity,
				};
				RegisterUGC(details.m_hFile.m_UGCHandle, null, details.m_pchFileName, item.OwnerId);
				ResultBus.Complete<RemoteStorageGetPublishedFileDetailsResult_t>(call, details);
				return "";
			}, "GetPublishedFileDetails");
			return call;
		}

		public static SteamAPICall_t EnumerateUserPublishedFiles(uint unStartIndex)
		{
			var call = ResultBus.NewCall();
			Enumerate("published", "creationdate", CSteamID.Nil, unStartIndex, (items, offset, total) =>
			{
				ResultBus.Complete<RemoteStorageEnumerateUserPublishedFilesResult_t>(call, new RemoteStorageEnumerateUserPublishedFilesResult_t
				{
					m_eResult = EResult.k_EResultOK,
					m_nResultsReturned = Filled(items, offset),
					m_nTotalResultCount = total,
					m_rgPublishedFileId = Slice(items, offset),
				});
			});
			return call;
		}

		public static SteamAPICall_t EnumerateUserSubscribedFiles(uint unStartIndex)
		{
			var call = ResultBus.NewCall();
			Enumerate("subscribed", "creationdate", CSteamID.Nil, unStartIndex, (items, offset, total) =>
			{
				var times = new uint[MaxEnumeratedFiles];
				for (int i = 0; i < times.Length; i++)
				{
					var index = offset + i;
					times[i] = index < items.Length ? (uint)(items[index].Updated / 1000) : 0;
				}

				ResultBus.Complete<RemoteStorageEnumerateUserSubscribedFilesResult_t>(call, new RemoteStorageEnumerateUserSubscribedFilesResult_t
				{
					m_eResult = EResult.k_EResultOK,
					m_nResultsReturned = Filled(items, offset),
					m_nTotalResultCount = total,
					m_rgPublishedFileId = Slice(items, offset),
					m_rgRTimeSubscribed = times,
				});
			});
			return call;
		}

		public static SteamAPICall_t EnumerateUserSharedWorkshopFiles(CSteamID steamId, uint unStartIndex, IList<string> pRequiredTags, IList<string> pExcludedTags)
		{
			var call = ResultBus.NewCall();
			Enumerate("published", "creationdate", steamId, unStartIndex, (items, offset, total) =>
			{
				ResultBus.Complete<RemoteStorageEnumerateUserSharedWorkshopFilesResult_t>(call, new RemoteStorageEnumerateUserSharedWorkshopFilesResult_t
				{
					m_eResult = EResult.k_EResultOK,
					m_nResultsReturned = Filled(items, offset),
					m_nTotalResultCount = total,
					m_rgPublishedFileId = Slice(items, offset),
				});
			});
			return call;
		}

		public static SteamAPICall_t EnumeratePublishedFilesByUserAction(EWorkshopFileAction eAction, uint unStartIndex)
		{
			var call = ResultBus.NewCall();
			Enumerate("favorited", "creationdate", CSteamID.Nil, unStartIndex, (items, offset, total) =>
			{
				var times = new uint[MaxEnumeratedFiles];
				for (int i = 0; i < times.Length; i++)
				{
					var index = offset + i;
					times[i] = index < items.Length ? (uint)(items[index].Updated / 1000) : 0;
				}

				ResultBus.Complete<RemoteStorageEnumeratePublishedFilesByUserActionResult_t>(call, new RemoteStorageEnumeratePublishedFilesByUserActionResult_t
				{
					m_eResult = EResult.k_EResultOK,
					m_eAction = eAction,
					m_nResultsReturned = Filled(items, offset),
					m_nTotalResultCount = total,
					m_rgPublishedFileId = Slice(items, offset),
					m_rgRTimeUpdated = times,
				});
			});
			return call;
		}

		static SteamAPICall_t ToggleSubscription(PublishedFileId_t fileId, bool subscribe, string route)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var body = "{\"fileId\":" + fileId.m_PublishedFileId + "}";
				var payload = await ApiClient.PostAsync(route, body);
				var result = ErrorFrom(payload);
				if (result == EResult.k_EResultOK) SetSubscribed(fileId.m_PublishedFileId, subscribe);
				ResultBus.Complete<RemoteStorageSubscribePublishedFileResult_t>(call, new RemoteStorageSubscribePublishedFileResult_t
				{
					m_eResult = result,
					m_nPublishedFileID = fileId,
				});
				return "";
			}, route);
			return call;
		}

		// The server answers subscribe/unsubscribe with the same shape, so both callbacks
		// share the RemoteStorageSubscribePublishedFileResult_t layout.
		static SteamAPICall_t ToggleUnsubscription(PublishedFileId_t fileId)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var body = "{\"fileId\":" + fileId.m_PublishedFileId + "}";
				var result = ErrorFrom(await ApiClient.PostAsync("/UGC/Unsubscribe", body));
				if (result == EResult.k_EResultOK) SetSubscribed(fileId.m_PublishedFileId, false);
				ResultBus.Complete<RemoteStorageUnsubscribePublishedFileResult_t>(call, new RemoteStorageUnsubscribePublishedFileResult_t
				{
					m_eResult = result,
					m_nPublishedFileID = fileId,
				});
				return "";
			}, "UnsubscribePublishedFile");
			return call;
		}

		public static SteamAPICall_t SubscribePublishedFile(PublishedFileId_t unPublishedFileId)
		{
			return ToggleSubscription(unPublishedFileId, true, "/UGC/Subscribe");
		}

		public static SteamAPICall_t UnsubscribePublishedFile(PublishedFileId_t unPublishedFileId)
		{
			return ToggleUnsubscription(unPublishedFileId);
		}

		static void SetSubscribed(ulong fileId, bool subscribed)
		{
			WorkshopContent.SetSubscribed(fileId, subscribed);
		}

		public static SteamAPICall_t UpdateUserPublishedItemVote(PublishedFileId_t unPublishedFileId, bool bVoteUp)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var body = "{\"fileId\":" + unPublishedFileId.m_PublishedFileId + ",\"up\":" + (bVoteUp ? "true" : "false") + "}";
				var payload = await ApiClient.PostAsync("/UGC/Vote", body);
				ResultBus.Complete<RemoteStorageUpdateUserPublishedItemVoteResult_t>(call, new RemoteStorageUpdateUserPublishedItemVoteResult_t
				{
					m_eResult = ErrorFrom(payload),
					m_nPublishedFileId = unPublishedFileId,
				});
				return "";
			}, "UpdateUserPublishedItemVote");
			return call;
		}

		public static SteamAPICall_t GetUserPublishedItemVoteDetails(PublishedFileId_t unPublishedFileId)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/UGC/Vote?fileId=" + unPublishedFileId.m_PublishedFileId));
				ResultBus.Complete<RemoteStorageGetPublishedItemVoteDetailsResult_t>(call, new RemoteStorageGetPublishedItemVoteDetailsResult_t
				{
					m_eResult = payload == null ? EResult.k_EResultNoConnection : EResult.k_EResultOK,
					m_unPublishedFileId = unPublishedFileId,
					m_nVotesFor = WebworksJson.Bool(payload, "votedUp") ? 1 : 0,
					m_nVotesAgainst = WebworksJson.Bool(payload, "votedDown") ? 1 : 0,
					m_nReports = 0,
					m_fScore = 0,
				});
				return "";
			}, "GetUserPublishedItemVoteDetails");
			return call;
		}

		public static SteamAPICall_t GetPublishedItemVoteDetails(PublishedFileId_t unPublishedFileId)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/UGC/Item?id=" + unPublishedFileId.m_PublishedFileId));
				var item = WebworksJson.Bool(payload, "found") ? ParseItem(WebworksJson.Map(payload, "item")) : null;
				var total = item == null ? 0 : item.VotesUp + item.VotesDown;
				ResultBus.Complete<RemoteStorageGetPublishedItemVoteDetailsResult_t>(call, new RemoteStorageGetPublishedItemVoteDetailsResult_t
				{
					m_eResult = item == null ? EResult.k_EResultFileNotFound : EResult.k_EResultOK,
					m_unPublishedFileId = unPublishedFileId,
					m_nVotesFor = item == null ? 0 : item.VotesUp,
					m_nVotesAgainst = item == null ? 0 : item.VotesDown,
					m_nReports = 0,
					m_fScore = total > 0 ? (float)item.VotesUp / total : 0f,
				});
				return "";
			}, "GetPublishedItemVoteDetails");
			return call;
		}
	}
}
