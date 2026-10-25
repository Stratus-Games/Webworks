using System;
using System.Collections.Generic;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	// Social graph surface: clans, co-play tracking, chat membership and the account-level
	// follow relation. Webworks has no clan service, so clan lookups are empty by design;
	// followers/following and co-play are real and come from the server.
	public static partial class SteamFriends
	{
		// Friends currently in the same game, plus lobby members, in a stable order.
		internal static List<ulong> CoplayFriendIds()
		{
			var list = new List<ulong>();
			uint appId;
			uint.TryParse(SessionState.AppId, out appId);

			foreach (var id in SNetState.Friends.AllIds)
			{
				FriendGameInfo_t info;
				if (!_friendGames.TryGetValue(id, out info)) continue;
				if (appId != 0 && info.m_gameID.AppID().m_AppId != appId) continue;
				if (!list.Contains(id)) list.Add(id);
			}

			var me = SteamUser.GetSteamID().m_SteamID;
			foreach (var lobby in SNetState.Lobbies.All)
			{
				foreach (var member in lobby.Members)
				{
					if (member == me || list.Contains(member)) continue;
					list.Add(member);
				}
			}

			return list;
		}

		public static CSteamID GetClanByIndex(int iClan) { return CSteamID.Nil; }

		public static SteamAPICall_t DownloadClanActivityCounts(CSteamID[] psteamIDClans, int cClansToRequest)
		{
			// No clan service exists in Webworks, so the request completes with "not found"
			// instead of hanging forever.
			var call = ResultBus.NewCall();
			MainThreadDispatcher.Enqueue(() => ResultBus.Complete(call, new DownloadClanActivityCountsResult_t { m_bSuccess = false }));
			return call;
		}

		public static CSteamID GetFriendFromSourceByIndex(CSteamID steamIDSource, int iFriend)
		{
			if (iFriend < 0) return CSteamID.Nil;

			var lobby = SNetState.Lobbies.Get(steamIDSource.m_SteamID);
			if (lobby != null)
				return iFriend < lobby.Members.Count ? new CSteamID(lobby.Members[iFriend]) : CSteamID.Nil;

			var ids = new List<ulong>(SNetState.Friends.AllIds);
			return iFriend < ids.Count ? new CSteamID(ids[iFriend]) : CSteamID.Nil;
		}

		public static SteamAPICall_t RequestClanOfficerList(CSteamID steamIDClan)
		{
			var call = ResultBus.NewCall();
			MainThreadDispatcher.Enqueue(() => ResultBus.Complete(call, new ClanOfficerListResponse_t
			{
				m_steamIDClan = steamIDClan,
				m_cOfficers = 0,
				m_bSuccess = 0,
			}));
			return call;
		}

		public static CSteamID GetClanOwner(CSteamID steamIDClan) { return CSteamID.Nil; }

		public static CSteamID GetClanOfficerByIndex(CSteamID steamIDClan, int iOfficer) { return CSteamID.Nil; }

		public static CSteamID GetCoplayFriend(int iCoplayFriend)
		{
			var ids = CoplayFriendIds();
			return iCoplayFriend >= 0 && iCoplayFriend < ids.Count ? new CSteamID(ids[iCoplayFriend]) : CSteamID.Nil;
		}

		public static AppId_t GetFriendCoplayGame(CSteamID steamIDFriend)
		{
			FriendGameInfo_t info;
			return _friendGames.TryGetValue(steamIDFriend.m_SteamID, out info) ? info.m_gameID.AppID() : AppId_t.Invalid;
		}

		public static CSteamID GetChatMemberByIndex(CSteamID steamIDClan, int iUser)
		{
			// Clan chat has no membership, so the closest real chat is a lobby you are in.
			foreach (var lobby in SNetState.Lobbies.All)
			{
				if (iUser < 0 || iUser >= lobby.Members.Count) continue;
				return new CSteamID(lobby.Members[iUser]);
			}
			return CSteamID.Nil;
		}

		static void CompleteFollowerCount(SteamAPICall_t call, CSteamID steamID, string json)
		{
			var payload = WebworksJson.Obj(json);
			var accounts = payload != null ? WebworksJson.Arr(payload, "accounts") : null;
			ResultBus.Complete(call, new FriendsGetFollowerCount_t
			{
				m_eResult = payload != null ? EResult.k_EResultOK : EResult.k_EResultFail,
				m_steamID = steamID,
				m_nCount = accounts == null ? 0 : accounts.Count,
			});
		}

		public static SteamAPICall_t GetFollowerCount(CSteamID steamID)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var json = await ApiClient.GetAsync("/Friends/Followers?steamId=" + steamID.m_SteamID);
				CompleteFollowerCount(call, steamID, json);
				return "";
			}, "GetFollowerCount");
			return call;
		}

		public static SteamAPICall_t IsFollowing(CSteamID steamID)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/Friends/IsFollowing?steamId=" + steamID.m_SteamID));
				ResultBus.Complete(call, new FriendsIsFollowing_t
				{
					m_eResult = payload != null ? EResult.k_EResultOK : EResult.k_EResultFail,
					m_steamID = steamID,
					m_bIsFollowing = payload != null && WebworksJson.Bool(payload, "following"),
				});
				return "";
			}, "IsFollowing");
			return call;
		}

		public static SteamAPICall_t EnumerateFollowingList(uint unStartIndex)
		{
			var call = ResultBus.NewCall();
			ApiClient.Fire(async () =>
			{
				var payload = WebworksJson.Obj(await ApiClient.GetAsync("/Friends/Following"));
				var accounts = payload != null ? WebworksJson.Arr(payload, "accounts") : null;
				var ids = new CSteamID[Constants.k_cEnumerateFollowersMax];
				int count = 0;

				if (accounts != null)
				{
					for (int i = (int)unStartIndex; i < accounts.Count && count < ids.Length; i++)
					{
						var entry = accounts[i] as Dictionary<string, object>;
						var steamId = WebworksJson.Ulong(entry, "steamId");
						if (steamId == 0) continue;
						ids[count++] = new CSteamID(steamId);
					}
				}

				ResultBus.Complete(call, new FriendsEnumerateFollowingList_t
				{
					m_eResult = payload != null ? EResult.k_EResultOK : EResult.k_EResultFail,
					m_rgSteamID = ids,
					m_nResultsReturned = count,
					m_nTotalResultCount = accounts == null ? 0 : accounts.Count,
				});
				return "";
			}, "EnumerateFollowingList");
			return call;
		}

		public static SteamAPICall_t RequestEquippedProfileItems(CSteamID steamID)
		{
			// Community profile items are not part of a Webworks account, so the answer is a
			// clean "nothing equipped" rather than a pending call that never completes.
			var call = ResultBus.NewCall();
			MainThreadDispatcher.Enqueue(() => ResultBus.Complete(call, new EquippedProfileItems_t
			{
				m_eResult = EResult.k_EResultOK,
				m_steamID = steamID,
				m_bHasAnimatedAvatar = false,
				m_bHasAvatarFrame = false,
				m_bHasProfileModifier = false,
				m_bHasProfileBackground = false,
				m_bHasMiniProfileBackground = false,
			}));
			return call;
		}
	}
}
