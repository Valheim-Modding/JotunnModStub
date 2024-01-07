using BepInEx.Configuration;
using ServerSync;
using System;

namespace QuickStackStore
{
    internal static class ServerSyncWrapper
    {
        internal static ConfigEntry<T> BindSyncLocker<T>(this ConfigFile configFile, ConfigSync serverSyncInstance, string group, string name, T value, ConfigDescription description) where T : IConvertible
        {
            ConfigEntry<T> configEntry = configFile.Bind(group, name, value, description);

            serverSyncInstance.AddLockingConfigEntry(configEntry);

            return configEntry;
        }

        internal static ConfigEntry<T> BindSyncLocker<T>(this ConfigFile configFile, ConfigSync serverSyncInstance, string group, string name, T value, string description) where T : IConvertible
        {
            return BindSyncLocker(configFile, serverSyncInstance, group, name, value, new ConfigDescription(description));
        }

        internal static ConfigEntry<T> BindSynced<T>(this ConfigFile configFile, ConfigSync serverSyncInstance, string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = configFile.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = serverSyncInstance.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        internal static ConfigEntry<T> BindSynced<T>(this ConfigFile configFile, ConfigSync serverSyncInstance, string group, string name, T value, string description, bool synchronizedSetting = true)
        {
            return BindSynced(configFile, serverSyncInstance, group, name, value, new ConfigDescription(description), synchronizedSetting);
        }
    }
}