using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class PreventLaserTouch
{
    [HarmonyPatch(typeof(InteractionLaser), "GetTouchable",
        [typeof(RelayTouchSource), typeof(float3), typeof(float3), typeof(float3), typeof(bool)],
        [ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out])]
    private class InteractionLaserGetTouchablePatch
    {
        private static void Postfix(ref ITouchable? __result)
        {
            if (__result?.World == Userspace.UserspaceWorld) return;
            if (RestrainiteMod.IsRestricted(PreventionType.PreventLaserTouch))
            {
                if (__result is Canvas && __result.Slot?.Parent?.GetComponent<ContextMenu>() != null) return;
                __result = null!;
            }
        }
    }
}