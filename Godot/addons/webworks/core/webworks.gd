extends Node

## Webworks for Godot.
##
## The addon replaces the Steam extension with plain HTTP against a Webworks server: an
## account (or a guest name), a game session, presence, friends and lobbies. It has no
## native dependency, so it keeps working after the GodotSteam extension is removed.
##
## Everything is async: `var reply = await Webworks.sign_in(user, pass)`.
## A reply is always a Dictionary shaped like
##     { "ok": bool, "status": int, "data": Variant, "text": String, "error": String }

signal session_started(session: Dictionary)
signal presence_changed(online: bool)
signal friends_changed(graph: Dictionary)
signal invites_changed(invites: Array)

## Routes are preloaded by path so the addon works even before the editor has
## registered its global class names (a fresh checkout, a headless export).
const Routes := preload("res://addons/webworks/core/webworks_routes.gd")

const CONFIG_PATH := "res://webworks.cfg"
const USER_CONFIG_PATH := "user://webworks.cfg"
const STATE_PATH := "user://webworks_session.cfg"

const HEARTBEAT_SECONDS := 20.0

var base_url := "http://localhost:3000"
var app_id := "0"
var engine_path := "Godot/GodotSteam"
var timeout_ms := 8000
var offline_on_exit := true

## Persona name this project asks for on a guest session. Empty means "let the server
## pick one", which is what the name entry screen does anyway.
var persona_name := ""

## The Steam shim opens the session by itself on startup. Set [steam] auto_init=false in
## webworks.cfg to call Steam.init() from your own menu instead.
var steam_auto_init := true

var session_token := ""
var account_token := ""
var session: Dictionary = {}
var account: Dictionary = {}
var lobby_id := ""

var _heartbeat: Timer
var _heartbeat_running := false

func _ready() -> void:
	_load_config()
	_load_state()

	_heartbeat = Timer.new()
	_heartbeat.wait_time = HEARTBEAT_SECONDS
	_heartbeat.one_shot = false
	_heartbeat.autostart = false
	add_child(_heartbeat)
	_heartbeat.timeout.connect(_tick_presence)

func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_CLOSE_REQUEST and offline_on_exit:
		send_offline()

# ------------------------------------------------------------------ configuration

func _load_config() -> void:
	var config := ConfigFile.new()
	var path := CONFIG_PATH
	if FileAccess.file_exists(USER_CONFIG_PATH):
		path = USER_CONFIG_PATH
	if config.load(path) != OK:
		return

	base_url = str(config.get_value("webworks", "server_url", base_url)).strip_edges().trim_suffix("/")
	app_id = str(config.get_value("webworks", "app_id", app_id))
	engine_path = _normalize_engine(str(config.get_value("webworks", "engine_path", engine_path)))
	timeout_ms = int(config.get_value("webworks", "timeout_ms", timeout_ms))
	persona_name = str(config.get_value("webworks", "persona_name", persona_name))
	steam_auto_init = bool(config.get_value("steam", "auto_init", steam_auto_init))

static func _normalize_engine(value: String) -> String:
	var engine := value.strip_edges().trim_prefix("/").trim_suffix("/")
	if engine.begins_with("api/"):
		engine = engine.substr(4)
	return engine

func _load_state() -> void:
	var config := ConfigFile.new()
	if config.load(STATE_PATH) != OK:
		return
	session_token = str(config.get_value("session", "token", ""))
	account_token = str(config.get_value("account", "token", ""))
	account = {
		"username": str(config.get_value("account", "username", "")),
		"display_name": str(config.get_value("account", "display_name", "")),
	}
	session = {
		"steam_id": str(config.get_value("session", "steam_id", "")),
		"name": str(config.get_value("session", "name", "")),
		"app_id": str(config.get_value("session", "app_id", "")),
	}

func _save_state() -> void:
	var config := ConfigFile.new()
	config.set_value("session", "token", session_token)
	config.set_value("session", "steam_id", str(session.get("steam_id", "")))
	config.set_value("session", "name", str(session.get("name", "")))
	config.set_value("session", "app_id", str(session.get("app_id", "")))
	config.set_value("account", "token", account_token)
	config.set_value("account", "username", str(account.get("username", "")))
	config.set_value("account", "display_name", str(account.get("display_name", "")))
	config.save(STATE_PATH)

func has_account() -> bool:
	return account_token != ""

func has_session() -> bool:
	return session_token != ""

func display_name() -> String:
	return str(account.get("display_name", "")) if has_account() else str(session.get("name", ""))

func steam_id() -> String:
	return str(session.get("steam_id", ""))

# ------------------------------------------------------------------ transport

func mounted(path: String) -> String:
	return "/api/" + engine_path + path if engine_path != "" else "/api" + path

func get_json(path: String) -> Dictionary:
	return await request("GET", path)

func post_json(path: String, body: Variant = {}) -> Dictionary:
	return await request("POST", path, body)

## Walks this engine mount and then the historical ones, so a server configured for an
## older shim still answers. Only a 404/405 without JSON counts as "wrong mount".
func request(method: String, path: String, body: Variant = null, use_account_token := false) -> Dictionary:
	var mounts: Array = [engine_path]
	for mount in Routes.ENGINE_MOUNTS:
		if not mounts.has(mount):
			mounts.append(mount)

	var reply := {}
	for mount in mounts:
		reply = await _request_at(str(mount), method, path, body, use_account_token)
		if not _route_missing(reply):
			return reply
	return reply

func _request_at(mount: String, method: String, path: String, body: Variant, use_account_token: bool) -> Dictionary:
	var http := HTTPRequest.new()
	http.timeout = float(timeout_ms) / 1000.0
	add_child(http)

	var headers := PackedStringArray(["Accept: application/json"])
	var token := account_token if use_account_token else session_token
	if token != "":
		headers.append("x-webworks-session: " + token)
		if use_account_token:
			headers.append("x-webworks-account: " + token)

	var payload := ""
	if body != null:
		headers.append("Content-Type: application/json")
		payload = JSON.stringify(body)

	var url := base_url + ("/api/" + mount if mount != "" else "/api") + path
	var verb := HTTPClient.METHOD_GET
	match method.to_upper():
		"POST": verb = HTTPClient.METHOD_POST
		"PATCH": verb = HTTPClient.METHOD_PATCH
		"PUT": verb = HTTPClient.METHOD_PUT
		"DELETE": verb = HTTPClient.METHOD_DELETE

	var err := http.request(url, headers, verb, payload)
	if err != OK:
		http.queue_free()
		return {"ok": false, "status": 0, "data": null, "text": "", "error": "transport_error", "url": url}

	var result: Array = await http.request_completed
	http.queue_free()

	var code := int(result[1])
	var bytes: PackedByteArray = result[3]
	var text := bytes.get_string_from_utf8()
	var data: Variant = null
	if text != "":
		data = JSON.parse_string(text)

	var reply := {"ok": code >= 200 and code < 300, "status": code, "data": data, "text": text, "url": url}
	if not reply["ok"]:
		reply["error"] = _error_code(data, code)
	return reply

static func _error_code(data: Variant, code: int) -> String:
	if data is Dictionary:
		var dict: Dictionary = data
		if dict.has("error"):
			return str(dict["error"])
		if dict.has("code"):
			return str(dict["code"])
	return "http_" + str(code)

static func _route_missing(reply: Dictionary) -> bool:
	var code := int(reply.get("status", 0))
	if code != 404 and code != 405:
		return false
	var text := str(reply.get("text", "")).strip_edges()
	return text == "" or not text.begins_with("{")

# ------------------------------------------------------------------ accounts

func account_request(method: String, path: String, body: Variant = null) -> Dictionary:
	return await request(method, path, body, true)

func sign_in(username: String, password: String) -> Dictionary:
	var reply := await account_request("POST", Routes.ACCOUNTS["login"], {"username": username, "password": password})
	return await _apply_account(reply, username)

func register(username: String, password: String, display_name := "", avatar_base64 := "") -> Dictionary:
	var body := {"username": username, "password": password}
	if display_name != "":
		body["displayName"] = display_name
	if avatar_base64 != "":
		body["avatarBase64"] = avatar_base64
	var reply := await account_request("POST", Routes.ACCOUNTS["register"], body)
	return await _apply_account(reply, username)

func _apply_account(reply: Dictionary, username: String) -> Dictionary:
	if not reply.get("ok", false):
		return reply
	var data: Variant = reply.get("data")
	var token := ""
	var profile: Dictionary = {}
	if data is Dictionary:
		token = str(data.get("token", ""))
		if data.get("account") is Dictionary:
			profile = data["account"]
	if token == "":
		return {"ok": false, "status": int(reply.get("status", 0)), "data": data, "error": "invalid_upstream_response"}

	account_token = token
	account = {
		"username": str(profile.get("username", username)),
		"display_name": str(profile.get("displayName", username)),
	}
	_save_state()
	return await start_account_session()

## Turns the stored account token into a game session. The friends page and every lobby
## route authenticate with that session, exactly like the engine shims do.
func start_account_session() -> Dictionary:
	if account_token == "":
		return {"ok": false, "status": 0, "error": "not_signed_in"}
	return await guest_session(str(account.get("display_name", "")), account_token)

func guest_session(name: String, with_account_token := "") -> Dictionary:
	var body := {"appId": app_id, "name": name}
	var token := with_account_token if with_account_token != "" else account_token
	if token != "":
		body["accountToken"] = token

	var reply := await post_json(Routes.USER["guest_session"], body)
	if not reply.get("ok", false):
		return reply

	var data: Variant = reply.get("data")
	if not (data is Dictionary):
		return {"ok": false, "status": int(reply.get("status", 0)), "error": "invalid_upstream_response"}

	var info: Dictionary = data
	session_token = str(info.get("sessionToken", ""))
	session = {
		"steam_id": str(info.get("steamId", "")),
		"name": str(info.get("name", name)),
		"app_id": str(info.get("appId", app_id)),
	}
	_save_state()
	begin_presence()
	session_started.emit(session)
	return reply

func sign_out() -> void:
	stop_presence()
	if account_token != "":
		await account_request("POST", Routes.ACCOUNTS["logout"], {})
	session_token = ""
	account_token = ""
	session = {}
	account = {}
	lobby_id = ""
	_save_state()

func verify_session() -> bool:
	if session_token == "":
		return false
	var reply := await get_json(Routes.USER["profile"])
	return reply.get("ok", false)

# ------------------------------------------------------------------ presence

## A heartbeat is what keeps this player Online for their friends while they sit in a menu
## or move the Godot window to the background. The tab/game only needs to stay open.
func begin_presence() -> void:
	if session_token == "":
		return
	_heartbeat_running = true
	if _heartbeat and _heartbeat.is_stopped():
		_heartbeat.start()
	_send_presence(Routes.USER["heartbeat"])

func stop_presence() -> void:
	_heartbeat_running = false
	if _heartbeat:
		_heartbeat.stop()

func _tick_presence() -> void:
	if not _heartbeat_running:
		return
	_send_presence(Routes.USER["heartbeat"])

func send_offline() -> void:
	_send_presence(Routes.USER["offline"])

## Presence only needs to arrive, never to answer, so this one is fire-and-forget: the
## timer and the window-close notification must not wait on the network.
func _send_presence(path: String) -> void:
	if session_token == "":
		return
	var http := HTTPRequest.new()
	http.timeout = float(timeout_ms) / 1000.0
	add_child(http)
	http.request_completed.connect(func(_result, _code, _headers, _body) -> void: http.queue_free())
	var headers := PackedStringArray([
		"Accept: application/json",
		"Content-Type: application/json",
		"x-webworks-session: " + session_token,
	])
	var url := base_url + mounted(path)
	var err := http.request(url, headers, HTTPClient.METHOD_POST, "{}")
	if err != OK:
		http.queue_free()

func set_persona(persona_state: String, persona_name := "") -> Dictionary:
	var body := {"personaState": persona_state}
	if persona_name != "":
		body["name"] = persona_name
	return await post_json(Routes.USER["persona_state"], body)

# ------------------------------------------------------------------ friends

func friends_graph() -> Dictionary:
	var reply := await get_json(Routes.FRIENDS["graph"])
	if reply.get("ok", false) and reply.get("data") is Dictionary:
		friends_changed.emit(reply["data"])
	return reply

func add_friend(username: String) -> Dictionary:
	return await post_json(Routes.FRIENDS["add"], {"username": username})

func accept_friend(username: String) -> Dictionary:
	return await post_json(Routes.FRIENDS["accept"], {"username": username})

func remove_friend(username: String) -> Dictionary:
	return await post_json(Routes.FRIENDS["remove"], {"username": username})

func invites() -> Dictionary:
	var reply := await get_json(Routes.MATCHMAKING["invites"])
	if reply.get("ok", false) and reply.get("data") is Dictionary:
		invites_changed.emit(reply["data"].get("invites", []))
	return reply

# ------------------------------------------------------------------ lobbies

func create_lobby(max_members := 8) -> Dictionary:
	var reply := await post_json(Routes.MATCHMAKING["create_lobby"], {"maxMembers": max_members})
	if reply.get("ok", false) and reply.get("data") is Dictionary:
		lobby_id = str(reply["data"].get("id", ""))
	return reply

func join_lobby(id: String) -> Dictionary:
	var reply := await post_json(Routes.MATCHMAKING["join_lobby"], {"lobbyId": id})
	if reply.get("ok", false) and reply.get("data") is Dictionary:
		lobby_id = str(reply["data"].get("id", id))
		await dismiss_invite(lobby_id)
	return reply

func leave_lobby() -> Dictionary:
	if lobby_id == "":
		return {"ok": true, "status": 200, "data": null}
	var reply := await post_json(Routes.MATCHMAKING["leave_lobby"], {"lobbyId": lobby_id})
	lobby_id = ""
	return reply

## Opens a room on first use, then hands that friend a one-time invite. The room id stays
## in Webworks.lobby_id so the next scene can join the same room.
func invite_friend(friend_steam_id: String) -> Dictionary:
	if friend_steam_id == "":
		return {"ok": false, "status": 0, "error": "missing_friend_steam_id"}
	if lobby_id == "":
		var created := await create_lobby()
		if not created.get("ok", false):
			return created
	var reply := await post_json(Routes.MATCHMAKING["invite_friend"], {
		"lobbyId": lobby_id,
		"friendSteamId": friend_steam_id,
	})
	return reply

func dismiss_invite(id: String) -> Dictionary:
	if id == "":
		return {"ok": true, "status": 200, "data": null}
	return await post_json(Routes.MATCHMAKING["invites_dismiss"], {"lobbyId": id})

# ------------------------------------------------------------------ scene flow

## Hands the player from the bootstrap to the game. The scene is chosen in the Inspector;
## nothing is hard-coded here.
func go_to_scene(scene_path: String) -> void:
	if scene_path == "":
		push_warning("[Webworks] No scene selected in the bootstrap's Scene To Load field.")
		return
	var err := get_tree().change_scene_to_file(scene_path)
	if err != OK:
		push_error("[Webworks] Could not load scene '%s' (%d)." % [scene_path, err])
