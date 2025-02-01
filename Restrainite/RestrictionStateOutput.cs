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
        var show = _configuration.AllowRestrictionsFromWorld(userSlot.World) ||
                   userSlot.World == Userspace.UserspaceWorld;
        if (!show && !_isBeingShown) return;
        _isBeingShown = show;

        userSlot.RunInUpdates(0, show ? AddRestrainiteSlot : RemoveRestrainiteSlot);
    }

    private void AddRestrainiteSlot()
    {
        if (!_userSlot.TryGetTarget(out var userSlot)) return;
        if (userSlot.IsDestroyed || userSlot.IsDestroying) return;
        CreateDynamicVariableSpace();

        ResoniteMod.Msg($"Adding Restrainite slot to {userSlot.Name} {userSlot.ReferenceID} " +
                        $"in {userSlot.Parent?.Name} {userSlot.World?.Name}");
        DeleteOldSlotIfMoved();
        var restrainiteSlot = userSlot.FindChildOrAdd(RestrainiteRootSlotName, false);
        _oldSlot = new WeakReference<Slot>(restrainiteSlot);

        CreateVersionComponent(restrainiteSlot);

        if (restrainiteSlot.World == Userspace.UserspaceWorld) CreatePresetComponent(restrainiteSlot);

        CreatePasswordComponent(restrainiteSlot);

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
        ResoniteMod.Msg($"Adding Restrainite DynamicVariableSpace to {userSlot.Name} {userSlot.ReferenceID} " +
                        $"in {userSlot.Parent?.Name} {userSlot.World?.Name}");
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

    private static void CreateVersionComponent(Slot restrainiteSlot)
    {
        const string versionName = $"{DynamicVariableSpaceStatusName}/Version";
        var component = restrainiteSlot.GetComponentOrAttach<DynamicValueVariable<uint3>>(
            search => versionName.Equals(search.VariableName.Value));
        component.VariableName.Value = versionName;
        component.Persistent = false;
        var version = RestrainiteMod.AssemblyVersion;
        var versionArray = new uint3(
            version.Major < 0 ? 0 : (uint)version.Major,
            version.Minor < 0 ? 0 : (uint)version.Minor,
            version.Build < 0 ? 0 : (uint)version.Build);
        component.Value.Value = versionArray;
    }

    private void CreatePresetComponent(Slot restrainiteSlot)
    {
        const string presetName = $"{DynamicVariableSpaceStatusName}/Preset";
        var component = restrainiteSlot.GetComponentOrAttach<DynamicValueVariable<string>>(
            out var attached,
            search => presetName.Equals(search.VariableName.Value));
        component.VariableName.Value = presetName;
        component.Persistent = false;
        component.Value.Value = _configuration.CurrentPreset?.ToString() ?? "";

        if (!attached) return;
        component.Value.OnValueChange += OnPresetChanged;
    }

    private void CreatePasswordComponent(Slot restrainiteSlot)
    {
        const string passwordName = $"{DynamicVariableSpaceStatusName}/Requires Password";
        var component = restrainiteSlot.GetComponentOrAttach<DynamicValueVariable<bool>>(
            search => passwordName.Equals(search.VariableName.Value));
        component.VariableName.Value = passwordName;
        component.Persistent = false;
        component.Value.Value = _configuration.RequiresPassword;
    }

    private static void OnPresetChanged(SyncField<string> syncField)
    {
        try
        {
            var presetType = (PresetType)Enum.Parse(typeof(PresetType), syncField.Value);
            if ((int)presetType >= PresetTypes.Max || (int)presetType < 0) throw new OverflowException();
            RestrainiteMod.Configuration.CurrentPreset = presetType;
        }
        catch (Exception ex) when (ex is ArgumentNullException or ArgumentException or OverflowException)
        {
            syncField.Slot.RunInUpdates(0,
                () => syncField.Value = RestrainiteMod.Configuration.CurrentPreset?.ToString() ?? "");
        }
    }

    private static void CreateComponents(Slot restrainiteSlot, PreventionType preventionType)
    {
        var expandedName = preventionType.ToExpandedString();
        var slot = restrainiteSlot.FindChild(expandedName);
        if (slot == null)
        {
            ResoniteMod.Msg($"Creating Components for {preventionType} in {restrainiteSlot.Name} " +
                            $"{restrainiteSlot.ReferenceID} in {restrainiteSlot.Parent?.Name} {restrainiteSlot.World?.Name}");
            slot = restrainiteSlot.AddSlot(expandedName, false);
        }

        slot.Tag = $"{DynamicVariableSpaceSync.DynamicVariableSpaceName}/{expandedName}";

        var component = GetComponentOrCreate(preventionType, slot,
            $"{DynamicVariableSpaceStatusName}/{expandedName}",
            out var attached);

        if (!attached) return;
        Action<PreventionType, bool> onUpdate = (type, value) =>
        {
            if (preventionType == type) restrainiteSlot.RunInUpdates(0, () => component.Value.Value = value);
        };
        RestrainiteMod.OnRestrictionChanged += onUpdate;
        component.Disposing += _ => { RestrainiteMod.OnRestrictionChanged -= onUpdate; };
    }

    private static DynamicValueVariable<bool> GetComponentOrCreate(PreventionType preventionType, Slot slot,
        string nameWithPrefix, out bool attached)
    {
        var component = slot.GetComponentOrAttach<DynamicValueVariable<bool>>(out attached,
            search => nameWithPrefix.Equals(search.VariableName.Value));

        component.VariableName.Value = nameWithPrefix;
        component.Value.Value = RestrainiteMod.IsRestricted(preventionType);
        component.Persistent = false;
        return component;
    }

    private static void RemoveComponents(Slot restrainiteSlot, PreventionType preventionType)
    {
        var expandedName = preventionType.ToExpandedString();
        var oldSlot = restrainiteSlot.FindChild(expandedName);

        if (oldSlot == null) return;
        if (oldSlot.IsDestroyed || oldSlot.IsDestroying) return;

        if (oldSlot.ChildrenCount != 0)
        {
            ResoniteMod.Warn($"Unable to remove slot {oldSlot.Name} {oldSlot.ReferenceID} in " +
                             $"{oldSlot.Parent?.Name} {oldSlot.World?.Name}, too many children: {oldSlot.ChildrenCount}");
            return;
        }

        var nameWithPrefix = $"{DynamicVariableSpaceStatusName}/{expandedName}";
        oldSlot.RemoveAllComponents(component => component is GizmoLink);
        oldSlot.RemoveAllComponents(component => component is DynamicValueVariable<bool> dynComponent &&
                                                 nameWithPrefix.Equals(dynComponent.VariableName.Value));

        if (oldSlot.ComponentCount != 0)
        {
            ResoniteMod.Warn($"Unable to remove slot {oldSlot.Name} {oldSlot.ReferenceID} in " +
                             $"{oldSlot.Parent?.Name} {oldSlot.World?.Name}, too many components: {oldSlot.ComponentCount}");
            return;
        }

        ResoniteMod.Msg($"Removing Components for {preventionType} in {restrainiteSlot.Name} " +
                        $"{restrainiteSlot.ReferenceID} in {restrainiteSlot.Parent?.Name} {restrainiteSlot.World?.Name}");

        oldSlot.Destroy(true);
    }
}