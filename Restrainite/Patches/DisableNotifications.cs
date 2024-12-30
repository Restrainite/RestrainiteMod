using System;
using System.Collections.Generic;
using System.Reflection;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class DisableNotifications
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		return AccessTools.GetDeclaredMethods(typeof(NotificationPanel)).FindAll(info => "AddNotification".Equals(info.Name));
	}
	
	[HarmonyPrefix]
	private static bool DisableNotifications_NotificationPanelAddNotification_Prefix()
	{
		return !RestrainiteMod.IsRestricted(PreventionType.DisableNotifications);
	}
	
}