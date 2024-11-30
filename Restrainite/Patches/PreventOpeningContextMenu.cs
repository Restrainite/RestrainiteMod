using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class PreventOpeningContextMenu
{
    public static void OnChange(Slot slot, PreventionType preventionType, bool value)
    {
        if (preventionType != PreventionType.PreventOpeningContextMenu ||
            !value ||
            !RestrainiteMod.Cfg.IsPreventionTypeEnabled(preventionType))
            return;

        slot.World.LocalUser.CloseContextMenu(null!);
    }

    [HarmonyPatch(typeof(InteractionHandler), "TryOpenContextMenu")]
    private class InteractionHandlerTryOpenContextMenuPatch
    {
        private static bool Prefix(InteractionHandler __instance)
        {
            return __instance.World == Userspace.UserspaceWorld ||
                   !RestrainiteMod.IsRestricted(PreventionType.PreventOpeningContextMenu);
        }
    }
}