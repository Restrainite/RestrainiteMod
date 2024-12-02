using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal static class ShowOrHideDashScreens
{
    private const string DashScreensExit = "Dash.Screens.Exit";

    private static bool ScreenToLabel(RadiantDashScreen? screen, out string? label)
    {
        if (screen?.Label == null)
        {
            label = null!;
            return false;
        }

        var localeStringDriver = screen.Label.FindNearestParent<Slot>()
            .GetComponent<LocaleStringDriver>(l => l.Target.Target == screen.Label);
        label = localeStringDriver?.LocaleString.content ?? (screen.Label.Value ?? "");
        return true;
    }

    [HarmonyPatch(typeof(UserspaceRadiantDash), nameof(UserspaceRadiantDash.Open), MethodType.Setter)]
    private static class UserspaceRadiantDashBlockOpenClosePatch
    {
        private static void Postfix(bool value, UserspaceRadiantDash __instance)
        {
            if (!value) return;

            SwitchScreen(__instance);

            HideButtons(__instance);
        }

        private static void HideButtons(UserspaceRadiantDash __instance)
        {
            foreach (var button in __instance.Dash.Slot.FindChild("Render")?.FindChild("Buttons")
                         ?.GetComponentsInChildren<RadiantDashButton>() ?? [])
            {
                var screen = button.Screen.Target;
                if (!ScreenToLabel(screen, out var label) || label == null) continue;

                if (RestrainiteMod.IsRestricted(PreventionType.ShowDashScreens) &&
                    !DashScreensExit.Equals(label) &&
                    !RestrainiteMod.Cfg.GetStrings(PreventionType.ShowDashScreens).Contains(label))
                {
                    button.Slot.ActiveSelf = false;
                    continue;
                }

                if (RestrainiteMod.IsRestricted(PreventionType.HideDashScreens) &&
                    !DashScreensExit.Equals(label) &&
                    RestrainiteMod.Cfg.GetStrings(PreventionType.HideDashScreens).Contains(label))
                {
                    button.Slot.ActiveSelf = false;
                    continue;
                }

                button.Slot.ActiveSelf = true;
            }
        }

        private static void SwitchScreen(UserspaceRadiantDash __instance)
        {
            var target = __instance.Dash.CurrentScreen.Target;
            if (target == null || !ScreenToLabel(target, out var label) || label == null) return;

            if (RestrainiteMod.IsRestricted(PreventionType.ShowDashScreens) &&
                !DashScreensExit.Equals(label) &&
                !RestrainiteMod.Cfg.GetStrings(PreventionType.ShowDashScreens).Contains(label))
                __instance.Dash.CurrentScreen.Target = __instance.Dash.GetScreen<ExitScreen>();

            if (RestrainiteMod.IsRestricted(PreventionType.HideDashScreens) &&
                !DashScreensExit.Equals(label) &&
                RestrainiteMod.Cfg.GetStrings(PreventionType.HideDashScreens).Contains(label))
                __instance.Dash.CurrentScreen.Target = __instance.Dash.GetScreen<ExitScreen>();
        }
    }
}