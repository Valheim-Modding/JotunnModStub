using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace kingskills
{
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
    */

    [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
    class BlockPatch : Humanoid
    {
        //How much flat block armor do we get per level up to max
        public const float FlatBlockPowerMax = 50f;
        public const float FlatBlockPowerMin = 0f;
        //What percent block armor do we get per level min to max
        public const float PerBlockPowerMax = 1f;
        public const float PerBlockPowerMin = -.25f;
        //How much is the stamina cost for blocking reduced from min to max
        public const float BlockStaminaReduxMax = .5f;
        public const float BlockStaminaReduxMin = -.1f;
        //How much is the player's stagger limit increased from min to max
        public const float AbsoluteStaggerLimitIncreaseMax = .3f;
        public const float AbsoluteStaggerLimitIncreaseMin = 0f;
        //Used for perks, shouldn't be a float eventually
        public const float AdditionalParryBonus = 1f;
        //How much experience do we get per damage blocked?
        public const float BlockExpMod = .22f;
        //What is the bonus experience for parrying?
        public const float ParryExpMod = 2f;

        //How much bonus xp do we get for unarmed when we unarmed block?
        public const float UnarmedBXPBlock = 20f;


        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool sawBlockPower = false;
            bool patchedBlockPower = false;
            CodeInstruction storeBlockPower = new CodeInstruction(OpCodes.Nop);
            CodeInstruction loadParryFlag = new CodeInstruction(OpCodes.Nop);

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
                    else if (sawBlockPower && instruction.IsLdloc()) // load parry flag
                    {
                        loadParryFlag = instruction.Clone();
                        yield return instruction.Clone(); // parry flag
                        yield return new CodeInstruction(OpCodes.Ldarg_0); // Humanoid this (blocker)
                        yield return new CodeInstruction(OpCodes.Ldarg_1); // HitData hit
                        yield return new CodeInstruction(OpCodes.Ldarg_2); // Character attacker
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(BlockPatch), "FixBlockPower"));
                        yield return storeBlockPower; // store result (block power)
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
                else if (instruction.Calls(AccessTools.DeclaredMethod(typeof(Character), nameof(Character.UseStamina))))
                {
                    instruction.operand = AccessTools.DeclaredMethod(typeof(BlockPatch), nameof(BlockPatch.UseBlockStamina));
                    yield return instruction;
                }
                else if (instruction.Calls(AccessTools.DeclaredMethod(typeof(HitData), nameof(HitData.BlockDamage))))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return loadParryFlag;
                    loadParryFlag = null;
                    instruction.operand = AccessTools.DeclaredMethod(typeof(BlockPatch), nameof(BlockPatch.BlockDamageExpPatch));
                    yield return instruction;
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

        private static void UseBlockStamina(Humanoid __instance, float stamina)
        {
            float skillFactor = __instance.GetSkillFactor(Skills.SkillType.Blocking);
            //Jotunn.Logger.LogMessage($"Stamina before redux is {stamina}");
            stamina *= 1f - Mathf.Lerp(BlockStaminaReduxMin, BlockStaminaReduxMax, skillFactor);
            //Jotunn.Logger.LogMessage($"Stamina after redux is {stamina}");
            __instance.UseStamina(stamina);
        }


        private static void BlockDamageExpPatch(HitData hit, float damage, Humanoid __instance, bool isParry)
        {
            hit.BlockDamage(damage);
            float expValue = damage * BlockExpMod;
            if (isParry)
            {
                expValue *= ParryExpMod;
                //Jotunn.Logger.LogMessage($"Parried! Exp Value doubled!");
            }
            __instance.RaiseSkill(Skills.SkillType.Blocking, expValue);
            if (__instance.GetCurrentBlocker() == __instance.m_unarmedWeapon.m_itemData)
            {
                //Bonus exp for unarmed block!
                __instance.RaiseSkill(Skills.SkillType.Unarmed, UnarmedBXPBlock);
            }
            //Jotunn.Logger.LogMessage($"Increased blocking skill by {expValue} due to damage");
        }

        private static float FixBlockPower(
            ItemDrop.ItemData currentBlocker,
            float skillFactor,
            bool isParry,
            Humanoid instance,
            HitData hit,
            Character attacker
            )
        {
            //Jotunn.Logger.LogMessage($"block power of {currentBlocker.GetBlockPower(skillFactor)} is now mine, also am I parrying? {isParry}");
            float blockPower = 0f;
            float itemBlockPower = currentBlocker.GetBaseBlockPower();
            float baseBlockPower = itemBlockPower + FlatBlockPowerMin + (skillFactor * (FlatBlockPowerMax - FlatBlockPowerMin));
            blockPower = baseBlockPower + (baseBlockPower * PerBlockPowerMin) + baseBlockPower * (PerBlockPowerMax - PerBlockPowerMin);

            if (isParry)
            {
                blockPower *= AdditionalParryBonus;
            }
            return blockPower;
        }
    }
}
