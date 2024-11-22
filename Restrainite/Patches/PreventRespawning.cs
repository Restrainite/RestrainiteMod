using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class PreventRespawning
{
    [HarmonyPatch(typeof(Slot), nameof(Slot.DestroyPreservingAssets), [typeof(Slot), typeof(bool)],
        [ArgumentType.Normal, ArgumentType.Normal])]
    private class SlotDestroyPreservingAssetsPatch
    {
        private static bool Prefix(Slot __instance)
        {
            var userRootSlot = __instance.Engine.WorldManager.FocusedWorld.LocalUser.Root.Slot;
            return __instance != userRootSlot || !Restrainite.IsRestricted(PreventionType.PreventRespawning);
        }
    }
}