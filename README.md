# Restrainite

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for 
[Resonite](https://resonite.com/) that allows others to control restrictions of the local user. 

With the current use of dynamic variables, it's not possible to restrict the access. Anyone in
your game world can toggle it. Please keep that in mind and use the options in the extensive settings menu.
There is currently no known way to restrict this based on user ids or similar, because of how the FrooxEngine works. 
(PRs welcome!) We might add an option to use cloud variables in the future.

## Features/Restrictions

_(As of version 0.3.14)_
- `PreventEquippingAvatar`: Prevents equipping in-world avatars or switching from inventory.
- `PreventOpeningContextMenu`: Prevents opening the context menu, and closes it if already opened.
- `PreventUsingTools`: Prevents equipping tools, and drops them if already equipped.
- `PreventOpeningDash`: Prevents opening the dashboard, and closes it if already opened.
- `PreventGrabbing`: Prevents grabbing objects physically/via laser, and drops any that are already grabbed.
- `PreventHearing`: Forces all other users voices to be muted.
- `EnforceSelectiveHearing` **[+ string]**: When enabled, All users will be muted except those whose **user-ID's** (not usernames) are in this list.
  - _PreventHearing_ takes precedence over _EnforceSelectiveHearing_.
- `PreventLaserTouch`: Prevents any laser-based interaction.
- `PreventPhysicalTouch`: Prevents any physically-based interaction.
  - _PreventLaserTouch_ & _PreventPhysicalTouch_ also prevent grabbing respectively.
- `PreventSpeaking`: Forces the user to be muted.
- `EnforceWhispering`: Forces the user to only be able to talk in whisper mode (they can still mute themselves).
  - _PreventSpeaking_ takes precedence over _EnforceWhispering_.
- `PreventRespawning`: Prevents respawning, including emergency respawn gesture.
- `PreventEmergencyRespawning`: Prevents using the emergency respawn gesture (can still respawn via session users tab).
- `PreventSwitchingWorld`: Prevents starting a new world, joining another session, leaving the current world, or changing focus.
- `ShowContextMenuItems`: **[+ string]** When enabled, any **root** context menu items not in this list will be hidden.
- `HideContextMenuItems`: **[+ string]** When enabled, any **root** context menu items in this list will be hidden.
  - _ShowContextMenuItems_ is evaluated before _HideContextMenuItems_ if both are enabled.
  - For default context menu items, you need to list their locale string names. See the "interact with the mod" section below.
  - These options only show/hide root context menu items.
- `ShowDashScreens`: **[+ string]** When enabled, any dashboard screens not in this list will be hidden.
- `HideDashScreens`: **[+ string]** When enabled, any dashboard screens in this list will be hidden.
  - The exit screen can not be hidden.
  - _ShowDashScreens_ is evaluated before _HideDashScreens_ if both are enabled.
  - For non-custom screens, you need to list their locale string names. See the "interact with the mod" section below.
- `PreventUserScaling`: Prevents the user from rescaling themselves.
- `PreventCrouching`: Prevents crouching in desktop mode.
- `PreventJumping`: Prevents jumping, but does not prevent exiting anchors.
- `PreventChangeLocomotion`: Prevents the user from changing their locomotion mode.
- `ResetUserScale`: Utility variable that resets a user to their default scale.
  - You should use this by enabling then disabling in the next frame. Think of it like an impulse.
  - Keeping it enabled does not prevent the user from rescaling themselves, and will only prevent other items from using this.
- `PreventLeavingAnchors`: Prevents the user from leaving any anchor themselves.

## Installation

1. Install either [MonkeyLoader](https://github.com/ResoniteModdingGroup/MonkeyLoader.GamePacks.ResoniteModLoader) or
   [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Place [Restrainite.dll](https://github.com/SnepDrone/Restrainite/releases/latest/download/Restrainite.dll) into your 
   `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a 
   default install. You can create it, if it's missing.
3. If you use ResoniteModLoader, install [ResoniteModSettings](https://github.com/badhaloninja/ResoniteModSettings)
4. Start Resonite.
5. Check the settings menu to customize your options.
6. Once enabled, the options will show up as `DynamicValueVariable` components under the `Restrainite` slot in your user 
   root slot.

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