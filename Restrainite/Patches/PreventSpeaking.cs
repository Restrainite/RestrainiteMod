using FrooxEngine;
using HarmonyLib;
using Restrainite.Enums;

namespace Restrainite.Patches;

internal class PreventSpeaking
{
    [HarmonyPatch(typeof(AudioSystem), "IsMuted", MethodType.Getter)]
    private class AudioSystemIsMutedPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (RestrainiteMod.IsRestricted(PreventionType.PreventSpeaking)) __result = true;
        }
    }
}