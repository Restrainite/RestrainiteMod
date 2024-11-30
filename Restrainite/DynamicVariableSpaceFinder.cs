using System;
using System.Collections.Generic;
using System.Reflection;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using Restrainite.Enums;

namespace Restrainite;

internal static class DynamicVariableSpaceFinder
{
    [HarmonyPatch(typeof(DynamicVariableSpace), "UpdateName")]
    private static class DynamicVariableSpaceUpdateNamePatch
    {
        private static void Postfix(DynamicVariableSpace __instance)
        {
            DynamicVariableSpaceSync.UpdateListAndGetIfValid(__instance, out _);
        }
    }

    [HarmonyPatch(typeof(DynamicVariableSpace), "OnDispose")]
    private static class DynamicVariableSpaceOnDisposePatch
    {
        private static void Postfix(DynamicVariableSpace __instance)
        {
            DynamicVariableSpaceSync.Remove(__instance);
        }
    }

    [HarmonyPatch(typeof(DynamicVariableSpace))]
    private static class DynamicVariableSpaceAllocateManagerBooleanPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var boolType = typeof(DynamicVariableSpace)
                .GetMethod("AllocateManager", BindingFlags.NonPublic | BindingFlags.Instance)?
                .MakeGenericMethod(typeof(bool));
            if (boolType != null) yield return boolType;
        }

        private static void Postfix(string name, DynamicVariableSpace __instance,
            ref DynamicVariableSpace.ValueManager<bool> __result)
        {
            if (!DynamicVariableSpaceSync.UpdateListAndGetIfValid(__instance, out var dynamicVariableSpaceSync)
                || dynamicVariableSpaceSync == null) return;
            if (!PreventionTypes.NameToPreventionType.TryGetValue(name, out var preventionType)) return;

            ResoniteMod.Msg($"DynamicVariableSpaceAllocateManagerBoolean found {preventionType}");

            var booleanValueManagerWrapper = new BooleanValueManagerWrapper(name, preventionType, __instance);
            dynamicVariableSpaceSync.Register(booleanValueManagerWrapper);
            __result = booleanValueManagerWrapper;
        }
    }

    [HarmonyPatch(typeof(DynamicVariableSpace.ValueManager<bool>), "SetValue")]
    private static class DynamicVariableSpaceValueManagerPatch
    {
        private static void Prefix(bool value, DynamicVariableSpace.ValueManager<bool> __instance)
        {
            if (__instance is BooleanValueManagerWrapper wrapper) wrapper.SetValueOverride(value);
        }
    }

    internal class BooleanValueManagerWrapper : DynamicVariableSpace.ValueManager<bool>
    {
        private readonly PreventionType _preventionType;

        internal BooleanValueManagerWrapper(string name, PreventionType preventionType, DynamicVariableSpace space) :
            base(name, space)
        {
            _preventionType = preventionType;
        }

        internal event Action<PreventionType, bool>? OnChange;

        public void SetValueOverride(bool value)
        {
            OnChange?.Invoke(_preventionType, value);
        }
    }
}