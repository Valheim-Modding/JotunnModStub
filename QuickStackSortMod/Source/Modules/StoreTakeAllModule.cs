using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using static ItemDrop;
using static QuickStackStore.QSSConfig;

namespace QuickStackStore
{
    [HarmonyPatch(typeof(InventoryGui))]
    internal class TakeAllPatch
    {
        [HarmonyPatch(nameof(InventoryGui.OnTakeAll)), HarmonyPrefix]
        internal static bool ContextSensitiveTakeAll(InventoryGui __instance)
        {
            if (!StoreTakeAllConfig.ChestsUseImprovedTakeAllLogic.Value)
            {
                return true;
            }

            if (!__instance.m_currentContainer || __instance.m_currentContainer.GetComponent<TombStone>())
            {
                return true;
            }

            StoreTakeAllModule.TakeAllItemsInOrder(Player.m_localPlayer);
            return false;
        }
    }

    internal class StoreTakeAllModule
    {
        internal static void DoTakeAllWithKeybind(Player player)
        {
            if (!InventoryGui.instance || !InventoryGui.IsVisible())
            {
                return;
            }

            if (player != Player.m_localPlayer)
            {
                return;
            }

            InventoryGui.instance.OnTakeAll();
        }

        internal static void DoStoreAllWithKeybind(Player player)
        {
            if (!InventoryGui.instance || !InventoryGui.IsVisible())
            {
                return;
            }

            if (player != Player.m_localPlayer)
            {
                return;
            }

            StoreAllItemsInOrder(player);
        }

        private static bool ShouldStoreItem(ItemData item, UserConfig playerConfig, int inventoryHeight, int inventoryWidth, bool includeHotbar)
        {
            return (item.m_gridPos.y > 0 || includeHotbar)
                && (StoreTakeAllConfig.StoreAllIncludesEquippedItems.Value || !item.m_equipped)
                && !playerConfig.IsItemNameOrSlotFavorited(item)
                && !CompatibilitySupport.IsEquipOrQuickSlot(inventoryHeight, inventoryWidth, item.m_gridPos);
        }

        internal static void TakeAllItemsInOrder(Player player)
        {
            if (!InventoryGui.instance.m_currentContainer)
            {
                return;
            }

            Inventory fromInventory = InventoryGui.instance.m_currentContainer.m_inventory;
            Inventory toInventory = player.m_inventory;

            MoveAllItemsInOrder(player, fromInventory, toInventory, true);
        }

        internal static void StoreAllItemsInOrder(Player player)
        {
            if (!InventoryGui.instance.m_currentContainer)
            {
                return;
            }

            Inventory fromInventory = player.m_inventory;
            Inventory toInventory = InventoryGui.instance.m_currentContainer.m_inventory;

            MoveAllItemsInOrder(player, fromInventory, toInventory);
        }

        internal static void MoveAllItemsInOrder(Player player, Inventory fromInventory, Inventory toInventory, bool takeAllOverride = false)
        {
            if (player.IsTeleporting())
            {
                return;
            }

            InventoryGui.instance.SetupDragItem(null, null, 0);

            List<ItemData> list;

            if (takeAllOverride)
            {
                list = new List<ItemData>(fromInventory.m_inventory);
            }
            else
            {
                UserConfig playerConfig = UserConfig.GetPlayerConfig(player.GetPlayerID());
                bool includeHotbar = GeneralConfig.OverrideHotkeyBarBehavior.Value != OverrideHotkeyBarBehavior.NeverAffectHotkeyBar && StoreTakeAllConfig.StoreAllIncludesHotkeyBar.Value;

                list = fromInventory.m_inventory.Where((item) => ShouldStoreItem(item, playerConfig, fromInventory.GetHeight(), fromInventory.GetWidth(), includeHotbar)).ToList();
            }

            list.Sort((ItemData a, ItemData b) => Helper.CompareSlotOrder(a.m_gridPos, b.m_gridPos));

            int num = 0;

            foreach (ItemData itemData in list)
            {
                if (toInventory.AddItem(itemData))
                {
                    fromInventory.RemoveItem(itemData);
                    num++;

                    if (itemData.m_equipped)
                    {
                        Player.m_localPlayer.RemoveEquipAction(itemData);
                        Player.m_localPlayer.UnequipItem(itemData, false);
                    }
                }
            }

            if (takeAllOverride)
            {
                Helper.Log($"Moved {num} item/s from container to player inventory");
            }
            else
            {
                Helper.Log($"Moved {num} item/s from player inventory to container");
            }

            toInventory.Changed();
            fromInventory.Changed();
        }
    }
}