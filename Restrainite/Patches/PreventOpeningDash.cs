using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal static class PreventOpeningDash
{
    static PreventOpeningDash()
    {
        RestrainiteMod.OnRestrictionChanged += OnChange;
    }

    private static void OnChange(PreventionType preventionType, bool value)
    {
        if (preventionType != PreventionType.PreventOpeningDash ||
            !value)
            return;

        Userspace.Current.RunSynchronously(() =>
        {
            Userspace.UserspaceWorld.GetGloballyRegisteredComponent<UserspaceRadiantDash>().Open = false;
        });
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