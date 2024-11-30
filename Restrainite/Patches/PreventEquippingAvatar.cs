using FrooxEngine;
using FrooxEngine.CommonAvatar;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class PreventEquippingAvatar
{
    [HarmonyPatch(typeof(AvatarManager),
        nameof(AvatarManager.Equip))]
    private class AvatarManagerEquipPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!RestrainiteMod.IsRestricted(PreventionType.PreventEquippingAvatar))
                return true;

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(WorldPermissionsExtensoins),
        nameof(WorldPermissionsExtensoins.CanSwapAvatar))]
    private class WorldPermissionsExtensoinsEquipPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!RestrainiteMod.IsRestricted(PreventionType.PreventEquippingAvatar))
                return true;

            __result = false;
            return false;
        }
    }
}