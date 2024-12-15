using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal static class PreventCrouching
{
    [HarmonyPatch(typeof(LocomotionController), nameof(LocomotionController.CanCrouch), MethodType.Getter)]
    private static class LocomotionControllerCanCrouchPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!RestrainiteMod.IsRestricted(PreventionType.PreventCrouching)) return true;
            __result = false;
            return false;
        }
    }
}