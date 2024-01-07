using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using PieceManagerKG;
using ServerSyncKG;
using UnityEngine.Audio;
using UnityEngine;

namespace ItemDrawersKGMod;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid)]
//[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
internal class ItemDrawersKG : BaseUnityPlugin
{
    public const string PluginGUID = "kg_ItemDrawers"; // kg.ItemDrawersKG
    public const string PluginName = "Item Drawers KG Mod";
    public const string PluginVersion = "0.0.1";
    
    // Use this class to add your own localization to the game
    // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
    public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();
        
    private static ConfigSync configSync = new(PluginGUID) { DisplayName = PluginGUID, CurrentVersion = PluginVersion, MinimumRequiredVersion = PluginVersion, ModRequired = true, IsLocked = true };
    public static ItemDrawersKG _thistype;
    private static AssetBundle asset;

    public static ConfigEntry<int> DrawerPickupRange;
    public static ConfigEntry<int> MaxDrawerPickupRange;
    public static ConfigEntry<Vector3> DefaultColor;
    private static ConfigEntry<string> IncludeList;
    public static HashSet<string> IncludeSet = new();
    private static BuildPiece _drawer_wood;
    private static BuildPiece _drawer_stone;
    private static BuildPiece _drawer_marble;
    private static BuildPiece _drawer_wood_panel;
    private static BuildPiece _drawer_stone_panel;
    private static BuildPiece _drawer_marble_panel;
    private static BuildPiece _drawer_nomodel;
    public static GameObject Explosion;

    private void OnGUI() => DrawerComponent.ProcessGUI();
    private void Update() => DrawerComponent.ProcessInput();

    private void Awake()
    {
        Jotunn.Logger.LogInfo("ModStubKG has landed");
        LoadTimer();
        _thistype = this;
        IncludeList = config("General", "IncludeList", "DragonEgg", "List of items with max stack size 1 to include in the drawer. Leave blank to include all items.");
        IncludeList.SettingChanged += ResetList;
        ResetList(null, null);
        DrawerPickupRange = config("General", "DrawerPickupRange", 4, "Range at which you can pick up items from the drawer.");
        MaxDrawerPickupRange = config("General", "MaxDrawerPickupRange", 100, "Maximum range at which you can pick up items from the drawer.");
        DefaultColor = config("General", "DefaultColor", Vector3.right, "Default color of the drawer text.");
        asset = GetAssetBundle("kg_itemdrawers");

        Explosion = asset.LoadAsset<GameObject>("kg_ItemDrawer_Explosion");

        _drawer_wood = new BuildPiece(asset, "kg_ItemDrawer_Wood");
        _drawer_wood.Name.English("Wooden Item Drawer");
        _drawer_wood.Prefab.AddComponent<DrawerComponent>();
        _drawer_wood.Category.Set("Item Drawers");
        _drawer_wood.Crafting.Set(CraftingTable.None);
        _drawer_wood.RequiredItems.Add("Wood", 10, true);

        _drawer_stone = new BuildPiece(asset, "kg_ItemDrawer_Stone");
        _drawer_stone.Name.English("Stone Item Drawer");
        _drawer_stone.Prefab.AddComponent<DrawerComponent>();
        _drawer_stone.Category.Set("Item Drawers");
        _drawer_stone.Crafting.Set(CraftingTable.None);
        _drawer_stone.RequiredItems.Add("Stone", 10, true);

        _drawer_marble = new BuildPiece(asset, "kg_ItemDrawer_Marble");
        _drawer_marble.Name.English("Marble Item Drawer");
        _drawer_marble.Prefab.AddComponent<DrawerComponent>();
        _drawer_marble.Category.Set("Item Drawers");
        _drawer_marble.Crafting.Set(CraftingTable.None);
        _drawer_marble.RequiredItems.Add("BlackMarble", 10, true);

        _drawer_wood_panel = new BuildPiece(asset, "kg_ItemDrawerPanel_Wood");
        _drawer_wood_panel.Name.English("Wooden Item Drawer Panel");
        _drawer_wood_panel.Prefab.AddComponent<DrawerComponent>();
        _drawer_wood_panel.Category.Set("Item Drawers");
        _drawer_wood_panel.Crafting.Set(CraftingTable.None);
        _drawer_wood_panel.RequiredItems.Add("Wood", 10, true);

        _drawer_stone_panel = new BuildPiece(asset, "kg_ItemDrawerPanel_Stone");
        _drawer_stone_panel.Name.English("Stone Item Drawer Panel");
        _drawer_stone_panel.Prefab.AddComponent<DrawerComponent>();
        _drawer_stone_panel.Category.Set("Item Drawers");
        _drawer_stone_panel.Crafting.Set(CraftingTable.None);
        _drawer_stone_panel.RequiredItems.Add("Stone", 10, true);

        _drawer_marble_panel = new BuildPiece(asset, "kg_ItemDrawerPanel_Marble");
        _drawer_marble_panel.Name.English("Marble Item Drawer Panel");
        _drawer_marble_panel.Prefab.AddComponent<DrawerComponent>();
        _drawer_marble_panel.Category.Set("Item Drawers");
        _drawer_marble_panel.Crafting.Set(CraftingTable.None);
        _drawer_marble_panel.RequiredItems.Add("BlackMarble", 10, true);

        _drawer_nomodel = new BuildPiece(asset, "kg_ItemDrawer_NoModel");
        _drawer_nomodel.Name.English("Item Drawer (No Model)");
        _drawer_nomodel.Prefab.AddComponent<DrawerComponent>();
        _drawer_nomodel.Category.Set("Item Drawers");
        _drawer_nomodel.Crafting.Set(CraftingTable.None);
        _drawer_nomodel.RequiredItems.Add("GreydwarfEye", 10, true);

        new Harmony(PluginGUID).PatchAll();
    }

    #region StupidTimer

    public System.Timers.Timer _timer;

    private void LoadTimer()
    {
        _timer = new System.Timers.Timer();
        _timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
        _timer.Interval = 5000;
        _timer.Start();
    }
    private void OnTimedEvent(object sender, ElapsedEventArgs e)
        => Logger.LogInfo($"{nameof(ItemDrawersKG)} has ticked ${DateTime.Now:O}");

    #endregion

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    private static class ZNetScene_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ZNetScene __instance)
        {
            __instance.m_namedPrefabs[Explosion.name.GetStableHashCode()] = Explosion;

            _drawer_wood.Prefab.GetComponent<Piece>().m_placeEffect = __instance.GetPrefab("woodwall").GetComponent<Piece>().m_placeEffect;
            _drawer_stone.Prefab.GetComponent<Piece>().m_placeEffect = __instance.GetPrefab("stone_wall_1x1").GetComponent<Piece>().m_placeEffect;
            _drawer_marble.Prefab.GetComponent<Piece>().m_placeEffect = __instance.GetPrefab("blackmarble_1x1").GetComponent<Piece>().m_placeEffect;
            _drawer_wood_panel.Prefab.GetComponent<Piece>().m_placeEffect = __instance.GetPrefab("woodwall").GetComponent<Piece>().m_placeEffect;
            _drawer_stone_panel.Prefab.GetComponent<Piece>().m_placeEffect = __instance.GetPrefab("stone_wall_1x1").GetComponent<Piece>().m_placeEffect;
            _drawer_marble_panel.Prefab.GetComponent<Piece>().m_placeEffect = __instance.GetPrefab("blackmarble_1x1").GetComponent<Piece>().m_placeEffect;
            _drawer_nomodel.Prefab.GetComponent<Piece>().m_placeEffect = __instance.GetPrefab("woodwall").GetComponent<Piece>().m_placeEffect;
        }
    }

    [HarmonyPatch(typeof(AudioMan), nameof(AudioMan.Awake))]
    private static class AudioMan_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(AudioMan __instance)
        {
            AudioMixerGroup SFXgroup = __instance.m_masterMixer.FindMatchingGroups("SFX")[0];
            foreach (GameObject go in asset.LoadAllAssets<GameObject>())
            {
                foreach (AudioSource audioSource in go.GetComponentsInChildren<AudioSource>(true))
                    audioSource.outputAudioMixerGroup = SFXgroup;
            }
        }
    }

    private void ResetList(object sender, EventArgs eventArgs) =>
        IncludeSet = new HashSet<string>(IncludeList.Value.Replace(" ", "").Split(','));

    private static AssetBundle GetAssetBundle(string filename)
    {
        Assembly execAssembly = Assembly.GetExecutingAssembly();
        string resourceName = execAssembly.GetManifestResourceNames().Single(str => str.EndsWith(filename));
        using Stream stream = execAssembly.GetManifestResourceStream(resourceName)!;
        return AssetBundle.LoadFromStream(stream);
    }

    ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
    {
        ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

        SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);

}