using FrooxEngine;
using FrooxEngine.CommonAvatar;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventUserScaling
{
    internal static void Initialize()
    {
        RestrainiteMod.OnRestrictionChanged += OnRestrictionChanged;
    }

    private static void OnRestrictionChanged(PreventionType preventionType, bool value)
    {
        if (preventionType != PreventionType.ResetUserScale || !value) return;
        var user = Engine.Current.WorldManager.FocusedWorld.LocalUser;
        if (user == null) return;
        var activeUserRoot = user.Root.Slot.ActiveUserRoot;
        activeUserRoot.RunInUpdates(0, () => activeUserRoot.SetUserScale(activeUserRoot.GetDefaultScale(), 0.25f));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LocomotionController), nameof(LocomotionController.CanScale), MethodType.Getter)]
    private static bool PreventUserScaling_LocomotionControllerCanScale_Getter_Prefix(ref bool __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventUserScaling)) return true;
        __result = false;
        return false;
    }
}