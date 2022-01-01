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
    internal class MyWearNTear
    {

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WearNTear), "Awake")]
        private static void PatchAwake(ref WearNTear __instance)
        {
            Jotunn.Logger.LogInfo("MyWearNTear Awake");
            if (SklentMod.SklentMod.buildingHighlightAlwaysOn)
            {
                __instance.Highlight();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WearNTear), "Highlight")]
        private static void PatchHighlight(ref WearNTear __instance)
        {
            //Jotunn.Logger.LogInfo("MyWearNTear Highlight");
            if (SklentMod.SklentMod.buildingHighlightAlwaysOn)
            {
                SklentMod.SklentMod.highlighted.Add(__instance);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WearNTear), "ResetHighlight")]
        private static bool PatchResetHighlight(ref WearNTear __instance)
        {
            //Jotunn.Logger.LogInfo("MyWearNTear Reset Highlight, prefix");
            if (SklentMod.SklentMod.buildingHighlightAlwaysOn)
            {
                return false; // Block the original method from firing
            }
            else return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WearNTear), "UpdateSupport")]
        private static void PatchUpdateSupport(ref WearNTear __instance)
        {
            if (SklentMod.SklentMod.buildingHighlightAlwaysOn)
            {
                __instance.Highlight();
            }
        }
    }
}
