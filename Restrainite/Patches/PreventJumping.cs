using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventJumping
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CharacterController), nameof(CharacterController.Jump), MethodType.Getter)]
    private static bool PreventJumping_CharacterControllerJump_Getter_Prefix(ref bool __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventJumping)) return true;
        __result = false;
        return false;
    }
}