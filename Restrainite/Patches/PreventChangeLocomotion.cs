using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal static class PreventChangeLocomotion
{
    [HarmonyPatch(typeof(LocomotionController), nameof(LocomotionController.CanUseAnyLocomotion))]
    private static class LocomotionControllerCanUseAnyLocomotionPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!RestrainiteMod.IsRestricted(PreventionType.PreventChangeLocomotion)) return true;
            __result = false;
            return false;
        }
    }
}