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
    internal class MyHumanoid
    {

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Humanoid), "Awake")]
        private static void PatchAwake(ref Humanoid __instance)
        {
            //Jotunn.Logger.LogInfo("MyHumanoid Awake");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Humanoid), "EquipItem")]
        private static void PatchEquipItem(ref Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects = true)
        {
            Jotunn.Logger.LogInfo("MyHumanoid EquipItem " + item.m_shared.m_name);
            if (item == null || item.m_shared == null || item.m_shared.m_name == null)
            {
                return;
            }
            if (item.m_shared.m_name == "$item_hammer") {
                SklentMod.SklentMod.buildingHighlightAlwaysOn = true;
            }
            Jotunn.Logger.LogInfo("buildingHighlightAlwaysOn = " + SklentMod.SklentMod.buildingHighlightAlwaysOn);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Humanoid), "UnequipItem")]
        private static void PatchUnEquipItem(ref Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects = true)
        {
            Jotunn.Logger.LogInfo("MyHumanoid UnequipItem");
            if (item == null || item.m_shared == null || item.m_shared.m_name == null)
            {
                return;
            }
            if (item.m_shared.m_name == "$item_hammer")
            {
                SklentMod.SklentMod.buildingHighlightAlwaysOn = false;
                foreach (WearNTear wearNTear in SklentMod.SklentMod.highlighted) {
                    wearNTear.ResetHighlight();
                }
                SklentMod.SklentMod.highlighted = new List<WearNTear>();
            }
             Jotunn.Logger.LogInfo("buildingHighlightAlwaysOn = " + SklentMod.SklentMod.buildingHighlightAlwaysOn);
        }

    }
}
