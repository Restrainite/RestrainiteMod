using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventOpeningDash
{
    static PreventOpeningDash()
    {
        DynamicVariableSpaceSync.OnGlobalStateChanged += OnChange;
    }

    private static void OnChange(Slot slot, PreventionType preventionType, bool value)
    {
        if (preventionType != PreventionType.PreventOpeningDash ||
            !value ||
            !RestrainiteMod.Cfg.IsPreventionTypeEnabled(preventionType))
            return;

        Userspace.Current.RunSynchronously(delegate
        {
            Userspace.UserspaceWorld.GetGloballyRegisteredComponent<UserspaceRadiantDash>().Open = false;
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UserspaceRadiantDash), nameof(UserspaceRadiantDash.Open), MethodType.Setter)]
    private static void PreventOpeningDash_UserspaceRadiantDashOpen_Setter_Prefix(ref bool value)
    {
        if (RestrainiteMod.IsRestricted(PreventionType.PreventOpeningDash)) value = false;
    }
}