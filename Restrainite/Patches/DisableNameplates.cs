using FrooxEngine;
using ResoniteModLoader;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal static class DisableNameplates
{
    private static NameplateVisibility _originalVisibility;

    internal static void Initialize()
    {
        RestrainiteMod.OnRestrictionChanged += OnRestrictionChanged;

        Settings.RegisterValueChanges<NamePlateSettings>(OnNamePlateSettingsChanged);

        var nameplateSettings = Settings.GetActiveSetting<NamePlateSettings>();
        if (nameplateSettings != null) _originalVisibility = nameplateSettings.NameplateVisibility.Value;
    }

    private static void OnRestrictionChanged(PreventionType preventionType, bool value)
    {
        if (preventionType != PreventionType.DisableNameplates) return;

        var nameplateSettings = Settings.GetActiveSetting<NamePlateSettings>();
        if (nameplateSettings == null)
        {
            ResoniteMod.Warn("Couldn't acquire NameplateSettings reference");
            return;
        }

        if (value)
        {
            _originalVisibility = nameplateSettings.NameplateVisibility.Value;
            Settings.UpdateActiveSetting<NamePlateSettings>(
                namePlateSettings => namePlateSettings.NameplateVisibility.Value = NameplateVisibility.None
            );
        }
        else
        {
            Settings.UpdateActiveSetting<NamePlateSettings>(
                namePlateSettings => namePlateSettings.NameplateVisibility.Value = _originalVisibility
            );
        }
    }

    private static void OnNamePlateSettingsChanged(NamePlateSettings nameplateSettings)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.DisableNameplates) ||
            nameplateSettings.NameplateVisibility.Value == NameplateVisibility.None) return;

        Settings.UpdateActiveSetting<NamePlateSettings>(
            settings => settings.NameplateVisibility.Value = NameplateVisibility.None
        );
    }
}