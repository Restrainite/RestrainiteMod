using FrooxEngine;
using FrooxEngine.CommonAvatar;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal class PreventEquippingAvatar
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AvatarManager), nameof(AvatarManager.Equip))]
    private static bool PreventEquippingAvatar_AvatarManagerEquip_Prefix(ref bool __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventEquippingAvatar))
            return true;

        __result = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(WorldPermissionsExtensoins), nameof(WorldPermissionsExtensoins.CanSwapAvatar))]
    private static bool PreventEquippingAvatar_WorldPermissionsExtensoinsCanSwapAvatar_Prefix(ref bool __result)
    {
        if (!RestrainiteMod.IsRestricted(PreventionType.PreventEquippingAvatar))
            return true;

        __result = false;
        return false;
    }
}