using static QuickStackStore.QSSConfig;

namespace QuickStackStore
{
    internal class AreaStackRestockHelper
    {
        internal static bool IsTrueSingleplayer()
        {
            return !ZNet.m_openServer && !ZNet.m_publicServer
                && ZNet.instance && ZNet.instance.IsServer() && !ZNet.instance.IsDedicated() && ZNet.instance.GetConnectedPeers().Count == 0;
        }

        private static bool CheckWard(Container container)
        {
            return !container.m_checkGuardStone || PrivateArea.CheckAccess(container.transform.position, 0f, true, false);
        }

        private static bool CheckContainerPrivacy(Container container, long playerID)
        {
            return container.CheckAccess(playerID);
        }

        private static bool CheckPieceConfigs(Container container)
        {
            if (container.m_piece)
            {
                return container.m_piece.IsPlacedByPlayer()
                    || QuickStackRestockConfig.AllowAreaStackingToPhysicalNonPlayerBuiltContainers.Value;
            }
            else
            {
                return container.m_name == "piece_chest_drawer"
                    || QuickStackRestockConfig.AllowAreaStackingToNonPhysicalContainers.Value;
            }
        }

        private static bool IsInUseClientSide(Container container)
        {
            return container.IsInUse() || (container.m_wagon && container.m_wagon.InUse());
        }

        private static bool IsInUseServerSide(Container container)
        {
            // this is mostly used to sync the animation but it's the best we got
            return container.m_nview.GetZDO().GetInt("InUse", 0) == 1;
        }

        private static bool IsInUse(Container container)
        {
            return IsInUseServerSide(container) || IsInUseClientSide(container);
        }

        // written with extensibility in mind (I wouldn't even mind if other mods patch this)
        private static bool IsExcludedContainer(Container container)
        {
            // prevent claiming ownership of other players (e.g. through adventure backpacks)
            Player player = container.m_nview.GetComponent<Player>();

            return player && player != Player.m_localPlayer;
        }

        // written with extensibility in mind (I wouldn't even mind if other mods patch this)
        private static bool IsNonMUCExcludedContainer(Container container)
        {
            bool isExcluded = false;

            // ship chests are just too risky right now
            isExcluded |= container.transform.root.GetComponentInChildren<Ship>();

            return isExcluded;
        }

        // ownership is useless since every container always has an owner (its last user)
        // based on Container.Interact
        internal static bool ShouldAffectNonOwnerContainer(Container container, long playerID, bool isSinglePlayer)
        {
            bool basicCheck = !IsExcludedContainer(container)
                && CheckContainerPrivacy(container, playerID)
                && CheckWard(container)
                && CheckPieceConfigs(container);

            if (CompatibilitySupport.HasPlugin(CompatibilitySupport.multiUserChest))
            {
                return basicCheck;
            }

            if (!isSinglePlayer && IsNonMUCExcludedContainer(container))
            {
                return false;
            }
            else
            {
                return basicCheck && !IsInUse(container);
            }
        }

        internal static void SetNonMUCContainerInUse(Container container, bool isInUse)
        {
            container.m_inUse = isInUse;

            if (QuickStackRestockConfig.SuppressContainerSoundAndVisuals.Value)
            {
                container.m_nview.GetZDO().Set("InUse", isInUse ? 1 : 0);
            }
            else
            {
                container.SetInUse(isInUse);
            }

            ZDOMan.instance.ForceSendZDO(ZNet.GetUID(), container.m_nview.GetZDO().m_uid);
        }
    }
}