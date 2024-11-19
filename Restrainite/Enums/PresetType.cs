using System;
using System.Collections.Generic;
using System.Linq;

namespace Restrainite.Enums;

internal enum PresetType
{
    None,
    All,
    Customized,
    StoredPresetAlpha,
    StoredPresetBeta,
    StoredPresetGamma,
    StoredPresetDelta,
    StoredPresetOmega
}

internal static class PresetTypes
{
    internal static readonly IEnumerable<PresetType> List =
        Enum.GetValues(typeof(PresetType)).Cast<PresetType>();
}