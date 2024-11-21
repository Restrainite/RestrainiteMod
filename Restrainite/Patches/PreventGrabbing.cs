using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class PreventGrabbing
{
    private static InteractionHandler? _interactionHandler;
    
    [HarmonyPatch(typeof(InteractionHandler), "StartGrab")]
    private class InteractionHandlerStartGrabPatch
    {
        private static bool Prefix(InteractionHandler __instance)
        {
            _interactionHandler = __instance;
            return __instance.World == Userspace.UserspaceWorld ||
                   !Restrainite.GetValue(PreventionType.PreventGrabbing);
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

    
    public static void OnChange(Slot slot, PreventionType preventionType, bool value)
    {
        if (preventionType != PreventionType.PreventGrabbing || _interactionHandler == null) return;
        
        var method = AccessTools.Method(typeof(InteractionHandler), "EndGrab", [typeof(bool)]);
        method?.Invoke(_interactionHandler, [false]);
    }
    
}