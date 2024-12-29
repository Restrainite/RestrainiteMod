using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal class PreventPhysicalTouch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(RaycastTouchSource), "GetTouchable")]
    private static void PreventPhysicalTouch_RaycastTouchSourceGetTouchable_Postfix(ref ITouchable? __result)
    {
        if (__result?.World == Userspace.UserspaceWorld) return;
        if (RestrainiteMod.IsRestricted(PreventionType.PreventPhysicalTouch)) __result = null!;
    }
}