extends RefCounted

## The half of the GodotSteam shim that does the work.
##
## steam_api.gd forwards every GodotSteam call here by name; this file answers it from
## the Webworks server instead of the Steam client. Calls stay synchronous like
## GodotSteam's: a request that has to travel is started in the background and its answer
## arrives as the same signal GodotSteam would have emitted (`lobby_created`,
## `lobby_joined`, `leaderboard_find_result`, ...).
##
## Families with real behaviour: identity/session, persona, friends, rich presence,
## lobbies (including GWAS cross-server lobbies), stats, achievements, leaderboards,
## remote storage/cloud saves, the in-game overlay and relay-backed P2P packets. Anything
## not listed here answers with the empty value GodotSteam declares for it, so game code
## that pokes an unsupported corner keeps running instead of crashing.

signal overlay_requested(tab: String)

## Routes are preloaded by path so the addon works even before the editor has
## registered its global class names (a fresh checkout, a headless export).
const Routes := preload("res://addons/webworks/core/webworks_routes.gd")

const POLL_SECONDS := 4.0
const RELAY_RETRY_SECONDS := 8.0
const OVERLAY_SCENE := "res://addons/webworks/ui/steam_overlay.tscn"

var steam: Node
var webworks: Node

var _session_ready := false
var _poll := 0.0
var _relay_retry := 0.0
var _name := ""

# Friends come from the server's decorated friend graph; ids stay strings on the server
# and become GodotSteam's int64 handles here.
var _graph: Dictionary = {}
var _friends: Dictionary = {}
var _friend_order: Array = []
var _friend_states: Dictionary = {}
var _invite_seen: Dictionary = {}

var _rich: Dictionary = {}

var _lobby_id := ""
var _lobby: Dictionary = {}
var _lobby_dirty := false

var _catalog: Array = []
var _achievements: Dictionary = {}
var _stats: Dictionary = {}
var _stats_dirty := false
var _pending_unlocks: Array = []
var _pending_clears: Array = []

var _boards: Dictionary = {}
var _board_by_handle: Dictionary = {}
var _board_counts: Dictionary = {}
var _next_handle := 1

var _files: Dictionary = {}
var _quota_total := 0
var _quota_used := 0

var _relay = null
var _relay_lobby := ""
var _relay_ready := false
var _relay_outbox: Array = []
var _relay_inbox: Dictionary = {}
var _relay_peers: Dictionary = {}
var _relay_ticket := ""

var _overlay_tab := ""

const RICH_PRESENCE_KEY_MAX := 20

# ------------------------------------------------------------------ dispatch

func dispatch(method: String, args: Array):
	match method:
		# ---------------------------------------------------------- lifecycle
		"init":
			start()
			return {"status": true, "verbal": true}
		"steamShutdown", "shutdown":
			_shutdown()
			return null
		"run_callbacks":
			tick(0.0)
			return null
		"restartAppIfNecessary":
			return false
		"isSteamRunning", "loggedOn":
			return _session_ready
		"getAppID":
			return _app_id_number()
		"getSteamId", "getSteamID":
			return _as_id(webworks.steam_id() if webworks else "")
		"getPersonaName":
			return _name
		"getPersonaState":
			return 1
		"setPersonaName":
			return _set_persona_name(str(args[0]))
		"getPlayerSteamLevel":
			return 1
		"getServerRealTime":
			return int(Time.get_unix_time_from_system())
		"getSteamUILanguage", "getCurrentGameLanguage":
			return "english"
		"getIPCountry":
			return ""
		"getSecondsSinceAppActive", "getSecondsSinceComputerActive":
			return int(Time.get_ticks_msec() / 1000)
		"getAvailableGameLanguages":
			return "english"
		"getAuthTicketForWebApi":
			return _auth_ticket()
		"getAuthSessionTicket":
			return {"ret": 0, "ticket": PackedByteArray(), "size": 0}
		"cancelAuthTicket", "endAuthSession":
			return null

		# ---------------------------------------------------------- friends
		"getFriendCount":
			return _friend_order.size()
		"getFriendByIndex":
			return _friend_at(int(args[0]))
		"getFriendPersonaName":
			return _friend_value(args[0], "name", "")
		"getFriendPersonaState":
			return _friend_persona_state(args[0])
		"getFriendRelationship":
			return 3 if _friend_value(args[0], "name", "") != "" else 0
		"getFriendGamePlayed":
			return _friend_game(args[0])
		"getFriendRichPresence":
			return _friend_value(args[0], "presence", "")
		"getFriendNickname", "getFriendAlias":
			return _friend_value(args[0], "name", "")
		"getSmallFriendAvatar", "getMediumFriendAvatar", "getLargeFriendAvatar":
			return _avatar_handle(args[0])
		"requestUserInformation":
			return true
		"requestFriendRichPresence":
			_poll_friends()
			return true
		"setPlayedWith":
			return null
		"getPlayedWith", "getCoplayFriend", "getCoplayFriendCount":
			return steam._empty("getPlayedWith")
		"setRichPresence":
			return _set_rich_presence(str(args[0]), str(args[1]))
		"clearRichPresence":
			_clear_rich_presence()
			return null
		"getFriendsGroupCount", "getClanCount":
			return 0

		# ---------------------------------------------------------- lobbies
		"createLobby":
			_create_lobby(int(args[0]), int(args[1]))
			return null
		"joinLobby":
			_join_lobby(_as_id_string(args[0]))
			return null
		"leaveLobby":
			_leave_lobby(_as_id_string(args[0]))
			return null
		"requestLobbyList":
			_request_lobby_list()
			return null
		"getLobbyData":
			return str(_lobby_data().get(str(args[1]), ""))
		"getAllLobbyData":
			var out := {}
			for key in _lobby_data().keys():
				out[str(key)] = str(_lobby_data()[key])
			return out
		"getLobbyDataCount":
			return _lobby_data().size()
		"getLobbyDataByIndex":
			var keys: Array = _lobby_data().keys()
			var index: int = int(args[1])
			return str(keys[index]) if index >= 0 and index < keys.size() else ""
		"setLobbyData":
			_lobby_set(str(args[1]), str(args[2]))
			return true
		"deleteLobbyData":
			_lobby_set(str(args[1]), "")
			return true
		"getLobbyMemberData":
			return _member_data(args[1], str(args[2]))
		"setLobbyMemberData":
			_set_member_data(str(args[1]), str(args[2]))
			return null
		"getNumLobbyMembers":
			return _lobby_members().size()
		"getLobbyMemberByIndex":
			return _lobby_member_at(int(args[1]))
		"getLobbyMemberLimit":
			return int(_lobby.get("maxMembers", 0))
		"setLobbyMemberLimit":
			_lobby_post(Routes.MATCHMAKING["lobby_max_members"], {"maxMembers": int(args[1])})
			return int(args[1]) > 0
		"getLobbyOwner":
			return _as_id(str(_lobby.get("ownerId", "")))
		"setLobbyOwner":
			_lobby_post(Routes.MATCHMAKING["lobby_owner"], {"steamId": _as_id_string(int(args[1]))})
			return true
		"setLobbyType":
			_lobby_post(Routes.MATCHMAKING["lobby_visibility"], {"visibility": _visibility_name(int(args[1]))})
			return true
		"getLobbyType":
			return _visibility_id(str(_lobby.get("visibility", "public")))
		"setLobbyJoinable":
			_lobby_post(Routes.MATCHMAKING["lobby_joinable"], {"joinable": bool(args[1])})
			return true
		"getLobbyJoinable":
			return bool(_lobby.get("joinable", true))
		"getLobbyGameServer":
			var server: Dictionary = _lobby.get("gameServer", {})
			return {
				"steam_id_game_server": _as_id(str(server.get("serverId", "0"))),
				"server_ip": str(server.get("ip", "0")),
				"server_port": int(server.get("port", 0)),
			}
		"setLobbyGameServer":
			_lobby_post(Routes.MATCHMAKING["lobby_game_server"], {
				"ip": str(args[1]),
				"port": int(args[2]),
				"serverId": _as_id_string(int(args[3])),
			})
			return null
		"sendLobbyChatMsg":
			return _send_lobby_chat(str(args[1]))
		"inviteUserToLobby":
			return _invite_to_lobby(_as_id_string(int(args[1])))
		"getLobbyChatEntry", "getLobbyChatEntryCount":
			return steam._empty("getLobbyChatEntry")

		# ---------------------------------------------------------- stats
		"getStatInt":
			return int(_stats.get(str(args[0]), 0))
		"getStatFloat":
			return float(_stats.get(str(args[0]), 0.0))
		"setStatInt":
			_stats[str(args[0])] = int(args[1])
			_stats_dirty = true
			return true
		"setStatFloat":
			_stats[str(args[0])] = float(args[1])
			_stats_dirty = true
			return true
		"requestCurrentStats":
			_pull_stats()
			return true
		"storeStats", "requestGlobalStats":
			if method == "requestGlobalStats":
				_pull_global_stats(int(args[0]))
				return true
			return _store_stats()
		"resetAllStats":
			_reset_stats(bool(args[0]))
			return true
		"getAchievement":
			return {"ret": _achievements.has(str(args[0])), "achieved": _unlocked(str(args[0]))}
		"getAchievementAndUnlockTime":
			return {
				"retrieve": _achievements.has(str(args[0])),
				"achieved": _unlocked(str(args[0])),
				"unlocked": int(_achievement(str(args[0])).get("unlockTime", 0)),
			}
		"setAchievement":
			return _unlock_achievement(str(args[0]))
		"clearAchievement":
			return _clear_achievement(str(args[0]))
		"indicateAchievementProgress":
			_achievement_progress(str(args[0]), int(args[1]), int(args[2]))
			return true
		"getNumAchievements":
			return _catalog.size()
		"getAchievementName":
			var index: int = int(args[0])
			if index < 0 or index >= _catalog.size():
				return ""
			return str(_catalog[index].get("key", ""))
		"getAchievementDisplayAttribute":
			return _achievement_attribute(str(args[0]), str(args[1]))
		"getAchievementIcon":
			return 0

		# ---------------------------------------------------------- leaderboards
		"findOrCreateLeaderboard":
			_find_leaderboard(str(args[0]), int(args[1]), int(args[2]), true)
			return null
		"findLeaderboard":
			_find_leaderboard(str(args[0]), 0, 0, false)
			return null
		"uploadLeaderboardScore":
			_upload_score(int(args[0]), bool(args[1]), args[2], int(args[3]))
			return null
		"getLeaderboardEntries":
			_download_entries(int(args[0]), int(args[2]), int(args[3]))
			return null
		"getLeaderboardEntryCount":
			return int(_board_counts.get(int(args[0]), 0))
		"getLeaderboardName":
			return str(_board_by_handle.get(int(args[0]), ""))

		# ---------------------------------------------------------- remote storage
		"fileWrite", "fileWriteAsync", "fileWriteStreamWriteChunk":
			return _file_write(str(args[0]), args[1])
		"fileRead", "fileReadAsync":
			return _file_read(str(args[0]), int(args[1]) if args.size() > 1 else 0)
		"fileExists":
			return _files.has(str(args[0]))
		"fileDelete":
			return _file_delete(str(args[0]))
		"filePersisted":
			return _files.has(str(args[0]))
		"fileSize":
			return int(_file_entry(str(args[0])).get("size", 0))
		"fileForget":
			return _file_delete(str(args[0]))
		"getQuota":
			return {"total_bytes": _quota_total, "available_bytes": maxi(0, _quota_total - _quota_used)}
		"getFileCount":
			return _files.size()
		"getFileNameAndSize":
			var names: Array = _files.keys()
			var index: int = int(args[0])
			if index < 0 or index >= names.size():
				return {"name": "", "size": 0}
			var key := str(names[index])
			return {"name": key, "size": int(_file_entry(key).get("size", 0))}
		"getCachedUGCCount", "getUGCDetails":
			return steam._empty("getCachedUGCCount")

		# ---------------------------------------------------------- overlay
		"activateGameOverlay", "activateGameOverlayToUser":
			_show_overlay(str(args[0]))
			return null
		"activateGameOverlayInviteDialog":
			_show_overlay("lobbyinvite")
			return null
		"activateGameOverlayToWebPage":
			if str(args[0]) != "":
				OS.shell_open(str(args[0]))
			return null
		"activateGameOverlayToStore":
			OS.shell_open("https://store.steampowered.com/app/%d" % int(args[0]))
			return null
		"isOverlayEnabled":
			return true
		"overlayNeedsPresent":
			return false
		"isGameOverlayActive":
			return _overlay_open()

		# ---------------------------------------------------------- p2p packets
		"acceptP2PSessionWithUser":
			_relay_add_peer(_as_id_string(int(args[0])))
			return true
		"closeP2PSessionWithUser":
			_relay_peers.erase(_as_id_string(int(args[0])))
			return true
		"allowP2PPacketRelay":
			return true
		"sendP2PPacket":
			return _send_packet(int(args[0]), args[1], int(args[2]), int(args[3]))
		"isP2PPacketAvailable":
			return not _inbox(int(args[0])).is_empty()
		"getAvailableP2PPacketSize":
			var queue: Array = _inbox(int(args[0]))
			return int(queue[0].get("data", PackedByteArray()).size()) if not queue.is_empty() else 0
		"readP2PPacket":
			return _read_packet(int(args[1]))

	return steam._empty(method)

# ------------------------------------------------------------------ lifecycle

## Called by steam.gd once the autoloads are wired. Games that would rather control the
## moment themselves set `[steam] auto_init=false` in webworks.cfg and call `Steam.init()`.
func auto_start() -> void:
	if webworks != null and webworks.steam_auto_init:
		start()

func shutdown_now() -> void:
	_shutdown()

## Establishes the Webworks session the shim answers from: the saved account session when
## there is one, a guest session otherwise, exactly like the Unity and Unreal shims.
func start() -> void:
	if webworks == null or _session_ready:
		return
	_go(_establish_session)

func _establish_session() -> void:
	if webworks.has_session():
		var valid: bool = await webworks.verify_session()
		if valid:
			webworks.begin_presence()
			_after_session()
			return
	var reply: Dictionary = await webworks.guest_session(webworks.persona_name)
	if not reply.get("ok", false):
		steam.emit_signal("steam_server_connect_failed", -1, false)
		return
	_after_session()

func _after_session() -> void:
	_session_ready = true
	_name = str(webworks.session.get("name", webworks.display_name()))
	steam.emit_signal("steam_server_connected")
	_poll = 0.0
	_poll_friends()
	_pull_stats()
	_pull_files()

func _shutdown() -> void:
	if webworks == null:
		return
	webworks.send_offline()
	webworks.stop_presence()
	_session_ready = false
	_close_relay()
	steam.emit_signal("steam_server_disconnected", 1)

func tick(delta: float) -> void:
	if webworks == null or not _session_ready:
		return
	_poll -= delta
	if _poll <= 0.0:
		_poll = POLL_SECONDS
		_poll_friends()
		_poll_invites()
		if _lobby_id != "":
			_poll_lobby()
	_pump_relay(delta)

## Runs a coroutine without waiting for it. GodotSteam's API is synchronous and callback
## driven, so each request keeps running and reports through a signal.
func _go(callable: Callable) -> void:
	callable.call()

# ------------------------------------------------------------------ identity

func _app_id_number() -> int:
	var raw := str(webworks.app_id if webworks else "0")
	return int(raw) if raw.is_valid_int() else 0

func _as_id(raw) -> int:
	var text := str(raw)
	return int(text) if text.is_valid_int() else 0

func _as_id_string(raw) -> String:
	if raw is int:
		return str(raw)
	return str(raw)

func _set_persona_name(name: String) -> bool:
	if name == "":
		return false
	_name = name
	webworks.persona_name = name
	_go(_push_persona.bind(name))
	return true

func _push_persona(name: String) -> void:
	await webworks.set_persona("Online", name)

func _auth_ticket() -> String:
	# The server hands out session tokens, and that token already identifies this player
	# to every Webworks route, so it doubles as the auth ticket.
	return str(webworks.account_token if webworks.has_account() else webworks.session_token)

# ------------------------------------------------------------------ friends

func _poll_friends() -> void:
	if webworks == null or not _session_ready:
		return
	_go(_fetch_friends)

func _fetch_friends() -> void:
	var reply: Dictionary = await webworks.get_json(Routes.FRIENDS["graph"])
	if not reply.get("ok", false) or not (reply.get("data") is Dictionary):
		return
	_graph = reply["data"]
	var accepted: Array = _graph.get("accepted", [])
	var order: Array = []
	var friends: Dictionary = {}
	for entry in accepted:
		if not (entry is Dictionary):
			continue
		var id := str(entry.get("steamId", ""))
		if id == "":
			continue
		order.append(id)
		friends[id] = {
			"name": str(entry.get("name", entry.get("displayName", ""))),
			"state": str(entry.get("personaState", "Offline")),
			"game": entry.get("game", null),
		}
		var previous := str(_friend_states.get(id, ""))
		var current := str(friends[id]["state"])
		if previous != current:
			_friend_states[id] = current
			steam.emit_signal("persona_state_change", _as_id(id), 3 if current == "Online" else 0)
		if friends[id]["game"] != null:
			steam.emit_signal("friend_rich_presence_update", _as_id(id), _app_id_number())
	_friends = friends
	_friend_order = order
	steam.emit_signal("friends_refresh_complete")

func _friend_at(index: int) -> int:
	if index < 0 or index >= _friend_order.size():
		return 0
	return _as_id(_friend_order[index])

func _friend_entry(steam_id) -> Dictionary:
	return _friends.get(_as_id_string(int(steam_id)), {})

func _friend_value(steam_id, key: String, fallback):
	var entry := _friend_entry(steam_id)
	return entry.get(key, fallback)

func _friend_persona_state(steam_id) -> int:
	var state := str(_friend_value(steam_id, "state", "Offline"))
	match state:
		"Online":
			return 1
		"Away":
			return 3
		"Busy":
			return 2
		"Snooze":
			return 4
		"Invisible":
			return 5
	return 0

func _friend_game(steam_id) -> Dictionary:
	var game = _friend_value(steam_id, "game", null)
	if game is Dictionary and not game.is_empty():
		return {
			"id": _app_id_number(),
			"ip": "0",
			"game_port": 0,
			"query_port": 0,
			"lobby": _as_id(str(game.get("lobbyId", "0"))),
		}
	return {}

func _avatar_handle(steam_id) -> int:
	return 0

func _set_rich_presence(key: String, value: String) -> bool:
	if key == "":
		return false
	if not _rich.has(key) and _rich.size() >= RICH_PRESENCE_KEY_MAX:
		return false
	_rich[key] = value
	_go(_push_rich.bind(key, value))
	return true

func _push_rich(key: String, value: String) -> void:
	await webworks.post_json(Routes.FRIENDS["rich_presence"], {"key": key, "value": value})

func _clear_rich_presence() -> void:
	_rich.clear()
	_go(_push_rich_clear)

func _push_rich_clear() -> void:
	await webworks.request("DELETE", Routes.FRIENDS["rich_presence"])

# ------------------------------------------------------------------ lobbies

func _create_lobby(lobby_type: int, max_members: int) -> void:
	_go(_op_create_lobby.bind(max_members))

func _op_create_lobby(max_members: int) -> void:
	var reply: Dictionary = await webworks.post_json(Routes.MATCHMAKING["create_lobby"], {"maxMembers": maxi(2, max_members)})
	if not reply.get("ok", false):
		steam.emit_signal("lobby_created", 2, 0)
		return
	var data = reply.get("data")
	if data is Dictionary:
		_lobby_id = str(data.get("id", ""))
		_lobby = data
		steam.emit_signal("lobby_created", 1, _as_id(_lobby_id))
		_ensure_relay()

func _join_lobby(lobby_id: String) -> void:
	if lobby_id == "":
		return
	_go(_op_join_lobby.bind(lobby_id))

func _op_join_lobby(lobby_id: String) -> void:
	var reply: Dictionary = await webworks.post_json(Routes.MATCHMAKING["join_lobby"], {"lobbyId": lobby_id})
	var joined: bool = reply.get("ok", false)
	if joined:
		_lobby_id = str((reply.get("data") or {}).get("id", lobby_id))
		_lobby = reply.get("data") or {}
		steam.emit_signal("lobby_joined", _as_id(_lobby_id), 0, false, 1)
		_ensure_relay()
		_poll_lobby()
		return
	steam.emit_signal("lobby_joined", _as_id(lobby_id), 0, true, 2)

func _leave_lobby(lobby_id: String) -> void:
	var id := lobby_id if lobby_id != "" else _lobby_id
	if id == "":
		return
	_lobby_id = ""
	_lobby = {}
	_close_relay()
	_go(_op_leave_lobby.bind(id))

func _op_leave_lobby(lobby_id: String) -> void:
	await webworks.post_json(Routes.MATCHMAKING["leave_lobby"], {"lobbyId": lobby_id})

func _request_lobby_list() -> void:
	_go(_op_lobby_list)

func _op_lobby_list() -> void:
	var reply: Dictionary = await webworks.post_json(Routes.MATCHMAKING["lobby_list"], {"max": 50, "slots": 1})
	var ids: Array = []
	var data = reply.get("data")
	if reply.get("ok", false) and data is Dictionary:
		for lobby in data.get("lobbies", []):
			if lobby is Dictionary:
				ids.append(_as_id(str(lobby.get("id", "0"))))
	steam.emit_signal("lobby_match_list", ids)

func _poll_lobby() -> void:
	if _lobby_id == "":
		return
	_go(_op_lobby_snapshot)

func _op_lobby_snapshot() -> void:
	var reply: Dictionary = await webworks.get_json(Routes.MATCHMAKING["lobby"])
	if not reply.get("ok", false):
		return
	var data = reply.get("data")
	if not (data is Dictionary):
		return
	var previous: Dictionary = _lobby
	_lobby = data
	if str(previous.get("id", "")) == "":
		return
	var previous_data: Dictionary = previous.get("data", {})
	var current_data: Dictionary = _lobby_data()
	for key in current_data.keys():
		if str(current_data[key]) != str(previous_data.get(key, "")):
			steam.emit_signal("lobby_data_update", 1, _as_id(_lobby_id), 0)
			break
	if previous.get("ownerId", "") != _lobby.get("ownerId", ""):
		steam.emit_signal("lobby_data_update", 1, _as_id(_lobby_id), _as_id(str(_lobby.get("ownerId", "0"))))
	var before: Array = previous.get("members", [])
	var after: Array = _lobby.get("members", [])
	if before.size() != after.size():
		steam.emit_signal("lobby_chat_update", _as_id(_lobby_id), 0, _as_id(str(_lobby.get("ownerId", "0"))), 1)

func _lobby_data() -> Dictionary:
	var data = _lobby.get("data", {})
	return data if data is Dictionary else {}

func _lobby_members() -> Array:
	var members = _lobby.get("members", [])
	return members if members is Array else []

func _lobby_member_at(index: int) -> int:
	var members := _lobby_members()
	if index < 0 or index >= members.size():
		return 0
	var entry = members[index]
	return _as_id(str(entry.get("steamId", "0"))) if entry is Dictionary else 0

func _member_data(steam_id, key: String) -> String:
	var member_data = _lobby.get("memberData", {})
	if not (member_data is Dictionary):
		return ""
	var entry = member_data.get(_as_id_string(int(steam_id)), {})
	if not (entry is Dictionary):
		return ""
	return str(entry.get(key, ""))

func _lobby_set(key: String, value: String) -> void:
	var data := _lobby_data().duplicate()
	data[key] = value
	_lobby["data"] = data
	_lobby_post(Routes.MATCHMAKING["lobby_data"], {"data": {key: value}})

func _set_member_data(key: String, value: String) -> void:
	_lobby_post(Routes.MATCHMAKING["member_data"], {"key": key, "value": value})

func _lobby_post(path: String, body: Dictionary) -> void:
	if _lobby_id == "":
		return
	var payload := body.duplicate()
	payload["lobbyId"] = _lobby_id
	_go(_op_lobby_post.bind(path, payload))

func _op_lobby_post(path: String, body: Dictionary) -> void:
	await webworks.post_json(path, body)

func _send_lobby_chat(message: String) -> bool:
	if _lobby_id == "" or message == "":
		return false
	_go(_op_lobby_chat.bind(message))
	return true

func _op_lobby_chat(message: String) -> void:
	var reply: Dictionary = await webworks.post_json(Routes.MATCHMAKING["chat_send"], {
		"lobbyId": _lobby_id,
		"message": message,
	})
	if reply.get("ok", false):
		steam.emit_signal("lobby_message", _as_id(_lobby_id), _as_id(webworks.steam_id()), message, 0)

func _invite_to_lobby(steam_id: String) -> bool:
	if _lobby_id == "" or steam_id == "":
		return false
	_go(_op_invite_to_lobby.bind(steam_id))
	return true

func _op_invite_to_lobby(steam_id: String) -> void:
	await webworks.post_json(Routes.MATCHMAKING["invite_friend"], {
		"lobbyId": _lobby_id,
		"friendSteamId": steam_id,
	})

func _visibility_name(lobby_type: int) -> String:
	match lobby_type:
		2:
			return "private"
		3:
			return "invisible"
	return "public"

func _visibility_id(visibility: String) -> int:
	match visibility:
		"private":
			return 2
		"invisible":
			return 3
	return 1

# Invites arrive from either engine: a friend on another node server can invite this
# player, and the poll turns each new invite into GodotSteam's lobby_invite signal.
func _poll_invites() -> void:
	_go(_fetch_invites)

func _fetch_invites() -> void:
	var reply: Dictionary = await webworks.get_json(Routes.MATCHMAKING["invites"])
	var data = reply.get("data")
	if not reply.get("ok", false) or not (data is Dictionary):
		return
	for invite in data.get("invites", []):
		if not (invite is Dictionary):
			continue
		var lobby := str(invite.get("lobbyId", invite.get("id", "")))
		if lobby == "" or _invite_seen.has(lobby):
			continue
		_invite_seen[lobby] = true
		steam.emit_signal("lobby_invite", _as_id(str(invite.get("fromSteamId", invite.get("from", "0")))), _as_id(lobby), _app_id_number())

# ------------------------------------------------------------------ stats

func _pull_stats() -> void:
	_go(_op_pull_stats)

func _op_pull_stats() -> void:
	var achievements_reply: Dictionary = await webworks.get_json(Routes.USER_STATS["achievements"])
	var data = achievements_reply.get("data")
	if achievements_reply.get("ok", false) and data is Dictionary:
		for entry in data.get("achievements", []):
			if not (entry is Dictionary):
				continue
			var key := str(entry.get("key", entry.get("id", "")))
			if key == "":
				continue
			_achievements[key] = {
				"unlocked": bool(entry.get("unlocked", false)),
				"unlockTime": int(entry.get("unlockTime", 0)),
				"progress": int(entry.get("progress", 0)),
				"maxProgress": int(entry.get("maxProgress", 0)),
			}

	var catalog_reply: Dictionary = await webworks.get_json(Routes.USER_STATS["achievements_catalog"])
	var catalog_data = catalog_reply.get("data")
	if catalog_reply.get("ok", false) and catalog_data is Dictionary:
		var list: Array = []
		for entry in catalog_data.get("achievements", []):
			if entry is Dictionary:
				list.append(entry)
		_catalog = list

	var stats_reply: Dictionary = await webworks.get_json(Routes.USER_STATS["stats"])
	var stats_data = stats_reply.get("data")
	if stats_reply.get("ok", false) and stats_data is Dictionary:
		var stats = stats_data.get("stats", {})
		if stats is Dictionary:
			_stats = stats.duplicate()

func _achievement(name: String) -> Dictionary:
	var entry = _achievements.get(name, {})
	return entry if entry is Dictionary else {}

func _unlocked(name: String) -> bool:
	return bool(_achievement(name).get("unlocked", false))

func _unlock_achievement(name: String) -> bool:
	if name == "":
		return false
	var entry := _achievement(name).duplicate()
	entry["unlocked"] = true
	entry["unlockTime"] = int(Time.get_unix_time_from_system())
	_achievements[name] = entry
	if not _pending_unlocks.has(name):
		_pending_unlocks.append(name)
	_stats_dirty = true
	return true

func _clear_achievement(name: String) -> bool:
	if name == "":
		return false
	_pending_unlocks.erase(name)
	if not _pending_clears.has(name):
		_pending_clears.append(name)
	var entry := _achievement(name).duplicate()
	entry["unlocked"] = false
	entry["unlockTime"] = 0
	_achievements[name] = entry
	_stats_dirty = true
	return true

func _achievement_progress(name: String, current: int, max_progress: int) -> void:
	var entry := _achievement(name).duplicate()
	entry["progress"] = current
	entry["maxProgress"] = max_progress
	if max_progress > 0 and current >= max_progress:
		entry["unlocked"] = true
		entry["unlockTime"] = int(Time.get_unix_time_from_system())
	_achievements[name] = entry
	_go(_op_achievement_progress.bind(name, current, max_progress))

func _op_achievement_progress(name: String, current: int, max_progress: int) -> void:
	await webworks.post_json(Routes.USER_STATS["achievement_progress"], {
		"name": name,
		"current": current,
		"max": max_progress,
	})

func _achievement_attribute(name: String, key: String) -> String:
	for entry in _catalog:
		if str(entry.get("key", "")) != name:
			continue
		match key:
			"name":
				return str(entry.get("name", name))
			"desc":
				return str(entry.get("description", ""))
			"hidden":
				return "1" if bool(entry.get("hidden", false)) else "0"
	return ""

## GodotSteam batches: setStat/setAchievement only touch memory and storeStats sends the
## batch. The same single POST carries stats, unlocks and clears so a game that calls
## storeStats once per session still costs one request.
func _store_stats() -> bool:
	if _stats_dirty:
		_stats_dirty = false
		_go(_op_store_stats)
		steam.emit_signal("user_stats_stored", _app_id_number(), 1)
	return true

func _op_store_stats() -> void:
	var unlocked := _pending_unlocks.duplicate()
	var cleared := _pending_clears.duplicate()
	_pending_unlocks.clear()
	_pending_clears.clear()
	await webworks.post_json(Routes.USER_STATS["stats"], {
		"stats": _stats,
		"unlocked": unlocked,
		"cleared": cleared,
	})

func _reset_stats(achievements_too: bool) -> void:
	_stats.clear()
	if achievements_too:
		for key in _achievements.keys():
			_achievements[key] = {"unlocked": false, "unlockTime": 0, "progress": 0, "maxProgress": 0}
		_pending_unlocks.clear()
	_stats_dirty = false
	_go(_op_reset_stats.bind(achievements_too))

func _op_reset_stats(achievements_too: bool) -> void:
	await webworks.post_json(Routes.USER_STATS["reset"], {"achievements": achievements_too})

func _pull_global_stats(days: int) -> void:
	_go(_op_global_stats.bind(days))

func _op_global_stats(days: int) -> void:
	var reply: Dictionary = await webworks.get_json(Routes.USER_STATS["global_stats"])
	if reply.get("ok", false):
		steam.emit_signal("global_stats_received", _app_id_number(), 1)

# ------------------------------------------------------------------ leaderboards

func _find_leaderboard(name: String, sort: int, display: int, create: bool) -> void:
	_go(_op_find_leaderboard.bind(name, sort, display, create))

func _op_find_leaderboard(name: String, sort: int, display: int, create: bool) -> void:
	var path: String = Routes.USER_STATS["leaderboard_find_or_create"] if create else Routes.USER_STATS["leaderboard_find"]
	var reply: Dictionary
	if create:
		reply = await webworks.post_json(path, {"name": name, "sort": _sort_name(sort), "display": _display_name(display)})
	else:
		reply = await webworks.get_json(path + "?name=" + name.uri_encode())
	var data = reply.get("data")
	var found := false
	if reply.get("ok", false) and data is Dictionary:
		found = bool((data as Dictionary).get("found", false))
	var handle := 0
	if found:
		handle = _handle_for(name)
		_board_counts[handle] = int((data as Dictionary).get("entryCount", 0))
	steam.emit_signal("leaderboard_find_result", handle, 1 if found else 0)

func _handle_for(name: String) -> int:
	if _boards.has(name):
		return int(_boards[name])
	var handle := _next_handle
	_next_handle += 1
	_boards[name] = handle
	_board_by_handle[handle] = name
	return handle

func _sort_name(sort: int) -> String:
	return "ascending" if sort == 2 else "descending"

func _display_name(display: int) -> String:
	match display:
		2:
			return "time_seconds"
		3:
			return "time_milliseconds"
	return "numeric"

func _upload_score(score: int, keep_best: bool, details, handle: int) -> void:
	var name := str(_board_by_handle.get(handle, ""))
	if name == "":
		steam.emit_signal("leaderboard_score_uploaded", false, handle, {})
		return
	var detail_values: Array = []
	if details is PackedInt32Array or details is Array:
		for value in details:
			detail_values.append(int(value))
	_go(_op_upload_score.bind(name, score, keep_best, detail_values, handle))

func _op_upload_score(name: String, score: int, keep_best: bool, details: Array, handle: int) -> void:
	var reply: Dictionary = await webworks.post_json(Routes.USER_STATS["leaderboard_submit"], {
		"name": name,
		"score": score,
		"details": details,
		"forceUpdate": not keep_best,
	})
	var success: bool = reply.get("ok", false)
	var data = reply.get("data")
	var report := {"score": score, "details": details, "rank": 0}
	if data is Dictionary:
		report["rank"] = int(data.get("rank", data.get("globalRank", 0)))
	steam.emit_signal("leaderboard_score_uploaded", success, handle, report)

func _download_entries(handle: int, range_start: int, range_end: int) -> void:
	var name := str(_board_by_handle.get(handle, ""))
	if name == "":
		steam.emit_signal("leaderboard_scores_downloaded", "", handle, [])
		return
	_go(_op_download_entries.bind(name, handle, range_start, range_end))

func _op_download_entries(name: String, handle: int, range_start: int, range_end: int) -> void:
	var query := "?name=%s&start=%d&end=%d" % [name.uri_encode(), range_start, range_end]
	var reply: Dictionary = await webworks.get_json(Routes.USER_STATS["leaderboard_entries"] + query)
	var entries: Array = []
	var data = reply.get("data")
	if reply.get("ok", false) and data is Dictionary:
		_board_counts[handle] = int(data.get("entryCount", 0))
		for entry in data.get("entries", []):
			if not (entry is Dictionary):
				continue
			entries.append({
				"score": int(entry.get("score", 0)),
				"steam_id": _as_id(str(entry.get("steamId", "0"))),
				"global_rank": int(entry.get("globalRank", 0)),
				"details": entry.get("details", []),
			})
	steam.emit_signal("leaderboard_scores_downloaded", "", handle, entries)

# ------------------------------------------------------------------ remote storage

func _pull_files() -> void:
	_go(_op_pull_files)

func _op_pull_files() -> void:
	var reply: Dictionary = await webworks.get_json(Routes.REMOTE_STORAGE["files"])
	var data = reply.get("data")
	if not reply.get("ok", false) or not (data is Dictionary):
		return
	_quota_total = int(data.get("quota", _quota_total))
	_quota_used = int(data.get("used", _quota_used))
	var files := {}
	for entry in data.get("files", []):
		if not (entry is Dictionary):
			continue
		var file_name := str(entry.get("name", ""))
		if file_name == "":
			continue
		var bytes := Marshalls.base64_to_raw(str(entry.get("data", "")))
		files[file_name] = {"size": bytes.size(), "data": bytes, "time": int(entry.get("time", 0))}
	_files = files

func _file_entry(name: String) -> Dictionary:
	var entry = _files.get(name, {})
	return entry if entry is Dictionary else {}

func _file_write(name: String, data) -> bool:
	if name == "" or not (data is PackedByteArray):
		return false
	var bytes: PackedByteArray = data
	_files[name] = {"size": bytes.size(), "data": bytes, "time": int(Time.get_unix_time_from_system())}
	_go(_op_file_write.bind(name, bytes))
	return true

func _op_file_write(name: String, bytes: PackedByteArray) -> void:
	await webworks.post_json(Routes.REMOTE_STORAGE["write"], {
		"name": name,
		"data": Marshalls.raw_to_base64(bytes),
	})

func _file_read(name: String, size: int) -> Dictionary:
	var entry := _file_entry(name)
	if entry.is_empty():
		return {"ret": false, "buf": PackedByteArray()}
	var bytes: PackedByteArray = entry.get("data", PackedByteArray())
	if size > 0 and size < bytes.size():
		bytes = bytes.slice(0, size)
	return {"ret": true, "buf": bytes}

func _file_delete(name: String) -> bool:
	if not _files.has(name):
		return false
	_files.erase(name)
	_go(_op_file_delete.bind(name))
	return true

func _op_file_delete(name: String) -> void:
	await webworks.post_json(Routes.REMOTE_STORAGE["delete"], {"name": name})

# ------------------------------------------------------------------ overlay

func _show_overlay(tab: String) -> void:
	_overlay_tab = tab.to_lower()
	_go(_open_overlay)

func _open_overlay() -> void:
	var tree := steam.get_tree() if steam else null
	if tree == null:
		return
	var root := tree.current_scene
	if root == null:
		return
	var existing := root.get_node_or_null("WebworksSteamOverlay")
	if existing == null:
		if not ResourceLoader.exists(OVERLAY_SCENE):
			overlay_requested.emit(_overlay_tab)
			return
		existing = load(OVERLAY_SCENE).instantiate()
		existing.name = "WebworksSteamOverlay"
		root.add_child(existing)
	if existing.has_method("open"):
		existing.open(_overlay_tab)
	else:
		existing.visible = true

func _overlay_open() -> bool:
	var tree := steam.get_tree() if steam else null
	if tree == null or tree.current_scene == null:
		return false
	return tree.current_scene.get_node_or_null("WebworksSteamOverlay") != null

# ------------------------------------------------------------------ p2p packets

## P2P runs over the server's relay worker: the lobby hands out a signed ticket, the
## players open their own WebSocket straight to the relay, and packets go peer to peer
## through it. The lobby server never sees the payloads and nothing is stored, so a
## long play session costs the hosted worker nothing.
func _ensure_relay() -> void:
	if _relay != null or _lobby_id == "":
		return
	_go(_op_connect_relay)

func _op_connect_relay() -> void:
	var reply: Dictionary = await webworks.post_json(Routes.NETWORKING["relay_ticket"], {"lobbyId": _lobby_id})
	var data = reply.get("data")
	if not reply.get("ok", false) or not (data is Dictionary):
		_relay_retry = RELAY_RETRY_SECONDS
		return
	var url := str(data.get("wsUrl", ""))
	var ticket := str(data.get("ticket", ""))
	if url == "" or ticket == "":
		_relay_retry = RELAY_RETRY_SECONDS
		return

	var socket := WebSocketPeer.new()
	socket.outbound_buffer_size = 4 * 1024 * 1024
	socket.inbound_buffer_size = 4 * 1024 * 1024
	var err := socket.connect_to_url(url)
	if err != OK:
		_relay_retry = RELAY_RETRY_SECONDS
		return
	_relay = socket
	_relay_lobby = _lobby_id
	_relay_ready = false
	_relay_ticket = ticket

func _pump_relay(delta: float) -> void:
	if _relay == null:
		if _relay_retry > 0.0:
			_relay_retry -= delta
			if _relay_retry <= 0.0 and _lobby_id != "":
				_ensure_relay()
		return

	_relay.poll()
	match _relay.get_ready_state():
		WebSocketPeer.STATE_OPEN:
			if not _relay_ready:
				_relay.send_text(JSON.stringify({"type": "hello", "ticket": _relay_ticket}))
				_relay_ready = true
			_flush_outbox()
			while _relay.get_available_packet_count() > 0:
				_read_relay_frame(_relay.get_packet().get_string_from_utf8())
		WebSocketPeer.STATE_CLOSED:
			_close_relay()

func _read_relay_frame(raw: String) -> void:
	var parsed = JSON.parse_string(raw)
	if not (parsed is Dictionary):
		return
	var frame: Dictionary = parsed
	match str(frame.get("type", "")):
		"welcome":
			_relay_ready = true
			_flush_outbox()
		"packet":
			var from := str(frame.get("from", "0"))
			var channel := int(frame.get("channel", 0))
			var bytes := Marshalls.base64_to_raw(str(frame.get("data", "")))
			if not _relay_inbox.has(channel):
				_relay_inbox[channel] = []
			_relay_inbox[channel].append({"from": from, "data": bytes})
			if not _relay_peers.has(from):
				_relay_add_peer(from)
				steam.emit_signal("p2p_session_request", _as_id(from))

func _relay_add_peer(steam_id: String) -> void:
	if steam_id != "" and steam_id != "0":
		_relay_peers[steam_id] = true

func _send_packet(remote_steam_id: int, data, send_type: int, channel: int) -> bool:
	if _lobby_id == "" or not (data is PackedByteArray):
		return false
	var target := _as_id_string(remote_steam_id)
	if target == "0" or target == "":
		return false
	_ensure_relay()
	_relay_outbox.append({
		"to": target,
		"channel": channel,
		"reliability": send_type,
		"data": Marshalls.raw_to_base64(data),
	})
	if _relay_ready:
		_flush_outbox()
	return true

func _flush_outbox() -> void:
	if _relay == null or not _relay_ready:
		return
	while not _relay_outbox.is_empty():
		var frame: Dictionary = _relay_outbox.pop_front()
		_relay.send_text(JSON.stringify({
			"type": "send",
			"to": frame["to"],
			"channel": frame["channel"],
			"reliability": frame["reliability"],
			"data": frame["data"],
		}))

func _inbox(channel: int) -> Array:
	var queue = _relay_inbox.get(channel, [])
	return queue if queue is Array else []

func _read_packet(channel: int) -> Dictionary:
	var queue := _inbox(channel)
	if queue.is_empty():
		return {}
	var packet: Dictionary = queue.pop_front()
	return {
		"data": packet.get("data", PackedByteArray()),
		"remote_steam_id": _as_id(str(packet.get("from", "0"))),
	}

func _close_relay() -> void:
	if _relay != null:
		_relay.close()
	_relay = null
	_relay_ready = false
	_relay_lobby = ""
	_relay_outbox.clear()
	_relay_inbox.clear()
	_relay_peers.clear()

