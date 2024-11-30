using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class PreventOpeningDash
{
    public static void OnChange(Slot slot, PreventionType preventionType, bool value)
    {
        if (preventionType != PreventionType.PreventOpeningDash ||
            !value ||
            !RestrainiteMod.Cfg.IsPreventionTypeEnabled(preventionType))
            return;

        Userspace.UserspaceWorld.GetGloballyRegisteredComponent<UserspaceRadiantDash>().Open = false;
    }

    [HarmonyPatch(typeof(UserspaceRadiantDash), nameof(UserspaceRadiantDash.Open), MethodType.Setter)]
    private class UserspaceRadiantDashBlockOpenClosePatch
    {
        private static void Prefix(ref bool value)
        {
            if (RestrainiteMod.IsRestricted(PreventionType.PreventOpeningDash)) value = false;
        }
    }
}