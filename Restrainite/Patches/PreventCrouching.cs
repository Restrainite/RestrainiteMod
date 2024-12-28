using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventCrouching
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(LocomotionController), nameof(LocomotionController.CanCrouch), MethodType.Getter)]
    private static bool PreventCrouching_LocomotionControllerCanCrouch_Getter_Prefix(ref bool __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventCrouching)) return true;
        __result = false;
        return false;
    }
}