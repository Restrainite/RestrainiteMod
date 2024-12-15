using System;
using System.Collections.Immutable;
using System.Reflection;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class ShowOrHideContextMenuItems
{
    private static bool _insideRootContextMenuCreation;

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


    [HarmonyPatch]
    private static class InteractionHandlerOpenContextMenuPatch
    {
        public static MethodBase TargetMethod()
        {
            var type = typeof(InteractionHandler);
            return AccessTools.FirstMethod(type,
                method => "OpenContextMenu".Equals(method.Name) && method.GetParameters().Length == 2);
        }

        public static void Prefix()
        {
            _insideRootContextMenuCreation = true;
        }

        public static void Postfix()
        {
            _insideRootContextMenuCreation = false;
        }
    }

    [HarmonyPatch(typeof(ContextMenu), "AddItem",
        [
            typeof(LocaleString), typeof(IAssetProvider<ITexture2D>), typeof(Uri), typeof(IAssetProvider<Sprite>),
            typeof(colorX?)
        ],
        [ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref])]
    private static class ContextMenuAddItemPatch
    {
        public static void Postfix(LocaleString label, ContextMenuItem __result)
        {
            if (!_insideRootContextMenuCreation) return;

            if (ShouldDisableButton(__result, label)) __result.Button.Slot.ActiveSelf = false;
        }
    }


    [HarmonyPatch(typeof(ContextMenu), "AddToggleItem")]
    private static class ContextMenuAddToggleItemPatch
    {
        public static bool Prefix(LocaleString trueLabel, LocaleString falseLabel, ContextMenu __instance)
        {
            return !_insideRootContextMenuCreation ||
                   !(ShouldDisableButton(__instance, trueLabel) || ShouldDisableButton(__instance, falseLabel));
        }
    }
}