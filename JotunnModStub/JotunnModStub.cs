// JotunnModStub
// a Valheim mod skeleton using JötunnLib
// 
// File:    JotunnModStub.cs
// Project: JotunnModStub

using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using ValheimMod.UnityWrappers;

namespace JotunnModStub
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    internal class JotunnModStubPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.jotunn.jotunnmodstub";
        public const string PluginName = "JotunnModStub";
        public const string PluginVersion = "0.0.1";

        private Harmony m_harmony;
        
        private void Awake()
        {
            // Create harmony patches
            m_harmony = new Harmony(PluginGUID);
            m_harmony.PatchAll();

            // Make sure the references for the Unity wrappers are loaded
            Assembly.GetAssembly(typeof(ItemDropWrapper));
        }

#if DEBUG
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            { // Set a breakpoint here to break on F6 key press
            }
        }
#endif

        private void OnDestroy()
        {
            // Remove harmony patches
            m_harmony.UnpatchAll(PluginGUID);
        }
    }
}