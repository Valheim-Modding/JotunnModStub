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
    public static class MyInventoryGui
    {

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), "Awake")]
        private static void PatchInventory(ref InventoryGui __instance)
        {
            //Jotunn.Logger.LogInfo("InventoryGui Awake");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), "Show")]
        private static void PatchShow(ref InventoryGui __instance, Container container)
        {
            Jotunn.Logger.LogInfo("InventoryGui Show..");
            // Organize twice to fix an issue
            organizeInventory(__instance.m_playerGrid);
            organizeInventory(__instance.m_playerGrid);
        }

        public static void closeCustomGui(GameObject panel)
        {
            Jotunn.Logger.LogInfo("Close gui..");
            SklentMod.SklentMod.Destroy(panel);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), "Hide")]
        private static void PatchHide(ref InventoryGui __instance)
        {
            if (SklentMod.SklentMod.inventoryPanel != null)
            {
                Jotunn.Logger.LogInfo("InventoryGui Destory Panel");
                SklentMod.SklentMod.Destroy(SklentMod.SklentMod.inventoryPanel);
                SklentMod.SklentMod.inventoryPanel = null;
            }
            //Jotunn.Logger.LogInfo("InventoryGui Hide");
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), "Update")]
        private static void PatchUpdate(ref InventoryGui __instance)
        {
            //Jotunn.Logger.LogInfo("InventoryGui Update");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), "UpdateInventory")]
        private static void PatchUpdateInventory(ref InventoryGui __instance, Player player)
        {
            //Jotunn.Logger.LogInfo("InventoryGui UpdateInventory");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), "UpdateContainer")]
        private static void PatchUpdateContainer(ref InventoryGui __instance, Player player)
        {
            //Jotunn.Logger.LogInfo("InventoryGui UpdateContainer");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), "OnTakeAll")]
        private static void PatchOnTakeAll(ref InventoryGui __instance)
        {
            Jotunn.Logger.LogInfo("InventoryGui OnTakeAll");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), "OnDropOutside")]
        private static void PatchOnDropOutsidel(ref InventoryGui __instance)
        {
            Jotunn.Logger.LogInfo("InventoryGui OnDropOutside");
        }

        private static void organizeInventory(InventoryGrid __instance)
        {

            List<String> seenItemNames = new List<String>();

            List<ItemDrop.ItemData> equipment = new List<ItemDrop.ItemData>();
            List<ItemDrop.ItemData> consumables = new List<ItemDrop.ItemData>();
            List<ItemDrop.ItemData> bsItemsInEquipBar = new List<ItemDrop.ItemData>();
            HashSet<String> combinable = new HashSet<String>();
            foreach (ItemDrop.ItemData item in __instance.GetInventory().GetAllItems())
            {
                if (item.m_gridPos.y != 0)
                {
                    if (item.IsEquipable())
                    {
                        equipment.Add(item);
                    }
                    if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable)
                    {
                        consumables.Add(item);
                    }
                    if (item.m_stack < item.m_shared.m_maxStackSize)
                    {
                        if (seenItemNames.Contains(item.m_shared.m_name))
                        {

                            combinable.Add(item.m_shared.m_name);
                        } else seenItemNames.Add(item.m_shared.m_name);
                    }
                }
                else
                {
                    if (!item.IsEquipable() && !item.m_shared.m_name.Contains("mead"))
                    {
                        bsItemsInEquipBar.Add(item);
                    }
                    Jotunn.Logger.LogInfo("Skipping " + item.m_shared.m_name);
                }
            }

            bool doRefresh = false;
            if (combinable.Count > 0)
            {
                foreach (String itemName in combinable)
                {
                    List<ItemDrop.ItemData> toCombine = __instance.GetInventory().GetAllItems().FindAll(it => it.m_shared.m_name.Equals(itemName));
                    ItemDrop.ItemData first = toCombine[0];
                    int stackSize = first.m_stack;
                    for (int i = 1; i < toCombine.Count; i++)
                    {
                        if (stackSize < first.m_shared.m_maxStackSize)
                        {
                            __instance.DropItem(__instance.GetInventory(), toCombine[i], toCombine[i].m_stack, new Vector2i(first.m_gridPos.x, first.m_gridPos.y));
                            doRefresh = true;
                            stackSize = stackSize + toCombine[i].m_stack;
                        }
                        else break;
                    }
                }
            }
            if (doRefresh)
            {
                organizeInventory(__instance);
            }


            List<ItemDrop.ItemData> sortedEquipment = sortEquipment(equipment);
            int equip_x_index = 0;
            int equip_y_index = 1;
            foreach (ItemDrop.ItemData item in sortedEquipment)
            {
                if (equip_x_index > 7)
                {
                    equip_y_index = equip_y_index + 1;
                    equip_x_index = 0;
                }
                Jotunn.Logger.LogInfo("Moving " + item.m_shared.m_name + " from " + item.m_gridPos.x + item.m_gridPos.y + " to " + equip_x_index + equip_y_index);
                __instance.DropItem(__instance.GetInventory(), item, item.m_stack, new Vector2i(equip_x_index, equip_y_index));
                equip_x_index = equip_x_index + 1;
            }

            List<ItemDrop.ItemData> sortedConsumables = sortConsumables(consumables);
            int c_x_index = 0;
            int c_y_index = 2;
            foreach (ItemDrop.ItemData item in sortedConsumables)
            {
                if (c_x_index > 7)
                {
                    c_y_index = c_y_index + 1;
                    c_x_index = 0;
                }
                Jotunn.Logger.LogInfo("Moving " + item.m_shared.m_name + " from " + item.m_gridPos.x + item.m_gridPos.y + " to " + c_x_index + c_y_index);
                __instance.DropItem(__instance.GetInventory(), item, item.m_stack, new Vector2i(c_x_index, c_y_index));
                c_x_index = c_x_index + 1;
            }

            // Remove any bullshit from empty equip slots (Equipment and mead only)
            foreach (ItemDrop.ItemData item in bsItemsInEquipBar)
            {
                if (__instance.m_inventory.GetEmptySlots() < 1)
                {
                    break;
                }
                Vector2i emptySlot = __instance.m_inventory.FindEmptySlot(false);
                Jotunn.Logger.LogInfo("Moving " + item.m_shared.m_name + " from " + item.m_gridPos.x + item.m_gridPos.y + " to " + emptySlot.x + emptySlot.y);
                __instance.DropItem(__instance.GetInventory(), item, item.m_stack, new Vector2i(emptySlot.x, emptySlot.y));
            }
        }


        private static List<ItemDrop.ItemData> sortEquipment(List<ItemDrop.ItemData> items)
        {
            List<ItemDrop.ItemData> sorted = new List<ItemDrop.ItemData>();
            // Helmet, chest, legs, cape, belt, arrows, everything else

            List<ItemDrop.ItemData> helms = items.FindAll(it => it.m_shared.m_name.Contains("helm"));
            List<ItemDrop.ItemData> chests = items.FindAll(it => it.m_shared.m_name.Contains("chest"));
            List<ItemDrop.ItemData> legs = items.FindAll(it => it.m_shared.m_name.Contains("legs"));
            List<ItemDrop.ItemData> capes = items.FindAll(it => it.m_shared.m_name.Contains("cape"));
            List<ItemDrop.ItemData> belts = items.FindAll(it => it.m_shared.m_name.Contains("belt"));
            List<ItemDrop.ItemData> rest = items.FindAll(it =>
                !it.m_shared.m_name.Contains("helm") &&
                 !it.m_shared.m_name.Contains("chest") &&
                 !it.m_shared.m_name.Contains("legs") &&
                 !it.m_shared.m_name.Contains("cape") &&
                 !it.m_shared.m_name.Contains("belt")
            );

            sorted.AddRange(helms);
            sorted.AddRange(chests);
            sorted.AddRange(legs);
            sorted.AddRange(capes);
            sorted.AddRange(belts);
            sorted.AddRange(rest);
            return sorted;
        }
        private static List<ItemDrop.ItemData> sortConsumables(List<ItemDrop.ItemData> items)
        {
            List<ItemDrop.ItemData> sorted = new List<ItemDrop.ItemData>();
            List<ItemDrop.ItemData> meads = items.FindAll(it => it.m_shared.m_name.Contains("mead"));
            List<ItemDrop.ItemData> health = items.FindAll(it => !it.m_shared.m_name.Contains("mead") && it.m_shared.m_food > it.m_shared.m_foodStamina);
            List<ItemDrop.ItemData> stam = items.FindAll(it => !it.m_shared.m_name.Contains("mead") && it.m_shared.m_food < it.m_shared.m_foodStamina);
            List<ItemDrop.ItemData> balanced = items.FindAll(it => !it.m_shared.m_name.Contains("mead") && it.m_shared.m_food == it.m_shared.m_foodStamina);
            sorted.AddRange(health);
            sorted.AddRange(balanced);
            sorted.AddRange(stam);
            sorted.AddRange(meads);
            return sorted;
        }
    }
}
