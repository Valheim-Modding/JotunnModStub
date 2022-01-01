using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Configs;

namespace JotunnModStub
{
    [HarmonyPatch]
    public static class MyInventoryGrid
    {

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
        private static void PatchInventory(ref InventoryGrid __instance, ref Player player, ItemDrop.ItemData dragItem)
        {
            Inventory inventory = __instance.GetInventory();
            List<ItemDrop.ItemData> items = inventory.GetAllItems();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGrid), "Awake")]
        private static void PatchAwake(ref InventoryGrid __instance)
        {
           // Jotunn.Logger.LogInfo("InventoryGrid: Awake");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGrid), "ResetView")]
        private static void PatchResetView(ref InventoryGrid __instance)
        {
            Jotunn.Logger.LogInfo("InventoryGrid: ResetView");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGrid), "UpdateInventory")]
        private static void PatchUpdateInventory(ref InventoryGrid __instance, ref Player player, ItemDrop.ItemData dragItem)
        {
            //Jotunn.Logger.LogInfo("InventoryGrid: UpdateInventory");
        }

    }

}