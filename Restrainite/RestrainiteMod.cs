using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using Restrainite.Enums;

namespace Restrainite;

public class RestrainiteMod : ResoniteMod
{
    internal static readonly Configuration Cfg = new();
    private static readonly DynamicVariableSync DynVarSync = new(Cfg);

    public override string Name => "Restrainite";
    public override string Author => "SnepDrone Zenuru";
    public override string Version => "0.3.1";
    public override string Link => "https://github.com/SnepDrone/Restrainite";

    public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
    {
        Cfg.DefineConfiguration(builder);
    }

    public override void OnEngineInit()
    {
        Cfg.Init(GetConfiguration());

        var harmony = new Harmony("drone.Restrainite");
        harmony.PatchAll();
    }

    internal static bool IsRestricted(PreventionType value)
    {
        return Cfg.IsRestricted(value);
    }

    [HarmonyPatch(typeof(World), nameof(World.LocalUser), MethodType.Setter)]
    private class LocalUserSetterPatch
    {
        private static void Postfix(User value)
        {
            Msg($"Restrainite inject into LocalUser {value}.");
            DynVarSync.InjectIntoUser(value);
        }
    }
}