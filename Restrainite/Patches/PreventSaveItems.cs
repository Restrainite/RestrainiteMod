using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventSaveItems
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WorldPermissionsExtensoins), nameof(WorldPermissionsExtensoins.CanSaveItems))]
    private static bool WorldPermissionsExtensoins_CanSaveItems_Prefix(ref bool __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventSaveItems))
            return true;

        __result = false;
        return false;
    }
}