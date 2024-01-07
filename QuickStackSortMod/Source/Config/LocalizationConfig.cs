using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace QuickStackStore
{
    [HarmonyPatch(typeof(Localization))]
    internal class LocalizationPatch
    {
        [HarmonyPatch(nameof(Localization.SetupLanguage)), HarmonyPostfix]
        private static void SetupLanguagePatch(Localization __instance, string language)
        {
            LocalizationConfig.FixTakeAllDefaultText(__instance, language);
        }
    }

    internal class LocalizationConfig
    {
        public static ConfigEntry<string> RestockLabelCharacter;
        public static ConfigEntry<string> QuickStackLabelCharacter;
        public static ConfigEntry<string> SortLabelCharacter;

        public static ConfigEntry<string> QuickStackResultMessageNothing;
        public static ConfigEntry<string> QuickStackResultMessageNone;
        public static ConfigEntry<string> QuickStackResultMessageOne;
        public static ConfigEntry<string> QuickStackResultMessageMore;

        public static ConfigEntry<string> RestockResultMessageNothing;
        public static ConfigEntry<string> RestockResultMessageNone;
        public static ConfigEntry<string> RestockResultMessagePartial;
        public static ConfigEntry<string> RestockResultMessageFull;

        public static ConfigEntry<string> QuickStackLabel;
        public static ConfigEntry<string> StoreAllLabel;
        public static ConfigEntry<string> SortLabel;
        public static ConfigEntry<string> RestockLabel;
        public static ConfigEntry<string> TrashLabel;
        public static ConfigEntry<string> TakeAllLabel;

        public static ConfigEntry<string> SortByInternalNameLabel;
        public static ConfigEntry<string> SortByTranslatedNameLabel;
        public static ConfigEntry<string> SortByValueLabel;
        public static ConfigEntry<string> SortByWeightLabel;
        public static ConfigEntry<string> SortByTypeLabel;

        public static ConfigEntry<string> TrashConfirmationOkayButton;
        public static ConfigEntry<string> QuickTrashConfirmation;
        public static ConfigEntry<string> CantTrashFavoritedItemWarning;
        public static ConfigEntry<string> CantTrashFlagFavoritedItemWarning;
        public static ConfigEntry<string> CantTrashHotkeyBarItemWarning;
        public static ConfigEntry<string> CantFavoriteTrashFlaggedItemWarning;

        public static ConfigEntry<string> FavoritedItemTooltip;
        public static ConfigEntry<string> TrashFlaggedItemTooltip;

        public const string takeAllKey = "inventory_takeall";

        internal static string GetRelevantTranslation(ConfigEntry<string> config, string configName)
        {
            return !(config?.Value).IsNullOrWhiteSpace() ? config.Value : Localization.instance.Translate($"quickstackstore_{configName.ToLower()}");
        }

        internal static void FixTakeAllDefaultText(Localization localization, string language)
        {
            if (localization.m_translations.ContainsKey(takeAllKey))
            {
                switch (language)
                {
                    case "English":
                        localization.m_translations[takeAllKey] = "Take All";
                        break;

                    case "Russian":
                        localization.m_translations[takeAllKey] = "взять всё";
                        break;

                    case "French":
                        localization.m_translations[takeAllKey] = "Tout Prendre";
                        break;

                    case "Portuguese_Brazilian":
                        localization.m_translations[takeAllKey] = "Pegar Tudo";
                        break;

                    default:
                        break;
                }
            }
        }
    }
}