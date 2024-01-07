using BepInEx.Bootstrap;
using BepInEx.Configuration;
using System.Linq;
using System.Reflection;
using static QuickStackStore.QSSConfig;

namespace QuickStackStore
{
    public static class CompatibilitySupport
    {
        private static MethodInfo IsComfyArmorSlot;
        private static FieldInfo IsQuiverEnabled;
        private static FieldInfo QuiverRowIndex;
        private static FieldInfo AedenAddEquipmentRow;
        private static FieldInfo OdinExAddEquipmentRow;
        private static FieldInfo OdinQOLAddEquipmentRow;
        private static FieldInfo AzuEPIAddEquipmentRow;
        private static FieldInfo RandyQuickSlotsEnabled;

        public const string aeden = "aedenthorn.ExtendedPlayerInventory";
        public const string comfy = "com.bruce.valheim.comfyquickslots";
        public const string odinPlus = "com.odinplusqol.mod";
        public const string odinExInv = "odinplusqol.OdinsExtendedInventory";
        public const string randy = "randyknapp.mods.equipmentandquickslots";
        public const string azuEPI = "Azumatt.AzuExtendedPlayerInventory";
        public const string auga = "randyknapp.mods.auga";

        public static bool isUsingAzuEPIWithAPI = false;
        public static bool isUsingAzuEPIWithQuickslotCompatibleAPI = false;

        // intentionally up here so, we don't forget to update it
        public static bool HasAedenLikeEquipOrQuickSlotPlugin()
        {
            return HasPlugin(aeden) || HasPlugin(odinExInv) || HasPlugin(odinPlus) || (HasPlugin(azuEPI) && !isUsingAzuEPIWithAPI);
        }

        public const string betterArchery = "ishid4.mods.betterarchery";
        public const string smartContainers = "flueno.SmartContainers";
        public const string backpacks = "org.bepinex.plugins.backpacks";
        public const string multiUserChest = "com.maxsch.valheim.MultiUserChest";
        public const string jewelCrafting = "org.bepinex.plugins.jewelcrafting";
        public const string recyclePlus = "TastyChickenLegs.RecyclePlus";

        public static System.Version mucUpdateVersion = new System.Version(0, 4, 0);
        public static System.Version azuEPIOnOffUpdate = new System.Version(1, 2, 0);
        public static System.Version azuEPIQuickSlotAPIUpdate = new System.Version(1, 3, 2);

        public static bool AllowAreaStackingRestocking()
        {
            return AreaStackRestockHelper.IsTrueSingleplayer() || HasPlugin(multiUserChest) || QuickStackRestockConfig.AllowAreaStackingInMultiplayerWithoutMUC.Value;
        }

        public static bool DisallowAllTrashCanFeatures()
        {
            return HasPlugin(recyclePlus);
        }

        public static bool ShouldBlockChangesToTakeAllButton()
        {
            return StoreTakeAllConfig.NeverMoveTakeAllButton.Value || ShouldBlockChangesToTakeAllButtonDueToPlugin();
        }

        public static bool ShouldBlockChangesToTakeAllButtonDueToPlugin()
        {
            return HasPlugin(smartContainers) || HasPlugin(backpacks) || HasPlugin(jewelCrafting);
        }

        public static bool HasOutdatedMUCPlugin()
        {
            if (Chainloader.PluginInfos.ContainsKey(multiUserChest))
            {
                var info = Chainloader.PluginInfos[multiUserChest];

                return info.Metadata.Version < mucUpdateVersion;
            }
            else
            {
                return false;
            }
        }

        public static bool HasAzuEPIWithQuickslotCompatibleAPI()
        {
            if (Chainloader.PluginInfos.ContainsKey(azuEPI))
            {
                var info = Chainloader.PluginInfos[azuEPI];

                return info.Metadata.Version >= azuEPIQuickSlotAPIUpdate;
            }
            else
            {
                return false;
            }
        }

        public static bool HasPlugin(string guid)
        {
            return Chainloader.PluginInfos.ContainsKey(guid);
        }

        public enum RandyStatus
        {
            Disabled,
            EnabledWithoutQuickSlots,
            EnabledWithQuickSlots
        }

        public static RandyStatus HasRandyPlugin()
        {
            if (!HasPlugin(randy))
            {
                return RandyStatus.Disabled;
            }

            RandyStatus randyStatus = RandyStatus.EnabledWithQuickSlots;

            if (RandyQuickSlotsEnabled == null)
            {
                var assembly = Assembly.Load("EquipmentAndQuickSlots");

                if (assembly != null)
                {
                    var type = assembly.GetTypes().First(a => a.IsClass && a.Name == "EquipmentAndQuickSlots");
                    var pubStaticFields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                    RandyQuickSlotsEnabled = pubStaticFields.First(t => t.Name == "QuickSlotsEnabled");
                }
            }

            if (RandyQuickSlotsEnabled?.GetValue(null) is ConfigEntry<bool> config && !config.Value)
            {
                randyStatus = RandyStatus.EnabledWithoutQuickSlots;
            }

            return randyStatus;
        }

        public static bool HasPluginThatRequiresMiniButtonVMove()
        {
            return HasAedenLikeEquipOrQuickSlotPlugin() || (HasPlugin(azuEPI) && isUsingAzuEPIWithAPI);
        }

        public static bool IsEquipSlot(int inventoryHeight, int inventoryWidth, Vector2i itemPos)
        {
            return InternalIsEquipOrQuickSlot(inventoryHeight, inventoryWidth, itemPos, false);
        }

        public static bool IsEquipOrQuickSlot(int inventoryHeight, int inventoryWidth, Vector2i itemPos)
        {
            return InternalIsEquipOrQuickSlot(inventoryHeight, inventoryWidth, itemPos, true);
        }

        private static bool InternalIsEquipOrQuickSlot(int inventoryHeight, int inventoryWidth, Vector2i itemPos, bool includeRestockableSlots)
        {
            //if (HasPlugin(randy))
            //{
            //    // randyknapps mod ignores everything this mod does anyway, so no need for specific compatibility
            //}

            if (HasPlugin(aeden) && IsAedenLikeEquipOrQuickSlot(ref AedenAddEquipmentRow, "ExtendedPlayerInventory", "BepInExPlugin", "addEquipmentRow", inventoryHeight, itemPos, includeRestockableSlots))
            {
                return true;
            }

            if (HasPlugin(odinExInv) && IsAedenLikeEquipOrQuickSlot(ref OdinExAddEquipmentRow, "OdinsExtendedInventory", "OdinsExtendedInventoryPlugin", "addEquipmentRow", inventoryHeight, itemPos, includeRestockableSlots))
            {
                return true;
            }

            if (HasPlugin(odinPlus) && IsAedenLikeEquipOrQuickSlot(ref OdinQOLAddEquipmentRow, "OdinQOL", "QuickAccessBar", "AddEquipmentRow", inventoryHeight, itemPos, includeRestockableSlots))
            {
                return true;
            }

            if (HasPlugin(azuEPI) && !isUsingAzuEPIWithAPI && IsAedenLikeEquipOrQuickSlot(ref AzuEPIAddEquipmentRow, "AzuExtendedPlayerInventory", "AzuExtendedPlayerInventoryPlugin", "AddEquipmentRow", inventoryHeight, itemPos, includeRestockableSlots))
            {
                return true;
            }

            if (HasPlugin(azuEPI) && isUsingAzuEPIWithAPI && IsNewAzuEPIEquipOrQuickSlot(inventoryHeight, inventoryWidth, itemPos, includeRestockableSlots))
            {
                return true;
            }

            if (HasPlugin(comfy) && IsComfyEquipOrQuickSlot(inventoryHeight, itemPos, includeRestockableSlots))
            {
                return true;
            }

            if (HasPlugin(betterArchery) && IsBetterArcheryQuiverSlot(itemPos, includeRestockableSlots))
            {
                return true;
            }

            return false;
        }

        private static bool HasAedenLikeEquipmentRow(ref FieldInfo fieldInfo, string assemblyName, string className, string fieldName)
        {
            if (fieldInfo == null)
            {
                var assembly = Assembly.Load(assemblyName);

                if (assembly != null)
                {
                    var type = assembly.GetTypes().First(a => a.IsClass && a.Name == className);
                    var pubStaticFields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                    fieldInfo = pubStaticFields.First(t => t.Name == fieldName);
                }
            }

            if (fieldInfo == AzuEPIAddEquipmentRow && Chainloader.PluginInfos[azuEPI].Metadata.Version >= azuEPIOnOffUpdate)
            {
                // here it's actually of type ConfigEntry<Toggle>, which the usual config enum
                return fieldInfo?.GetValue(null) is ConfigEntryBase config && (int)config.BoxedValue != 0;
            }
            else
            {
                return fieldInfo?.GetValue(null) is ConfigEntry<bool> config && config.Value;
            }
        }

        private static bool IsNewAzuEPIEquipOrQuickSlot(int inventoryHeight, int inventoryWidth, Vector2i itemPos, bool includeRestockableSlots)
        {
            if (!HasAedenLikeEquipmentRow(ref AzuEPIAddEquipmentRow, "AzuExtendedPlayerInventory", "AzuExtendedPlayerInventoryPlugin", "AddEquipmentRow"))
            {
                return false;
            }

            if (!includeRestockableSlots && isUsingAzuEPIWithQuickslotCompatibleAPI)
            {
                foreach (var item in AzuExtendedPlayerInventory.API.GetQuickSlotsItems())
                {
                    if (item.m_gridPos == itemPos)
                    {
                        return false;
                    }
                }
            }

            int customSlotAddedRows = AzuExtendedPlayerInventory.API.GetAddedRows(inventoryWidth);

            for (int i = 1; i <= customSlotAddedRows; i++)
            {
                if (itemPos.y == inventoryHeight - i)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAedenLikeEquipOrQuickSlot(ref FieldInfo fieldInfo, string assemblyName, string className, string fieldName, int inventoryHeight, Vector2i itemPos, bool checkForRestockableSlots)
        {
            if (!HasAedenLikeEquipmentRow(ref fieldInfo, assemblyName, className, fieldName))
            {
                return false;
            }

            bool isEquipmentRow = itemPos.y == inventoryHeight - 1;

            return isEquipmentRow && (checkForRestockableSlots || itemPos.x < 5 || itemPos.x > 7);
        }

        private static bool IsComfyEquipOrQuickSlot(int inventoryHeight, Vector2i itemPos, bool includeRestockableSlots)
        {
            if (IsComfyArmorSlot == null)
            {
                var assembly = Assembly.Load("ComfyQuickSlots");

                if (assembly != null)
                {
                    var type = assembly.GetTypes().First(a => a.IsClass && a.Name == "ComfyQuickSlots");
                    var pubStaticMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                    IsComfyArmorSlot = pubStaticMethods.First(t => t.Name == "IsArmorSlot" && t.GetParameters().Length == 1);
                }
            }

            if (IsComfyArmorSlot?.Invoke(null, new object[] { itemPos }) is bool isArmorSlot && isArmorSlot)
            {
                return true;
            }

            if (includeRestockableSlots)
            {
                // check for quickslot (could also be armor slot)
                if (IsComfyArmorSlot?.Invoke(null, new object[] { new Vector2i(itemPos.x - 3, itemPos.y) }) is bool isArmorOrQuickSlot && isArmorOrQuickSlot)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBetterArcheryQuiverSlot(Vector2i itemPos, bool includeRestockableSlots)
        {
            if (IsQuiverEnabled == null || QuiverRowIndex == null)
            {
                var assembly = Assembly.Load("BetterArchery");

                if (assembly != null)
                {
                    var type = assembly.GetTypes().First(a => a.IsClass && a.Name == "BetterArchery");
                    var pubStaticFields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                    IsQuiverEnabled = pubStaticFields.First(t => t.Name == "configQuiverEnabled");
                    QuiverRowIndex = pubStaticFields.First(t => t.Name == "QuiverRowIndex");
                }
            }

            if (!(IsQuiverEnabled?.GetValue(null) is ConfigEntry<bool> config) || !config.Value)
            {
                return false;
            }

            // this would also return the same thing: GetBonusInventoryRowIndex
            if (QuiverRowIndex?.GetValue(null) is int rowIndex)
            {
                // it doesn't make sense for it to be the hotkey bar
                if (rowIndex == 0)
                {
                    return false;
                }

                if (itemPos.y == rowIndex && (includeRestockableSlots || itemPos.x < 0 || itemPos.x > 2))
                {
                    return true;
                }

                // for some reason 'Better Archery' adds two entire rows and doesn't even use this one (probably for backwards compatibility)
                if (itemPos.y == rowIndex - 1)
                {
                    return true;
                }
            }

            return false;
        }
    }
}