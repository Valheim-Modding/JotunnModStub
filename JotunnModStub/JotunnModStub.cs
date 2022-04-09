// JotunnModStub
// a Valheim mod skeleton using Jötunn
// 
// File:    JotunnModStub.cs
// Project: JotunnModStub

using BepInEx;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;

namespace JotunnModStub
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [HarmonyPatch]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class JotunnModStub : BaseUnityPlugin
    {
        public const string PluginGUID = "com.jotunn.jotunnmodstub";
        public const string PluginName = "JotunnModStub";
        public const string PluginVersion = "0.0.1";

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private Harmony harmony;

        private void Awake()
        {
            // HarmonyX is used to patch into game code. PatchAll() will effect all classes that have the [HarmonyPatch] attribute
            // See https://github.com/BepInEx/HarmonyX for more details
            harmony = new Harmony(PluginGUID);
            harmony.PatchAll();

            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("ModStub has landed");

            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html
        }

        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
        [HarmonyPostfix]
        private static void FejdStartup_Awake_Postfix(FejdStartup __instance)
        {
            // This code runs after Valheim's FejdStartup.Awake
            Jotunn.Logger.LogInfo("FejdStartup has awoken");
        }
    }
}