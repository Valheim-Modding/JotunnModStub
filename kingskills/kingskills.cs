// kingskills
// a Valheim mod skeleton using Jötunn
// 
// File:    kingskills.cs
// Project: kingskills

using BepInEx;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using kingskills.Commands;

namespace kingskills
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class KingSkills : BaseUnityPlugin
    {
        public const string PluginGUID = "bearking.kingskills";
        public const string PluginName = "King's Skills";
        public const string PluginVersion = "0.0.1";

        public static Skills.SkillType TestSkillType = 0;

        Harmony harmony = new Harmony(PluginGUID);
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private void Awake()
        {
            Jotunn.Logger.LogInfo("King's skills has awakened!");
            InitConfig();
            CommandManager.Instance.AddConsoleCommand(new BearSkillCommand());
            CommandManager.Instance.AddConsoleCommand(new SkillUpdateCommand());
            AddSkills();
            harmony.PatchAll();
        }

        private void InitConfig()
        {
            WeaponExperience.Config.Init(Config);
        }

        private void AddSkills()
        {
            Jotunn.Configs.SkillConfig skill = new Jotunn.Configs.SkillConfig();
            skill.Identifier = "bearking.kingskills.bearskill";
            skill.Name = "Bear";
            skill.Description = "Become good at bearing";
            skill.IncreaseStep = 1f;

            TestSkillType = SkillManager.Instance.AddSkill(skill);

            Jotunn.Logger.LogMessage(TestSkillType);
        }
    }
}

