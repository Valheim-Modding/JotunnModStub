using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using static ItemDrop;
using static QuickStackStore.QSSConfig;

namespace QuickStackStore
{
    internal class QuickStackModule
    {
        private static bool ShouldQuickStackItem(ItemData item, UserConfig playerConfig, int inventoryHeight, int inventoryWidth, bool includeHotbar)
        {
            return item.m_shared.m_maxStackSize > 1
                && (item.m_gridPos.y > 0 || includeHotbar)
                && !playerConfig.IsItemNameOrSlotFavorited(item)
                && !CompatibilitySupport.IsEquipOrQuickSlot(inventoryHeight, inventoryWidth, item.m_gridPos);
        }

        private static bool ShouldAreaQuickStack(Container currentContainer)
        {
            return QuickStackConfig.QuickStackToNearbyRange.Value > 0
                && (currentContainer == null || QuickStackConfig.QuickStackHotkeyBehaviorWhenContainerOpen.Value != QuickStackBehavior.QuickStackOnlyToCurrentContainer)
                && CompatibilitySupport.AllowAreaStackingRestocking();
        }

        internal static void DoQuickStack(Player player, bool onlyQuickStackToCurrentContainer = false, Container currentContainerOverride = null)
        {
            if (player.IsTeleporting() || !InventoryGui.instance.m_container)
            {
                return;
            }

            InventoryGui.instance.SetupDragItem(null, null, 0);

            UserConfig playerConfig = UserConfig.GetPlayerConfig(player.GetPlayerID());

            bool includeHotbar = GeneralConfig.OverrideHotkeyBarBehavior.Value != OverrideHotkeyBarBehavior.NeverAffectHotkeyBar && QuickStackConfig.QuickStackIncludesHotkeyBar.Value;

            List<ItemData> quickStackables = player.m_inventory.m_inventory.Where((itm) => ShouldQuickStackItem(itm, playerConfig, player.m_inventory.GetHeight(), player.m_inventory.GetWidth(), includeHotbar)).ToList();

            if (quickStackables.Count == 0 && QuickStackConfig.ShowQuickStackResultMessage.Value)
            {
                player.Message(MessageHud.MessageType.Center, LocalizationConfig.GetRelevantTranslation(LocalizationConfig.QuickStackResultMessageNothing, nameof(LocalizationConfig.QuickStackResultMessageNothing)), 0, null);
                return;
            }

            // sort in reverse, because we iterate in reverse
            quickStackables.Sort((ItemData a, ItemData b) => -1 * Helper.CompareSlotOrder(a.m_gridPos, b.m_gridPos));

            List<ItemData> trophies = null;

            if (QuickStackConfig.QuickStackTrophiesIntoSameContainer.Value)
            {
                trophies = new List<ItemData>();

                for (int i = quickStackables.Count - 1; i >= 0; i--)
                {
                    var item = quickStackables[i];

                    if (item.m_shared.m_itemType == ItemData.ItemType.Trophy)
                    {
                        quickStackables.RemoveAt(i);
                        // add at beginning to keep the same order of the already sorted list
                        trophies.Insert(0, item);
                    }
                }
            }

            int movedCount = 0;
            Container currentContainer = currentContainerOverride ? currentContainerOverride : InventoryGui.instance.m_currentContainer;

            if (currentContainer != null)
            {
                movedCount = QuickStackIntoThisContainer(trophies, quickStackables, player.m_inventory, currentContainer.m_inventory);
            }

            if (onlyQuickStackToCurrentContainer || !ShouldAreaQuickStack(currentContainer))
            {
                ReportQuickStackResult(player, movedCount);
                return;
            }

            List<Container> containers = ContainerFinder.FindContainersInRange(player.transform.position, QuickStackConfig.QuickStackToNearbyRange.Value);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            if (containers.Count > 0)
            {
                movedCount += QuickStackIntoMultipleContainers(trophies, quickStackables, player, containers);
            }

            sw.Stop();
            Helper.Log($"Quick stacking time: {sw.Elapsed}", DebugSeverity.AlsoSpeedTests);

            ReportQuickStackResult(player, movedCount);
        }

        private static int QuickStackIntoThisContainer(List<ItemData> trophies, List<ItemData> nonTrophies, Inventory playerInventory, Inventory container, bool callPlayerInvChanged = true)
        {
            int movedStackCount = 0;

            Helper.Log($"Starting quick stack: inventory count: {playerInventory.m_inventory.Count}, container count: {container.m_inventory.Count}", DebugSeverity.Everything);

            if (QuickStackConfig.QuickStackTrophiesIntoSameContainer.Value && trophies?.Count > 0)
            {
                for (int i = container.m_inventory.Count - 1; i >= 0; i--)
                {
                    var containerItem = container.m_inventory[i];

                    if (containerItem.m_shared.m_itemType != ItemData.ItemType.Trophy)
                    {
                        continue;
                    }

                    for (int j = trophies.Count - 1; j >= 0; j--)
                    {
                        var playerItem = trophies[j];

                        if (container.AddItem(playerItem))
                        {
                            playerInventory.RemoveItem(playerItem);
                            trophies.RemoveAt(j);
                            movedStackCount++;
                        }
                    }

                    break;
                }
            }

            if (nonTrophies?.Count > 0)
            {
                for (int i = container.m_inventory.Count - 1; i >= 0; i--)
                {
                    var containerItem = container.m_inventory[i];

                    for (int j = nonTrophies.Count - 1; j >= 0; j--)
                    {
                        var playerItem = nonTrophies[j];

                        // don't check for quality or custom data, we want to quick stack solely based on name, and AddItem will figure out the rest
                        if (containerItem.m_shared.m_name != playerItem.m_shared.m_name)
                        {
                            continue;
                        }

                        if (container.AddItem(playerItem))
                        {
                            playerInventory.RemoveItem(playerItem);
                            nonTrophies.RemoveAt(j);
                            movedStackCount++;
                        }
                    }
                }
            }

            Helper.Log($"Finished quick stack: Removed {movedStackCount} stacks (remember that these merge with non full stacks in the container first). Inventory count: {playerInventory.m_inventory.Count}, container count: {container.m_inventory.Count}", DebugSeverity.Everything);

            if (callPlayerInvChanged)
            {
                playerInventory.Changed();
            }

            return movedStackCount;
        }

        private static int QuickStackIntoMultipleContainers(List<ItemData> trophies, List<ItemData> nonTrophies, Player player, List<Container> containers)
        {
            int movedStackCount = 0;

            bool isSinglePlayer = AreaStackRestockHelper.IsTrueSingleplayer();

            foreach (Container container in containers)
            {
                if (!AreaStackRestockHelper.ShouldAffectNonOwnerContainer(container, player.GetPlayerID(), isSinglePlayer))
                {
                    continue;
                }

                if (CompatibilitySupport.HasPlugin(CompatibilitySupport.multiUserChest))
                {
                    movedStackCount += QuickStackIntoThisContainer(trophies, nonTrophies, player.m_inventory, container.m_inventory, false);
                }
                else
                {
                    container.m_nview.ClaimOwnership();

                    AreaStackRestockHelper.SetNonMUCContainerInUse(container, true);

                    movedStackCount += QuickStackIntoThisContainer(trophies, nonTrophies, player.m_inventory, container.m_inventory, false);

                    AreaStackRestockHelper.SetNonMUCContainerInUse(container, false);
                }
            }

            player.m_inventory.Changed();

            return movedStackCount;
        }

        public static void ReportQuickStackResult(Player player, int movedCount)
        {
            if (!QuickStackConfig.ShowQuickStackResultMessage.Value)
            {
                return;
            }

            string message;

            if (movedCount == 0)
            {
                message = LocalizationConfig.GetRelevantTranslation(LocalizationConfig.QuickStackResultMessageNone, nameof(LocalizationConfig.QuickStackResultMessageNone));
            }
            else if (movedCount == 1)
            {
                message = LocalizationConfig.GetRelevantTranslation(LocalizationConfig.QuickStackResultMessageOne, nameof(LocalizationConfig.QuickStackResultMessageOne));
            }
            else
            {
                message = string.Format(LocalizationConfig.GetRelevantTranslation(LocalizationConfig.QuickStackResultMessageMore, nameof(LocalizationConfig.QuickStackResultMessageMore)), movedCount);
            }

            player.Message(MessageHud.MessageType.Center, message, 0, null);
        }
    }

    [HarmonyPatch(typeof(Container))]
    public static class StackAllPatch
    {
        [HarmonyPatch(nameof(Container.RPC_StackResponse)), HarmonyPrefix]
        public static bool ContainerStackAllPatch(Container __instance, bool granted)
        {
            if (!QuickStackConfig.ChangeHoldToStackFeatureToUseModdedQuickStackingLogic.Value)
            {
                // call original method
                return true;
            }

            if (!Player.m_localPlayer)
            {
                return false;
            }

            if (granted)
            {
                QuickStackModule.DoQuickStack(Player.m_localPlayer, true, __instance);
            }
            else
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse", 0, null);
            }

            return false;
        }
    }
}