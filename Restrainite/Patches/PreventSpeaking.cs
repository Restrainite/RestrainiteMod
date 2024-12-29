using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

[HarmonyPatch]
internal class PreventSpeaking
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(AudioSystem), "IsMuted", MethodType.Getter)]
    private static void PreventSpeaking_AudioSystemIsMuted_Getter_Postfix(ref bool __result)
    {
        if (RestrainiteMod.IsRestricted(PreventionType.PreventSpeaking)) __result = true;
    }
}