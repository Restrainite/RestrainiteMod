using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal static class PreventGrabbing
{
    private static InteractionHandler? _interactionHandler;

    internal static void Initialize()
    {
        RestrainiteMod.OnRestrictionChanged += OnChange;
    }

    private static void OnChange(PreventionType preventionType, bool value)
    {
        if (preventionType != PreventionType.PreventGrabbing ||
            !value ||
            _interactionHandler == null)
            return;

        var method = AccessTools.Method(typeof(InteractionHandler), "EndGrab", [typeof(bool)]);
        var user = Engine.Current.WorldManager.FocusedWorld.LocalUser;
        if (user == null) return;
        var leftInteractionHandler = user.GetInteractionHandler(Chirality.Left);
        if (leftInteractionHandler != null)
            leftInteractionHandler.RunInUpdates(0, () => { method?.Invoke(leftInteractionHandler, [false]); });

        var rightInteractionHandler = user.GetInteractionHandler(Chirality.Right);
        if (rightInteractionHandler != null)
            rightInteractionHandler.RunInUpdates(0, () => { method?.Invoke(rightInteractionHandler, [false]); });
    }

    [HarmonyPatch(typeof(InteractionHandler), "StartGrab")]
    private class InteractionHandlerStartGrabPatch
    {
        private static bool Prefix(InteractionHandler __instance)
        {
            _interactionHandler = __instance;
            return __instance.World == Userspace.UserspaceWorld ||
                   !RestrainiteMod.IsRestricted(PreventionType.PreventGrabbing);
        }
    }

    [HarmonyPatch(typeof(InteractionHandler), "EndGrab")]
    private class InteractionHandlerEndGrabPatch
    {
        private static void Prefix(InteractionHandler __instance)
        {
            if (_interactionHandler != __instance) return;
            _interactionHandler = null;
        }
    }
}