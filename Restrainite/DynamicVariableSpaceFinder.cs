using System;
using System.Collections.Generic;
using System.Reflection;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using Restrainite.Enums;

namespace Restrainite;

/*
 * These methods can only be used with basic types, like bool or float.
 *
 * Also, if anyone of the Resonite team reads this, can we please have events, when a value in a dynamic variable space
 * changes. Then we would not need this horrible code.
 */
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
            if (!name.TryParsePreventionType(out var preventionType)) return;

            ResoniteMod.Msg($"DynamicVariableSpaceAllocateManagerBoolean found {preventionType}");

            var booleanValueManagerWrapper = new ValueManagerWrapper<bool>(name, preventionType, __instance);
            booleanValueManagerWrapper.OnChange += dynamicVariableSpaceSync.UpdateLocalState;
            __result = booleanValueManagerWrapper;
        }
    }

    [HarmonyPatch(typeof(DynamicVariableSpace))]
    private static class DynamicVariableSpaceAllocateManagerFloatPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var floatType = typeof(DynamicVariableSpace)
                .GetMethod("AllocateManager", BindingFlags.NonPublic | BindingFlags.Instance)?
                .MakeGenericMethod(typeof(float));
            if (floatType != null) yield return floatType;
        }

        private static void Postfix(string name, DynamicVariableSpace __instance,
            ref DynamicVariableSpace.ValueManager<float> __result)
        {
            if (!DynamicVariableSpaceSync.UpdateListAndGetIfValid(__instance, out var dynamicVariableSpaceSync)
                || dynamicVariableSpaceSync == null) return;
            if (!name.TryParsePreventionType(out var preventionType)) return;

            ResoniteMod.Msg($"DynamicVariableSpaceAllocateManagerFloat found {preventionType}");

            var booleanValueManagerWrapper = new ValueManagerWrapper<float>(name, preventionType, __instance);
            booleanValueManagerWrapper.OnChange += dynamicVariableSpaceSync.UpdateLocalFloatState;
            __result = booleanValueManagerWrapper;
        }
    }

    [HarmonyPatch(typeof(DynamicVariableSpace.ValueManager<bool>), "SetValue")]
    private static class DynamicVariableSpaceValueManagerBoolSetValuePatch
    {
        private static void Postfix(bool value, DynamicVariableSpace.ValueManager<bool> __instance)
        {
            if (__instance is ValueManagerWrapper<bool> wrapper) wrapper.SetValueOverride(value);
        }
    }

    [HarmonyPatch(typeof(DynamicVariableSpace.ValueManager<bool>), "Unregister")]
    private static class DynamicVariableSpaceValueManagerBoolUnregisterPatch
    {
        private static void Postfix(DynamicVariableSpace.ValueManager<bool> __instance)
        {
            if (__instance is ValueManagerWrapper<bool> wrapper && __instance.ReadableValueCount == 0)
                wrapper.SetValueOverride(false);
        }
    }

    [HarmonyPatch(typeof(DynamicVariableSpace.ValueManager<float>), "SetValue")]
    private static class DynamicVariableSpaceValueManagerFloatSetValuePatch
    {
        private static void Postfix(float value, DynamicVariableSpace.ValueManager<float> __instance)
        {
            if (__instance is ValueManagerWrapper<float> wrapper) wrapper.SetValueOverride(value);
        }
    }

    [HarmonyPatch(typeof(DynamicVariableSpace.ValueManager<float>), "Unregister")]
    private static class DynamicVariableSpaceValueManagerFloatUnregisterPatch
    {
        private static void Postfix(DynamicVariableSpace.ValueManager<float> __instance)
        {
            if (__instance is ValueManagerWrapper<float> wrapper && __instance.ReadableValueCount == 0)
                wrapper.SetValueOverride(float.NaN);
        }
    }

    private class ValueManagerWrapper<T> : DynamicVariableSpace.ValueManager<T>
    {
        private readonly PreventionType _preventionType;

        internal ValueManagerWrapper(string name, PreventionType preventionType, DynamicVariableSpace space) :
            base(name, space)
        {
            _preventionType = preventionType;
        }

        internal event Action<PreventionType, T>? OnChange;

        public void SetValueOverride(T value)
        {
            OnChange?.Invoke(_preventionType, value);
        }
    }
}