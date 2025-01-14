using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventRunning
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ScreenLocomotionDirection), nameof(ScreenLocomotionDirection.Evaluate))]
    private static void ScreenLocomotionDirection_Evaluate_Prefix(ScreenLocomotionDirection __instance, out float __state)
    {
        __state = __instance.FastMultiplier;
        if (RestrainiteMod.IsRestricted(PreventionType.PreventRunning))
        {
            __instance.FastMultiplier = 1.0f;
        }
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ScreenLocomotionDirection), nameof(ScreenLocomotionDirection.Evaluate))]
    private static void ScreenLocomotionDirection_Evaluate_Postfix(ScreenLocomotionDirection __instance, float __state)
    {
        __instance.FastMultiplier = __state;
    }
}