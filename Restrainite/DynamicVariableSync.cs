using System;
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
    private const string RestrainiteRootSlotName = "Restrainite";
    private const string DynamicVariableSpaceName = "Restrainite";
    private static readonly FieldInfo UserRootField = AccessTools.Field(typeof(User), "userRoot");

    private readonly Configuration _configuration;

    public DynamicVariableSync(Configuration configuration)
    {
        _configuration = configuration;
        OnIntegerValueChanged += SyncIntegerToBoolean;
        OnBoolValueChanged += SyncBooleanToInteger;
    }

    private void SyncBooleanToInteger(Slot slot, PreventionType preventionType, bool value)
    {
        if (!_configuration.IsPreventionTypeEnabled(preventionType)) return;
        switch (value)
        {
            case true when _configuration.GetCounter(preventionType) <= 0:
                UpdateCounter(slot, preventionType, 1);
                break;
            case false when _configuration.GetCounter(preventionType) > 0:
                UpdateCounter(slot, preventionType, 0);
                break;
        }
    }

    private void SyncIntegerToBoolean(Slot slot, PreventionType preventionType, int value)
    {
        if (!_configuration.IsPreventionTypeEnabled(preventionType)) return;
        switch (value)
        {
            case <= 0 when _configuration.GetValue(preventionType):
                UpdateValue(slot, preventionType, false);
                break;
            case > 0 when !_configuration.GetValue(preventionType):
                UpdateValue(slot, preventionType, true);
                break;
        }
    }

    private event Action<Slot, PreventionType, bool>? OnBoolValueChanged;

    private event Action<Slot, PreventionType, int>? OnIntegerValueChanged;

    private event Action<Slot, PreventionType, string>? OnStringValueChanged;

    internal void InjectIntoUser(User value)
    {
        var userRoot = (LinkRef<UserRoot>)UserRootField.GetValue(value);
        userRoot.OnTargetChange += RootChanged;
    }

    private void RootChanged(SyncRef<UserRoot> userRoot)
    {
        if (userRoot.Target == null) return;
        ResoniteMod.Msg($"Restrainite root changed for {userRoot} {userRoot.Target.Slot}");
        var slot = userRoot.Target.Slot;
        ShowOrHideRestrainiteRootSlot(slot, slot.World.WorldManager.FocusedWorld);

        Action<IChangeable> onChanged = _ => ShowOrHideRestrainiteRootSlot(slot, slot.World.WorldManager.FocusedWorld);
        slot.World.Configuration.AccessLevel.Changed += onChanged;
        slot.World.Configuration.HideFromListing.Changed += onChanged;
        Action<World> worldFocused = _ => ShowOrHideRestrainiteRootSlot(slot, slot.World);
        userRoot.World.WorldManager.WorldFocused += worldFocused;
        ModConfigurationKey.OnChangedHandler onPresetChanged =
            _ => ShowOrHideRestrainiteRootSlot(slot, slot.World.WorldManager.FocusedWorld);
        _configuration.OnPresetChange += onPresetChanged;

        slot.OnPrepareDestroy += _ =>
        {
            slot.World.Configuration.AccessLevel.Changed -= onChanged;
            slot.World.Configuration.HideFromListing.Changed -= onChanged;
            userRoot.World.WorldManager.WorldFocused -= worldFocused;
            _configuration.OnPresetChange -= onPresetChanged;
        };
    }

    private void ShowOrHideRestrainiteRootSlot(Slot slot, World focusedWorld)
    {
        var show = slot.World == focusedWorld &&
                   _configuration.OnWorldPermission(slot.World.AccessLevel, slot.World.HideFromListing);
        ResoniteMod.Msg($"ShowOrHideRestrainiteRootSlot {show}");
        slot.RunInUpdates(0, show ? AddRestrainiteSlot(slot) : RemoveRestrainiteSlot(slot));
    }

    private Action AddRestrainiteSlot(Slot slot)
    {
        return () =>
        {
            if (slot.IsDestroyed) return;
            ResoniteMod.Msg($"Adding Restrainite DynamicVariableSpace to {slot}");
            var dynamicVariableSpace = slot.GetComponentOrAttach<DynamicVariableSpace>(
                component => component.CurrentName == DynamicVariableSpaceName
            );
            dynamicVariableSpace.OnlyDirectBinding.Value = true;
            dynamicVariableSpace.SpaceName.Value = DynamicVariableSpaceName;
            dynamicVariableSpace.Persistent = false;

            ResoniteMod.Msg($"Adding Restrainite slot to {slot}");
            var restrainiteSlot = slot.FindChildOrAdd(RestrainiteRootSlotName, false);

            foreach (var preventionType in PreventionTypes.List)
                AddOrRemoveBoolPreventionDynamicVariable(restrainiteSlot, preventionType);

            OnBoolValueChanged -= SendImpulse;
            OnBoolValueChanged += SendImpulse;
            OnStringValueChanged -= SendImpulse;
            OnStringValueChanged += SendImpulse;
            OnBoolValueChanged -= PreventGrabbing.OnChange;
            OnBoolValueChanged += PreventGrabbing.OnChange;

            slot.OnPrepareDestroy += _ =>
            {
                OnBoolValueChanged -= SendImpulse;
                OnStringValueChanged -= SendImpulse;
                OnBoolValueChanged -= PreventGrabbing.OnChange;
            };
        };
    }

    private static Action RemoveRestrainiteSlot(Slot slot)
    {
        return () =>
        {
            if (slot.IsDestroyed) return;
            ResoniteMod.Msg($"Removing Restrainite slot from {slot}");
            var restrainiteSlot = slot.FindChild(RestrainiteRootSlotName);
            if (restrainiteSlot == null) return;
            slot.RemoveAllComponents(component => component is DynamicVariableSpace
            {
                CurrentName: DynamicVariableSpaceName
            });
            restrainiteSlot.Destroy(true);
        };
    }

    private static void SendImpulse<T>(Slot restrainiteSlot, PreventionType type, T value)
    {
        var slot = restrainiteSlot.Parent;
        slot.RunInUpdates(0, () =>
        {
            if (slot.IsDestroyed || slot.IsDestroying) return;
            if (slot.Engine.WorldManager.FocusedWorld != slot.World) return;
            ProtoFluxHelper.DynamicImpulseHandler.TriggerAsyncDynamicImpulseWithArgument(
                slot, $"{DynamicVariableSpaceName} Change", true, $"{type.ToExpandedString()}:{value}"
            );
            ProtoFluxHelper.DynamicImpulseHandler.TriggerAsyncDynamicImpulseWithArgument(
                slot, $"{DynamicVariableSpaceName} {type.ToExpandedString()}", true, value
            );
        });
    }

    private void AddOrRemoveBoolPreventionDynamicVariable(Slot restrainiteSlot, PreventionType preventionType)
    {
        if (_configuration.GetDisplayedPreventionTypeConfig(preventionType, out var key)) return;
        var expandedName = preventionType.ToExpandedString();
        var isActive = _configuration.IsPreventionTypeEnabled(preventionType);
        var nameWithPrefix = $"{DynamicVariableSpaceName}/{expandedName}";

        key.OnChanged += _ => AddOrRemoveBoolPreventionDynamicVariable(restrainiteSlot, preventionType);

        if (!isActive)
        {
            var oldSlot = restrainiteSlot.FindChild(expandedName);
            if (oldSlot == null) return;
            oldSlot.RunInUpdates(0, () =>
            {
                var active = _configuration.IsPreventionTypeEnabled(preventionType);
                if (active || oldSlot.ChildrenCount != 0)
                {
                    ResoniteMod.Warn($"Unable to remove slot {oldSlot}, {active}, {oldSlot.ChildrenCount}");
                    return;
                }

                oldSlot.RemoveAllComponents(component => component is GizmoLink);
                oldSlot.RemoveAllComponents(component => component is DynamicValueVariable<bool> dynComponent &&
                                                         dynComponent.VariableName == nameWithPrefix);
                if (preventionType == PreventionType.EnforceSelectiveHearing)
                    oldSlot.RemoveAllComponents(component => component is DynamicValueVariable<string> dynComponent &&
                                                             dynComponent.VariableName == nameWithPrefix);
                if (oldSlot.ComponentCount != 0)
                {
                    ResoniteMod.Warn($"Unable to remove slot {oldSlot}, {active}, {oldSlot.ComponentCount}");
                    return;
                }

                oldSlot.Destroy(true);
            });
            return;
        }

        restrainiteSlot.RunInUpdates(0, () =>
        {
            var slot = restrainiteSlot.FindChildOrAdd(expandedName, false);
            slot.Tag = nameWithPrefix;

            CreateBoolDynVarComponent(restrainiteSlot, preventionType, slot, nameWithPrefix);
            CreateIntegerDynVarComponent(restrainiteSlot, preventionType, slot, nameWithPrefix);

            if (preventionType == PreventionType.EnforceSelectiveHearing)
                CreateSelectiveHearingListDynVar(
                    restrainiteSlot,
                    slot,
                    nameWithPrefix);
        });
    }

    private void CreateBoolDynVarComponent(Slot restrainiteSlot, PreventionType preventionType, Slot slot,
        string nameWithPrefix)
    {
        var component = slot.GetComponentOrAttach<DynamicValueVariable<bool>>(out var attached,
            search => search.VariableName.Value == nameWithPrefix);
        if (!attached) return;

        component.VariableName.Value = nameWithPrefix;
        component.Value.Value = _configuration.GetValue(preventionType);
        component.Persistent = false;

        SyncFieldEvent<bool> onValueChangeHandler = field =>
        {
            UpdateValue(restrainiteSlot, preventionType, field.Value);
        };

        component.Value.OnValueChange += onValueChangeHandler;

        var resyncEventHandler = CreateResyncEventHandler(preventionType, component);

        OnBoolValueChanged += resyncEventHandler;

        component.Destroyed += _ =>
        {
            component.Value.OnValueChange -= onValueChangeHandler;
            OnBoolValueChanged -= resyncEventHandler;
        };
    }

    private void CreateIntegerDynVarComponent(Slot restrainiteSlot, PreventionType preventionType, Slot slot,
        string nameWithPrefix)
    {
        var component = slot.GetComponentOrAttach<DynamicValueVariable<int>>(out var attached,
            search => search.VariableName.Value == nameWithPrefix);
        if (!attached) return;

        component.VariableName.Value = nameWithPrefix;
        component.Value.Value = _configuration.GetCounter(preventionType);
        component.Persistent = false;

        SyncFieldEvent<int> onValueChangeHandler = field =>
        {
            UpdateCounter(restrainiteSlot, preventionType, field.Value);
        };

        component.Value.OnValueChange += onValueChangeHandler;

        var resyncEventHandler = CreateResyncEventHandler(preventionType, component);

        OnIntegerValueChanged += resyncEventHandler;

        component.Destroyed += _ =>
        {
            component.Value.OnValueChange -= onValueChangeHandler;
            OnIntegerValueChanged -= resyncEventHandler;
        };
    }


    private void CreateSelectiveHearingListDynVar(Slot restrainiteSlot, Slot slot, string nameWithPrefix)
    {
        var component = slot.GetComponentOrAttach<DynamicValueVariable<string>>(out var attached,
            search => search.VariableName.Value == nameWithPrefix);
        if (!attached) return;

        component.VariableName.Value = nameWithPrefix;
        component.Value.Value = _configuration.SelectiveHearingUserIDsAsString;
        component.Persistent = false;

        var onValueChangeHandler = CreateOnValueChangeEventHandlerForSelectiveHearingList(restrainiteSlot,
            PreventionType.EnforceSelectiveHearing);

        component.Value.OnValueChange += onValueChangeHandler;

        var resyncEventHandler = CreateResyncEventHandler(PreventionType.EnforceSelectiveHearing, component);
        OnStringValueChanged += resyncEventHandler;

        component.Destroyed += _ =>
        {
            component.Value.OnValueChange -= onValueChangeHandler;
            OnStringValueChanged -= resyncEventHandler;
        };
    }


    private static Action<Slot, PreventionType, T> CreateResyncEventHandler<T>(PreventionType preventionType,
        DynamicValueVariable<T> component) where T : IEquatable<T>
    {
        return (_, type, value) =>
        {
            if (component.IsDestroyed) return;
            if (type != preventionType) return;
            component.RunInUpdates(0, SetComponentValue(component, () => value));
        };
    }

    private void UpdateValue(Slot restrainiteSlot, PreventionType preventionType, bool value)
    {
        if (_configuration.UpdateValue(preventionType, value)) return;

        ResoniteMod.Msg($"Value {preventionType} changed to {value}");
        OnBoolValueChanged?.Invoke(restrainiteSlot, preventionType, value);
    }

    private void UpdateCounter(Slot restrainiteSlot, PreventionType preventionType, int value)
    {
        if (_configuration.UpdateCounter(preventionType, value)) return;

        ResoniteMod.Msg($"Counter {preventionType} changed to {value}");
        OnIntegerValueChanged?.Invoke(restrainiteSlot, preventionType, value);
    }

    private SyncFieldEvent<string> CreateOnValueChangeEventHandlerForSelectiveHearingList(Slot restrainiteSlot,
        PreventionType preventionType)
    {
        return field =>
        {
            var value = field.Value;


            var oldValue = _configuration.SelectiveHearingUserIDsAsString;
            if (value == oldValue) return;
            _configuration.SelectiveHearingUserIDsAsString = value;


            ResoniteMod.Msg($"Value SelectiveHearingList changed to {value}");
            OnStringValueChanged?.Invoke(restrainiteSlot, preventionType, value);
        };
    }

    private static Action SetComponentValue<T>(DynamicValueVariable<T> component, Func<T> valueFunc)
        where T : IEquatable<T>
    {
        return () =>
        {
            if (component.IsDestroyed) return;
            var value = valueFunc();
            if (value == null) return;
            if (!value.Equals(component.Value.Value)) component.Value.Value = value;
        };
    }
}