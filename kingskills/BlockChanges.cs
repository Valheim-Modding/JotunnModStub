using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace kingskills
{
    class BlockChanges
    {
    }
    /*
    [HarmonyPatch(typeof(HitData), nameof(HitData.BlockDamage))]
    class BlockExp
    {
        public static void Postfix(HitData __instance, float damage)
        {
            Player.m_localPlayer.RaiseSkill(Skills.SkillType.Blocking, damage);
            Jotunn.Logger.LogMessage("Increased block skill by " + damage + " thanks to blocked damage");
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBlockPower))]
    class BlockPatch
    {
        public static bool Prefix(ItemDrop.ItemData __instance, float skillFactor)=
        {
            bool dontSkip = true;
            float baseBlockPower = __instance.GetBaseBlockPower(__instance.m_quality);
            Character player = Player.m_localPlayer;
            if (player)
            {
                skillFactor = player.GetSkillFactor(Skills.SkillType.Blocking) * 100;
                Jotunn.Logger.LogMessage("Your block level is " + skillFactor);
            }
            else
            {
                skillFactor = 0;
                Jotunn.Logger.LogMessage("No player found in this block event");
            }

            return dontSkip;
        }
    }*/

    [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
    class BlockPatch : Humanoid
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool sawBlockPower = false;
            bool patchedBlockPower = false;
            CodeInstruction storeBlockPower = new CodeInstruction(OpCodes.Nop);
            foreach (var instruction in instructions)
            {
                if (!patchedBlockPower)
                {
                    if (instruction.Calls(AccessTools.Method(typeof(ItemDrop.ItemData), "GetBlockPower", new Type[] { typeof(float) })))
                    {
                        sawBlockPower = true;
                    }
                    else if (sawBlockPower && instruction.IsStloc())
                    {
                        storeBlockPower = instruction.Clone();
                    }
                    else if (sawBlockPower && instruction.IsLdloc())
                    {
                        yield return instruction.Clone(); // parry flag
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(BlockPatch), "FixBlockPower"));
                        yield return storeBlockPower;
                        yield return instruction;
                        storeBlockPower = null;
                        sawBlockPower = false;
                        patchedBlockPower = true;
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
                else if (instruction.Calls(AccessTools.DeclaredMethod(typeof(Character), "RaiseSkill")))
                {
                    instruction.opcode = OpCodes.Pop;
                    instruction.operand = null;
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Pop);
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        private static float FixBlockPower(ItemDrop.ItemData currentBlocker, float skillFactor, bool isParry, Humanoid instance)
        {
            //Jotunn.Logger.LogMessage($"block power of {currentBlocker.GetBlockPower(skillFactor)} is now mine, also am I parrying? {isParry}");

            return 0f;
        }
    }
}
