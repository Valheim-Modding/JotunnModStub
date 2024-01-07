using HarmonyLib;
using System;
using System.Text;
using UnityEngine;
using static QuickStackStore.QSSConfig;

namespace QuickStackStore
{
    [HarmonyPatch(typeof(ItemDrop.ItemData))]
    internal static class TooltipRenderer
    {
        [HarmonyPatch(nameof(ItemDrop.ItemData.GetTooltip), new Type[]
        {
            typeof(ItemDrop.ItemData),
            typeof(int),
            typeof(bool),
            typeof(float)
        })]
        [HarmonyPostfix]
        public static void GetTooltip(ItemDrop.ItemData item, bool crafting, ref string __result)
        {
            if (crafting || !FavoriteConfig.DisplayTooltipHint.Value)
            {
                return;
            }

            StringBuilder stringBuilder = new StringBuilder(256);
            stringBuilder.Append(__result);

            var userConfig = UserConfig.GetPlayerConfig(Player.m_localPlayer.GetPlayerID());

            if (userConfig.IsItemNameFavorited(item.m_shared))
            {
                string color = ColorUtility.ToHtmlStringRGB(FavoriteConfig.BorderColorFavoritedItem.Value);

                stringBuilder.Append($"\n<color=#{color}>{LocalizationConfig.GetRelevantTranslation(LocalizationConfig.FavoritedItemTooltip, nameof(LocalizationConfig.FavoritedItemTooltip))}</color>");
            }
            else if (userConfig.IsItemNameConsideredTrashFlagged(item.m_shared))
            {
                string color = ColorUtility.ToHtmlStringRGB(FavoriteConfig.BorderColorTrashFlaggedItem.Value);

                stringBuilder.Append($"\n<color=#{color}>{LocalizationConfig.GetRelevantTranslation(LocalizationConfig.TrashFlaggedItemTooltip, nameof(LocalizationConfig.TrashFlaggedItemTooltip))}</color>");
            }

            __result = stringBuilder.ToString();
        }
    }
}