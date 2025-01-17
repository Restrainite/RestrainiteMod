# Restrainite

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for 
[Resonite](https://resonite.com/) that allows others to control restrictions of the local user. 

With the current use of dynamic variables, it's not possible to restrict the access. Anyone in
your game world can toggle it. Please keep that in mind and use the options in the extensive settings menu.
There is currently no known way to restrict this based on user ids or similar, because of how the FrooxEngine works. 
(PRs welcome!) We might add an option to use cloud variables in the future.

## Features/Restrictions

_(As of version 0.4.10)_

### Interaction
- `Prevent Equipping Avatar`: Prevents equipping in-world avatars or switching from inventory.
- `Prevent Using Tools`: Prevents equipping tools, and drops them if already equipped.
- `Prevent Grabbing`: Prevents grabbing objects physically/via laser, and drops any that are already grabbed.
- `Prevent Laser Touch`: Prevents any laser-based interaction.
- `Prevent Physical Touch`: Prevents any physically-based interaction.
    - _PreventLaserTouch_ & _PreventPhysicalTouch_ also prevent grabbing respectively.
- `Prevent Spawn Objects`: Prevents spawning objects into the current world.
- `Prevent Save Items`: Prevents saving items to the inventory.
- `Allow Grabbing By Slot Tags`: **[+ string]** When enabled, only allows grabbing items with tags in this list.
- `Deny Grabbing By Slot Tags`: **[+ string]** When enabled, prevents grabbing any items with tags in this list.
  - _AllowGrabbingTags_ is evaluated before _DenyGrabbingTags_ if both are enabled.
  - Slots without tag will be evaluated with the string "null". 
  - _PreventGrabbing_ takes precedence over both of these options.
- `Allow Touching By Slot Tags`: **[+ string]** When enabled, only allows interacting with items with tags in this list.
- `Deny Touching By Slot Tags`: **[+ string]** When enabled, prevents interacting with any items with tags in this list.
  - _AllowTouchingTags_ is evaluated before _DenyTouchingTags_ if both are enabled.
  - Slots without tag will be evaluated with the string "null".
  - _PreventPhysicalTouch_ and _PreventLaserTouch_ take precedence over these options.
- `Prevent Non Dash Userspace Interaction`: Prevents interacting with anything in userspace besides the dashboard (facet anchors, userspace inspectors, etc.).

### Respawning and changing worlds
- `Prevent Respawning`: Prevents respawning, including emergency respawn gesture.
- `Prevent Emergency Respawning`: Prevents using the emergency respawn gesture (can still respawn via session users tab).
- `Prevent Switching World`: Prevents starting a new world, joining another session, leaving the current world, or changing focus.

### Visual
- `Show User Avatars`: **[+ string]** When enabled, only user avatars are shown, whose user id is in the list.
- `Hide User Avatars`: **[+ string]** When enabled, user avatars are hidden, whose user id is in the list.
- `Disable Nameplates`: Hides all avatar nameplates.

### Audio
- `Prevent Hearing`: Forces all other users voices to be muted.
- `Enforce Selective Hearing`: **[+ string]** When enabled, All users will be muted except those whose **user-ID's** (not usernames) are in this list.
  - _PreventHearing_ takes precedence over _EnforceSelectiveHearing_.
- `Prevent Speaking`: Forces the user to be muted.
- `Enforce Whispering`: Forces the user to only be able to talk in whisper mode (they can still mute themselves).
  - _PreventSpeaking_ takes precedence over _EnforceWhispering_.

### UI
- `Prevent Opening Context Menu`: Prevents opening the context menu, and closes it if already opened.
- `Show Context Menu Items`: **[+ string]** When enabled, any context menu items not in this list will be hidden.
- `Hide Context Menu Items`: **[+ string]** When enabled, any context menu items in this list will be hidden.
  - _ShowContextMenuItems_ is evaluated before _HideContextMenuItems_ if both are enabled.
  - For default context menu items, you need to list their locale string names. See the "interact with the mod" section below.
- `Prevent Opening Dash`: Prevents opening the dashboard, and closes it if already opened.
- `Show Dash Screens`: **[+ string]** When enabled, any dashboard screens not in this list will be hidden.
- `Hide Dash Screens`: **[+ string]** When enabled, any dashboard screens in this list will be hidden.
  - The exit screen can not be hidden.
  - _ShowDashScreens_ is evaluated before _HideDashScreens_ if both are enabled.
  - For non-custom screens, you need to list their locale string names. See the "interact with the mod" section below.
- `Disable Notifications`: The user can't see notifications anymore.
- `Prevent Sending Messages`: The user can't send messages to contacts.
- `Prevent Invite Contact`: The user can't invite contacts to the current world.
- `Prevent Third Person View`: Desktop users can't switch to third person view anymore.

### Locomotion
- `Prevent User Scaling`: Prevents the user from rescaling themselves.
- `Prevent Crouching`: Prevents crouching in desktop mode.
- `Prevent Jumping`: Prevents jumping, but does not prevent exiting anchors.
- `Prevent Running`: Prevents running, when using keyboard (double tap or shift) or gamepad controls (joystick press).
- `Prevent Climbing`: Prevents climbing by grabbing the world or characters.
- `Prevent Change Locomotion`: Prevents the user from changing their locomotion mode.
- `Reset User Scale`: Utility variable that resets a user to their default scale.
  - You should use this by enabling then disabling in the next frame. Think of it like an impulse.
  - Keeping it enabled does not prevent the user from rescaling themselves, and will only prevent other items from using this.
- `Prevent Leaving Anchors`: Prevents the user from leaving any anchor themselves.
- `Prevent Movement`: Prevent the user being able to move around via VR controller or keyboard.
- `Prevent Turning`: Prevent the user from turning his body via VR controller or look around via mouse or keyboard. The user is still able to look around in VR by turning his head. Turning can't be restricted for Gamepad users.

## Installation

1. Install either [MonkeyLoader](https://github.com/ResoniteModdingGroup/MonkeyLoader.GamePacks.ResoniteModLoader) or
   [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Place [Restrainite.dll](https://github.com/SnepDrone/Restrainite/releases/latest/download/Restrainite.dll) into your 
   `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a 
   default install. You can create it, if it's missing.
3. If you use ResoniteModLoader, install [ResoniteModSettings](https://github.com/badhaloninja/ResoniteModSettings)
4. Start Resonite.
5. Check the settings menu to customize your options.

## How to interact with the mod

This mod creates a DynamicVariableSpace `Restrainite Status` under the root slot of the User and 
a `Restrainite Status` slot with all restriction options. If the preset in the config is set to None, 
the DynamicVariableSpace and the Restrainite slot will not be created, or it will be deleted, if it already exists. 
Restriction settings, that are not enabled by the user, will also not have a slot under the Restrainite slot. 
The tag of each restriction slot contains the name of the DynamicValueVariable required to set the value.

To interact with this, create an empty slot. Add a DynamicVariableSpace with the name `Restrainite` to it. Add a 
`DynamicReferenceVariable<User>` component with the name `Target User`, that points to the user who should be 
affected by the restriction. Add a `DynamicValueVariable<bool>` component with the name listed in the tag of the 
restriction. Toggle the value to enable/disable the restriction.

For certain features, it's also possible to add a `DynamicValueVariable<string>` component with the same name, to select
 for example which Context Menu Items should be shown or hidden. The string is a comma seperated list. If items are 
from the base game, use the locale keys to refer to them, e.g. Interaction.Undo. 
See [Resonite Locale](https://github.com/Yellow-Dog-Man/Locale/blob/main/en.json)

If you create a remote, use the tag "RestrainiteRemote" on the object root. This helps with the restrictions 
`Deny Grabbing By Slot Tags` and similar.

## Why does this exist?

There are people who have various reasons for wanting certain features of the game disabled. A lot of these features 
can also be disabled through other means like Protoflux and could be seen as malicious, because then they could be 
applied to anyone. Those in-game items already exists. In NeosVR there also existed the NeosNoEscape mod, with similar 
objectives.

Some features can't be disabled with in-game code, so people could try to find in-game exploits to achieve their goal.
This presents a strong incentive to not report any security exploits they find. The primary motivation behind this mod 
is not to remove safety features, but give people a consenting choice to disable them.

If someone is using this mod maliciously, this a moderation issue. 

## How to get yourself unstuck

With the default settings, restarting the game will remove all restrictions. If you still manage to get yourself 
completely stuck, you can always delete or modify the Restrainite settings file. 
For MonkeyLoader that is under `MonkeyLoader/Configs/Restrainite.json`.

## Building from source and contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md).
