using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventMovement
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(VR_LocomotionDirection), nameof(VR_LocomotionDirection.Evaluate))]
    private static bool VR_LocomotionDirection_Evaluate_Prefix(ref float3? __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventMovement)) return true;

        __result = float3.Zero;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ScreenLocomotionDirection), nameof(ScreenLocomotionDirection.Evaluate))]
    private static bool ScreenLocomotionDirection_Evaluate_Prefix(ref float3? __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventMovement)) return true;

        __result = float3.Zero;
        return false;
    }
}