using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Elements.Core;
using FrooxEngine;
using ResoniteModLoader;
using Restrainite.Enums;

namespace Restrainite;

internal class DynamicVariableSpaceSync
{
    internal const string DynamicVariableSpaceName = "Restrainite";
    private const string TargetUserVariableName = "Target User";
    private const string FullTargetUserVariableName = $"{DynamicVariableSpaceName}/{TargetUserVariableName}";

    private static readonly List<DynamicVariableSpaceSync> Spaces = [];

    private static readonly BitArray GlobalState = new(PreventionTypes.Max, false);

    private readonly WeakReference<DynamicVariableSpace> _dynamicVariableSpace;

    private readonly BitArray _localState = new(PreventionTypes.Max, false);

    private DynamicVariableSpaceSync(DynamicVariableSpace dynamicVariableSpace)
    {
        _dynamicVariableSpace = new WeakReference<DynamicVariableSpace>(dynamicVariableSpace);
    }

    private bool Equals(DynamicVariableSpace dynamicVariableSpace)
    {
        var found = _dynamicVariableSpace.TryGetTarget(out var internalDynamicVariableSpace);
        return found && internalDynamicVariableSpace != null &&
               internalDynamicVariableSpace == dynamicVariableSpace;
    }

    private bool GetLocalState(PreventionType preventionType)
    {
        return _localState[(int)preventionType];
    }

    internal void UpdateLocalState(PreventionType preventionType, bool value)
    {
        UpdateLocalStateInternal(preventionType, IsActiveForLocalUser(preventionType) && value);
    }

    private void UpdateLocalStateInternal(PreventionType preventionType, bool value)
    {
        if (_localState[(int)preventionType] == value) return;
        _localState[(int)preventionType] = value;
        ResoniteMod.Msg($"Local State of {preventionType} changed to {value}");

        UpdateGlobalState(preventionType);
     }
    
    private void UpdateGlobalState(PreventionType preventionType)
    {
        var globalState = CalculateGlobalState(preventionType);

        if (GetGlobalState(preventionType) == globalState) return;
        GlobalState[(int)preventionType] = globalState;
        ResoniteMod.Msg($"Value of {preventionType.ToExpandedString()} changed to {globalState}");
        NotifyGlobalStateChange(preventionType, globalState);
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

    private void NotifyGlobalStateChange(PreventionType preventionType, bool value)
    {
        if (!GetDynamicVariableSpace(out var dynamicVariableSpace)) return;
        RestrainiteMod.NotifyRestrictionChanged(dynamicVariableSpace, preventionType, value);
    }

    private void UpdateAllGlobalStates()
    {
        foreach (var preventionType in PreventionTypes.List)
        {
            UpdateGlobalState(preventionType);
        }
    }

    internal static bool GetGlobalState(PreventionType preventionType)
    {
        return GlobalState[(int)preventionType];
    }

    internal static IImmutableSet<string> GetGlobalStrings(PreventionType preventionType)
    {
        List<DynamicVariableSpaceSync> spaces;
        lock (Spaces)
        {
            spaces = Spaces.Where(space => space.GetLocalState(preventionType)).ToList();
        }

        return spaces.SelectMany(space => space.GetLocalStrings(preventionType)).ToImmutableHashSet();
    }

    private IImmutableSet<string> GetLocalStrings(PreventionType preventionType)
    {
        if (!GetDynamicVariableSpace(out var dynamicVariableSpace)) return ImmutableHashSet<string>.Empty;
        var manager = dynamicVariableSpace.GetManager<string>(preventionType.ToExpandedString(), false);
        if (manager == null || manager.ReadableValueCount == 0) return ImmutableHashSet<string>.Empty;
        return SplitStringToList(manager.Value ?? string.Empty);
    }

    private static IImmutableSet<string> SplitStringToList(object? value)
    {
        var splitArray = (value as string)?.Split(',') ?? [];
        return splitArray.Select(t => t.Trim())
                .Where(trimmed => trimmed.Length != 0)
                .ToList()
                .ToImmutableHashSet()
            ;
    }

    internal static bool UpdateListAndGetIfValid(DynamicVariableSpace dynamicVariableSpace,
        out DynamicVariableSpaceSync? dynamicVariableSpaceSync)
    {
        var isValid = IsValidRestrainiteDynamicSpace(dynamicVariableSpace);
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

                Spaces[index].Unregister(dynamicVariableSpace);
                Spaces.RemoveAt(index);
            }
            else
            {
                if (isValid)
                {
                    dynamicVariableSpaceSync = new DynamicVariableSpaceSync(dynamicVariableSpace);
                    Spaces.Add(dynamicVariableSpaceSync);
                    dynamicVariableSpaceSync.Register(dynamicVariableSpace);
                    return true;
                }
            }
        }

        dynamicVariableSpaceSync = null;
        return false;
    }

    private void Register(DynamicVariableSpace dynamicVariableSpace)
    {
        dynamicVariableSpace.Slot.ComponentAdded += ComponentAdded;
        RestrainiteMod.Configuration.ShouldRecheckPermissions += ShouldRecheckPermissions;
        foreach (var dynamicReferenceVariable in
                 dynamicVariableSpace.Slot.GetComponents<DynamicReferenceVariable<User>>())
            ComponentAdded(dynamicReferenceVariable);
        UpdateAllGlobalStates();
    }

    private void Unregister(DynamicVariableSpace dynamicVariableSpace)
    {
        dynamicVariableSpace.Slot.ComponentAdded -= ComponentAdded;
        RestrainiteMod.Configuration.ShouldRecheckPermissions -= ShouldRecheckPermissions;
        foreach (var dynamicReferenceVariable in
                 dynamicVariableSpace.Slot.GetComponents<DynamicReferenceVariable<User>>())
        {
            dynamicReferenceVariable.VariableName.OnValueChange -= OnUserVariableNameUpdate;
            dynamicReferenceVariable.Reference.OnTargetChange -= OnUserRefUpdate;
        }
        UpdateAllGlobalStates();
    }

    private void ShouldRecheckPermissions()
    {
        CheckLocalState();
    }

    private void ComponentAdded(Component component)
    {
        if (component is not DynamicReferenceVariable<User> dynamicReferenceVariable) return;

        dynamicReferenceVariable.VariableName.OnValueChange += OnUserVariableNameUpdate;
        if (TargetUserVariableName.Equals(dynamicReferenceVariable.VariableName.Value) ||
            FullTargetUserVariableName.Equals(dynamicReferenceVariable.VariableName.Value))
            OnUserVariableNameUpdate(dynamicReferenceVariable.VariableName);
    }

    private void OnUserVariableNameUpdate(SyncField<string> syncField)
    {
        var component = syncField.Component;
        if (component is not DynamicReferenceVariable<User> dynamicReferenceVariable) return;

        if (TargetUserVariableName.Equals(syncField.Value) ||
            FullTargetUserVariableName.Equals(syncField.Value))
            dynamicReferenceVariable.Reference.OnTargetChange += OnUserRefUpdate;
        else
            dynamicReferenceVariable.Reference.OnTargetChange -= OnUserRefUpdate;

        OnUserRefUpdate(dynamicReferenceVariable.Reference);
    }

    private void OnUserRefUpdate(SyncField<RefID> syncField)
    {
        if (syncField.Component is not DynamicReferenceVariable<User> dynamicReferenceVariable) return;

        // Force refresh value in DynamicVariableSpace Manager 
        dynamicReferenceVariable.UpdateLinking();
        CheckLocalState();
    }

    private void CheckLocalState()
    {
        if (!GetDynamicVariableSpace(out var dynamicVariableSpace)) return;

        foreach (var preventionType in PreventionTypes.List)
            CheckLocalState(dynamicVariableSpace, preventionType);
    }

    private void CheckLocalState(DynamicVariableSpace dynamicVariableSpace, PreventionType preventionType)
    {
        var state = IsActiveForLocalUser(preventionType);
        if (state)
        {
            var manager = dynamicVariableSpace.GetManager<bool>(preventionType.ToExpandedString(), false);
            state = manager != null && manager.ReadableValueCount > 0 && manager.Value;
        }

        if (_localState[(int)preventionType] != state) UpdateLocalStateInternal(preventionType, state);
    }

    private bool GetDynamicVariableSpace(out DynamicVariableSpace dynamicVariableSpace)
    {
        return _dynamicVariableSpace.TryGetTarget(out dynamicVariableSpace);
    }

    private static bool IsValidRestrainiteDynamicSpace(DynamicVariableSpace dynamicVariableSpace)
    {
        return dynamicVariableSpace is { IsDestroyed: false, IsDisposed: false, CurrentName: DynamicVariableSpaceName };
    }

    private bool IsActiveForLocalUser(PreventionType preventionType)
    {
        if (!GetDynamicVariableSpace(out var dynamicVariableSpace)) return false;
        if (!IsValidRestrainiteDynamicSpace(dynamicVariableSpace) ||
            !RestrainiteMod.Configuration.AllowRestrictionsFromWorld(dynamicVariableSpace.World, preventionType))
            return false;
        var manager = dynamicVariableSpace.GetManager<User>(TargetUserVariableName, false);
        if (manager == null || manager.ReadableValueCount == 0) return false;
        return manager.Value == dynamicVariableSpace.LocalUser;
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