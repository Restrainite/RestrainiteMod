using System;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal static class DisableNotifications
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(NotificationPanel), "AddNotification", [
		typeof(string), typeof(string), typeof(Uri), typeof(colorX), typeof(NotificationType), typeof(string),
		typeof(Uri), typeof(IAssetProvider<AudioClip>)
	])]
	private static bool DisableNotifications_NotificationPanelAddNotification_Prefix(NotificationPanel __instance)
	{
		return !RestrainiteMod.IsRestricted(PreventionType.DisableNotifications);
	}
	
}