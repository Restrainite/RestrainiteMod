using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class EnforceWhispering
{
    [HarmonyPatch(typeof(User), nameof(User.VoiceMode), MethodType.Getter)]
    private class VoiceModeGetterPatch
    {
        private static void Postfix(ref VoiceMode __result, User __instance)
        {
            if (__instance.IsLocalUser && Restrainite.IsRestricted(PreventionType.EnforceWhispering))
                __result = VoiceMode.Whisper;
        }
    }

    [HarmonyPatch(typeof(User), nameof(User.VoiceMode), MethodType.Setter)]
    private class VoiceModeSetterPatch
    {
        private static bool Prefix(User __instance)
        {
            return !(__instance.IsLocalUser && Restrainite.IsRestricted(PreventionType.EnforceWhispering));
        }
    }
}