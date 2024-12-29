using System.Collections.Generic;
using System.Reflection;
using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal class PreventRespawning
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Slot), nameof(Slot.DestroyPreservingAssets),
            [typeof(Slot), typeof(bool)]);
        yield return AccessTools.Method(typeof(Slot), nameof(Slot.Destroy),
            [typeof(bool)]);
    }

    private static bool Prefix(Slot __instance)
    {
        var userRootSlot = __instance.Engine.WorldManager.FocusedWorld.LocalUser.Root.Slot;
        return __instance != userRootSlot || !RestrainiteMod.IsRestricted(PreventionType.PreventRespawning);
    }
}