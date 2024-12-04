using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal static class PreventOpeningContextMenu
{
    static PreventOpeningContextMenu()
    {
        RestrainiteMod.OnRestrictionChanged += OnChange;
    }

    private static void OnChange(PreventionType preventionType, bool value)
    {
        if (preventionType != PreventionType.PreventOpeningContextMenu ||
            !value)
            return;

        var user = Engine.Current.WorldManager.FocusedWorld.LocalUser;
        user.Root.RunInUpdates(0, () => user.CloseContextMenu(null!));
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