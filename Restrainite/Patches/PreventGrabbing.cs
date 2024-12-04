using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventGrabbing
{
    private static InteractionHandler? _interactionHandler;

    static PreventGrabbing()
    {
        DynamicVariableSpaceSync.OnGlobalStateChanged += OnChange;
    }

    private static void OnChange(Slot slot, PreventionType preventionType, bool value)
    {
        if (preventionType != PreventionType.PreventGrabbing ||
            !value ||
            _interactionHandler == null ||
            !RestrainiteMod.Cfg.IsPreventionTypeEnabled(preventionType))
            return;

        var method = AccessTools.Method(typeof(InteractionHandler), "EndGrab", [typeof(bool)]);
        method?.Invoke(_interactionHandler, [false]);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(InteractionHandler), "StartGrab")]
    private static bool PreventGrabbing_InteractionHandlerStartGrab_Prefix(InteractionHandler __instance)
    {
        _interactionHandler = __instance;
        return __instance.World == Userspace.UserspaceWorld ||
                !RestrainiteMod.IsRestricted(PreventionType.PreventGrabbing);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(InteractionHandler), "EndGrab")]
    private static void PreventGrabbing_InteractionHandlerEndGrab_Prefix(InteractionHandler __instance)
    {
        if (_interactionHandler != __instance) return;
        _interactionHandler = null;
    }
}