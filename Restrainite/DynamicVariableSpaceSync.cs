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
    private const string PasswordVariableName = "Password";
    private const string FullPasswordVariableName = $"{DynamicVariableSpaceName}/{PasswordVariableName}";

    private static readonly List<DynamicVariableSpaceSync> Spaces = [];

    private static readonly BitArray GlobalState = new(PreventionTypes.Max, false);
    private static readonly List<float> LowestFloatState = new(PreventionTypes.Max);

    private readonly WeakReference<DynamicVariableSpace> _dynamicVariableSpace;

    private readonly List<float> _localFloatValues = new(PreventionTypes.Max);

    private readonly BitArray _localState = new(PreventionTypes.Max, false);
    private readonly string _refId;

    static DynamicVariableSpaceSync()
    {
        for (var i = 0; i < PreventionTypes.Max; i++) LowestFloatState.Add(float.NaN);
    }

    private DynamicVariableSpaceSync(DynamicVariableSpace dynamicVariableSpace)
    {
        _dynamicVariableSpace = new WeakReference<DynamicVariableSpace>(dynamicVariableSpace);
        var user = dynamicVariableSpace.World.GetUserByAllocationID(dynamicVariableSpace.ReferenceID.User);
        _refId = $"Dynamic Variable Space {dynamicVariableSpace.ReferenceID} created by {user?.UserID} in {dynamicVariableSpace.World?.Name}";
        for (var i = 0; i < PreventionTypes.Max; i++) _localFloatValues.Add(float.NaN);
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

    private float GetLocalFloat(PreventionType preventionType)
    {
        return _localFloatValues[(int)preventionType];
    }

    internal void UpdateLocalState(PreventionType preventionType, bool value)
    {
        UpdateLocalStateInternal(preventionType, IsActiveForLocalUser(preventionType) && value);
    }

    internal void UpdateLocalFloatState(PreventionType preventionType, float value)
    {
        if (!preventionType.IsFloatType() ||
            (float.IsNaN(_localFloatValues[(int)preventionType]) && float.IsNaN(value)) ||
            _localFloatValues[(int)preventionType] == value) return;
        _localFloatValues[(int)preventionType] = value;
        var source = Source();
        ResoniteMod.Msg($"Local Float of {preventionType.ToExpandedString()} changed to {value}. ({source})");

        UpdateGlobalState(preventionType, source);
    }

    private void UpdateLocalStateInternal(PreventionType preventionType, bool value)
    {
        if (_localState[(int)preventionType] == value) return;
        _localState[(int)preventionType] = value;
        var source = Source();
        ResoniteMod.Msg($"Local State of {preventionType.ToExpandedString()} changed to {value}. ({source})");

        UpdateGlobalState(preventionType, source);
    }

    private void UpdateGlobalState(PreventionType preventionType, string source)
    {
        var globalState = CalculateGlobalState(preventionType);

        if (GetGlobalState(preventionType) != globalState)
        {
            GlobalState[(int)preventionType] = globalState;
            ResoniteMod.Msg($"Global State of {preventionType.ToExpandedString()} changed to {globalState}. ({source})");
            NotifyGlobalStateChange(preventionType, globalState);
        }

        if (!preventionType.IsFloatType()) return;
        var lowestFloat = CalculateLowestFloatState(preventionType);
        var currentValue = GetLowestGlobalFloat(preventionType);
        if (float.IsNaN(currentValue) && float.IsNaN(lowestFloat)) return;
        if (currentValue == lowestFloat) return;
        LowestFloatState[(int)preventionType] = lowestFloat;
        ResoniteMod.Msg($"Global Float of {preventionType.ToExpandedString()} changed to {lowestFloat}. ({source})");
    }

    private string Source()
    {
        var found = GetDynamicVariableSpace(out var internalDynamicVariableSpace);
        return found ? $"{_refId} @{internalDynamicVariableSpace?.Slot?.GlobalPosition}" : _refId;
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

    private static float CalculateLowestFloatState(PreventionType preventionType)
    {
        var globalState = float.NaN;
        lock (Spaces)
        {
            foreach (var space in Spaces)
            {
                if (!space.GetLocalState(preventionType)) continue;
                var local = space.GetLocalFloat(preventionType);
                if (float.IsNaN(local)) continue;
                if (float.IsNaN(globalState)) globalState = local;
                else if (local < globalState) globalState = local;
            }
        }

        return globalState;
    }

    private void NotifyGlobalStateChange(PreventionType preventionType, bool value)
    {
        if (!GetDynamicVariableSpace(out var dynamicVariableSpace)) return;
        RestrainiteMod.NotifyRestrictionChanged(dynamicVariableSpace.World, preventionType, value);
    }

    private void UpdateAllGlobalStates()
    {
        var source = Source();
        foreach (var preventionType in PreventionTypes.List) UpdateGlobalState(preventionType, source);
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

                var dynamicVariableSpaceSyncDynamic = Spaces[index];
                Spaces.RemoveAt(index);
                dynamicVariableSpaceSyncDynamic.Unregister(dynamicVariableSpace);
            }
            else
            {
                if (isValid)
                {
                    dynamicVariableSpaceSync = new DynamicVariableSpaceSync(dynamicVariableSpace);
                    Spaces.Add(dynamicVariableSpaceSync);
                    dynamicVariableSpaceSync.Register(dynamicVariableSpace);
                    dynamicVariableSpace.Destroyed += _ => { Remove(dynamicVariableSpace); };
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
        foreach (var dynamicValueVariable in
                 dynamicVariableSpace.Slot.GetComponents<DynamicValueVariable<string>>())
            ComponentAdded(dynamicValueVariable);
        UpdateAllGlobalStates();
    }

    private void Unregister(DynamicVariableSpace dynamicVariableSpace)
    {
        dynamicVariableSpace.Slot.ComponentAdded -= ComponentAdded;
        RestrainiteMod.Configuration.ShouldRecheckPermissions -= ShouldRecheckPermissions;
        foreach (var dynamicReferenceVariable in
                 dynamicVariableSpace.Slot.GetComponents<DynamicReferenceVariable<User>>())
        {
            dynamicReferenceVariable.VariableName.OnValueChange -= OnVariableNameUpdate;
            dynamicReferenceVariable.Reference.OnTargetChange -= OnUserRefUpdate;
        }

        foreach (var dynamicValueVariable in
                 dynamicVariableSpace.Slot.GetComponents<DynamicValueVariable<string>>())
        {
            dynamicValueVariable.VariableName.OnValueChange -= OnVariableNameUpdate;
            dynamicValueVariable.Value.OnValueChange -= OnStringValueChange;
        }

        UpdateAllGlobalStates();
    }

    private void ShouldRecheckPermissions()
    {
        CheckLocalState();
    }

    private void ComponentAdded(Component component)
    {
        switch (component)
        {
            case DynamicReferenceVariable<User> dynamicReferenceVariable:
            {
                dynamicReferenceVariable.VariableName.OnValueChange += OnVariableNameUpdate;
                if (TargetUserVariableName.Equals(dynamicReferenceVariable.VariableName.Value) ||
                    FullTargetUserVariableName.Equals(dynamicReferenceVariable.VariableName.Value))
                    OnVariableNameUpdate(dynamicReferenceVariable.VariableName);
                break;
            }
            case DynamicValueVariable<string> dynamicValueVariable:
            {
                dynamicValueVariable.VariableName.OnValueChange += OnVariableNameUpdate;
                if (PasswordVariableName.Equals(dynamicValueVariable.VariableName.Value) ||
                    FullPasswordVariableName.Equals(dynamicValueVariable.VariableName.Value))
                    OnVariableNameUpdate(dynamicValueVariable.VariableName);
                break;
            }
        }
    }

    private void OnVariableNameUpdate(SyncField<string> syncField)
    {
        var component = syncField.Component;
        switch (component)
        {
            case DynamicReferenceVariable<User> dynamicReferenceVariable:
            {
                if (TargetUserVariableName.Equals(syncField.Value) ||
                    FullTargetUserVariableName.Equals(syncField.Value))
                    dynamicReferenceVariable.Reference.OnTargetChange += OnUserRefUpdate;
                else
                    dynamicReferenceVariable.Reference.OnTargetChange -= OnUserRefUpdate;

                OnUserRefUpdate(dynamicReferenceVariable.Reference);
                break;
            }
            case DynamicValueVariable<string> dynamicValueVariable:
            {
                if (PasswordVariableName.Equals(syncField.Value) ||
                    FullPasswordVariableName.Equals(syncField.Value))
                    dynamicValueVariable.Value.OnValueChange += OnStringValueChange;
                else
                    dynamicValueVariable.Value.OnValueChange -= OnStringValueChange;

                OnStringValueChange(dynamicValueVariable.Value);
                break;
            }
        }
    }

    private void OnUserRefUpdate(SyncField<RefID> syncField)
    {
        if (syncField.Component is not DynamicReferenceVariable<User> dynamicReferenceVariable) return;

        // Force refresh value in DynamicVariableSpace Manager 
        dynamicReferenceVariable.RunInUpdates(0, () =>
        {
            dynamicReferenceVariable.UpdateLinking();
            CheckLocalState();
        });
    }

    private void OnStringValueChange(SyncField<string> syncField)
    {
        if (syncField.Component is not DynamicValueVariable<string> dynamicValueVariable) return;

        // Force refresh value in DynamicVariableSpace Manager 
        dynamicValueVariable.RunInUpdates(0, () =>
        {
            dynamicValueVariable.UpdateLinking();
            CheckLocalState();
        });
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
        if (manager.Value != dynamicVariableSpace.LocalUser) return false;

        if (!RestrainiteMod.Configuration.RequiresPassword) return true;
        var stringManager = dynamicVariableSpace.GetManager<string>(PasswordVariableName, false);
        if (stringManager == null || stringManager.ReadableValueCount == 0) return false;
        return RestrainiteMod.Configuration.IsCorrectPassword(stringManager.Value);
    }

    public static void Remove(DynamicVariableSpace dynamicVariableSpace)
    {
        DynamicVariableSpaceSync? dynamicVariableSpaceToRemove = null;
        lock (Spaces)
        {
            var index = Spaces.FindIndex(space => space.Equals(dynamicVariableSpace));
            if (index != -1)
            {
                dynamicVariableSpaceToRemove = Spaces[index];
                Spaces.RemoveAt(index);
            }
        }

        dynamicVariableSpaceToRemove?.UpdateAllGlobalStates();
    }

    public static float GetLowestGlobalFloat(PreventionType preventionType)
    {
        return LowestFloatState[(int)preventionType];
    }
}