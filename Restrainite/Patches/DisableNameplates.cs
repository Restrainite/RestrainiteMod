using FrooxEngine;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class DisableNameplates
{
	private static NamePlateSettings? _nameplateSettings = Settings.GetActiveSetting<NamePlateSettings>();
	private static NameplateVisibility _originalVisibility = _nameplateSettings?.NameplateVisibility.Value ?? NameplateVisibility.All;
	private static bool _listenerRegistered = false;

	internal static void Initialize()
	{
		RestrainiteMod.OnRestrictionChanged += OnRestrictionChanged;
		RegisterSettingsListener(true);
	}

	private static void OnRestrictionChanged(PreventionType preventionType, bool value)
	{
		if (preventionType != PreventionType.DisableNameplates) return;
		_nameplateSettings ??= Settings.GetActiveSetting<NamePlateSettings>();
		if (_nameplateSettings == null)
		{
			RestrainiteMod.Warn("Couldn't acquire NameplateSettings reference");
			return;
		}

		if (value)
		{
			_originalVisibility = _nameplateSettings.NameplateVisibility.Value;
			TrySetNameplateVisibility(NameplateVisibility.None);
			RegisterSettingsListener(true);
		}
		else
		{
			// RegisterSettingsListener(false);
			TrySetNameplateVisibility(_originalVisibility);
		}
	}

	private static void OnNamePlateSettingsChanged(NamePlateSettings nameplateSettings)
	{
		_nameplateSettings = nameplateSettings;
		if (RestrainiteMod.IsRestricted(PreventionType.DisableNameplates) &&
		    nameplateSettings.NameplateVisibility.Value != NameplateVisibility.None)
		{
		    // Called in userspace world thread
			nameplateSettings.NameplateVisibility.Value = NameplateVisibility.None;
		}
	}

	private static void RegisterSettingsListener(bool status)
	{
		if (status == _listenerRegistered) return;
		if (status)
			Settings.RegisterValueChanges<NamePlateSettings>(OnNamePlateSettingsChanged);
		else
			Settings.UnregisterValueChanges<NamePlateSettings>(OnNamePlateSettingsChanged);
		_listenerRegistered = status;
	}

	private static bool TrySetNameplateVisibility(NameplateVisibility visibility)
	{
		if (_nameplateSettings == null) return false;
		Userspace.UserspaceWorld.RunSynchronously(delegate
		{
			_nameplateSettings.NameplateVisibility.Value = visibility;
		});
		return true;
	}
}