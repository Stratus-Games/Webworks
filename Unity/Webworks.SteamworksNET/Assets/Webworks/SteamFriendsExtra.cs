using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Webworks.SNetInternal;

namespace Steamworks
{
	// Friend metadata that Steam keeps client-side: nicknames, groups, clans, chat
	// history and the profile item surface. Everything the server knows comes from the
	// friend graph and the realtime channel.
	public static partial class SteamFriends
	{
		internal class IncomingMessage
		{
			public ulong From;
			public string Text;
			public EChatEntryType Type;
			public DateTime At;
		}

		static readonly Dictionary<ulong, string> _nicknames = new Dictionary<ulong, string>();
		static readonly Dictionary<ulong, FriendGameInfo_t> _friendGames = new Dictionary<ulong, FriendGameInfo_t>();
		static readonly Dictionary<ulong, Dictionary<string, string>> _friendPresence = new Dictionary<ulong, Dictionary<string, string>>();
		static readonly List<IncomingMessage> _inbox = new List<IncomingMessage>();
		static readonly HashSet<string> _overlayProtocols = new HashSet<string>();
		static bool _listenForFriendMessages = true;

		internal static void SetFriendGame(ulong steamId, uint appId, ulong lobbyId, uint gameIp, ushort gamePort, ushort queryPort)
		{
			if (appId == 0)
			{
				_friendGames.Remove(steamId);
				return;
			}
			_friendGames[steamId] = new FriendGameInfo_t
			{
				m_gameID = new CGameID((ulong)appId),
				m_steamIDLobby = new CSteamID(lobbyId),
				m_unGameIP = gameIp,
				m_usGamePort = gamePort,
				m_usQueryPort = queryPort,
			};
		}

		internal static void QueueFriendMessage(ulong from, string text)
		{
			if (!_listenForFriendMessages) return;
			_inbox.Add(new IncomingMessage { From = from, Text = text ?? "", Type = EChatEntryType.k_EChatEntryTypeChatMsg, At = DateTime.UtcNow });
			while (_inbox.Count > 256) _inbox.RemoveAt(0);
		}

		internal static void SetFriendPresence(ulong steamId, string key, string value)
		{
			Dictionary<string, string> map;
			if (!_friendPresence.TryGetValue(steamId, out map))
			{
				map = new Dictionary<string, string>();
				_friendPresence[steamId] = map;
			}
			if (value == null) map.Remove(key);
			else map[key] = value;
		}

		public static bool GetFriendGamePlayed(CSteamID steamIDFriend, out FriendGameInfo_t pFriendGameInfo)
		{
			return _friendGames.TryGetValue(steamIDFriend.m_SteamID, out pFriendGameInfo);
		}

		public static string GetFriendPersonaNameHistory(CSteamID steamIDFriend, int iPersonaName)
		{
			return iPersonaName == 0 ? GetFriendPersonaName(steamIDFriend) : string.Empty;
		}

		public static int GetFriendSteamLevel(CSteamID steamIDFriend)
		{
			return SNetState.Friends.IsFriend(steamIDFriend.m_SteamID) ? 1 : 0;
		}

		public static string GetPlayerNickname(CSteamID steamIDPlayer)
		{
			string nick;
			return _nicknames.TryGetValue(steamIDPlayer.m_SteamID, out nick) ? nick : string.Empty;
		}

		public static bool SetPlayerNickname(CSteamID steamIDPlayer, string cszNickname)
		{
			if (steamIDPlayer.m_SteamID == 0) return false;
			if (string.IsNullOrEmpty(cszNickname)) _nicknames.Remove(steamIDPlayer.m_SteamID);
			else _nicknames[steamIDPlayer.m_SteamID] = cszNickname;
			return true;
		}

		const short FriendsGroupId = 1;

		public static int GetFriendsGroupCount() { return 1; }

		public static FriendsGroupID_t GetFriendsGroupIDByIndex(int iFG)
		{
			return iFG == 0 ? new FriendsGroupID_t(FriendsGroupId) : new FriendsGroupID_t(0);
		}

		public static string GetFriendsGroupName(FriendsGroupID_t friendsGroupID)
		{
			return friendsGroupID.m_FriendsGroupID == FriendsGroupId ? "Friends" : string.Empty;
		}

		public static int GetFriendsGroupMembersCount(FriendsGroupID_t friendsGroupID)
		{
			return friendsGroupID.m_FriendsGroupID == FriendsGroupId ? GetFriendCount(EFriendFlags.k_EFriendFlagImmediate) : 0;
		}

		public static void GetFriendsGroupMembersList(FriendsGroupID_t friendsGroupID, CSteamID[] pOutSteamIDMembers, int nMembersCount)
		{
			if (friendsGroupID.m_FriendsGroupID != FriendsGroupId || pOutSteamIDMembers == null) return;
			var list = new List<ulong>(SNetState.Friends.AllIds);
			for (int i = 0; i < nMembersCount && i < list.Count && i < pOutSteamIDMembers.Length; i++)
				pOutSteamIDMembers[i] = new CSteamID(list[i]);
		}

		public static bool HasFriend(CSteamID steamIDFriend, EFriendFlags iFriendFlags)
		{
			var relationship = GetFriendRelationship(steamIDFriend);
			if (iFriendFlags.HasFlag(EFriendFlags.k_EFriendFlagBlocked)) return relationship == EFriendRelationship.k_EFriendRelationshipBlocked;
			return relationship == EFriendRelationship.k_EFriendRelationshipFriend;
		}

		public static int GetClanCount() { return 0; }
		public static string GetClanName(CSteamID steamIDClan) { return string.Empty; }
		public static string GetClanTag(CSteamID steamIDClan) { return string.Empty; }

		public static bool GetClanActivityCounts(CSteamID steamIDClan, out int pnOnline, out int pnInGame, out int pnChatting)
		{
			pnOnline = 0;
			pnInGame = 0;
			pnChatting = 0;
			return false;
		}

		public static int GetFriendCountFromSource(CSteamID steamIDSource) { return 0; }
		public static bool IsUserInSource(CSteamID steamIDUser, CSteamID steamIDSource) { return false; }

		public static void ActivateGameOverlayToWebPage(string pchURL, EActivateGameOverlayToWebPageMode eMode)
		{
			ActivateGameOverlayToWebPage(pchURL);
		}

		public static int GetClanOfficerCount(CSteamID steamIDClan) { return 0; }

		public static int GetFriendRichPresenceKeyCount(CSteamID steamIDFriend)
		{
			Dictionary<string, string> map;
			return _friendPresence.TryGetValue(steamIDFriend.m_SteamID, out map) ? map.Count : 0;
		}

		public static string GetFriendRichPresenceKeyByIndex(CSteamID steamIDFriend, int iKey)
		{
			Dictionary<string, string> map;
			if (!_friendPresence.TryGetValue(steamIDFriend.m_SteamID, out map) || iKey < 0 || iKey >= map.Count) return string.Empty;
			int index = 0;
			foreach (var key in map.Keys)
			{
				if (index++ == iKey) return key;
			}
			return string.Empty;
		}

		public static void RequestFriendRichPresence(CSteamID steamIDFriend)
		{
			// Presence arrives with the friend graph refresh; ask for a fresh list so the
			// caller's next read sees current values.
			RefreshFriendsList();
		}

		public static int GetCoplayFriendCount() { return CoplayFriendIds().Count; }
		public static int GetFriendCoplayTime(CSteamID steamIDFriend) { return _friendGames.ContainsKey(steamIDFriend.m_SteamID) ? 1 : 0; }

		static readonly HashSet<ulong> _clanChats = new HashSet<ulong>();

		public static bool JoinClanChatRoom(CSteamID steamIDClan) { return false; }

		public static bool LeaveClanChatRoom(CSteamID steamIDClan)
		{
			return _clanChats.Remove(steamIDClan.m_SteamID);
		}

		public static int GetClanChatMemberCount(CSteamID steamIDClan) { return 0; }

		public static bool SendClanChatMessage(CSteamID steamIDClanChat, string pchText)
		{
			return false;
		}

		public static int GetClanChatMessage(CSteamID steamIDClanChat, int iMessage, out string prgchText, int cchTextMax, out EChatEntryType peChatEntryType, out CSteamID psteamidChatter)
		{
			prgchText = string.Empty;
			peChatEntryType = EChatEntryType.k_EChatEntryTypeInvalid;
			psteamidChatter = CSteamID.Nil;
			return 0;
		}

		public static bool IsClanChatAdmin(CSteamID steamIDClanChat, CSteamID steamIDUser) { return false; }
		public static bool IsClanChatWindowOpenInSteam(CSteamID steamIDClanChat) { return _clanChats.Contains(steamIDClanChat.m_SteamID); }
		public static bool OpenClanChatWindowInSteam(CSteamID steamIDClanChat) { return _clanChats.Add(steamIDClanChat.m_SteamID); }
		public static bool CloseClanChatWindowInSteam(CSteamID steamIDClanChat) { return _clanChats.Remove(steamIDClanChat.m_SteamID); }
		public static bool SetListenForFriendsMessages(bool bInterceptEnabled) { _listenForFriendMessages = bInterceptEnabled; return true; }

		public static bool ReplyToFriendMessage(CSteamID steamIDFriend, string pchMsgToSend)
		{
			return SendFriendMessage(steamIDFriend, pchMsgToSend);
		}

		public static int GetFriendMessage(CSteamID steamIDFriend, int iMessageID, out string pvData, int cubData, out EChatEntryType peChatEntryType)
		{
			pvData = string.Empty;
			peChatEntryType = EChatEntryType.k_EChatEntryTypeInvalid;

			int index = 0;
			for (int i = 0; i < _inbox.Count; i++)
			{
				var message = _inbox[i];
				if (message.From != steamIDFriend.m_SteamID) continue;
				if (index++ != iMessageID) continue;

				_inbox.RemoveAt(i);
				int max = Math.Max(0, cubData - 1);
				pvData = message.Text.Length > max ? message.Text.Substring(0, max) : message.Text;
				peChatEntryType = message.Type;
				return pvData.Length + 1;
			}
			return 0;
		}

		public static bool IsClanPublic(CSteamID steamIDClan) { return false; }
		public static bool IsClanOfficialGameGroup(CSteamID steamIDClan) { return false; }
		public static int GetNumChatsWithUnreadPriorityMessages() { return _inbox.Count; }

		public static void ActivateGameOverlayRemotePlayTogetherInviteDialog(CSteamID steamIDLobby)
		{
			ActivateGameOverlayInviteDialog(steamIDLobby);
		}

		public static bool RegisterProtocolInOverlayBrowser(string pchProtocol)
		{
			if (string.IsNullOrEmpty(pchProtocol)) return false;
			return _overlayProtocols.Add(pchProtocol);
		}

		public static void ActivateGameOverlayInviteDialogConnectString(string pchConnectString)
		{
			Debug.Log("[Webworks] invite dialog: " + (pchConnectString ?? ""));
		}

		public static bool BHasEquippedProfileItem(CSteamID steamID, ECommunityProfileItemType itemType) { return false; }

		public static string GetProfileItemPropertyString(CSteamID steamID, ECommunityProfileItemType itemType, ECommunityProfileItemProperty prop)
		{
			return string.Empty;
		}

		public static uint GetProfileItemPropertyUint(CSteamID steamID, ECommunityProfileItemType itemType, ECommunityProfileItemProperty prop)
		{
			return 0;
		}
	}
}
