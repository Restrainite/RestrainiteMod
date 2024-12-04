using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal class EnforceWhispering
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(User), nameof(User.VoiceMode), MethodType.Getter)]
    private static void EnforceWhispering_UserVoiceMode_Getter_Postfix(ref VoiceMode __result, User __instance)
    {
        if (__instance.IsLocalUser && RestrainiteMod.IsRestricted(PreventionType.EnforceWhispering))
            __result = VoiceMode.Whisper;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(User), nameof(User.VoiceMode), MethodType.Setter)]
    private static bool EnforceWhispering_UserVoiceMode_Setter_Prefix(User __instance)
    {
        return !(__instance.IsLocalUser && RestrainiteMod.IsRestricted(PreventionType.EnforceWhispering));
    }
}