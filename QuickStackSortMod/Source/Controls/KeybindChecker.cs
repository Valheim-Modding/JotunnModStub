using BepInEx.Configuration;
using HarmonyLib;
using System.Linq;
using UnityEngine;
using static QuickStackStore.QSSConfig;

namespace QuickStackStore
{
    [HarmonyPatch]
    public static class KeybindChecker
    {
        public static bool IgnoreKeyPresses()
        {
            // removed InventoryGui.IsVisible() because we specifically want to allow that
            return IgnoreKeyPressesDueToPlayer(Player.m_localPlayer)
                || !ZNetScene.instance
                || Minimap.IsOpen()
                || Menu.IsVisible()
                || Console.IsVisible()
                || StoreGui.IsVisible()
                || TextInput.IsVisible()
                || (Chat.instance && Chat.instance.HasFocus())
                || (ZNet.instance && ZNet.instance.InPasswordDialog())
                || (TextViewer.instance && TextViewer.instance.IsVisible());
        }

        private static bool IgnoreKeyPressesDueToPlayer(Player player)
        {
            return !player
                || player.InCutscene()
                || player.IsTeleporting()
                || player.IsDead()
                || player.InPlaceMode();
        }

        // thank you to 'Margmas' for giving me this snippet from VNEI https://github.com/MSchmoecker/VNEI/blob/master/VNEI/Logic/BepInExExtensions.cs#L21
        // since KeyboardShortcut.IsPressed and KeyboardShortcut.IsDown behave unintuitively
        public static bool IsKeyDown(this KeyboardShortcut shortcut)
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
        }

        public static bool IsKeyHeld(this KeyboardShortcut shortcut)
        {
            return shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
        }

        [HarmonyPatch(typeof(Player))]
        public static class Player_Update_Patch
        {
            [HarmonyPatch(nameof(Player.Update)), HarmonyPostfix]
            public static void Postfix_Patch(Player __instance)
            {
                if (Player.m_localPlayer != __instance)
                {
                    return;
                }

                if (IgnoreKeyPresses())
                {
                    return;
                }

                if (ZInput.IsGamepadActive() && ControllerConfig.UseHardcodedControllerSupport.Value)
                {
                    HandleControllerKeys(__instance);
                }
                else
                {
                    HandleGenericKeys(__instance);
                }
            }

            private static void HandleGenericKeys(Player player)
            {
                if (GeneralConfig.OverrideKeybindBehavior.Value == OverrideKeybindBehavior.DisableAllNewHotkeys)
                {
                    return;
                }

                if (QuickStackConfig.QuickStackKeybind.Value.IsKeyDown())
                {
                    QuickStackModule.DoQuickStack(player);
                    return;
                }
                else if (RestockConfig.RestockKeybind.Value.IsKeyDown())
                {
                    RestockModule.DoRestock(player);
                    return;
                }

                if (!InventoryGui.IsVisible())
                {
                    return;
                }

                if (SortConfig.SortKeybind.Value.IsKeyDown())
                {
                    SortModule.DoSort(player);
                    return;
                }

                if (!CompatibilitySupport.DisallowAllTrashCanFeatures())
                {
                    if (TrashConfig.QuickTrashKeybind.Value.IsKeyDown())
                    {
                        TrashModule.AttemptQuickTrash();
                        return;
                    }
                    else if (TrashConfig.TrashKeybind.Value.IsKeyDown())
                    {
                        TrashModule.TrashOrTrashFlagItem(true);
                        return;
                    }
                }

                if (StoreTakeAllConfig.TakeAllKeybind.Value.IsKeyDown())
                {
                    StoreTakeAllModule.DoTakeAllWithKeybind(player);
                    return;
                }
                else if (StoreTakeAllConfig.StoreAllKeybind.Value.IsKeyDown())
                {
                    StoreTakeAllModule.DoStoreAllWithKeybind(player);
                    return;
                }
            }

            private static void HandleControllerKeys(Player player)
            {
                if (!InventoryGui.IsVisible())
                {
                    return;
                }

                if (ZInput.GetButtonDown(joyGetButtonDownPrefix + joySort))
                {
                    SortModule.DoSort(player);
                    return;
                }
                else if (ZInput.GetButtonDown(joyGetButtonDownPrefix + joyStoreAll))
                {
                    StoreTakeAllModule.DoStoreAllWithKeybind(player);
                    return;
                }

                if (ControllerConfig.ControllerDPadUsageInInventoryGrid.Value == DPadUsage.InventorySlotMovement)
                {
                    return;
                }

                if (ControllerConfig.ControllerDPadUsageInInventoryGrid.Value == DPadUsage.KeybindsWhileHoldingModifierKey
                    && !IsKeyHeld(ControllerConfig.ControllerDPadUsageModifierKeybind.Value))
                {
                    return;
                }

                if (ZInput.GetButtonDown(joyGetButtonDownPrefix + joyQuickStack))
                {
                    QuickStackModule.DoQuickStack(player);
                    return;
                }
                else if (ZInput.GetButtonDown(joyGetButtonDownPrefix + joyRestock))
                {
                    RestockModule.DoRestock(player);
                    return;
                }
                else if (ZInput.GetButtonDown(joyGetButtonDownPrefix + joyFavoriteToggling))
                {
                    FavoritingMode.ToggleFavoriteToggling();
                    return;
                }

                if (CompatibilitySupport.DisallowAllTrashCanFeatures())
                {
                    return;
                }

                if (ZInput.GetButtonDown(joyGetButtonDownPrefix + joyTrash))
                {
                    TrashModule.TrashOrTrashFlagItem();
                }
            }
        }

        internal const string joyTranslationPrefix = "KEY_";
        internal const string joyGetButtonDownPrefix = "Joy";

        internal const string joySort = "Back";
        internal const string joyStoreAll = "RStick";

        internal const string joyQuickStack = "DPadDown";
        internal const string joyRestock = "DPadUp";
        internal const string joyFavoriteToggling = "DPadLeft";
        internal const string joyTrash = "DPadRight";
    }
}