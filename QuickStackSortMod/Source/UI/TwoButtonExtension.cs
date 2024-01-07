using static QuickStackStore.QSSConfig;

namespace QuickStackStore
{
    internal static class TwoButtonExtension
    {
        internal static bool ShouldSpawnMiniInventoryButton(this ShowTwoButtons showTwoButtons)
        {
            switch (showTwoButtons)
            {
                case ShowTwoButtons.Both:
                case ShowTwoButtons.OnlyInventoryButton:
                case ShowTwoButtons.BothButDependingOnContext:
                    return true;

                default:
                    return false;
            }
        }

        internal static bool ShouldShowMiniInventoryButton(this ShowTwoButtons showTwoButtons, InventoryGui gui)
        {
            switch (showTwoButtons)
            {
                case ShowTwoButtons.Both:
                case ShowTwoButtons.OnlyInventoryButton:
                    return true;

                case ShowTwoButtons.BothButDependingOnContext:
                    return !gui.m_currentContainer;

                default:
                    return false;
            }
        }

        internal static bool ShouldSpawnContainerButton(this ShowTwoButtons showTwoButtons)
        {
            switch (showTwoButtons)
            {
                case ShowTwoButtons.Both:
                case ShowTwoButtons.OnlyContainerButton:
                case ShowTwoButtons.BothButDependingOnContext:
                    return true;

                default:
                    return false;
            }
        }

        internal static bool ShouldShowContainerButton(this ShowTwoButtons showTwoButtons, InventoryGui gui)
        {
            switch (showTwoButtons)
            {
                case ShowTwoButtons.Both:
                case ShowTwoButtons.OnlyContainerButton:
                    return true;

                case ShowTwoButtons.BothButDependingOnContext:
                    return gui.m_currentContainer;

                default:
                    return false;
            }
        }
    }
}