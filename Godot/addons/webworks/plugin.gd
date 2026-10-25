@tool
extends EditorPlugin

## Registers the Webworks client (and the GodotSteam-compatible Steam shim) as autoloads
## when the plugin is enabled, and removes them when it is disabled. That is the whole
## integration: drop the folder in, enable "Webworks" in Project Settings, add the
## bootstrap scene, press Play.
##
## If a native Steam extension is still installed, `Steam` already exists as a global
## singleton and the shim stays off: add the shim after the extension is removed.

const AUTOLOAD_NAME := "Webworks"
const AUTOLOAD_PATH := "res://addons/webworks/core/webworks.gd"

const STEAM_AUTOLOAD := "Steam"
const STEAM_PATH := "res://addons/webworks/steam/steam.gd"

var _steam_added := false

func _enter_tree() -> void:
	add_autoload_singleton(AUTOLOAD_NAME, AUTOLOAD_PATH)

	if Engine.has_singleton(STEAM_AUTOLOAD):
		push_warning("[Webworks] A native Steam extension is installed, so the Webworks Steam shim was not added. Remove the extension's addon, then re-enable this plugin.")
		return
	if ProjectSettings.has_setting("autoload/" + STEAM_AUTOLOAD):
		return
	add_autoload_singleton(STEAM_AUTOLOAD, STEAM_PATH)
	_steam_added = true

func _exit_tree() -> void:
	if _steam_added:
		remove_autoload_singleton(STEAM_AUTOLOAD)
		_steam_added = false
	remove_autoload_singleton(AUTOLOAD_NAME)
