extends Control

## Webworks bootstrap: page one signs in (or continues as a guest), page two is the live
## friends list for account players. The layout lives in bootstrap.tscn; this script only
## fills it in, so the scene can be restyled in the editor without touching code.
##
## The hand-off to the game is the exported "Scene To Load" field on this node, which is
## picked in the Inspector exactly like the Unity bootstrap's Scene Asset field.

signal finished(joined_lobby_id: String)

const ROW_SCENE := preload("res://addons/webworks/ui/friends_row.tscn")
const PREFS_PATH := "user://webworks_bootstrap.cfg"

@export_file("*.tscn") var scene_to_load := ""
@export var friends_refresh_seconds := 4.0
@export var auto_join_on_invite := true

var _mode := "signin"
var _busy := false
var _signature := ""
var _invites: Array = []
var _refresh: Timer
var _live_on := false

@onready var entry_page: Control = %EntryPage
@onready var friends_page: Control = %FriendsPage

@onready var title_label: Label = %TitleLabel
@onready var account_fields: VBoxContainer = %AccountFields
@onready var display_name_row: Control = %DisplayNameRow
@onready var username_input: LineEdit = %UsernameInput
@onready var password_input: LineEdit = %PasswordInput
@onready var display_name_input: LineEdit = %DisplayNameInput
@onready var sign_in_button: Button = %SignInButton
@onready var sign_in_status: Label = %SignInStatus
@onready var mode_toggle: Button = %ModeToggle
@onready var guest_input: LineEdit = %GuestInput
@onready var guest_button: Button = %GuestButton
@onready var guest_status: Label = %GuestStatus
@onready var entry_service: Label = %EntryService

@onready var friends_who: Label = %FriendsWho
@onready var live_dot: ColorRect = %LiveDot
@onready var friend_list: VBoxContainer = %FriendList
@onready var friends_summary: Label = %FriendsSummary
@onready var add_friend_input: LineEdit = %AddFriendInput
@onready var add_friend_button: Button = %AddFriendButton
@onready var friends_status: Label = %FriendsStatus
@onready var continue_button: Button = %ContinueButton
@onready var sign_out_button: Button = %SignOutButton

func _ready() -> void:
	_refresh = Timer.new()
	_refresh.wait_time = maxf(1.5, friends_refresh_seconds)
	_refresh.one_shot = false
	_refresh.autostart = false
	add_child(_refresh)
	_refresh.timeout.connect(_on_refresh_tick)

	sign_in_button.pressed.connect(_on_sign_in)
	mode_toggle.pressed.connect(_toggle_mode)
	guest_button.pressed.connect(_on_guest)
	password_input.text_submitted.connect(func(_t): _on_sign_in())
	guest_input.text_submitted.connect(func(_t): _on_guest())
	add_friend_button.pressed.connect(_on_add_friend)
	add_friend_input.text_submitted.connect(func(_t): _on_add_friend())
	continue_button.pressed.connect(_continue_to_game)
	sign_out_button.pressed.connect(_on_sign_out)

	entry_service.text = "Account service: " + Webworks.base_url
	guest_input.text = _load_guest_name()
	if Webworks.has_session():
		await _restore_session()
	else:
		_show_entry()

func _process(delta: float) -> void:
	if live_dot == null:
		return
	var target := 1.0 if _live_on else 0.28
	live_dot.modulate.a = lerpf(live_dot.modulate.a, target, minf(1.0, delta * 4.0))

# ------------------------------------------------------------------ startup

func _restore_session() -> void:
	var still_valid := await Webworks.verify_session()
	if not still_valid:
		Webworks.session_token = ""
		_show_entry()
		return

	Webworks.begin_presence()
	if Webworks.has_account():
		_show_friends()
	else:
		_continue_to_game()

func _load_guest_name() -> String:
	var config := ConfigFile.new()
	if config.load(PREFS_PATH) != OK:
		return ""
	return str(config.get_value("bootstrap", "guest_name", ""))

func _save_guest_name(name: String) -> void:
	var config := ConfigFile.new()
	config.load(PREFS_PATH)
	config.set_value("bootstrap", "guest_name", name)
	config.save(PREFS_PATH)

# ------------------------------------------------------------------ entry page

func _show_entry() -> void:
	_refresh.stop()
	_live_on = false
	entry_page.visible = true
	friends_page.visible = false
	_apply_mode()
	if Webworks.has_account() and str(Webworks.account.get("username", "")) != "":
		username_input.text = str(Webworks.account.get("username", ""))
		password_input.placeholder_text = "Password (leave blank to continue)"
		title_label.text = "WELCOME BACK"
		sign_in_button.text = "Continue"
		_set_status(sign_in_status, "Signed in as " + str(Webworks.account.get("display_name", "")) + ". Press Continue, or type a password to switch.")

func _apply_mode() -> void:
	var register := _mode == "register"
	if not Webworks.has_account() or password_input.text != "":
		title_label.text = "CREATE ACCOUNT" if register else "SIGN IN"
		sign_in_button.text = "Create Account" if register else "Sign In"
	display_name_row.visible = register
	mode_toggle.text = "Already have an account? Sign In" if register else "Need an account? Register"
	password_input.secret = true

func _toggle_mode() -> void:
	if _busy:
		return
	_mode = "signin" if _mode == "register" else "register"
	_set_status(sign_in_status, "")
	_apply_mode()

func _on_sign_in() -> void:
	if _busy:
		return

	var username := username_input.text.strip_edges()
	var password := password_input.text

	# Cached account plus an empty password is a resume, not a new sign-in.
	if _mode == "signin" and Webworks.has_account() and password == "" \
			and (username == "" or username == str(Webworks.account.get("username", ""))):
		_set_busy(true)
		_set_status(sign_in_status, "Restoring your session...")
		var restored := await Webworks.start_account_session()
		_set_busy(false)
		if restored.get("ok", false):
			_show_friends()
		else:
			_set_status(sign_in_status, _friendly(restored), true)
		return

	if username == "" or password == "":
		_set_status(sign_in_status, "Enter your username and password.", true)
		return

	_set_busy(true)
	_set_status(sign_in_status, "Creating account..." if _mode == "register" else "Signing in...")

	var reply := {}
	if _mode == "register":
		reply = await Webworks.register(username, password, display_name_input.text.strip_edges())
	else:
		reply = await Webworks.sign_in(username, password)

	_set_busy(false)
	if not reply.get("ok", false):
		_set_status(sign_in_status, _friendly(reply), true)
		return

	password_input.text = ""
	_show_friends()

func _on_guest() -> void:
	if _busy:
		return
	var name := guest_input.text.strip_edges()
	if name == "":
		_set_status(guest_status, "Type a name first.", true)
		return

	_save_guest_name(name)
	_set_busy(true)
	_set_status(guest_status, "Connecting...")
	var reply := await Webworks.guest_session(name)
	_set_busy(false)

	if not reply.get("ok", false):
		_set_status(guest_status, _friendly(reply), true)
		return
	_continue_to_game()

func _set_busy(busy: bool) -> void:
	_busy = busy
	sign_in_button.disabled = busy
	guest_button.disabled = busy
	mode_toggle.disabled = busy

# ------------------------------------------------------------------ friends page

func _show_friends() -> void:
	entry_page.visible = false
	friends_page.visible = true
	friends_who.text = "Signed in as " + Webworks.display_name()
	_set_status(friends_status, "")
	_signature = ""
	Webworks.begin_presence()
	_refresh.start()
	await _refresh_once()

func _on_refresh_tick() -> void:
	if not friends_page.visible or _busy:
		return
	await _refresh_once()

func _refresh_once() -> void:
	await _load_invites()
	await _load_friends(true)

func _load_friends(soft: bool) -> void:
	var reply: Dictionary = await Webworks.friends_graph()
	if not reply.get("ok", false):
		_live_on = false
		if not soft:
			_clear_rows()
			_add_row("Friends could not be loaded", "the server did not answer", false, "")
			_set_status(friends_status, "Couldn't load friends (%d)." % int(reply.get("status", 0)), true)
			friends_summary.text = "You can still continue to the game."
		return

	_live_on = true
	var graph: Dictionary = reply.get("data") if reply.get("data") is Dictionary else {}
	var accepted: Array = graph.get("accepted", [])
	var incoming: Array = graph.get("incoming", [])
	var outgoing: Array = graph.get("outgoing", [])

	var signature := _signature_of(accepted, incoming, outgoing)
	if soft and signature == _signature:
		return
	_signature = signature

	accepted.sort_custom(func(a, b): return _sort_key(a) < _sort_key(b))

	_clear_rows()
	_render_invites()

	var online := 0
	for raw in accepted:
		if not (raw is Dictionary):
			continue
		var friend: Dictionary = raw
		var persona := str(friend.get("personaState", "Offline"))
		var is_online := persona == "Online"
		if is_online:
			online += 1
		var game := str(friend.get("game", ""))
		var detail := "in game" if game != "" else ("online" if is_online else "offline")
		var friend_username := str(friend.get("username", ""))
		var friend_steam_id := str(friend.get("steamId", ""))
		_add_row(
			_friend_name(friend),
			detail,
			is_online,
			"Invite" if is_online else "",
			func(): _invite(friend_username, friend_steam_id)
		)

	for raw in incoming:
		var incoming_name := _item_text(raw)
		if incoming_name == "":
			continue
		var pending_name := incoming_name
		_add_row(pending_name, "wants to be friends", false, "Accept", func(): _accept(pending_name))

	for raw in outgoing:
		var outgoing_name := _item_text(raw)
		if outgoing_name == "":
			continue
		_add_row(outgoing_name, "request sent", false, "")

	if accepted.is_empty() and incoming.is_empty() and outgoing.is_empty():
		_add_row("No friends yet", "add someone by username below", false, "")

	friends_summary.text = "%d %s \u00b7 %d online%s" % [
		accepted.size(),
		"friend" if accepted.size() == 1 else "friends",
		online,
		(" \u00b7 %d waiting" % incoming.size()) if incoming.size() > 0 else "",
	]

func _load_invites() -> void:
	var reply: Dictionary = await Webworks.invites()
	if not reply.get("ok", false):
		return
	var data: Dictionary = reply.get("data") if reply.get("data") is Dictionary else {}
	_invites = data.get("invites", [])
	# A new invite needs a row, so let the next friend pass rebuild the list.
	_signature = ""

func _render_invites() -> void:
	for invite in _invites:
		if not (invite is Dictionary):
			continue
		var entry: Dictionary = invite
		var who := str(entry.get("fromName", ""))
		if who == "":
			who = "A friend"
		var detail := "invited you"
		var members := int(entry.get("memberCount", 0))
		if members > 0:
			detail += " \u00b7 %d in room" % members
		var lobby := str(entry.get("lobbyId", ""))
		_add_row(who, detail, true, "Join", func(): _join_invite(lobby))

func _signature_of(accepted: Array, incoming: Array, outgoing: Array) -> String:
	var parts: Array[String] = []
	for raw in accepted:
		if raw is Dictionary:
			var friend: Dictionary = raw
			parts.append("%s|%s|%s|%s" % [
				str(friend.get("username", "")),
				str(friend.get("personaState", "")),
				str(friend.get("game", "")),
				_friend_name(friend),
			])
	parts.append("#")
	for raw in incoming:
		parts.append(_item_text(raw))
	parts.append("#")
	for raw in outgoing:
		parts.append(_item_text(raw))
	parts.append("#")
	for raw in _invites:
		if raw is Dictionary:
			parts.append(str(raw.get("lobbyId", "")))
	return ",".join(parts)

static func _friend_name(friend: Dictionary) -> String:
	var name := str(friend.get("displayName", ""))
	if name == "":
		name = str(friend.get("name", ""))
	if name == "":
		name = str(friend.get("username", ""))
	return name

static func _item_text(raw: Variant) -> String:
	if raw is String:
		return raw
	if raw is Dictionary:
		return str(raw.get("username", ""))
	return ""

static func _sort_key(raw: Variant) -> String:
	if not (raw is Dictionary):
		return "9"
	var friend: Dictionary = raw
	var rank := "0" if str(friend.get("personaState", "")) == "Online" else "1"
	return rank + _friend_name(friend).to_lower()

func _clear_rows() -> void:
	for child in friend_list.get_children():
		child.queue_free()

func _add_row(title: String, detail: String, online: bool, action_label: String, action := Callable()) -> void:
	var row := ROW_SCENE.instantiate() as WebworksFriendsRow
	# Added first so the row's @onready references exist before setup() touches them.
	friend_list.add_child(row)
	row.setup(title, detail, online, action_label)
	if action_label != "" and action.is_valid():
		row.action_pressed.connect(action)

# ------------------------------------------------------------------ friends actions

func _invite(username: String, steam_id: String) -> void:
	if _busy:
		return
	_busy = true
	_set_status(friends_status, "Opening your room...")
	var reply: Dictionary = await Webworks.invite_friend(steam_id)
	_busy = false
	if not reply.get("ok", false):
		_set_status(friends_status, _friendly(reply), true)
		return
	_set_status(friends_status, "Invited %s. Room code %s." % [username, Webworks.lobby_id])

func _join_invite(lobby_id: String) -> void:
	if _busy or lobby_id == "":
		return
	_busy = true
	_set_status(friends_status, "Joining room...")
	var reply: Dictionary = await Webworks.join_lobby(lobby_id)
	_busy = false
	if not reply.get("ok", false):
		_set_status(friends_status, "That room has closed." if int(reply.get("status", 0)) == 404 else _friendly(reply), true)
		_signature = ""
		await _refresh_once()
		return

	_set_status(friends_status, "Joined the room. Loading the game...")
	if auto_join_on_invite:
		_continue_to_game()

func _accept(username: String) -> void:
	if _busy:
		return
	_busy = true
	var reply: Dictionary = await Webworks.accept_friend(username)
	_busy = false
	if not reply.get("ok", false):
		_set_status(friends_status, _friendly(reply), true)
		return
	_set_status(friends_status, "Accepted " + username + ".")
	_signature = ""
	await _refresh_once()

func _on_add_friend() -> void:
	if _busy:
		return
	var username := add_friend_input.text.strip_edges()
	if username == "":
		_set_status(friends_status, "Type the username you want to add.", true)
		return

	_busy = true
	_set_status(friends_status, "Sending a request to %s..." % username)
	var reply: Dictionary = await Webworks.add_friend(username)
	_busy = false

	if not reply.get("ok", false):
		_set_status(friends_status, _friendly(reply), true)
		return
	add_friend_input.text = ""
	_set_status(friends_status, "Friend request sent to " + username + ".")
	_signature = ""
	await _refresh_once()

func _on_sign_out() -> void:
	if _busy:
		return
	_refresh.stop()
	_live_on = false
	_busy = true
	await Webworks.sign_out()
	_busy = false
	username_input.text = ""
	password_input.text = ""
	_show_entry()

# ------------------------------------------------------------------ scene change

## The same hand-off as the Unity bootstrap: pick the scene in the Inspector, press
## Continue (or accept an invite) and the tree changes to it.
func _continue_to_game() -> void:
	if scene_to_load.strip_edges() == "":
		_set_status(friends_status, "No scene selected in the bootstrap's Scene To Load field.", true)
		return
	finished.emit(Webworks.lobby_id)
	Webworks.go_to_scene(scene_to_load)

# ------------------------------------------------------------------ helpers

func _set_status(label: Label, message: String, is_error := false) -> void:
	if label == null:
		return
	label.text = message
	label.add_theme_color_override("font_color", Color(1.0, 0.42, 0.42) if is_error else Color(0.58, 0.53, 0.53))

func _friendly(reply: Dictionary) -> String:
	var code := str(reply.get("error", ""))
	match code:
		"bad_credentials":
			return "Incorrect username or password."
		"no_such_account":
			return "No account with that username."
		"invalid_username":
			return "Use 3-32 letters, numbers, dots, dashes or underscores."
		"weak_password", "invalid_password":
			return "Password must be 6-200 characters."
		"username_taken":
			return "That username is already taken."
		"rate_limited":
			return "Too many attempts. Wait a minute."
		"accounts_unavailable":
			return "The accounts service is unavailable."
		"invalid_token", "invalid_or_missing_session":
			return "Sign in again."
		"cannot_invite_yourself":
			return "That is you."
		"missing_friend_steam_id":
			return "That friend is not known to the server yet."
		"cannot_friend_yourself":
			return "That is you."
		"transport_error":
			return "Couldn't reach the server at " + Webworks.base_url + "."

	var status := int(reply.get("status", 0))
	if status == 0:
		return "Couldn't reach the server at " + Webworks.base_url + "."
	if status == 502:
		return "The server did not answer (502). Check its console."
	return "Something went wrong (%d)." % status
