using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class PreventOpeningDash
{
    [HarmonyPatch(typeof(UserspaceRadiantDash), nameof(UserspaceRadiantDash.Open), MethodType.Setter)]
    private class UserspaceRadiantDashBlockOpenClosePatch
    {
        private static void Prefix(ref bool value)
        {
            if (Restrainite.GetValue(PreventionType.PreventOpeningDash)) value = false;
        }
    }
}