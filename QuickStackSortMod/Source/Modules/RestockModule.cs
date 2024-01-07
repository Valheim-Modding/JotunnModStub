using System;
using System.Collections.Generic;
using System.Linq;
using static ItemDrop;
using static QuickStackStore.QSSConfig;

namespace QuickStackStore
{
    internal struct RestockData
    {
        internal ItemData itemData;
        internal int potentialCurrentStackSize;
        internal int maximumWantedStackSize;

        public RestockData(ItemData itemData, int potentialCurrentStackSize, int maximumWantedStackSize)
        {
            this.itemData = itemData;
            this.potentialCurrentStackSize = potentialCurrentStackSize;
            this.maximumWantedStackSize = maximumWantedStackSize;
        }
    }

    internal class RestockModule
    {
        private static bool ShouldRestockItem(ItemData item, UserConfig playerConfig, int inventoryHeight, int inventoryWidth, bool includeHotbar)
        {
            int maxStack = GetConfigBasedMaxStackSize(item.m_shared);
            ItemData.ItemType type = item.m_shared.m_itemType;

            return maxStack > 1 && maxStack > item.m_stack
                && (item.m_customData == null || item.m_customData.Count == 0)
                && (item.m_gridPos.y > 0 || includeHotbar)
                && (!RestockConfig.RestockOnlyAmmoAndConsumables.Value || type == ItemData.ItemType.Ammo || type == ItemData.ItemType.Consumable)
                && (!RestockConfig.RestockOnlyFavoritedItems.Value || playerConfig.IsItemNameOrSlotFavorited(item))
                && !CompatibilitySupport.IsEquipSlot(inventoryHeight, inventoryWidth, item.m_gridPos);
        }

        private static int GetConfigBasedMaxStackSize(ItemData.SharedData shared)
        {
            int maxStack = shared.m_maxStackSize;
            int configValue = 0;

            // yes, I considered a switch statement, but I want the general case to override the ammo and consumable case if those config values are not set (0 or negative)
            if (shared.m_itemType == ItemData.ItemType.Ammo)
            {
                configValue = RestockConfig.RestockStackSizeLimitAmmo.Value;
            }
            else if (shared.m_itemType == ItemData.ItemType.Consumable)
            {
                configValue = RestockConfig.RestockStackSizeLimitConsumables.Value;
            }

            if (configValue <= 0)
            {
                configValue = RestockConfig.RestockStackSizeLimitGeneral.Value;
            }

            if (configValue > 0)
            {
                maxStack = Math.Min(maxStack, configValue);
            }

            return maxStack;
        }

        private static bool ShouldAreaRestock(Container currentContainer)
        {
            return RestockConfig.RestockFromNearbyRange.Value > 0
                && (currentContainer == null || RestockConfig.RestockHotkeyBehaviorWhenContainerOpen.Value != RestockBehavior.RestockOnlyFromCurrentContainer)
                && CompatibilitySupport.AllowAreaStackingRestocking();
        }

        internal static void DoRestock(Player player, bool RestockOnlyFromCurrentContainerOverride = false)
        {
            if (player.IsTeleporting() || !InventoryGui.instance.m_container)
            {
                return;
            }

            InventoryGui.instance.SetupDragItem(null, null, 0);

            UserConfig playerConfig = UserConfig.GetPlayerConfig(player.GetPlayerID());

            bool includeHotbar = GeneralConfig.OverrideHotkeyBarBehavior.Value != OverrideHotkeyBarBehavior.NeverAffectHotkeyBar && RestockConfig.RestockIncludesHotkeyBar.Value;

            List<RestockData> restockables = player.m_inventory.m_inventory
                .Where((itemData) => ShouldRestockItem(itemData, playerConfig, player.m_inventory.GetHeight(), player.m_inventory.GetWidth(), includeHotbar))
                .Select((itemData) => new RestockData(itemData, itemData.m_stack, GetConfigBasedMaxStackSize(itemData.m_shared)))
                .ToList();

            int totalRestockableCount = restockables.Count;

            if (totalRestockableCount == 0 && RestockConfig.ShowRestockResultMessage.Value)
            {
                player.Message(MessageHud.MessageType.Center, LocalizationConfig.GetRelevantTranslation(LocalizationConfig.RestockResultMessageNothing, nameof(LocalizationConfig.RestockResultMessageNothing)), 0, null);
                return;
            }

            // sort in reverse, because we iterate in reverse
            restockables.Sort((RestockData a, RestockData b) => -1 * Helper.CompareSlotOrder(a.itemData.m_gridPos, b.itemData.m_gridPos));

            int restockedStackCount = 0;
            var partiallyFilledStacks = new HashSet<Vector2i>();
            Container currentContainer = InventoryGui.instance.m_currentContainer;

            if (currentContainer != null)
            {
                restockedStackCount = RestockFromThisContainer(restockables, player.m_inventory, currentContainer.m_inventory, partiallyFilledStacks);
            }

            if (RestockOnlyFromCurrentContainerOverride || !ShouldAreaRestock(currentContainer))
            {
                ReportRestockResult(player, restockedStackCount, partiallyFilledStacks.Count, totalRestockableCount);
                return;
            }

            List<Container> containers = ContainerFinder.FindContainersInRange(player.transform.position, RestockConfig.RestockFromNearbyRange.Value);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            if (containers.Count > 0)
            {
                restockedStackCount += RestockFromMultipleContainers(restockables, player, containers, partiallyFilledStacks);
            }

            sw.Stop();
            Helper.Log($"Restocking time: {sw.Elapsed}", DebugSeverity.AlsoSpeedTests);

            ReportRestockResult(player, restockedStackCount, partiallyFilledStacks.Count, totalRestockableCount);
        }

        private static int RestockFromThisContainer(List<RestockData> itemsToRestock, Inventory playerInventory, Inventory container, HashSet<Vector2i> partiallyFilledStacks, bool callPlayerInvChanged = true)
        {
            if (itemsToRestock?.Count <= 0)
            {
                return 0;
            }

            int restockedStackCount = 0;

            for (int i = itemsToRestock.Count - 1; i >= 0; i--)
            {
                var playerItem = itemsToRestock[i];

                for (int j = container.m_inventory.Count - 1; j >= 0; j--)
                {
                    var containerItem = container.m_inventory[j];

                    if (containerItem.m_customData != null && containerItem.m_customData.Count > 0)
                    {
                        continue;
                    }

                    if (containerItem.m_shared.m_name != playerItem.itemData.m_shared.m_name || containerItem.m_quality != playerItem.itemData.m_quality)
                    {
                        continue;
                    }

                    int itemCountToMove = Math.Min(playerItem.maximumWantedStackSize - playerItem.potentialCurrentStackSize, containerItem.m_stack);

                    if (itemCountToMove > 0)
                    {
                        // assume the moving works
                        playerItem.potentialCurrentStackSize += itemCountToMove;
                        partiallyFilledStacks.Add(playerItem.itemData.m_gridPos);
                        playerInventory.MoveItemToThis(container, containerItem, itemCountToMove, playerItem.itemData.m_gridPos.x, playerItem.itemData.m_gridPos.y);
                    }

                    if (playerItem.potentialCurrentStackSize == playerItem.maximumWantedStackSize)
                    {
                        itemsToRestock.RemoveAt(i);
                        restockedStackCount++;
                        break;
                    }
                }
            }

            if (callPlayerInvChanged)
            {
                playerInventory.Changed();
            }

            return restockedStackCount;
        }

        private static int RestockFromMultipleContainers(List<RestockData> itemsToRestock, Player player, List<Container> containers, HashSet<Vector2i> partialRestockCounter)
        {
            int restockedStackCount = 0;

            bool isSinglePlayer = AreaStackRestockHelper.IsTrueSingleplayer();

            foreach (Container container in containers)
            {
                if (!AreaStackRestockHelper.ShouldAffectNonOwnerContainer(container, player.GetPlayerID(), isSinglePlayer))
                {
                    continue;
                }

                if (CompatibilitySupport.HasPlugin(CompatibilitySupport.multiUserChest))
                {
                    restockedStackCount += RestockFromThisContainer(itemsToRestock, player.m_inventory, container.m_inventory, partialRestockCounter, false);
                }
                else
                {
                    container.m_nview.ClaimOwnership();

                    AreaStackRestockHelper.SetNonMUCContainerInUse(container, true);

                    restockedStackCount += RestockFromThisContainer(itemsToRestock, player.m_inventory, container.m_inventory, partialRestockCounter, false);

                    AreaStackRestockHelper.SetNonMUCContainerInUse(container, false);
                }
            }

            player.m_inventory.Changed();

            return restockedStackCount;
        }

        public static void ReportRestockResult(Player player, int movedCount, int partiallyFilledCount, int totalCount)
        {
            if (!RestockConfig.ShowRestockResultMessage.Value)
            {
                return;
            }

            string message;

            if (movedCount == 0 && partiallyFilledCount == 0)
            {
                message = string.Format(LocalizationConfig.GetRelevantTranslation(LocalizationConfig.RestockResultMessageNone, nameof(LocalizationConfig.RestockResultMessageNone)), totalCount);
            }
            else if (movedCount < totalCount)
            {
                message = string.Format(LocalizationConfig.GetRelevantTranslation(LocalizationConfig.RestockResultMessagePartial, nameof(LocalizationConfig.RestockResultMessagePartial)), partiallyFilledCount, totalCount);
            }
            else if (movedCount == totalCount)
            {
                message = string.Format(LocalizationConfig.GetRelevantTranslation(LocalizationConfig.RestockResultMessageFull, nameof(LocalizationConfig.RestockResultMessageFull)), totalCount);
            }
            else
            {
                message = $"Invalid restock: Restocked more items than we originally had ({movedCount}/{totalCount})";
                Helper.LogO(message, DebugLevel.Warning);
            }

            player.Message(MessageHud.MessageType.Center, message, 0, null);
        }
    }
}