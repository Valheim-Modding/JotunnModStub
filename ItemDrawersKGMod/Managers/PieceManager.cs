using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace PieceManagerKG;

[PublicAPI]
public enum CraftingTable
{
    None,
    [InternalName("piece_workbench")] Workbench,
    [InternalName("piece_cauldron")] Cauldron,
    [InternalName("forge")] Forge,
    [InternalName("piece_artisanstation")] ArtisanTable,
    [InternalName("piece_stonecutter")] StoneCutter,
    [InternalName("piece_magetable")] MageTable,
    [InternalName("blackforge")] BlackForge,
    Custom,
}

public class InternalName : Attribute
{
    public readonly string internalName;
    public InternalName(string internalName) => this.internalName = internalName;
}

[PublicAPI]
public class ExtensionList
{
    public readonly List<ExtensionConfig> ExtensionStations = new();

    public void Set(CraftingTable table, int maxStationDistance = 5) => ExtensionStations.Add(new ExtensionConfig
        { Table = table, maxStationDistance = maxStationDistance });

    public void Set(string customTable, int maxStationDistance = 5) => ExtensionStations.Add(new ExtensionConfig
        { Table = CraftingTable.Custom, custom = customTable, maxStationDistance = maxStationDistance });
}

public struct ExtensionConfig
{
    public CraftingTable Table;
    public float maxStationDistance;
    public string? custom;
}

[PublicAPI]
public class CraftingStationList
{
    public readonly List<CraftingStationConfig> Stations = new();

    public void Set(CraftingTable table) => Stations.Add(new CraftingStationConfig { Table = table });

    public void Set(string customTable) => Stations.Add(new CraftingStationConfig
        { Table = CraftingTable.Custom, custom = customTable });
}

public struct CraftingStationConfig
{
    public CraftingTable Table;
    public int level;
    public string? custom;
}

[PublicAPI]
public enum BuildPieceCategory
{
    Misc = 0,
    Crafting = 1,
    Building = 2,
    Furniture = 3,
    All = 100,
    Custom = 99,
}

[PublicAPI]
public class RequiredResourcesList
{
    public readonly List<Requirement> Requirements = new();

    public void Add(string item, int amount, bool recover) => Requirements.Add(new Requirement
        { itemName = item, amount = amount, recover = recover });
}

public struct Requirement
{
    public string itemName;
    public int amount;
    public bool recover;
}

public struct SpecialProperties
{
    [Description("Admins should be the only ones that can build this piece.")]
    public bool AdminOnly;

    [Description("Turns off generating a config for this build piece.")]
    public bool NoConfig;
}

[PublicAPI]
public class BuildingPieceCategory
{
    public BuildPieceCategory Category;
    public string custom = "";

    public void Set(BuildPieceCategory category) => Category = category;

    public void Set(string customCategory)
    {
        Category = BuildPieceCategory.Custom;
        custom = customCategory;
    }
}

[PublicAPI]
public class PieceTool
{
    public readonly HashSet<string> Tools = new();

    public void Add(string tool) => Tools.Add(tool);
}

[PublicAPI]
public class BuildPiece
{
    internal class PieceConfig
    {
        public ConfigEntry<string> craft = null!;
        public ConfigEntry<BuildPieceCategory> category = null!;
        public ConfigEntry<string> customCategory = null!;
        public ConfigEntry<string> tools = null!;
        public ConfigEntry<CraftingTable> extensionTable = null!;
        public ConfigEntry<string> customExtentionTable = null!;
        public ConfigEntry<float> maxStationDistance = null!;
        public ConfigEntry<CraftingTable> table = null!;
        public ConfigEntry<string> customTable = null!;
    }

    internal static readonly List<BuildPiece> registeredPieces = new();
    internal static Dictionary<BuildPiece, PieceConfig> pieceConfigs = new();
    internal List<Conversion> Conversions = new();
    internal List<Smelter.ItemConversion> conversions = new();

    [Description(
        "Disables generation of the configs for your pieces. This is global, this turns it off for all pieces in your mod.")]
    public static bool ConfigurationEnabled = true;

    public readonly GameObject Prefab;

    [Description(
        "Specifies the resources needed to craft the piece.\nUse .Add to add resources with their internal ID and an amount.\nUse one .Add for each resource type the building piece should need.")]
    public readonly RequiredResourcesList RequiredItems = new();

    [Description("Sets the category for the building piece.")]
    public readonly BuildingPieceCategory Category = new();

    [Description("Specifies the tool needed to build your piece.\nUse .Add to add a tool.")]
    public readonly PieceTool Tool = new();

    [Description(
        "Specifies the crafting station needed to build your piece.\nUse .Add to add a crafting station, using the CraftingTable enum and a minimum level for the crafting station.")]
    public CraftingStationList Crafting = new();

    [Description("Makes this piece a station extension")]
    public ExtensionList Extension = new();

    [Description("Change the extended/special properties of your build piece.")]
    public SpecialProperties SpecialProperties;

    [Description("Specifies a config entry which toggles whether a recipe is active.")]
    public ConfigEntryBase? RecipeIsActive = null;

    private LocalizeKey? _name;

    public LocalizeKey Name
    {
        get
        {
            if (_name is { } name)
            {
                return name;
            }

            Piece data = Prefab.GetComponent<Piece>();
            if (data.m_name.StartsWith("$"))
            {
                _name = new LocalizeKey(data.m_name);
            }
            else
            {
                string key = "$piece_" + Prefab.name.Replace(" ", "_");
                _name = new LocalizeKey(key).English(data.m_name);
                data.m_name = key;
            }

            return _name;
        }
    }

    private LocalizeKey? _description;

    public LocalizeKey Description
    {
        get
        {
            if (_description is { } description)
            {
                return description;
            }

            Piece data = Prefab.GetComponent<Piece>();
            if (data.m_description.StartsWith("$"))
            {
                _description = new LocalizeKey(data.m_description);
            }
            else
            {
                string key = "$piece_" + Prefab.name.Replace(" ", "_") + "_description";
                _description = new LocalizeKey(key).English(data.m_description);
                data.m_description = key;
            }

            return _description;
        }
    }

    public BuildPiece(string assetBundleFileName, string prefabName, string folderName = "assets") : this(
        PiecePrefabManager.RegisterAssetBundle(assetBundleFileName, folderName), prefabName)
    {
    }

    public BuildPiece(AssetBundle bundle, string prefabName)
    {
        Prefab = PiecePrefabManager.RegisterPrefab(bundle, prefabName);
        registeredPieces.Add(this);
    }

    private class ConfigurationManagerAttributes
    {
        [UsedImplicitly] public int? Order;
        [UsedImplicitly] public bool? Browsable;
        [UsedImplicitly] public string? Category;
        [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
    }

    internal string[] activeTools = null!;

    private static object? configManager;

    internal static void Patch_FejdStartup(FejdStartup __instance)
    {
        Assembly? bepinexConfigManager = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "ConfigurationManager");

        Type? configManagerType = bepinexConfigManager?.GetType("ConfigurationManager.ConfigurationManager");
        configManager = configManagerType == null
            ? null
            : BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(configManagerType);

        void ReloadConfigDisplay()
        {
            if (configManagerType?.GetProperty("DisplayingWindow")!.GetValue(configManager) is true)
            {
                configManagerType.GetMethod("BuildSettingList")!.Invoke(configManager, Array.Empty<object>());
            }
        }

        foreach (BuildPiece piece in registeredPieces)
        {
            piece.activeTools = piece.Tool.Tools.DefaultIfEmpty("Hammer").ToArray();
        }

        if (ConfigurationEnabled)
        {
            bool SaveOnConfigSet = plugin.Config.SaveOnConfigSet;
            plugin.Config.SaveOnConfigSet = false;
            foreach (BuildPiece piece in registeredPieces)
            {
                if (piece.SpecialProperties.NoConfig) continue;
                PieceConfig cfg = pieceConfigs[piece] = new PieceConfig();
                Piece piecePrefab = piece.Prefab.GetComponent<Piece>();
                string pieceName = piecePrefab.m_name;
                string englishName = new Regex(@"[=\n\t\\""\'\[\]]*").Replace(english.Localize(pieceName), "").Trim();
                string localizedName = Localization.instance.Localize(pieceName).Trim();

                int order = 0;

                cfg.category = config(englishName, "Build Table Category",
                    piece.Category.Category,
                    new ConfigDescription($"Build Category where {localizedName} is available.", null,
                        new ConfigurationManagerAttributes { Order = --order, Category = localizedName }));
                ConfigurationManagerAttributes customTableAttributes = new()
                {
                    Order = --order, Browsable = cfg.category.Value == BuildPieceCategory.Custom,
                    Category = localizedName,
                };
                cfg.customCategory = config(englishName, "Custom Build Category",
                    piece.Category.custom,
                    new ConfigDescription("", null, customTableAttributes));

                void BuildTableConfigChanged(object o, EventArgs e)
                {
                    if (registeredPieces.Count > 0)
                    {
                        if (cfg.category.Value is BuildPieceCategory.Custom)
                        {
                            piecePrefab.m_category = PiecePrefabManager.GetCategory(cfg.customCategory.Value);
                        }
                        else
                        {
                            piecePrefab.m_category = (Piece.PieceCategory)cfg.category.Value;
                        }

                        if (Hud.instance)
                        {
                            PiecePrefabManager.CreateCategoryTabs();
                        }
                    }

                    customTableAttributes.Browsable = cfg.category.Value == BuildPieceCategory.Custom;
                    ReloadConfigDisplay();
                }

                cfg.category.SettingChanged += BuildTableConfigChanged;
                cfg.customCategory.SettingChanged += BuildTableConfigChanged;

                if (cfg.category.Value is BuildPieceCategory.Custom)
                {
                    piecePrefab.m_category = PiecePrefabManager.GetCategory(cfg.customCategory.Value);
                }
                else
                {
                    piecePrefab.m_category = (Piece.PieceCategory)cfg.category.Value;
                }

                cfg.tools = config(englishName, "Tools",
                    string.Join(", ", piece.activeTools),
                    new ConfigDescription($"Comma separated list of tools where {localizedName} is available.", null, customTableAttributes));
                piece.activeTools = cfg.tools.Value.Split(',').Select(s => s.Trim()).ToArray();
                cfg.tools.SettingChanged += (_, _) =>
                {
                    Inventory[] inventories = Player.s_players.Select(p => p.GetInventory()).Concat(Object.FindObjectsOfType<Container>().Select(c => c.GetInventory())).Where(c => c is not null).ToArray();
                    Dictionary<string, List<PieceTable>> tools = ObjectDB.instance.m_items.Select(p => p.GetComponent<ItemDrop>()).Where(c => c && c.GetComponent<ZNetView>()).Concat(ItemDrop.s_instances).Select(i => new KeyValuePair<string, ItemDrop.ItemData>(Utils.GetPrefabName(i.gameObject), i.m_itemData)).Concat(inventories.SelectMany(i => i.GetAllItems()).Select(i => new KeyValuePair<string, ItemDrop.ItemData>(i.m_dropPrefab.name, i))).Where(kv => kv.Value.m_shared.m_buildPieces).GroupBy(kv => kv.Key).ToDictionary(g => g.Key, g => g.Select(kv => kv.Value.m_shared.m_buildPieces).Distinct().ToList());

                    foreach (string tool in piece.activeTools)
                    {
                        if (tools.TryGetValue(tool, out List<PieceTable> existingTools))
                        {
                            foreach (PieceTable table in existingTools)
                            {
                                table.m_pieces.Remove(piece.Prefab);
                            }
                        }
                    }

                    piece.activeTools = cfg.tools.Value.Split(',').Select(s => s.Trim()).ToArray();
                    if (ObjectDB.instance)
                    {
                        foreach (string tool in piece.activeTools)
                        {
                            if (tools.TryGetValue(tool, out List<PieceTable> existingTools))
                            {
                                foreach (PieceTable table in existingTools)
                                {
                                    if (!table.m_pieces.Contains(piece.Prefab))
                                    {
                                        table.m_pieces.Add(piece.Prefab);
                                    }
                                }
                            }
                        }

                        if (Player.m_localPlayer && Player.m_localPlayer.m_buildPieces)
                        {
                            Player.m_localPlayer.SetPlaceMode(Player.m_localPlayer.m_buildPieces);
                        }
                    }
                };

                if (piece.Extension.ExtensionStations.Count > 0)
                {
                    StationExtension pieceExtensionComp = piece.Prefab.GetOrAddComponent<StationExtension>();
                    cfg.extensionTable = config(englishName, "Extends Station",
                        piece.Extension.ExtensionStations.First().Table,
                        new ConfigDescription($"Crafting station that {localizedName} extends.", null,
                            new ConfigurationManagerAttributes { Order = --order }));
                    cfg.customExtentionTable = config(englishName, "Custom Extend Station",
                        piece.Extension.ExtensionStations.First().custom ?? "",
                        new ConfigDescription("", null, customTableAttributes));
                    cfg.maxStationDistance = config(englishName, "Max Station Distance",
                        piece.Extension.ExtensionStations.First().maxStationDistance,
                        new ConfigDescription($"Distance from the station that {localizedName} can be placed.", null,
                            new ConfigurationManagerAttributes { Order = --order }));
                    List<ConfigurationManagerAttributes> hideWhenNoneAttributes = new();

                    void ExtensionTableConfigChanged(object o, EventArgs e)
                    {
                        if (piece.RequiredItems.Requirements.Count > 0)
                        {
                            switch (cfg.extensionTable.Value)
                            {
                                case CraftingTable.Custom:
                                    pieceExtensionComp.m_craftingStation = ZNetScene.instance
                                        .GetPrefab(cfg.customExtentionTable.Value)?.GetComponent<CraftingStation>();
                                    break;
                                default:
                                    pieceExtensionComp.m_craftingStation = ZNetScene.instance
                                        .GetPrefab(
                                            ((InternalName)typeof(CraftingTable).GetMember(cfg.extensionTable.Value
                                                .ToString())[0].GetCustomAttributes(typeof(InternalName)).First())
                                            .internalName).GetComponent<CraftingStation>();
                                    break;
                            }

                            pieceExtensionComp.m_maxStationDistance = cfg.maxStationDistance.Value;
                        }

                        customTableAttributes.Browsable = cfg.extensionTable.Value == CraftingTable.Custom;
                        foreach (ConfigurationManagerAttributes attributes in hideWhenNoneAttributes)
                        {
                            attributes.Browsable = cfg.extensionTable.Value != CraftingTable.None;
                        }

                        ReloadConfigDisplay();
                        plugin.Config.Save();
                    }

                    cfg.extensionTable.SettingChanged += ExtensionTableConfigChanged;
                    cfg.customExtentionTable.SettingChanged += ExtensionTableConfigChanged;
                    cfg.maxStationDistance.SettingChanged += ExtensionTableConfigChanged;

                    ConfigurationManagerAttributes tableLevelAttributes = new()
                        { Order = --order, Browsable = cfg.extensionTable.Value != CraftingTable.None };
                    hideWhenNoneAttributes.Add(tableLevelAttributes);
                }

                if (piece.Crafting.Stations.Count > 0)
                {
                    List<ConfigurationManagerAttributes> hideWhenNoneAttributes = new();

                    cfg.table = config(englishName, "Crafting Station", piece.Crafting.Stations.First().Table,
                        new ConfigDescription($"Crafting station where {localizedName} is available.", null,
                            new ConfigurationManagerAttributes { Order = --order }));
                    cfg.customTable = config(englishName, "Custom Crafting Station",
                        piece.Crafting.Stations.First().custom ?? "",
                        new ConfigDescription("", null, customTableAttributes));

                    void TableConfigChanged(object o, EventArgs e)
                    {
                        if (piece.RequiredItems.Requirements.Count > 0)
                        {
                            switch (cfg.table.Value)
                            {
                                case CraftingTable.None:
                                    piecePrefab.m_craftingStation = null;
                                    break;
                                case CraftingTable.Custom:
                                    piecePrefab.m_craftingStation = ZNetScene.instance.GetPrefab(cfg.customTable.Value)
                                        ?.GetComponent<CraftingStation>();
                                    break;
                                default:
                                    piecePrefab.m_craftingStation = ZNetScene.instance
                                        .GetPrefab(
                                            ((InternalName)typeof(CraftingTable).GetMember(cfg.table.Value.ToString())
                                                [0].GetCustomAttributes(typeof(InternalName)).First()).internalName)
                                        .GetComponent<CraftingStation>();
                                    break;
                            }
                        }

                        customTableAttributes.Browsable = cfg.table.Value == CraftingTable.Custom;
                        foreach (ConfigurationManagerAttributes attributes in hideWhenNoneAttributes)
                        {
                            attributes.Browsable = cfg.table.Value != CraftingTable.None;
                        }

                        ReloadConfigDisplay();
                        plugin.Config.Save();
                    }

                    cfg.table.SettingChanged += TableConfigChanged;
                    cfg.customTable.SettingChanged += TableConfigChanged;

                    ConfigurationManagerAttributes tableLevelAttributes = new()
                        { Order = --order, Browsable = cfg.table.Value != CraftingTable.None };
                    hideWhenNoneAttributes.Add(tableLevelAttributes);
                }

                ConfigEntry<string> itemConfig(string name, string value, string desc)
                {
                    ConfigurationManagerAttributes attributes = new()
                        { CustomDrawer = DrawConfigTable, Order = --order, Category = localizedName };
                    return config(englishName, name, value, new ConfigDescription(desc, null, attributes));
                }

                cfg.craft = itemConfig("Crafting Costs",
                    new SerializedRequirements(piece.RequiredItems.Requirements).ToString(),
                    $"Item costs to craft {localizedName}");
                cfg.craft.SettingChanged += (_, _) =>
                {
                    if (ObjectDB.instance && ObjectDB.instance.GetItemPrefab("YagluthDrop") != null)
                    {
                        Piece.Requirement[] requirements =
                            SerializedRequirements.toPieceReqs(new SerializedRequirements(cfg.craft.Value));
                        piecePrefab.m_resources = requirements;
                        foreach (Piece instantiatedPiece in Object.FindObjectsOfType<Piece>())
                        {
                            if (instantiatedPiece.m_name == pieceName)
                            {
                                instantiatedPiece.m_resources = requirements;
                            }
                        }
                    }
                };

                for (int i = 0; i < piece.Conversions.Count; ++i)
                {
                    string prefix = piece.Conversions.Count > 1 ? $"{i + 1}. " : "";
                    Conversion conversion = piece.Conversions[i];
                    conversion.config = new Conversion.ConversionConfig();
                    int index = i;

                    conversion.config.input = config(englishName, $"{prefix}Conversion Input Item", conversion.Input, new ConfigDescription($"Conversion input item within {englishName}", null, new ConfigurationManagerAttributes { Category = localizedName }));
                    conversion.config.input.SettingChanged += (_, _) =>
                    {
                        if (index < piece.conversions.Count && ObjectDB.instance is { } objectDB)
                        {
                            ItemDrop? inputItem = SerializedRequirements.fetchByName(objectDB, conversion.config.input.Value);
                            piece.conversions[index].m_from = inputItem;
                        }
                    };
                    conversion.config.output = config(englishName, $"{prefix}Conversion Output Item", conversion.Output, new ConfigDescription($"Conversion output item within {englishName}", null, new ConfigurationManagerAttributes { Category = localizedName }));
                    conversion.config.output.SettingChanged += (_, _) =>
                    {
                        if (index < piece.conversions.Count && ObjectDB.instance is { } objectDB)
                        {
                            ItemDrop? outputItem = SerializedRequirements.fetchByName(objectDB, conversion.config.output.Value);
                            piece.conversions[index].m_to = outputItem;
                        }
                    };
                }

                if (SaveOnConfigSet)
                {
                    plugin.Config.SaveOnConfigSet = true;
                    plugin.Config.Save();
                }
            }

            foreach (BuildPiece piece in registeredPieces)
            {
                if (piece.RecipeIsActive is { } enabledCfg)
                {
                    Piece piecePrefab = piece.Prefab.GetComponent<Piece>();
                    void ConfigChanged(object? o, EventArgs? e) => piecePrefab.m_enabled = (int)enabledCfg.BoxedValue != 0;
                    ConfigChanged(null, null);
                    enabledCfg.GetType().GetEvent(nameof(ConfigEntry<int>.SettingChanged)).AddEventHandler(enabledCfg, new EventHandler(ConfigChanged));
                }
            }
        }
    }

    [HarmonyPriority(Priority.VeryHigh)]
    internal static void Patch_ObjectDBInit(ObjectDB __instance)
    {
        if (__instance.GetItemPrefab("YagluthDrop") == null)
        {
            return;
        }

        foreach (BuildPiece piece in registeredPieces)
        {
            pieceConfigs.TryGetValue(piece, out PieceConfig? cfg);
            piece.Prefab.GetComponent<Piece>().m_resources = SerializedRequirements.toPieceReqs(cfg == null
                ? new SerializedRequirements(piece.RequiredItems.Requirements)
                : new SerializedRequirements(cfg.craft.Value));
            foreach (ExtensionConfig station in piece.Extension.ExtensionStations)
            {
                switch ((cfg == null || piece.Extension.ExtensionStations.Count > 0
                            ? station.Table
                            : cfg.extensionTable.Value))
                {
                    case CraftingTable.None:
                        piece.Prefab.GetComponent<StationExtension>().m_craftingStation = null;
                        break;
                    case CraftingTable.Custom
                        when ZNetScene.instance.GetPrefab(cfg == null || piece.Extension.ExtensionStations.Count > 0
                            ? station.custom
                            : cfg.customExtentionTable.Value) is { } craftingTable:
                        piece.Prefab.GetComponent<StationExtension>().m_craftingStation =
                            craftingTable.GetComponent<CraftingStation>();
                        break;
                    case CraftingTable.Custom:
                        Debug.LogWarning(
                            $"Custom crafting station '{(cfg == null || piece.Extension.ExtensionStations.Count > 0 ? station.custom : cfg.customExtentionTable.Value)}' does not exist");
                        break;
                    default:
                    {
                        if (cfg != null && cfg.table.Value == CraftingTable.None)
                        {
                            piece.Prefab.GetComponent<StationExtension>().m_craftingStation = null;
                        }
                        else
                        {
                            piece.Prefab.GetComponent<StationExtension>().m_craftingStation = ZNetScene.instance
                                .GetPrefab(((InternalName)typeof(CraftingTable).GetMember(
                                    (cfg == null || piece.Extension.ExtensionStations.Count > 0
                                        ? station.Table
                                        : cfg.extensionTable.Value)
                                    .ToString())[0].GetCustomAttributes(typeof(InternalName)).First()).internalName)
                                .GetComponent<CraftingStation>();
                        }

                        break;
                    }
                }
            }

            foreach (CraftingStationConfig station in piece.Crafting.Stations)
            {
                switch ((cfg == null || piece.Crafting.Stations.Count > 0 ? station.Table : cfg.table.Value))
                {
                    case CraftingTable.None:
                        piece.Prefab.GetComponent<Piece>().m_craftingStation = null;
                        break;
                    case CraftingTable.Custom
                        when ZNetScene.instance.GetPrefab(cfg == null || piece.Crafting.Stations.Count > 0
                            ? station.custom
                            : cfg.customTable.Value) is { } craftingTable:
                        piece.Prefab.GetComponent<Piece>().m_craftingStation =
                            craftingTable.GetComponent<CraftingStation>();
                        break;
                    case CraftingTable.Custom:
                        Debug.LogWarning(
                            $"Custom crafting station '{(cfg == null || piece.Crafting.Stations.Count > 0 ? station.custom : cfg.customTable.Value)}' does not exist");
                        break;
                    default:
                    {
                        if (cfg != null && cfg.table.Value == CraftingTable.None)
                        {
                            piece.Prefab.GetComponent<Piece>().m_craftingStation = null;
                        }
                        else
                        {
                            piece.Prefab.GetComponent<Piece>().m_craftingStation = ZNetScene.instance
                                .GetPrefab(((InternalName)typeof(CraftingTable).GetMember(
                                    (cfg == null || piece.Crafting.Stations.Count > 0 ? station.Table : cfg.table.Value)
                                    .ToString())[0].GetCustomAttributes(typeof(InternalName)).First()).internalName)
                                .GetComponent<CraftingStation>();
                        }

                        break;
                    }
                }
            }
            piece.conversions = new List<Smelter.ItemConversion>();
            for (int i = 0; i < piece.Conversions.Count; ++i)
            {
                Conversion conversion = piece.Conversions[i];
                piece.conversions.Add(new Smelter.ItemConversion
                {
                    m_from = SerializedRequirements.fetchByName(ObjectDB.instance, conversion.config?.input.Value ?? conversion.Input),
                    m_to = SerializedRequirements.fetchByName(ObjectDB.instance, conversion.config?.output.Value ?? conversion.Output),
                });
                if (piece.conversions[i].m_from is not null && piece.conversions[i].m_to is not null)
                {
                    piece.Prefab.GetComponent<Smelter>().m_conversion.Add(piece.conversions[i]);
                }
            }
        }
    }

    public void Snapshot(float lightIntensity = 1.3f, Quaternion? cameraRotation = null) => SnapshotPiece(Prefab, lightIntensity, cameraRotation);

    internal void SnapshotPiece(GameObject prefab, float lightIntensity = 1.3f, Quaternion? cameraRotation = null)
    {
        const int layer = 3;
        if (prefab == null) return;
        if (!prefab.GetComponentsInChildren<Renderer>().Any() && !prefab.GetComponentsInChildren<MeshFilter>().Any())
        {
            return;
        }

        Camera camera = new GameObject("CameraIcon", typeof(Camera)).GetComponent<Camera>();
        camera.backgroundColor = Color.clear;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(10000f, 10000f, 10000f);
        camera.transform.rotation = cameraRotation ?? Quaternion.Euler(0f, 180f, 0f);
        camera.fieldOfView = 0.5f;
        camera.farClipPlane = 100000;
        camera.cullingMask = 1 << layer;

        Light sideLight = new GameObject("LightIcon", typeof(Light)).GetComponent<Light>();
        sideLight.transform.position = new Vector3(10000f, 10000f, 10000f);
        sideLight.transform.rotation = Quaternion.Euler(5f, 180f, 5f);
        sideLight.type = LightType.Directional;
        sideLight.cullingMask = 1 << layer;
        sideLight.intensity = lightIntensity;

        GameObject visual = Object.Instantiate(prefab);
        foreach (Transform child in visual.GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = layer;
        }

        visual.transform.position = Vector3.zero;
        visual.transform.rotation = Quaternion.Euler(23, 51, 25.8f);
        visual.name = prefab.name;

        MeshRenderer[] renderers = visual.GetComponentsInChildren<MeshRenderer>();
        Vector3 min = renderers.Aggregate(Vector3.positiveInfinity,
            (cur, renderer) => Vector3.Min(cur, renderer.bounds.min));
        Vector3 max = renderers.Aggregate(Vector3.negativeInfinity,
            (cur, renderer) => Vector3.Max(cur, renderer.bounds.max));
        // center the prefab
        visual.transform.position = (new Vector3(10000f, 10000f, 10000f)) - (min + max) / 2f;
        Vector3 size = max - min;

        // just in case it doesn't gets deleted properly later
        TimedDestruction timedDestruction = visual.AddComponent<TimedDestruction>();
        timedDestruction.Trigger(1f);
        Rect rect = new(0, 0, 128, 128);
        camera.targetTexture = RenderTexture.GetTemporary((int)rect.width, (int)rect.height);

        camera.fieldOfView = 20f;
        // calculate the Z position of the prefab as it needs to be far away from the camera
        float maxMeshSize = Mathf.Max(size.x, size.y) + 0.1f;
        float distance = maxMeshSize / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad) * 1.1f;

        camera.transform.position = (new Vector3(10000f, 10000f, 10000f)) + new Vector3(0, 0, distance);

        camera.Render();

        RenderTexture currentRenderTexture = RenderTexture.active;
        RenderTexture.active = camera.targetTexture;

        Texture2D previewImage = new((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
        previewImage.ReadPixels(new Rect(0, 0, (int)rect.width, (int)rect.height), 0, 0);
        previewImage.Apply();

        RenderTexture.active = currentRenderTexture;

        prefab.GetComponent<Piece>().m_icon = Sprite.Create(previewImage, new Rect(0, 0, (int)rect.width, (int)rect.height), Vector2.one / 2f);
        sideLight.gameObject.SetActive(false);
        camera.targetTexture.Release();
        camera.gameObject.SetActive(false);
        visual.SetActive(false);
        Object.DestroyImmediate(visual);

        Object.Destroy(camera);
        Object.Destroy(sideLight);
        Object.Destroy(camera.gameObject);
        Object.Destroy(sideLight.gameObject);
    }

    private static void DrawConfigTable(ConfigEntryBase cfg)
    {
        bool locked = cfg.Description.Tags
            .Select(a =>
                a.GetType().Name == "ConfigurationManagerAttributes"
                    ? (bool?)a.GetType().GetField("ReadOnly")?.GetValue(a)
                    : null).FirstOrDefault(v => v != null) ?? false;

        List<Requirement> newReqs = new();
        bool wasUpdated = false;

        int RightColumnWidth =
            (int)(configManager?.GetType()
                .GetProperty("RightColumnWidth", BindingFlags.Instance | BindingFlags.NonPublic)!.GetGetMethod(true)
                .Invoke(configManager, Array.Empty<object>()) ?? 130);

        GUILayout.BeginVertical();
        foreach (Requirement req in new SerializedRequirements((string)cfg.BoxedValue).Reqs)
        {
            GUILayout.BeginHorizontal();

            int amount = req.amount;
            if (int.TryParse(
                    GUILayout.TextField(amount.ToString(), new GUIStyle(GUI.skin.textField) { fixedWidth = 40 }),
                    out int newAmount) && newAmount != amount && !locked)
            {
                amount = newAmount;
                wasUpdated = true;
            }

            string newItemName = GUILayout.TextField(req.itemName,
                new GUIStyle(GUI.skin.textField) { fixedWidth = RightColumnWidth - 40 - 67 - 21 - 21 - 12 });
            string itemName = locked ? req.itemName : newItemName;
            wasUpdated = wasUpdated || itemName != req.itemName;

            bool recover = req.recover;
            if (GUILayout.Toggle(req.recover, "Recover", new GUIStyle(GUI.skin.toggle) { fixedWidth = 67 }) !=
                req.recover)
            {
                recover = !recover;
                wasUpdated = true;
            }

            if (GUILayout.Button("x", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }) && !locked)
            {
                wasUpdated = true;
            }
            else
            {
                newReqs.Add(new Requirement { amount = amount, itemName = itemName, recover = recover });
            }

            if (GUILayout.Button("+", new GUIStyle(GUI.skin.button) { fixedWidth = 21 }) && !locked)
            {
                wasUpdated = true;
                newReqs.Add(new Requirement { amount = 1, itemName = "", recover = false });
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();

        if (wasUpdated)
        {
            cfg.BoxedValue = new SerializedRequirements(newReqs).ToString();
        }
    }

    private class SerializedRequirements
    {
        public readonly List<Requirement> Reqs;

        public SerializedRequirements(List<Requirement> reqs) => Reqs = reqs;

        public SerializedRequirements(string reqs)
        {
            Reqs = reqs.Split(',').Select(r =>
            {
                string[] parts = r.Split(':');
                return new Requirement
                {
                    itemName = parts[0],
                    amount = parts.Length > 1 && int.TryParse(parts[1], out int amount) ? amount : 1,
                    recover = parts.Length <= 2 || !bool.TryParse(parts[2], out bool recover) || recover,
                };
            }).ToList();
        }

        public override string ToString()
        {
            return string.Join(",", Reqs.Select(r => $"{r.itemName}:{r.amount}:{r.recover}"));
        }

        public static ItemDrop? fetchByName(ObjectDB objectDB, string name)
        {
            ItemDrop? item = objectDB.GetItemPrefab(name)?.GetComponent<ItemDrop>();
            if (item == null)
            {
                Debug.LogWarning($"{(!string.IsNullOrWhiteSpace(plugin.name) ? $"[{plugin.name}]" : "")} The required item '{name}' does not exist.");
            }
            return item;
        }

        public static Piece.Requirement[] toPieceReqs(SerializedRequirements craft)
        {
            ItemDrop? ResItem(Requirement r) => fetchByName(ObjectDB.instance, r.itemName);

            Dictionary<string, Piece.Requirement?> resources = craft.Reqs.Where(r => r.itemName != "")
                .ToDictionary(r => r.itemName,
                    r => ResItem(r) is { } item
                        ? new Piece.Requirement { m_amount = r.amount, m_resItem = item, m_recover = r.recover }
                        : null);

            return resources.Values.Where(v => v != null).ToArray()!;
        }
    }

    private static Localization? _english;

    private static Localization english => _english ??= LocalizationCache.ForLanguage("English");

    internal static BaseUnityPlugin? _plugin = null!;

    internal static BaseUnityPlugin plugin
    {
        get
        {
            if (_plugin is not null) return _plugin;
            IEnumerable<TypeInfo> types;
            try
            {
                types = Assembly.GetExecutingAssembly().DefinedTypes.ToList();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).Select(t => t.GetTypeInfo());
            }

            _plugin = (BaseUnityPlugin)BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(types.First(t =>
                t.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(t)));

            return _plugin;
        }
    }

    private static bool hasConfigSync = true;
    private static object? _configSync;

    private static object? configSync
    {
        get
        {
            if (_configSync != null || !hasConfigSync) return _configSync;
            if (Assembly.GetExecutingAssembly().GetType("ServerSync.ConfigSync") is { } configSyncType)
            {
                _configSync = Activator.CreateInstance(configSyncType, plugin.Info.Metadata.GUID + " PieceManager");
                configSyncType.GetField("CurrentVersion")
                    .SetValue(_configSync, plugin.Info.Metadata.Version.ToString());
                configSyncType.GetProperty("IsLocked")!.SetValue(_configSync, true);
            }
            else
            {
                hasConfigSync = false;
            }

            return _configSync;
        }
    }

    private static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description)
    {
        ConfigEntry<T> configEntry = plugin.Config.Bind(group, name, value, description);

        configSync?.GetType().GetMethod("AddConfigEntry")!.MakeGenericMethod(typeof(T))
            .Invoke(configSync, new object[] { configEntry });

        return configEntry;
    }

    private static ConfigEntry<T> config<T>(string group, string name, T value, string description) =>
        config(group, name, value, new ConfigDescription(description));
}

public static class GoExtensions
{
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : UnityEngine.Component =>
        gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
}

[PublicAPI]
public class LocalizeKey
{
    private static readonly List<LocalizeKey> keys = new();

    public readonly string Key;
    public readonly Dictionary<string, string> Localizations = new();

    public LocalizeKey(string key)
    {
        Key = key.Replace("$", "");
        keys.Add(this);
    }

    public void Alias(string alias)
    {
        Localizations.Clear();
        if (!alias.Contains("$"))
        {
            alias = $"${alias}";
        }

        Localizations["alias"] = alias;
        Localization.instance.AddWord(Key, Localization.instance.Localize(alias));
    }

    public LocalizeKey English(string key) => addForLang("English", key);
    public LocalizeKey Swedish(string key) => addForLang("Swedish", key);
    public LocalizeKey French(string key) => addForLang("French", key);
    public LocalizeKey Italian(string key) => addForLang("Italian", key);
    public LocalizeKey German(string key) => addForLang("German", key);
    public LocalizeKey Spanish(string key) => addForLang("Spanish", key);
    public LocalizeKey Russian(string key) => addForLang("Russian", key);
    public LocalizeKey Romanian(string key) => addForLang("Romanian", key);
    public LocalizeKey Bulgarian(string key) => addForLang("Bulgarian", key);
    public LocalizeKey Macedonian(string key) => addForLang("Macedonian", key);
    public LocalizeKey Finnish(string key) => addForLang("Finnish", key);
    public LocalizeKey Danish(string key) => addForLang("Danish", key);
    public LocalizeKey Norwegian(string key) => addForLang("Norwegian", key);
    public LocalizeKey Icelandic(string key) => addForLang("Icelandic", key);
    public LocalizeKey Turkish(string key) => addForLang("Turkish", key);
    public LocalizeKey Lithuanian(string key) => addForLang("Lithuanian", key);
    public LocalizeKey Czech(string key) => addForLang("Czech", key);
    public LocalizeKey Hungarian(string key) => addForLang("Hungarian", key);
    public LocalizeKey Slovak(string key) => addForLang("Slovak", key);
    public LocalizeKey Polish(string key) => addForLang("Polish", key);
    public LocalizeKey Dutch(string key) => addForLang("Dutch", key);
    public LocalizeKey Portuguese_European(string key) => addForLang("Portuguese_European", key);
    public LocalizeKey Portuguese_Brazilian(string key) => addForLang("Portuguese_Brazilian", key);
    public LocalizeKey Chinese(string key) => addForLang("Chinese", key);
    public LocalizeKey Japanese(string key) => addForLang("Japanese", key);
    public LocalizeKey Korean(string key) => addForLang("Korean", key);
    public LocalizeKey Hindi(string key) => addForLang("Hindi", key);
    public LocalizeKey Thai(string key) => addForLang("Thai", key);
    public LocalizeKey Abenaki(string key) => addForLang("Abenaki", key);
    public LocalizeKey Croatian(string key) => addForLang("Croatian", key);
    public LocalizeKey Georgian(string key) => addForLang("Georgian", key);
    public LocalizeKey Greek(string key) => addForLang("Greek", key);
    public LocalizeKey Serbian(string key) => addForLang("Serbian", key);
    public LocalizeKey Ukrainian(string key) => addForLang("Ukrainian", key);

    private LocalizeKey addForLang(string lang, string value)
    {
        Localizations[lang] = value;
        if (Localization.instance.GetSelectedLanguage() == lang)
        {
            Localization.instance.AddWord(Key, value);
        }
        else if (lang == "English" && !Localization.instance.m_translations.ContainsKey(Key))
        {
            Localization.instance.AddWord(Key, value);
        }

        return this;
    }

    [HarmonyPriority(Priority.LowerThanNormal)]
    internal static void AddLocalizedKeys(Localization __instance, string language)
    {
        foreach (LocalizeKey key in keys)
        {
            if (key.Localizations.TryGetValue(language, out string Translation) ||
                key.Localizations.TryGetValue("English", out Translation))
            {
                __instance.AddWord(key.Key, Translation);
            }
            else if (key.Localizations.TryGetValue("alias", out string alias))
            {
                __instance.AddWord(key.Key, Localization.instance.Localize(alias));
            }
        }
    }
}

public static class LocalizationCache
{
    private static readonly Dictionary<string, Localization> localizations = new();

    internal static void LocalizationPostfix(Localization __instance, string language)
    {
        if (localizations.FirstOrDefault(l => l.Value == __instance).Key is { } oldValue)
        {
            localizations.Remove(oldValue);
        }

        if (!localizations.ContainsKey(language))
        {
            localizations.Add(language, __instance);
        }
    }

    public static Localization ForLanguage(string? language = null)
    {
        if (localizations.TryGetValue(language ?? PlayerPrefs.GetString("language", "English"),
                out Localization localization))
        {
            return localization;
        }

        localization = new Localization();
        if (language is not null)
        {
            localization.SetupLanguage(language);
        }

        return localization;
    }
}

public class AdminSyncing
{
    private static bool isServer;
    internal static bool registeredOnClient;

    [HarmonyPriority(Priority.VeryHigh)]
    internal static void AdminStatusSync(ZNet __instance)
    {
        isServer = __instance.IsServer();
        if (BuildPiece._plugin is not null)
        {
            if (isServer)
            {
                ZRoutedRpc.instance.Register<ZPackage>(BuildPiece._plugin.Info.Metadata.Name + " PMAdminStatusSync", RPC_AdminPieceAddRemove);
            }
            else if (!registeredOnClient)
            {
                ZRoutedRpc.instance.Register<ZPackage>(BuildPiece._plugin.Info.Metadata.Name + " PMAdminStatusSync", RPC_AdminPieceAddRemove);
                registeredOnClient = true;
            }
        }

        IEnumerator WatchAdminListChanges()
        {
            List<string> currentList = new(ZNet.instance.m_adminList.GetList());
            for (;;)
            {
                yield return new WaitForSeconds(30);
                if (!ZNet.instance.m_adminList.GetList().SequenceEqual(currentList))
                {
                    currentList = new List<string>(ZNet.instance.m_adminList.GetList());
                    List<ZNetPeer> adminPeer = ZNet.instance.GetPeers().Where(p =>
                            ZNet.instance.ListContainsId(ZNet.instance.m_adminList, p.m_rpc.GetSocket().GetHostName()))
                        .ToList();
                    List<ZNetPeer> nonAdminPeer = ZNet.instance.GetPeers().Except(adminPeer).ToList();
                    SendAdmin(nonAdminPeer, false);
                    SendAdmin(adminPeer, true);

                    void SendAdmin(List<ZNetPeer> peers, bool isAdmin)
                    {
                        ZPackage package = new();
                        package.Write(isAdmin);
                        ZNet.instance.StartCoroutine(sendZPackage(peers, package));
                    }
                }
            }
            // ReSharper disable once IteratorNeverReturns
        }

        if (isServer)
        {
            ZNet.instance.StartCoroutine(WatchAdminListChanges());
        }
    }

    private static IEnumerator sendZPackage(List<ZNetPeer> peers, ZPackage package)
    {
        if (!ZNet.instance)
        {
            yield break;
        }

        const int compressMinSize = 10000;

        if (package.GetArray() is { LongLength: > compressMinSize } rawData)
        {
            ZPackage compressedPackage = new();
            compressedPackage.Write(4);
            MemoryStream output = new();
            using (DeflateStream deflateStream = new(output, System.IO.Compression.CompressionLevel.Optimal))
            {
                deflateStream.Write(rawData, 0, rawData.Length);
            }

            compressedPackage.Write(output.ToArray());
            package = compressedPackage;
        }

        List<IEnumerator<bool>> writers =
            peers.Where(peer => peer.IsReady()).Select(p => TellPeerAdminStatus(p, package)).ToList();
        writers.RemoveAll(writer => !writer.MoveNext());
        while (writers.Count > 0)
        {
            yield return null;
            writers.RemoveAll(writer => !writer.MoveNext());
        }
    }

    private static IEnumerator<bool> TellPeerAdminStatus(ZNetPeer peer, ZPackage package)
    {
        if (ZRoutedRpc.instance is not { } rpc)
        {
            yield break;
        }

        SendPackage(package);

        void SendPackage(ZPackage pkg)
        {
            string method = BuildPiece._plugin?.Info.Metadata.Name + " PMAdminStatusSync";
            if (isServer)
            {
                peer.m_rpc.Invoke(method, pkg);
            }
            else
            {
                rpc.InvokeRoutedRPC(peer.m_server ? 0 : peer.m_uid, method, pkg);
            }
        }
    }

    internal static void RPC_AdminPieceAddRemove(long sender, ZPackage package)
    {
        ZNetPeer? currentPeer = ZNet.instance.GetPeer(sender);
        bool admin = false;
        try
        {
            admin = package.ReadBool();
        }
        catch
        {
            // ignore
        }

        if (isServer)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody,
                BuildPiece._plugin?.Info.Metadata.Name + " PMAdminStatusSync", new ZPackage());
            if (ZNet.instance.ListContainsId(ZNet.instance.m_adminList, currentPeer.m_rpc.GetSocket().GetHostName()))
            {
                ZPackage pkg = new();
                pkg.Write(true);
                currentPeer.m_rpc.Invoke(BuildPiece._plugin?.Info.Metadata.Name + " PMAdminStatusSync", pkg);
            }
        }
        else
        {
            // Remove everything they shouldn't be able to build by disabling and removing.
            foreach (BuildPiece piece in BuildPiece.registeredPieces)
            {
                if (!piece.SpecialProperties.AdminOnly) continue;
                Piece piecePrefab = piece.Prefab.GetComponent<Piece>();
                string pieceName = piecePrefab.m_name;
                string localizedName = Localization.instance.Localize(pieceName).Trim();
                if (!ObjectDB.instance || ObjectDB.instance.GetItemPrefab("YagluthDrop") == null) continue;
                foreach (Piece instantiatedPiece in Object.FindObjectsOfType<Piece>())
                {
                    if (admin)
                    {
                        if (instantiatedPiece.m_name == pieceName)
                        {
                            instantiatedPiece.m_enabled = true;
                        }
                    }
                    else
                    {
                        if (instantiatedPiece.m_name == pieceName)
                        {
                            instantiatedPiece.m_enabled = false;
                        }
                    }
                }

                List<GameObject>? hammerPieces = ObjectDB.instance.GetItemPrefab("Hammer").GetComponent<ItemDrop>()
                    .m_itemData.m_shared.m_buildPieces
                    .m_pieces;
                if (admin)
                {
                    if (!hammerPieces.Contains(ZNetScene.instance.GetPrefab(piecePrefab.name)))
                        hammerPieces.Add(ZNetScene.instance.GetPrefab(piecePrefab.name));
                }
                else
                {
                    if (hammerPieces.Contains(ZNetScene.instance.GetPrefab(piecePrefab.name)))
                        hammerPieces.Remove(ZNetScene.instance.GetPrefab(piecePrefab.name));
                }
            }
        }
    }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
class RegisterClientRPCPatch
{
    private static void Postfix(ZNet __instance, ZNetPeer peer)
    {
        if (!__instance.IsServer())
        {
            peer.m_rpc.Register<ZPackage>(BuildPiece._plugin?.Info.Metadata.Name + " PMAdminStatusSync",
                RPC_InitialAdminSync);
        }
        else
        {
            ZPackage packge = new();
            packge.Write(__instance.ListContainsId(__instance.m_adminList, peer.m_rpc.GetSocket().GetHostName()));

            peer.m_rpc.Invoke(BuildPiece._plugin?.Info.Metadata.Name + " PMAdminStatusSync", packge);
        }
    }

    private static void RPC_InitialAdminSync(ZRpc rpc, ZPackage package) =>
        AdminSyncing.RPC_AdminPieceAddRemove(0, package);
}

public static class PiecePrefabManager
{
    static PiecePrefabManager()
    {
        Harmony harmony = new("org.bepinex.helpers.PieceManager");
        harmony.Patch(AccessTools.DeclaredMethod(typeof(FejdStartup), nameof(FejdStartup.Awake)), new HarmonyMethod(AccessTools.DeclaredMethod(typeof(BuildPiece), nameof(BuildPiece.Patch_FejdStartup))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Localization), nameof(Localization.LoadCSV)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(LocalizeKey), nameof(LocalizeKey.AddLocalizedKeys))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Localization), nameof(Localization.SetupLanguage)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(LocalizationCache), nameof(LocalizationCache.LocalizationPostfix))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(ObjectDB), nameof(ObjectDB.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(Patch_ObjectDBInit))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(ObjectDB), nameof(ObjectDB.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(BuildPiece), nameof(BuildPiece.Patch_ObjectDBInit))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(Patch_ObjectDBInit))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(ZNet), nameof(ZNet.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(AdminSyncing), nameof(AdminSyncing.AdminStatusSync))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(ZNetScene), nameof(ZNetScene.Awake)), new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(Patch_ZNetSceneAwake))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(ZNetScene), nameof(ZNetScene.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(RefFixPatch_ZNetSceneAwake))));

        harmony.Patch(AccessTools.DeclaredMethod(typeof(PieceTable), nameof(PieceTable.PrevCategory)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(Patch_PieceTable_PrevCategory))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(PieceTable), nameof(PieceTable.PrevCategory)), transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(PrevCategory_Transpiler))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(PieceTable), nameof(PieceTable.NextCategory)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(Patch_PieceTable_NextCategory))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(PieceTable), nameof(PieceTable.NextCategory)), transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(NextCategory_Transpiler))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(PieceTable), nameof(PieceTable.SetCategory)), transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(SetCategory_Transpiler))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(PieceTable), nameof(PieceTable.UpdateAvailable)), transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(UpdateAvailable_Transpiler))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(PieceTable), nameof(PieceTable.UpdateAvailable)), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(UpdateAvailable_Prefix))), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(UpdateAvailable_Postfix))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Player), nameof(Player.SetPlaceMode)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(Patch_SetPlaceMode))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Hud), nameof(Hud.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(Hud_AwakeCreateTabs))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Enum), nameof(Enum.GetValues)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(EnumGetValuesPatch))));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(Enum), nameof(Enum.GetNames)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(EnumGetNamesPatch))));
    }

    private struct BundleId
    {
        [UsedImplicitly] public string assetBundleFileName;
        [UsedImplicitly] public string folderName;
    }

    private static readonly Dictionary<BundleId, AssetBundle> bundleCache = new();

    public static AssetBundle RegisterAssetBundle(string assetBundleFileName, string folderName = "assets")
    {
        BundleId id = new() { assetBundleFileName = assetBundleFileName, folderName = folderName };
        if (!bundleCache.TryGetValue(id, out AssetBundle assets))
        {
            assets = bundleCache[id] =
                Resources.FindObjectsOfTypeAll<AssetBundle>().FirstOrDefault(a => a.name == assetBundleFileName) ??
                AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name + $".{folderName}." +
                                               assetBundleFileName));
        }

        return assets;
    }

    public static IEnumerable<GameObject> FixRefs(AssetBundle assetBundle)
    {
        GameObject[] allshits = assetBundle.LoadAllAssets<GameObject>();
        return allshits;
    }

    private static readonly List<GameObject> piecePrefabs = new();
    private static readonly Dictionary<string, Piece.PieceCategory> PieceCategories = new();
    private static readonly Dictionary<string, Piece.PieceCategory> OtherPieceCategories = new();
    private const string _hiddenCategoryMagic = "(HiddenCategory)";

    public static GameObject RegisterPrefab(
        string assetBundleFileName,
        string prefabName,
        string folderName = "assets") =>
        RegisterPrefab(RegisterAssetBundle(assetBundleFileName, folderName), prefabName);

    public static GameObject RegisterPrefab(AssetBundle assets, string prefabName)
    {
        GameObject prefab = assets.LoadAsset<GameObject>(prefabName);

        //foreach (GameObject gameObject in FixRefs(assets))
        //{
        //    MaterialReplacer.RegisterGameObjectForShaderSwap(gameObject, MaterialReplacer.ShaderType.UseUnityShader);
        //}

        piecePrefabs.Add(prefab);

        return prefab;
    }

    /* Sprites Only! */
    public static Sprite RegisterSprite(
        string assetBundleFileName,
        string prefabName,
        string folderName = "assets") =>
        RegisterSprite(RegisterAssetBundle(assetBundleFileName, folderName), prefabName);

    public static Sprite RegisterSprite(AssetBundle assets, string prefabName)
    {
        Sprite prefab = assets.LoadAsset<Sprite>(prefabName);
        return prefab;
    }

    private static void EnumGetValuesPatch(Type enumType, ref Array __result)
    {
        if (enumType != typeof(Piece.PieceCategory))
        {
            return;
        }

        if (PieceCategories.Count == 0)
        {
            return;
        }

        Piece.PieceCategory[] categories = new Piece.PieceCategory[__result.Length + PieceCategories.Count];

        __result.CopyTo(categories, 0);
        PieceCategories.Values.CopyTo(categories, __result.Length);

        __result = categories;
    }

    private static void EnumGetNamesPatch(Type enumType, ref string[] __result)
    {
        if (enumType != typeof(Piece.PieceCategory))
        {
            return;
        }

        if (PieceCategories.Count == 0)
        {
            return;
        }

        __result = __result.AddRangeToArray(PieceCategories.Keys.ToArray());
    }

    public static Dictionary<Piece.PieceCategory, string> GetPieceCategoriesMap()
    {
        Array values = Enum.GetValues(typeof(Piece.PieceCategory));
        string[] names = Enum.GetNames(typeof(Piece.PieceCategory));

        Dictionary<Piece.PieceCategory, string> map = new Dictionary<Piece.PieceCategory, string>();

        for (int i = 0; i < values.Length; i++)
        {
            map[(Piece.PieceCategory)values.GetValue(i)] = names[i];
        }

        return map;
    }

    public static Piece.PieceCategory GetCategory(string name)
    {
        if (Enum.TryParse(name, true, out Piece.PieceCategory category))
        {
            return category;
        }

        if (PieceCategories.TryGetValue(name, out category))
        {
            return category;
        }

        if (OtherPieceCategories.TryGetValue(name, out category))
        {
            return category;
        }

        Dictionary<Piece.PieceCategory, string> categories = GetPieceCategoriesMap();

        foreach (KeyValuePair<Piece.PieceCategory, string> categoryPair in categories)
        {
            if (categoryPair.Value == name)
            {
                category = categoryPair.Key;
                OtherPieceCategories[name] = category;
                return category;
            }
        }

        // create a new category
        category = (Piece.PieceCategory)categories.Count - 1;
        PieceCategories[name] = category;
        string tokenName = GetCategoryToken(name);
        Localization.instance.AddWord(tokenName, name);

        return category;
    }

    internal static void CreateCategoryTabs()
    {
        int maxCategory = MaxCategory();

        // Fill empty category names to prevent index issues, the correct names are set by the respective mods later
        for (int i = Hud.instance.m_buildCategoryNames.Count; i < maxCategory; ++i)
        {
            Hud.instance.m_buildCategoryNames.Add("");
        }

        // Append tabs and their names to the GUI for every custom category not already added
        for (int i = Hud.instance.m_pieceCategoryTabs.Length; i < maxCategory; ++i)
        {
            GameObject tab = CreateCategoryTab();
            Hud.instance.m_pieceCategoryTabs = Hud.instance.m_pieceCategoryTabs.AddItem(tab).ToArray();
        }

        if (Player.m_localPlayer && Player.m_localPlayer.m_buildPieces)
        {
            RepositionCategories(Player.m_localPlayer.m_buildPieces);
            Player.m_localPlayer.UpdateAvailablePiecesList();
        }
    }

    private static GameObject CreateCategoryTab()
    {
        Transform categoryRoot = Hud.instance.m_pieceCategoryRoot.transform;

        GameObject newTab = Object.Instantiate(Hud.instance.m_pieceCategoryTabs[0], categoryRoot);
        newTab.SetActive(false);
        newTab.GetOrAddComponent<UIInputHandler>().m_onLeftDown += Hud.instance.OnLeftClickCategory;

        foreach (TMP_Text text in newTab.GetComponentsInChildren<TMP_Text>())
        {
            text.rectTransform.offsetMin = new Vector2(3, 1);
            text.rectTransform.offsetMax = new Vector2(-3, -1);
            text.enableAutoSizing = true;
            text.fontSizeMin = 12;
            text.fontSizeMax = 20;
            text.lineSpacing = 0.8f;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
        }

        return newTab;
    }

    private static int MaxCategory() => Enum.GetValues(typeof(Piece.PieceCategory)).Length - 1;

    private static List<CodeInstruction> TranspileMaxCategory(IEnumerable<CodeInstruction> instructions, int maxOffset)
    {
        int number = (int)Piece.PieceCategory.Max + maxOffset;
        List<CodeInstruction> newInstructions = new();
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.LoadsConstant(number))
            {
                newInstructions.Add(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(PiecePrefabManager), nameof(MaxCategory))));
                if (maxOffset != 0)
                {
                    newInstructions.Add(new CodeInstruction(OpCodes.Ldc_I4, maxOffset));
                    newInstructions.Add(new CodeInstruction(OpCodes.Add));
                }
            }
            else
            {
                newInstructions.Add(instruction);
            }
        }

        return newInstructions;
    }

    static IEnumerable<CodeInstruction> NextCategory_Transpiler(IEnumerable<CodeInstruction> instructions) => TranspileMaxCategory(instructions, 0);

    static IEnumerable<CodeInstruction> PrevCategory_Transpiler(IEnumerable<CodeInstruction> instructions) => TranspileMaxCategory(instructions, -1);

    static IEnumerable<CodeInstruction> SetCategory_Transpiler(IEnumerable<CodeInstruction> instructions) => TranspileMaxCategory(instructions, -1);

    static IEnumerable<CodeInstruction> UpdateAvailable_Transpiler(IEnumerable<CodeInstruction> instructions) => TranspileMaxCategory(instructions, 0);

    private static HashSet<Piece.PieceCategory> CategoriesInPieceTable(PieceTable pieceTable)
    {
        HashSet<Piece.PieceCategory> categories = new();

        foreach (GameObject piece in pieceTable.m_pieces)
        {
            categories.Add(piece.GetComponent<Piece>().m_category);
        }

        return categories;
    }

    private static void RepositionCategories(PieceTable pieceTable)
    {
        RectTransform firstTab = (RectTransform)Hud.instance.m_pieceCategoryTabs[0].transform;
        RectTransform categoryRoot = (RectTransform)Hud.instance.m_pieceCategoryRoot.transform;
        RectTransform selectionWindow = (RectTransform)Hud.instance.m_pieceSelectionWindow.transform;

        const int verticalSpacing = 1;
        Vector2 tabSize = firstTab.rect.size;

        HashSet<Piece.PieceCategory> visibleCategories = CategoriesInPieceTable(pieceTable);
        Dictionary<Piece.PieceCategory, string> categories = GetPieceCategoriesMap();

        bool onlyMiscActive = visibleCategories.Count == 1 && visibleCategories.First() == Piece.PieceCategory.Misc;
        pieceTable.m_useCategories = !onlyMiscActive;

        int maxHorizontalTabs = Mathf.Max((int)(categoryRoot.rect.width / tabSize.x), 1);
        int visibleTabs = VisibleTabCount(visibleCategories);

        float tabAnchorX = (-tabSize.x * maxHorizontalTabs) / 2f + tabSize.x / 2f;
        float tabAnchorY = (tabSize.y + verticalSpacing) * Mathf.Floor((float)(visibleTabs - 1) / maxHorizontalTabs) + 5f;
        Vector2 tabAnchor = new Vector2(tabAnchorX, tabAnchorY);

        int tabIndex = 0;

        for (int i = 0; i < Hud.instance.m_pieceCategoryTabs.Length; ++i)
        {
            GameObject tab = Hud.instance.m_pieceCategoryTabs[i];
            string categoryName = categories[(Piece.PieceCategory)i];
            bool active = visibleCategories.Contains((Piece.PieceCategory)i);

            SetTabActive(tab, categoryName, active);

            if (active)
            {
                RectTransform rect = tab.GetComponent<RectTransform>();
                float x = tabSize.x * (tabIndex % maxHorizontalTabs);
                float y = -(tabSize.y + verticalSpacing) * Mathf.Floor((float)tabIndex / maxHorizontalTabs);
                rect.anchoredPosition = tabAnchor + new Vector2(x, y);
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                tabIndex++;
            }

            // only update names of own tabs, as translation tokens may be different between mods
            if (PieceCategories.ContainsKey(categoryName))
            {
                Hud.instance.m_buildCategoryNames[i] = $"${GetCategoryToken(categoryName)}";
            }
        }

        RectTransform background = (RectTransform)selectionWindow.Find("Bkg2")?.transform!;

        if (background)
        {
            float height = (tabSize.y + verticalSpacing) * Mathf.Max(0, Mathf.FloorToInt((float)(tabIndex - 1) / maxHorizontalTabs));
            background.offsetMax = new Vector2(background.offsetMax.x, height);
        }
        else
        {
            Debug.LogWarning("RefreshCategories: Could not find background image");
        }

        if ((int)Player.m_localPlayer.m_buildPieces.m_selectedCategory >= Hud.instance.m_buildCategoryNames.Count)
        {
            Player.m_localPlayer.m_buildPieces.SetCategory((int)visibleCategories.First());
        }

        Hud.instance.GetComponentInParent<Localize>().RefreshLocalization();
    }

    private static int VisibleTabCount(HashSet<Piece.PieceCategory> visibleCategories)
    {
        int visibleTabs = 0;

        for (int i = 0; i < Hud.instance.m_pieceCategoryTabs.Length; ++i)
        {
            bool active = visibleCategories.Contains((Piece.PieceCategory)i);

            if (active)
            {
                visibleTabs++;
            }
        }

        return visibleTabs;
    }

    private static void SetTabActive(GameObject tab, string tabName, bool active)
    {
        tab.SetActive(active);

        if (active)
        {
            tab.name = tabName.Replace(_hiddenCategoryMagic, "");
        }
        else
        {
            tab.name = $"{tabName}{_hiddenCategoryMagic}";
        }
    }

    private static string GetCategoryToken(string name)
    {
        char[] forbiddenCharsArray = Localization.instance.m_endChars;
        string tokenCategory = string.Concat(name.ToLower().Split(forbiddenCharsArray));
        return $"piecemanager_cat_{tokenCategory}";
    }

    private static void Patch_SetPlaceMode(Player __instance)
    {
        if (__instance.m_buildPieces)
        {
            RepositionCategories(__instance.m_buildPieces);
        }
    }

    private static void Patch_PieceTable_NextCategory(PieceTable __instance)
    {
        if (__instance.m_pieces.Count == 0 || !__instance.m_useCategories)
        {
            return;
        }

        GameObject selectedTab = Hud.instance.m_pieceCategoryTabs[(int)__instance.m_selectedCategory];

        if (selectedTab.name.Contains(_hiddenCategoryMagic))
        {
            __instance.NextCategory();
        }
    }

    private static void Patch_PieceTable_PrevCategory(PieceTable __instance)
    {
        if (__instance.m_pieces.Count == 0 || !__instance.m_useCategories)
        {
            return;
        }

        GameObject selectedTab = Hud.instance.m_pieceCategoryTabs[(int)__instance.m_selectedCategory];

        if (selectedTab.name.Contains(_hiddenCategoryMagic))
        {
            __instance.PrevCategory();
        }
    }

    private static void UpdateAvailable_Prefix(PieceTable __instance)
    {
        if (__instance.m_availablePieces.Count > 0)
        {
            int missing = MaxCategory() - __instance.m_availablePieces.Count;
            for (int i = 0; i < missing; i++)
            {
                __instance.m_availablePieces.Add(new List<Piece>());
            }
        }
    }

    private static void UpdateAvailable_Postfix(PieceTable __instance)
    {
        Array.Resize(ref __instance.m_selectedPiece, __instance.m_availablePieces.Count);
        Array.Resize(ref __instance.m_lastSelectedPiece, __instance.m_availablePieces.Count);
    }

    [HarmonyPriority(Priority.Low)]
    private static void Hud_AwakeCreateTabs()
    {
        CreateCategoryTabs();
    }

    [HarmonyPriority(Priority.VeryHigh)]
    private static void Patch_ZNetSceneAwake(ZNetScene __instance)
    {
        foreach (GameObject prefab in piecePrefabs)
        {
            if (!__instance.m_prefabs.Contains(prefab))
                __instance.m_prefabs.Add(prefab);
        }
    }

    [HarmonyPriority(Priority.VeryHigh)]
    private static void RefFixPatch_ZNetSceneAwake(ZNetScene __instance)
    {
        foreach (GameObject prefab in piecePrefabs)
        {
            if (__instance.m_prefabs.Contains(prefab))
            {
                if (prefab.GetComponent<StationExtension>())
                {
                    prefab.GetComponent<Piece>().m_isUpgrade = true;
                    prefab.GetComponent<StationExtension>().m_connectionPrefab = __instance
                        .GetPrefab("piece_workbench_ext3").GetComponent<StationExtension>().m_connectionPrefab;
                    prefab.GetComponent<StationExtension>().m_connectionOffset = __instance
                        .GetPrefab("piece_workbench_ext3").GetComponent<StationExtension>().m_connectionOffset;
                }
            }
        }
    }

    [HarmonyPriority(Priority.LowerThanNormal)]
    private static void Patch_ObjectDBInit(ObjectDB __instance)
    {
        foreach (BuildPiece piece in BuildPiece.registeredPieces)
        {
            foreach (string tool in piece.activeTools)
            {
                if (__instance.GetItemPrefab(tool)?.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces is { } pieceTable)
                {
                    if (!pieceTable.m_pieces.Contains(piece.Prefab))
                    {
                        pieceTable.m_pieces.Add(piece.Prefab);
                    }
                }
            }
        }
    }
}

[PublicAPI]
public class Conversion
{
    internal class ConversionConfig
    {
        public ConfigEntry<string> input = null!;
        public ConfigEntry<string> output = null!;
    }

    public string Input = null!;
    public string Output = null!;

    internal ConversionConfig? config;

    public Conversion(BuildPiece conversionPiece)
    {
        conversionPiece.Conversions.Add(this);
    }
}