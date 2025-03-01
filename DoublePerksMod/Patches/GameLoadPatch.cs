using HarmonyLib;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection;
using DoublePerksMod.Utilities;

namespace DoublePerksMod.Patches
{
    [HarmonyPatch(typeof(Campaign), "OnGameLoaded")]
    public static class GameLoadPatch
    {
        /// <summary>
        /// Checks and adds alternative perks for all alive heroes after the game loads, applying effects immediately,
        /// without displaying skill gain messages in chat.
        /// </summary>
        public static void Postfix()
        {
            foreach (Hero hero in Hero.AllAliveHeroes)
            {
                if (hero.HeroDeveloper == null || !DoublePerksUtility.ShouldApplyDoublePerks(hero))
                    continue;

                var currentPerks = PerkObject.All.Where(p => hero.GetPerkValue(p)).ToList();
                foreach (var perk in currentPerks)
                {
                    if (perk.AlternativePerk != null && !hero.GetPerkValue(perk.AlternativePerk))
                    {
                        hero.HeroDeveloper.AddPerk(perk.AlternativePerk);
                        TriggerPerkEffects(hero, perk.AlternativePerk);
                    }
                }
            }
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