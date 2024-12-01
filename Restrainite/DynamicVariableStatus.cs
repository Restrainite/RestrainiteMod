using System;
using System.Collections.Generic;
using System.Reflection;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using Restrainite.Enums;

namespace Restrainite;

public class DynamicVariableStatus(Configuration configuration)
{
    private const string RestrainiteRootSlotName = "Restrainite Status";
    private const string DynamicVariableSpaceStatusName = "Restrainite Status";

    private static readonly FieldInfo UserRootField = AccessTools.Field(typeof(User), "userRoot");

    internal void InjectIntoUser(User value)
    {
        var userRoot = (LinkRef<UserRoot>)UserRootField.GetValue(value);
        userRoot.OnTargetChange += OnUserRootTargetChanged;
    }

    private void OnUserRootTargetChanged(SyncRef<UserRoot> userRoot)
    {
        if (userRoot.Target == null) return;
        ResoniteMod.Msg($"Restrainite root changed for {userRoot} {userRoot.Target.Slot}");
        var slot = userRoot.Target.Slot;
        ShowOrHideRestrainiteRootSlot(slot);

        Action<IChangeable> onChanged = _ =>
            ShowOrHideRestrainiteRootSlot(slot);
        slot.World.Configuration.AccessLevel.Changed += onChanged;
        slot.World.Configuration.HideFromListing.Changed += onChanged;

        Action<World> worldFocused = _ => ShowOrHideRestrainiteRootSlot(slot);
        userRoot.World.WorldManager.WorldFocused += worldFocused;

        ModConfigurationKey.OnChangedHandler onPresetChanged = _ =>
        {
            if (slot.IsDestroyed || slot.IsDestroying) return;
            slot.RunInUpdates(0, () =>
                ShowOrHideRestrainiteRootSlot(slot, true));
        };
        configuration.OnPresetChange += onPresetChanged;

        slot.OnPrepareDestroy += _ =>
        {
            slot.World.Configuration.AccessLevel.Changed -= onChanged;
            slot.World.Configuration.HideFromListing.Changed -= onChanged;
            userRoot.World.WorldManager.WorldFocused -= worldFocused;
            configuration.OnPresetChange -= onPresetChanged;
        };
    }

    private void ShowOrHideRestrainiteRootSlot(Slot slot, bool skipWorldPermissions = false)
    {
        var show = slot.World == slot.World.WorldManager.FocusedWorld &&
                   !configuration.ShouldHide() &&
                   (skipWorldPermissions ||
                    configuration.OnWorldPermission(slot.World.AccessLevel, slot.World.HideFromListing));
        ResoniteMod.Msg($"ShowOrHideRestrainiteRootSlot {show}");
        slot.RunInUpdates(0, show ? AddRestrainiteSlot(slot) : RemoveRestrainiteSlot(slot));
    }

    private Action AddRestrainiteSlot(Slot slot)
    {
        return () =>
        {
            if (slot.IsDestroyed || slot.IsDestroying) return;
            ResoniteMod.Msg($"Adding Restrainite DynamicVariableSpace to {slot}");
            var dynamicVariableSpace = slot.GetComponentOrAttach<DynamicVariableSpace>(
                component => component.CurrentName == DynamicVariableSpaceStatusName
            );
            dynamicVariableSpace.OnlyDirectBinding.Value = true;
            dynamicVariableSpace.SpaceName.Value = DynamicVariableSpaceStatusName;
            dynamicVariableSpace.Persistent = false;

            ResoniteMod.Msg($"Adding Restrainite slot to {slot}");
            var restrainiteSlot = slot.FindChildOrAdd(RestrainiteRootSlotName, false);

            var handlerDict = new Dictionary<PreventionType, ModConfigurationKey.OnChangedHandler>();
            foreach (var preventionType in PreventionTypes.List)
            {
                if (configuration.GetDisplayedPreventionTypeConfig(preventionType, out var key)) continue;
                AddOrRemoveComponents(restrainiteSlot, preventionType);
                ModConfigurationKey.OnChangedHandler rerunUpdate = _ =>
                {
                    if (restrainiteSlot.IsDestroyed || restrainiteSlot.IsDestroying) return;
                    restrainiteSlot.RunInUpdates(0,
                        () => { AddOrRemoveComponents(restrainiteSlot, preventionType); });
                };
                handlerDict.Add(preventionType, rerunUpdate);
                key.OnChanged += rerunUpdate;
            }

            slot.OnPrepareDestroy += _ =>
            {
                foreach (var handler in handlerDict)
                {
                    if (configuration.GetDisplayedPreventionTypeConfig(handler.Key, out var key)) continue;
                    key.OnChanged -= handler.Value;
                }
            };
        };
    }

    private static Action RemoveRestrainiteSlot(Slot slot)
    {
        return () =>
        {
            if (slot.IsDestroyed || slot.IsDestroying) return;
            var restrainiteSlot = slot.FindChild(RestrainiteRootSlotName);
            if (restrainiteSlot == null) return;
            ResoniteMod.Msg($"Removing Restrainite slot from {slot}");
            slot.RemoveAllComponents(component => component is DynamicVariableSpace
            {
                CurrentName: DynamicVariableSpaceStatusName
            });
            restrainiteSlot.Destroy(true);
        };
    }

    private void AddOrRemoveComponents(Slot restrainiteSlot, PreventionType preventionType)
    {
        if (configuration.IsPreventionTypeEnabled(preventionType))
            CreateComponents(restrainiteSlot, preventionType);
        else
            RemoveComponents(restrainiteSlot, preventionType);
    }

    private void CreateComponents(Slot restrainiteSlot, PreventionType preventionType)
    {
        ResoniteMod.Msg($"Creating Components for {preventionType} in {restrainiteSlot}");
        var expandedName = preventionType.ToExpandedString();
        var slot = restrainiteSlot.FindChildOrAdd(expandedName, false);
        slot.Tag = $"{DynamicVariableSpaceSync.DynamicVariableSpaceName}/{expandedName}";

        // Create State Component
        var dynamicVariableBooleanComponent = new DynamicVariableComponent<bool>(preventionType, slot,
            $"{DynamicVariableSpaceStatusName}/{expandedName}", configuration.IsRestricted(preventionType));
        DynamicVariableSpaceSync.OnGlobalStateChanged +=
            dynamicVariableBooleanComponent.OnInternalStateChange;
        dynamicVariableBooleanComponent.OnDestroyed += () =>
            DynamicVariableSpaceSync.OnGlobalStateChanged -=
                dynamicVariableBooleanComponent.OnInternalStateChange;
    }

    private void RemoveComponents(Slot restrainiteSlot, PreventionType preventionType)
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
                                                 dynComponent.VariableName == nameWithPrefix);

        if (oldSlot.ComponentCount != 0)
        {
            ResoniteMod.Warn($"Unable to remove slot {oldSlot.Name}, {oldSlot.ComponentCount}");
            return;
        }

        oldSlot.Destroy(true);
    }

    private class DynamicVariableComponent<T>
    {
        private readonly PreventionType _preventionType;
        private DynamicValueVariable<T>? _component;

        internal DynamicVariableComponent(PreventionType preventionType,
            Slot slot, string nameWithPrefix, T defaultValue)
        {
            _preventionType = preventionType;
            _component = slot.GetComponentOrAttach<DynamicValueVariable<T>>(out var attached,
                search => search.VariableName.Value == nameWithPrefix);
            if (!attached) return;

            _component.VariableName.Value = nameWithPrefix;
            _component.Value.Value = defaultValue;
            _component.Persistent = false;

            _component.Destroyed += Destroyed;
        }

        internal event Action? OnDestroyed;

        private void Destroyed(IDestroyable destroyable)
        {
            if (_component == null) return;
            _component.Destroyed -= Destroyed;
            OnDestroyed?.Invoke();
            _component = null;
        }

        internal void OnInternalStateChange(Slot _, PreventionType preventionType, T value)
        {
            if (_component == null || _component.IsDestroyed) return;
            if (preventionType != _preventionType) return;
            _component.RunInUpdates(0, () =>
            {
                if (_component.IsDestroyed) return;
                if (value == null) return;
                if (!value.Equals(_component.Value.Value)) _component.Value.Value = value;
            });
        }
    }
}