using System;
using System.Collections;
using System.Collections.Generic;
using Elements.Core;
using FrooxEngine;
using ResoniteModLoader;
using Restrainite.Enums;

namespace Restrainite;

internal class Configuration
{
    private readonly ModConfigurationKey<bool> _allowRestrictionsFromFocusedWorldOnly = new(
        "Allow Restrictions from Focused World only",
        "Restrictions can only be modified from the focused world",
        () => true);

    private readonly Dictionary<WorldPermissionType, ModConfigurationKey<PresetChangeType>>
        _changeOnWorldPermissionChangeDict = new();

    private readonly Dictionary<PreventionType, ModConfigurationKey<bool>> _displayedPreventionTypes = new();

    private readonly ModConfigurationKey<PresetType> _presetConfig = new("Preset",
        "Select a preset",
        () => PresetType.None);

    private readonly ModConfigurationKey<PresetChangeType> _presetStartupConfig = new(
        "Preset on Startup",
        "Select a preset that should be loaded on game startup. DoNotChange will not change the preset on startup.",
        () => PresetChangeType.None);

    private readonly Dictionary<PresetType, ModConfigurationKey<bool[]>> _presetStore = new();

    private readonly ModConfigurationKey<bool> _sendDynamicImpulses = new(
        "Send dynamic impulses",
        "Send a dynamic impulse to the user root slot every time a restriction is activated or deactivated.",
        () => true);


    private ModConfiguration? _config;

    public uint3 Version;

    public Configuration()
    {
        foreach (var presetType in PresetTypes.List)
            _presetStore.Add(presetType, new ModConfigurationKey<bool[]>(
                $"PresetStore{presetType}", "", () => [], true));
    }

    internal bool SendDynamicImpulses => _config?.GetValue(_sendDynamicImpulses) ?? true;

    internal event Action? ShouldRecheckPermissions;

    public void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
    {
        ResoniteMod.Msg("Define configuration");
        builder.Key(_presetConfig);
        builder.Key(_presetStartupConfig);

        foreach (var key in _presetStore.Values) builder.Key(key);

        foreach (var preventionType in PreventionTypes.List)
        {
            var key = new ModConfigurationKey<bool>($"Allow {preventionType} Restriction",
                "Should others be able to control this ability.", () => false);
            builder.Key(key);
            _displayedPreventionTypes.Add(preventionType, key);
        }

        foreach (var worldPermissionType in WorldPermissionTypes.List)
        {
            var key = new ModConfigurationKey<PresetChangeType>(
                $"Change to Preset, if world permissions are {worldPermissionType.AsExpandedString()}",
                "", () => worldPermissionType.Default()
            );
            builder.Key(key);
            _changeOnWorldPermissionChangeDict.Add(worldPermissionType, key);
        }

        builder.Key(_allowRestrictionsFromFocusedWorldOnly);
        builder.Key(_sendDynamicImpulses);
    }

    public void Init(ModConfiguration? config, string version)
    {
        _config = config;
        _presetConfig.OnChanged += OnPresetSelected;
        foreach (var displayedPreventionType in _displayedPreventionTypes)
            displayedPreventionType.Value.OnChanged += OnPreventionTypeConfigChanged(displayedPreventionType.Key);

        var presetOnStartup = _config?.GetValue(_presetStartupConfig) ?? PresetChangeType.None;
        if (presetOnStartup != PresetChangeType.DoNotChange) _config?.Set(_presetConfig, (PresetType)presetOnStartup);

        foreach (var key in _presetStore.Values)
            key.OnChanged += _ => ShouldRecheckPermissions?.Invoke();

        foreach (var key in _changeOnWorldPermissionChangeDict.Values)
            key.OnChanged += _ => ShouldRecheckPermissions?.Invoke();

        _presetConfig.OnChanged += _ => ShouldRecheckPermissions?.Invoke();
        _allowRestrictionsFromFocusedWorldOnly.OnChanged += _ => ShouldRecheckPermissions?.Invoke();

        _config?.Save(true);

        Version = ParseVersion(version);
    }

    private static uint3 ParseVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length != 3) throw new FormatException("Invalid version format");
        var major = uint.Parse(parts[0]);
        var minor = uint.Parse(parts[1]);
        var patch = uint.Parse(parts[2]);
        return new uint3(major, minor, patch);
    }

    private ModConfigurationKey.OnChangedHandler OnPreventionTypeConfigChanged(PreventionType preventionType)
    {
        return value =>
        {
            ResoniteMod.Msg($"Config for {preventionType} changed to {value}.");
            var boolValue = value as bool? ?? false;
            var presetType = _config?.GetValue(_presetConfig) ?? PresetType.Customized;
            switch (presetType)
            {
                case PresetType.None when !boolValue:
                    ResoniteMod.Msg($"Config for {preventionType} changed {presetType} to {boolValue}.");
                    return;
                case PresetType.None:
                    SwitchToCustomized(preventionType, true);
                    ResoniteMod.Msg($"Config for {preventionType} changed {presetType} to {boolValue}.");
                    return;
                case PresetType.All when boolValue:
                    ResoniteMod.Msg($"Config for {preventionType} changed {presetType} to {boolValue}.");
                    return;
                case PresetType.All:
                    SwitchToCustomized(preventionType, false);
                    ResoniteMod.Msg($"Config for {preventionType} changed {presetType} to {boolValue}.");
                    return;
                case PresetType.Customized:
                case PresetType.StoredPresetAlpha:
                case PresetType.StoredPresetBeta:
                case PresetType.StoredPresetGamma:
                case PresetType.StoredPresetDelta:
                case PresetType.StoredPresetOmega:
                default:
                    var customStored = GetCustomStored(presetType);
                    customStored.Set((int)preventionType, boolValue);
                    SetCustomStored(presetType, customStored);
                    ResoniteMod.Msg($"Config for {preventionType} changed {presetType} to {boolValue}.");
                    return;
            }
        };
    }

    private void SwitchToCustomized(PreventionType preventionType, bool value)
    {
        var customStored = GetCustomStored(PresetType.Customized);
        customStored.SetAll(!value);
        customStored.Set((int)preventionType, value);
        SetCustomStored(PresetType.Customized, customStored);
        _config?.Set(_presetConfig, PresetType.Customized);
    }

    private BitArray GetCustomStored(PresetType presetType)
    {
        if (_config == null) return new BitArray(PreventionTypes.Max, false);
        if (presetType == PresetType.None) return new BitArray(PreventionTypes.Max, false);
        if (presetType == PresetType.All) return new BitArray(PreventionTypes.Max, true);
        var savedPresetFound = _config.TryGetValue(_presetStore[presetType], out var value);
        if (!savedPresetFound || value == null) return new BitArray(PreventionTypes.Max, false);
        var bitArray = new BitArray(value)
        {
            Length = PreventionTypes.Max
        };
        return bitArray;
    }

    private void SetCustomStored(PresetType presetType, BitArray bitArray)
    {
        var array = new bool[bitArray.Count];
        bitArray.CopyTo(array, 0);
        _config?.Set(_presetStore[presetType], array);
    }

    private void OnPresetSelected(object? value)
    {
        ResoniteMod.Msg($"Restrainite preset changed to {value}.");
        var selectedPreset = value as PresetType? ?? PresetType.None;
        var preventionTypeValues = GetCustomStored(selectedPreset);
        foreach (var preventionType in PreventionTypes.List)
        {
            if (GetDisplayedPreventionTypeConfig(preventionType, out var configurationKey)) continue;
            var preventionTypeValue = preventionTypeValues[(int)preventionType];
            if (_config?.GetValue(configurationKey) == preventionTypeValue) continue;
            _config?.Set(configurationKey, preventionTypeValue);
            ResoniteMod.Msg($"{preventionType.ToExpandedString()} set to {preventionTypeValue}.");
        }
    }

    private bool GetDisplayedPreventionTypeConfig(PreventionType preventionType,
        out ModConfigurationKey<bool> configurationKey)
    {
        var found = _displayedPreventionTypes.TryGetValue(preventionType, out configurationKey);
        return !found || configurationKey == null;
    }

    internal bool IsPreventionTypeEnabled(PreventionType preventionType)
    {
        if (GetDisplayedPreventionTypeConfig(preventionType, out var key)) return false;
        var configValue = false;
        var foundConfigValue = _config?.TryGetValue(key, out configValue) ?? false;
        return foundConfigValue && configValue;
    }

    internal void OnWorldPermissionChanged(World world)
    {
        // If not in the focused world, we only trigger recheck permissions, which will remove it from those worlds
        // which should be restricted.
        if (world != world.WorldManager.FocusedWorld)
        {
            ShouldRecheckPermissions?.Invoke();
            return;
        }

        // Focused world, we change the preset.
        var currentPreset = _config?.GetValue(_presetConfig);
        var changePreset = GetWorldPresetChangeType(world);
        switch (changePreset)
        {
            case PresetChangeType.None:
                if (currentPreset == PresetType.None) return;
                _config?.Set(_presetConfig, PresetType.None);
                return;
            case PresetChangeType.All:
            case PresetChangeType.StoredPresetAlpha:
            case PresetChangeType.StoredPresetBeta:
            case PresetChangeType.StoredPresetGamma:
            case PresetChangeType.StoredPresetDelta:
            case PresetChangeType.StoredPresetOmega:
                if (currentPreset == (PresetType)changePreset) return;
                _config?.Set(_presetConfig, (PresetType)changePreset);
                return;
            case PresetChangeType.DoNotChange:
            case null:
                ShouldRecheckPermissions?.Invoke();
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private PresetChangeType? GetWorldPresetChangeType(World? world)
    {
        var worldPermissionType = world?.ToWorldPermissionType();
        if (worldPermissionType == null) return null;
        var found = _changeOnWorldPermissionChangeDict.TryGetValue(
            (WorldPermissionType)worldPermissionType, out var key);
        return !found || key == null ? null! : _config?.GetValue(key);
    }

    internal bool AllowRestrictionsFromWorld(World? world, PreventionType? preventionType = null)
    {
        if (_config?.GetValue(_presetConfig) == PresetType.None) return false;

        if (preventionType.HasValue && !IsPreventionTypeEnabled(preventionType.Value)) return false;

        return world == world?.WorldManager.FocusedWorld ||
               (
                   !(_config?.GetValue(_allowRestrictionsFromFocusedWorldOnly) ?? true) &&
                   GetWorldPresetChangeType(world) != PresetChangeType.None
               );
    }
}