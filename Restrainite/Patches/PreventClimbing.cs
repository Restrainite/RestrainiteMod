using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventClimbing
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GrabWorldLocomotion), "TryActivate")]
    private static bool PreventClimbing_GrabWorldLocomotionTryActivate_Prefix(ref float3? ___currentAnchor)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventClimbing)) return true;
        ___currentAnchor = null;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GrabWorldLocomotion), "CheckDeactivate")]
    private static bool PreventClimbing_GrabWorldLocomotionCheckDeactivate_Prefix(ref float3? ___currentAnchor)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventClimbing)) return true;
        ___currentAnchor = null;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PhysicalLocomotion), "CheckKeepGrip")]
    private static bool PreventClimbingPhysicalLocomotionCheckKeepGrip_Prefix(ref bool __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventClimbing)) return true;
        __result = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PhysicalLocomotion), "CheckAquireGrip")]
    private static bool PreventClimbingPhysicalLocomotionCheckAquireGrip_Prefix()
    {
        return !RestrainiteMod.IsRestricted(PreventionType.PreventClimbing);
    }
}