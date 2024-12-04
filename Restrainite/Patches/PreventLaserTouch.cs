using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal class PreventLaserTouch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(InteractionLaser), "GetTouchable",
        [typeof(RelayTouchSource), typeof(float3), typeof(float3), typeof(float3), typeof(bool)],
        [ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out])]
    private static void PreventLaserTouch_InteractionLaserGetTouchable_Postfix(ref ITouchable? __result)
    {
        if (__result?.World == Userspace.UserspaceWorld) return;
        if (RestrainiteMod.IsRestricted(PreventionType.PreventLaserTouch)) __result = null!;
    }
}