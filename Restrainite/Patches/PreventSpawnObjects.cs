using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventSpawnObjects
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WorldPermissionsExtensoins), nameof(WorldPermissionsExtensoins.CanSpawnObjects))]
    private static bool WorldPermissionsExtensoins_CanSpawnObjects_Prefix(ref bool __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventSpawnObjects))
            return true;

        __result = false;
        return false;
    }
}