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
using JotunnModExample.ConsoleCommands;

namespace JotunnModExample
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(JotunnLib.Main.ModGuid)]
    [NetworkCompatibilty(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
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

        private bool showMenu = false;
        private bool showGUIButton = false;
        private Texture2D testTex;
        private Sprite testSprite;
        private GameObject testPanel;
        private bool clonedItemsAdded = false;
        private GameObject backpackPrefab;
        private AssetBundle embeddedResourceBundle;

        private void Awake()
        {
            Config = base.Config;

            LocalizationManager.Instance.LocalizationRegister += registerLocalization;

            // Do all your init stuff here
            loadAssets();
            addItemsWithConfigs();
            addEmptyPiece();
            addMockedItems();
            addCommands();
            addSkills();
            createConfigValues();

            On.ObjectDB.CopyOtherDB += addClonedItems;

        }

        // Called every frame
        private void Update()
        {
            // Since our Update function in our BepInEx mod class will load BEFORE Valheim loads,
            // we need to check that ZInput is ready to use first.
            if (ZInput.instance != null)
            {
                // Check if our button is pressed. This will only return true ONCE, right after our button is pressed.
                // If we hold the button down, it won't spam toggle our menu.
                if (ZInput.GetButtonDown("JotunnModExample_Menu"))
                {
                    showMenu = !showMenu;
                }

                if (ZInput.GetButtonDown("GUIManagerTest"))
                {
                    showGUIButton = !showGUIButton;
                }
            }

#if DEBUG
            if (Input.GetKeyDown(KeyCode.F8))
            {
                Player.m_localPlayer.RaiseSkill(TestSkillType, 1);
            }
#endif
        }

        // Display our GUI if enabled
        private void OnGUI()
        {
            if (showMenu)
            {
                GUI.Box(new Rect(40, 40, 150, 250), "JotunnModExample");
            }

            if (showGUIButton)
            {
                if (testPanel == null)
                {
                    if (GUIManager.Instance == null)
                    {
                        Logger.LogError("GUIManager instance is null");
                        return;
                    }

                    if (GUIManager.PixelFix == null)
                    {
                        Logger.LogError("GUIManager pixelfix is null");
                        return;
                    }
                    testPanel = GUIManager.Instance.CreateWoodpanel(GUIManager.PixelFix.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), 850, 600);

                    GUIManager.Instance.CreateButton("A Test Button - long dong schlongsen text", testPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0, 0), 250, 100).SetActive(true);
                    if (testPanel == null)
                    {
                        return;
                    }
                }
                testPanel.SetActive(!testPanel.activeSelf);
                showGUIButton = false;
            }
        }

        // Various forms of asset loading
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

        // Implementation of cloned items
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

        // Add new assets via item Configs
        private void addItemsWithConfigs()
        {
            // Add a custom piece table
            PieceManager.Instance.AddPieceTable(BlueprintRuneBundle.LoadAsset<GameObject>("_BlueprintPieceTable"));
            CreateBlueprintRune();
            CreateRunePieces();

            // Don't forget to unload the bundle to free the resources
            BlueprintRuneBundle.Unload(false);
        }

        //Implementation of items and recipes via configs
        private void CreateBlueprintRune()
        {
            // Create and add a custom item
            var rune_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("BlueprintRune");
            var rune = new CustomItem(rune_prefab, fixReference: false,
                new ItemConfig
                {
                    Amount = 1,
                    Requirements = new[]
                    {
                        new RequirementConfig { Item = "Stone", Amount = 1 }
                    }
                });
            ItemManager.Instance.AddItem(rune);
        }

        //Implementation of stub objects
        private void addEmptyPiece()
        {
            CustomPiece CP = new CustomPiece("$piece_lul", "Hammer");
            if (CP != null)
            {
                var piece = CP.Piece;
                piece.m_icon = testSprite;
                var prefab = CP.PiecePrefab;
                prefab.GetComponent<MeshRenderer>().material.mainTexture = testTex;
                PieceManager.Instance.AddPiece(CP);
            }
        }

        //Implementation of pieces via configs.
        private void CreateRunePieces()
        {
            // Create and add custom pieces
            var makebp_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("make_blueprint");
            var makebp = new CustomPiece(makebp_prefab,
                new PieceConfig
                {
                    PieceTable = "_BlueprintPieceTable"
                });
            PieceManager.Instance.AddPiece(makebp);

            var placebp_prefab = BlueprintRuneBundle.LoadAsset<GameObject>("piece_blueprint");
            var placebp = new CustomPiece(placebp_prefab,
                new PieceConfig
                {
                    PieceTable = "_BlueprintPieceTable",
                    AllowedInDungeons = true,
                    Requirements = new[]
                    {
                        new RequirementConfig { Item = "Wood", Amount = 2 }
                    }
                });
            PieceManager.Instance.AddPiece(placebp);
            blueprintRuneLocalizations();
        }

        //Implementation of assets via using manual recipe creation and prefab cache's
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

        //Implementation of assets using mocks, adding recipe's manually without the config abstraction
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

        //A manual implementation of localisation.
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

        //Add localisations from asset bundles
        private void blueprintRuneLocalizations()
        {
            TextAsset[] textAssets = BlueprintRuneBundle.LoadAllAssets<TextAsset>();
            foreach (var textAsset in textAssets)
            {
                var lang = textAsset.name.Replace(".json", null);
                LocalizationManager.Instance.AddJson(lang, textAsset.ToString());
            }
        }

        //Add a new test skill
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
        }

        // Register new console commands
        private void addCommands()
        {
            CommandManager.Instance.AddConsoleCommand(new PrintItemsCommand());
            CommandManager.Instance.AddConsoleCommand(new TpCommand());
            CommandManager.Instance.AddConsoleCommand(new ListPlayersCommand());
            CommandManager.Instance.AddConsoleCommand(new SkinColorCommand());
            CommandManager.Instance.AddConsoleCommand(new RaiseSkillCommand());
            CommandManager.Instance.AddConsoleCommand(new BetterSpawnCommand());
        }

        // Create some sample configuration values to check server sync
        private void createConfigValues()
        {
            Config.SaveOnConfigSet = true;

            //Here we showcase BepInEx's configuration flexibility. This is nothing to do we JVL, however we do provide an interface that is capable of respecting the configuration parameters detailed here.
            Config.Bind("JotunnLibTest", "StringValue1", "StringValue", new ConfigDescription("Server side string", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("JotunnLibTest", "FloatValue1", 750f, new ConfigDescription("Server side float", new AcceptableValueRange<float>(0f,1000f), new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("JotunnLibTest", "IntegerValue1", 200, new ConfigDescription("Server side integer", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("JotunnLibTest", "BoolValue1", false, new ConfigDescription("Server side bool", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Config.Bind("JotunnLibTest", "KeycodeValue", KeyCode.F10,
                new ConfigDescription("Server side Keycode", null, new ConfigurationManagerAttributes() { IsAdminOnly = true }));
        }
    }
}