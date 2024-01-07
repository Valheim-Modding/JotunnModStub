using System;
using System.IO;
using System.Timers;
using BepInEx;
using ExampleRecipesMod.Models;
using ExampleRecipesMod.Services;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace ExampleRecipesMod;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid)]
//[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
internal class ExampleRecipes : BaseUnityPlugin
{
    public const string PluginGUID = "com.wernercd.examplerecipesmod";
    public const string PluginName = "ExampleRecipesMod";
    public const string PluginVersion = "0.0.1";
        
    // Use this class to add your own localization to the game
    // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
    public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();


    private void Awake()
    {
        // https://valheim-modding.github.io/Jotunn/tutorials/overview.html
        Jotunn.Logger.LogInfo("Example Recipes Mod has landed");
        //LoadStupidTimer();
        LoadAssetBundle();
        AddRecipes();
        UnloadAssetBundle();

        PrefabManager.OnPrefabsRegistered += () =>
        {
            // The null check is necessary in case users remove the item from the config
            var funkySword = PrefabManager.Instance.GetPrefab("FunkySword");
            if (funkySword != null)
            {
                // Add fire damage to the funky sword
                funkySword.GetComponent<ItemDrop>().m_itemData.m_shared.m_damages.m_fire = 1000;

                // Add funky sword to skeleton drops with 100% drop chance
                var skeletonDrop = PrefabManager.Instance.GetPrefab("Skeleton").GetComponent<CharacterDrop>();
                skeletonDrop.m_drops.Add(new CharacterDrop.Drop
                {
                    m_amountMax = 1,
                    m_amountMin = 1,
                    m_chance = 1f,
                    m_levelMultiplier = false,
                    m_onePerPlayer = false,
                    m_prefab = funkySword
                });

                var segullDrop = PrefabManager.Instance.GetPrefab("Seagal").GetComponent<DropOnDestroyed>().m_dropWhenDestroyed;
                segullDrop.m_drops.Add(new DropTable.DropData
                {
                    m_item = funkySword,
                    m_stackMin = 1,
                    m_stackMax = 1,
                    m_weight = 1f
                });
            }
        };
    }

    #region StupidTimer

    public System.Timers.Timer _timer;

    private void LoadStupidTimer()
    {
        _timer = new System.Timers.Timer();
        _timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
        _timer.Interval = 5000;
        _timer.Start();
    }
    private void OnTimedEvent(object sender, ElapsedEventArgs e) 
        => Jotunn.Logger.LogInfo($"{nameof(ExampleRecipes)} has ticked ${DateTime.Now:O}");

    #endregion

    #region AssetBundle

    private AssetBundle _embeddedResourceBundle;

    private void LoadAssetBundle()
    {
        Jotunn.Logger.LogInfo($"Embedded resources: {string.Join(",", typeof(ExampleRecipes).Assembly.GetManifestResourceNames())}");
        _embeddedResourceBundle = AssetUtils.LoadAssetBundleFromResources("testbundle", typeof(ExampleRecipes).Assembly);
    }
    private void UnloadAssetBundle()
    {
        _embeddedResourceBundle.Unload(false);
    }

    #endregion

    #region Recipies

    private void AddRecipes()
    {
        var extendedRecipes = ExtendedRecipeManager.LoadRecipesFromJson($"{Path.GetDirectoryName(typeof(ExampleRecipes).Assembly.Location)}/Assets/recipes.json");

        extendedRecipes.ForEach(extendedRecipe =>
        {
            // Load prefab from asset bundle
            var prefab = _embeddedResourceBundle.LoadAsset<GameObject>(extendedRecipe.prefabPath);

            // Create custom item
            var customItem = new CustomItem(prefab, true);

            // Edit item drop to set name and description
            var itemDrop = customItem.ItemDrop;
            itemDrop.m_itemData.m_shared.m_name = extendedRecipe.name;
            itemDrop.m_itemData.m_shared.m_description = extendedRecipe.description;

            // Add localizations for name and description
            LocalizationManager.Instance.AddToken(extendedRecipe.name, extendedRecipe.nameValue, false);
            LocalizationManager.Instance.AddToken(extendedRecipe.descriptionToken, extendedRecipe.description, false);

            // Add item with recipe
            customItem.Recipe = ExtendedRecipe.Convert(extendedRecipe);
            ItemManager.Instance.AddItem(customItem);
        });
    }

    #endregion
}