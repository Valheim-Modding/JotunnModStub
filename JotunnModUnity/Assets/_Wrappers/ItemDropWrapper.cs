using UnityEngine;

namespace ValheimMod.UnityWrappers
{
    /// <summary>
    /// A wrapper for Valheim's <see cref="ItemDrop" />.
    /// </summary>
    public class ItemDropWrapper : ItemDrop
    {
        public bool getFromRuntime = false;
        public bool includeInRelease = false;

        public ItemDrop runtimeItemDrop
        {
            get
            {
                if (!getFromRuntime) return this;

                var prefab = ObjectDB.instance.GetItemPrefab(gameObject.name);
                if (prefab == null) return this;

                return prefab.GetComponent<ItemDrop>();
            }
        }
    }
}