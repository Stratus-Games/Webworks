extends "res://addons/webworks/steam/steam_api.gd"

## The Webworks `Steam` singleton.
##
## Drop the addon in, remove the native Steam extension, and any code that called
## GodotSteam's `Steam` keeps compiling: same names, same arguments, same signals. The
## answers come from your Webworks server through steam_bridge.gd instead of the Steam
## client, so accounts, friends, lobbies (including cross-server ones), stats,
## achievements, leaderboards, cloud saves, the overlay and relay P2P all keep working
## without Steam installed or a Steam App ID.
##
## Register this script as an autoload named `Steam` (the plugin does it for you) and add
## `Webworks` next to it.

const Bridge := preload("res://addons/webworks/steam/steam_bridge.gd")

var _bridge

func _ready() -> void:
	_bridge = Bridge.new()
	_bridge.steam = self
	_bridge.webworks = get_node_or_null("/root/Webworks")
	bridge = _bridge
	if _bridge.webworks == null:
		push_warning("[Webworks] The Steam shim needs the Webworks autoload. Enable the Webworks plugin, or add res://addons/webworks/core/webworks.gd as an autoload named Webworks.")
		return
	_bridge.auto_start()

func _process(delta: float) -> void:
	if _bridge != null:
		_bridge.tick(delta)

func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_CLOSE_REQUEST and _bridge != null:
		_bridge.shutdown_now()
