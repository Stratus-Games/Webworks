using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Steamworks.Data;
using Webworks.Internal;

namespace Steamworks.Ugc
{
	public struct UserItemVote { public bool VotedUp; public bool VotedDown; public bool VoteSkipped; }

	public struct PublishResult
	{
		public bool Success;
		public bool NeedsWorkshopAgreement;
		public Result Result;
		public PublishedFileId FileId;
	}

	public struct Item : IEquatable<Item>
	{
		public PublishedFileId Id;
		public string Title;
		public string Description;
		public string PreviewImageUrl;
		public string[] Tags;
		public Friend Owner;
		public DateTime Created;
		public DateTime Updated;
		public float Score;
		public uint VotesUp;
		public uint VotesDown;
		public ulong SizeBytes;

		public string Url { get { return ApiClient.FullUrl("/UGC/Item?id=" + Id.Value); } }

		public string Directory { get { return WorkshopContent.DirectoryFor(Id.Value); } }

		public bool IsInstalled { get { return WorkshopContent.IsInstalled(Id.Value); } }
		public bool IsSubscribed { get { return WorkshopContent.IsSubscribed(Id.Value); } }
		public bool IsDownloading { get { return WorkshopContent.IsDownloading(Id.Value); } }
		public bool IsDownloadPending { get { return false; } }
		public bool NeedsUpdate { get { return false; } }
		public bool IsPublic { get { return true; } }
		public bool IsFriendsOnly { get { return false; } }
		public bool IsPrivate { get { return false; } }
		public bool IsBanned { get { return false; } }
		public bool IsAcceptedForUse { get { return true; } }

		public static Task<Item?> GetAsync(PublishedFileId id, int maxageseconds = 1800) { return SteamUGC.QueryFileAsync(id); }

		public bool HasTag(string tag) { return Tags != null && Array.IndexOf(Tags, tag) >= 0; }

		public bool Download(bool highPriority = false) { SteamUGC.Download(Id, highPriority); return true; }
		public Task<bool> DownloadAsync(Action<float> progress = null, int milisecondsUpdateDelay = 60) { return SteamUGC.DownloadAsync(Id, progress, milisecondsUpdateDelay); }

		public async Task<bool> Subscribe() { return await Workshop.SubscribeAsync(Id.Value, true); }
		public async Task<bool> Unsubscribe() { return await Workshop.SubscribeAsync(Id.Value, false); }
		public async Task<bool> AddFavorite() { return await Workshop.FavoriteAsync(Id.Value, true); }
		public async Task<bool> RemoveFavorite() { return await Workshop.FavoriteAsync(Id.Value, false); }

		public async Task<Result?> Vote(bool up)
		{
			var d = WebworksJson.Obj(await ApiClient.PostAsync("/UGC/Vote", "{\"fileId\":" + WebworksJson.Quote(Id.Value.ToString()) + ",\"up\":" + (up ? "true" : "false") + "}"));
			return d != null && WebworksJson.Str(d, "error") == null ? Result.OK : (Result?)Result.Fail;
		}

		public async Task<UserItemVote?> GetUserVote()
		{
			var d = WebworksJson.Obj(await ApiClient.GetAsync("/UGC/Vote?fileId=" + Id.Value));
			if (d == null) return null;
			return new UserItemVote { VotedUp = WebworksJson.Bool(d, "votedUp"), VotedDown = WebworksJson.Bool(d, "votedDown"), VoteSkipped = false };
		}

		public Editor Edit() { return new Editor(Id); }

		public override string ToString() { return Title; }
		public override int GetHashCode() { return Id.Value.GetHashCode(); }
		public override bool Equals(object obj) { return obj is Item && ((Item)obj).Id.Value == Id.Value; }
		public bool Equals(Item other) { return Id.Value == other.Id.Value; }
		public static bool operator ==(Item a, Item b) { return a.Id.Value == b.Id.Value; }
		public static bool operator !=(Item a, Item b) { return a.Id.Value != b.Id.Value; }
	}

	public struct ResultPage : IDisposable
	{
		internal Item[] _entries;
		internal int _totalCount;

		public int ResultCount { get { return _entries != null ? _entries.Length : 0; } }
		public int TotalCount { get { return _totalCount; } }
		public bool CachedData { get { return false; } }
		public IEnumerable<Item> Entries { get { return _entries ?? new Item[0]; } }
		public void Dispose() { }
	}

	public class Query
	{
		internal string _searchText;
		internal readonly List<string> _tags = new List<string>();
		internal bool _matchAllTags;
		internal string _sort = "creationdate";
		internal string _type;

		public static Query All { get { return new Query(); } }
		public static Query AllGuides { get { return new Query(); } }
		public static Query Artwork { get { return new Query(); } }
		public static Query Collections { get { return new Query(); } }
		public static Query ControllerBindings { get { return new Query(); } }
		public static Query GameManagedItems { get { return new Query(); } }
		public static Query IntegratedGuides { get { return new Query(); } }
		public static Query Items { get { return new Query(); } }
		public static Query ItemsMtx { get { return new Query(); } }
		public static Query ItemsReadyToUse { get { return new Query(); } }
		public static Query Screenshots { get { return new Query(); } }
		public static Query UsableInGame { get { return new Query(); } }
		public static Query Videos { get { return new Query(); } }
		public static Query WebGuides { get { return new Query(); } }

		public Query AddRequiredKeyValueTag(string key, string value) { return WithKeyValueTag(key, value); }
		public Query AllowCachedResponse(int maxSecondsAge) { return this; }
		public Query InLanguage(string language) { return this; }
		public Query WhereSearchText(string searchText) { _searchText = searchText; return this; }
		public Query WithAdditionalPreviews(bool b = true) { return this; }
		public Query WithChildren(bool b = true) { return this; }
		public Query WithFileId(params PublishedFileId[] files) { return this; }
		public Query WithKeyValueTag(string key, string value) { return WithTag(key + ":" + value); }
		public Query WithLongDescription(bool b = true) { return this; }
		public Query WithMetadata(bool b = true) { return this; }
		public Query WithOnlyIDs(bool b = true) { return this; }
		public Query WithPlaytimeStats(uint days) { return this; }
		public Query WithTag(string tag) { if (!string.IsNullOrEmpty(tag)) _tags.Add(tag); return this; }
		public Query WithoutTag(string tag) { return this; }
		public Query WithTotalOnly(bool b = true) { return this; }
		public Query WithTrendDays(int days) { return this; }
		public Query WithType(UgcType type) { return this; }
		public Query MatchAllTags() { _matchAllTags = true; return this; }
		public Query MatchAnyTag() { _matchAllTags = false; return this; }
		public Query NotYetRated() { return this; }

		public Query RankedByAcceptanceDate() { return SortBy("creationdate"); }
		public Query RankedByAveragePlaytimeTrend() { return this; }
		public Query RankedByLifetimeAveragePlaytime() { return this; }
		public Query RankedByLifetimePlaytimeSessions() { return this; }
		public Query RankedByNumTimesReported() { return this; }
		public Query RankedByPlaytimeSessionsTrend() { return this; }
		public Query RankedByPlaytimeTrend() { return this; }
		public Query RankedByPublicationDate() { return SortBy("creationdate"); }
		public Query RankedByTextSearch() { return this; }
		public Query RankedByTotalPlaytime() { return this; }
		public Query RankedByTotalUniqueSubscriptions() { return SortBy("subscriptions"); }
		public Query RankedByTotalVotesAsc() { return SortBy("vote"); }
		public Query RankedByTrend() { return SortBy("vote"); }
		public Query RankedByVote() { return SortBy("vote"); }
		public Query RankedByVotesUp() { return SortBy("vote"); }
		public Query SortByCreationDate() { return SortBy("creationdate"); }
		public Query SortByCreationDateAsc() { return SortBy("creationdateasc"); }
		public Query SortByModeration() { return this; }
		public Query SortBySubscriptionDate() { return SortBy("subscriptions"); }
		public Query SortByTitleAsc() { return SortBy("title"); }
		public Query SortByUpdateDate() { return SortBy("updatedate"); }
		public Query SortByVoteScore() { return SortBy("vote"); }

		public Query CreatedByFollowedUsers() { return this; }
		public Query CreatedByFriends() { return this; }
		public Query FavoritedByFriends() { return this; }
		public Query WhereUserFavorited(SteamId userid = default(SteamId)) { return AsUserQuery("favorited", userid); }
		public Query WhereUserFollowed(SteamId userid = default(SteamId)) { return this; }
		public Query WhereUserPublished(SteamId userid = default(SteamId)) { return AsUserQuery("published", userid); }
		public Query WhereUserSubscribed(SteamId userid = default(SteamId)) { return AsUserQuery("subscribed", userid); }
		public Query WhereUserUsedOrPlayed(SteamId userid = default(SteamId)) { return this; }
		public Query WhereUserVotedDown(SteamId userid = default(SteamId)) { return AsUserQuery("voteddown", userid); }
		public Query WhereUserVotedOn(SteamId userid = default(SteamId)) { return AsUserQuery("votedon", userid); }
		public Query WhereUserVotedUp(SteamId userid = default(SteamId)) { return AsUserQuery("votedup", userid); }
		public Query WhereUserWillVoteLater(SteamId userid = default(SteamId)) { return this; }

		internal Query SortBy(string sort) { _sort = sort; return this; }
		internal SteamId _forUser;
		internal Query AsUserQuery(string type, SteamId userid) { _type = type; _forUser = userid; return this; }

		public Task<ResultPage?> GetPageAsync(int page) { return Workshop.QueryAsync(this, page); }
	}

	public class UserQuery : Query
	{
		UserQuery(string type) { _type = type; }

		public static new UserQuery All { get { return new UserQuery(null); } }
		public static UserQuery Published { get { return new UserQuery("published"); } }
		public static UserQuery VotedOn { get { return new UserQuery("votedon"); } }
		public static UserQuery VotedUp { get { return new UserQuery("votedup"); } }
		public static UserQuery VotedDown { get { return new UserQuery("voteddown"); } }
		public static UserQuery WillVoteLater { get { return new UserQuery(null); } }
		public static UserQuery Favorited { get { return new UserQuery("favorited"); } }
		public static UserQuery Subscribed { get { return new UserQuery("subscribed"); } }
		public static UserQuery UsedOrPlayed { get { return new UserQuery(null); } }
		public static UserQuery Followed { get { return new UserQuery(null); } }
	}

	public class Editor
	{
		PublishedFileId? _existingId;
		string _title, _description, _changeLog, _contentFolder, _previewFile;
		readonly List<string> _tags = new List<string>();

		public static Editor NewCommunityFile { get { return new Editor(); } }
		public static Editor NewMicrotransactionFile { get { return new Editor(); } }

		public Editor() { }
		public Editor(PublishedFileId existingId) { _existingId = existingId; }

		public Editor ForAppId(AppId id) { return this; }
		public Editor WithTitle(string title) { _title = title; return this; }
		public Editor WithDescription(string description) { _description = description; return this; }
		public Editor WithChangeLog(string log) { _changeLog = log; return this; }
		public Editor WithTag(string tag) { if (!string.IsNullOrEmpty(tag)) _tags.Add(tag); return this; }
		public Editor AddKeyValueTag(string key, string value) { _tags.Add(key + ":" + value); return this; }
		public Editor WithMetaData(string metadata) { return this; }
		public Editor InLanguage(string language) { return this; }
		public Editor WithContent(string folderName) { _contentFolder = folderName; return this; }
		public Editor WithPreviewFile(string file) { _previewFile = file; return this; }

		public Editor WithPublicVisibility() { return this; }
		public Editor WithFriendsOnlyVisibility() { return this; }
		public Editor WithPrivateVisibility() { return this; }

		public async Task<PublishResult> SubmitAsync(IProgress<float> progress = null)
		{
			var content = WorkshopContent.ReadFolder(_contentFolder);
			byte[] preview = null;
			string previewName = null;
			if (!string.IsNullOrEmpty(_previewFile))
			{
				var files = WorkshopContent.ReadFolder(System.IO.Path.GetDirectoryName(_previewFile));
				var name = System.IO.Path.GetFileName(_previewFile);
				byte[] data;
				if (files.TryGetValue(name, out data)) { preview = data; previewName = name; }
			}

			var body = new StringBuilder("{");
			if (_existingId.HasValue) body.Append("\"fileId\":").Append(WebworksJson.Quote(_existingId.Value.Value.ToString())).Append(",");
			if (_title != null) body.Append("\"title\":").Append(WebworksJson.Quote(_title)).Append(",");
			if (_description != null) body.Append("\"description\":").Append(WebworksJson.Quote(_description)).Append(",");
			if (_changeLog != null) body.Append("\"changelog\":").Append(WebworksJson.Quote(_changeLog)).Append(",");
			if (previewName != null) body.Append("\"previewName\":").Append(WebworksJson.Quote(previewName)).Append(",\"previewData\":").Append(WebworksJson.Quote(Convert.ToBase64String(preview))).Append(",");
			body.Append("\"tags\":[");
			for (int i = 0; i < _tags.Count; i++) body.Append(i > 0 ? "," : "").Append(WebworksJson.Quote(_tags[i]));
			body.Append("],\"content\":{");
			int ci = 0;
			foreach (var kv in content)
			{
				body.Append(ci++ > 0 ? "," : "").Append(WebworksJson.Quote(kv.Key)).Append(":").Append(WebworksJson.Quote(Convert.ToBase64String(kv.Value)));
			}
			body.Append("}}");

			progress?.Report(0.5f);
			var path = _existingId.HasValue ? "/UGC/Update" : "/UGC/Publish";
			var d = WebworksJson.Obj(await ApiClient.PostAsync(path, body.ToString()));
			progress?.Report(1f);

			var error = d != null ? WebworksJson.Str(d, "error") : "no_response";
			if (error != null) return new PublishResult { Success = false, Result = Result.Fail };

			var idStr = WebworksJson.Str(d, "fileId") ?? (_existingId.HasValue ? _existingId.Value.Value.ToString() : null);
			ulong id;
			ulong.TryParse(idStr, out id);
			return new PublishResult { Success = true, Result = Result.OK, FileId = id };
		}
	}
}

namespace Steamworks
{
	using Steamworks.Ugc;

	public static class SteamUGC
	{
		public static event Action<Result> OnDownloadItemResult;

		public static bool Download(PublishedFileId fileId, bool highPriority = false)
		{
			ApiClient.Fire(async () => { await DownloadAsync(fileId); return ""; }, "Workshop download of " + fileId.Value);
			return true;
		}

		public static async Task<bool> DownloadAsync(PublishedFileId fileId, Action<float> progress = null, int milisecondsUpdateDelay = 60)
		{
			WorkshopContent.BeginDownload(fileId.Value);
			try
			{
				progress?.Invoke(0.25f);
				var d = WebworksJson.Obj(await ApiClient.GetAsync("/UGC/Download?id=" + fileId.Value));
				progress?.Invoke(0.75f);
				var content = d != null ? WebworksJson.Map(d, "content") : null;
				bool ok = content != null && WorkshopContent.WriteContent(fileId.Value, content);
				progress?.Invoke(1f);
				OnDownloadItemResult?.Invoke(ok ? Result.OK : Result.Fail);
				return ok;
			}
			finally
			{
				WorkshopContent.EndDownload(fileId.Value);
			}
		}

		public static async Task<Item?> QueryFileAsync(PublishedFileId fileId)
		{
			var d = WebworksJson.Obj(await ApiClient.GetAsync("/UGC/Item?id=" + fileId.Value));
			if (d == null || !WebworksJson.Bool(d, "found")) return null;
			return Workshop.ItemFromJson(WebworksJson.Map(d, "item"));
		}

		public static async Task<bool> DeleteFileAsync(PublishedFileId fileId)
		{
			var d = WebworksJson.Obj(await ApiClient.PostAsync("/UGC/Delete", "{\"fileId\":" + WebworksJson.Quote(fileId.Value.ToString()) + "}"));
			return d != null && WebworksJson.Str(d, "error") == null;
		}

		public static bool StartPlaytimeTracking(params PublishedFileId[] fileIds) { return true; }
		public static bool StopPlaytimeTracking(params PublishedFileId[] fileIds) { return true; }
		public static bool StopPlaytimeTrackingForAllItems() { return true; }
	}

	internal static class Workshop
	{
		public static Item ItemFromJson(System.Collections.Generic.Dictionary<string, object> o)
		{
			if (o == null) return default(Item);
			ulong ownerId = WebworksJson.Ulong(o, "ownerId");
			PlayerDirectory.Set(ownerId, WebworksJson.Str(o, "ownerName"));

			var tagsList = new System.Collections.Generic.List<string>();
			var rawTags = WebworksJson.Arr(o, "tags");
			if (rawTags != null) foreach (var t in rawTags) if (t is string) tagsList.Add((string)t);

			ulong id = WebworksJson.Ulong(o, "id");
			bool subscribed = false;
			if (WorkshopContent.IsSubscribed(id)) subscribed = true;
			if (!subscribed) subscribed = false;

			return new Item
			{
				Id = id,
				Title = WebworksJson.Str(o, "title") ?? "",
				Description = WebworksJson.Str(o, "description") ?? "",
				PreviewImageUrl = BuildPreviewUrl(o),
				Tags = tagsList.ToArray(),
				Owner = new Friend(ownerId),
				Created = Epoch.AddMilliseconds(WebworksJson.Num(o, "created")),
				Updated = Epoch.AddMilliseconds(WebworksJson.Num(o, "updated")),
				VotesUp = (uint)WebworksJson.Int(o, "votesUp"),
				VotesDown = (uint)WebworksJson.Int(o, "votesDown"),
				Score = (float)WebworksJson.Num(o, "score"),
				SizeBytes = (ulong)WebworksJson.Num(o, "sizeBytes"),
			};
		}

		static string BuildPreviewUrl(System.Collections.Generic.Dictionary<string, object> o)
		{
			var data = WebworksJson.Str(o, "previewData");
			if (string.IsNullOrEmpty(data)) return null;
			var name = WebworksJson.Str(o, "previewName") ?? "";
			var mime = name.EndsWith(".png") ? "image/png" : name.EndsWith(".jpg") || name.EndsWith(".jpeg") ? "image/jpeg" : "application/octet-stream";
			return "data:" + mime + ";base64," + data;
		}

		static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static async Task<bool> SubscribeAsync(ulong fileId, bool on)
		{
			var d = WebworksJson.Obj(await ApiClient.PostAsync(on ? "/UGC/Subscribe" : "/UGC/Unsubscribe", "{\"fileId\":" + WebworksJson.Quote(fileId.ToString()) + "}"));
			bool ok = d != null && WebworksJson.Str(d, "error") == null;
			if (ok) WorkshopContent.SetSubscribed(fileId, on);
			return ok;
		}

		public static async Task<bool> FavoriteAsync(ulong fileId, bool on)
		{
			var d = WebworksJson.Obj(await ApiClient.PostAsync(on ? "/UGC/Favorite" : "/UGC/Unfavorite", "{\"fileId\":" + WebworksJson.Quote(fileId.ToString()) + "}"));
			return d != null && WebworksJson.Str(d, "error") == null;
		}

		public static async Task<ResultPage?> QueryAsync(Query q, int page)
		{
			var body = new StringBuilder("{\"page\":").Append(page).Append(",\"sort\":").Append(WebworksJson.Quote(q._sort));
			if (q._searchText != null) body.Append(",\"searchText\":").Append(WebworksJson.Quote(q._searchText));
			if (q._type != null) body.Append(",\"type\":").Append(WebworksJson.Quote(q._type));
			if (q._forUser.Value != 0) body.Append(",\"userId\":").Append(WebworksJson.Quote(q._forUser.Value.ToString()));
			body.Append(",\"matchAllTags\":").Append(q._matchAllTags ? "true" : "false");
			body.Append(",\"tags\":[");
			for (int i = 0; i < q._tags.Count; i++) body.Append(i > 0 ? "," : "").Append(WebworksJson.Quote(q._tags[i]));
			body.Append("]}");

			var d = WebworksJson.Obj(await ApiClient.PostAsync("/UGC/Query", body.ToString()));
			if (d == null) return null;

			var entries = new System.Collections.Generic.List<Item>();
			var arr = WebworksJson.Arr(d, "entries");
			if (arr != null) foreach (var o in arr) entries.Add(ItemFromJson(o as System.Collections.Generic.Dictionary<string, object>));

			return new ResultPage { _entries = entries.ToArray(), _totalCount = WebworksJson.Int(d, "totalCount") };
		}
	}
}
