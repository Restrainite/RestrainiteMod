using System;
using Elements.Core;
using FrooxEngine;
using ResoniteModLoader;
using Restrainite.Enums;

namespace Restrainite;

internal class RestrictionStateOutput
{
    private const string RestrainiteRootSlotName = "Restrainite Status";
    private const string DynamicVariableSpaceStatusName = "Restrainite Status";
    private readonly Configuration _configuration;
    private readonly WeakReference<Slot> _userSlot;
    private bool _isBeingShown;
    private WeakReference<Slot>? _oldSlot;

    internal RestrictionStateOutput(Configuration configuration, Slot userSlot)
    {
        _configuration = configuration;
        _userSlot = new WeakReference<Slot>(userSlot);

        ShowOrHideRestrainiteRootSlot();
    }

    internal void OnShouldRecheckPermissions()
    {
        if (!_userSlot.TryGetTarget(out var userSlot)) return;
        if (userSlot.IsDestroyed || userSlot.IsDestroying) return;
        userSlot.RunInUpdates(0, ShowOrHideRestrainiteRootSlot);
    }

    private void ShowOrHideRestrainiteRootSlot()
    {
        if (!_userSlot.TryGetTarget(out var userSlot)) return;
        var show = _configuration.AllowRestrictionsFromWorld(userSlot.World);
        if (!show && !_isBeingShown) return;
        _isBeingShown = show;

        userSlot.RunInUpdates(0, show ? AddRestrainiteSlot : RemoveRestrainiteSlot);
    }

    private void AddRestrainiteSlot()
    {
        if (!_userSlot.TryGetTarget(out var userSlot)) return;
        if (userSlot.IsDestroyed || userSlot.IsDestroying) return;
        CreateDynamicVariableSpace();

        ResoniteMod.Msg($"Adding Restrainite slot to {_userSlot}");
        DeleteOldSlotIfMoved();
        var restrainiteSlot = userSlot.FindChildOrAdd(RestrainiteRootSlotName, false);
        _oldSlot = new WeakReference<Slot>(restrainiteSlot);

        CreateVersionComponent(restrainiteSlot);

        AddOrRemoveComponents(restrainiteSlot);
    }

    private void DeleteOldSlotIfMoved()
    {
        if (_oldSlot == null ||
            !_oldSlot.TryGetTarget(out var slot) ||
            !_userSlot.TryGetTarget(out var userSlot) ||
            slot.Parent == userSlot) return;
        slot.Destroy(true);
    }

    private void CreateDynamicVariableSpace()
    {
        if (!_userSlot.TryGetTarget(out var userSlot)) return;
        ResoniteMod.Msg($"Adding Restrainite DynamicVariableSpace to {_userSlot}");
        var dynamicVariableSpace = userSlot.GetComponentOrAttach<DynamicVariableSpace>(
            component => DynamicVariableSpaceStatusName.Equals(component.CurrentName)
        );
        dynamicVariableSpace.OnlyDirectBinding.Value = true;
        dynamicVariableSpace.SpaceName.Value = DynamicVariableSpaceStatusName;
        dynamicVariableSpace.Persistent = false;
    }

    private void RemoveRestrainiteSlot()
    {
        if (_userSlot.TryGetTarget(out var userSlot) &&
            !userSlot.IsDestroyed &&
            !userSlot.IsDestroying)
        {
            userSlot.RemoveAllComponents(component => component is DynamicVariableSpace
            {
                CurrentName: DynamicVariableSpaceStatusName
            });

            var restrainiteSlot = userSlot.FindChild(RestrainiteRootSlotName);
            restrainiteSlot?.Destroy(true);
        }

        if (_oldSlot == null || !_oldSlot.TryGetTarget(out var slot)) return;
        if (slot.IsDestroying || slot.IsDestroyed) return;
        slot.Destroy(true);
    }

    private void AddOrRemoveComponents(Slot restrainiteSlot)
    {
        foreach (var preventionType in PreventionTypes.List)
            AddOrRemoveComponents(restrainiteSlot, preventionType);
    }

    private void AddOrRemoveComponents(Slot restrainiteSlot, PreventionType preventionType)
    {
        if (_configuration.IsPreventionTypeEnabled(preventionType))
            CreateComponents(restrainiteSlot, preventionType);
        else
            RemoveComponents(restrainiteSlot, preventionType);
    }

    private void CreateVersionComponent(Slot restrainiteSlot)
    {
        var versionName = $"{DynamicVariableSpaceStatusName}/Version";
        var component = restrainiteSlot.GetComponentOrAttach<DynamicValueVariable<uint3>>(
            search => versionName.Equals(search.VariableName.Value));
        component.VariableName.Value = versionName;
        component.Value.Value = _configuration.Version;
    }

    private static void CreateComponents(Slot restrainiteSlot, PreventionType preventionType)
    {
        ResoniteMod.Msg($"Creating Components for {preventionType} in {restrainiteSlot}");
        var expandedName = preventionType.ToExpandedString();
        var slot = restrainiteSlot.FindChildOrAdd(expandedName, false);
        slot.Tag = $"{DynamicVariableSpaceSync.DynamicVariableSpaceName}/{expandedName}";

        var component = GetComponentOrCreate(preventionType, slot, $"{DynamicVariableSpaceStatusName}/{expandedName}");
        Action<PreventionType, bool> onUpdate = (type, value) =>
        {
            if (preventionType == type)
                component.Value.Value = value;
        };
        RestrainiteMod.OnRestrictionChanged += onUpdate;
        component.Disposing += _ => { RestrainiteMod.OnRestrictionChanged -= onUpdate; };
    }

    private static DynamicValueVariable<bool> GetComponentOrCreate(PreventionType preventionType, Slot slot,
        string nameWithPrefix)
    {
        var component = slot.GetComponentOrAttach<DynamicValueVariable<bool>>(out var attached,
            search => nameWithPrefix.Equals(search.VariableName.Value));

        component.VariableName.Value = nameWithPrefix;
        component.Value.Value = RestrainiteMod.IsRestricted(preventionType);
        component.Persistent = false;
        return component;
    }

    private static void RemoveComponents(Slot restrainiteSlot, PreventionType preventionType)
    {
        ResoniteMod.Msg($"Removing Components for {preventionType} in {restrainiteSlot}");
        var expandedName = preventionType.ToExpandedString();
        var oldSlot = restrainiteSlot.FindChild(expandedName);

        if (oldSlot == null) return;
        if (oldSlot.IsDestroyed || oldSlot.IsDestroying) return;

        if (oldSlot.ChildrenCount != 0)
        {
            ResoniteMod.Warn($"Unable to remove slot {oldSlot.Name}, {oldSlot.ChildrenCount}");
            return;
        }

        var nameWithPrefix = $"{DynamicVariableSpaceStatusName}/{expandedName}";
        oldSlot.RemoveAllComponents(component => component is GizmoLink);
        oldSlot.RemoveAllComponents(component => component is DynamicValueVariable<bool> dynComponent &&
                                                 nameWithPrefix.Equals(dynComponent.VariableName.Value));

        if (oldSlot.ComponentCount != 0)
        {
            ResoniteMod.Warn($"Unable to remove slot {oldSlot.Name}, {oldSlot.ComponentCount}");
            return;
        }

        oldSlot.Destroy(true);
    }
}