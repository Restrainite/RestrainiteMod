using System;
using System.Collections.Immutable;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal class ShowOrHideContextMenuItems
{
    private static bool ShouldDisableButton(IWorldElement contextMenuItem, LocaleString label)
    {
        if (RestrainiteMod.IsRestricted(PreventionType.ShowContextMenuItems))
        {
            var items = RestrainiteMod.GetStrings(PreventionType.ShowContextMenuItems);

            var hidden = !FindInList(contextMenuItem, items, label);

            if (hidden) return true;
        }

        if (RestrainiteMod.IsRestricted(PreventionType.HideContextMenuItems))
        {
            var items = RestrainiteMod.GetStrings(PreventionType.HideContextMenuItems);
            var hidden = FindInList(contextMenuItem, items, label);

            if (hidden) return true;
        }

        if (RestrainiteMod.IsRestricted(PreventionType.PreventLaserTouch) &&
            "Interaction.LaserEnabled".Equals(label.content))
            return true;

        return false;
    }

    private static bool FindInList(IWorldElement element, IImmutableSet<string> items, LocaleString label)
    {
        foreach (var item in items)
        {
            if (item.Equals(label.content)) return true;

            // Special case for locomotion item
            if (label.isLocaleKey) continue;
            var localized = element.GetLocalized(item);
            if (localized == null) continue;
            if (label.content.StartsWith(localized)) return true;
        }

        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ContextMenu), "AddItem",
        [
            typeof(LocaleString), typeof(IAssetProvider<ITexture2D>), typeof(Uri), typeof(IAssetProvider<Sprite>),
            typeof(colorX?)
        ],
        [ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref])]
    public static void ShowOrHideContextMenuItems_ContextMenuAddItem_Postfix(LocaleString label, ContextMenuItem __result)
    {
        if (ShouldDisableButton(__result, label)) __result.Button.Slot.ActiveSelf = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ContextMenu), "AddToggleItem")]
    public static bool ShowOrHideContextMenuItems_ContextMenuAddToggleItem_Prefix(LocaleString trueLabel, LocaleString falseLabel, ContextMenu __instance)
    {
        return !(ShouldDisableButton(__instance, trueLabel) || ShouldDisableButton(__instance, falseLabel));
    }
}