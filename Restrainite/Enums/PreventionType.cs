using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Restrainite.Enums;

internal enum PreventionType
{
    PreventEquippingAvatar,
    PreventOpeningContextMenu,
    PreventUsingTools,
    PreventOpeningDash,
    PreventGrabbing,
    PreventHearing,
    EnforceSelectiveHearing,
    PreventLaserTouch,
    PreventPhysicalTouch,
    PreventSpeaking,
    EnforceWhispering,
    PreventRespawning,
    PreventEmergencyRespawning,
    PreventSwitchingWorld
}

internal static class PreventionTypes
{
    internal static readonly IEnumerable<PreventionType> List =
        Enum.GetValues(typeof(PreventionType)).Cast<PreventionType>();

    internal static readonly int Max = (int)List.Max() + 1;

    private static readonly Dictionary<PreventionType, string> Dictionary =
        List.ToDictionary(l => l,
            l => Regex.Replace(l.ToString(), "([a-z])([A-Z])", "$1 $2"));

    internal static string ToExpandedString(this PreventionType type)
    {
        return Dictionary[type];
    }
}