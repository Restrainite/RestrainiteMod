using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class PreventInviteContact
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ContactsDialog), "OnInviteContact")]
    private static bool ContactsDialog_OnInviteContact_Prefix()
    {
        return !RestrainiteMod.IsRestricted(PreventionType.PreventInviteContact);
    }
}