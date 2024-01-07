using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static QuickStackStore.QSSConfig;

namespace QuickStackStore
{
    [HarmonyPatch(typeof(InventoryGrid))]
    internal class InventoryGridButtonHandlingPatches
    {
        [HarmonyPatch(nameof(InventoryGrid.OnRightClick)), HarmonyPrefix]
        private static bool OnRightClick(InventoryGrid __instance, UIInputHandler element)
        {
            return HandleClick(__instance, element, false);
        }

        [HarmonyPatch(nameof(InventoryGrid.OnLeftClick)), HarmonyPrefix]
        private static bool OnLeftClick(InventoryGrid __instance, UIInputHandler clickHandler)
        {
            return HandleClick(__instance, clickHandler, true);
        }

        private static bool HandleClick(InventoryGrid __instance, UIInputHandler clickHandler, bool isLeftClick)
        {
            if (ShouldIgnoreFavoritingClick(__instance))
            {
                return true;
            }

            Vector2i buttonPos = __instance.GetButtonPos(clickHandler.gameObject);

            return HandleClickInternal(__instance, buttonPos, isLeftClick);
        }

        [HarmonyPatch(nameof(InventoryGrid.UpdateGamepad)), HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> UpdateGamepad_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo GetButtonDownMethod = AccessTools.DeclaredMethod(typeof(ZInput), nameof(ZInput.GetButtonDown));
            MethodInfo GetButtonDownWithConfigMethod = AccessTools.DeclaredMethod(typeof(InventoryGridButtonHandlingPatches), nameof(ReactToGetButtonDown));

            List<CodeInstruction> instructionsList = instructions.ToList();

            foreach (CodeInstruction instruction in instructionsList)
            {
                if (instruction.opcode != OpCodes.Call || !instruction.OperandIs(GetButtonDownMethod))
                {
                    yield return instruction;
                    continue;
                }

                // put another copy of the keybind name onto the stack
                yield return new CodeInstruction(OpCodes.Dup);

                // call the original ZInput.GetButtonDown
                yield return instruction;

                // put a 'this' object onto the stack
                yield return new CodeInstruction(OpCodes.Ldarg_0);

                // call my version and consume the boolean return value currently on the stack, replacing it with a potentially edited version
                yield return new CodeInstruction(OpCodes.Call, GetButtonDownWithConfigMethod);
            }
        }

        public static bool ReactToGetButtonDown(string keybindName, bool originalReturnValue, InventoryGrid __instance)
        {
            if (__instance != InventoryGui.instance.m_playerGrid)
            {
                return originalReturnValue;
            }

            if (!__instance.m_uiGroup.IsActive)
            {
                return originalReturnValue;
            }

            switch (keybindName)
            {
                case "JoyDPadLeft":
                case "JoyDPadRight":
                case "JoyDPadDown":
                case "JoyDPadUp":
                    if (originalReturnValue && ShouldSuppressDPad())
                    {
                        return false;
                    }
                    else
                    {
                        return originalReturnValue;
                    }

                case "JoyButtonA":
                case "JoyButtonX":
                    if (!originalReturnValue || ShouldIgnoreFavoritingClick(__instance))
                    {
                        return originalReturnValue;
                    }
                    else
                    {
                        return HandleClickInternal(__instance, __instance.m_selected, keybindName == "JoyButtonA");
                    }

                default:
                    return originalReturnValue;
            }
        }

        private static bool ShouldSuppressDPad()
        {
            switch (ControllerConfig.ControllerDPadUsageInInventoryGrid.Value)
            {
                case DPadUsage.InventorySlotMovement:
                    return false;

                case DPadUsage.KeybindsWhileHoldingModifierKey:
                    return KeybindChecker.IsKeyHeld(ControllerConfig.ControllerDPadUsageModifierKeybind.Value);

                case DPadUsage.Keybinds:
                default:
                    return true;
            }
        }

        private static bool ShouldIgnoreFavoritingClick(InventoryGrid __instance)
        {
            if (__instance != InventoryGui.instance.m_playerGrid)
            {
                return true;
            }

            if (Player.m_localPlayer.IsTeleporting())
            {
                return true;
            }

            if (InventoryGui.instance.m_dragGo)
            {
                return true;
            }

            if (!FavoritingMode.IsInFavoritingMode())
            {
                return true;
            }

            return false;
        }

        private static bool HandleClickInternal(InventoryGrid __instance, Vector2i buttonPos, bool isLeftClick)
        {
            if (buttonPos == new Vector2i(-1, -1))
            {
                return true;
            }

            Player localPlayer = Player.m_localPlayer;

            if (!isLeftClick)
            {
                UserConfig.GetPlayerConfig(localPlayer.GetPlayerID()).ToggleSlotFavoriting(buttonPos);
            }
            else
            {
                ItemDrop.ItemData itemAt = __instance.m_inventory.GetItemAt(buttonPos.x, buttonPos.y);

                if (itemAt == null)
                {
                    return true;
                }

                bool wasToggleSuccessful = UserConfig.GetPlayerConfig(localPlayer.GetPlayerID()).ToggleItemNameFavoriting(itemAt.m_shared);

                if (!wasToggleSuccessful)
                {
                    localPlayer.Message(MessageHud.MessageType.Center, LocalizationConfig.GetRelevantTranslation(LocalizationConfig.CantFavoriteTrashFlaggedItemWarning, nameof(LocalizationConfig.CantFavoriteTrashFlaggedItemWarning)), 0, null);
                }
            }

            return false;
        }
    }
}