using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class MovementSpeedMultiplier
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(VR_LocomotionDirection), nameof(VR_LocomotionDirection.Evaluate))]
    private static void VR_LocomotionDirection_Evaluate_Postfix(ref float3? __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.MovementSpeedMultiplier) || __result == null) return;

        var multiplier = RestrainiteMod.GetLowestFloat(PreventionType.MovementSpeedMultiplier);
        if (float.IsNaN(multiplier)) return;
        if (multiplier < 0.0f) multiplier = 0.0f;
        else if (multiplier > 1.0f) multiplier = 1.0f;
        __result = __result.Value * multiplier;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ScreenLocomotionDirection), nameof(ScreenLocomotionDirection.Evaluate))]
    private static void ScreenLocomotionDirection_Evaluate_PostFix(ref float3? __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.MovementSpeedMultiplier) || __result == null) return;

        var multiplier = RestrainiteMod.GetLowestFloat(PreventionType.MovementSpeedMultiplier);
        if (float.IsNaN(multiplier)) return;
        if (multiplier < 0.0f) multiplier = 0.0f;
        else if (multiplier > 1.0f) multiplier = 1.0f;
        __result = __result.Value * multiplier;
    }
}