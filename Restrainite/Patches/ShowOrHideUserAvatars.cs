using System;
using System.Linq;
using System.Threading;
using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class ShowOrHideUserAvatars
{
    private static readonly ThreadLocal<bool> InUpdateBlocking = new();

    internal static void Initialize()
    {
        RestrainiteMod.OnRestrictionChanged += OnRestrictionChanged;
    }

    private static void OnRestrictionChanged(PreventionType preventionType, bool value)
    {
        if (preventionType is not (PreventionType.ShowUserAvatars or PreventionType.HideUserAvatars)) return;
        var userList = Engine.Current?.WorldManager?.FocusedWorld?.AllUsers;
        if (userList is null) return;
        foreach (var slot in userList.Select(user => user?.Root?.Slot))
        {
            slot?.RunInUpdates(0, () =>
            {
                slot.ForeachComponentInChildren<Component>(c => c?.MarkChangeDirty());
            });
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(User), nameof(User.UpdateBlocking))]
    private static void User_UpdateBlocking_Prefix()
    {
        InUpdateBlocking.Value = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(User), nameof(User.UpdateBlocking))]
    private static void User_UpdateBlocking_Postfix()
    {
        InUpdateBlocking.Value = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(User), nameof(User.IsRenderingLocallyBlocked), MethodType.Getter)]
    private static void User_IsRenderingLocallyBlocked_Postfix(ref bool __result, User __instance)
    {
        if (InUpdateBlocking.Value || __result || __instance == __instance.LocalUser) return;
        if (RestrainiteMod.IsRestricted(PreventionType.ShowUserAvatars) &&
            !RestrainiteMod.GetStrings(PreventionType.ShowUserAvatars).Contains(__instance.UserID))
        {
            __result = true;
            return;
        }

        if (!RestrainiteMod.IsRestricted(PreventionType.HideUserAvatars) ||
            !RestrainiteMod.GetStrings(PreventionType.HideUserAvatars).Contains(__instance.UserID)) return;
        __result = true;
    }
}