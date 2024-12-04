using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventUsingTools
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(InteractionHandler), nameof(InteractionHandler.CanEquip))]
    private static bool PreventUsingTools_InteractionHandlerCanEquip_Postfix(bool result, InteractionHandler __instance)
    {
        if (__instance.World == Userspace.UserspaceWorld) return result;

        return !RestrainiteMod.IsRestricted(PreventionType.PreventUsingTools) && result;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(InteractionHandler), nameof(InteractionHandler.CanKeepEquipped))]
    private static bool PreventUsingTools_InteractionHandlerCanKeepEquipped_Postfix(bool result, InteractionHandler __instance)
    {
        if (__instance.World == Userspace.UserspaceWorld) return result;

        return !RestrainiteMod.IsRestricted(PreventionType.PreventUsingTools) && result;
    }
}