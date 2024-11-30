using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class PreventSwitchingWorld
{
    [HarmonyPatch(typeof(WorldOrb), "OpenAsync", MethodType.Async)]
    private class WorldOrbOpenPatch
    {
        private static bool Prefix()
        {
            return !RestrainiteMod.IsRestricted(PreventionType.PreventSwitchingWorld);
        }
    }

    [HarmonyPatch(typeof(Userspace), nameof(Userspace.OpenWorld), MethodType.Async)]
    private class UserspaceOpenWorldPatch
    {
        private static bool Prefix()
        {
            return !RestrainiteMod.IsRestricted(PreventionType.PreventSwitchingWorld);
        }
    }

    [HarmonyPatch(typeof(Userspace), nameof(Userspace.EndSession))]
    private class UserspaceEndSessionPatch
    {
        private static bool Prefix()
        {
            return !RestrainiteMod.IsRestricted(PreventionType.PreventSwitchingWorld);
        }
    }

    [HarmonyPatch(typeof(Userspace), nameof(Userspace.LeaveSession))]
    private class UserspaceLeaveSessionPatch
    {
        private static bool Prefix()
        {
            return !RestrainiteMod.IsRestricted(PreventionType.PreventSwitchingWorld);
        }
    }
}