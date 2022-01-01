using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Configs;
using System.IO;
using System.Reflection;

namespace JotunnModStub
{
    [HarmonyPatch]
    internal class MyPlayer
    {

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "Awake")]
        private static void PatchAwake(ref Player __instance)
        {
            //Jotunn.Logger.LogInfo("MyPlayer Awake");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "UpdatePlacement")]
        private static void PatchUpdatePlacement(ref Player __instance, bool takeInput, float dt)
        {
            //Jotunn.Logger.LogInfo("MyPlayer UpdatePlacement");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "UpdateWearNTearHover")]
        private static void PatchUpdateWearNTearHover(ref Player __instance)
        {
            //Jotunn.Logger.LogInfo("MyPlayer UpdateWearNTearHover");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "GetHoveringPiece")]
        private static void PatchGetHoveringPiece(ref Player __instance)
        {
            String itemName = "";
            if (__instance.m_hoveringPiece != null)
            {
                itemName = __instance.m_hoveringPiece.m_name;
                Jotunn.Logger.LogInfo("MyPlayer GetHoveringPiece " + itemName);
            }
        }
    }
}