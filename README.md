# Restrainite

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for 
[Resonite](https://resonite.com/) that allows others to control restrictions of the local user. 

With the current use of dynamic variables, it's not possible to restrict the access. Anyone in
your game world can toggle it. Please keep that in mind and use the options in the extensive settings menu.
There is currently no known way to restrict this based on user ids or similar, because of how the FrooxEngine works. 
(PRs welcome!) We might add an option to use cloud variables in the future.

## Features

TODO: Add list of restrictions here

## Upcoming Features / ToDo

- Fix EnforceWhisper, it's local only
- Restrictions to Locomotion
- Hide the laser, if laser touch is disabled.
- If laser touch is disabled, the context menu can't be used.
- Allow/deny lists for tabs in the dash
- Test if the plugin also affects other users, if the mod user is the host.
- Try to circumvent the mod restrictions
- Testing, testing, testing

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

This mod creates a DynamicVariableSpace `Restrainite` under the root slot of the User and a `Restrainite` slot with all
restriction options. If the preset in the config is set to None, the DynamicVariableSpace and the Restrainite slot will 
not be created, or it will be deleted, if it already exists. Restriction settings, that are not enabled by the user, 
will also not have a slot under the Restrainite slot. The tag of each restriction slot contains the name of the 
DynamicValueVariable.

There are two DynamicValueVariable components for each setting. The boolean toggles the restriction on and off, and 
it will also reset the counter. The integer is a counter, which toggles the restriction on, if it is above 0 and off, 
if it is 0 or below.

If you build an item, that interacts with this mod, like a gag for example, it is recommended that you use the counter.
Increment it on equip, decrement it on unequip or deletion, if it's still equipped.

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

## Contributing

The mod has the following guidelines:

- No feature should require spawning an object or require creating complex objects in the world.
- Do not try to fix social issues with code.
- Cleanup after injecting code or objects. Leave no trace behind. Always make things non-persistent.
