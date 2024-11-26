using System.Collections.Generic;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using Restrainite.Enums;

namespace Restrainite;

internal class DynamicVariableSpaceFinder
{
    private static List<DynamicVariableSpace> _dynamicVariableSpaces = [];

    private static void UpdateDynamicVariableSpacesList(DynamicVariableSpace dynamicVariableSpace)
    {
        var list = _dynamicVariableSpaces.FindAll(space =>
            dynamicVariableSpace != space && space is { IsDestroyed: false, IsDisposed: false });

        if (dynamicVariableSpace.CurrentName == "Restrainite" &&
            dynamicVariableSpace is { IsDestroyed: false, IsDisposed: false })
        {
            ResoniteMod.Msg("Dynamic variable space Restrainite found {slot}");
            list.Add(dynamicVariableSpace);
        }

        _dynamicVariableSpaces = list;
    }

    internal static bool IsActive(PreventionType preventionType)
    {
        foreach (var space in _dynamicVariableSpaces)
        {
            if (space is not { IsDestroyed: false, IsDisposed: false, Slot: not null } ||
                space.Slot == space.Slot.ActiveUserRoot?.Slot ||
                !HasLocalUserInUserRefVariable(space)) continue;

            var manager = space.GetManager<bool>(preventionType.ToExpandedString(), false);
            if (manager == null || manager.ReadableValueCount == 0) continue;
            if (manager.Value) return true;
        }

        return false;
    }

    private static bool HasLocalUserInUserRefVariable(DynamicVariableSpace dynamicVariableSpace)
    {
        var manager = dynamicVariableSpace.GetManager<User>("Target User", false);
        if (manager == null || manager.ReadableValueCount == 0) return false;
        return manager.Value == dynamicVariableSpace.LocalUser;
    }

    [HarmonyPatch(typeof(DynamicVariableSpace), "OnStart")]
    private static class DynamicVariableSpaceOnStartPatch
    {
        private static void Postfix(DynamicVariableSpace __instance)
        {
            UpdateDynamicVariableSpacesList(__instance);
        }
    }

    [HarmonyPatch(typeof(DynamicVariableSpace), "OnChanges")]
    private static class DynamicVariableSpaceOnChangesPatch
    {
        private static void Postfix(DynamicVariableSpace __instance)
        {
            UpdateDynamicVariableSpacesList(__instance);
        }
    }

    [HarmonyPatch(typeof(DynamicVariableSpace), "OnDuplicate")]
    private static class DynamicVariableSpaceOnDuplicatePatch
    {
        private static void Postfix(DynamicVariableSpace __instance)
        {
            UpdateDynamicVariableSpacesList(__instance);
        }
    }

    [HarmonyPatch(typeof(DynamicVariableSpace), "OnDispose")]
    private static class DynamicVariableSpaceOnDisposePatch
    {
        private static void Postfix(DynamicVariableSpace __instance)
        {
            UpdateDynamicVariableSpacesList(__instance);
        }
    }
}