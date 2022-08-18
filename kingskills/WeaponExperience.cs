using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace kingskills.WeaponExperience
{
    class Config
    {
        public static ConfigEntry<float> XpStrikeCharFactor;
        public static ConfigEntry<float> XpStrikeDestructableFactor;

        public static ConfigEntry<float> XpHoldFactor;
        public static ConfigEntry<float> XpHoldTimer;
        public static ConfigEntry<float> XpHoldUnarmedFactor;

        public static ConfigEntry<float> XpSwingFactor;
        public static ConfigEntry<float> XpSwingsPerSecondInCombat;

        public static ConfigEntry<float> XpStrikeFactor;
        public static ConfigEntry<float> XpBonusFactor;

        const float TotalXp = 20553.6f;
        const float MasteryTime = 5f * 60 * 60;  // seconds
        const float XpPerSec = TotalXp / MasteryTime; // xp/s needed to master skill in target_mastery_time

        public static float XpHoldRate;
        public static float XpSwingRate;
        public const float XpDamageDegree = 0.45f;

        public static void Init(ConfigFile cfg)
        {
            XpHoldFactor = cfg.Bind("Experience.Weapons", "HoldFactor", 0.05f, "Percentage (0-1) of weapon XP expected to come from holding the weapon");
            XpHoldTimer = cfg.Bind("Experience.Weapons", "WeaponHoldTimer", 1.0f, "Interval at which experience is gained from holding a weapon.");
            XpHoldUnarmedFactor = cfg.Bind("Experience.Weapons", "UnarmedHoldFactor", 0.2f, "Factor affecting XP gain from 'holding' out your fists");

            XpSwingFactor = cfg.Bind("Experience.Weapons", "SwingFactor", 0.10f, "Percentage (0-1) of weapon XP expected to come from swinging the weapon");
            XpSwingsPerSecondInCombat = cfg.Bind("Experience.Weapons", "XpSwingsPerSecondInCombat", 10.0f / 30.0f, "Number of expected swings per second in combat");

            XpStrikeFactor = cfg.Bind("Experience.Weapons", "StrikeFactor", 0.50f, "Percentage (0-1) of weapon XP expected to come from striking with the weapon");
            XpBonusFactor = cfg.Bind("Experience.Weapons", "BonusFactor", 0.35f, "Percentage (0-1) of weapon XP expected to come from bonus experience");

            XpStrikeCharFactor = cfg.Bind("Experience.Weapons", "StrikeCharacterFactor", 1.0f, "Multiplier to modify experience gain from striking characters");
            XpStrikeDestructableFactor = cfg.Bind("Experience.Weapons", "StrikeDestructibleFactor", 0.4f, "Multiplier to modify experience gain from striking destructibles");

            XpHoldRate = XpHoldFactor.Value * XpPerSec;
            XpSwingRate = (XpSwingFactor.Value / XpSwingsPerSecondInCombat.Value) * XpPerSec;
        }
    }

    class Manager
    {
        // Patch IDestructible.Damage() to gain experience for player based on damage events.
        public static void Strike(Player p, IDestructible __instance, HitData hit, float factor=1.0f)
        {
            if (hit.m_attacker == p.GetZDOID())
            {
                //Jotunn.Logger.LogMessage($"Player dealt damage to {__instance.GetDestructibleType()}");
                float damage = hit.m_damage.GetTotalDamage();
                // TODO: account for attack speed, vulnerabilities?
                float damage_xp = 2 * Config.XpStrikeFactor.Value * Mathf.Pow(damage, Config.XpDamageDegree);
                float final_xp = damage_xp * factor;
                //Jotunn.Logger.LogMessage($"Incrementing {hit.m_skill} by {final_xp} = damage {damage} ^ {Config.XpDamageDegree} * factor {factor}");
                p.RaiseSkill(hit.m_skill, final_xp);
            }
        }

        public static void Swing(Player p)
        {
            Skills.SkillType skill = p.GetCurrentWeapon().m_shared.m_skillType;
            //Jotunn.Logger.LogMessage($"Player swinging with {skill} for {Config.XpSwingRate} XP");
            p.RaiseSkill(skill, Config.XpSwingRate);
        }
    }

    [HarmonyPatch]
    class PatchWeaponXp
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        static void Character_Damage(Character __instance, HitData hit)
        {
            Manager.Strike(Player.m_localPlayer, __instance, hit, Config.XpStrikeCharFactor.Value);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Destructible), nameof(Destructible.Damage))]
        static void Destructible_Damage(Destructible __instance, HitData hit)
        {
            Manager.Strike(Player.m_localPlayer, __instance, hit, Config.XpStrikeDestructableFactor.Value);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
        static void Humanoid_StartAttack(Humanoid __instance, Character target, bool secondaryAttack, bool __result)
        {
            if (__result && __instance is Player && __instance == Player.m_localPlayer) {
                Manager.Swing(__instance as Player);
            }
        }
    }

    [HarmonyPatch]
    class PatchWeaponHoldXp
    {
        static ItemDrop.ItemData last;
        static float timer;

        // Get the actual weapon a player would swing with, or null if the player couldn't swing in this state.
        // Compared to Player.GetCurrentWeapon(), for example, this returns null if the player is holding
        // a hammer or pickaxe, but will return unarmed even if the player is holding a shield.
        static ItemDrop.ItemData GetPlayerWeapon(Player p)
        {
            if (p.m_leftItem != null && p.m_leftItem.IsWeapon() && p.m_leftItem.m_shared.m_skillType != Skills.SkillType.Pickaxes)
            {
                return p.m_leftItem;
            }
            if (p.m_rightItem != null && p.m_rightItem.IsWeapon() && p.m_rightItem.m_shared.m_skillType != Skills.SkillType.Pickaxes)
            {
                return p.m_rightItem;
            }
            if (p.m_rightItem == null && (p.m_leftItem == null
                || p.m_leftItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield))
            {
                return p.m_unarmedWeapon.m_itemData;
            }
            return null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.FixedUpdate))]
        static void Player_FixedUpdate(Player __instance)
        {
            if (__instance != Player.m_localPlayer || !__instance.CanMove())
            {
                return;
            }

            ItemDrop.ItemData weapon = GetPlayerWeapon(__instance);
            if (weapon == null)
            {
                last = null;
                return;
            }

            float dt = Time.fixedDeltaTime;
            // Unequipped or changed weapon, restart timer.
            if (weapon != last)
            {
                last = weapon;
                timer = dt;
            }
            // Held onto same weapon for some time.
            else
            {
                timer += dt;
                if (timer >= Config.XpHoldTimer.Value)
                {
                    float ticks = timer / Config.XpHoldTimer.Value;
                    float holdXp = ticks * Config.XpHoldRate;
                    if (weapon == __instance.m_unarmedWeapon.m_itemData)
                    {
                        holdXp *= Config.XpHoldUnarmedFactor.Value;
                    }
                    Skills.SkillType skill = weapon.m_shared.m_skillType;
                    //Jotunn.Logger.LogMessage($"Holding {skill} for {timer}s, adding {holdXp} xp");
                    __instance.RaiseSkill(skill, holdXp);
                    timer -= ticks * Config.XpHoldTimer.Value;
                }
            }
        }
    }
}
