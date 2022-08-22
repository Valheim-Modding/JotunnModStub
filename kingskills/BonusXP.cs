using HarmonyLib;
using kingskills.WeaponExperience;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace kingskills
{

    [HarmonyPatch(typeof(Character))]
    class CharacterPatch
    {
        //How much bonus xp do we get for staggering an enemy with the club?
        public const float ClubBXPStagger = 20f;

        public const float SwordBXPStaggerHit = 20f;

        //For transferring stagger experience check
        static Player playerRef = null;
        static bool staggerFlag = false;

        [HarmonyPatch(nameof(Character.ApplyDamage))]
        [HarmonyPrefix]
        private static void OnDamageTrigger(Character __instance, HitData hit)
        {
            //Jotunn.Logger.LogMessage($"An apply damage function has run, and I'm catching a hit. the hit says");
            ZDOID player = hit.m_attacker;
            //Jotunn.Logger.LogMessage($"{player.ToString()} is the one perpetrating this attack");

            if (Player.m_localPlayer.GetZDOID().Equals(player))
            {
                //Jotunn.Logger.LogMessage($"A player hit someone");
                playerRef = Player.m_localPlayer;
                if (__instance.IsStaggering())
                {
                    staggerFlag = false;
                    OnStaggerHurt(playerRef);
                }
                else
                {
                    staggerFlag = true;
                }
            }
        }

        [HarmonyPatch(nameof(Character.RPC_Stagger))]
        [HarmonyPostfix]
        private static void StaggerPostFix(Character __instance)
        {
            if (staggerFlag)
            {
                //Jotunn.Logger.LogMessage($"Stagger flag redeemed! Turned back off");
                if (PatchWeaponHoldXp.GetPlayerWeapon(playerRef).m_shared.m_skillType == Skills.SkillType.Clubs)
                    playerRef.RaiseSkill(Skills.SkillType.Clubs, ClubBXPStagger);
                staggerFlag = false;
            }
        }

        private static void OnStaggerHurt(Player attacker)
        {
            if (PatchWeaponHoldXp.GetPlayerWeapon(attacker).m_shared.m_skillType == Skills.SkillType.Swords)
            {
                attacker.RaiseSkill(Skills.SkillType.Swords, SwordBXPStaggerHit);
                Jotunn.Logger.LogMessage($"A player just hit us with a sword while we were staggered, so applying bonus exp");
            }
        }
    }


    [HarmonyPatch(typeof(TreeLog))]
    class TreeLogPatch
    {
        public const float AxeBXPRange = 100f;
        public const float AxeBXPTreeAmount = 20f;

        [HarmonyPatch(nameof(TreeLog.Destroy))]
        [HarmonyPrefix]
        public static void TreeLogDestroyPatch(TreeLog __instance)
        {
            //Jotunn.Logger.LogMessage($"This log is killed. Closest player's getting the exp");
            Player closestPlayer = Player.GetClosestPlayer(__instance.m_body.transform.position, AxeBXPRange);
            if (closestPlayer != null)
            {
                if (PatchWeaponHoldXp.GetPlayerWeapon(closestPlayer).m_shared.m_skillType == Skills.SkillType.Axes)
                    closestPlayer.RaiseSkill(Skills.SkillType.Axes, AxeBXPTreeAmount);
            }
        }
    }
}
