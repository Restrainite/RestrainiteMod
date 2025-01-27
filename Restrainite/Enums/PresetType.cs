using System;
using System.Collections.Generic;
using System.Linq;

namespace Restrainite.Enums;

internal enum PresetType
{
    None,
    Customized,
    StoredPresetAlpha,
    StoredPresetBeta,
    StoredPresetGamma,
    StoredPresetDelta,
    StoredPresetOmega,
    All
}

internal static class PresetTypes
{
    internal static readonly IEnumerable<PresetType> List =
        Enum.GetValues(typeof(PresetType)).Cast<PresetType>();
}