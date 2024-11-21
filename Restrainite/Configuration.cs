using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ResoniteModLoader;
using Restrainite.Enums;
using SkyFrost.Base;

namespace Restrainite;

public class Configuration
{
    private readonly Dictionary<WorldPermissionType, ModConfigurationKey<PresetChangeType>>
        _changeOnWorldPermissionChangeDict = new();

    private readonly Dictionary<PreventionType, int> _currentPreventCounter = new();

    private readonly BitArray _currentPreventValues = new(PreventionTypes.Max, false);

    private readonly Dictionary<PreventionType, ModConfigurationKey<bool>> _displayedPreventionTypes = new();

    private readonly ModConfigurationKey<PresetType> _presetConfig = new("Preset",
        "Select a preset",
        () => PresetType.None);

    private readonly ModConfigurationKey<PresetChangeType> _presetStartupConfig = new(
        "Preset on Startup",
        "Select a preset that should be loaded on game startup. DoNotChange will not change the preset on startup.",
        () => PresetChangeType.None);

    private readonly Dictionary<PresetType, ModConfigurationKey<bool[]>> _presetStore = new();

    private readonly ModConfigurationKey<string> _selectiveHearingList = new(
        "Selective Hearing UserID List",
        "Comma separated list of user id",
        () => "", true);

    private ModConfiguration? _config;

    private BitArray? _currentPreventionTypes;

    public Configuration()
    {
        foreach (var presetType in PresetTypes.List)
            _presetStore.Add(presetType, new ModConfigurationKey<bool[]>(
                $"PresetStore{presetType}", "", () => [], true));
        foreach (var preventionType in PreventionTypes.List) _currentPreventCounter.Add(preventionType, 0);
    }

    public List<string> SelectiveHearingUserIDs { get; private set; } = [];

    public string SelectiveHearingUserIDsAsString
    {
        get => _config?.GetValue(_selectiveHearingList) ?? "";
        set => _config?.Set(_selectiveHearingList, value);
    }

    public void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
    {
        builder.Key(_presetConfig);
        builder.Key(_presetStartupConfig);
        builder.Key(_selectiveHearingList);
        
        foreach (var presetType in PresetTypes.List) builder.Key(_presetStore[presetType]);
        
        _presetConfig.OnChanged += OnPresetSelected;
        
        foreach (var preventionType in PreventionTypes.List)
        {
            var key = new ModConfigurationKey<bool>($"Allow {preventionType} Restriction",
                "Should others be able to control this ability.", PreventionTypeConfigDefault(preventionType));
            key.OnChanged += PreventConfigOnChanged(preventionType);
            _displayedPreventionTypes.Add(preventionType, key);
            builder.Key(key);
        }

        foreach (var worldPermissionType in WorldPermissionTypes.List)
        {
            var key = new ModConfigurationKey<PresetChangeType>(
                $"Change to Preset, if world permissions are {worldPermissionType.AsExpandedString()}",
                "", () => worldPermissionType.Default()
            );
            _changeOnWorldPermissionChangeDict.Add(worldPermissionType, key);
            builder.Key(key);
        }

        _selectiveHearingList.OnChanged += UpdateHearingUserIDs;
    }

    private void UpdateHearingUserIDs(object? value)
    {
        var splitArray = (value as string)?.Split(',') ?? [];
        SelectiveHearingUserIDs = splitArray.Select(t => t.Trim())
            .Where(trimmed => trimmed.Length != 0)
            .ToList();
    }

    private Func<bool> PreventionTypeConfigDefault(PreventionType preventionType)
    {
        return () =>
        {
            var presetType = PresetType.None;
            var found = _config?.TryGetValue(_presetConfig, out presetType) ?? false;
            if (!found) return false;
            return presetType switch
            {
                PresetType.None => false,
                PresetType.All => true,
                _ => GetCustomStored(presetType)[(int)preventionType]
            };
        };
    }

    public void Init(ModConfiguration? config = null)
    {
        _config = config;
        var presetOnStartup = _config?.GetValue(_presetStartupConfig) ?? PresetChangeType.None;
        if (presetOnStartup != PresetChangeType.DoNotChange) _config?.Set(_presetConfig, (PresetType)presetOnStartup);

        var currentPreset = _config?.GetValue(_presetConfig) ?? PresetType.None;
        _currentPreventionTypes = GetCustomStored(currentPreset);
        _config?.Save(true);
    }

    private ModConfigurationKey.OnChangedHandler PreventConfigOnChanged(PreventionType preventionType)
    {
        return value =>
        {
            var boolValue = value as bool? ?? false;
            var presetType = _config?.GetValue(_presetConfig) ?? PresetType.Customized;
            switch (presetType)
            {
                case PresetType.None when !boolValue:
                    return;
                case PresetType.None:
                    SwitchToCustomized(preventionType, true);
                    return;
                case PresetType.All when boolValue:
                    return;
                case PresetType.All:
                    SwitchToCustomized(preventionType, false);
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
        _currentPreventionTypes ??= GetCustomStored(selectedPreset);
        Func<PreventionType, bool> getValueForPreventionType = value switch
        {
            PresetType.Customized => preventionType => _currentPreventionTypes[(int)preventionType],
            PresetType.StoredPresetAlpha => preventionType => _currentPreventionTypes[(int)preventionType],
            PresetType.All => _ => true,
            PresetType.None => _ => false,
            _ => _ => false
        };
        foreach (var preventionType in PreventionTypes.List)
        {
            if (GetDisplayedPreventionTypeConfig(preventionType, out var configurationKey)) continue;
            _config?.Set(configurationKey, getValueForPreventionType(preventionType));
        }
    }

    internal bool GetDisplayedPreventionTypeConfig(PreventionType preventionType,
        out ModConfigurationKey<bool> configurationKey)
    {
        var found = _displayedPreventionTypes.TryGetValue(preventionType, out configurationKey);
        return !found || configurationKey == null;
    }

    internal bool GetValue(PreventionType preventionType)
    {
        return IsPreventionTypeEnabled(preventionType) && _currentPreventValues[(int)preventionType];
    }

    internal int GetCounter(PreventionType preventionType)
    {
        var found = _currentPreventCounter.TryGetValue(preventionType, out var counter);
        return IsPreventionTypeEnabled(preventionType) && found ? counter : 0;
    }

    internal bool IsPreventionTypeEnabled(PreventionType preventionType)
    {
        if (GetDisplayedPreventionTypeConfig(preventionType, out var key)) return false;
        var configValue = false;
        var foundConfigValue = _config?.TryGetValue(key, out configValue) ?? false;
        return foundConfigValue && configValue;
    }

    internal bool UpdateValue(PreventionType preventionType, bool value)
    {
        if (value == _currentPreventValues[(int)preventionType]) return true;
        _currentPreventValues[(int)preventionType] = value;
        return false;
    }

    internal bool UpdateCounter(PreventionType preventionType, int value)
    {
        var found = _currentPreventCounter.TryGetValue(preventionType, out var counter);
        if (!found || value == counter) return true;
        _currentPreventCounter[preventionType] = value;
        return false;
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