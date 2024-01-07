using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ServerSync;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static QuickStackStore.ConfigurationManagerAttributes;
using static QuickStackStore.LocalizationConfig;
using static QuickStackStore.QSSConfig.ControllerConfig;
using static QuickStackStore.QSSConfig.FavoriteConfig;
using static QuickStackStore.QSSConfig.GeneralConfig;
using static QuickStackStore.QSSConfig.QuickStackConfig;
using static QuickStackStore.QSSConfig.QuickStackRestockConfig;
using static QuickStackStore.QSSConfig.RestockConfig;
using static QuickStackStore.QSSConfig.SortConfig;
using static QuickStackStore.QSSConfig.StoreTakeAllConfig;
using static QuickStackStore.QSSConfig.TrashConfig;

namespace QuickStackStore
{
    [HarmonyPatch(typeof(Player))]
    internal static class PlayerPatch
    {
        [HarmonyPatch(nameof(Player.Start))]
        [HarmonyPostfix]
        internal static void StartPatch(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                QSSConfig.ResetAllFavoritingData_SettingChanged(null, null);
            }
        }
    }

    internal class QSSConfig
    {
        public static ConfigFile Config;

        internal class GeneralConfig
        {
            public static ConfigEntry<ConfigTemplate> ConfigTemplate;
            public static ConfigEntry<OverrideButtonDisplay> OverrideButtonDisplay;
            public static ConfigEntry<OverrideKeybindBehavior> OverrideKeybindBehavior;
            public static ConfigEntry<OverrideHotkeyBarBehavior> OverrideHotkeyBarBehavior;
            public static ConfigEntry<bool> UseTopDownLogicForEverything;
        }

        internal class ControllerConfig
        {
            public static ConfigEntry<DPadUsage> ControllerDPadUsageInInventoryGrid;
            public static ConfigEntry<KeyboardShortcut> ControllerDPadUsageModifierKeybind;

            public static ConfigEntry<bool> RemoveControllerButtonHintFromTakeAllButton;
            public static ConfigEntry<bool> UseHardcodedControllerSupport;
        }

        internal class FavoriteConfig
        {
            public static ConfigEntry<Color> BorderColorFavoritedItem;
            public static ConfigEntry<Color> BorderColorFavoritedItemOnFavoritedSlot;
            public static ConfigEntry<Color> BorderColorFavoritedSlot;
            public static ConfigEntry<Color> BorderColorTrashFlaggedItem;
            public static ConfigEntry<Color> BorderColorTrashFlaggedItemOnFavoritedSlot;
            public static ConfigEntry<FavoritingToggling> DisplayFavoriteToggleButton;
            public static ConfigEntry<bool> DisplayTooltipHint;
            public static ConfigEntry<KeyboardShortcut> FavoritingModifierKeybind1;
            public static ConfigEntry<KeyboardShortcut> FavoritingModifierKeybind2;
            public static ConfigEntry<FavoriteToggleButtonStyle> FavoriteToggleButtonStyle;
        }

        internal class QuickStackRestockConfig
        {
            public static ConfigSync AreaStackRestockServerSync;
            public static ConfigEntry<bool> AllowAreaStackingInMultiplayerWithoutMUC;
            public static ConfigEntry<bool> AllowAreaStackingToNonPhysicalContainers;
            public static ConfigEntry<bool> AllowAreaStackingToPhysicalNonPlayerBuiltContainers;
            public static ConfigEntry<bool> SuppressContainerSoundAndVisuals;
            public static ConfigEntry<bool> ToggleAreaStackRestockConfigServerSync;
        }

        internal class QuickStackConfig
        {
            public static ConfigEntry<bool> ChangeHoldToStackFeatureToUseModdedQuickStackingLogic;
            public static ConfigEntry<ShowTwoButtons> DisplayQuickStackButtons;
            public static ConfigEntry<bool> HideBaseGamePlaceStacksButton;
            public static ConfigEntry<QuickStackBehavior> QuickStackHotkeyBehaviorWhenContainerOpen;
            public static ConfigEntry<bool> QuickStackIncludesHotkeyBar;
            public static ConfigEntry<KeyboardShortcut> QuickStackKeybind;
            public static ConfigEntry<float> QuickStackToNearbyRange;
            public static ConfigEntry<bool> QuickStackTrophiesIntoSameContainer;
            public static ConfigEntry<bool> ShowQuickStackResultMessage;
        }

        internal class RestockConfig
        {
            public static ConfigEntry<ShowTwoButtons> DisplayRestockButtons;
            public static ConfigEntry<float> RestockFromNearbyRange;
            public static ConfigEntry<RestockBehavior> RestockHotkeyBehaviorWhenContainerOpen;
            public static ConfigEntry<bool> RestockIncludesHotkeyBar;
            public static ConfigEntry<KeyboardShortcut> RestockKeybind;
            public static ConfigEntry<bool> RestockOnlyAmmoAndConsumables;
            public static ConfigEntry<bool> RestockOnlyFavoritedItems;

            public static ConfigEntry<int> RestockStackSizeLimitAmmo;
            public static ConfigEntry<int> RestockStackSizeLimitConsumables;
            public static ConfigEntry<int> RestockStackSizeLimitGeneral;

            public static ConfigEntry<bool> ShowRestockResultMessage;
        }

        internal class StoreTakeAllConfig
        {
            public static ConfigEntry<bool> ChestsUseImprovedTakeAllLogic;
            public static ConfigEntry<bool> DisplayStoreAllButton;

            public static ConfigEntry<bool> NeverMoveTakeAllButton;

            public static ConfigEntry<KeyboardShortcut> StoreAllKeybind;

            public static ConfigEntry<bool> StoreAllIncludesEquippedItems;
            public static ConfigEntry<bool> StoreAllIncludesHotkeyBar;

            public static ConfigEntry<KeyboardShortcut> TakeAllKeybind;
        }

        internal class SortConfig
        {
            public static ConfigEntry<AutoSortBehavior> AutoSort;
            public static ConfigEntry<ShowTwoButtons> DisplaySortButtons;
            public static ConfigEntry<bool> DisplaySortCriteriaInLabel;
            public static ConfigEntry<SortCriteriaEnum> SortCriteria;
            public static ConfigEntry<SortBehavior> SortHotkeyBehaviorWhenContainerOpen;
            public static ConfigEntry<bool> SortInAscendingOrder;
            public static ConfigEntry<bool> SortIncludesHotkeyBar;
            public static ConfigEntry<KeyboardShortcut> SortKeybind;
            public static ConfigEntry<bool> SortLeavesEmptyFavoritedSlotsEmpty;
            public static ConfigEntry<bool> SortMergesStacks;
        }

        internal class TrashConfig
        {
            public static ConfigEntry<bool> AlwaysConsiderTrophiesTrashFlagged;
            public static ConfigEntry<bool> DisplayTrashCanUI;
            public static ConfigEntry<bool> EnableQuickTrash;
            public static ConfigEntry<KeyboardShortcut> QuickTrashKeybind;
            public static ConfigEntry<ShowConfirmDialogOption> ShowConfirmDialogForNormalItem;
            public static ConfigEntry<bool> ShowConfirmDialogForQuickTrash;
            public static ConfigEntry<bool> TrashingCanAffectHotkeyBar;
            public static ConfigEntry<KeyboardShortcut> TrashKeybind;
            public static ConfigEntry<Color> TrashLabelColor;
        }

        internal class DebugConfig
        {
            public static ConfigEntry<DebugLevel> ShowDebugLogs;
            public static ConfigEntry<DebugSeverity> DebugSeverity;
            public static ConfigEntry<ResetFavoritingData> ResetAllFavoritingData;
        }

        internal static readonly List<ConfigEntryBase> keyBinds = new List<ConfigEntryBase>();

        internal static void LoadConfig(BaseUnityPlugin plugin)
        {
            Config = plugin.Config;

            // disable saving while we add config values, so it doesn't save to file on every change, then enable it again
            // this massively cuts down startup time by about 300%
            Config.SaveOnConfigSet = false;

            LoadConfigInternal(plugin);

            Config.Save();
            Config.SaveOnConfigSet = true;
        }

        private static void LoadConfigInternal(BaseUnityPlugin plugin)
        {
            if (Config == null)
            {
                Helper.LogO("Internal config load was called without its wrapper. This is slower but still works.", DebugLevel.Warning);
                Config = plugin.Config;
            }

            string sectionName;

            // keep the entries within a section in alphabetical order for the r2modman config manager

            string overrideButton = $"overridden by {nameof(GeneralConfig.OverrideButtonDisplay)}";
            string overrideHotkey = $"overridden by {nameof(GeneralConfig.OverrideKeybindBehavior)}";
            string overrideHotkeyBar = $"overridden by {nameof(GeneralConfig.OverrideHotkeyBarBehavior)}";
            string hotkey = "What to do when the hotkey is pressed while you have a container open.";
            string twoButtons = $"Which of the two buttons to display ({overrideButton}). Selecting {nameof(ShowTwoButtons.BothButDependingOnContext)} will hide the mini button while a container is open. The hotkey works independently.";
            string range = "How close the searched through containers have to be.";
            string favoriteFunction = "disallowing quick stacking, storing, sorting and trashing";
            string favoritingKey = $"While holding this, left clicking on items or right clicking on slots favorites them, {favoriteFunction}, or trash flags them if you are hovering an item on the trash can.";
            string restockLimitText = "Allows to set a custom stack size limit for {0} items in case you don't want them to restock to their maximum stack size. Use zero or negative numbers disable this.";

            sectionName = "0 - General";

            GeneralConfig.ConfigTemplate = Config.Bind(sectionName, nameof(GeneralConfig.ConfigTemplate), ConfigTemplate.NotCurrentlyLoadingTemplate, "Immediately or at the next startup, resets the config and applies the selected config template. Does not change any custom keybinds!");
            GeneralConfig.ConfigTemplate.SettingChanged += ConfigTemplate_SettingChanged;

            GeneralConfig.OverrideButtonDisplay = Config.Bind(sectionName, nameof(GeneralConfig.OverrideButtonDisplay), OverrideButtonDisplay.UseIndividualConfigOptions, "Override to disable all new UI elements no matter the current individual setting of each of them.");
            GeneralConfig.OverrideButtonDisplay.SettingChanged += (a, b) => ButtonRenderer.OnButtonRelevantSettingChanged(plugin, true);

            GeneralConfig.OverrideHotkeyBarBehavior = Config.Bind(sectionName, nameof(GeneralConfig.OverrideHotkeyBarBehavior), OverrideHotkeyBarBehavior.NeverAffectHotkeyBar, "Override to never affect the hotkey bar with any feature no matter the individual setting of each of them. Recommended to turn off if you are actually using favoriting.");
            GeneralConfig.OverrideKeybindBehavior = Config.Bind(sectionName, nameof(GeneralConfig.OverrideKeybindBehavior), OverrideKeybindBehavior.UseIndividualConfigOptions, "Override to disable all new keybinds no matter the current individual setting of each of them.");

            bool oldValue = false;

            if (TryGetOldConfigValue(new ConfigDefinition(sectionName, "DisableAllNewButtons"), ref oldValue))
            {
                GeneralConfig.OverrideButtonDisplay.Value = oldValue ? OverrideButtonDisplay.DisableAllNewButtons : OverrideButtonDisplay.UseIndividualConfigOptions;
            }

            if (TryGetOldConfigValue(new ConfigDefinition(sectionName, "DisableAllNewKeybinds"), ref oldValue))
            {
                GeneralConfig.OverrideKeybindBehavior.Value = oldValue ? OverrideKeybindBehavior.DisableAllNewHotkeys : OverrideKeybindBehavior.UseIndividualConfigOptions;
            }

            if (TryGetOldConfigValue(new ConfigDefinition(sectionName, "NeverAffectHotkeyBar"), ref oldValue))
            {
                GeneralConfig.OverrideHotkeyBarBehavior.Value = oldValue ? OverrideHotkeyBarBehavior.NeverAffectHotkeyBar : OverrideHotkeyBarBehavior.UseIndividualConfigOptions;
            }
            UseTopDownLogicForEverything = Config.Bind(sectionName, nameof(UseTopDownLogicForEverything), false, "Whether to always put items into the top first row (affects the entire game) rather than top or bottom first depending on the item type (base game uses top first only for weapons and tools, bottom first for the rest). Recommended to keep off.");

            sectionName = "0.1 - Controller General";

            ControllerDPadUsageInInventoryGrid = Config.Bind(sectionName, nameof(ControllerDPadUsageInInventoryGrid), DPadUsage.Keybinds, "In the base game the DPad and the left stick are both used for slot movement inside the inventory grid. This allows you to exclude the DPad from this to get more keys for keybinds.");
            ControllerDPadUsageModifierKeybind = Config.Bind(sectionName, nameof(ControllerDPadUsageModifierKeybind), new KeyboardShortcut(KeyCode.None), $"When {nameof(ControllerDPadUsageInInventoryGrid)} is set to {DPadUsage.KeybindsWhileHoldingModifierKey}, then holding this prevents slot movement in the inventory grid with the DPad.");

            RemoveControllerButtonHintFromTakeAllButton = Config.Bind(sectionName, nameof(RemoveControllerButtonHintFromTakeAllButton), false, $"Remove the button hint from the 'Take All' button while using a controller for consistency. Especially useful when using the new keybind {nameof(TakeAllKeybind)}.");
            RemoveControllerButtonHintFromTakeAllButton.SettingChanged += (a, b) => ButtonRenderer.OnButtonRelevantSettingChanged(plugin);

            UseHardcodedControllerSupport = Config.Bind(sectionName, nameof(UseHardcodedControllerSupport), true, "Whether to enable the hardcoded controller bindings including UI hints while a controller is used. This disables custom hotkeys.");
            UseHardcodedControllerSupport.SettingChanged += (a, b) => ButtonRenderer.OnButtonRelevantSettingChanged(plugin);

            sectionName = "1 - Favoriting";

            // valheim yellow/ orange-ish
            BorderColorFavoritedItem = Config.Bind(sectionName, nameof(BorderColorFavoritedItem), new Color(1f, 0.8482759f, 0f), "Color of the border for slots containing favorited items.");
            BorderColorFavoritedItem.SettingChanged += (a, b) => FavoritingMode.RefreshDisplay();

            // dark-ish green
            BorderColorFavoritedItemOnFavoritedSlot = Config.Bind(sectionName, nameof(BorderColorFavoritedItemOnFavoritedSlot), new Color(0.5f, 0.67413795f, 0.5f), "Color of the border of a favorited slot that also contains a favorited item.");

            // light-ish blue
            BorderColorFavoritedSlot = Config.Bind(sectionName, nameof(BorderColorFavoritedSlot), new Color(0f, 0.5f, 1f), "Color of the border for favorited slots.");
            // dark-ish red
            BorderColorTrashFlaggedItem = Config.Bind(sectionName, nameof(BorderColorTrashFlaggedItem), new Color(0.5f, 0f, 0), HiddenTrashingDisplay("Color of the border for slots containing trash flagged items."));
            // black
            BorderColorTrashFlaggedItemOnFavoritedSlot = Config.Bind(sectionName, nameof(BorderColorTrashFlaggedItemOnFavoritedSlot), Color.black, HiddenTrashingDisplay("Color of the border of a favorited slot that also contains a trash flagged item."));

            DisplayFavoriteToggleButton = Config.Bind(sectionName, nameof(DisplayFavoriteToggleButton), FavoritingToggling.EnabledTopButton, $"Whether to display a button to toggle favoriting mode on or off, allowing to favorite without holding any hotkey ({overrideButton}). This can also be used to trash flag. The hotkeys work independently.");
            DisplayFavoriteToggleButton.SettingChanged += (a, b) => ButtonRenderer.OnButtonRelevantSettingChanged(plugin);

            if (TryGetOldConfigValue(new ConfigDefinition(sectionName, "FavoritingModifierToggles"), ref oldValue))
            {
                DisplayFavoriteToggleButton.Value = oldValue ? FavoritingToggling.EnabledTopButton : FavoritingToggling.Disabled;
            }

            DisplayTooltipHint = Config.Bind(sectionName, nameof(DisplayTooltipHint), true, "Whether to add additional info the item tooltip of a favorited or trash flagged item.");

            FavoritingModifierKeybind1 = Config.Bind(sectionName, nameof(FavoritingModifierKeybind1), new KeyboardShortcut(KeyCode.LeftAlt), $"{favoritingKey} Identical to {nameof(FavoritingModifierKeybind2)}.");
            FavoritingModifierKeybind2 = Config.Bind(sectionName, nameof(FavoritingModifierKeybind2), new KeyboardShortcut(KeyCode.RightAlt), $"{favoritingKey} Identical to {nameof(FavoritingModifierKeybind1)}.");

            KeyCodeBackwardsCompatibility(FavoritingModifierKeybind1, sectionName, "FavoritingModifierKey1");
            KeyCodeBackwardsCompatibility(FavoritingModifierKeybind2, sectionName, "FavoritingModifierKey2");

            FavoriteConfig.FavoriteToggleButtonStyle = Config.Bind(sectionName, nameof(FavoriteConfig.FavoriteToggleButtonStyle), FavoriteToggleButtonStyle.TextStarInItemFavoriteColor, $"The style of the favorite toggling button enabled with {nameof(DisplayFavoriteToggleButton)}.");
            FavoriteConfig.FavoriteToggleButtonStyle.SettingChanged += (a, b) => FavoritingMode.RefreshDisplay();

            sectionName = "2 - Quick Stacking and Restocking";
            string areaStackSectionDisplayName = "2.0 - Area Quick Stacking and Restocking";

            AreaStackRestockServerSync = new ConfigSync(QuickStackStorePlugin.PluginGUID) { DisplayName = QuickStackStorePlugin.PluginName, CurrentVersion = QuickStackStorePlugin.PluginVersion, MinimumRequiredVersion = QuickStackStorePlugin.PluginVersion, ModRequired = false };

            AllowAreaStackingInMultiplayerWithoutMUC = Config.BindSynced(AreaStackRestockServerSync, sectionName, nameof(AllowAreaStackingInMultiplayerWithoutMUC), false, MUCSettingDisplay("AllowAreaStackingInMultiplayer", areaStackSectionDisplayName, "Whether you can use area quick stacking and area restocking in multiplayer while 'Multi User Chest' is not installed. While this is almost always safe, it can fail because no actual network requests are getting sent. Ship containers are inherently especially vulnerable and are therefore excluded."));
            AllowAreaStackingInMultiplayerWithoutMUC.SettingChanged += (a, b) => ButtonRenderer.OnButtonRelevantSettingChanged(plugin);

            AllowAreaStackingToNonPhysicalContainers = Config.BindSynced(AreaStackRestockServerSync, sectionName, nameof(AllowAreaStackingToNonPhysicalContainers), true, CustomCategoryWithDescription(areaStackSectionDisplayName, "Allow stacking to or restocking from containers without a physical piece object in the world, like backpacks."));
            AllowAreaStackingToPhysicalNonPlayerBuiltContainers = Config.BindSynced(AreaStackRestockServerSync, sectionName, nameof(AllowAreaStackingToPhysicalNonPlayerBuiltContainers), false, CustomCategoryWithDescription(areaStackSectionDisplayName, "Allow stacking to or restocking from containers like dungeon chests or probably some modded containers."));

            SuppressContainerSoundAndVisuals = Config.BindSynced(AreaStackRestockServerSync, sectionName, nameof(SuppressContainerSoundAndVisuals), true, CustomCategoryWithDescription(areaStackSectionDisplayName, "Whether when a feature checks multiple containers in an area, they actually play opening sounds and visuals. Disable if the suppression causes incompatibilities."));

            ToggleAreaStackRestockConfigServerSync = Config.BindSyncLocker(AreaStackRestockServerSync, sectionName, nameof(ToggleAreaStackRestockConfigServerSync), true, CustomCategoryWithDescription(areaStackSectionDisplayName, "Whether the config settings about area quick stacking and area restocking (including range) of the host/ server get applied to all other users using this mod. Does nothing if the host/ server does not have this mod installed."));

            sectionName = "2.1 - Quick Stacking";

            ChangeHoldToStackFeatureToUseModdedQuickStackingLogic = Config.Bind(sectionName, nameof(ChangeHoldToStackFeatureToUseModdedQuickStackingLogic), true, "Whether to override the behavior when holding open on a container that you are hovering on with the modded quick stacking behavior. Does not override the behavior of the default 'Place Stacks' button, if it's still enabled.");

            DisplayQuickStackButtons = Config.Bind(sectionName, nameof(DisplayQuickStackButtons), ShowTwoButtons.BothButDependingOnContext, twoButtons);
            DisplayQuickStackButtons.SettingChanged += (a, b) => ButtonRenderer.OnButtonRelevantSettingChanged(plugin);

            HideBaseGamePlaceStacksButton = Config.Bind(sectionName, nameof(HideBaseGamePlaceStacksButton), true, ForceEnabledDisplay(() => CompatibilitySupport.HasRandyPlugin() == CompatibilitySupport.RandyStatus.EnabledWithQuickSlots, "Whether to hide the 'Place Stacks' button that uses the base game quick stacking logic. Modded buttons are moved automatically based on this setting. Force enabled when using 'Randy's Equipment and Quick Slot' mod."));
            HideBaseGamePlaceStacksButton.SettingChanged += (a, b) => ButtonRenderer.OnButtonRelevantSettingChanged(plugin);

            QuickStackHotkeyBehaviorWhenContainerOpen = Config.Bind(sectionName, nameof(QuickStackHotkeyBehaviorWhenContainerOpen), QuickStackBehavior.QuickStackOnlyToCurrentContainer, hotkey);
            QuickStackIncludesHotkeyBar = Config.Bind(sectionName, nameof(QuickStackIncludesHotkeyBar), true, $"Whether to also quick stack items from the hotkey bar ({overrideHotkeyBar}).");

            QuickStackKeybind = Config.Bind(sectionName, nameof(QuickStackKeybind), new KeyboardShortcut(KeyCode.P), $"The hotkey to start quick stacking to the current or nearby containers (depending on {nameof(QuickStackHotkeyBehaviorWhenContainerOpen)}, {overrideHotkey}).");
            KeyCodeBackwardsCompatibility(QuickStackKeybind, sectionName, "QuickStackKey");

            QuickStackToNearbyRange = Config.BindSynced(AreaStackRestockServerSync, sectionName, nameof(QuickStackToNearbyRange), 10f, CustomCategoryWithDescription(areaStackSectionDisplayName, range));
            QuickStackToNearbyRange.SettingChanged += (a, b) => ButtonRenderer.OnButtonRelevantSettingChanged(plugin);

            QuickStackTrophiesIntoSameContainer = Config.Bind(sectionName, nameof(QuickStackTrophiesIntoSameContainer), false, "Whether to put all types of trophies in the container if any trophy is found in that container.");

            ShowQuickStackResultMessage = Config.Bind(sectionName, nameof(ShowQuickStackResultMessage), true, "Whether to show the central screen report message after quick stacking.");

            sectionName = "2.2 - Quick Restocking";

            DisplayRestockButtons = Config.Bind(sectionName, nameof(DisplayRestockButtons), ShowTwoButtons.BothButDependingOnContext, twoButtons);
            DisplayRestockButtons.SettingChanged += (a, b) => ButtonRenderer.OnButtonRelevantSettingChanged(plugin);

            RestockFromNearbyRange = Config.BindSynced(AreaStackRestockServerSync, sectionName, nameof(RestockFromNearbyRange), 10f, CustomCategoryWithDescription(areaStackSectionDisplayName, range));
            RestockFromNearbyRange.SettingChanged += (a, b) => ButtonRenderer.OnButtonRelevantSettingChanged(plugin);

            RestockHotkeyBehaviorWhenContainerOpen = Config.Bind(sectionName, nameof(RestockHotkeyBehaviorWhenContainerOpen), RestockBehavior.RestockOnlyFromCurrentContainer, hotkey);
            RestockIncludesHotkeyBar = Config.Bind(sectionName, nameof(RestockIncludesHotkeyBar), true, $"Whether to also try to restock items currently in the hotkey bar ({overrideHotkeyBar}).");

            RestockKeybind = Config.Bind(sectionName, nameof(RestockKeybind), new KeyboardShortcut(KeyCode.L), $"The hotkey to start restocking from the current or nearby containers (depending on {nameof(RestockHotkeyBehaviorWhenContainerOpen)}, {overrideHotkey}).");
            KeyCodeBackwardsCompatibility(RestockKeybind, sectionName, "RestockKey");

            RestockOnlyAmmoAndConsumables = Config.Bind(sectionName, nameof(RestockOnlyAmmoAndConsumables), true, $"Whether restocking should only restock ammo and consumable or every stackable item (like materials). Also affected by {nameof(RestockOnlyFavoritedItems)}.");
            RestockOnlyFavoritedItems = Config.Bind(sectionName, nameof(RestockOnlyFavoritedItems), false, $"Whether restocking should only restock favorited items or items on favorited slots or every stackable item. Also affected by {nameof(RestockOnlyAmmoAndConsumables)}.");

            RestockStackSizeLimitAmmo = Config.Bind(sectionName, nameof(RestockStackSizeLimitAmmo), 0, string.Format(restockLimitText, "ammo"));
            RestockStackSizeLimitConsumables = Config.Bind(sectionName, nameof(RestockStackSizeLimitConsumables), 0, string.Format(restockLimitText, "consumable"));
            RestockStackSizeLimitGeneral = Config.Bind(sectionName, nameof(RestockStackSizeLimitGeneral), 0, string.Format(restockLimitText, "all") + $" The stack size limits for ammo or consumables from their respective config setting ({nameof(RestockStackSizeLimitAmmo)} and {nameof(RestockStackSizeLimitConsumables)}) take priority if they are also enabled.");

            ShowRestockResultMessage = Config.Bind(sectionName, nameof(ShowRestockResultMessage), true, "Whether to show the central screen report message after restocking.");

            sectionName = "3 - Store and Take All";

            ChestsUseImprovedTakeAllLogic = Config.Bind(sectionName, nameof(ChestsUseImprovedTakeAllLogic), true, "Whether to use the improved logic for 'Take All' for non tomb stones. Disable if needed for compatibility.");

            DisplayStoreAllButton = Config.Bind(sectionName, nameof(DisplayStoreAllButton), true, $"Whether to display the 'Store All' button in containers ({overrideButton}).");
            DisplayStoreAllButton.SettingChanged += (a, b) => ButtonRenderer.OnButtonRelevantSettingChanged(plugin);

            NeverMoveTakeAllButton = Config.Bind(sectionName, nameof(NeverMoveTakeAllButton), false, ForceEnabledDisplay(CompatibilitySupport.ShouldBlockChangesToTakeAllButtonDueToPlugin, "Disallows my mod from moving the 'Take All' button. Enable for compatibility with other mods (when certain mods are detected, this setting is force enabled). If it was already moved, then you need to log out and back in (since I don't even allow to reset the position, since I don't know if that position is valid with your installed mods)."));

            StoreAllKeybind = Config.Bind(sectionName, nameof(StoreAllKeybind), new KeyboardShortcut(KeyCode.None), $"The hotkey to use 'Store All' on the currently opened container ({overrideHotkey}).");

            StoreAllIncludesEquippedItems = Config.Bind(sectionName, nameof(StoreAllIncludesEquippedItems), false, "Whether to also unequip and store non favorited equipped items or exclude them.");
            StoreAllIncludesHotkeyBar = Config.Bind(sectionName, nameof(StoreAllIncludesHotkeyBar), true, $"Whether to also store all non favorited items from the hotkey bar ({overrideHotkeyBar}).");

            TakeAllKeybind = Config.Bind(sectionName, nameof(TakeAllKeybind), new KeyboardShortcut(KeyCode.None), $"The hotkey to use 'Take All' on the currently opened container ({overrideHotkey}).");

            sectionName = "4 - Sorting";

            AutoSort = Config.Bind(sectionName, nameof(AutoSort), AutoSortBehavior.Never, "Automatically let the mod sort the player inventory every time you open it, as well as every container you open. This respects your other sorting config options.");

            DisplaySortButtons = Config.Bind(sectionName, nameof(DisplaySortButtons), ShowTwoButtons.Both, twoButtons);
            DisplaySortButtons.SettingChanged += (a, b) => ButtonRenderer.OnButtonRelevantSettingChanged(plugin);

            DisplaySortCriteriaInLabel = Config.Bind(sectionName, nameof(DisplaySortCriteriaInLabel), false, "Whether to display the current sort criteria in the inventory sort button as a reminder. The author thinks the button is a bit too small for it to look good.");
            DisplaySortCriteriaInLabel.SettingChanged += (a, b) => ButtonRenderer.OnButtonRelevantSettingChanged(plugin);

            SortCriteria = Config.Bind(sectionName, nameof(SortCriteria), SortCriteriaEnum.Type, "The sort criteria the sort button uses. Ties are broken by internal name, quality and stack size.");
            SortHotkeyBehaviorWhenContainerOpen = Config.Bind(sectionName, nameof(SortHotkeyBehaviorWhenContainerOpen), SortBehavior.OnlySortContainer, hotkey);
            SortInAscendingOrder = Config.Bind(sectionName, nameof(SortInAscendingOrder), true, "Whether the current first sort criteria should be used in ascending or descending order.");
            SortIncludesHotkeyBar = Config.Bind(sectionName, nameof(SortIncludesHotkeyBar), true, $"Whether to also sort non favorited items from the hotkey bar ({overrideHotkeyBar}).");

            SortKeybind = Config.Bind(sectionName, nameof(SortKeybind), new KeyboardShortcut(KeyCode.O), $"The hotkey to sort the inventory or the current container or both (depending on {nameof(SortHotkeyBehaviorWhenContainerOpen)}, {overrideHotkey}).");
            KeyCodeBackwardsCompatibility(SortKeybind, sectionName, "SortKey");

            SortLeavesEmptyFavoritedSlotsEmpty = Config.Bind(sectionName, nameof(SortLeavesEmptyFavoritedSlotsEmpty), true, "Whether sort treats empty favorited slots as occupied and leaves them empty, so you don't accidentally put items on them.");
            SortMergesStacks = Config.Bind(sectionName, nameof(SortMergesStacks), true, "Whether to merge stacks after sorting or keep them as separate non full stacks.");

            sectionName = "5 - Trashing";

            AlwaysConsiderTrophiesTrashFlagged = Config.Bind(sectionName, nameof(AlwaysConsiderTrophiesTrashFlagged), false, HiddenTrashingDisplay("Whether to always consider trophies as trash flagged, allowing for immediate trashing or to be affected by quick trashing."));

            DisplayTrashCanUI = Config.Bind(sectionName, nameof(DisplayTrashCanUI), true, TrashingCategoryHideNotificationDisplay($"Whether to display the trash can UI element ({overrideButton}). Hotkeys work independently."));
            DisplayTrashCanUI.SettingChanged += (a, b) => ButtonRenderer.OnButtonRelevantSettingChanged(plugin, true);

            EnableQuickTrash = Config.Bind(sectionName, nameof(EnableQuickTrash), true, HiddenTrashingDisplay("Whether quick trashing can be called with the hotkey or be clicking on the trash can while not holding anything."));

            QuickTrashKeybind = Config.Bind(sectionName, nameof(QuickTrashKeybind), new KeyboardShortcut(KeyCode.None), HiddenTrashingDisplay($"The hotkey to perform a quick trash on the player inventory, deleting all trash flagged items ({overrideHotkey})."));
            KeyCodeBackwardsCompatibility(QuickTrashKeybind, sectionName, "QuickTrashHotkey");

            ShowConfirmDialogForNormalItem = Config.Bind(sectionName, nameof(ShowConfirmDialogForNormalItem), ShowConfirmDialogOption.WhenNotTrashFlagged, HiddenTrashingDisplay("When to show a confirmation dialog while doing a non quick trash."));
            ShowConfirmDialogForQuickTrash = Config.Bind(sectionName, nameof(ShowConfirmDialogForQuickTrash), true, HiddenTrashingDisplay("Whether to show a confirmation dialog while doing a quick trash."));

            TrashingCanAffectHotkeyBar = Config.Bind(sectionName, nameof(TrashingCanAffectHotkeyBar), true, HiddenTrashingDisplay($"Whether trashing and quick trashing can trash items that are currently in the hotkey bar ({overrideHotkeyBar})."));

            TrashKeybind = Config.Bind(sectionName, nameof(TrashKeybind), new KeyboardShortcut(KeyCode.Delete), HiddenTrashingDisplay($"The hotkey to trash the currently held item ({overrideHotkey})."));
            KeyCodeBackwardsCompatibility(TrashKeybind, sectionName, "TrashHotkey");

            TrashLabelColor = Config.Bind(sectionName, nameof(TrashLabelColor), new Color(1f, 0.8482759f, 0), HiddenTrashingDisplay("The color of the text below the trash can in the player inventory."));

            sectionName = "8 - Debugging";

            DebugConfig.ShowDebugLogs = Config.Bind(sectionName, nameof(DebugConfig.ShowDebugLogs), DebugLevel.Disabled, "Enable debug logs into the console. Optionally set it to print as warnings, so the yellow color is easier to spot. Some important prints ignore this setting.");
            DebugConfig.ResetAllFavoritingData = Config.Bind(sectionName, nameof(DebugConfig.ResetAllFavoritingData), ResetFavoritingData.No, "This deletes all the favoriting of your items and slots, as well as trash flagging, the next time the mod checks for it (either on loading a character or on config change while ingame), and then resets this config back to 'No'.");
            DebugConfig.ResetAllFavoritingData.SettingChanged -= ResetAllFavoritingData_SettingChanged;
            DebugConfig.ResetAllFavoritingData.SettingChanged += ResetAllFavoritingData_SettingChanged;
            DebugConfig.DebugSeverity = Config.Bind(sectionName, nameof(DebugConfig.DebugSeverity), DebugSeverity.Normal, $"Filters which kind of debug messages are shown when {DebugConfig.ShowDebugLogs} is not disabled. Only use {DebugSeverity.Everything} for testing.");

            sectionName = "9 - Localization";

            TrashLabel = Config.Bind(sectionName, nameof(TrashLabel), string.Empty, string.Empty);
            TrashLabel.SettingChanged += (a, b) => ButtonRenderer.OnButtonTextTranslationSettingChanged();

            QuickStackLabel = Config.Bind(sectionName, nameof(QuickStackLabel), string.Empty, string.Empty);
            QuickStackLabel.SettingChanged += (a, b) => ButtonRenderer.OnButtonTextTranslationSettingChanged();

            StoreAllLabel = Config.Bind(sectionName, nameof(StoreAllLabel), string.Empty, string.Empty);
            StoreAllLabel.SettingChanged += (a, b) => ButtonRenderer.OnButtonTextTranslationSettingChanged();

            TakeAllLabel = Config.Bind(sectionName, nameof(TakeAllLabel), string.Empty, string.Empty);
            TakeAllLabel.SettingChanged += (a, b) => ButtonRenderer.OnButtonTextTranslationSettingChanged();

            RestockLabel = Config.Bind(sectionName, nameof(RestockLabel), string.Empty, string.Empty);
            RestockLabel.SettingChanged += (a, b) => ButtonRenderer.OnButtonTextTranslationSettingChanged();

            SortLabel = Config.Bind(sectionName, nameof(SortLabel), string.Empty, string.Empty);
            SortLabel.SettingChanged += (a, b) => ButtonRenderer.OnButtonTextTranslationSettingChanged();

            QuickStackLabelCharacter = Config.Bind(sectionName, nameof(QuickStackLabelCharacter), string.Empty, string.Empty);
            QuickStackLabelCharacter.SettingChanged += (a, b) => ButtonRenderer.OnButtonTextTranslationSettingChanged();

            SortLabelCharacter = Config.Bind(sectionName, nameof(SortLabelCharacter), string.Empty, string.Empty);
            SortLabelCharacter.SettingChanged += (a, b) => ButtonRenderer.OnButtonTextTranslationSettingChanged();

            RestockLabelCharacter = Config.Bind(sectionName, nameof(RestockLabelCharacter), string.Empty, string.Empty);
            RestockLabelCharacter.SettingChanged += (a, b) => ButtonRenderer.OnButtonTextTranslationSettingChanged();

            SortByInternalNameLabel = Config.Bind(sectionName, nameof(SortByInternalNameLabel), string.Empty, string.Empty);
            SortByInternalNameLabel.SettingChanged += (a, b) => ButtonRenderer.OnButtonTextTranslationSettingChanged();

            SortByTranslatedNameLabel = Config.Bind(sectionName, nameof(SortByTranslatedNameLabel), string.Empty, string.Empty);
            SortByTranslatedNameLabel.SettingChanged += (a, b) => ButtonRenderer.OnButtonTextTranslationSettingChanged();

            SortByValueLabel = Config.Bind(sectionName, nameof(SortByValueLabel), string.Empty, string.Empty);
            SortByValueLabel.SettingChanged += (a, b) => ButtonRenderer.OnButtonTextTranslationSettingChanged();

            SortByWeightLabel = Config.Bind(sectionName, nameof(SortByWeightLabel), string.Empty, string.Empty);
            SortByWeightLabel.SettingChanged += (a, b) => ButtonRenderer.OnButtonTextTranslationSettingChanged();

            SortByTypeLabel = Config.Bind(sectionName, nameof(SortByTypeLabel), string.Empty, string.Empty);
            SortByTypeLabel.SettingChanged += (a, b) => ButtonRenderer.OnButtonTextTranslationSettingChanged();

            QuickStackResultMessageNothing = Config.Bind(sectionName, nameof(QuickStackResultMessageNothing), string.Empty, string.Empty);
            QuickStackResultMessageNone = Config.Bind(sectionName, nameof(QuickStackResultMessageNone), string.Empty, string.Empty);
            QuickStackResultMessageOne = Config.Bind(sectionName, nameof(QuickStackResultMessageOne), string.Empty, string.Empty);
            QuickStackResultMessageMore = Config.Bind(sectionName, nameof(QuickStackResultMessageMore), string.Empty, string.Empty);

            RestockResultMessageNothing = Config.Bind(sectionName, nameof(RestockResultMessageNothing), string.Empty, string.Empty);
            RestockResultMessageNone = Config.Bind(sectionName, nameof(RestockResultMessageNone), string.Empty, string.Empty);
            RestockResultMessagePartial = Config.Bind(sectionName, nameof(RestockResultMessagePartial), string.Empty, string.Empty);
            RestockResultMessageFull = Config.Bind(sectionName, nameof(RestockResultMessageFull), string.Empty, string.Empty);

            TrashConfirmationOkayButton = Config.Bind(sectionName, nameof(TrashConfirmationOkayButton), string.Empty, string.Empty);
            QuickTrashConfirmation = Config.Bind(sectionName, nameof(QuickTrashConfirmation), string.Empty, string.Empty);
            CantTrashFavoritedItemWarning = Config.Bind(sectionName, nameof(CantTrashFavoritedItemWarning), string.Empty, string.Empty);
            CantTrashHotkeyBarItemWarning = Config.Bind(sectionName, nameof(CantTrashHotkeyBarItemWarning), string.Empty, string.Empty);
            CantTrashFlagFavoritedItemWarning = Config.Bind(sectionName, nameof(CantTrashFlagFavoritedItemWarning), string.Empty, string.Empty);
            CantFavoriteTrashFlaggedItemWarning = Config.Bind(sectionName, nameof(CantFavoriteTrashFlaggedItemWarning), string.Empty, string.Empty);

            FavoritedItemTooltip = Config.Bind(sectionName, nameof(FavoritedItemTooltip), string.Empty, string.Empty);
            TrashFlaggedItemTooltip = Config.Bind(sectionName, nameof(TrashFlaggedItemTooltip), string.Empty, string.Empty);

            keyBinds.Clear();

            keyBinds.Add(ControllerDPadUsageModifierKeybind);
            keyBinds.Add(FavoritingModifierKeybind1);
            keyBinds.Add(FavoritingModifierKeybind2);
            keyBinds.Add(QuickStackKeybind);
            keyBinds.Add(QuickTrashKeybind);
            keyBinds.Add(RestockKeybind);
            keyBinds.Add(SortKeybind);
            keyBinds.Add(StoreAllKeybind);
            keyBinds.Add(TakeAllKeybind);
            keyBinds.Add(TrashKeybind);
        }

        internal static void ConfigTemplate_SettingChanged(object sender, EventArgs e)
        {
            GeneralConfig.ConfigTemplate.SettingChanged -= ConfigTemplate_SettingChanged;

            ApplyTemplate(GeneralConfig.ConfigTemplate.Value);

            GeneralConfig.ConfigTemplate.Value = ConfigTemplate.NotCurrentlyLoadingTemplate;
            GeneralConfig.ConfigTemplate.SettingChanged += ConfigTemplate_SettingChanged;
        }

        internal static void ApplyTemplate(ConfigTemplate template)
        {
            if (GeneralConfig.ConfigTemplate.Value == ConfigTemplate.NotCurrentlyLoadingTemplate)
            {
                return;
            }

            var forbiddenKeys = new HashSet<string>(keyBinds.Select((a) => a.Definition.Key));

            foreach (var config in Config)
            {
                if (forbiddenKeys.Contains(config.Key.Key))
                {
                    continue;
                }

                config.Value.BoxedValue = config.Value.DefaultValue;
            }

            switch (template)
            {
                case ConfigTemplate.BasicControllerKeybinds:
                    GeneralConfig.OverrideKeybindBehavior.Value = OverrideKeybindBehavior.DisableAllNewHotkeys;

                    ControllerConfig.UseHardcodedControllerSupport.Value = true;
                    ControllerConfig.ControllerDPadUsageInInventoryGrid.Value = DPadUsage.Keybinds;
                    ControllerConfig.RemoveControllerButtonHintFromTakeAllButton.Value = false;
                    break;

                case ConfigTemplate.CustomControllerKeybinds:
                    GeneralConfig.OverrideKeybindBehavior.Value = OverrideKeybindBehavior.UseIndividualConfigOptions;

                    FavoriteConfig.DisplayFavoriteToggleButton.Value = FavoritingToggling.Disabled;
                    ControllerConfig.UseHardcodedControllerSupport.Value = false;
                    ControllerConfig.ControllerDPadUsageInInventoryGrid.Value = DPadUsage.InventorySlotMovement;
                    ControllerConfig.RemoveControllerButtonHintFromTakeAllButton.Value = true;
                    break;

                case ConfigTemplate.MouseAndKeyboardWithButtons:
                    GeneralConfig.OverrideKeybindBehavior.Value = OverrideKeybindBehavior.DisableAllNewHotkeys;

                    break;

                case ConfigTemplate.MouseAndKeyboardWithHotkeys:
                    GeneralConfig.OverrideButtonDisplay.Value = OverrideButtonDisplay.DisableAllNewButtons;
                    GeneralConfig.OverrideKeybindBehavior.Value = OverrideKeybindBehavior.UseIndividualConfigOptions;

                    break;

                case ConfigTemplate.GoldensChoice:

                    GeneralConfig.OverrideKeybindBehavior.Value = OverrideKeybindBehavior.DisableAllNewHotkeys;
                    GeneralConfig.OverrideButtonDisplay.Value = OverrideButtonDisplay.UseIndividualConfigOptions;
                    GeneralConfig.OverrideHotkeyBarBehavior.Value = OverrideHotkeyBarBehavior.UseIndividualConfigOptions;
                    QuickStackRestockConfig.AllowAreaStackingInMultiplayerWithoutMUC.Value = true;
                    RestockConfig.RestockOnlyAmmoAndConsumables.Value = false;
                    RestockConfig.RestockOnlyFavoritedItems.Value = true;
                    break;
            }

            Config.Save();
        }

        internal static void ResetAllFavoritingData_SettingChanged(object sender, EventArgs e)
        {
            if (DebugConfig.ResetAllFavoritingData?.Value != ResetFavoritingData.YesDeleteAllMyFavoritingData)
            {
                return;
            }

            DebugConfig.ResetAllFavoritingData.SettingChanged -= ResetAllFavoritingData_SettingChanged;
            DebugConfig.ResetAllFavoritingData.Value = ResetFavoritingData.No;
            DebugConfig.ResetAllFavoritingData.SettingChanged += ResetAllFavoritingData_SettingChanged;

            if (Player.m_localPlayer != null)
            {
                var playerConfig = UserConfig.GetPlayerConfig(Player.m_localPlayer.GetPlayerID());

                playerConfig.ResetAllFavoriting();
            }
        }

        public static bool TryGetOldConfigValue<T>(ConfigDefinition configDefinition, ref T oldValue, bool removeIfFound = true)
        {
            if (!TomlTypeConverter.CanConvert(typeof(T)))
            {
                throw new ArgumentException(string.Format("Type {0} is not supported by the config system. Supported types: {1}", typeof(T), string.Join(", ", (from x in TomlTypeConverter.GetSupportedTypes() select x.Name).ToArray())));
            }

            try
            {
                var iolock = AccessTools.FieldRefAccess<ConfigFile, object>("_ioLock").Invoke(Config);
                var orphanedEntries = (Dictionary<ConfigDefinition, string>)AccessTools.PropertyGetter(typeof(ConfigFile), "OrphanedEntries").Invoke(Config, new object[0]);

                lock (iolock)
                {
                    if (orphanedEntries.TryGetValue(configDefinition, out string oldValueString))
                    {
                        oldValue = (T)TomlTypeConverter.ConvertToValue(oldValueString, typeof(T));

                        if (removeIfFound)
                        {
                            orphanedEntries.Remove(configDefinition);
                        }

                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Helper.LogO($"Error getting orphaned entry: {e.StackTrace}", DebugLevel.Warning);
            }

            return false;
        }

        public static void KeyCodeBackwardsCompatibility(ConfigEntry<KeyboardShortcut> configEntry, string sectionName, string oldEntry)
        {
            KeyCode oldKeyValue = KeyCode.None;

            if (TryGetOldConfigValue(new ConfigDefinition(sectionName, oldEntry), ref oldKeyValue))
            {
                configEntry.Value = new KeyboardShortcut(oldKeyValue);
            }
        }

        public enum OverrideButtonDisplay
        {
            DisableAllNewButtons,
            UseIndividualConfigOptions
        }

        public enum OverrideKeybindBehavior
        {
            DisableAllNewHotkeys,
            UseIndividualConfigOptions
        }

        public enum OverrideHotkeyBarBehavior
        {
            NeverAffectHotkeyBar,
            UseIndividualConfigOptions
        }

        public enum ShowConfirmDialogOption
        {
            Never,
            WhenNotTrashFlagged,
            Always
        }

        public enum ShowTwoButtons
        {
            Both,
            OnlyInventoryButton,
            OnlyContainerButton,
            BothButDependingOnContext,
            Disabled
        }

        public enum QuickStackBehavior
        {
            QuickStackOnlyToCurrentContainer,
            QuickStackToBoth
        }

        public enum RestockBehavior
        {
            RestockOnlyFromCurrentContainer,
            RestockFromBoth
        }

        public enum SortBehavior
        {
            OnlySortContainer,
            SortBoth
        }

        public enum SortCriteriaEnum
        {
            InternalName,
            TranslatedName,
            Value,
            Weight,
            Type
        }

        public enum AutoSortBehavior
        {
            Never,
            SortContainerOnOpen,
            SortPlayerInventoryOnOpen,
            Both
        }

        internal enum DebugLevel
        {
            Disabled = 0,
            Log = 1,
            Warning = 2
        }

        internal enum DebugSeverity
        {
            Normal = 0,
            AlsoSpeedTests = 1,
            Everything = 2
        }

        internal enum ResetFavoritingData
        {
            No,
            YesDeleteAllMyFavoritingData
        }

        internal enum FavoritingToggling
        {
            Disabled = 0,
            EnabledTopButton = 1,
            EnabledBottomButton = 2
        }

        internal enum FavoriteToggleButtonStyle
        {
            DefaultTextStar = 0,
            TextStarInItemFavoriteColor = 1,
        }

        internal enum DPadUsage
        {
            InventorySlotMovement = 0,
            Keybinds = 1,
            KeybindsWhileHoldingModifierKey = 2,
        }

        internal enum ConfigTemplate
        {
            NotCurrentlyLoadingTemplate = 0,
            BasicControllerKeybinds = 1,
            CustomControllerKeybinds = 2,
            MouseAndKeyboardWithButtons = 3,
            MouseAndKeyboardWithHotkeys = 4,
            GoldensChoice = 5,
            ResetToDefault = 6,
        }
    }
}