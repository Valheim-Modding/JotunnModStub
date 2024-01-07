using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Timers;
using UnityEngine;

namespace QuickStackStore;

//[BepInIncompatibility("virtuacode.valheim.trashitems")]
//[BepInDependency(Jotunn.Main.ModGuid)]
//[BepInDependency(CompatibilitySupport.azuEPI, BepInDependency.DependencyFlags.SoftDependency)]
//[BepInDependency(CompatibilitySupport.multiUserChest, BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
//[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
public class QuickStackStorePlugin : BaseUnityPlugin
{
    public const string PluginGUID = "goldenrevolver.quick_stack_store";
    public const string PluginName = "Quick Stack - Store - Sort - Trash - Restock";
    public const string PluginVersion = "1.4.5";

    #region StupidTimer

    public System.Timers.Timer _timer;

    private void LoadStupidTimer()
    {
        _timer = new System.Timers.Timer();
        _timer.Elapsed += OnTimedEvent;
        _timer.Interval = 5000;
        _timer.Start();
    }
    private void OnTimedEvent(object sender, ElapsedEventArgs e)
        => Logger.LogError($"{nameof(QuickStackStorePlugin)} has ticked ${DateTime.Now:O}");

    #endregion

    // intentionally not Awake, so the chainloader is done (for compatibility checks, mostly in the config)
    protected void Start()
    {
        LoadStupidTimer();
        Logger.LogError($"***************************************************************************");
        Logger.LogError($"***************************************************************************");
        Logger.LogError($"****  {nameof(QuickStackStorePlugin)} has started ${DateTime.Now:O} ");
        Logger.LogError($"***************************************************************************");
        Logger.LogError($"***************************************************************************");

        if (CompatibilitySupport.HasOutdatedMUCPlugin())
        {
            Helper.LogO("This mod is not compatible with versions of Multi User Chest earlier than 0.4.0, aborting start", QSSConfig.DebugLevel.Warning);
            return;
        }

        if (AzuExtendedPlayerInventory.API.IsLoaded())
        {
            CompatibilitySupport.isUsingAzuEPIWithAPI = true;

            if (CompatibilitySupport.HasAzuEPIWithQuickslotCompatibleAPI())
            {
                CompatibilitySupport.isUsingAzuEPIWithQuickslotCompatibleAPI = true;
            }
        }

        string path = "QuickStackStore.Resources";

        BorderRenderer.border = Helper.LoadSprite($"{path}.border.png", new Rect(0, 0, 1024, 1024));
        TrashModule.trashSprite = Helper.LoadSprite($"{path}.trash.png", new Rect(0, 0, 64, 64));
        TrashModule.bgSprite = Helper.LoadSprite($"{path}.trashmask.png", new Rect(0, 0, 96, 112));

        QSSConfig.LoadConfig(this);

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }

    [HarmonyPatch(typeof(FejdStartup))]
    internal class FejdStartupPatch
    {
        [HarmonyPatch(nameof(FejdStartup.Awake)), HarmonyPostfix]
        private static void FejdStartupAwakePatch()
        {
            LocalizationLoader.SetupTranslations();
            QSSConfig.ConfigTemplate_SettingChanged(null, null);
        }
    }
}