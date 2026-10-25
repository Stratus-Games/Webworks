class_name WebworksFriendsRow
extends PanelContainer

## One row of the friends list: a round initial avatar, a name, a detail line and an
## optional action (Invite / Accept / Join). The layout is friends_row.tscn.

signal action_pressed

@onready var avatar: Panel = $Margin/HBox/Avatar
@onready var initial_label: Label = $Margin/HBox/Avatar/Initial
@onready var name_label: Label = $Margin/HBox/Body/Name
@onready var detail_label: Label = $Margin/HBox/Body/Detail
@onready var action_button: Button = $Margin/HBox/Action

func _ready() -> void:
	action_button.pressed.connect(func() -> void: action_pressed.emit())

func setup(title: String, detail: String, online: bool, action_label: String) -> void:
	name_label.text = title
	detail_label.text = detail
	initial_label.text = title.substr(0, 1).to_upper() if title.length() > 0 else "?"

	action_button.visible = action_label != ""
	action_button.text = action_label

	var style := (avatar.get_theme_stylebox("panel") as StyleBoxFlat).duplicate() as StyleBoxFlat
	var colour := colour_for(title)
	if not online:
		colour = colour.darkened(0.5)
	style.bg_color = colour
	avatar.add_theme_stylebox_override("panel", style)

	name_label.add_theme_color_override("font_color", Color(0.95, 0.95, 0.95) if online else Color(0.68, 0.64, 0.64))

static func colour_for(name: String) -> Color:
	var hash := 17
	for i in name.length():
		hash = hash * 31 + name.unicode_at(i)
	var hue := float(absi(hash) % 360) / 360.0
	return Color.from_hsv(hue, 0.55, 0.72)
