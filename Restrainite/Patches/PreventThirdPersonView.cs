using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventThirdPersonView
{

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ScreenController), nameof(ScreenController.CanUseViewTargetting))]
    private static bool PreventThirdPersonView_ScreenControllerCanUseViewTargetting_Prefix(IViewTargettingController view, ref bool __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventThirdPersonView)) return true;
        if (view is not (ThirdPersonTargettingController or FreeformTargettingController)) return true;
        
        __result = false;
        return false;
    }
}