using System;
using System.Timers;
using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;

namespace JotunnModStub;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid)]
//[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
internal class JotunnModStub : BaseUnityPlugin
{
    public const string PluginGUID = "com.jotunn.jotunnmodstub";
    public const string PluginName = "JotunnModStub";
    public const string PluginVersion = "0.0.1";
        
    // Use this class to add your own localization to the game
    // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
    public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();
    public System.Timers.Timer _timer;
        

    private void Awake()
    {
        // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
        Jotunn.Logger.LogInfo("ModStub has landed Redux");
            
        // To learn more about Jotunn's features, go to
        // https://valheim-modding.github.io/Jotunn/tutorials/overview.html

        _timer = new System.Timers.Timer();
        _timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
        _timer.Interval = 5000;
        _timer.Enabled = true;
        _timer.Start();
    }

    private void OnTimedEvent(object sender, ElapsedEventArgs e) => Jotunn.Logger.LogInfo($"ModStub has ticked ${DateTime.Now:O}");
}