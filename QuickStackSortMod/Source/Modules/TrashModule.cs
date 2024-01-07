using HarmonyLib;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static QuickStackStore.QSSConfig;
using Object = UnityEngine.Object;

namespace QuickStackStore
{
    // base implementation originally from 'Trash Items' mod, as allowed in their permission settings on nexus
    // https://www.nexusmods.com/valheim/mods/441
    // https://github.com/virtuaCode/valheim-mods/tree/main/TrashItems
    internal class TrashModule
    {
        private static ClickState clickState = 0;

        public static Sprite trashSprite;
        public static Sprite bgSprite;
        public static GameObject dialog;
        public static Transform trashRoot;
        public static TrashButton trashButton;

        private static void DoQuickTrash()
        {
            if (TrashConfig.ShowConfirmDialogForQuickTrash.Value)
            {
                ShowBaseConfirmDialog(null, LocalizationConfig.GetRelevantTranslation(LocalizationConfig.QuickTrashConfirmation, nameof(LocalizationConfig.QuickTrashConfirmation)), string.Empty, QuickTrash);
            }
            else
            {
                QuickTrash();
            }
        }

        private static void QuickTrash()
        {
            var player = Player.m_localPlayer;
            UserConfig playerConfig = UserConfig.GetPlayerConfig(player.GetPlayerID());

            int num = 0;

            var list = player.m_inventory.m_inventory;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var item = list[i];

                if (item.m_gridPos.y == 0 && (GeneralConfig.OverrideHotkeyBarBehavior.Value == OverrideHotkeyBarBehavior.NeverAffectHotkeyBar || !TrashConfig.TrashingCanAffectHotkeyBar.Value))
                {
                    continue;
                }

                if (!playerConfig.IsSlotFavorited(item.m_gridPos) && playerConfig.IsItemNameConsideredTrashFlagged(item.m_shared))
                {
                    num++;
                    player.RemoveEquipAction(item);
                    player.UnequipItem(item, false);
                    player.m_inventory.RemoveItem(item);
                }
            }

            InventoryGui.instance.SetupDragItem(null, null, 0);
            InventoryGui.instance.UpdateCraftingPanel(false);

            Helper.Log($"Quick trashed {num} item/s from player inventory");

            player.m_inventory.Changed();
        }

        private static void TrashItem(InventoryGui __instance, Inventory ___m_dragInventory, ItemDrop.ItemData ___m_dragItem, int ___m_dragAmount)
        {
            if (___m_dragAmount == ___m_dragItem.m_stack)
            {
                Player.m_localPlayer.RemoveEquipAction(___m_dragItem);
                Player.m_localPlayer.UnequipItem(___m_dragItem, false);
                ___m_dragInventory.RemoveItem(___m_dragItem);
            }
            else
            {
                ___m_dragInventory.RemoveItem(___m_dragItem, ___m_dragAmount);
            }

            __instance.SetupDragItem(null, null, 0);
            __instance.UpdateCraftingPanel(false);
        }

        [HarmonyPatch(typeof(InventoryGui))]
        internal static class TrashItemsPatches
        {
            internal static bool hasOpenedInventoryOnce = false;

            // slightly lower priority so we get rendered on top of equipment slot mods
            // (lower priority -> later rendering -> you get rendered on top)
            [HarmonyPriority(Priority.LowerThanNormal)]
            [HarmonyPatch(nameof(InventoryGui.Show)), HarmonyPostfix]
            private static void Show_Postfix(InventoryGui __instance)
            {
                hasOpenedInventoryOnce = true;

                UpdateTrashCanUI(__instance);
            }

            internal static void UpdateTrashCanUI(InventoryGui __instance)
            {
                if (!hasOpenedInventoryOnce)
                {
                    return;
                }

                if (__instance != InventoryGui.instance)
                {
                    return;
                }

                if (trashRoot != null)
                {
                    return;
                }

                if (CompatibilitySupport.DisallowAllTrashCanFeatures())
                {
                    return;
                }

                if (CompatibilitySupport.HasPlugin(CompatibilitySupport.auga))
                {
                    return;
                }

                if (GeneralConfig.OverrideButtonDisplay.Value == OverrideButtonDisplay.DisableAllNewButtons)
                {
                    return;
                }

                if (!TrashConfig.DisplayTrashCanUI.Value)
                {
                    return;
                }

                Transform playerInventory = __instance.m_player.transform;

                Transform armor = playerInventory.Find("Armor");
                trashRoot = Object.Instantiate(armor, playerInventory);
                // fix rendering order by going to the right place in the hierachy
                trashRoot.SetSiblingIndex(armor.GetSiblingIndex() + 1);
                trashButton = trashRoot.gameObject.AddComponent<TrashButton>();
            }

            [HarmonyPatch(nameof(InventoryGui.Hide)), HarmonyPostfix]
            private static void Hide_Postfix()
            {
                OnChoice();
            }

            [HarmonyPatch(nameof(InventoryGui.UpdateItemDrag)), HarmonyPostfix]
            private static void UpdateItemDrag_Postfix(InventoryGui __instance, ItemDrop.ItemData ___m_dragItem, Inventory ___m_dragInventory, int ___m_dragAmount)
            {
                if (dialog != null || clickState == 0)
                {
                    return;
                }

                if (___m_dragItem == null)
                {
                    if (clickState == ClickState.ClickedQuickTrash)
                    {
                        DoQuickTrash();
                    }

                    clickState = 0;
                    return;
                }

                if (___m_dragInventory == null || !___m_dragInventory.ContainsItem(___m_dragItem))
                {
                    clickState = 0;
                    return;
                }

                var player = Player.m_localPlayer;
                var playerConfig = UserConfig.GetPlayerConfig(player.GetPlayerID());

                if (clickState == ClickState.ClickedTrashFlagging)
                {
                    bool didFlagSuccessfully = playerConfig.ToggleItemNameTrashFlagging(InventoryGui.instance.m_dragItem.m_shared);

                    if (!didFlagSuccessfully)
                    {
                        player.Message(MessageHud.MessageType.Center, LocalizationConfig.GetRelevantTranslation(LocalizationConfig.CantTrashFlagFavoritedItemWarning, nameof(LocalizationConfig.CantTrashFlagFavoritedItemWarning)), 0, null);
                    }

                    clickState = 0;
                    return;
                }

                if (clickState == ClickState.ClickedTrash)
                {
                    if (player.m_inventory == ___m_dragInventory && ___m_dragItem.m_gridPos.y == 0 && (GeneralConfig.OverrideHotkeyBarBehavior.Value == OverrideHotkeyBarBehavior.NeverAffectHotkeyBar || !TrashConfig.TrashingCanAffectHotkeyBar.Value))
                    {
                        player.Message(MessageHud.MessageType.Center, LocalizationConfig.GetRelevantTranslation(LocalizationConfig.CantTrashHotkeyBarItemWarning, nameof(LocalizationConfig.CantTrashHotkeyBarItemWarning)), 0, null);
                        clickState = 0;
                        return;
                    }

                    if ((player.m_inventory == ___m_dragInventory && playerConfig.IsSlotFavorited(___m_dragItem.m_gridPos)) || playerConfig.IsItemNameFavorited(___m_dragItem.m_shared))
                    {
                        player.Message(MessageHud.MessageType.Center, LocalizationConfig.GetRelevantTranslation(LocalizationConfig.CantTrashFavoritedItemWarning, nameof(LocalizationConfig.CantTrashFavoritedItemWarning)), 0, null);
                        clickState = 0;
                        return;
                    }

                    var showConfirmDialogForNormalItem = TrashConfig.ShowConfirmDialogForNormalItem.Value;

                    if (showConfirmDialogForNormalItem == ShowConfirmDialogOption.Always
                        || (showConfirmDialogForNormalItem == ShowConfirmDialogOption.WhenNotTrashFlagged && !playerConfig.IsItemNameConsideredTrashFlagged(___m_dragItem.m_shared)))
                    {
                        ShowConfirmDialog(___m_dragItem, ___m_dragAmount, () => TrashItem(__instance, ___m_dragInventory, ___m_dragItem, ___m_dragAmount));
                    }
                    else
                    {
                        TrashItem(__instance, ___m_dragInventory, ___m_dragItem, ___m_dragAmount);
                    }

                    clickState = 0;
                    return;
                }
            }
        }

        public class TrashButton : MonoBehaviour
        {
            protected Canvas canvas;
            protected GraphicRaycaster raycaster;
            protected RectTransform rectTransform;
            protected GameObject buttonGo;

            protected void Awake()
            {
                if (InventoryGui.instance == null)
                {
                    return;
                }

                var playerInventory = InventoryGui.instance.m_player.transform;
                RectTransform rect = GetComponent<RectTransform>();
                rect.anchoredPosition -= new Vector2(0, 78);

                Transform tText = transform.Find("ac_text");
                Transform tArmor = transform.Find("armor_icon");

                if (!tText || !tArmor)
                {
                    if (!tText)
                    {
                        Helper.LogO("ac_text not found!", DebugLevel.Warning);
                    }

                    if (!tArmor)
                    {
                        Helper.LogO("armor_icon not found!", DebugLevel.Warning);
                    }

                    Helper.LogO("If you are using Better UI Reforged, this happens the moment you edit the config to reenable the trash can UI while already ingame. A simple log out to main menu and log back in will fix this, don't worry about it. If not, please report this on the mod page or on github.", DebugLevel.Warning);

                    return;
                }

                tText.GetComponent<TextMeshProUGUI>().text = LocalizationConfig.GetRelevantTranslation(LocalizationConfig.TrashLabel, nameof(LocalizationConfig.TrashLabel));
                tText.GetComponent<TextMeshProUGUI>().color = TrashConfig.TrashLabelColor.Value;

                // this fixes that the left most letter wants to go below the inventory wood panel if the label text is too long
                tText.GetComponent<RectTransform>().sizeDelta -= new Vector2(9, 0);

                // Replace armor with trash icon
                tArmor.GetComponent<Image>().sprite = trashSprite;

                transform.gameObject.name = "Trash";

                buttonGo = new GameObject("ButtonCanvas");
                rectTransform = buttonGo.AddComponent<RectTransform>();
                rectTransform.transform.SetParent(transform.transform, true);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(70, 74);

                // Add canvas and raycaster for DelayedOverrideSorting
                canvas = buttonGo.AddComponent<Canvas>();
                raycaster = buttonGo.AddComponent<GraphicRaycaster>();

                // Add trash ui button
                Button button = buttonGo.AddComponent<Button>();
                button.onClick.AddListener(() => TrashOrTrashFlagItem());

                ControllerButtonHintHelper.AddControllerTooltipToTrashCan(button, tArmor);

                // Add invisible image so we become clickable
                var image = buttonGo.AddComponent<Image>();
                image.color = new Color(0, 0, 0, 0);

                // Add border background
                Transform frames = playerInventory.Find("selected_frame");
                GameObject newFrame = Instantiate(frames.GetChild(0).gameObject, transform);
                newFrame.GetComponent<Image>().sprite = bgSprite;
                newFrame.transform.SetAsFirstSibling();
                newFrame.GetComponent<RectTransform>().sizeDelta = new Vector2(-8, 22);
                newFrame.GetComponent<RectTransform>().anchoredPosition = new Vector2(6, 7.5f);

                gameObject.AddComponent<TrashFrameHandler>().frame = newFrame;
            }

            protected void OnEnable()
            {
                StartCoroutine(DelayedOverrideSorting());
            }

            private IEnumerator DelayedOverrideSorting()
            {
                yield return null;

                if (canvas == null)
                {
                    yield break;
                }

                canvas.overrideSorting = true;
                canvas.sortingOrder = 1;
            }
        }

        private static void ShowConfirmDialog(ItemDrop.ItemData item, int itemAmount, UnityAction onConfirm)
        {
            ShowBaseConfirmDialog(item.GetIcon(),
                Localization.instance.Localize(item.m_shared.m_name),
                $"{itemAmount}/{item.m_shared.m_maxStackSize}",
                onConfirm);
        }

        private static void ShowBaseConfirmDialog(Sprite potentialIcon, string name, string amountText, UnityAction onConfirm)
        {
            if (InventoryGui.instance == null || dialog != null)
            {
                return;
            }

            dialog = Object.Instantiate(InventoryGui.instance.m_splitPanel.gameObject, InventoryGui.instance.transform);

            var okButton = dialog.transform.Find("win_bkg/Button_ok").GetComponent<Button>();
            okButton.onClick.RemoveAllListeners();
            okButton.onClick.AddListener(new UnityAction(OnChoice));
            okButton.onClick.AddListener(onConfirm);
            okButton.GetComponentInChildren<TextMeshProUGUI>().text = LocalizationConfig.GetRelevantTranslation(LocalizationConfig.TrashConfirmationOkayButton, nameof(LocalizationConfig.TrashConfirmationOkayButton));
            okButton.GetComponentInChildren<TextMeshProUGUI>().color = new Color(1, 0.2f, 0.1f);

            var cancelButton = dialog.transform.Find("win_bkg/Button_cancel").GetComponent<Button>();
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(new UnityAction(OnChoice));

            dialog.transform.Find("win_bkg/Slider").gameObject.SetActive(false);

            var text = dialog.transform.Find("win_bkg/Text").GetComponent<TextMeshProUGUI>();
            text.text = name;

            var iconComp = dialog.transform.Find("win_bkg/Icon_bkg/Icon").GetComponent<Image>();

            if (potentialIcon)
            {
                iconComp.sprite = potentialIcon;
            }
            else
            {
                iconComp.sprite = trashSprite;
            }

            var amountComp = dialog.transform.Find("win_bkg/amount").GetComponent<TextMeshProUGUI>();

            amountComp.text = amountText;

            dialog.gameObject.SetActive(true);
        }

        private static void OnChoice()
        {
            clickState = 0;

            if (dialog != null)
            {
                Object.Destroy(dialog);
                dialog = null;
            }
        }

        public static void TrashOrTrashFlagItem(bool usedFromHotkey = false)
        {
            if (CompatibilitySupport.DisallowAllTrashCanFeatures())
            {
                return;
            }

            Helper.Log("Trash Item called!");

            if (clickState != ClickState.None || InventoryGui.instance == null)
            {
                return;
            }

            if (InventoryGui.instance.m_dragGo != null)
            {
                if (FavoritingMode.IsInFavoritingMode())
                {
                    clickState = ClickState.ClickedTrashFlagging;
                }
                else
                {
                    clickState = ClickState.ClickedTrash;
                }
            }
            else
            {
                if (!usedFromHotkey && TrashConfig.EnableQuickTrash.Value && !FavoritingMode.IsInFavoritingMode())
                {
                    clickState = ClickState.ClickedQuickTrash;
                }
            }
        }

        public static void AttemptQuickTrash()
        {
            if (CompatibilitySupport.DisallowAllTrashCanFeatures())
            {
                return;
            }

            Helper.Log("Quick Trash Item called!");

            if (clickState != ClickState.None || InventoryGui.instance == null || InventoryGui.instance.m_dragGo != null)
            {
                return;
            }

            if (TrashConfig.EnableQuickTrash.Value && !FavoritingMode.IsInFavoritingMode())
            {
                clickState = ClickState.ClickedQuickTrash;
            }
        }

        private enum ClickState
        {
            None = 0,
            ClickedTrash = 1,
            ClickedTrashFlagging = 2,
            ClickedQuickTrash = 3
        }
    }

    public class TrashFrameHandler : MonoBehaviour
    {
        internal GameObject frame;

        protected void Update()
        {
            frame.SetActive(ZInput.IsGamepadActive() && InventoryGui.instance.m_activeGroup == 1);
        }
    }
}