using System;
using System.Collections;
using System.Collections.Generic;
using FrooxEngine;
using ResoniteModLoader;
using Restrainite.Enums;

namespace Restrainite;

internal class DynamicVariableSpaceSync
{
    private static readonly List<DynamicVariableSpaceSync> Spaces = [];

    private static readonly BitArray GlobalState = new(PreventionTypes.Max, false);

    private readonly WeakReference<DynamicVariableSpace> _dynamicVariableSpace;

    private readonly BitArray _localState = new(PreventionTypes.Max, false);

    private DynamicVariableSpaceSync(DynamicVariableSpace dynamicVariableSpace)
    {
        _dynamicVariableSpace = new WeakReference<DynamicVariableSpace>(dynamicVariableSpace);
    }

    internal static event Action<Slot, PreventionType, bool>? OnGlobalStateChanged;

    private bool Equals(DynamicVariableSpace dynamicVariableSpace)
    {
        var found = _dynamicVariableSpace.TryGetTarget(out var internalDynamicVariableSpace);
        return found && internalDynamicVariableSpace != null &&
               internalDynamicVariableSpace == dynamicVariableSpace;
    }

    private bool GetLocalState(PreventionType preventionType)
    {
        return IsActiveForLocalUser() && _localState[(int)preventionType];
    }

    private void UpdateLocalState(PreventionType preventionType, bool value)
    {
        value = IsActiveForLocalUser() && value;
        if (_localState[(int)preventionType] == value) return;
        _localState[(int)preventionType] = value;

        var globalState = CalculateGlobalState(preventionType);

        if (GetGlobalState(preventionType) == globalState) return;
        GlobalState[(int)preventionType] = value;
        ResoniteMod.Msg($"Value of {preventionType.ToExpandedString()} changed to {value}");
        UpdateRestrictionState(preventionType, value);
    }

    private static bool CalculateGlobalState(PreventionType preventionType)
    {
        bool globalState;
        lock (Spaces)
        {
            globalState = Spaces.FindIndex(space => space.GetLocalState(preventionType)) != -1;
        }

        return globalState;
    }

    private void UpdateRestrictionState(PreventionType preventionType, bool value)
    {
        if (!_dynamicVariableSpace.TryGetTarget(out var dynamicVariableSpace) || dynamicVariableSpace == null)
            return;
        if (!RestrainiteMod.Cfg.IsPreventionTypeEnabled(preventionType)) return;
        dynamicVariableSpace.RunInUpdates(0, () =>
        {
            ResoniteMod.Msg($"State of {preventionType} changed to {value}");
            OnGlobalStateChanged?.Invoke(dynamicVariableSpace.LocalUser.LocalUserRoot.Slot, preventionType, value);
        });
    }

    internal static bool GetGlobalState(PreventionType preventionType)
    {
        return GlobalState[(int)preventionType];
    }

    internal static bool UpdateListAndGetIfValid(DynamicVariableSpace dynamicVariableSpace,
        out DynamicVariableSpaceSync? dynamicVariableSpaceSync)
    {
        var isValid = IsRestrainiteDynamicSpace(dynamicVariableSpace);
        lock (Spaces)
        {
            var index = Spaces.FindIndex(space => space.Equals(dynamicVariableSpace));
            if (index != -1)
            {
                if (isValid)
                {
                    dynamicVariableSpaceSync = Spaces[index];
                    return true;
                }

                Spaces.RemoveAt(index);
            }
            else
            {
                if (isValid)
                {
                    dynamicVariableSpaceSync = new DynamicVariableSpaceSync(dynamicVariableSpace);
                    Spaces.Add(dynamicVariableSpaceSync);
                    return true;
                }
            }
        }

        dynamicVariableSpaceSync = null;
        return false;
    }

    private static bool IsRestrainiteDynamicSpace(DynamicVariableSpace dynamicVariableSpace)
    {
        return dynamicVariableSpace is { IsDestroyed: false, IsDisposed: false, CurrentName: "Restrainite" };
    }

    private bool IsActiveForLocalUser()
    {
        if (!_dynamicVariableSpace.TryGetTarget(out var dynamicVariableSpace) || dynamicVariableSpace == null)
            return false;
        if (!IsRestrainiteDynamicSpace(dynamicVariableSpace) ||
            dynamicVariableSpace.World != dynamicVariableSpace.World?.WorldManager?.FocusedWorld) return false;
        var manager = dynamicVariableSpace.GetManager<User>("Target User", false);
        if (manager == null || manager.ReadableValueCount == 0) return false;
        return manager.Value == dynamicVariableSpace.LocalUser;
    }

    internal void Register(DynamicVariableSpaceFinder.BooleanValueManagerWrapper booleanValueManager)
    {
        booleanValueManager.OnChange += UpdateLocalState;
    }

    public static void Remove(DynamicVariableSpace dynamicVariableSpace)
    {
        lock (Spaces)
        {
            var index = Spaces.FindIndex(space => space.Equals(dynamicVariableSpace));
            if (index != -1) Spaces.RemoveAt(index);
        }
    }
}