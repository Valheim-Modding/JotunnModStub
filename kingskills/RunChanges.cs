using BepInEx;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using kingskills.Commands;
using UnityEngine;

namespace kingskills
{
    class RunChanges
    {
    }

    [HarmonyPatch(typeof(Player), nameof(Player.RaiseSkill))]
    class RunExp
    {
        const float maxWeight = 2000;
        const float maxWeightWeight = 20;
        const float relativeWeightWeight = 1;

        static bool Prefix(Player __instance, Skills.SkillType skill, float value = 1f)
        {
            bool dontSkip = true;

            if (skill == Skills.SkillType.Run)
            {
                float expValue = 1f;
                float x = 0;

                //Allow status effects to modify exp gain rate
                __instance.GetSEMan().ModifyRaiseSkill(skill, ref expValue);

                //The effects of absolute weight on your experience rate
                x = absoluteWeightMod(__instance);
                //Jotunn.Logger.LogMessage("Absolute weight mod: " + x);
                expValue *= x;

                x = 0;

                //The effects of relative weight on your experience rate
                x = relativeWeightMod(__instance);
                //Jotunn.Logger.LogMessage("Relative weight mod: " + x);
                expValue *= x;

                x = 0;


                x = runSpeedMod(__instance);
                //Jotunn.Logger.LogMessage("Run speed mod: " + x);
                expValue *= x;

                __instance.GetSkills().RaiseSkill(skill, expValue);

                dontSkip = false;
                return dontSkip;
            }

            return dontSkip;
        }

        public static float absoluteWeightMod(Player player)
        {
            float weightPercent = Mathf.Clamp01(player.GetInventory().GetTotalWeight() / maxWeight);
            return  1 + (maxWeightWeight * Mathf.Pow(weightPercent, 2.2f));
        }

        public static float relativeWeightMod(Player player)
        {
            float weightPercent = player.GetInventory().GetTotalWeight() / player.GetMaxCarryWeight();
            float modifier = 0f;
            //If you're carrying less than 30% carry weight
            if (weightPercent <= .3f)
            {
                modifier = -.25f;
            }
            else if (weightPercent <= .5f)
            {
                modifier = 0f;
            }
            else if (weightPercent <= .66f)
            {
                modifier = .25f;
            }
            else if (weightPercent <= .8f)
            {
                modifier = .5f;
            }
            else if (weightPercent <= 1f)
            {
                modifier = .8f;
            }
            else 
            {
                modifier = 1.5f;
            }

            return relativeWeightWeight * (1 + modifier);
        }

        public static float runSpeedMod(Player player)
        {
            float runMod = 1f; 
            float skillFactor = player.GetSkills().GetSkillFactor(Skills.SkillType.Run);
            runMod = (1f + skillFactor * 0.25f) * (1f + player.GetEquipmentMovementModifier() * 1.5f);

            return runMod;
        }
    }
}
