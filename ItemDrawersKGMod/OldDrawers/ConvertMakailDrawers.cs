using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace ItemDrawersKGMod;

public static class ConvertMakailDrawers
{
    [HarmonyPatch(typeof(ZDOMan),nameof(ZDOMan.Load))]
    private static class ZDOMan_Load_Patch
    {
        private static readonly int ToSearch = "piece_drawer".GetStableHashCode();
        
        private static void TryConvert(List<ZDO> list)
        {
            List<ZDO> oldDrawers = list.FindAll(zdo => zdo.m_prefab == ToSearch);
            if (oldDrawers.Count == 0) return;
            foreach (ZDO zdo in oldDrawers)
            {
                zdo.m_prefab = "kg_ItemDrawer_Wood".GetStableHashCode();
                zdo.m_position += new Vector3(0.25f, 0.33f, 0.25f);
                zdo.m_rotation += new Vector3(0f, 180f, 0f);
                string items = ZDOExtraData.GetString(zdo.m_uid, ZDOVars.s_items);
                ZDOExtraData.Release(zdo, zdo.m_uid);
                if (string.IsNullOrEmpty(items)) continue;
                try
                {
                    ZPackage inventory = new(items);
                    inventory.ReadInt();
                    string item = inventory.ReadString();
                    if (string.IsNullOrEmpty(item)) continue;
                    int amount = inventory.ReadInt();
                    zdo.Set("Prefab", item);
                    zdo.Set("Amount", amount);
                }
                catch{}
            }
        }
        
        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> code)
        {
            MethodInfo insertAfter = AccessTools.Method(typeof(Game), nameof(Game.ConnectPortals));
            CodeMatcher matcher = new(code);
            matcher.MatchForward(false, new CodeMatch(OpCodes.Callvirt, insertAfter));
            if (matcher.IsInvalid) return matcher.Instructions();
            matcher.Advance(1);
            matcher.InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_2));
            matcher.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ZDOMan_Load_Patch), nameof(TryConvert))));
            return matcher.Instructions();
        }
    }
}