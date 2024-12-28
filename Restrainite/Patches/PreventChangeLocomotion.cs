using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventChangeLocomotion
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(LocomotionController), nameof(LocomotionController.CanUseAnyLocomotion))]
    private static bool PreventChangeLocomotion_LocomotionControllerCanUseAnyLocomotion_Prefix(ref bool __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventChangeLocomotion)) return true;
        __result = false;
        return false;
    }
}