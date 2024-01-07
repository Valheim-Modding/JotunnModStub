using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ItemDrawersKG;

public static class UtilsKG
{
    public static string Localize(this string s) => Localization.instance.Localize(s);

    public static int CustomCountItems(string prefab, int level)
    {
        int num = 0;
        foreach (ItemDrop.ItemData itemData in Player.m_localPlayer.m_inventory.m_inventory)
        {
            if (itemData.m_dropPrefab.name == prefab && level == itemData.m_quality)
            {
                num += itemData.m_stack;
            }
        }

        return num;
    }

    public static void CustomRemoveItems(string prefab, int amount, int level)
    {
        foreach (ItemDrop.ItemData itemData in Player.m_localPlayer.m_inventory.m_inventory)
        {
            if (itemData.m_dropPrefab.name == prefab && itemData.m_quality == level)
            {
                int num = Mathf.Min(itemData.m_stack, amount);
                itemData.m_stack -= num;
                amount -= num;
                if (amount <= 0)
                    break;
            }
        }

        Player.m_localPlayer.m_inventory.m_inventory.RemoveAll(x => x.m_stack <= 0);
        Player.m_localPlayer.m_inventory.Changed();
    }

    public static void InstantiateItem(GameObject prefab, int stack, int level)
    {
        Player p = Player.m_localPlayer;
        if (!p || !prefab) return;
        if (prefab.GetComponent<ItemDrop>() is not { } item) return;
        
        if (item.m_itemData.m_shared.m_maxStackSize > 1)
        {
            while (stack > 0)
            {
                int addStack = Math.Min(stack, item.m_itemData.m_shared.m_maxStackSize);
                stack -= addStack;
                ItemDrop itemDrop = Object.Instantiate(prefab, p.transform.position + Vector3.up * 1.5f, Quaternion.identity).GetComponent<ItemDrop>();
                itemDrop.m_itemData.m_stack = addStack;
                itemDrop.m_itemData.m_durability = item.m_itemData.GetMaxDurability();
                itemDrop.Save();
                if (p.m_inventory.CanAddItem(itemDrop.gameObject))
                {
                    p.m_inventory.AddItem(itemDrop.m_itemData);
                    ZNetScene.instance.Destroy(itemDrop.gameObject);
                }
            }
        }
        else
        { 
            for (int i = 0; i < stack; ++i)
            {
                GameObject go = Object.Instantiate(prefab, p.transform.position + Vector3.up * 1.5f, Quaternion.identity);
                ItemDrop itemDrop = go.GetComponent<ItemDrop>();
                itemDrop.m_itemData.m_quality = level;
                itemDrop.m_itemData.m_durability = itemDrop.m_itemData.GetMaxDurability();
                itemDrop.Save();
                if (p.m_inventory.CanAddItem(go))
                {
                    p.m_inventory.AddItem(itemDrop.m_itemData);
                    ZNetScene.instance.Destroy(go);
                }
            }
        }
    }

    public static void InstantiateAtPos(GameObject prefab, int stack, int level, Vector3 pos)
    {
        Player p = Player.m_localPlayer;
        if (!p || !prefab) return;
        ItemDrop item = prefab.GetComponent<ItemDrop>();
        while (stack > 0)
        {
            int addStack = Math.Min(stack, item.m_itemData.m_shared.m_maxStackSize);
            stack -= addStack;
            ItemDrop itemDrop = Object.Instantiate(prefab, pos, Quaternion.identity).GetComponent<ItemDrop>();
            itemDrop.m_itemData.m_stack = addStack;
            float durability = item.m_itemData.GetMaxDurability();
            itemDrop.m_itemData.m_durability = durability;
            itemDrop.Save();
        }
    }
}