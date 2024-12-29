using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal class PreventRespawning
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Slot), nameof(Slot.DestroyPreservingAssets),
        [typeof(Slot), typeof(bool)], [ArgumentType.Normal, ArgumentType.Normal])]
    private static bool PreventRespawning_SlotDestroyPreservingAssets_Prefix(Slot __instance)
    {
        var userRootSlot = __instance.Engine.WorldManager.FocusedWorld.LocalUser.Root.Slot;
        return __instance != userRootSlot || !RestrainiteMod.IsRestricted(PreventionType.PreventRespawning);
    }
}