using System;
using System.Collections.Generic;
using System.Reflection;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;

namespace Restrainite;

internal static class UserRootInjector
{
    private static readonly Dictionary<RefID, ImpulseSender> ImpulseSenders = new();
    private static readonly Dictionary<RefID, RestrictionStateOutput> DynamicVariableStatusMap = new();
    private static bool _hasInjectedIntoWorldManager;
    private static readonly FieldInfo UserRootField = AccessTools.Field(typeof(User), "userRoot");

    private static void WorldFocused(World world)
    {
        WorldPermissionChanged(world);
        Action<IChangeable> worldPermissionChange = _ => WorldPermissionChanged(world);
        world.Configuration.AccessLevel.Changed += worldPermissionChange;
        world.Configuration.HideFromListing.Changed += worldPermissionChange;
        world.WorldDestroyed += _ =>
        {
            world.Configuration.AccessLevel.Changed -= worldPermissionChange;
            world.Configuration.HideFromListing.Changed -= worldPermissionChange;
        };
    }

    private static void WorldPermissionChanged(World world)
    {
        RestrainiteMod.Configuration.OnWorldPermissionChanged(world);
    }

    private static void InjectIntoUser(User value)
    {
        var userRoot = (LinkRef<UserRoot>)UserRootField.GetValue(value);
        userRoot.OnTargetChange += OnUserRootTargetChanged;
    }

    private static void OnUserRootTargetChanged(SyncRef<UserRoot> reference)
    {
        if (reference.Target == null) return;

        ResoniteMod.Msg($"Restrainite root changed for {reference} {reference.Target.Slot}");

        var userRoot = reference.Target;
        var refId = userRoot.ReferenceID;

        if (!ImpulseSenders.ContainsKey(refId))
        {
            var impulseSender = new ImpulseSender(RestrainiteMod.Configuration, userRoot);
            RestrainiteMod.OnRestrictionChanged += impulseSender.SendDynamicImpulse;
            ImpulseSenders.Add(refId, impulseSender);
        }

        var userSlot = userRoot.Slot;
        if (!DynamicVariableStatusMap.ContainsKey(refId) && userSlot != null)
        {
            var restrictionStateOutput = new RestrictionStateOutput(RestrainiteMod.Configuration, userSlot);
            RestrainiteMod.Configuration.ShouldRecheckPermissions += restrictionStateOutput.OnShouldRecheckPermissions;
            DynamicVariableStatusMap.Add(refId, restrictionStateOutput);
        }

        userRoot.Disposing += _ =>
        {
            if (ImpulseSenders.TryGetValue(refId, out var impulseSender))
            {
                RestrainiteMod.OnRestrictionChanged -= impulseSender.SendDynamicImpulse;
                ImpulseSenders.Remove(refId);
            }

            if (DynamicVariableStatusMap.TryGetValue(refId, out var restrictionStateOutput))
            {
                RestrainiteMod.Configuration.ShouldRecheckPermissions -=
                    restrictionStateOutput.OnShouldRecheckPermissions;
                DynamicVariableStatusMap.Remove(refId);
            }
        };
    }


    [HarmonyPatch(typeof(World), nameof(World.LocalUser), MethodType.Setter)]
    private class LocalUserSetterPatch
    {
        private static void Postfix(User value)
        {
            if (!_hasInjectedIntoWorldManager)
            {
                _hasInjectedIntoWorldManager = true;
                value.World.WorldManager.WorldFocused += WorldFocused;
            }

            ResoniteMod.Msg($"Restrainite inject into LocalUser {value}.");
            InjectIntoUser(value);
        }
    }
}