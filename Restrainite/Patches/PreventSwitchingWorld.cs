using System.Collections.Generic;
using System.Reflection;
using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class PreventSwitchingWorld
{
    [HarmonyPatch()]
    private class PreventSwitchingWorld_Patches
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Userspace), nameof(Userspace.EndSession));
            yield return AccessTools.Method(typeof(Userspace), nameof(Userspace.LeaveSession));
        }

        private static bool Prefix()
        {
            return !RestrainiteMod.IsRestricted(PreventionType.PreventSwitchingWorld);
        }
    }

    [HarmonyPatch(MethodType.Async)]
    private class PreventSwitchingWorld_AsyncPatches
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(WorldOrb), "OpenAsync");
            yield return AccessTools.Method(typeof(Userspace), nameof(Userspace.OpenWorld));
        }

        private static bool Prefix()
        {
            return !RestrainiteMod.IsRestricted(PreventionType.PreventSwitchingWorld);
        }
    }
}