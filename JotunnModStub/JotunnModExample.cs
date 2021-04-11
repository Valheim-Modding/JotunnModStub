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

        private void Awake()
        {
            Config = base.Config;
            // Do all your init stuff here
            loadAssets();
            addSkills();
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
            embeddedResourceBundle = AssetUtils.LoadAssetBundleFromResources("capeironbackpack", Assembly.GetExecutingAssembly());
            backpackPrefab = embeddedResourceBundle.LoadAsset<GameObject>("Assets/Evie/CapeIronBackpack.prefab");

            // Embedded Resources
            
        }

        void addSkills()
        {
            // Test adding a skill with a texture
            Sprite testSkillSprite = Sprite.Create(testTex, new Rect(0f, 0f, testTex.width, testTex.height), Vector2.zero);
            //TestSkillType = SkillManager.Instance.AddSkill("com.jotunnlib.JotunnModExample.testskill", "TestingSkill", "A nice testing skill!", 1f, testSkillSprite);
            TestSkillType = SkillManager.Instance.AddSkill(new SkillConfig { Identifier = "com.jotunnlib.JotunnModExample.testskill",
                Name = "TestingSkill",
                Description = "A nice testing skill!",
                Icon = testSkillSprite,
                IncreaseStep = 1f
            }, true);
            Logger.LogDebug(TestSkillType);
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