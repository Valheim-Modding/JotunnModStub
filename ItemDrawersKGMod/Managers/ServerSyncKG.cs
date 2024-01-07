using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace ServerSyncKG;

[PublicAPI]
public abstract class OwnConfigEntryBase
{
	public object? LocalBaseValue;
	public abstract ConfigEntryBase BaseConfig { get; }

	public bool SynchronizedConfig = true;
}

[PublicAPI]
public class SyncedConfigEntry<T> : OwnConfigEntryBase
{
	public override ConfigEntryBase BaseConfig => SourceConfig;
	public readonly ConfigEntry<T> SourceConfig;

	public SyncedConfigEntry(ConfigEntry<T> sourceConfig)
	{
		SourceConfig = sourceConfig;
	}

	public T Value
	{
		get => SourceConfig.Value;
		set => SourceConfig.Value = value;
	}

	public void AssignLocalValue(T value)
	{
		if (LocalBaseValue == null)
		{
			Value = value;
		}
		else
		{
			LocalBaseValue = value;
		}
	}
}

public abstract class CustomSyncedValueBase
{
	public event Action? ValueChanged;

	public object? LocalBaseValue;

	public readonly string Identifier;
	public readonly Type Type;

	private object? boxedValue;

	public object? BoxedValue
	{
		get => boxedValue;
		set
		{
			boxedValue = value;
			ValueChanged?.Invoke();
		}
	}

	protected bool localIsOwner;
	public readonly int Priority;

	protected CustomSyncedValueBase(ConfigSync configSync, string identifier, Type type, int priority)
	{
		Priority = priority;
		Identifier = identifier;
		Type = type;
		configSync.AddCustomValue(this);
		localIsOwner = configSync.IsSourceOfTruth;
		configSync.SourceOfTruthChanged += truth => localIsOwner = truth;
	}
}

[PublicAPI]
public sealed class CustomSyncedValue<T> : CustomSyncedValueBase
{
	public T Value
	{
		get => (T)BoxedValue!;
		set => BoxedValue = value;
	}

	public CustomSyncedValue(ConfigSync configSync, string identifier, T value = default!, int priority = 0) : base(configSync, identifier, typeof(T), priority)
	{
		Value = value;
	}

	public void AssignLocalValue(T value)
	{
		if (localIsOwner)
		{
			Value = value;
		}
		else
		{
			LocalBaseValue = value;
		}
	}
}

internal class ConfigurationManagerAttributes
{
	[UsedImplicitly] public bool? ReadOnly = false;
}

[PublicAPI]
public class ConfigSync
{
	public static bool ProcessingServerUpdate = false;

	public readonly string Name;
	public string? DisplayName;
	public string? CurrentVersion;
	public string? MinimumRequiredVersion;
	public bool ModRequired = false;

	private bool? forceConfigLocking;

	public bool IsLocked
	{
		get => (forceConfigLocking ?? lockedConfig != null && ((IConvertible)lockedConfig.BaseConfig.BoxedValue).ToInt32(CultureInfo.InvariantCulture) != 0) && !lockExempt;
		set => forceConfigLocking = value;
	}

	public bool IsAdmin => lockExempt || isSourceOfTruth;

	private bool isSourceOfTruth = true;

	public bool IsSourceOfTruth
	{
		get => isSourceOfTruth;
		private set
		{
			if (value != isSourceOfTruth)
			{
				isSourceOfTruth = value;
				SourceOfTruthChanged?.Invoke(value);
			}
		}
	}

	public bool InitialSyncDone { get; private set; } = false;

	public event Action<bool>? SourceOfTruthChanged;

	private static readonly HashSet<ConfigSync> configSyncs = new();

	private readonly HashSet<OwnConfigEntryBase> allConfigs = new();
	private HashSet<CustomSyncedValueBase> allCustomValues = new();

	private static bool isServer;

	private static bool lockExempt = false;

	private OwnConfigEntryBase? lockedConfig = null;
	private event Action? lockedConfigChanged;

	static ConfigSync()
	{
		RuntimeHelpers.RunClassConstructor(typeof(VersionCheck).TypeHandle);
	}

	public ConfigSync(string name)
	{
		Name = name;
		configSyncs.Add(this);
		_ = new VersionCheck(this);
	}

	public SyncedConfigEntry<T> AddConfigEntry<T>(ConfigEntry<T> configEntry)
	{
		if (configData(configEntry) is not SyncedConfigEntry<T> syncedEntry)
		{
			syncedEntry = new SyncedConfigEntry<T>(configEntry);
			AccessTools.DeclaredField(typeof(ConfigDescription), "<Tags>k__BackingField").SetValue(configEntry.Description, new object[] { new ConfigurationManagerAttributes() }.Concat(configEntry.Description.Tags ?? Array.Empty<object>()).Concat(new[] { syncedEntry }).ToArray());
			configEntry.SettingChanged += (_, _) =>
			{
				if (!ProcessingServerUpdate && syncedEntry.SynchronizedConfig)
				{
					Broadcast(ZRoutedRpc.Everybody, configEntry);
				}
			};
			allConfigs.Add(syncedEntry);
		}

		return syncedEntry;
	}

	public SyncedConfigEntry<T> AddLockingConfigEntry<T>(ConfigEntry<T> lockingConfig) where T : IConvertible
	{
		if (lockedConfig != null)
		{
			throw new Exception("Cannot initialize locking ConfigEntry twice");
		}

		lockedConfig = AddConfigEntry(lockingConfig);
		lockingConfig.SettingChanged += (_, _) => lockedConfigChanged?.Invoke();

		return (SyncedConfigEntry<T>)lockedConfig;
	}

	internal void AddCustomValue(CustomSyncedValueBase customValue)
	{
		if (allCustomValues.Select(v => v.Identifier).Concat(new[] { "serverversion" }).Contains(customValue.Identifier))
		{
			throw new Exception("Cannot have multiple settings with the same name or with a reserved name (serverversion)");
		}

		allCustomValues.Add(customValue);
		allCustomValues = new HashSet<CustomSyncedValueBase>(allCustomValues.OrderByDescending(v => v.Priority));
		customValue.ValueChanged += () =>
		{
			if (!ProcessingServerUpdate)
			{
				Broadcast(ZRoutedRpc.Everybody, customValue);
			}
		};
	}

	[HarmonyPatch(typeof(ZRpc), "HandlePackage")]
	private static class SnatchCurrentlyHandlingRPC
	{
		public static ZRpc? currentRpc;

		[HarmonyPrefix]
		private static void Prefix(ZRpc __instance) => currentRpc = __instance;
	}

	[HarmonyPatch(typeof(ZNet), "Awake")]
	internal static class RegisterRPCPatch
	{
		[HarmonyPostfix]
		private static void Postfix(ZNet __instance)
		{
			isServer = __instance.IsServer();
			foreach (ConfigSync configSync in configSyncs)
			{
				ZRoutedRpc.instance.Register<ZPackage>(configSync.Name + " ConfigSync", configSync.RPC_FromOtherClientConfigSync);
				if (isServer)
				{
					configSync.InitialSyncDone = true;
					Debug.Log($"Registered '{configSync.Name} ConfigSync' RPC - waiting for incoming connections");
				}
			}

			IEnumerator WatchAdminListChanges()
			{
				MethodInfo? listContainsId = AccessTools.DeclaredMethod(typeof(ZNet), "ListContainsId");
				SyncedList adminList = (SyncedList)AccessTools.DeclaredField(typeof(ZNet), "m_adminList").GetValue(ZNet.instance);
				List<string> CurrentList = new(adminList.GetList());
				for (;;)
				{
					yield return new WaitForSeconds(30);
					if (!adminList.GetList().SequenceEqual(CurrentList))
					{
						CurrentList = new List<string>(adminList.GetList());

						void SendAdmin(List<ZNetPeer> peers, bool isAdmin)
						{
							ZPackage package = ConfigsToPackage(packageEntries: new[]
							{
								new PackageEntry { section = "Internal", key = "lockexempt", type = typeof(bool), value = isAdmin },
							});

							if (configSyncs.First() is { } configSync)
							{
								ZNet.instance.StartCoroutine(configSync.sendZPackage(peers, package));
							}
						}

						List<ZNetPeer> adminPeer = ZNet.instance.GetPeers().Where(p =>
						{
							string client = p.m_rpc.GetSocket().GetHostName();
							return listContainsId is null ? adminList.Contains(client) : (bool)listContainsId.Invoke(ZNet.instance, new object[] { adminList, client });
						}).ToList();
						List<ZNetPeer> nonAdminPeer = ZNet.instance.GetPeers().Except(adminPeer).ToList();
						SendAdmin(nonAdminPeer, false);
						SendAdmin(adminPeer, true);
					}
				}
				// ReSharper disable once IteratorNeverReturns
			}

			if (isServer)
			{
				__instance.StartCoroutine(WatchAdminListChanges());
			}
		}
	}

	[HarmonyPatch(typeof(ZNet), "OnNewConnection")]
	private static class RegisterClientRPCPatch
	{
		[HarmonyPostfix]
		private static void Postfix(ZNet __instance, ZNetPeer peer)
		{
			if (!__instance.IsServer())
			{
				foreach (ConfigSync configSync in configSyncs)
				{
					peer.m_rpc.Register<ZPackage>(configSync.Name + " ConfigSync", configSync.RPC_FromServerConfigSync);
				}
			}
		}
	}

	private const byte PARTIAL_CONFIGS = 1;
	private const byte FRAGMENTED_CONFIG = 2;
	private const byte COMPRESSED_CONFIG = 4;

	private readonly Dictionary<string, SortedDictionary<int, byte[]>> configValueCache = new();
	private readonly List<KeyValuePair<long, string>> cacheExpirations = new(); // avoid leaking memory

	private void RPC_FromServerConfigSync(ZRpc rpc, ZPackage package)
	{
		lockedConfigChanged += serverLockedSettingChanged;
		IsSourceOfTruth = false;

		if (HandleConfigSyncRPC(0, package, false))
		{
			InitialSyncDone = true;
		}
	}

	private void RPC_FromOtherClientConfigSync(long sender, ZPackage package) => HandleConfigSyncRPC(sender, package, true);

	private bool HandleConfigSyncRPC(long sender, ZPackage package, bool clientUpdate)
	{
		try
		{
			if (isServer && IsLocked && SnatchCurrentlyHandlingRPC.currentRpc?.GetSocket()?.GetHostName() is { } client)
			{
				MethodInfo? listContainsId = AccessTools.DeclaredMethod(typeof(ZNet), "ListContainsId");
				SyncedList adminList = (SyncedList)AccessTools.DeclaredField(typeof(ZNet), "m_adminList").GetValue(ZNet.instance);
				bool exempt = listContainsId is null ? adminList.Contains(client) : (bool)listContainsId.Invoke(ZNet.instance, new object[] { adminList, client });
				if (!exempt)
				{
					return false;
				}
			}

			cacheExpirations.RemoveAll(kv =>
			{
				if (kv.Key < DateTimeOffset.Now.Ticks)
				{
					configValueCache.Remove(kv.Value);
					return true;
				}

				return false;
			});

			byte packageFlags = package.ReadByte();

			if ((packageFlags & FRAGMENTED_CONFIG) != 0)
			{
				long uniqueIdentifier = package.ReadLong();
				string cacheKey = sender.ToString() + uniqueIdentifier;
				if (!configValueCache.TryGetValue(cacheKey, out SortedDictionary<int, byte[]> dataFragments))
				{
					dataFragments = new SortedDictionary<int, byte[]>();
					configValueCache[cacheKey] = dataFragments;
					cacheExpirations.Add(new KeyValuePair<long, string>(DateTimeOffset.Now.AddSeconds(60).Ticks, cacheKey));
				}

				int fragment = package.ReadInt();
				int fragments = package.ReadInt();

				dataFragments.Add(fragment, package.ReadByteArray());

				if (dataFragments.Count < fragments)
				{
					return false;
				}

				configValueCache.Remove(cacheKey);

				package = new ZPackage(dataFragments.Values.SelectMany(a => a).ToArray());
				packageFlags = package.ReadByte();
			}

			ProcessingServerUpdate = true;

			if ((packageFlags & COMPRESSED_CONFIG) != 0)
			{
				byte[] data = package.ReadByteArray();

				MemoryStream input = new(data);
				MemoryStream output = new();
				using (DeflateStream deflateStream = new(input, CompressionMode.Decompress))
				{
					deflateStream.CopyTo(output);
				}

				package = new ZPackage(output.ToArray());
				packageFlags = package.ReadByte();
			}

			if ((packageFlags & PARTIAL_CONFIGS) == 0)
			{
				resetConfigsFromServer();
			}

			ParsedConfigs configs = ReadConfigsFromPackage(package);

			ConfigFile? configFile = null;
			bool originalSaveOnConfigSet = false;
			foreach (KeyValuePair<OwnConfigEntryBase, object?> configKv in configs.configValues)
			{
				if (!isServer && configKv.Key.LocalBaseValue == null)
				{
					configKv.Key.LocalBaseValue = configKv.Key.BaseConfig.BoxedValue;
				}

				if (configFile is null)
				{
					configFile = configKv.Key.BaseConfig.ConfigFile;
					originalSaveOnConfigSet = configFile.SaveOnConfigSet;
					configFile.SaveOnConfigSet = false;
				}
				configKv.Key.BaseConfig.BoxedValue = configKv.Value;
			}
			if (configFile is not null)
			{
				configFile.SaveOnConfigSet = originalSaveOnConfigSet;
			}

			foreach (KeyValuePair<CustomSyncedValueBase, object?> configKv in configs.customValues)
			{
				if (!isServer)
				{
					configKv.Key.LocalBaseValue ??= configKv.Key.BoxedValue;
				}

				configKv.Key.BoxedValue = configKv.Value;
			}

			Debug.Log($"Received {configs.configValues.Count} configs and {configs.customValues.Count} custom values from {(isServer || clientUpdate ? $"client {sender}" : "the server")} for mod {DisplayName ?? Name}");

			if (!isServer)
			{
				serverLockedSettingChanged(); // Re-evaluate for intial locking
			}

			return true;
		}
		finally
		{
			ProcessingServerUpdate = false;
		}
	}

	private class ParsedConfigs
	{
		public readonly Dictionary<OwnConfigEntryBase, object?> configValues = new();
		public readonly Dictionary<CustomSyncedValueBase, object?> customValues = new();
	}

	private ParsedConfigs ReadConfigsFromPackage(ZPackage package)
	{
		ParsedConfigs configs = new();
		Dictionary<string, OwnConfigEntryBase> configMap = allConfigs.Where(c => c.SynchronizedConfig).ToDictionary(c => c.BaseConfig.Definition.Section + "_" + c.BaseConfig.Definition.Key, c => c);

		Dictionary<string, CustomSyncedValueBase> customValueMap = allCustomValues.ToDictionary(c => c.Identifier, c => c);

		int valueCount = package.ReadInt();
		for (int i = 0; i < valueCount; ++i)
		{
			string groupName = package.ReadString();
			string configName = package.ReadString();
			string typeName = package.ReadString();

			Type? type = Type.GetType(typeName);
			if (typeName == "" || type != null)
			{
				object? value;
				try
				{
					value = typeName == "" ? null : ReadValueWithTypeFromZPackage(package, type!);
				}
				catch (InvalidDeserializationTypeException e)
				{
					Debug.LogWarning($"Got unexpected struct internal type {e.received} for field {e.field} struct {typeName} for {configName} in section {groupName} for mod {DisplayName ?? Name}, expecting {e.expected}");
					continue;
				}
				if (groupName == "Internal")
				{
					if (configName == "serverversion")
					{
						if (value?.ToString() != CurrentVersion)
						{
							Debug.LogWarning($"Received server version is not equal: server version = {value?.ToString() ?? "null"}; local version = {CurrentVersion ?? "unknown"}");
						}
					}
					else if (configName == "lockexempt")
					{
						if (value is bool exempt)
						{
							lockExempt = exempt;
						}
					}
					else if (customValueMap.TryGetValue(configName, out CustomSyncedValueBase config))
					{
						if ((typeName == "" && (!config.Type.IsValueType || Nullable.GetUnderlyingType(config.Type) != null)) || GetZPackageTypeString(config.Type) == typeName)
						{
							configs.customValues[config] = value;
						}
						else
						{
							Debug.LogWarning($"Got unexpected type {typeName} for internal value {configName} for mod {DisplayName ?? Name}, expecting {config.Type.AssemblyQualifiedName}");
						}
					}
				}
				else if (configMap.TryGetValue(groupName + "_" + configName, out OwnConfigEntryBase config))
				{
					Type expectedType = configType(config.BaseConfig);
					if ((typeName == "" && (!expectedType.IsValueType || Nullable.GetUnderlyingType(expectedType) != null)) || GetZPackageTypeString(expectedType) == typeName)
					{
						configs.configValues[config] = value;
					}
					else
					{
						Debug.LogWarning($"Got unexpected type {typeName} for {configName} in section {groupName} for mod {DisplayName ?? Name}, expecting {expectedType.AssemblyQualifiedName}");
					}
				}
				else
				{
					Debug.LogWarning($"Received unknown config entry {configName} in section {groupName} for mod {DisplayName ?? Name}. This may happen if client and server versions of the mod do not match.");
				}
			}
			else
			{
				Debug.LogWarning($"Got invalid type {typeName}, abort reading of received configs");
				return new ParsedConfigs();
			}
		}

		return configs;
	}

	[HarmonyPatch(typeof(ZNet), "Shutdown")]
	private class ResetConfigsOnShutdown
	{
		[HarmonyPostfix]
		private static void Postfix()
		{
			ProcessingServerUpdate = true;
			foreach (ConfigSync serverSync in configSyncs)
			{
				serverSync.resetConfigsFromServer();
				serverSync.IsSourceOfTruth = true;
				serverSync.InitialSyncDone = false;
			}
			ProcessingServerUpdate = false;
		}
	}

	private static bool isWritableConfig(OwnConfigEntryBase config)
	{
		if (configSyncs.FirstOrDefault(cs => cs.allConfigs.Contains(config)) is not { } configSync)
		{
			return true;
		}

		return configSync.IsSourceOfTruth || !config.SynchronizedConfig || config.LocalBaseValue == null || (!configSync.IsLocked && (config != configSync.lockedConfig || lockExempt));
	}

	private void serverLockedSettingChanged()
	{
		foreach (OwnConfigEntryBase configEntryBase in allConfigs)
		{
			configAttribute<ConfigurationManagerAttributes>(configEntryBase.BaseConfig).ReadOnly = !isWritableConfig(configEntryBase);
		}
	}

	private void resetConfigsFromServer()
	{
		ConfigFile? configFile = null;
		bool originalSaveOnConfigSet = false;
		foreach (OwnConfigEntryBase config in allConfigs.Where(config => config.LocalBaseValue != null))
		{
			if (configFile is null)
			{
				configFile = config.BaseConfig.ConfigFile;
				originalSaveOnConfigSet = configFile.SaveOnConfigSet;
				configFile.SaveOnConfigSet = false;
			}
			config.BaseConfig.BoxedValue = config.LocalBaseValue;
			config.LocalBaseValue = null;
		}
		if (configFile is not null)
		{
			configFile.SaveOnConfigSet = originalSaveOnConfigSet;
		}
		
		foreach (CustomSyncedValueBase config in allCustomValues.Where(config => config.LocalBaseValue != null))
		{
			config.BoxedValue = config.LocalBaseValue;
			config.LocalBaseValue = null;
		}

		lockedConfigChanged -= serverLockedSettingChanged;
		serverLockedSettingChanged();
	}

	private static long packageCounter = 0;

	private IEnumerator<bool> distributeConfigToPeers(ZNetPeer peer, ZPackage package)
	{
		if (ZRoutedRpc.instance is not { } rpc)
		{
			yield break;
		}

		const int packageSliceSize = 250000;
		const int maximumSendQueueSize = 20000;

		IEnumerable<bool> waitForQueue()
		{
			float timeout = Time.time + 30;
			while (peer.m_socket.GetSendQueueSize() > maximumSendQueueSize)
			{
				if (Time.time > timeout)
				{
					Debug.Log($"Disconnecting {peer.m_uid} after 30 seconds config sending timeout");
					peer.m_rpc.Invoke("Error", ZNet.ConnectionStatus.ErrorConnectFailed);
					ZNet.instance.Disconnect(peer);
					yield break;
				}

				yield return false;
			}
		}

		void SendPackage(ZPackage pkg)
		{
			string method = Name + " ConfigSync";
			if (isServer)
			{
				peer.m_rpc.Invoke(method, pkg);
			}
			else
			{
				rpc.InvokeRoutedRPC(peer.m_server ? 0 : peer.m_uid, method, pkg);
			}
		}

		if (package.GetArray() is { LongLength: > packageSliceSize } data)
		{
			int fragments = (int)(1 + (data.LongLength - 1) / packageSliceSize);
			long packageIdentifier = ++packageCounter;
			for (int fragment = 0; fragment < fragments; ++fragment)
			{
				foreach (bool wait in waitForQueue())
				{
					yield return wait;
				}

				if (!peer.m_socket.IsConnected())
				{
					yield break;
				}

				ZPackage fragmentedPackage = new();
				fragmentedPackage.Write(FRAGMENTED_CONFIG);
				fragmentedPackage.Write(packageIdentifier);
				fragmentedPackage.Write(fragment);
				fragmentedPackage.Write(fragments);
				fragmentedPackage.Write(data.Skip(packageSliceSize * fragment).Take(packageSliceSize).ToArray());
				SendPackage(fragmentedPackage);

				if (fragment != fragments - 1)
				{
					yield return true;
				}
			}
		}
		else
		{
			foreach (bool wait in waitForQueue())
			{
				yield return wait;
			}

			SendPackage(package);
		}
	}

	private IEnumerator sendZPackage(long target, ZPackage package)
	{
		if (!ZNet.instance)
		{
			return Enumerable.Empty<object>().GetEnumerator();
		}

		List<ZNetPeer> peers = (List<ZNetPeer>)AccessTools.DeclaredField(typeof(ZRoutedRpc), "m_peers").GetValue(ZRoutedRpc.instance);
		if (target != ZRoutedRpc.Everybody)
		{
			peers = peers.Where(p => p.m_uid == target).ToList();
		}

		return sendZPackage(peers, package);
	}

	private IEnumerator sendZPackage(List<ZNetPeer> peers, ZPackage package)
	{
		if (!ZNet.instance)
		{
			yield break;
		}

		const int compressMinSize = 10000;

		if (package.GetArray() is { LongLength: > compressMinSize } rawData)
		{
			ZPackage compressedPackage = new();
			compressedPackage.Write(COMPRESSED_CONFIG);
			MemoryStream output = new();
			using (DeflateStream deflateStream = new(output, CompressionLevel.Optimal))
			{
				deflateStream.Write(rawData, 0, rawData.Length);
			}
			compressedPackage.Write(output.ToArray());
			package = compressedPackage;
		}

		List<IEnumerator<bool>> writers = peers.Where(peer => peer.IsReady()).Select(p => distributeConfigToPeers(p, package)).ToList();
		writers.RemoveAll(writer => !writer.MoveNext());
		while (writers.Count > 0)
		{
			yield return null;
			writers.RemoveAll(writer => !writer.MoveNext());
		}
	}

	[HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
	private class SendConfigsAfterLogin
	{
		private class BufferingSocket : ISocket
		{
			public volatile bool finished = false;
			public volatile int versionMatchQueued = -1;
			public readonly List<ZPackage> Package = new();
			public readonly ISocket Original;

			public BufferingSocket(ISocket original)
			{
				Original = original;
			}

			public bool IsConnected() => Original.IsConnected();
			public ZPackage Recv() => Original.Recv();
			public int GetSendQueueSize() => Original.GetSendQueueSize();
			public int GetCurrentSendRate() => Original.GetCurrentSendRate();
			public bool IsHost() => Original.IsHost();
			public void Dispose() => Original.Dispose();
			public bool GotNewData() => Original.GotNewData();
			public void Close() => Original.Close();
			public string GetEndPointString() => Original.GetEndPointString();
			public void GetAndResetStats(out int totalSent, out int totalRecv) => Original.GetAndResetStats(out totalSent, out totalRecv);
			public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec) => Original.GetConnectionQuality(out localQuality, out remoteQuality, out ping, out outByteSec, out inByteSec);
			public ISocket Accept() => Original.Accept();
			public int GetHostPort() => Original.GetHostPort();
			public bool Flush() => Original.Flush();
			public string GetHostName() => Original.GetHostName();

			public void VersionMatch()
			{
				if (finished)
				{
					Original.VersionMatch();
				}
				else
				{
					versionMatchQueued = Package.Count;
				}
			}

			public void Send(ZPackage pkg)
			{
				int oldPos = pkg.GetPos();
				pkg.SetPos(0);
				int methodHash = pkg.ReadInt();
				if ((methodHash == "PeerInfo".GetStableHashCode() || methodHash == "RoutedRPC".GetStableHashCode() || methodHash == "ZDOData".GetStableHashCode()) && !finished)
				{
					ZPackage newPkg = new(pkg.GetArray());
					newPkg.SetPos(oldPos);
					Package.Add(newPkg); // the original ZPackage gets reused, create a new one
				}
				else
				{
					pkg.SetPos(oldPos);
					Original.Send(pkg);
				}
			}
		}

		[HarmonyPriority(Priority.First)]
		[HarmonyPrefix]
		private static void Prefix(ref Dictionary<Assembly, BufferingSocket>? __state, ZNet __instance, ZRpc rpc)
		{
			if (__instance.IsServer())
			{
				BufferingSocket bufferingSocket = new(rpc.GetSocket());
				AccessTools.DeclaredField(typeof(ZRpc), "m_socket").SetValue(rpc, bufferingSocket);
				// Don't replace on steam sockets, RPC_PeerInfo does peer.m_socket as ZSteamSocket - which will cause a nullref when replaced
				if (AccessTools.DeclaredMethod(typeof(ZNet), "GetPeer", new[] { typeof(ZRpc) }).Invoke(__instance, new object[] { rpc }) is ZNetPeer peer && ZNet.m_onlineBackend != OnlineBackendType.Steamworks)
				{
					AccessTools.DeclaredField(typeof(ZNetPeer), "m_socket").SetValue(peer, bufferingSocket);
				}

				__state ??= new Dictionary<Assembly, BufferingSocket>();
				__state[Assembly.GetExecutingAssembly()] = bufferingSocket;
			}
		}

		[HarmonyPostfix]
		private static void Postfix(Dictionary<Assembly, BufferingSocket> __state, ZNet __instance, ZRpc rpc)
		{
			if (!__instance.IsServer())
			{
				return;
			}

			void SendBufferedData()
			{
				if (rpc.GetSocket() is BufferingSocket bufferingSocket)
				{
					AccessTools.DeclaredField(typeof(ZRpc), "m_socket").SetValue(rpc, bufferingSocket.Original);
					if (AccessTools.DeclaredMethod(typeof(ZNet), "GetPeer", new[] { typeof(ZRpc) }).Invoke(__instance, new object[] { rpc }) is ZNetPeer peer)
					{
						AccessTools.DeclaredField(typeof(ZNetPeer), "m_socket").SetValue(peer, bufferingSocket.Original);
					}
				}

				bufferingSocket = __state[Assembly.GetExecutingAssembly()];
				bufferingSocket.finished = true;

				for (int i = 0; i < bufferingSocket.Package.Count; ++i)
				{
					if (i == bufferingSocket.versionMatchQueued)
					{
						bufferingSocket.Original.VersionMatch();
					}
					bufferingSocket.Original.Send(bufferingSocket.Package[i]);
				}
				if (bufferingSocket.Package.Count == bufferingSocket.versionMatchQueued)
				{
					bufferingSocket.Original.VersionMatch();
				}
			}

			if (AccessTools.DeclaredMethod(typeof(ZNet), "GetPeer", new[] { typeof(ZRpc) }).Invoke(__instance, new object[] { rpc }) is not ZNetPeer peer)
			{
				SendBufferedData();
				return;
			}

			IEnumerator sendAsync()
			{
				foreach (ConfigSync configSync in configSyncs)
				{
					List<PackageEntry> entries = new();
					if (configSync.CurrentVersion != null)
					{
						entries.Add(new PackageEntry { section = "Internal", key = "serverversion", type = typeof(string), value = configSync.CurrentVersion });
					}

					MethodInfo? listContainsId = AccessTools.DeclaredMethod(typeof(ZNet), "ListContainsId");
					SyncedList adminList = (SyncedList)AccessTools.DeclaredField(typeof(ZNet), "m_adminList").GetValue(ZNet.instance);
					entries.Add(new PackageEntry { section = "Internal", key = "lockexempt", type = typeof(bool), value = listContainsId is null ? adminList.Contains(rpc.GetSocket().GetHostName()) : listContainsId.Invoke(ZNet.instance, new object[] { adminList, rpc.GetSocket().GetHostName() }) });

					ZPackage package = ConfigsToPackage(configSync.allConfigs.Select(c => c.BaseConfig), configSync.allCustomValues, entries, false);

					yield return __instance.StartCoroutine(configSync.sendZPackage(new List<ZNetPeer> { peer }, package));

				}

				SendBufferedData();
			}

			__instance.StartCoroutine(sendAsync());
		}
	}

	private class PackageEntry
	{
		public string section = null!;
		public string key = null!;
		public Type type = null!;
		public object? value;
	}

	private void Broadcast(long target, params ConfigEntryBase[] configs)
	{
		if (!IsLocked || isServer)
		{
			ZPackage package = ConfigsToPackage(configs);
			ZNet.instance?.StartCoroutine(sendZPackage(target, package));
		}
	}

	private void Broadcast(long target, params CustomSyncedValueBase[] customValues)
	{
		if (!IsLocked || isServer)
		{
			ZPackage package = ConfigsToPackage(customValues: customValues);
			ZNet.instance?.StartCoroutine(sendZPackage(target, package));
		}
	}

	private static OwnConfigEntryBase? configData(ConfigEntryBase config)
	{
		return config.Description.Tags?.OfType<OwnConfigEntryBase>().SingleOrDefault();
	}

	public static SyncedConfigEntry<T>? ConfigData<T>(ConfigEntry<T> config)
	{
		return config.Description.Tags?.OfType<SyncedConfigEntry<T>>().SingleOrDefault();
	}

	private static T configAttribute<T>(ConfigEntryBase config)
	{
		return config.Description.Tags.OfType<T>().First();
	}

	private static Type configType(ConfigEntryBase config) => configType(config.SettingType);

	private static Type configType(Type type) => type.IsEnum ? Enum.GetUnderlyingType(type) : type;

	[HarmonyPatch(typeof(ConfigEntryBase), nameof(ConfigEntryBase.GetSerializedValue))]
	private static class PreventSavingServerInfo
	{
		[HarmonyPrefix]
		private static bool Prefix(ConfigEntryBase __instance, ref string __result)
		{
			if (configData(__instance) is not { } data || isWritableConfig(data))
			{
				return true;
			}

			__result = TomlTypeConverter.ConvertToString(data.LocalBaseValue, __instance.SettingType);
			return false;
		}
	}

	[HarmonyPatch(typeof(ConfigEntryBase), nameof(ConfigEntryBase.SetSerializedValue))]
	private static class PreventConfigRereadChangingValues
	{
		[HarmonyPrefix]
		private static bool Prefix(ConfigEntryBase __instance, string value)
		{
			if (configData(__instance) is not { } data || data.LocalBaseValue == null)
			{
				return true;
			}

			try
			{
				data.LocalBaseValue = TomlTypeConverter.ConvertToValue(value, __instance.SettingType);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"Config value of setting \"{__instance.Definition}\" could not be parsed and will be ignored. Reason: {e.Message}; Value: {value}");
			}
			return false;
		}
	}

	private static ZPackage ConfigsToPackage(IEnumerable<ConfigEntryBase>? configs = null, IEnumerable<CustomSyncedValueBase>? customValues = null, IEnumerable<PackageEntry>? packageEntries = null, bool partial = true)
	{
		List<ConfigEntryBase> configList = configs?.Where(config => configData(config)!.SynchronizedConfig).ToList() ?? new List<ConfigEntryBase>();
		List<CustomSyncedValueBase> customValueList = customValues?.ToList() ?? new List<CustomSyncedValueBase>();
		ZPackage package = new();
		package.Write(partial ? PARTIAL_CONFIGS : (byte)0);
		package.Write(configList.Count + customValueList.Count + (packageEntries?.Count() ?? 0));
		foreach (PackageEntry packageEntry in packageEntries ?? Array.Empty<PackageEntry>())
		{
			AddEntryToPackage(package, packageEntry);
		}
		foreach (CustomSyncedValueBase customValue in customValueList)
		{
			AddEntryToPackage(package, new PackageEntry { section = "Internal", key = customValue.Identifier, type = customValue.Type, value = customValue.BoxedValue });
		}
		foreach (ConfigEntryBase config in configList)
		{
			AddEntryToPackage(package, new PackageEntry { section = config.Definition.Section, key = config.Definition.Key, type = configType(config), value = config.BoxedValue });
		}

		return package;
	}

	private static void AddEntryToPackage(ZPackage package, PackageEntry entry)
	{
		package.Write(entry.section);
		package.Write(entry.key);
		package.Write(entry.value == null ? "" : GetZPackageTypeString(entry.type));
		AddValueToZPackage(package, entry.value);
	}

	private static string GetZPackageTypeString(Type type) => type.AssemblyQualifiedName!;

	private static void AddValueToZPackage(ZPackage package, object? value)
	{
		Type? type = value?.GetType();
		if (value is Enum)
		{
			value = ((IConvertible)value).ToType(Enum.GetUnderlyingType(value.GetType()), CultureInfo.InvariantCulture);
		}
		else if (value is ICollection collection)
		{
			package.Write(collection.Count);
			foreach (object item in collection)
			{
				AddValueToZPackage(package, item);
			}
			return;
		}
		else if (type is { IsValueType: true, IsPrimitive: false })
		{
			FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			package.Write(fields.Length);
			foreach (FieldInfo field in fields)
			{
				package.Write(GetZPackageTypeString(field.FieldType));
				AddValueToZPackage(package, field.GetValue(value));
			}
			return;
		}

		ZRpc.Serialize(new[] { value }, ref package);
	}

	private static object ReadValueWithTypeFromZPackage(ZPackage package, Type type)
	{
		if (type is { IsValueType: true, IsPrimitive: false, IsEnum: false })
		{
			FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			int fieldCount = package.ReadInt();
			if (fieldCount != fields.Length)
			{
				throw new InvalidDeserializationTypeException { received = $"(field count: {fieldCount})", expected = $"(field count: {fields.Length})" };
			}

			object value = FormatterServices.GetUninitializedObject(type);
			foreach (FieldInfo field in fields)
			{
				string typeName = package.ReadString();
				if (typeName != GetZPackageTypeString(field.FieldType))
				{
					throw new InvalidDeserializationTypeException { received = typeName, expected = GetZPackageTypeString(field.FieldType), field = field.Name };
				}
				field.SetValue(value, ReadValueWithTypeFromZPackage(package, field.FieldType));
			}
			return value;
		}
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
		{
			int entriesCount = package.ReadInt();
			IDictionary dict = (IDictionary)Activator.CreateInstance(type);
			Type kvType = typeof(KeyValuePair<,>).MakeGenericType(type.GenericTypeArguments);
			FieldInfo keyField = kvType.GetField("key", BindingFlags.NonPublic | BindingFlags.Instance)!;
			FieldInfo valueField = kvType.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance)!;
			for (int i = 0; i < entriesCount; ++i)
			{
				object kv = ReadValueWithTypeFromZPackage(package, kvType);
				dict.Add(keyField.GetValue(kv), valueField.GetValue(kv));
			}
			return dict;
		}
		if (type != typeof(List<string>) && type.IsGenericType && typeof(ICollection<>).MakeGenericType(type.GenericTypeArguments[0]) is { } collectionType && collectionType.IsAssignableFrom(type))
		{
			int entriesCount = package.ReadInt();
			object list = Activator.CreateInstance(type);
			MethodInfo adder = collectionType.GetMethod("Add")!;
			for (int i = 0; i < entriesCount; ++i)
			{
				adder.Invoke(list, new[] { ReadValueWithTypeFromZPackage(package, type.GenericTypeArguments[0]) });
			}
			return list;
		}

		ParameterInfo param = (ParameterInfo)FormatterServices.GetUninitializedObject(typeof(ParameterInfo));
		AccessTools.DeclaredField(typeof(ParameterInfo), "ClassImpl").SetValue(param, type);
		List<object> data = new();
		ZRpc.Deserialize(new[] { null, param }, package, ref data);
		return data.First();
	}

	private class InvalidDeserializationTypeException : Exception
	{
		public string expected = null!;
		public string received = null!;
		public string field = "";
	}
}

[PublicAPI]
[HarmonyPatch]
public class VersionCheck
{
	private static readonly HashSet<VersionCheck> versionChecks = new();
	private static readonly Dictionary<string, string> notProcessedNames = new();

	public string Name;

	private string? displayName;

	public string DisplayName
	{
		get => displayName ?? Name;
		set => displayName = value;
	}

	private string? currentVersion;

	public string CurrentVersion
	{
		get => currentVersion ?? "0.0.0";
		set => currentVersion = value;
	}

	private string? minimumRequiredVersion;

	public string MinimumRequiredVersion
	{
		get => minimumRequiredVersion ?? (ModRequired ? CurrentVersion : "0.0.0");
		set => minimumRequiredVersion = value;
	}

	public bool ModRequired = true;

	private string? ReceivedCurrentVersion;

	private string? ReceivedMinimumRequiredVersion;

	// Tracks which clients have passed the version check (only for servers).
	private readonly List<ZRpc> ValidatedClients = new();

	// Optional backing field to use ConfigSync values (will override other fields).
	private ConfigSync? ConfigSync;

	private static void PatchServerSync()
	{
		if (PatchProcessor.GetPatchInfo(AccessTools.DeclaredMethod(typeof(ZNet), "Awake"))?.Postfixes.Count(p => p.PatchMethod.DeclaringType == typeof(ConfigSync.RegisterRPCPatch)) > 0)
		{
			return;
		}

		Harmony harmony = new("org.bepinex.helpers.ServerSync");
		foreach (Type type in typeof(ConfigSync).GetNestedTypes(BindingFlags.NonPublic).Concat(new[] { typeof(VersionCheck) }).Where(t => t.IsClass))
		{
			harmony.PatchAll(type);
		}
	}

	static VersionCheck()
	{
		typeof(ThreadingHelper).GetMethod("StartSyncInvoke")!.Invoke(ThreadingHelper.Instance, new object[] { (Action)PatchServerSync });
	}

	public VersionCheck(string name)
	{
		Name = name;
		ModRequired = true;
		versionChecks.Add(this);
	}

	public VersionCheck(ConfigSync configSync)
	{
		ConfigSync = configSync;
		Name = ConfigSync.Name;
		versionChecks.Add(this);
	}

	public void Initialize()
	{
		ReceivedCurrentVersion = null;
		ReceivedMinimumRequiredVersion = null;
		if (ConfigSync == null)
		{
			return;
		}
		Name = ConfigSync.Name;
		DisplayName = ConfigSync.DisplayName!;
		CurrentVersion = ConfigSync.CurrentVersion!;
		MinimumRequiredVersion = ConfigSync.MinimumRequiredVersion!;
		ModRequired = ConfigSync.ModRequired;
	}

	[SuppressMessage("ReSharper", "RedundantNameQualifier")]
	private bool IsVersionOk()
	{
		if (ReceivedMinimumRequiredVersion == null || ReceivedCurrentVersion == null)
		{
			return !ModRequired;
		}
		bool myVersionOk = new System.Version(CurrentVersion) >= new System.Version(ReceivedMinimumRequiredVersion);
		bool otherVersionOk = new System.Version(ReceivedCurrentVersion) >= new System.Version(MinimumRequiredVersion);
		return myVersionOk && otherVersionOk;
	}
	
	[SuppressMessage("ReSharper", "RedundantNameQualifier")]
	private string ErrorClient()
	{
		if (ReceivedMinimumRequiredVersion == null)
		{
			return $"{DisplayName} is not installed on the server.";
		}
		bool myVersionOk = new System.Version(CurrentVersion) >= new System.Version(ReceivedMinimumRequiredVersion);
		return myVersionOk ? $"{DisplayName} may not be higher than version {ReceivedCurrentVersion}. You have version {CurrentVersion}." : $"{DisplayName} needs to be at least version {ReceivedMinimumRequiredVersion}. You have version {CurrentVersion}.";
	}

	private string ErrorServer(ZRpc rpc)
	{
		return $"Disconnect: The client ({rpc.GetSocket().GetHostName()}) doesn't have the correct {DisplayName} version {MinimumRequiredVersion}";
	}

	private string Error(ZRpc? rpc = null)
	{
		return rpc == null ? ErrorClient() : ErrorServer(rpc);
	}

	private static VersionCheck[] GetFailedClient()
	{
		return versionChecks.Where(check => !check.IsVersionOk()).ToArray();
	}

	private static VersionCheck[] GetFailedServer(ZRpc rpc)
	{
		return versionChecks.Where(check => check.ModRequired && !check.ValidatedClients.Contains(rpc)).ToArray();
	}

	private static void Logout()
	{
		Game.instance.Logout();
		AccessTools.DeclaredField(typeof(ZNet), "m_connectionStatus").SetValue(null, ZNet.ConnectionStatus.ErrorVersion);
	}

	private static void DisconnectClient(ZRpc rpc)
	{
		rpc.Invoke("Error", (int)ZNet.ConnectionStatus.ErrorVersion);
	}

	private static void CheckVersion(ZRpc rpc, ZPackage pkg) => CheckVersion(rpc, pkg, null);

	private static void CheckVersion(ZRpc rpc, ZPackage pkg, Action<ZRpc, ZPackage>? original)
	{
		string guid = pkg.ReadString();
		string minimumRequiredVersion = pkg.ReadString();
		string currentVersion = pkg.ReadString();

		bool matched = false;

		foreach (VersionCheck check in versionChecks)
		{
			if (guid != check.Name)
			{
				continue;
			}

			Debug.Log($"Received {check.DisplayName} version {currentVersion} and minimum version {minimumRequiredVersion} from the {(ZNet.instance.IsServer() ? "client" : "server")}.");

			check.ReceivedMinimumRequiredVersion = minimumRequiredVersion;
			check.ReceivedCurrentVersion = currentVersion;
			if (ZNet.instance.IsServer() && check.IsVersionOk())
			{
				check.ValidatedClients.Add(rpc);
			}

			matched = true;
		}

		if (!matched)
		{
			pkg.SetPos(0);
			if (original is not null)
			{
				original(rpc, pkg);
				if (pkg.GetPos() == 0)
				{
					notProcessedNames.Add(guid, currentVersion);
				}
			}
		}
	}

	[HarmonyPatch(typeof(ZNet), "RPC_PeerInfo"), HarmonyPrefix]
	private static bool RPC_PeerInfo(ZRpc rpc, ZNet __instance)
	{
		VersionCheck[] failedChecks = __instance.IsServer() ? GetFailedServer(rpc) : GetFailedClient();
		if (failedChecks.Length == 0)
		{
			return true;
		}

		foreach (VersionCheck check in failedChecks)
		{
			Debug.LogWarning(check.Error(rpc));
		}

		if (__instance.IsServer())
		{
			DisconnectClient(rpc);
		}
		else
		{
			Logout();
		}
		return false;
	}

	[HarmonyPatch(typeof(ZNet), "OnNewConnection"), HarmonyPrefix]
	private static void RegisterAndCheckVersion(ZNetPeer peer, ZNet __instance)
	{
		notProcessedNames.Clear();

		IDictionary rpcFunctions = (IDictionary)typeof(ZRpc).GetField("m_functions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(peer.m_rpc);
		if (rpcFunctions.Contains("ServerSync VersionCheck".GetStableHashCode()))
		{
			object function = rpcFunctions["ServerSync VersionCheck".GetStableHashCode()];
			Action<ZRpc, ZPackage> action = (Action<ZRpc, ZPackage>)function.GetType().GetField("m_action", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(function);
			peer.m_rpc.Register<ZPackage>("ServerSync VersionCheck", (rpc, pkg) => CheckVersion(rpc, pkg, action));
		}
		else
		{
			peer.m_rpc.Register<ZPackage>("ServerSync VersionCheck", CheckVersion);
		}

		foreach (VersionCheck check in versionChecks)
		{
			check.Initialize();
			// If the mod is not required, then it's enough for only one side to do the check.
			if (!check.ModRequired && !__instance.IsServer())
			{
				continue;
			}

			Debug.Log($"Sending {check.DisplayName} version {check.CurrentVersion} and minimum version {check.MinimumRequiredVersion} to the {(__instance.IsServer() ? "client" : "server")}.");

			ZPackage zpackage = new();
			zpackage.Write(check.Name);
			zpackage.Write(check.MinimumRequiredVersion);
			zpackage.Write(check.CurrentVersion);
			peer.m_rpc.Invoke("ServerSync VersionCheck", zpackage);
		}
	}

	[HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect)), HarmonyPrefix]
	private static void RemoveDisconnected(ZNetPeer peer, ZNet __instance)
	{
		if (!__instance.IsServer())
		{
			return;
		}
		foreach (VersionCheck check in versionChecks)
		{
			check.ValidatedClients.Remove(peer.m_rpc);
		}
	}

	[HarmonyPatch(typeof(FejdStartup), "ShowConnectError"), HarmonyPostfix]
	private static void ShowConnectionError(FejdStartup __instance)
	{
		if (!__instance.m_connectionFailedPanel.activeSelf || ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.ErrorVersion)
		{
			return;
		}
		bool failedCheck = false;
		VersionCheck[] failedChecks = GetFailedClient();
		if (failedChecks.Length > 0)
		{
			string error = string.Join("\n", failedChecks.Select(check => check.Error()));
			__instance.m_connectionFailedError.text += "\n" + error;
			failedCheck = true;
		}

		foreach (KeyValuePair<string, string> kv in notProcessedNames.OrderBy(kv => kv.Key))
		{
			if (!__instance.m_connectionFailedError.text.Contains(kv.Key))
			{
				__instance.m_connectionFailedError.text += $"\nServer expects you to have {kv.Key} (Version: {kv.Value}) installed.";
				failedCheck = true;
			}
		}

		if (failedCheck)
		{
			RectTransform panel = __instance.m_connectionFailedPanel.transform.Find("Image").GetComponent<RectTransform>();
			panel.sizeDelta = panel.sizeDelta with { x = 675 };
			__instance.m_connectionFailedError.ForceMeshUpdate();
			float newHeight = __instance.m_connectionFailedError.renderedHeight + 105;
			RectTransform button = panel.transform.Find("ButtonOk").GetComponent<RectTransform>();
			button.anchoredPosition = new Vector2(button.anchoredPosition.x, button.anchoredPosition.y - (newHeight - panel.sizeDelta.y) / 2);
			panel.sizeDelta = panel.sizeDelta with { y = newHeight };
		}
	}
}