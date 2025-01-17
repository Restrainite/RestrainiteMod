using System.Collections.Generic;
using System.Reflection;
using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class AllowOrDenyGrabbing
{
    private static bool IsItemGrabbable(Slot slot, bool checkObjectRoot = true)
    {
        var tag = string.IsNullOrEmpty(slot.Tag) ? "null" : slot.Tag;
        var result = true;

        if (RestrainiteMod.IsRestricted(PreventionType.AllowGrabbingBySlotTags))
        {
            var allowed = RestrainiteMod.GetStrings(PreventionType.AllowGrabbingBySlotTags);
            result &= allowed.Contains(tag);
        }

        if (RestrainiteMod.IsRestricted(PreventionType.DenyGrabbingBySlotTags))
        {
            var denied = RestrainiteMod.GetStrings(PreventionType.DenyGrabbingBySlotTags);
            result &= !denied.Contains(tag);
        }

        if (checkObjectRoot && slot.GetObjectRoot() is Slot root)
            result &= IsItemGrabbable(root, false);

        return result;
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Grabbable), nameof(Grabbable.CanGrab));
        yield return AccessTools.Method(typeof(Draggable), nameof(Draggable.CanGrab));
        yield return AccessTools.Method(typeof(GrabInstancer), nameof(GrabInstancer.CanGrab));
    }

    private static void Postfix(IGrabbable __instance, ref bool __result)
    {
        if (__instance.World != Userspace.UserspaceWorld)
            __result &= IsItemGrabbable(__instance.Slot);
        else if (RestrainiteMod.IsRestricted(PreventionType.PreventNonDashUserspaceInteraction))
            __result &= __instance.Slot.GetComponentInParents<UserspaceRadiantDash>() != null;
    }
}