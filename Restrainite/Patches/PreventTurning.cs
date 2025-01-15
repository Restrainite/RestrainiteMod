using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventTurning
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(VR_LocomotionTurn), "ComputeSmoothTurn")]
    private static bool VR_LocomotionTurn_ComputeSmoothTurn_Prefix(ref float __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventTurning)) return true;
        
        __result = 0.0f;
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(VR_LocomotionTurn), "ComputeSnapTurn")]
    private static bool VR_LocomotionTurn_ComputeSnapTurn_Prefix(ref float __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventTurning)) return true;
        
        __result = 0.0f;
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(VR_LocomotionThreeAxisTurn), "ComputeSmoothTurn")]
    private static bool VR_LocomotionTurn_ComputeSmoothTurn_Prefix(ref float3 __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventTurning)) return true;
        
        __result = float3.Zero;
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(VR_LocomotionThreeAxisTurn), "ComputeSnapTurn")]
    private static bool VR_LocomotionTurn_ComputeSnapTurn_Prefix(ref float3 __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventTurning)) return true;
        
        __result = float3.Zero;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MouseSettings), nameof(MouseSettings.ActualLookSpeed), MethodType.Getter)]
    private static bool MouseSettings_ActualLookSpeed_Prefix(ref float __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventTurning)) return true;
        __result = 0.0f;
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(KeyboardLookSettings), nameof(KeyboardLookSettings.Speed), MethodType.Getter)]
    private static bool KeyboardLookSettings_Speed_Prefix(ref float2 __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventTurning)) return true;
        __result = float2.Zero;
        return false;
    }
}