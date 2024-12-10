using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;
using static FrooxEngine.VoiceMode;

namespace Restrainite.Patches;

internal class EnforceWhispering
{
    private static VoiceMode _originalVoiceMode = Whisper;

    internal static void Initialize()
    {
        RestrainiteMod.OnRestrictionChanged += OnRestrictionChanged;
    }

    private static void OnRestrictionChanged(PreventionType preventionType, bool value)
    {
        if (preventionType != PreventionType.EnforceWhispering) return;

        var user = Engine.Current.WorldManager.FocusedWorld.LocalUser;
        if (user == null) return;

        user.Root.Slot.RunInUpdates(0, () =>
        {
            if (RestrainiteMod.IsRestricted(PreventionType.EnforceWhispering))
            {
                if (!value || user.VoiceMode is not (Normal or Shout or Broadcast)) return;
                _originalVoiceMode = user.VoiceMode;
                user.VoiceMode = Whisper;
            }
            else if (!value && user.VoiceMode is Whisper)
            {
                user.VoiceMode = _originalVoiceMode;
            }
        });
    }

    [HarmonyPatch(typeof(User), nameof(User.VoiceMode), MethodType.Setter)]
    private class VoiceModeSetterPatch
    {
        private static bool Prefix(VoiceMode value, User __instance)
        {
            return !(__instance.IsLocalUser &&
                     RestrainiteMod.IsRestricted(PreventionType.EnforceWhispering) &&
                     value is Normal or Shout or Broadcast);
        }
    }
}