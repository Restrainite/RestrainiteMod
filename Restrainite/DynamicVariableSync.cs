using System;
using System.Collections.Generic;
using System.Reflection;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using ResoniteModLoader;
using Restrainite.Enums;
using Restrainite.Patches;

namespace Restrainite;

public class DynamicVariableSync
{
    private const string RestrainiteRootSlotName = "Restrainite Status";
    private const string DynamicVariableSpaceName = "Restrainite Status";
    private static readonly FieldInfo UserRootField = AccessTools.Field(typeof(User), "userRoot");

    private readonly Configuration _configuration;

    public DynamicVariableSync(Configuration configuration)
    {
        _configuration = configuration;
        DynamicVariableSpaceSync.OnGlobalStateChanged += SendDynamicImpulse;
        DynamicVariableSpaceSync.OnGlobalStateChanged += PreventGrabbing.OnChange;
        DynamicVariableSpaceSync.OnGlobalStateChanged += PreventOpeningDash.OnChange;
        DynamicVariableSpaceSync.OnGlobalStateChanged += PreventOpeningContextMenu.OnChange;
    }

    private void SendDynamicImpulse<T>(Slot restrainiteSlot, PreventionType preventionType, T value)
    {
        if (restrainiteSlot.IsDestroyed || restrainiteSlot.IsDestroying) return;
        var slot = restrainiteSlot.Parent;
        if (slot == null) return;
        slot.RunInUpdates(0, () =>
        {
            if (slot.IsDestroyed || slot.IsDestroying) return;
            if (slot.Engine.WorldManager.FocusedWorld != slot.World) return;
            if (!_configuration.IsPreventionTypeEnabled(preventionType)) return;
            ProtoFluxHelper.DynamicImpulseHandler.TriggerAsyncDynamicImpulseWithArgument(
                slot, $"{DynamicVariableSpaceName} Change", true,
                $"{preventionType.ToExpandedString()}:{typeof(T)}:{value}"
            );
            ProtoFluxHelper.DynamicImpulseHandler.TriggerAsyncDynamicImpulseWithArgument(
                slot, $"{DynamicVariableSpaceName} {preventionType.ToExpandedString()}", true, value
            );
        });
    }

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
        _configuration.OnPresetChange += onPresetChanged;

        slot.OnPrepareDestroy += _ =>
        {
            slot.World.Configuration.AccessLevel.Changed -= onChanged;
            slot.World.Configuration.HideFromListing.Changed -= onChanged;
            userRoot.World.WorldManager.WorldFocused -= worldFocused;
            _configuration.OnPresetChange -= onPresetChanged;
        };
    }

    private void ShowOrHideRestrainiteRootSlot(Slot slot, bool skipWorldPermissions = false)
    {
        var show = slot.World == slot.World.WorldManager.FocusedWorld &&
                   !_configuration.ShouldHide() &&
                   (skipWorldPermissions ||
                    _configuration.OnWorldPermission(slot.World.AccessLevel, slot.World.HideFromListing));
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
                component => component.CurrentName == DynamicVariableSpaceName
            );
            dynamicVariableSpace.OnlyDirectBinding.Value = true;
            dynamicVariableSpace.SpaceName.Value = DynamicVariableSpaceName;
            dynamicVariableSpace.Persistent = false;

            ResoniteMod.Msg($"Adding Restrainite slot to {slot}");
            var restrainiteSlot = slot.FindChildOrAdd(RestrainiteRootSlotName, false);

            var handlerDict = new Dictionary<PreventionType, ModConfigurationKey.OnChangedHandler>();
            foreach (var preventionType in PreventionTypes.List)
            {
                if (_configuration.GetDisplayedPreventionTypeConfig(preventionType, out var key)) continue;
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
                    if (_configuration.GetDisplayedPreventionTypeConfig(handler.Key, out var key)) continue;
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
                CurrentName: DynamicVariableSpaceName
            });
            restrainiteSlot.Destroy(true);
        };
    }

    private void AddOrRemoveComponents(Slot restrainiteSlot, PreventionType preventionType)
    {
        if (_configuration.IsPreventionTypeEnabled(preventionType))
            CreateComponents(restrainiteSlot, preventionType);
        else
            RemoveComponents(restrainiteSlot, preventionType);
    }

    private void CreateComponents(Slot restrainiteSlot, PreventionType preventionType)
    {
        ResoniteMod.Msg($"Creating Components for {preventionType} in {restrainiteSlot}");
        var expandedName = preventionType.ToExpandedString();
        var slot = restrainiteSlot.FindChildOrAdd(expandedName, false);
        var nameWithPrefix = $"{DynamicVariableSpaceName}/{expandedName}";
        slot.Tag = nameWithPrefix;

        // Create State Component
        var dynamicVariableBooleanComponent = new DynamicVariableComponent<bool>(preventionType, slot,
            nameWithPrefix, _configuration.IsRestricted(preventionType));
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

        var nameWithPrefix = $"{DynamicVariableSpaceName}/{expandedName}";
        oldSlot.RemoveAllComponents(component => component is GizmoLink);
        oldSlot.RemoveAllComponents(component => component is DynamicValueVariable<bool> dynComponent &&
                                                 dynComponent.VariableName == nameWithPrefix);
        oldSlot.RemoveAllComponents(component => component is DynamicValueVariable<int> dynComponent &&
                                                 dynComponent.VariableName == nameWithPrefix);
        if (preventionType.HasStringVariable())
            oldSlot.RemoveAllComponents(component => component is DynamicValueVariable<string> dynComponent &&
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