using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using static QuickStackStore.QSSConfig;

namespace QuickStackStore
{
    public class UserConfig
    {
        private static readonly Dictionary<long, UserConfig> playerConfigs = new Dictionary<long, UserConfig>();

        public static UserConfig GetPlayerConfig(long playerID)
        {
            if (playerConfigs.TryGetValue(playerID, out UserConfig userConfig))
            {
                return userConfig;
            }
            else
            {
                userConfig = new UserConfig(playerID);
                playerConfigs[playerID] = userConfig;

                return userConfig;
            }
        }

        /// <summary>
        /// Create a user config for this local save file
        /// </summary>
        public UserConfig(long uid)
        {
            this._uid = uid;
            this._configPath = Path.Combine(Paths.ConfigPath, $"QuickStackStore_player_{this._uid}.dat");
            this.Load();
        }

        internal void ResetAllFavoriting()
        {
            Helper.LogO("Resetting all favoriting data!", DebugLevel.Warning);

            this.favoritedSlots = new HashSet<Vector2i>();
            this.favoritedItems = new HashSet<string>();
            this.trashFlaggedItems = new HashSet<string>();

            Save();
        }

        private void Save()
        {
            using (Stream stream = File.Open(this._configPath, FileMode.Create))
            {
                var tupledSlots = new List<Tuple<int, int>>();

                foreach (var item in this.favoritedSlots)
                {
                    tupledSlots.Add(new Tuple<int, int>(item.x, item.y));
                }

                _bf.Serialize(stream, tupledSlots);
                _bf.Serialize(stream, this.favoritedItems.ToList());
                _bf.Serialize(stream, this.trashFlaggedItems.ToList());
            }
        }

        private static object TryDeserialize(Stream stream)
        {
            object result;

            try
            {
                result = _bf.Deserialize(stream);
            }
            catch (SerializationException)
            {
                result = null;
            }

            return result;
        }

        private static void LoadProperty<T>(Stream file, out T property) where T : new()
        {
            object obj = TryDeserialize(file);

            if (obj is T)
            {
                T t = (T)((object)obj);
                property = t;
                return;
            }

            property = Activator.CreateInstance<T>();
        }

        private void Load()
        {
            using (Stream stream = File.Open(this._configPath, FileMode.OpenOrCreate))
            {
                stream.Seek(0L, SeekOrigin.Begin);

                favoritedSlots = new HashSet<Vector2i>();
                favoritedItems = new HashSet<string>();
                trashFlaggedItems = new HashSet<string>();

                var deserializedFavoritedSlots = new List<Tuple<int, int>>();
                LoadProperty(stream, out deserializedFavoritedSlots);

                foreach (var item in deserializedFavoritedSlots)
                {
                    favoritedSlots.Add(new Vector2i(item.Item1, item.Item2));
                }

                var deserializedFavoritedItems = new List<string>();
                LoadProperty(stream, out deserializedFavoritedItems);

                foreach (var item in deserializedFavoritedItems)
                {
                    favoritedItems.Add(item);
                }

                var deserializedTrashFlaggedItems = new List<string>();
                LoadProperty(stream, out deserializedTrashFlaggedItems);

                foreach (var item in deserializedTrashFlaggedItems)
                {
                    trashFlaggedItems.Add(item);
                }
            }
        }

        public void ToggleSlotFavoriting(Vector2i position)
        {
            this.favoritedSlots.XAdd(position);
            this.Save();
        }

        public bool ToggleItemNameFavoriting(ItemDrop.ItemData.SharedData item)
        {
            if (this.trashFlaggedItems.Contains(item.m_name))
            {
                return false;
            }

            this.favoritedItems.XAdd(item.m_name);
            this.Save();

            return true;
        }

        public bool ToggleItemNameTrashFlagging(ItemDrop.ItemData.SharedData item)
        {
            if (this.favoritedItems.Contains(item.m_name))
            {
                return false;
            }

            this.trashFlaggedItems.XAdd(item.m_name);
            this.Save();

            return true;
        }

        public bool IsSlotFavorited(Vector2i position)
        {
            return this.favoritedSlots.Contains(position);
        }

        public bool IsItemNameFavorited(ItemDrop.ItemData.SharedData item)
        {
            return this.favoritedItems.Contains(item.m_name);
        }

        public bool IsItemNameLiterallyTrashFlagged(ItemDrop.ItemData.SharedData item)
        {
            return this.trashFlaggedItems.Contains(item.m_name);
        }

        public bool IsItemNameConsideredTrashFlagged(ItemDrop.ItemData.SharedData item)
        {
            if (CompatibilitySupport.DisallowAllTrashCanFeatures())
            {
                return false;
            }

            if (IsItemNameLiterallyTrashFlagged(item))
            {
                return true;
            }

            if (!TrashConfig.AlwaysConsiderTrophiesTrashFlagged.Value)
            {
                return false;
            }

            return item.m_itemType == ItemDrop.ItemData.ItemType.Trophy && !IsItemNameFavorited(item);
        }

        public bool IsItemNameOrSlotFavorited(ItemDrop.ItemData item)
        {
            return IsItemNameFavorited(item.m_shared) || IsSlotFavorited(item.m_gridPos);
        }

        private readonly string _configPath = string.Empty;
        private HashSet<Vector2i> favoritedSlots;
        private HashSet<string> favoritedItems;
        private HashSet<string> trashFlaggedItems;
        private readonly long _uid;
        private static readonly BinaryFormatter _bf = new BinaryFormatter();
    }

    public static class CollectionExtension
    {
        public static bool XAdd<T>(this HashSet<T> instance, T item)
        {
            if (instance.Contains(item))
            {
                instance.Remove(item);
                return false;
            }
            else
            {
                instance.Add(item);
                return true;
            }
        }
    }
}