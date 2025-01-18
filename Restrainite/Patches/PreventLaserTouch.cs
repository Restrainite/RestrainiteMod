using System.Reflection;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal static class PreventLaserTouch
{
    private static bool _leftOriginalValue = true;
    private static bool _rightOriginalValue = true;

    private static readonly FieldInfo
        LaserEnabledField = AccessTools.Field(typeof(InteractionHandler), "_laserEnabled");

    internal static void Initialize()
    {
        RestrainiteMod.OnRestrictionChanged += OnRestrictionChanged;
    }

    private static void OnRestrictionChanged(PreventionType preventionType, bool value)
    {
        if (preventionType != PreventionType.PreventLaserTouch) return;

        var user = Engine.Current?.WorldManager?.FocusedWorld?.LocalUser;
        if (user is null) return;

        var leftInteractionHandler = user.GetInteractionHandler(Chirality.Left);
        leftInteractionHandler.RunInUpdates(0, () =>
            SetLaserActive(value, leftInteractionHandler, ref _leftOriginalValue));

        var rightInteractionHandler = user.GetInteractionHandler(Chirality.Right);
        rightInteractionHandler.RunInUpdates(0, () =>
            SetLaserActive(value, rightInteractionHandler, ref _rightOriginalValue));
    }

    private static void SetLaserActive(bool value, InteractionHandler? interactionHandler, ref bool originalValue)
    {
        if (interactionHandler == null) return;
        if (LaserEnabledField.GetValue(interactionHandler) is not Sync<bool> syncBool) return;
        if (value)
        {
            originalValue = syncBool.Value;
            syncBool.Value = false;
        }
        else
        {
            syncBool.Value = originalValue;
        }
    }
}