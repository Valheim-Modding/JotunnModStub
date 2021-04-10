// JotunnModStub
// a Valheim mod skeleton using JötunnLib
// 
// File:    JotunnModStub.cs
// Project: JotunnModStub

using BepInEx;
using UnityEngine;
using BepInEx.Configuration;

namespace JotunnModStub
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(JotunnLib.Main.ModGuid)]
    //[NetworkCompatibilty(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class JotunnModStubPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.jotunn.jotunnmodstub";
        public const string PluginName = "JotunnModStub";
        public const string PluginVersion = "0.0.1";
        public static new JotunnLib.Logger Logger;
        public static new ConfigFile Config;

        private void Awake()
        {
            Config = base.Config;
            // Do all your init stuff here
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