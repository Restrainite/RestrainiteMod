using FrooxEngine;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class DisableNameplates
{
	private static NamePlateSettings? _nameplateSettings = Settings.GetActiveSetting<NamePlateSettings>();
	private static NameplateVisibility _originalVisibility = _nameplateSettings?.NameplateVisibility.Value ?? NameplateVisibility.All;

	internal static void Initialize()
	{
		RestrainiteMod.OnRestrictionChanged += OnRestrictionChanged;
	}

	private static void OnRestrictionChanged(PreventionType preventionType, bool value)
	{
		if (preventionType != PreventionType.DisableNameplates) return;
		if (_nameplateSettings == null)
		{
			RestrainiteMod.Warn("Couldn't acquire NameplateSettings reference");
			return;
		}

		if (value)
		{
			_originalVisibility = _nameplateSettings.NameplateVisibility.Value;
			_nameplateSettings.NameplateVisibility.Value = NameplateVisibility.None;
			Settings.RegisterValueChanges<NamePlateSettings>(OnNamePlateSettingsChanged);
		}
		else
		{
			Settings.UnregisterValueChanges<NamePlateSettings>(OnNamePlateSettingsChanged);
			_nameplateSettings.NameplateVisibility.Value = _originalVisibility;
		}
	}

	private static void OnNamePlateSettingsChanged(NamePlateSettings nameplateSettings)
	{
		_nameplateSettings = nameplateSettings;
		if (RestrainiteMod.IsRestricted(PreventionType.DisableNameplates) &&
		    nameplateSettings.NameplateVisibility.Value != NameplateVisibility.None)
		{
			nameplateSettings.NameplateVisibility.Value = NameplateVisibility.None;
		}
	}
}