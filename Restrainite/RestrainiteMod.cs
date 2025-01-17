﻿using System;
using System.Collections.Immutable;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using Restrainite.Enums;
using Restrainite.Patches;

namespace Restrainite;

public class RestrainiteMod : ResoniteMod
{
    internal static readonly Configuration Configuration = new();

    public override string Name => "Restrainite";
    public override string Author => "SnepDrone Zenuru";

    public static Version AssemblyVersion => typeof(RestrainiteMod).Assembly.GetName().Version;
    public override string Version => $"{AssemblyVersion.Major}.{AssemblyVersion.Minor}.{AssemblyVersion.Build}";

    public override string Link => "https://github.com/SnepDrone/Restrainite";

    /**
     * OnRestrictionChanged will fire, when the restriction is activated or deactivated. It will take into account, if
     * the restriction is disabled by the user. It will run in the update cycle of the world that triggered the
     * change. The value is debounced, meaning it will only trigger, if it actually changes.
     */
    internal static event Action<PreventionType, bool>? OnRestrictionChanged;

    public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
    {
        Configuration.DefineConfiguration(builder);
    }

    public override void OnEngineInit()
    {
        Configuration.Init(GetConfiguration());

        var harmony = new Harmony("drone.Restrainite");
        harmony.PatchAll();

        InitializePatches();
    }

    private static void InitializePatches()
    {
        EnforceWhispering.Initialize();
        PreventGrabbing.Initialize();
        PreventOpeningContextMenu.Initialize();
        PreventOpeningDash.Initialize();
        PreventLaserTouch.Initialize();
        PreventUserScaling.Initialize();
        ShowOrHideUserAvatars.Initialize();
        DisableNameplates.Initialize();
    }

    internal static bool IsRestricted(PreventionType preventionType)
    {
        return DynamicVariableSpaceSync.GetGlobalState(preventionType);
    }

    internal static IImmutableSet<string> GetStrings(PreventionType preventionType)
    {
        return DynamicVariableSpaceSync.GetGlobalStrings(preventionType);
    }

    /**
     * Only to be called by DynamicVariableSpaceSync.
     */
    internal static void NotifyRestrictionChanged(World source, PreventionType preventionType, bool value)
    {
        source.RunInUpdates(0, () =>
        {
            Msg($"State of {preventionType.ToExpandedString()} changed to {value}");
            OnRestrictionChanged?.Invoke(preventionType, value);
        });
    }
}