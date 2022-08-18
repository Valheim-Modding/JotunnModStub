using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace kingskills.WeaponExperience
{
    class Config
    {
        public static ConfigEntry<float> xp_character_factor;
        public static ConfigEntry<float> xp_destructible_factor;

        public static void Init(ConfigFile cfg)
        {
            xp_character_factor = cfg.Bind("Experience.Weapons", "CharacterFactor", 1.0f, "Factor to modify experience gain from hitting characters.");
            xp_destructible_factor = cfg.Bind("Experience.Weapons", "DestructibleFactor", 0.4f, "Factor to modify experience gain from hitting destructibles.");
        }
    }

    class Manager
    {
        // Patch IDestructible.Damage() to gain experience for player based on damage events.
        public static void Damage(IDestructible __instance, HitData hit, float factor=1.0f)
        {
            Jotunn.Logger.LogMessage($"Damage to {__instance.GetDestructibleType()} detected");
            if (hit.m_attacker == Player.m_localPlayer.GetZDOID())
            {
                float xp_factor = hit.m_damage.m_damage * factor;
                Jotunn.Logger.LogMessage($"Incrementing {hit.m_skill} by {xp_factor} = damage {hit.m_damage.m_damage} * factor {factor}");
                Player.m_localPlayer.RaiseSkill(hit.m_skill, xp_factor);
            }
        }
    }

    [HarmonyPatch]
    class PatchDamage
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        static void Character_Damage(Character __instance, HitData hit) {
            Manager.Damage(__instance, hit, Config.xp_character_factor.Value);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Destructible), nameof(Destructible.Damage))]
        static void Destructible_Damage(Destructible __instance, HitData hit) {
            Manager.Damage(__instance, hit, Config.xp_destructible_factor.Value);
        }
    }
}
