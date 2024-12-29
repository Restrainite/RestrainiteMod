using FrooxEngine;
using FrooxEngine.CommonAvatar;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventLeavingAnchors
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(AvatarAnchor), nameof(AvatarAnchor.Release))]
	private static bool PreventLeavingAnchors_AvatarAnchorRelease_Prefix(AvatarAnchor __instance)
	{
		return __instance.Engine.WorldManager.FocusedWorld.LocalUser != __instance.AnchoredUser
			|| RestrainiteMod.IsRestricted(PreventionType.PreventLeavingAnchors);
	}
}