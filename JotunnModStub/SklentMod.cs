// SklentMod
// a Valheim mod skeleton using Jötunn
// 
// File:    SklentMod.cs
// Project: SklentMod

using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Configs;

namespace SklentMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class SklentMod : BaseUnityPlugin
    {
        public const string PluginGUID = "com.sklent";
        public const string PluginName = "SklentMod";
        public const string PluginVersion = "0.0.1";
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private void Awake()
        {
            AddRecipes();

            // Jotunn comes with MonoMod Detours enabled for hooking Valheim's code
            // https://github.com/MonoMod/MonoMod
            On.FejdStartup.Awake += FejdStartup_Awake;
            
            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("SklentMod has landed");

            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html
        }

        private void FejdStartup_Awake(On.FejdStartup.orig_Awake orig, FejdStartup self)
        {
            // This code runs before Valheim's FejdStartup.Awake
            Jotunn.Logger.LogInfo("FejdStartup is going to awake");

            // Call this method so the original game method is invoked
            orig(self);

            // This code runs after Valheim's FejdStartup.Awake
            Jotunn.Logger.LogInfo("FejdStartup has awoken");
        }

        // Add custom recipes
        private void AddRecipes()
        {
            // Create a custom recipe with a RecipeConfig
            CustomRecipe meatRecipe = new CustomRecipe(new RecipeConfig()
            {
                Item = "CookedMeat",                    // Name of the item prefab to be crafted
                Requirements = new RequirementConfig[]  // Resources and amount needed for it to be crafted
                {
            new RequirementConfig { Item = "Stone", Amount = 2 },
            new RequirementConfig { Item = "Wood", Amount = 1 }
                }
            });
            ItemManager.Instance.AddRecipe(meatRecipe);
        }
    }
}