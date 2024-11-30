using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using ResoniteModLoader;
using Restrainite.Enums;
using SkyFrost.Base;

namespace Restrainite;

public class Configuration
{
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


    private ModConfiguration? _config;

    private BitArray? _currentPreventionTypes;

    public Configuration()
    {
        foreach (var presetType in PresetTypes.List)
            _presetStore.Add(presetType, new ModConfigurationKey<bool[]>(
                $"PresetStore{presetType}", "", () => [], true));
    }

    public void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
    {
        ResoniteMod.Msg("Define configuration");
        builder.Key(_presetConfig);
        builder.Key(_presetStartupConfig);

        foreach (var presetType in PresetTypes.List) builder.Key(_presetStore[presetType]);

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
    }

    public void Init(ModConfiguration? config = null)
    {
        _config = config;
        _presetConfig.OnChanged += OnPresetSelected;
        foreach (var displayedPreventionType in _displayedPreventionTypes)
            displayedPreventionType.Value.OnChanged += OnPreventionTypeConfigChanged(displayedPreventionType.Key);

        var presetOnStartup = _config?.GetValue(_presetStartupConfig) ?? PresetChangeType.None;
        if (presetOnStartup != PresetChangeType.DoNotChange) _config?.Set(_presetConfig, (PresetType)presetOnStartup);

        var currentPreset = _config?.GetValue(_presetConfig) ?? PresetType.None;
        _currentPreventionTypes = GetCustomStored(currentPreset);
        _config?.Save(true);
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
                    _currentPreventionTypes = customStored;
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
        _currentPreventionTypes = customStored;
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
        _currentPreventionTypes = GetCustomStored(selectedPreset);
        foreach (var preventionType in PreventionTypes.List)
        {
            if (GetDisplayedPreventionTypeConfig(preventionType, out var configurationKey)) continue;
            var preventionTypeValue = _currentPreventionTypes[(int)preventionType];
            if (_config?.GetValue(configurationKey) == preventionTypeValue) continue;
            _config?.Set(configurationKey, preventionTypeValue);
            ResoniteMod.Msg($"{preventionType} set to {preventionTypeValue}.");
        }
    }

    internal bool GetDisplayedPreventionTypeConfig(PreventionType preventionType,
        out ModConfigurationKey<bool> configurationKey)
    {
        var found = _displayedPreventionTypes.TryGetValue(preventionType, out configurationKey);
        return !found || configurationKey == null;
    }

    internal bool IsRestricted(PreventionType preventionType)
    {
        return IsPreventionTypeEnabled(preventionType) && DynamicVariableSpaceSync.GetGlobalState(preventionType);
    }

    internal IImmutableSet<string> GetStrings(PreventionType preventionType)
    {
        return IsPreventionTypeEnabled(preventionType)
            ? DynamicVariableSpaceSync.GetGlobalStrings(preventionType)
            : ImmutableHashSet<string>.Empty;
    }

    internal bool IsPreventionTypeEnabled(PreventionType preventionType)
    {
        if (GetDisplayedPreventionTypeConfig(preventionType, out var key)) return false;
        var configValue = false;
        var foundConfigValue = _config?.TryGetValue(key, out configValue) ?? false;
        return foundConfigValue && configValue;
    }

    public bool ShouldHide()
    {
        return _config?.GetValue(_presetConfig) == PresetType.None;
    }

    public bool OnWorldPermission(SessionAccessLevel? sessionAccessLevel, bool hideFromListing)
    {
        var currentPreset = _config?.GetValue(_presetConfig);
        if (sessionAccessLevel == null) return currentPreset != PresetType.None;
        var worldPermissionType =
            WorldPermissionTypes.FromResonite((SessionAccessLevel)sessionAccessLevel, hideFromListing);
        var key = _changeOnWorldPermissionChangeDict[worldPermissionType];
        if (key == null) return currentPreset != PresetType.None;
        var changePreset = _config?.GetValue(key);
        switch (changePreset)
        {
            case PresetChangeType.None:
                if (currentPreset == PresetType.None) return false;
                _config?.Set(_presetConfig, PresetType.None);
                return false;
            case PresetChangeType.All:
            case PresetChangeType.StoredPresetAlpha:
            case PresetChangeType.StoredPresetBeta:
            case PresetChangeType.StoredPresetGamma:
            case PresetChangeType.StoredPresetDelta:
            case PresetChangeType.StoredPresetOmega:
                if (currentPreset == (PresetType)changePreset) return true;
                _config?.Set(_presetConfig, (PresetType)changePreset);
                return true;
            case PresetChangeType.DoNotChange:
            case null:
                return currentPreset != PresetType.None;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    internal event ModConfigurationKey.OnChangedHandler? OnPresetChange
    {
        add => _presetConfig.OnChanged += value;
        remove => _presetConfig.OnChanged -= value;
    }
}