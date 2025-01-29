using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class MaximumLaserDistance
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(InteractionHandler), nameof(InteractionHandler.MaxLaserDistance), MethodType.Getter)]
    private static void InteractionHandler_MaxLaserDistance_Postfix(ref float __result, InteractionHandler __instance)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.MaximumLaserDistance) || __result < float.MaxValue)
            return;

        var distance = RestrainiteMod.GetLowestFloat(PreventionType.MaximumLaserDistance);
        if (float.IsNaN(distance)) return;
        if (distance < 0.0f) distance = 0.0f;
        if (distance > __result) return;
        __result = distance;
    }
}