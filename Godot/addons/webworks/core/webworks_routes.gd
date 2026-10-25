@tool
class_name WebworksRoutes
extends RefCounted

## Every route the Webworks server mounts, grouped by mount. Paths are relative to the
## engine mount (for example /api/Godot/GodotSteam), which Webworks.mounted() adds.
##
## The bootstrap itself only uses Accounts, User, Friends and Matchmaking; the rest are
## here so a game can reach any family the other shims expose without hunting for strings.

const ACCOUNTS := {
	"mode": "/Accounts/Mode",
	"register": "/Accounts/Register",
	"login": "/Accounts/Login",
	"logout": "/Accounts/Logout",
	"me": "/Accounts/Me",
	"password": "/Accounts/Password",
	"friends": "/Accounts/Friends",
	"friends_add": "/Accounts/Friends/Add",
	"friends_accept": "/Accounts/Friends/Accept",
	"friends_remove": "/Accounts/Friends/Remove",
	"friends_block": "/Accounts/Friends/Block",
	"friends_unblock": "/Accounts/Friends/Unblock",
	"search": "/Accounts/Search",
	"avatar": "/Accounts/Avatar",
}

const USER := {
	"guest_session": "/User/GuestSession",
	"link_account": "/User/LinkAccount",
	"profile": "/User/Profile",
	"status": "/User/Status",
	"persona_state": "/User/PersonaState",
	"auth_ticket": "/User/AuthTicket",
	"begin_auth_session": "/User/BeginAuthSession",
	"end_auth_session": "/User/EndAuthSession",
	"encrypted_app_ticket": "/User/EncryptedAppTicket",
	"duration_control": "/User/DurationControl",
	"heartbeat": "/User/Heartbeat",
	"offline": "/User/Offline",
	"ping": "/User/Ping",
}

const FRIENDS := {
	"list": "/Friends/List",
	"graph": "/Friends/Graph",
	"search": "/Friends/Search",
	"add": "/Friends/Add",
	"accept": "/Friends/Accept",
	"remove": "/Friends/Remove",
	"block": "/Friends/Block",
	"online": "/Friends/Online",
	"message": "/Friends/Message",
	"game_invite": "/Friends/GameInvite",
	"rich_presence": "/Friends/RichPresence",
	"rich_presence_other": "/Friends/RichPresence/Other",
	"request_user_info": "/Friends/RequestUserInfo",
	"played_with": "/Friends/PlayedWith",
	"avatar": "/Friends/Avatar",
	"follow": "/Friends/Follow",
	"unfollow": "/Friends/Unfollow",
	"followers": "/Friends/Followers",
	"following": "/Friends/Following",
	"is_following": "/Friends/IsFollowing",
}

const MATCHMAKING := {
	"create_lobby": "/Matchmaking/CreateLobby",
	"join_lobby": "/Matchmaking/JoinLobby",
	"leave_lobby": "/Matchmaking/LeaveLobby",
	"lobby": "/Matchmaking/Lobby",
	"lobby_members": "/Matchmaking/LobbyMembers",
	"lobby_data": "/Matchmaking/LobbyData",
	"lobby_visibility": "/Matchmaking/LobbyVisibility",
	"lobby_joinable": "/Matchmaking/LobbyJoinable",
	"lobby_max_members": "/Matchmaking/LobbyMaxMembers",
	"lobby_owner": "/Matchmaking/LobbyOwner",
	"lobby_linked": "/Matchmaking/LobbyLinked",
	"lobby_game_server": "/Matchmaking/LobbyGameServer",
	"lobby_list": "/Matchmaking/LobbyList",
	"member_data": "/Matchmaking/MemberData",
	"chat_send": "/Matchmaking/ChatSend",
	"create_invite": "/Matchmaking/CreateInvite",
	"resolve_invite": "/Matchmaking/ResolveInvite",
	"invite_friend": "/Matchmaking/InviteFriend",
	"invites": "/Matchmaking/Invites",
	"invites_dismiss": "/Matchmaking/Invites/Dismiss",
	"friends_active": "/Matchmaking/FriendsActive",
}

const NETWORKING := {
	"relay_ticket": "/Networking/RelayTicket",
	"accept_session": "/Networking/AcceptSession",
	"close_session": "/Networking/CloseSession",
}

const USER_STATS := {
	"stats": "/UserStats/Stats",
	"stat": "/UserStats/Stat",
	"reset": "/UserStats/Reset",
	"global_stats": "/UserStats/GlobalStats",
	"player_count": "/UserStats/PlayerCount",
	"user_stats": "/UserStats/UserStats",
	"achievements": "/UserStats/Achievements",
	"achievements_define": "/UserStats/Achievements/Define",
	"achievements_unlock": "/UserStats/Achievements/Unlock",
	"achievements_clear": "/UserStats/Achievements/Clear",
	"achievements_earned": "/UserStats/Achievements/Earned",
	"achievements_catalog": "/UserStats/Achievements/Catalog",
	"achievements_profile": "/UserStats/Achievements/Profile",
	"achievements_percentages": "/UserStats/Achievements/Percentages",
	"achievement_progress": "/UserStats/AchievementProgress",
	"leaderboard": "/UserStats/Leaderboard",
	"leaderboard_find": "/UserStats/Leaderboard/Find",
	"leaderboard_find_or_create": "/UserStats/Leaderboard/FindOrCreate",
	"leaderboard_submit": "/UserStats/Leaderboard/Submit",
	"leaderboard_entries": "/UserStats/Leaderboard/Entries",
	"leaderboard_entries_for_users": "/UserStats/Leaderboard/EntriesForUsers",
	"leaderboard_attach_ugc": "/UserStats/Leaderboard/AttachUGC",
}

const REMOTE_STORAGE := {
	"files": "/RemoteStorage/Files",
	"write": "/RemoteStorage/Write",
	"delete": "/RemoteStorage/Delete",
}

const UGC := {
	"publish": "/UGC/Publish",
	"query": "/UGC/Query",
	"item": "/UGC/Item",
	"download": "/UGC/Download",
	"update": "/UGC/Update",
	"delete": "/UGC/Delete",
	"subscribe": "/UGC/Subscribe",
	"unsubscribe": "/UGC/Unsubscribe",
	"favorite": "/UGC/Favorite",
	"unfavorite": "/UGC/Unfavorite",
	"vote": "/UGC/Vote",
}

const GAME_SERVER := {
	"log_on": "/GameServer/LogOn",
	"log_off": "/GameServer/LogOff",
	"list": "/GameServer/List",
	"heartbeat": "/GameServer/Heartbeat",
	"keys": "/GameServer/Keys",
	"player": "/GameServer/Player",
	"stats": "/GameServer/Stats",
}

const INVENTORY := {
	"definitions": "/Inventory/Definitions",
	"define": "/Inventory/Define",
	"items": "/Inventory/Items",
	"grant_self": "/Inventory/GrantSelf",
	"consume": "/Inventory/Consume",
	"exchange": "/Inventory/Exchange",
	"trade": "/Inventory/Trade",
	"properties": "/Inventory/Properties",
	"inspect": "/Inventory/Inspect",
	"grant": "/Inventory/Grant",
	"transfer": "/Inventory/Transfer",
}

const MEDIA := {
	"music_state": "/Music/State",
	"party_create": "/Party/Create",
	"party_beacons": "/Party/Beacons",
	"party_beacon": "/Party/Beacon",
	"party_join": "/Party/Join",
	"party_leave": "/Party/Leave",
	"party_invite": "/Party/Invite",
	"video_start": "/Video/Broadcast/Start",
	"video_stop": "/Video/Broadcast/Stop",
	"video_status": "/Video/Status",
	"video_url": "/Video/URL",
	"video_watch": "/Video/Watch",
	"video_unwatch": "/Video/Unwatch",
	"remote_play_start": "/RemotePlay/Start",
	"remote_play_stop": "/RemotePlay/Stop",
	"remote_play_sessions": "/RemotePlay/Sessions",
	"remote_play_session": "/RemotePlay/Session",
	"remote_play_join": "/RemotePlay/Join",
	"remote_play_leave": "/RemotePlay/Leave",
	"remote_play_invite": "/RemotePlay/Invite",
	"remote_play_input": "/RemotePlay/Input",
}

const EXTRAS := {
	"apps_info": "/Apps/Info",
	"screenshots": "/Screenshots/List",
	"screenshot_add": "/Screenshots/Add",
	"screenshot_image": "/Screenshots/Image",
	"screenshot_delete": "/Screenshots/Delete",
	"timeline": "/Timeline/Events",
	"timeline_event": "/Timeline/Event",
	"log": "/Log",
	"html_fetch": "/HTML/Fetch",
	"http_request": "/HTTP/Request",
	"ping": "/Ping",
}

## Engine mounts the server has answered on, newest first. A project that was set up for
## an older shim still works: the client retries these when a route answers 404.
const ENGINE_MOUNTS := [
	"Godot/GodotSteam",
	"Godot",
	"Unity/SteamworksNET",
	"Unity/Steamworks.net",
	"Unity/SteamworksNet",
	"Unity/Steamworks",
	"Unity/Facepunch",
	"Unity/Webeam",
	"Unity/Weeam",
	"Unity/Webworks",
	"Unity",
	"Unreal/Steam",
	"Unreal/Steamworks",
	"Unreal",
	"",
]
