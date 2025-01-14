using System.Collections.Generic;
using System.Reflection;
using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal class AllowOrDenyGrabbing
{
	static bool IsItemGrabbable(Slot slot, bool checkObjectRoot = true)
	{
		string tag = string.IsNullOrEmpty(slot.Tag) ? "null" : slot.Tag;
		bool result = true;
		
		if (RestrainiteMod.IsRestricted(PreventionType.AllowGrabbingTags))
		{
			var allowed = RestrainiteMod.GetStrings(PreventionType.AllowGrabbingTags);
			result &= allowed.Contains(tag);
		}

		if (RestrainiteMod.IsRestricted(PreventionType.DenyGrabbingTags))
		{
			var denied = RestrainiteMod.GetStrings(PreventionType.DenyGrabbingTags);
			result &= !denied.Contains(tag);
		}
		
		if (checkObjectRoot && slot.GetObjectRoot() is Slot root)
			result &= IsItemGrabbable(root, false);

		return result;
	}
	
	static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.Method(typeof(Grabbable), nameof(Grabbable.CanGrab));
		yield return AccessTools.Method(typeof(Draggable), nameof(Draggable.CanGrab));
		yield return AccessTools.Method(typeof(GrabInstancer), nameof(GrabInstancer.CanGrab));
	}

	static void Postfix(IGrabbable __instance, Grabber grabber, ref bool __result)
	{
		if (__instance.World != Userspace.UserspaceWorld)
			__result &= IsItemGrabbable(__instance.Slot);
		else if (RestrainiteMod.IsRestricted(PreventionType.PreventNonDashUserspaceInteraction))
			__result &= __instance.Slot.GetComponentInParents<UserspaceRadiantDash>() != null;
	}
}