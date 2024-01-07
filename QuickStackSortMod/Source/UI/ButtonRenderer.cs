using BepInEx;
using BepInEx.Configuration;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static QuickStackStore.CompatibilitySupport;
using static QuickStackStore.QSSConfig;

namespace QuickStackStore
{
    internal class ButtonRenderer
    {
        internal static bool hasOpenedInventoryOnce = false;
        internal static float origButtonLength = -1;
        internal static Vector3 origButtonPosition;

        internal static TextMeshProUGUI favoritingTogglingButtonText;

        internal static Button favoritingTogglingButton;
        internal static Button quickStackAreaButton;
        internal static Button sortInventoryButton;
        internal static Button restockAreaButton;

        internal static Button quickStackToContainerButton;
        internal static Button storeAllButton;
        internal static Button sortContainerButton;
        internal static Button restockFromContainerButton;

        private const float shrinkFactor = 0.9f;
        private const int vPadding = 8;
        private const int hAlign = 1;

        internal class MainButtonUpdate
        {
            internal static void UpdateInventoryGuiButtons(InventoryGui __instance)
            {
                if (!hasOpenedInventoryOnce)
                {
                    return;
                }

                if (__instance != InventoryGui.instance)
                {
                    return;
                }

                if (Player.m_localPlayer)
                {
                    // reset in case player forgot to turn it off
                    FavoritingMode.HasCurrentlyToggledFavoriting = false;

                    AutoSortBehavior autoSortBehavior = SortConfig.AutoSort.Value;

                    if (autoSortBehavior == AutoSortBehavior.SortPlayerInventoryOnOpen || autoSortBehavior == AutoSortBehavior.Both)
                    {
                        SortModule.SortPlayerInv(Player.m_localPlayer.m_inventory, UserConfig.GetPlayerConfig(Player.m_localPlayer.GetPlayerID()));
                    }

                    if (__instance.m_currentContainer && (autoSortBehavior == AutoSortBehavior.SortContainerOnOpen || autoSortBehavior == AutoSortBehavior.Both))
                    {
                        SortModule.SortContainer(__instance.m_currentContainer);
                    }
                }

                // if AUGA is installed, the 'take all' button might not exist
                if (!__instance.m_takeAllButton || !__instance.m_takeAllButton.TryGetComponent(out RectTransform takeAllButtonRect))
                {
                    return;
                }

                ControllerButtonHintHelper.FixTakeAllButtonControllerHint(__instance);

                if (origButtonLength == -1)
                {
                    origButtonLength = takeAllButtonRect.sizeDelta.x;
                    origButtonPosition = takeAllButtonRect.localPosition;
                }

                // intentionally not checking "ShouldBlockChangesToTakeAllButton", because then everything would look stupid
                if (takeAllButtonRect.sizeDelta.x == origButtonLength)
                {
                    takeAllButtonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, origButtonLength * shrinkFactor);
                }

                int extraContainerButtons = 0;

                bool displayStoreAllButton = StoreTakeAllConfig.DisplayStoreAllButton.Value;
                ShowTwoButtons displayQuickStackButtons = QuickStackConfig.DisplayQuickStackButtons.Value;
                ShowTwoButtons displayRestockButtons = RestockConfig.DisplayRestockButtons.Value;
                ShowTwoButtons displaySortButtons = SortConfig.DisplaySortButtons.Value;

                if (GeneralConfig.OverrideButtonDisplay.Value != OverrideButtonDisplay.DisableAllNewButtons)
                {
                    if (displayStoreAllButton)
                    {
                        extraContainerButtons++;
                    }

                    if (displayQuickStackButtons.ShouldSpawnContainerButton())
                    {
                        extraContainerButtons++;
                    }

                    if (displayRestockButtons.ShouldSpawnContainerButton())
                    {
                        extraContainerButtons++;
                    }

                    if (displaySortButtons.ShouldSpawnContainerButton())
                    {
                        extraContainerButtons++;
                    }
                }

                float vOffset = takeAllButtonRect.sizeDelta.y + vPadding;

                Vector3 startOffset = takeAllButtonRect.localPosition;

                if (takeAllButtonRect.localPosition == origButtonPosition)
                {
                    if (extraContainerButtons <= 1)
                    {
                        // move the button to the left by half of its removed length
                        startOffset -= new Vector3((origButtonLength / 2) * (1 - shrinkFactor), 0);
                    }
                    else
                    {
                        startOffset = OppositePositionOfTakeAllButton();

                        bool goToTop = !displayQuickStackButtons.ShouldSpawnContainerButton();
                        startOffset += new Vector3(origButtonLength - hAlign, goToTop ? 0 : -vOffset);
                    }

                    if (!ShouldBlockChangesToTakeAllButton())
                    {
                        takeAllButtonRect.localPosition = startOffset;
                    }
                }

                if (GeneralConfig.OverrideButtonDisplay.Value == OverrideButtonDisplay.DisableAllNewButtons)
                {
                    return;
                }

                int miniButtons = 0;

                Transform weight = __instance.m_player.transform.Find("Weight");

                RandyStatus randyStatus = HasRandyPlugin();

                if (displaySortButtons.ShouldSpawnMiniInventoryButton())
                {
                    // this one is deliberately unaffected by the randy equipment slot compatibility
                    bool shouldShow = displaySortButtons.ShouldShowMiniInventoryButton(__instance);

                    if (sortInventoryButton == null)
                    {
                        sortInventoryButton = CreateMiniButton(__instance, nameof(sortInventoryButton), KeybindChecker.joySort);
                        sortInventoryButton.gameObject.SetActive(shouldShow);

                        if (shouldShow)
                        {
                            __instance.StartCoroutine(WaitAFrameToRepositionMiniButton(__instance, sortInventoryButton.transform, weight, ++miniButtons, randyStatus));
                        }

                        sortInventoryButton.onClick.AddListener(new UnityAction(() => SortModule.SortPlayerInv(Player.m_localPlayer.m_inventory, UserConfig.GetPlayerConfig(Player.m_localPlayer.GetPlayerID()))));
                    }
                    else
                    {
                        sortInventoryButton.gameObject.SetActive(shouldShow);

                        if (shouldShow)
                        {
                            RepositionMiniButton(__instance, sortInventoryButton.transform, weight, ++miniButtons, randyStatus);
                        }
                    }
                }

                bool allowAreaButtons = AllowAreaStackingRestocking();

                if (allowAreaButtons && displayRestockButtons.ShouldSpawnMiniInventoryButton() && RestockConfig.RestockFromNearbyRange.Value > 0)
                {
                    bool shouldShow = displayRestockButtons.ShouldShowMiniInventoryButton(__instance) && !(__instance.m_currentContainer && randyStatus == RandyStatus.EnabledWithQuickSlots);

                    if (restockAreaButton == null)
                    {
                        restockAreaButton = CreateMiniButton(__instance, nameof(restockAreaButton), KeybindChecker.joyRestock);
                        restockAreaButton.gameObject.SetActive(shouldShow);

                        if (shouldShow)
                        {
                            __instance.StartCoroutine(WaitAFrameToRepositionMiniButton(__instance, restockAreaButton.transform, weight, ++miniButtons, randyStatus));
                        }

                        restockAreaButton.onClick.AddListener(new UnityAction(() => RestockModule.DoRestock(Player.m_localPlayer)));
                    }
                    else
                    {
                        restockAreaButton.gameObject.SetActive(shouldShow);

                        if (shouldShow)
                        {
                            RepositionMiniButton(__instance, restockAreaButton.transform, weight, ++miniButtons, randyStatus);
                        }
                    }
                }

                if (allowAreaButtons && displayQuickStackButtons.ShouldSpawnMiniInventoryButton() && QuickStackConfig.QuickStackToNearbyRange.Value > 0)
                {
                    bool shouldShow = displayQuickStackButtons.ShouldShowMiniInventoryButton(__instance) && !(__instance.m_currentContainer && randyStatus == RandyStatus.EnabledWithQuickSlots);

                    if (quickStackAreaButton == null)
                    {
                        quickStackAreaButton = CreateMiniButton(__instance, nameof(quickStackAreaButton), KeybindChecker.joyQuickStack);
                        quickStackAreaButton.gameObject.SetActive(shouldShow);

                        if (shouldShow)
                        {
                            __instance.StartCoroutine(WaitAFrameToRepositionMiniButton(__instance, quickStackAreaButton.transform, weight, ++miniButtons, randyStatus));
                        }

                        quickStackAreaButton.onClick.AddListener(new UnityAction(() => QuickStackModule.DoQuickStack(Player.m_localPlayer)));
                    }
                    else
                    {
                        quickStackAreaButton.gameObject.SetActive(shouldShow);

                        if (shouldShow)
                        {
                            RepositionMiniButton(__instance, quickStackAreaButton.transform, weight, ++miniButtons, randyStatus);
                        }
                    }
                }

                FavoritingToggling displayFavoriteToggleButton = FavoriteConfig.DisplayFavoriteToggleButton.Value;

                if (displayFavoriteToggleButton != FavoritingToggling.Disabled)
                {
                    int index;
                    Transform parent;

                    if (displayFavoriteToggleButton == FavoritingToggling.EnabledBottomButton)
                    {
                        index = ++miniButtons;
                        parent = weight;
                    }
                    else
                    {
                        index = -1;
                        parent = __instance.m_player.transform.Find("Armor");
                    }

                    if (favoritingTogglingButton == null)
                    {
                        favoritingTogglingButton = CreateMiniButton(__instance, nameof(favoritingTogglingButton), KeybindChecker.joyFavoriteToggling);
                        favoritingTogglingButton.gameObject.SetActive(true);

                        favoritingTogglingButtonText = favoritingTogglingButton.transform.Find("Text").GetComponent<TextMeshProUGUI>();

                        // trigger text reset without changing value
                        FavoritingMode.RefreshDisplay();

                        __instance.StartCoroutine(WaitAFrameToRepositionMiniButton(__instance, favoritingTogglingButton.transform, parent, index, randyStatus));

                        favoritingTogglingButton.onClick.AddListener(new UnityAction(() => FavoritingMode.ToggleFavoriteToggling()));
                    }
                    else
                    {
                        RepositionMiniButton(__instance, favoritingTogglingButton.transform, parent, index, randyStatus);
                    }
                }

                int buttonsBelowTakeAll = 0;

                if (displayQuickStackButtons.ShouldSpawnContainerButton())
                {
                    if (quickStackToContainerButton == null)
                    {
                        quickStackToContainerButton = CreateBigButton(__instance, nameof(quickStackToContainerButton), KeybindChecker.joyQuickStack);

                        if (ShouldBlockChangesToTakeAllButton() && randyStatus != RandyStatus.EnabledWithQuickSlots)
                        {
                            MoveButtonToIndex(ref quickStackToContainerButton, startOffset, 0, extraContainerButtons, 1);
                        }
                        else
                        {
                            // jump to the opposite side of the default 'take all' button position, because we are out of space due to randy's quickslots
                            bool forcePutAtOppositeOfTakeAll = randyStatus == RandyStatus.EnabledWithQuickSlots;

                            // revert the vertical movement from the 'take all' button
                            MoveButtonToIndex(ref quickStackToContainerButton, startOffset, -vOffset, extraContainerButtons, 1, forcePutAtOppositeOfTakeAll);
                        }

                        quickStackToContainerButton.onClick.AddListener(new UnityAction(() => QuickStackModule.DoQuickStack(Player.m_localPlayer, true)));
                    }

                    quickStackToContainerButton.gameObject.SetActive(displayQuickStackButtons.ShouldShowContainerButton(__instance));
                }

                if (displayStoreAllButton)
                {
                    if (storeAllButton == null)
                    {
                        storeAllButton = CreateBigButton(__instance, nameof(storeAllButton), KeybindChecker.joyStoreAll);
                        MoveButtonToIndex(ref storeAllButton, startOffset, vOffset, extraContainerButtons, ++buttonsBelowTakeAll);

                        storeAllButton.onClick.AddListener(new UnityAction(() => StoreTakeAllModule.StoreAllItemsInOrder(Player.m_localPlayer)));
                    }

                    storeAllButton.gameObject.SetActive(__instance.m_currentContainer != null);
                }

                if (displayRestockButtons.ShouldSpawnContainerButton())
                {
                    if (restockFromContainerButton == null)
                    {
                        restockFromContainerButton = CreateBigButton(__instance, nameof(restockFromContainerButton), KeybindChecker.joyRestock);
                        MoveButtonToIndex(ref restockFromContainerButton, startOffset, vOffset, extraContainerButtons, ++buttonsBelowTakeAll);

                        restockFromContainerButton.onClick.AddListener(new UnityAction(() => RestockModule.DoRestock(Player.m_localPlayer, true)));
                    }

                    restockFromContainerButton.gameObject.SetActive(displayRestockButtons.ShouldShowContainerButton(__instance));
                }

                if (displaySortButtons.ShouldSpawnContainerButton())
                {
                    if (sortContainerButton == null)
                    {
                        sortContainerButton = CreateBigButton(__instance, nameof(sortContainerButton), KeybindChecker.joySort);
                        MoveButtonToIndex(ref sortContainerButton, startOffset, vOffset, extraContainerButtons, ++buttonsBelowTakeAll);

                        sortContainerButton.onClick.AddListener(new UnityAction(() => SortModule.SortContainer(__instance.m_currentContainer)));
                    }

                    sortContainerButton.gameObject.SetActive(displaySortButtons.ShouldShowContainerButton(__instance));
                }

                if (!ShouldBlockChangesToTakeAllButton())
                {
                    takeAllButtonRect.gameObject.SetActive(__instance.m_currentContainer != null);
                }

                if (__instance.m_stackAllButton && __instance.m_stackAllButton.TryGetComponent(out RectTransform stackAllButtonRect))
                {
                    bool shouldShow = randyStatus != RandyStatus.EnabledWithQuickSlots && !QuickStackConfig.HideBaseGamePlaceStacksButton.Value;

                    stackAllButtonRect.gameObject.SetActive(shouldShow);
                }

                OnButtonTextTranslationSettingChanged(false);
            }
        }

        private static void MoveButtonToIndex(ref Button buttonToMove, Vector3 startVector, float vOffset, int visibleExtraButtons, int buttonsBelowTakeAll, bool forcePutAtOppositeOfTakeAll = false)
        {
            if (visibleExtraButtons == 1 || forcePutAtOppositeOfTakeAll)
            {
                buttonToMove.transform.localPosition = OppositePositionOfTakeAllButton();
            }
            else
            {
                buttonToMove.transform.localPosition = startVector;
                buttonToMove.transform.localPosition -= new Vector3(0, buttonsBelowTakeAll * vOffset);
            }
        }

        private static Vector3 OppositePositionOfTakeAllButton()
        {
            // move the button to the right by half of its removed length
            float scaleBased = (origButtonLength / 2) * (1 - shrinkFactor);
            return origButtonPosition + new Vector3(440f + scaleBased, 0f);
        }

        public static string SortCriteriaToShortHumanReadableString(SortCriteriaEnum sortingCriteria)
        {
            switch (sortingCriteria)
            {
                case SortCriteriaEnum.InternalName:
                    return LocalizationConfig.GetRelevantTranslation(LocalizationConfig.SortByInternalNameLabel, nameof(LocalizationConfig.SortByInternalNameLabel));

                case SortCriteriaEnum.TranslatedName:
                    return LocalizationConfig.GetRelevantTranslation(LocalizationConfig.SortByTranslatedNameLabel, nameof(LocalizationConfig.SortByTranslatedNameLabel));

                case SortCriteriaEnum.Value:
                    return LocalizationConfig.GetRelevantTranslation(LocalizationConfig.SortByValueLabel, nameof(LocalizationConfig.SortByValueLabel));

                case SortCriteriaEnum.Weight:
                    return LocalizationConfig.GetRelevantTranslation(LocalizationConfig.SortByWeightLabel, nameof(LocalizationConfig.SortByWeightLabel));

                case SortCriteriaEnum.Type:
                    return LocalizationConfig.GetRelevantTranslation(LocalizationConfig.SortByTypeLabel, nameof(LocalizationConfig.SortByTypeLabel));

                default:
                    return "invalid";
            }
        }

        private static Button CreateAbstractButton(InventoryGui instance, string name, string joyHint, Transform parent)
        {
            var gamepad = instance.m_takeAllButton.GetComponent<UIGamePad>();
            bool wasEnabled = gamepad.enabled;

            gamepad.enabled = false;
            var button = Object.Instantiate(instance.m_takeAllButton, parent);
            gamepad.enabled = wasEnabled;

            button.name = name;

            button.onClick.RemoveAllListeners();

            instance.StartCoroutine(ControllerButtonHintHelper.WaitAFrameToSetupControllerHint(button, joyHint));

            return button;
        }

        private static Button CreateBigButton(InventoryGui instance, string name, string joyHint)
        {
            return CreateAbstractButton(instance, name, joyHint, instance.m_takeAllButton.transform.parent);
        }

        private const int miniButtonSize = 38;
        private const int miniButtonHPadding = 2;
        private const float normalMiniButtonVOffset = -56f;
        private const float lowerMiniButtonVOffset = -75f;

        private static Button CreateMiniButton(InventoryGui instance, string name, string joyHint)
        {
            var button = CreateAbstractButton(instance, name, joyHint, instance.m_player.transform);

            RectTransform rect = (RectTransform)button.transform;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, miniButtonSize);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, miniButtonSize);

            return button;
        }

        private static void RepositionMiniButton(InventoryGui instance, Transform button, Transform weight, int existingMiniButtons, RandyStatus randyStatus)
        {
            if (existingMiniButtons == -1)
            {
                button.localPosition = weight.localPosition + new Vector3(hAlign, 70f);
                return;
            }

            float distanceToMove = (miniButtonSize + miniButtonHPadding) * (existingMiniButtons - 1);

            if (randyStatus == RandyStatus.EnabledWithQuickSlots)
            {
                button.localPosition = weight.localPosition + new Vector3(hAlign, -distanceToMove + normalMiniButtonVOffset);
            }
            else
            {
                bool shouldMoveLower = HasPluginThatRequiresMiniButtonVMove() && (instance.m_player.Find("EquipmentBkg") != null || instance.m_player.Find("AzuEquipmentBkg") != null);
                shouldMoveLower |= randyStatus == RandyStatus.EnabledWithoutQuickSlots;

                float vPos = shouldMoveLower ? lowerMiniButtonVOffset : normalMiniButtonVOffset;

                button.localPosition = weight.localPosition + new Vector3(hAlign + distanceToMove, vPos);
            }
        }

        /// <summary>
        /// Wait for one frame, so the two Odin equipment slot mods can finish spawning the 'EquipmentBkg' object
        /// </summary>
        internal static IEnumerator WaitAFrameToRepositionMiniButton(InventoryGui instance, Transform button, Transform weight, int existingMiniButtons, RandyStatus randyStatus)
        {
            yield return null;
            RepositionMiniButton(instance, button, weight, existingMiniButtons, randyStatus);
        }

        /// <summary>
        /// Wait one frame for Destroy to finish, then reset UI
        /// </summary>
        internal static IEnumerator WaitAFrameToUpdateUIElements(InventoryGui instance, bool includeTrashButton)
        {
            yield return null;

            if (instance == null)
            {
                yield break;
            }

            MainButtonUpdate.UpdateInventoryGuiButtons(instance);

            if (includeTrashButton)
            {
                TrashModule.TrashItemsPatches.UpdateTrashCanUI(instance);
            }
        }

        internal static void OnButtonRelevantSettingChanged(BaseUnityPlugin plugin, bool includeTrashButton = false)
        {
            if (!hasOpenedInventoryOnce || !TrashModule.TrashItemsPatches.hasOpenedInventoryOnce)
            {
                return;
            }

            // reminder to never use ?. on monobehaviors

            if (InventoryGui.instance != null)
            {
                var takeAllButton = InventoryGui.instance.m_takeAllButton;

                if (takeAllButton != null)
                {
                    if (!ShouldBlockChangesToTakeAllButton())
                    {
                        takeAllButton.transform.localPosition = origButtonPosition;
                    }
                }
            }

            var buttons = new Button[] { storeAllButton, quickStackToContainerButton, sortContainerButton, restockFromContainerButton, sortInventoryButton, quickStackAreaButton, restockAreaButton, favoritingTogglingButton };

            foreach (var button in buttons)
            {
                if (button != null)
                {
                    Object.Destroy(button.gameObject);
                }
            }

            favoritingTogglingButtonText = null;

            if (includeTrashButton)
            {
                if (TrashModule.trashRoot != null)
                {
                    Object.Destroy(TrashModule.trashRoot.gameObject);
                }
            }

            plugin.StartCoroutine(WaitAFrameToUpdateUIElements(InventoryGui.instance, includeTrashButton));
        }

        private static void UpdateButtonTextTranslation(Button button, ConfigEntry<string> overrideConfig, string configName)
        {
            if (button != null)
            {
                var text = button.GetComponentInChildren<TextMeshProUGUI>();

                if (text != null)
                {
                    text.text = LocalizationConfig.GetRelevantTranslation(overrideConfig, configName);
                }
            }
        }

        internal static void OnButtonTextTranslationSettingChanged(bool includeTrashButton = true)
        {
            // reminder to never use ?. on monobehaviors

            if (InventoryGui.instance != null)
            {
                var takeAllButton = InventoryGui.instance.m_takeAllButton;

                if (takeAllButton != null)
                {
                    var text = takeAllButton.GetComponentInChildren<TextMeshProUGUI>();

                    if (text != null)
                    {
                        text.text = !LocalizationConfig.TakeAllLabel.Value.IsNullOrWhiteSpace() ? LocalizationConfig.TakeAllLabel.Value : Localization.instance.Translate(LocalizationConfig.takeAllKey);
                    }
                }
            }

            UpdateButtonTextTranslation(storeAllButton, LocalizationConfig.StoreAllLabel, nameof(LocalizationConfig.StoreAllLabel));
            UpdateButtonTextTranslation(quickStackToContainerButton, LocalizationConfig.QuickStackLabel, nameof(LocalizationConfig.QuickStackLabel));
            UpdateButtonTextTranslation(restockFromContainerButton, LocalizationConfig.RestockLabel, nameof(LocalizationConfig.RestockLabel));
            UpdateButtonTextTranslation(sortInventoryButton, LocalizationConfig.SortLabelCharacter, nameof(LocalizationConfig.SortLabelCharacter));
            UpdateButtonTextTranslation(quickStackAreaButton, LocalizationConfig.QuickStackLabelCharacter, nameof(LocalizationConfig.QuickStackLabelCharacter));
            UpdateButtonTextTranslation(restockAreaButton, LocalizationConfig.RestockLabelCharacter, nameof(LocalizationConfig.RestockLabelCharacter));

            if (sortContainerButton != null)
            {
                var text = sortContainerButton.GetComponentInChildren<TextMeshProUGUI>();

                if (text != null)
                {
                    string label = LocalizationConfig.GetRelevantTranslation(LocalizationConfig.SortLabel, nameof(LocalizationConfig.SortLabel));

                    if (SortConfig.DisplaySortCriteriaInLabel.Value)
                    {
                        label += $" ({SortCriteriaToShortHumanReadableString(SortConfig.SortCriteria.Value)})";
                    }

                    text.text = label;
                }
            }

            if (includeTrashButton && TrashModule.trashButton != null)
            {
                var text = TrashModule.trashButton.GetComponentInChildren<TextMeshProUGUI>();

                if (text != null)
                {
                    text.text = LocalizationConfig.GetRelevantTranslation(LocalizationConfig.TrashLabel, nameof(LocalizationConfig.TrashLabel));
                }
            }
        }
    }
}