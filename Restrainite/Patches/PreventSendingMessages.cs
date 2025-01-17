using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventSendingMessages
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ContactsDialog), "TrySendMessage")]
    private static bool ContactsDialog_TrySendMessage_Prefix(ref bool __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventSendingMessages)) return true;
        __result = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ContactsDialog), "OnSendMessage")]
    private static bool ContactsDialog_OnSendMessage_Prefix()
    {
        return !RestrainiteMod.IsRestricted(PreventionType.PreventSendingMessages);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ContactsDialog), "OnStartRecordingVoiceMessage")]
    private static bool ContactsDialog_OnStartRecordingVoiceMessage_Prefix()
    {
        return !RestrainiteMod.IsRestricted(PreventionType.PreventSendingMessages);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ContactsDialog), "OnStopRecordingVoiceMessage")]
    private static bool ContactsDialog_OnStopRecordingVoiceMessage_Prefix()
    {
        return !RestrainiteMod.IsRestricted(PreventionType.PreventSendingMessages);
    }
}