using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal class PreventHearing
{
    /*
     * PreventHearing OFF EnforceSelectiveHearing OFF: No mute override
     * PreventHearing OFF EnforceSelectiveHearing ON: Anyone not in ESH list is muted
     * PreventHearing ON EnforceSelectiveHearing OFF: Everyone muted
     * PreventHearing ON EnforceSelectiveHearing ON: Everyone muted
     */
    [HarmonyPostfix]
    [HarmonyPatch(typeof(User), "LocalVolume", MethodType.Getter)]
    private static float PreventHearing_UserLocalVolume_Getter_Postfix(float result, User __instance)
    {
        var userId = __instance.UserID;
        if (userId is null) return RestrainiteMod.IsRestricted(PreventionType.PreventHearing) ? 0.0f : result;
        if (RestrainiteMod.IsRestricted(PreventionType.EnforceSelectiveHearing) &&
            !RestrainiteMod.GetStrings(PreventionType.EnforceSelectiveHearing).Contains(userId)) return 0.0f;
        return RestrainiteMod.IsRestricted(PreventionType.PreventHearing) ? 0.0f : result;
    }
}