using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventOpeningContextMenu
{
    internal static void Initialize()
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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(InteractionHandler), "TryOpenContextMenu")]
    private static bool PreventOpeningContextMenu_InteractionHandlerTryOpenContextMenu_Prefix(
        InteractionHandler __instance)
    {
        return __instance.World == Userspace.UserspaceWorld ||
               !RestrainiteMod.IsRestricted(PreventionType.PreventOpeningContextMenu);
    }
}