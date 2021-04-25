// JotunnModStub
// a Valheim mod skeleton using JötunnLib
// 
// File:    JotunnModStub.cs
// Project: JotunnModStub

using BepInEx;
using UnityEngine;
using BepInEx.Configuration;
using Jotunn.Utils;
using System.Reflection;
using Jotunn.Managers;
using Logger = Jotunn.Logger;
using Jotunn.Configs;
using System;
using Jotunn.Entities;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;

namespace JotunnModExample
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibilty(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class JotunnModExample : BaseUnityPlugin
    {
        public const string PluginGUID = "Jotunn.Mod.Example";
        public const string PluginName = "JotunnModExample";
        public const string PluginVersion = "0.0.1";
        public static new Jotunn.Logger Logger;

        private AssetBundle embeddedResourceBundle;
        private GameObject backpackPrefab;
        private AssetBundle magicBoxBundle;
        private GameObject magicboxPrefab;

        private bool clonedItemsProcessed = false;

        private void Awake()
        {
            // Do all your init stuff here
            // Acceptable value ranges can be defined to allow configuration via a slider in the BepInEx ConfigurationManager: https://github.com/BepInEx/BepInEx.ConfigurationManager
            Config.Bind<int>("Main Section", "Example configuration integer", 1, new ConfigDescription("This is an example config, using a range limitation for ConfigurationManager", new AcceptableValueRange<int>(0, 100)));

            LoadAssets();
            AddMockedItems();
            AddItemsWithConfigs();

            // Hook ObjectDB.CopyOtherDB to add custom items cloned from vanilla items
            On.ObjectDB.CopyOtherDB += AddClonedItems;

        }

        private void LoadAssets()
        {
            //Load embedded resources
            Logger.LogInfo($"Embedded resources: {string.Join(",", Assembly.GetExecutingAssembly().GetManifestResourceNames())}");
            embeddedResourceBundle = AssetUtils.LoadAssetBundleFromResources("eviesbackpacks", Assembly.GetExecutingAssembly());
            backpackPrefab = embeddedResourceBundle.LoadAsset<GameObject>("Assets/Evie/CapeSilverBackpack.prefab");
            magicBoxBundle = AssetUtils.LoadAssetBundleFromResources("magicbox", Assembly.GetExecutingAssembly());
            magicboxPrefab = magicBoxBundle.LoadAsset<GameObject>("Assets/CustomItems/MagicBox/piece_magic_box.prefab");


        }
        // Implementation of assets using mocks, adding recipe's manually without the config abstraction
        private void AddMockedItems()
        {
            if (!backpackPrefab) Jotunn.Logger.LogWarning($"Failed to load asset from bundle: {embeddedResourceBundle}");
            else
            {
                // Create and add a custom item
                CustomItem CI = new CustomItem(backpackPrefab, true);
                ItemManager.Instance.AddItem(CI);

                //Create and add a custom recipe
                Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
                recipe.m_item = backpackPrefab.GetComponent<ItemDrop>();
                recipe.m_craftingStation = Mock<CraftingStation>.Create("piece_workbench");
                var ingredients = new List<Piece.Requirement>
                {
                    MockRequirement.Create("LeatherScraps", 10),
                    MockRequirement.Create("DeerHide", 2),
                    MockRequirement.Create("Iron", 4),
                };
                recipe.m_resources = ingredients.ToArray();
                CustomRecipe CR = new CustomRecipe(recipe, true, true);
                ItemManager.Instance.AddRecipe(CR);

                //Enable BoneReorder
                BoneReorder.ApplyOnEquipmentChanged();
            }
            embeddedResourceBundle.Unload(false);
        }

        private void AddItemsWithConfigs()
        {
            CreateMagicBox();

        }

        private void CreateMagicBox()
        {
            var magicBox = new CustomPiece(magicboxPrefab,
                new PieceConfig
                {
                    PieceTable = "_HammerPieceTable",
                    AllowedInDungeons = true,
                    Requirements = new[]
                    {
                        new RequirementConfig { Item = "Wood", Amount = 2 }
                    }
                });
            PieceManager.Instance.AddPiece(magicBox);
            Logger.LogInfo("Loaded magicBox");
        }

        private void AddClonedItems(On.ObjectDB.orig_CopyOtherDB orig, ObjectDB self, ObjectDB other)
        {
            // You want that to run only once, JotunnLib has the item cached for the game session
            if (!clonedItemsProcessed)
            {
                try
                {
                   

                    // Create and add a custom item based on SwordBlackmetal
                    CustomItem magicArmor = new CustomItem("MagicArmor", "ArmorIronChest");
                    ItemManager.Instance.AddItem(magicArmor);

                    // Replace vanilla properties of the custom item
                    var itemDrop = magicArmor.ItemDrop;
                    itemDrop.m_itemData.m_shared.m_name = "Magic Armor";
                    itemDrop.m_itemData.m_shared.m_description = "Godlike armor with a twist";
                    itemDrop.m_itemData.m_shared.m_armor = 999;

                    var burning = ScriptableObject.CreateInstance<SE_Burning>();

                    // load damages field
                    var burnDamageField = AccessTools.Field(typeof(SE_Burning), "m_damage");
                    var burnDamage = (HitData.DamageTypes)burnDamageField.GetValue(burning);

                    // edit fire damage
                    burnDamage.m_fire = 1;

                    // save damages field
                    burnDamageField.SetValue(burning, burnDamage);

                    // set item status effect
                    itemDrop.m_itemData.m_shared.m_equipStatusEffect = burning;

                    RecipeMagicArmor(itemDrop);
                   Jotunn.Logger.LogInfo("Loaded Magic Armor");
                    
                }
                catch (Exception ex)
                {
                    Jotunn.Logger.LogError($"Error while adding cloned item: {ex.Message}");
                }
                finally
                {
                    clonedItemsProcessed = true;
                }
            }

            // Hook is prefix, we just need to be able to get the vanilla prefabs, JotunnLib registers them in ObjectDB
            orig(self, other);
        }
        private void RecipeMagicArmor(ItemDrop itemDrop)
        {
            // Create and add a recipe for the copied item
            Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
            recipe.name = "Recipe_magicArmor";
            recipe.m_item = itemDrop;
            recipe.m_craftingStation = PrefabManager.Cache.GetPrefab<CraftingStation>("piece_magic_box");
            recipe.m_repairStation = PrefabManager.Cache.GetPrefab<CraftingStation>("piece_magic_box");
            recipe.m_resources = new Piece.Requirement[]
            {
                    new Piece.Requirement()
                    {
                        m_resItem = PrefabManager.Cache.GetPrefab<ItemDrop>("Wood"),
                        m_amount = 1
                    }      
            };
            CustomRecipe CR = new CustomRecipe(recipe, false, false);
            ItemManager.Instance.AddRecipe(CR);
        }

#if DEBUG
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            { // Set a breakpoint here to break on F6 key press
            }
        }
#endif
    }
}