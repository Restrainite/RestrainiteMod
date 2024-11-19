using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class PreventEmergencyRespawning
{
    [HarmonyPatch(typeof(InteractionHandler), "HoldMenu")]
    private class InteractionHandlerHoldMenuPatch
    {
        private static void Prefix(ref float ___panicCharge, InteractionHandler __instance)
        {
            if (Restrainite.GetValue(PreventionType.PreventEmergencyRespawning))
                ___panicCharge = -__instance.Time.Delta;
        }
    }
}