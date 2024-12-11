using System;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using Restrainite.Enums;

namespace Restrainite;

internal class ImpulseSender
{
    private const string ImpulsePrefix = "Restrainite";
    private readonly Configuration _configuration;
    private readonly WeakReference<UserRoot> _userRoot;

    internal ImpulseSender(Configuration config, UserRoot userRoot)
    {
        _configuration = config;
        _userRoot = new WeakReference<UserRoot>(userRoot);
    }

    internal void SendDynamicImpulse<T>(PreventionType preventionType, T value)
    {
        if (!_configuration.SendDynamicImpulses) return;
        if (!GetLocalUserSlot(out var slot) || slot == null) return;
        slot.RunInUpdates(0, () =>
        {
            if (slot.IsDestroyed || slot.IsDestroying) return;
            if (!_configuration.AllowRestrictionsFromWorld(slot.World, preventionType)) return;
            ProtoFluxHelper.DynamicImpulseHandler.TriggerAsyncDynamicImpulseWithArgument(
                slot, $"{ImpulsePrefix} Change", true,
                $"{preventionType.ToExpandedString()}:{typeof(T)}:{value}"
            );
            ProtoFluxHelper.DynamicImpulseHandler.TriggerAsyncDynamicImpulseWithArgument(
                slot, $"{ImpulsePrefix} {preventionType.ToExpandedString()}", true,
                value
            );
        });
    }

    private bool GetLocalUserSlot(out Slot? slot)
    {
        slot = null;
        var userRootFound = _userRoot.TryGetTarget(out var userRoot);
        if (!userRootFound || userRoot == null || userRoot.IsDisposed || userRoot.IsDestroyed) return false;
        slot = userRoot.Slot;
        return slot is { IsDisposed: false, IsDestroyed: false };
    }
}