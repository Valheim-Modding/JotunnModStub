using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Configs;
using System.Collections.Generic;
using System;
using HarmonyLib;
using UnityEngine;
using System.IO;
using System.Reflection;

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

        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();
        internal Harmony harmony;
        internal Assembly assembly;
        public static GameObject inventoryPanel;
        public static bool buildingHighlightAlwaysOn = false;
        public static List<WearNTear> highlighted = new List<WearNTear>();

        public void Main()
        {
            harmony = new Harmony(PluginGUID);
            assembly = Assembly.GetExecutingAssembly();
        }

        public void Start()
        {
            harmony.PatchAll(assembly);
        }

        private void Awake()
        {
            AddLocalizations();
            PrefabManager.OnVanillaPrefabsAvailable += AddItems;
            On.FejdStartup.Awake += FejdStartup_Awake;
            Jotunn.Logger.LogInfo("SklentMod has landed");
        }

        private void AddLocalizations()
        {
            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                {"item_keefcakedough", "Keef Cake Dough"}, {"item_keefcakedough_desc", "keeeefy"},
                {"item_keefcake", "Keef Cake"}, {"item_keefcake_desc", "eat me ;)"}

            });
        }

        // Implementation of cloned items
        private void AddItems()
        {
            try
            {
                CustomItem keefDough = new CustomItem("KeefCakeDough", "BreadDough");
                ItemManager.Instance.AddItem(keefDough);
                var doughDrop = keefDough.ItemDrop;
                doughDrop.m_itemData.m_shared.m_name = "$item_keefcakedough";
                doughDrop.m_itemData.m_shared.m_description = "$item_keefcakedough_desc";

                CustomItem keefCake = new CustomItem("KeefCake", "Bread");
                ItemManager.Instance.AddItem(keefCake);
                var keefCakeDrop = keefCake.ItemDrop;
                keefCakeDrop.m_itemData.m_shared.m_name = "$item_keefcake";
                keefCakeDrop.m_itemData.m_shared.m_description = "$item_keefcake_desc";
                keefCakeDrop.m_itemData.m_shared.m_food = 0;
                keefCakeDrop.m_itemData.m_shared.m_foodRegen = 0;
                keefCakeDrop.m_itemData.m_shared.m_foodStamina = 90;
                keefCakeDrop.m_itemData.m_shared.m_foodColor = UnityEngine.Color.black;
                keefCakeDrop.m_itemData.m_shared.m_maxStackSize = 3;

                AddRecipes();
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogError($"Error while adding cloned item: {ex.Message}");
                Jotunn.Logger.LogError(ex);
                Jotunn.Logger.LogError(ex.StackTrace); 
            }
            finally
            {
                // You want that to run only once, Jotunn has the item cached for the game session
                PrefabManager.OnVanillaPrefabsAvailable -= AddItems;
            }
        }


        // Add custom recipes
        private void AddRecipes()
        {
            CustomRecipe odinCape = new CustomRecipe(new RecipeConfig()
            {
                Item = "SwordIronFire",
                Requirements = new RequirementConfig[]
                {
                    new RequirementConfig { Item = "Wood", Amount = 1 },
                },
                CraftingStation = "forge",
            });
            ItemManager.Instance.AddRecipe(odinCape);


            CustomRecipe keefDough = new CustomRecipe(new RecipeConfig()
            {
                Item = "KeefCakeDough",
                Requirements = new RequirementConfig[]
                {
                    new RequirementConfig { Item = "BarleyFlour", Amount = 1 },
                    new RequirementConfig { Item = "Honey", Amount = 1 },
                    new RequirementConfig { Item = "Tar", Amount = 1 }
                },
                CraftingStation = "piece_cauldron",
            });
            ItemManager.Instance.AddRecipe(keefDough);

            CustomRecipe keef = new CustomRecipe(new RecipeConfig()
            {
                Item = "KeefCake",
                Requirements = new RequirementConfig[]
               {
                    new RequirementConfig { Item = "KeefCakeDough", Amount = 1 },
               },
                CraftingStation = "piece_cauldron",
            });
            ItemManager.Instance.AddRecipe(keef);
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
    }
}