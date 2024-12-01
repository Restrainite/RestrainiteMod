using FrooxEngine;
using FrooxEngine.ProtoFlux;
using Restrainite.Enums;

namespace Restrainite;

public class ImpulseSender
{
    private const string ImpulsePrefix = "Restrainite";
    private readonly Configuration _configuration;

    internal ImpulseSender(Configuration config)
    {
        _configuration = config;
        DynamicVariableSpaceSync.OnGlobalStateChanged += SendDynamicImpulse;
    }

    private void SendDynamicImpulse<T>(Slot restrainiteSlot, PreventionType preventionType, T value)
    {
        if (restrainiteSlot.IsDestroyed || restrainiteSlot.IsDestroying) return;
        var slot = restrainiteSlot.Parent;
        if (slot == null) return;
        slot.RunInUpdates(0, () =>
        {
            if (slot.IsDestroyed || slot.IsDestroying) return;
            if (slot.Engine.WorldManager.FocusedWorld != slot.World) return;
            if (!_configuration.IsPreventionTypeEnabled(preventionType)) return;
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
}