using System;
using System.Reflection;
using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class ShowOrHideContextMenuItems
{
    private static bool _insideRootContextMenuCreation;

    private static bool ShouldDisableButton(LocaleString label)
    {
        if (Restrainite.IsRestricted(PreventionType.ShowContextMenuItems))
        {
            var hidden = !Restrainite.Cfg.GetStringList(PreventionType.ShowContextMenuItems)
                .Contains(label.content);
            ResoniteMod.Msg(
                $"Checking if the context menu item {label.content} is hidden by ShowContextMenuItems: {hidden}.");
            if (hidden) return true;
        }

        if (Restrainite.IsRestricted(PreventionType.HideContextMenuItems))
        {
            var hidden = Restrainite.Cfg.GetStringList(PreventionType.HideContextMenuItems).Contains(label.content);
            ResoniteMod.Msg(
                $"Checking if the context menu item {label.content} is hidden by HideContextMenuItems: {hidden}.");
            if (hidden) return true;
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
                method => method.Name == "OpenContextMenu" && method.GetParameters().Length == 2);
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

            if (ShouldDisableButton(label.content)) __result.Button.Slot.ActiveSelf = false;
        }
    }
}