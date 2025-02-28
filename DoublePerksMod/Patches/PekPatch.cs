using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection;

namespace DoublePerksMod
{
    [HarmonyPatch(typeof(HeroDeveloper), "SelectPerks")]
    public static class PerkPatch
    {
        /// <summary>
        /// Overrides perk selection to grant heroes both main and alternative perks if eligible for NPCs or automatic leveling.
        /// Applies effects immediately and returns false to skip the original method.
        /// </summary>
        /// <param name="__instance">The HeroDeveloper instance managing the hero's perks.</param>
        /// <returns>True to allow the original method to execute, false to skip it.</returns>
        public static bool Prefix(HeroDeveloper __instance)
        {
            Hero hero = __instance.Hero;
            if (hero == null || hero.HeroDeveloper == null)
                return false;

            if (!DoublePerksUtility.ShouldApplyDoublePerks(hero))
                return true;

            var eligiblePerks = PerkObject.All
                .Where(p => hero.GetSkillValue(p.Skill) >= p.RequiredSkillValue &&
                            !hero.GetPerkValue(p) &&
                            (p.AlternativePerk == null || !hero.GetPerkValue(p.AlternativePerk)))
                .ToList();

            var addedPerks = new HashSet<PerkObject>();

            foreach (var perk in eligiblePerks)
            {
                try
                {
                    if (!addedPerks.Contains(perk))
                    {
                        hero.HeroDeveloper.AddPerk(perk);
                        addedPerks.Add(perk);
                        TriggerPerkEffects(hero, perk);
                    }
                    if (perk.AlternativePerk != null && !hero.GetPerkValue(perk.AlternativePerk) && !addedPerks.Contains(perk.AlternativePerk))
                    {
                        hero.HeroDeveloper.AddPerk(perk.AlternativePerk);
                        addedPerks.Add(perk.AlternativePerk);
                        TriggerPerkEffects(hero, perk.AlternativePerk);
                    }
                }
                catch (Exception)
                {
                }
            }

            return false;
        }

        /// <summary>
        /// Triggers perk effects immediately by firing relevant campaign events for the specified hero and perk,
        /// without displaying skill gain messages in chat.
        /// </summary>
        /// <param name="hero">The hero to apply perk effects to.</param>
        /// <param name="perk">The perk whose effects should be triggered.</param>
        private static void TriggerPerkEffects(Hero hero, PerkObject perk)
        {
            if (hero == null || perk == null)
                return;

            Game.Current.EventManager.TriggerEvent(new PerkSelectedByPlayerEvent(perk));
        }
    }
}