using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class PreventGrabbing
{
    [HarmonyPatch(typeof(InteractionHandler), "StartGrab")]
    private class InteractionHandlerStartGrabPatch
    {
        private static bool Prefix(InteractionHandler __instance)
        {
            return __instance.World == Userspace.UserspaceWorld ||
                   !Restrainite.GetValue(PreventionType.PreventGrabbing);
        }
    }
}