using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal class PreventEmergencyRespawning
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InteractionHandler), "HoldMenu")]
    private static void PreventEmergencyRespawning_InteractionHandlerHoldMenu_Prefix(ref float ___panicCharge,
        InteractionHandler __instance)
    {
        if (RestrainiteMod.IsRestricted(PreventionType.PreventEmergencyRespawning))
            ___panicCharge = -__instance.Time.Delta;
    }
}