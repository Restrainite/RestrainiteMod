using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal static class PreventUsingTools
{
    [HarmonyPatch(typeof(InteractionHandler), nameof(InteractionHandler.CanEquip))]
    private static class InteractionHandlerCanEquipPatch
    {
        private static bool Postfix(bool result, InteractionHandler __instance)
        {
            if (__instance.World == Userspace.UserspaceWorld) return result;

            return !RestrainiteMod.IsRestricted(PreventionType.PreventUsingTools) && result;
        }
    }

    [HarmonyPatch(typeof(InteractionHandler), nameof(InteractionHandler.CanKeepEquipped))]
    private static class InteractionHandlerCanKeepEquippedPatch
    {
        private static bool Postfix(bool result, InteractionHandler __instance)
        {
            if (__instance.World == Userspace.UserspaceWorld) return result;

            return !RestrainiteMod.IsRestricted(PreventionType.PreventUsingTools) && result;
        }
    }
}