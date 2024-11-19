using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class PreventOpeningContextMenu
{
    [HarmonyPatch(typeof(InteractionHandler), "TryOpenContextMenu")]
    private class InteractionHandlerTryOpenContextMenuPatch
    {
        private static bool Prefix(InteractionHandler __instance)
        {
            return __instance.World == Userspace.UserspaceWorld ||
                   !Restrainite.GetValue(PreventionType.PreventOpeningContextMenu);
        }
    }
}