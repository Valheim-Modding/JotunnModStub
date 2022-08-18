using BepInEx;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using kingskills.Commands;
using UnityEngine;

namespace kingskills
{

    [HarmonyPatch(typeof(Player))]
    class MovePatch
    {
        const float BaseSwimSpeed = 2f;
        const float BaseSwimAccel = .05f;
        const float BaseSwimTurn = 100f;
        const float SwimSpeedMax = 3f;
        const float SwimSpeedMin = 0f;
        const float SwimAccelMax = 3f;
        const float SwimAccelMin = 0f;
        const float SwimTurnMax = 5f;
        const float SwimTurnMin = 0f;
        const float SwimStaminaPSMax = .5f;
        const float SwimStaminaPSMin = 5f;

        const float AbsoluteWeightMaxWeight = 1800;
        const float AbsoluteWeightMinWeight = 100;
        const float AbsoluteWeightMod = 20f;

        const float RelativeWeightLight = .33f;
            const float RelativeWeightLightMod = -.25f;
        const float RelativeWeightMed = .5f;
            const float RelativeWeightMedMod = 0f;
        const float RelativeWeightHighMed = .66f;
            const float RelativeWeightHighMedMod = .25f;
        const float RelativeWeightHeavy = .8f;
            const float RelativeWeightHeavyMod = .5f;
        const float RelativeWeightFull = 1f;
            const float RelativeWeightFullMod = .8f;

        const float RelativeWeightOverMod = 2f;

        const float RelativeWeightMod = 1f;

        const float RunSpeedMod = 1f;

        [HarmonyPatch(nameof(Player.RaiseSkill))]
        [HarmonyPrefix]
        static bool RaiseSkillPatch(Player __instance, Skills.SkillType skill, float value = 1f)
        {
            bool dontSkip = true;

            if (skill == Skills.SkillType.Run)
            {
                float expValue = 1f;
                float x = 0;

                //Allow status effects to modify exp gain rate
                __instance.GetSEMan().ModifyRaiseSkill(skill, ref expValue);

                x = absoluteWeightBonus(__instance);
                expValue *= x;
                //Jotunn.Logger.LogMessage("Absolute weight mod: " + x);

                x = relativeWeightBonus(__instance);
                expValue *= x;
                //Jotunn.Logger.LogMessage("Relative weight mod: " + x);

                x = runSpeedBonus(__instance);
                expValue *= x;
                //Jotunn.Logger.LogMessage("Run speed mod: " + x);

                __instance.GetSkills().RaiseSkill(skill, expValue);

                dontSkip = false;
                return dontSkip;
            }
            else if (skill == Skills.SkillType.Swim)
            {
                float expValue = 1f;
                float x = 0;

                //Allow status effects to modify exp gain rate
                __instance.GetSEMan().ModifyRaiseSkill(skill, ref expValue);

                x = absoluteWeightBonus(__instance);
                expValue *= x;
                x = relativeWeightBonus(__instance);
                expValue *= x;
                x = swimSpeedBonus(__instance);
                expValue *= x;

                __instance.GetSkills().RaiseSkill(skill, expValue);

                dontSkip = false;
                return dontSkip;
            }

            return dontSkip;
        }

        public static float absoluteWeightBonus(Player player)
        {
            float weightPercent = Mathf.Clamp01(
                  (player.GetInventory().GetTotalWeight() - AbsoluteWeightMinWeight) 
                / (AbsoluteWeightMaxWeight - AbsoluteWeightMinWeight));
            return  1 + (AbsoluteWeightMod * Mathf.Pow(weightPercent, 2.2f));
        }

        public static float relativeWeightBonus(Player player)
        {
            float weightPercent = player.GetInventory().GetTotalWeight() / player.GetMaxCarryWeight();
            float modifier = 0f;
            //If you're carrying less than 30% carry weight
            if (weightPercent <= RelativeWeightLight)
            {modifier = RelativeWeightLightMod;}

            else if (weightPercent <= RelativeWeightMed)
            {modifier = RelativeWeightMedMod;}

            else if (weightPercent <= RelativeWeightHighMed)
            {modifier = RelativeWeightHighMedMod;}

            else if (weightPercent <= RelativeWeightHeavy)
            {modifier = RelativeWeightHeavyMod;}

            else if (weightPercent <= RelativeWeightFull)
            {modifier = RelativeWeightFullMod;}

            else 
            {modifier = RelativeWeightOverMod;}

            return RelativeWeightMod * (1 + modifier);
        }

        public static float runSpeedBonus(Player player)
        {
            float runMod = player.GetRunSpeedFactor();
            player.m_seman.ApplyStatusEffectSpeedMods(ref runMod);

            return runMod * RunSpeedMod;
        }
        public static float swimSpeedBonus(Player player)
        {
            float swimMod = player.m_swimSpeed;
            player.m_seman.ApplyStatusEffectSpeedMods(ref swimMod);

            return swimMod;
        }


        [HarmonyPatch(nameof(Player.OnSkillLevelup))]
        [HarmonyPostfix]
        static void SkillLevelupPatch (Player __instance, Skills.SkillType skill)
        {
            if (skill == Skills.SkillType.Swim)
            {
                SwimSpeedUpdate(__instance);
            }
        }

        public static void SwimSpeedUpdate(Player player)
        {
            float skillFactor = player.GetSkillFactor(Skills.SkillType.Swim);
            player.m_swimSpeed = BaseSwimSpeed * (1f + Mathf.Lerp(SwimSpeedMin, SwimSpeedMax, skillFactor));
            player.m_swimAcceleration = BaseSwimAccel * (1f + Mathf.Lerp(SwimAccelMin, SwimAccelMax, skillFactor));
            player.m_swimTurnSpeed = BaseSwimTurn * (1f + Mathf.Lerp(SwimTurnMin, SwimTurnMax, skillFactor));
            player.m_swimStaminaDrainMinSkill = SwimStaminaPSMin;
            player.m_swimStaminaDrainMaxSkill = SwimStaminaPSMax;
        }
    }
}
