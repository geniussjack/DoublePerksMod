using HarmonyLib;
using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection;
using DoublePerksMod.Settings;

namespace DoublePerksMod.Patches
{
    [HarmonyPatch(typeof(SkillVM), "OnPerkSelectionOver")]
    public class PerkSelectionPatch
    {
        /// <summary>
        /// Modifies perk selection behavior to allow manual selection of both main and alternative perks for the player.
        /// Updates UI and applies perk effects immediately. Returns false to skip the original method.
        /// </summary>
        /// <param name="__instance">The SkillVM instance handling perk selection.</param>
        /// <param name="perk">The selected PerkVM object.</param>
        /// <returns>True to allow the original method to execute, false to skip it.</returns>
        private static bool Prefix(SkillVM __instance, PerkVM perk)
        {
            if (perk == null || perk.Perk == null || perk.PerkState != 3 || perk.AlternativeType == 0)
                return true;

            Hero hero = GetHeroFromSkillVM(__instance);
            if (hero == null || hero.HeroDeveloper == null)
                return true;

            if (hero != Hero.MainHero)
                return true;

            if (!DoublePerksSettings.Instance.EnableForPlayer)
                return true;

            try
            {
                if (!hero.GetPerkValue(perk.Perk))
                {
                    hero.HeroDeveloper.AddPerk(perk.Perk);
                    TriggerPerkEffects(hero, perk.Perk);
                }

                PerkObject alternativePerkObject = perk.Perk.AlternativePerk;
                if (alternativePerkObject != null)
                {
                    var alternativePerkVM = __instance.Perks.SingleOrDefault(p => p.Perk == alternativePerkObject);
                    if (alternativePerkVM != null)
                    {
                        if (!hero.GetPerkValue(alternativePerkObject))
                        {
                            hero.HeroDeveloper.AddPerk(alternativePerkObject);
                            TriggerPerkEffects(hero, alternativePerkObject);
                        }
                        alternativePerkVM.PerkState = 3;
                    }
                }

                __instance.OnPropertyChanged("Perks");
                __instance.RefreshValues();
            }
            catch (Exception)
            {
                return true;
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

        /// <summary>
        /// Retrieves the Hero associated with the SkillVM instance using its internal CharacterVM.
        /// </summary>
        /// <param name="skillVM">The SkillVM instance to extract the Hero from.</param>
        /// <returns>The Hero object associated with the SkillVM, or null if not found.</returns>
        private static Hero GetHeroFromSkillVM(SkillVM skillVM)
        {
            var characterVM = (CharacterVM)typeof(SkillVM).GetField("_developerVM", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(skillVM);
            return characterVM?.Hero ?? Hero.MainHero;
        }
    }
}