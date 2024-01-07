using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace QuickStackStore
{
    internal class ContainerFinder
    {
        public static List<Container> AllContainers = new List<Container>();

        public static List<Container> FindContainersInRange(Vector3 point, float range)
        {
            List<Container> list = new List<Container>();

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            foreach (Container container in AllContainers)
            {
                if (!container || !container.transform || !container.m_nview)
                {
                    continue;
                }

                if (!container.m_nview.HasOwner())
                {
                    continue;
                }

                if (Vector3.Distance(point, container.transform.position) < range)
                {
                    list.Add(container);
                }
            }

            sw.Stop();
            Helper.Log($"Found {list.Count} container/s out of {AllContainers.Count} in range in {sw.Elapsed}", QSSConfig.DebugSeverity.AlsoSpeedTests);

            return list;
        }
    }

    [HarmonyPatch(typeof(Container))]
    internal static class PatchContainer
    {
        [HarmonyPatch(nameof(Container.Awake))]
        [HarmonyPostfix]
        internal static void Awake(Container __instance)
        {
            ContainerFinder.AllContainers.Add(__instance);
        }

        [HarmonyPatch(nameof(Container.OnDestroyed))]
        [HarmonyPostfix]
        internal static void OnDestroyed(Container __instance)
        {
            ContainerFinder.AllContainers.Remove(__instance);
        }
    }
}