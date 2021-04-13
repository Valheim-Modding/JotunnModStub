// JotunnModStub
// a Valheim mod skeleton using JötunnLib
// 
// File:    JotunnModStub.cs
// Project: JotunnModStub

using BepInEx;
using UnityEngine;
using BepInEx.Configuration;
using JotunnLib.Utils;
using System.Reflection;
using JotunnLib.Managers;
using Logger = JotunnLib.Logger;
using JotunnLib.Configs;
using System;
using JotunnLib.Entities;
using System.Collections.Generic;
using System.IO;

namespace JotunnModExample
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(JotunnLib.Main.ModGuid)]
    //[NetworkCompatibilty(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class JotunnModExample : BaseUnityPlugin
    {
        public const string PluginGUID = "com.jotunn.JotunnModExample";
        public const string PluginName = "JotunnModExample";
        public const string PluginVersion = "1.0.0";
        public static new JotunnLib.Logger Logger;
        public static new ConfigFile Config;

        public AssetBundle TestAssets;
        public AssetBundle BlueprintRuneBundle;
        public Skills.SkillType TestSkillType = 0;
        private Texture2D testTex;
        private Sprite testSprite;
        private GameObject backpackPrefab;
        private AssetBundle embeddedResourceBundle;
        private bool clonedItemsAdded;

        private void Awake()
        {
            Config = base.Config;

            LocalizationManager.Instance.LocalizationRegister += registerLocalization;

            // Do all your init stuff here
            loadAssets();
            addItemsWithConfigs();
            addEmptyPiece();
            addMockedItems();
            addSkills();

            On.ObjectDB.CopyOtherDB += addClonedItems;
            ItemManager.OnAfterInit += () => 
            {
                //backpackPrefab.GetComponent<ParticleSystemRenderer>().gameObject;
            };
            
        }


        private void loadAssets()
        {
            // Load texture
            testTex = AssetUtils.LoadTexture("JotunnModExample/Assets/test_tex.jpg");
            testSprite = Sprite.Create(testTex, new Rect(0f, 0f, testTex.width, testTex.height), Vector2.zero);

            // Load asset bundle from filesystem
            TestAssets = AssetUtils.LoadAssetBundle("JotunnModExample/Assets/jotunnlibtest");
            JotunnLib.Logger.LogInfo(TestAssets);

            // Load asset bundle from filesystem
            BlueprintRuneBundle = AssetUtils.LoadAssetBundle("JotunnModExample/Assets/blueprints");
            JotunnLib.Logger.LogInfo(BlueprintRuneBundle);

            //Load embedded resources
            JotunnLib.Logger.LogInfo($"Embedded resources: {string.Join(",", Assembly.GetExecutingAssembly().GetManifestResourceNames())}");
            embeddedResourceBundle = AssetUtils.LoadAssetBundleFromResources("eviesbackpacks1", Assembly.GetExecutingAssembly());
            backpackPrefab = embeddedResourceBundle.LoadAsset<GameObject>("Assets/Evie/CapeSilverBackpack.prefab");

            // Embedded Resources
            
        }

        private void addClonedItems(On.ObjectDB.orig_CopyOtherDB orig, ObjectDB self, ObjectDB other)
        {
            // You want that to run only once, JotunnLib has the item cached for the game session
            if (!clonedItemsAdded)
            {
                //Create a custom resource
                CustomItem recipeComponent = new CustomItem("CustomWood", "Wood");
                ItemManager.Instance.AddItem(recipeComponent);
                recipeComponent.ItemDrop.m_itemData.m_shared.m_name = "$item_customWood";
                recipeComponent.ItemDrop.m_itemData.m_shared.m_description = "$item_customWood_desc";

                // Create and add a custom item based on SwordBlackmetal
                CustomItem CI = new CustomItem("EvilSword", "SwordBlackmetal");
                ItemManager.Instance.AddItem(CI);

                // Replace vanilla properties of the custom item
                var itemDrop = CI.ItemDrop;
                itemDrop.m_itemData.m_shared.m_name = "$item_evilsword";
                itemDrop.m_itemData.m_shared.m_description = "$item_evilsword_desc";

                recipeEvilSword(itemDrop);

                clonedItemsAdded = true;
            }

            // Hook is prefix, we just need to be able to get the vanilla prefabs, JotunnLib registers them in ObjectDB
            orig(self, other);
        }

        private static void recipeEvilSword(ItemDrop itemDrop)
        {
            // Create and add a recipe for the copied item
            Recipe recipe = ScriptableObject.CreateInstance<Recipe>();
            recipe.name = "Recipe_EvilSword";
            recipe.m_item = itemDrop;
            recipe.m_craftingStation = PrefabManager.Cache.GetPrefab<CraftingStation>("piece_workbench");
            recipe.m_resources = new Piece.Requirement[]
            {
                    new Piece.Requirement()
                    {
                        m_resItem = PrefabManager.Cache.GetPrefab<ItemDrop>("Stone"),
                        m_amount = 1
                    },
                    new Piece.Requirement()
                    {
                        m_resItem = PrefabManager.Cache.GetPrefab<ItemDrop>("CustomWood"),
                        m_amount = 1
                    }
            };
            CustomRecipe CR = new CustomRecipe(recipe, false, false);
            ItemManager.Instance.AddRecipe(CR);
        }

        // Add new Items with item Configs
        private void addItemsWithConfigs()
        {
            // Add a custom piece table
            PieceManager.Instance.AddPieceTable(BlueprintRuneBundle.LoadAsset<GameObject>("_BlueprintPieceTable"));
            CreateBlueprintRune();
            CreateRunePieces();

            // Don't forget to unload the bundle to free the resources
            BlueprintRuneBundle.Unload(false);
        }

        private void CreateBlueprintRune()
        {
            // Create and add a custom item
            // CustomItem can be instantiated with an AssetBundle and will load the prefab from there
            CustomItem rune = new CustomItem(BlueprintRuneBundle, "BlueprintRune", false);
            ItemManager.Instance.AddItem(rune);

            // Create and add a recipe for the custom item
            CustomRecipe runeRecipe = new CustomRecipe(new RecipeConfig()
            {
                Item = "BlueprintRune",
                Amount = 1,
                Requirements = new PieceRequirementConfig[]
                {
                    new PieceRequirementConfig {Item = "Stone", Amount = 1}
                }
            });
            ItemManager.Instance.AddRecipe(runeRecipe);
        }

        private void addEmptyPiece()
        {
            CustomPiece CP = new CustomPiece("$piece_lul", "Hammer");
            var piece = CP.Piece;
            piece.m_icon = testSprite;
            var prefab = CP.PiecePrefab;
            prefab.GetComponent<MeshRenderer>().material.mainTexture = testTex;
            PieceManager.Instance.AddPiece(CP);
        }

        private void CreateRunePieces()
        {
            // Create and add custom pieces
            GameObject makebp_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("make_blueprint");
            CustomPiece makebp = new CustomPiece(makebp_prefab, new PieceConfig
            {
                PieceTable = "_BlueprintPieceTable",
                AllowedInDungeons = false
            });
            PieceManager.Instance.AddPiece(makebp);
            GameObject placebp_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("piece_blueprint");
            CustomPiece placebp = new CustomPiece(placebp_prefab, new PieceConfig
            {
                PieceTable = "_BlueprintPieceTable",
                AllowedInDungeons = true,
                Requirements = new PieceRequirementConfig[]
                {
                    new PieceRequirementConfig {Item = "Wood", Amount = 2}
                }
            });
            blueprintRuneLocalizations();
            PieceManager.Instance.AddPiece(placebp);
        }

        private void addMockedItems()
        {
            if (!backpackPrefab) JotunnLib.Logger.LogWarning($"Failed to load asset from bundle: {embeddedResourceBundle}");
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

        void registerLocalization(object sender, EventArgs e)
        {
            // Add translations for the custom item in addClonedItems
            LocalizationManager.Instance.AddLocalization(new LocalizationConfig("English")
            {
                Translations =
                {
                    { "item_evilsword", "Sword of Darkness" },
                    { "item_evilsword_desc", "Bringing the light" }
                }
            });

            // Add translations for the custom piece in addEmptyItems
            LocalizationManager.Instance.AddLocalization(new LocalizationConfig("English")
            {
                Translations =
                {
                    { "piece_lul", "Lulz" }
                }
            });
        }

        private void blueprintRuneLocalizations()
        {
            TextAsset[] textAssets = BlueprintRuneBundle.LoadAllAssets<TextAsset>();
            foreach (var textAsset in textAssets)
            {
                var lang = textAsset.name.Replace(".json", null);
                LocalizationManager.Instance.AddJson(lang, textAsset.ToString());
            }
        }


        void addSkills()
        {
            // Test adding a skill with a texture
            Sprite testSkillSprite = Sprite.Create(testTex, new Rect(0f, 0f, testTex.width, testTex.height), Vector2.zero);
            TestSkillType = SkillManager.Instance.AddSkill(new SkillConfig
            {
                Identifier = "com.jotunnlib.JotunnModExample.testskill",
                Name = "TestingSkill",
                Description = "A nice testing skill!",
                Icon = testSkillSprite,
                IncreaseStep = 1f
            });
            //if(!TestSkillType) Logger.
        }

#if DEBUG
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            { // Set a breakpoint here to break on F6 key press
                Player.m_localPlayer.RaiseSkill(TestSkillType, 1);
            }
        }
#endif
    }
}