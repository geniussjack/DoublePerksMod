using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.Attributes.v1;
using System;

namespace DoublePerksMod
{
    public static class DoublePerksUtility
    {
        /// <summary>
        /// Determines whether double perks should be applied to the specified hero based on MCM settings.
        /// </summary>
        /// <param name="hero">The hero to check for double perk eligibility.</param>
        /// <returns>True if double perks should be applied, false otherwise.</returns>
        public static bool ShouldApplyDoublePerks(Hero hero)
        {
            if (hero == Hero.MainHero)
                return DoublePerksSettings.Instance.EnableForPlayer;

            if (hero.Clan == Clan.PlayerClan && hero != Hero.MainHero)
                return DoublePerksSettings.Instance.EnableForCompanions;

            if (hero.Clan?.Leader == hero)
                return DoublePerksSettings.Instance.EnableForRulers;

            if (hero.IsLord)
                return DoublePerksSettings.Instance.EnableForLords;

            return DoublePerksSettings.Instance.EnableForOthers;
        }
    }

    public class DoublePerksSettings : AttributeGlobalSettings<DoublePerksSettings>
    {
        [SettingProperty("Enable for Player", HintText = "Allow player to take both perks.")]
        [SettingPropertyGroup("Double Perks Options")]
        public bool EnableForPlayer { get; set; } = true;

        [SettingProperty("Enable for Lords", HintText = "Allow lords to take both perks.")]
        [SettingPropertyGroup("Double Perks Options")]
        public bool EnableForLords { get; set; } = true;

        [SettingProperty("Enable for Companions", HintText = "Allow companions to take both perks.")]
        [SettingPropertyGroup("Double Perks Options")]
        public bool EnableForCompanions { get; set; } = true;

        [SettingProperty("Enable for Others", HintText = "Allow other characters to take both perks.")]
        [SettingPropertyGroup("Double Perks Options")]
        public bool EnableForOthers { get; set; } = true;

        [SettingProperty("Enable for Rulers", HintText = "Allow rulers to take both perks.")]
        [SettingPropertyGroup("Double Perks Options")]
        public bool EnableForRulers { get; set; } = true;

        /// <summary>
        /// Gets the unique identifier for this settings instance.
        /// </summary>
        public override string Id => "DoublePerksMod";

        /// <summary>
        /// Gets the display name for this settings instance shown in the MCM menu.
        /// </summary>
        public override string DisplayName => "Double Perks Mod";

        /// <summary>
        /// Gets the folder name where settings are stored.
        /// </summary>
        public override string FolderName => "DoublePerksMod";

        /// <summary>
        /// Gets the format type for saving settings (e.g., "json2" for MCM v5).
        /// </summary>
        public override string FormatType => "json2";
    }

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
        /// Triggers perk effects immediately by firing relevant campaign events for the specified hero and perk.
        /// </summary>
        /// <param name="hero">The hero to apply perk effects to.</param>
        /// <param name="perk">The perk whose effects should be triggered.</param>
        private static void TriggerPerkEffects(Hero hero, PerkObject perk)
        {
            if (hero == null || perk == null)
                return;

            CampaignEventDispatcher.Instance.OnHeroGainedSkill(hero, perk.Skill, 0, true);
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
        /// Triggers perk effects immediately by firing relevant campaign events for the specified hero and perk.
        /// </summary>
        /// <param name="hero">The hero to apply perk effects to.</param>
        /// <param name="perk">The perk whose effects should be triggered.</param>
        private static void TriggerPerkEffects(Hero hero, PerkObject perk)
        {
            if (hero == null || perk == null)
                return;

            CampaignEventDispatcher.Instance.OnHeroGainedSkill(hero, perk.Skill, 0, true);
            Game.Current.EventManager.TriggerEvent(new PerkSelectedByPlayerEvent(perk));
        }
    }

    [HarmonyPatch(typeof(Campaign), "OnGameLoaded")]
    public static class GameLoadPatch
    {
        /// <summary>
        /// Checks and adds alternative perks for all alive heroes after the game loads, applying effects immediately.
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
        /// Triggers perk effects immediately by firing relevant campaign events for the specified hero and perk.
        /// </summary>
        /// <param name="hero">The hero to apply perk effects to.</param>
        /// <param name="perk">The perk whose effects should be triggered.</param>
        private static void TriggerPerkEffects(Hero hero, PerkObject perk)
        {
            if (hero == null || perk == null)
                return;

            CampaignEventDispatcher.Instance.OnHeroGainedSkill(hero, perk.Skill, 0, true);
            Game.Current.EventManager.TriggerEvent(new PerkSelectedByPlayerEvent(perk));
        }
    }
}