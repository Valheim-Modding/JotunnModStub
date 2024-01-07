using HarmonyLib;
using UnityEngine.UI;
using static QuickStackStore.ButtonRenderer;

namespace QuickStackStore
{
    internal class ButtonUIPatches
    {
        [HarmonyPatch(typeof(InventoryGui))]
        internal static class PatchInventoryGui
        {
            // slightly lower priority so we get rendered on top of equipment slot mods
            // (lower priority -> later rendering -> you get rendered on top)
            [HarmonyPriority(Priority.LowerThanNormal)]
            [HarmonyPatch(nameof(InventoryGui.Show)), HarmonyPostfix]
            private static void Show_Postfix(InventoryGui __instance)
            {
                hasOpenedInventoryOnce = true;

                MainButtonUpdate.UpdateInventoryGuiButtons(__instance);
            }

            [HarmonyPriority(Priority.LowerThanNormal)]
            [HarmonyPatch(nameof(InventoryGui.Hide)), HarmonyPostfix]
            private static void CloseInventory_Postfix()
            {
                // reset in case player forgot to turn it off
                FavoritingMode.HasCurrentlyToggledFavoriting = false;
            }

            [HarmonyPriority(Priority.LowerThanNormal)]
            [HarmonyPatch(nameof(InventoryGui.CloseContainer)), HarmonyPostfix]
            public static void CloseContainer_Postfix(InventoryGui __instance)
            {
                if (__instance.m_currentContainer != null)
                {
                    return;
                }

                var buttons = new Button[] { storeAllButton, quickStackToContainerButton, sortContainerButton, restockFromContainerButton };

                foreach (var button in buttons)
                {
                    // hide the buttons when the current container gets closed instead of relying on getting hidden when the container panel gets hidden
                    // in case a mod uses the container panel to add a custom container (like jewelcrafting) that my buttons don't work with anyway
                    if (button == null)
                    {
                        continue;
                    }

                    button.gameObject.SetActive(false);
                }
            }
        }

        [HarmonyPatch(typeof(Game))]
        internal static class PatchGame
        {
            [HarmonyPatch(nameof(Game.Logout)), HarmonyPrefix]
            internal static void ResetOnLogout()
            {
                hasOpenedInventoryOnce = false;
                TrashModule.TrashItemsPatches.hasOpenedInventoryOnce = false;

                origButtonLength = -1;
                origButtonPosition = default;
            }
        }
    }
}