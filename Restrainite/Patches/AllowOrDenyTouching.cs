using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal class AllowOrDenyTouching
{
	static bool IsItemTouchable(Slot slot, bool checkObjectRoot = true)
	{
		string tag = string.IsNullOrEmpty(slot.Tag) ? "null" : slot.Tag;
		bool result = true;
		
		if (RestrainiteMod.IsRestricted(PreventionType.AllowTouchingTags))
		{
			var allowed = RestrainiteMod.GetStrings(PreventionType.AllowTouchingTags);
			result &= allowed.Contains(tag);
		}

		if (RestrainiteMod.IsRestricted(PreventionType.DenyTouchingTags))
		{
			var denied = RestrainiteMod.GetStrings(PreventionType.DenyTouchingTags);
			result &= !denied.Contains(tag);
		}
		
		if (checkObjectRoot && slot.GetObjectRoot() is Slot root)
			result &= IsItemTouchable(root, false);

		return result;
	}
	
	[HarmonyPostfix]
	[HarmonyPatch(typeof(TouchablePermissionsExtensions), nameof(TouchablePermissionsExtensions.CanTouch))]
	static void AllowOrDenyTouching_TouchablePermissionsExtensionsCanTouch_Postfix(ITouchable touchable, ref bool __result)
	{
		if (touchable.World != Userspace.UserspaceWorld)
			__result &= IsItemTouchable(touchable.Slot);
		else if (RestrainiteMod.IsRestricted(PreventionType.PreventNonDashUserspaceInteraction))
			__result &= touchable.Slot.GetComponentInParents<UserspaceRadiantDash>() != null || touchable.Slot.GetComponentInParents<ContextMenu>() != null;
	}
}