using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class AllowOrDenyTouching
{
    private static bool IsItemTouchable(Slot slot, bool checkObjectRoot = true)
    {
        var tag = string.IsNullOrEmpty(slot.Tag) ? "null" : slot.Tag;
        var result = true;

        if (RestrainiteMod.IsRestricted(PreventionType.AllowTouchingBySlotTags))
        {
            var allowed = RestrainiteMod.GetStrings(PreventionType.AllowTouchingBySlotTags);
            result &= allowed.Contains(tag);
        }

        if (RestrainiteMod.IsRestricted(PreventionType.DenyTouchingBySlotTags))
        {
            var denied = RestrainiteMod.GetStrings(PreventionType.DenyTouchingBySlotTags);
            result &= !denied.Contains(tag);
        }

        if (checkObjectRoot && slot.GetObjectRoot() is Slot root)
            result &= IsItemTouchable(root, false);

        return result;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TouchablePermissionsExtensions), nameof(TouchablePermissionsExtensions.CanTouch))]
    private static void TouchablePermissionsExtensions_CanTouch_Postfix(ITouchable touchable, ref bool __result)
    {
        if (touchable.World != Userspace.UserspaceWorld)
            __result &= IsItemTouchable(touchable.Slot);
        else if (RestrainiteMod.IsRestricted(PreventionType.PreventNonDashUserspaceInteraction))
            __result &= touchable.Slot.GetComponentInParents<UserspaceRadiantDash>() != null ||
                        touchable.Slot.GetComponentInParents<ContextMenu>() != null;
    }
}