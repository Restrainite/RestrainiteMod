using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventGrabbing
{
    internal static void Initialize()
    {
        RestrainiteMod.OnRestrictionChanged += OnChange;
    }

    private static void OnChange(PreventionType preventionType, bool value)
    {
        if (preventionType != PreventionType.PreventGrabbing ||
            !value)
            return;

        var method = AccessTools.Method(typeof(InteractionHandler), "EndGrab", [typeof(bool)]);
        if (method == null) return;
        var user = Engine.Current?.WorldManager?.FocusedWorld?.LocalUser;
        if (user == null) return;
        var leftInteractionHandler = user.GetInteractionHandler(Chirality.Left);
        if (leftInteractionHandler != null)
            leftInteractionHandler.RunInUpdates(0, () => { method.Invoke(leftInteractionHandler, [false]); });

        var rightInteractionHandler = user.GetInteractionHandler(Chirality.Right);
        if (rightInteractionHandler != null)
            rightInteractionHandler.RunInUpdates(0, () => { method.Invoke(rightInteractionHandler, [false]); });
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(InteractionHandler), "StartGrab")]
    private static bool PreventGrabbing_InteractionHandlerStartGrab_Prefix(InteractionHandler __instance)
    {
        return __instance.World == Userspace.UserspaceWorld ||
                !RestrainiteMod.IsRestricted(PreventionType.PreventGrabbing);
    }
}