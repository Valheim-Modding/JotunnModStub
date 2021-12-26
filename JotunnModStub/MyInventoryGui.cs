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
            Jotunn.Logger.LogInfo("InventoryGui Awake");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), "Show")]
        private static void PatchShow(ref InventoryGui __instance, Container container)
        {
            Jotunn.Logger.LogInfo("InventoryGui Show..");
            if (__instance == null)
            {
                Jotunn.Logger.LogInfo("instance null");
            }
            else { Jotunn.Logger.LogInfo("not null"); }
            InventoryGrid inventoryGrid = __instance.m_playerGrid;
            if (inventoryGrid == null)
            {
                Jotunn.Logger.LogInfo("inventoryGrid instance null");
            }
            else { Jotunn.Logger.LogInfo("inventoryGrid not null"); }
            //Jotunn.Logger.LogInfo("Inventory Weight: " + inventoryGrid.m_inventory.m_totalWeight);
            addCustomInventoryGui(__instance.m_playerGrid);
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
            Jotunn.Logger.LogInfo("InventoryGui Hide");
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), "Update")]
        private static void PatchUpdate(ref InventoryGui __instance)
        {
            //Jotunn.Logger.LogInfo("InventoryGui Update");
        }

        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(InventoryGui), "OnDestory")]
        //private static void PatchOnDestroy(ref InventoryGui __instance)
        //{
        //    Jotunn.Logger.LogInfo("InventoryGui OnDestory");
        //}

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

        private static void addCustomInventoryGui(InventoryGrid inventoryGrid)
        {
            SklentMod.SklentMod.inventoryPanel = GUIManager.Instance.CreateWoodpanel(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f),
                width: 500f,
                height: 300f,
                draggable: true);
            var textObject = GUIManager.Instance.CreateText(
                text: "SklentMod",
                parent: SklentMod.SklentMod.inventoryPanel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, 0f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 20,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);
            var textObject2 = GUIManager.Instance.CreateText(
                text: "Hello",
                parent: SklentMod.SklentMod.inventoryPanel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(0f, -100f),
                font: GUIManager.Instance.AveriaSerifBold,
                fontSize: 20,
                color: GUIManager.Instance.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);
            var btnClose = GUIManager.Instance.CreateButton(
                text: "x",
                parent: SklentMod.SklentMod.inventoryPanel.transform,
                anchorMin: new Vector2(0.5f, 1f),
                anchorMax: new Vector2(0.5f, 1f),
                position: new Vector2(55f, -100f)
                );
            var btnOrganize = GUIManager.Instance.CreateButton(
                text: "Organize inventory",
                parent: SklentMod.SklentMod.inventoryPanel.transform,
                anchorMin: new Vector2(0.3f, 0.5f),
                anchorMax: new Vector2(0.3f, 0.5f),
                position: new Vector2(50f, -50f)
                );
            Button buttonClose = btnClose.GetComponent<Button>();
            buttonClose.onClick.AddListener(() => closeCustomGui(SklentMod.SklentMod.inventoryPanel));
            Button buttonOrganize = btnOrganize.GetComponent<Button>();
            if (inventoryGrid == null)
            {
                Jotunn.Logger.LogInfo("inventoryGrid instance null2");
            }
            else { Jotunn.Logger.LogInfo("inventoryGrid not null2"); }
            buttonOrganize.onClick.AddListener(() => organizeInventory(inventoryGrid));
        }

        private static void organizeInventory(InventoryGrid __instance)
        {
            List<ItemDrop.ItemData> equipment = new List<ItemDrop.ItemData>();
            List<ItemDrop.ItemData> consumables = new List<ItemDrop.ItemData>();
            List<ItemDrop.ItemData> bsItemsInEquipBar = new List<ItemDrop.ItemData>();
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

            int equip_x_index = 0;
            int equip_y_index = 1;
            foreach (ItemDrop.ItemData item in equipment)
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

            int c_x_index = 0;
            int c_y_index = 2;
            foreach (ItemDrop.ItemData item in consumables)
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
    }
}
