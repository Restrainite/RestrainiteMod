using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class PreventHearing
{
    [HarmonyPatch(typeof(User), "LocalVolume", MethodType.Getter)]
    private class UserLocalVolumePatch
    {
        private static float Postfix(float result, User __instance)
        {
            var userId = __instance.UserID;
            if (userId is null) return Restrainite.IsRestricted(PreventionType.PreventHearing) ? 0.0f : result;
            if (Restrainite.IsRestricted(PreventionType.EnforceSelectiveHearing) &&
                !Restrainite.Cfg.GetStringList(PreventionType.EnforceSelectiveHearing).Contains(userId)) return 0.0f;
            return Restrainite.IsRestricted(PreventionType.PreventHearing) ? 0.0f : result;
        }
    }
}