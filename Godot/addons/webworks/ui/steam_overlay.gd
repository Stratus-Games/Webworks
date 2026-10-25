extends Control

## The in-game overlay for the Webworks Steam shim.
##
## GodotSteam's `activateGameOverlay("Friends")` (and the other overlay entry points)
## land here: a panel above the game with the friend list, pending lobby invites, the
## achievement list and the account/service details. It is a normal scene, so it can be
## restyled in the editor without touching code, and it only reads through the shim's
## public API, which means it also works while the real Steam extension is installed.

signal closed

const ROW_SCENE := preload("res://addons/webworks/ui/friends_row.tscn")
const PERSONA_STATES := ["Offline", "Online", "Busy", "Away", "Snooze", "Invisible"]

var _busy := false

@onready var tabs: TabContainer = %Tabs
@onready var service_label: Label = %Service
@onready var close_button: Button = %Close

@onready var friends_summary: Label = %FriendsSummary
@onready var friends_list: VBoxContainer = %FriendsList
@onready var friends_status: Label = %FriendsStatus
@onready var friends_refresh: Button = %FriendsRefresh

@onready var invites_summary: Label = %InvitesSummary
@onready var invite_list: VBoxContainer = %InviteList
@onready var invites_status: Label = %InvitesStatus
@onready var invites_refresh: Button = %InvitesRefresh

@onready var achievements_summary: Label = %AchievementsSummary
@onready var achievement_list: VBoxContainer = %AchievementList
@onready var achievements_status: Label = %AchievementsStatus
@onready var achievements_refresh: Button = %AchievementsRefresh

@onready var persona_label: Label = %Persona
@onready var account_label: Label = %Account
@onready var server_label: Label = %Server
@onready var account_page_button: Button = %AccountPage
@onready var sign_out_button: Button = %SignOut

func _ready() -> void:
	visible = false
	close_button.pressed.connect(close)
	friends_refresh.pressed.connect(_refresh_friends)
	invites_refresh.pressed.connect(_refresh_invites)
	achievements_refresh.pressed.connect(_refresh_achievements)
	account_page_button.pressed.connect(_open_account_page)
	sign_out_button.pressed.connect(_sign_out)
	service_label.text = Webworks.base_url

func _unhandled_input(event: InputEvent) -> void:
	if visible and event.is_action_pressed("ui_cancel"):
		close()
		get_viewport().set_input_as_handled()

## Opens the overlay on one of the tabs GodotSteam names: friends, invites/lobbyinvite,
## achievements/stats, settings, community.
func open(tab := "") -> void:
	visible = true
	move_to_front()
	_select(tab)
	_refresh_all()

func close() -> void:
	visible = false
	closed.emit()

func _select(tab: String) -> void:
	var wanted := tab.to_lower()
	var index := 0
	match wanted:
		"friends", "friend", "players":
			index = 0
		"invites", "lobbyinvite", "invite":
			index = 1
		"achievements", "stats", "achievement":
			index = 2
		"settings", "community", "officialgamegroup":
			index = 3
	tabs.current_tab = index
	_update_settings()

func _refresh_all() -> void:
	if _busy:
		return
	_busy = true
	service_label.text = Webworks.base_url
	_refresh_friends()
	_refresh_invites()
	_refresh_achievements()
	_update_settings()
	_busy = false

# ------------------------------------------------------------------ friends

func _refresh_friends() -> void:
	for child in friends_list.get_children():
		child.queue_free()

	var count := int(Steam.getFriendCount())
	var online := 0
	for index in count:
		var steam_id := int(Steam.getFriendByIndex(index, 65535))
		var name := str(Steam.getFriendPersonaName(steam_id))
		var state := int(Steam.getFriendPersonaState(steam_id))
		var state_name: String = PERSONA_STATES[clampi(state, 0, PERSONA_STATES.size() - 1)]
		var is_online := state != 0 and state != 5
		if is_online:
			online += 1

		var row := ROW_SCENE.instantiate()
		friends_list.add_child(row)
		row.setup(name if name != "" else "Player", state_name.to_lower(), is_online, "Invite" if is_online else "")
		var id := steam_id
		row.action_pressed.connect(func() -> void: _invite(id, name))

	friends_summary.text = "%d of %d friends online" % [online, count]
	friends_status.text = "" if count > 0 else "No friends yet. Add them from the account page or the bootstrap friends screen."

func _invite(steam_id: int, name: String) -> void:
	friends_status.text = "Inviting %s..." % name
	var reply: Dictionary = await Webworks.invite_friend(str(steam_id))
	if reply.get("ok", false):
		friends_status.text = "Invite sent to %s." % name
	else:
		friends_status.text = "Could not invite %s (%s)." % [name, str(reply.get("error", "failed"))]

# ------------------------------------------------------------------ invites

func _refresh_invites() -> void:
	for child in invite_list.get_children():
		child.queue_free()

	var reply: Dictionary = await Webworks.invites()
	var invites: Array = []
	var data = reply.get("data")
	if reply.get("ok", false) and data is Dictionary:
		invites = data.get("invites", [])

	for invite in invites:
		if not (invite is Dictionary):
			continue
		var lobby_id := str(invite.get("lobbyId", invite.get("id", "")))
		var from := str(invite.get("fromName", invite.get("from", "A friend")))
		var members := int(invite.get("memberCount", 0))
		var row := ROW_SCENE.instantiate()
		invite_list.add_child(row)
		row.setup(from, "%d in lobby" % members, true, "Join")
		var id := lobby_id
		row.action_pressed.connect(func() -> void: _join(id))

	invites_summary.text = "%d pending" % invites.size()
	invites_status.text = "" if not invites.is_empty() else "No lobby invites right now."

func _join(lobby_id: String) -> void:
	invites_status.text = "Joining..."
	var reply: Dictionary = await Webworks.join_lobby(lobby_id)
	if reply.get("ok", false):
		invites_status.text = "Joined lobby %s." % lobby_id
		_refresh_invites()
	else:
		invites_status.text = "Could not join (%s)." % str(reply.get("error", "failed"))

# ------------------------------------------------------------------ achievements

func _refresh_achievements() -> void:
	for child in achievement_list.get_children():
		child.queue_free()

	var total := int(Steam.getNumAchievements())
	var unlocked := 0
	for index in total:
		var key := str(Steam.getAchievementName(index))
		if key == "":
			continue
		var title := str(Steam.getAchievementDisplayAttribute(key, "name"))
		var description := str(Steam.getAchievementDisplayAttribute(key, "desc"))
		var earned: bool = bool(Steam.getAchievement(key).get("achieved", false))
		if earned:
			unlocked += 1

		var row := ROW_SCENE.instantiate()
		achievement_list.add_child(row)
		row.setup(title if title != "" else key, "unlocked" if earned else (description if description != "" else "locked"), earned, "")

	achievements_summary.text = "%d of %d unlocked" % [unlocked, total]
	achievements_status.text = "" if total > 0 else "No achievements defined for this app id yet. Define them with the achievements endpoints or your server's admin routes."

# ------------------------------------------------------------------ settings

func _update_settings() -> void:
	persona_label.text = "Persona: %s" % (str(Steam.getPersonaName()) if Steam.getPersonaName() != "" else "(not signed in yet)")
	account_label.text = "Account: %s" % (Webworks.display_name() if Webworks.has_account() else "guest session")
	server_label.text = "Server: %s   App id: %s" % [Webworks.base_url, Webworks.app_id]

func _open_account_page() -> void:
	if Webworks.base_url != "":
		OS.shell_open(Webworks.base_url + "/account")

func _sign_out() -> void:
	Steam.steamShutdown()
	await Webworks.sign_out()
	_update_settings()
	close()
