using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

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
            foreach (ItemDrop.ItemData item in items)
            {
                Jotunn.Logger.LogInfo(item.GetType().Name + " " + item.GetValue().ToString() + " " + item.GetTooltip());
            }


            //foreach (ItemDrop.ItemData itemData in __instance.m_inventory.GetAllItems())
            //{
            //    InventoryGrid.Element element = __instance.GetElement(itemData.m_gridPos.x, itemData.m_gridPos.y, __instance.m_inventory.GetWidth());
                
            //}
        }
    }
}
