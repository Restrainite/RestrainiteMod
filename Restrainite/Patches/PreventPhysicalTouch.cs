using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class PreventPhysicalTouch
{
    [HarmonyPatch(typeof(RaycastTouchSource), "GetTouchable")]
    private class RaycastTouchSourceGetTouchablePatch
    {
        private static void Postfix(ref ITouchable? __result)
        {
            if (__result?.World == Userspace.UserspaceWorld) return;
            if (RestrainiteMod.IsRestricted(PreventionType.PreventPhysicalTouch)) __result = null!;
        }
    }
}