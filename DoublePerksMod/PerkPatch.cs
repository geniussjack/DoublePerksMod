using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection;
using TaleWorlds.Core;

namespace DoublePerksMod
{
    [HarmonyPatch(typeof(SkillVM), "OnPerkSelectionOver")]
    public class PerkSelectionPatch
    {
        /// <summary>
        /// Modifies perk selection behavior to allow manual selection of both main and alternative perks for the player.
        /// Updates UI and applies perk effects immediately. Returns false to skip the original method.
        /// </summary>
        private static bool Prefix(SkillVM __instance, PerkVM perk)
        {
            // Проверяем, что перк выбран и имеет альтернативу
            if (perk == null || perk.Perk == null || perk.PerkState != 3 || perk.AlternativeType == 0)
                return true; // Пропускаем, если перк не выбран или нет альтернативы

            // Получаем героя (предполагаем, что это игрок)
            Hero hero = GetHeroFromSkillVM(__instance);
            if (hero == null || hero.HeroDeveloper == null)
                return true; // Пропускаем, если герой не найден

            // Ограничиваем функциональность только игроком
            if (hero != Hero.MainHero)
                return true; // Пропускаем для NPC, оставляем это для PerkPatch

            Log($"Player selected perk: {perk.Perk.Name}");

            try
            {
                // Добавляем выбранный перк
                if (!hero.GetPerkValue(perk.Perk))
                {
                    hero.HeroDeveloper.AddPerk(perk.Perk);
                    Log($"Added perk: {perk.Perk.Name}");
                    TriggerPerkEffects(hero, perk.Perk);
                }

                // Проверяем и добавляем альтернативный перк
                PerkObject alternativePerkObject = perk.Perk.AlternativePerk;
                if (alternativePerkObject != null)
                {
                    var alternativePerkVM = __instance.Perks.SingleOrDefault(p => p.Perk == alternativePerkObject);
                    if (alternativePerkVM != null)
                    {
                        if (!hero.GetPerkValue(alternativePerkObject))
                        {
                            hero.HeroDeveloper.AddPerk(alternativePerkObject);
                            Log($"Added alternative perk: {alternativePerkObject.Name}");
                            TriggerPerkEffects(hero, alternativePerkObject);
                        }
                        alternativePerkVM.PerkState = 3; // Обновляем UI
                    }
                }

                // Обновляем UI
                __instance.OnPropertyChanged("Perks");
                __instance.RefreshValues();
            }
            catch (Exception ex)
            {
                Log($"Error processing perk selection: {ex.Message}");
                return true; // Пропускаем оригинальный метод при ошибке
            }

            return false; // Пропускаем оригинальный метод
        }

        /// <summary>
        /// Triggers perk effects immediately by firing campaign events.
        /// </summary>
        private static void TriggerPerkEffects(Hero hero, PerkObject perk)
        {
            if (hero == null || perk == null)
                return;

            CampaignEventDispatcher.Instance.OnHeroGainedSkill(hero, perk.Skill, 0, true);
            Game.Current.EventManager.TriggerEvent(new PerkSelectedByPlayerEvent(perk));
            Log($"Triggered effects for perk: {perk.Name}");
        }

        /// <summary>
        /// Retrieves the Hero associated with the SkillVM instance.
        /// </summary>
        private static Hero GetHeroFromSkillVM(SkillVM skillVM)
        {
            var characterVM = (CharacterVM)typeof(SkillVM).GetField("_developerVM", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(skillVM);
            return characterVM?.Hero ?? Hero.MainHero; // MainHero как запасной вариант
        }

        /// <summary>
        /// Logs messages to a file on the desktop for debugging.
        /// </summary>
        private static void Log(string message)
        {
            try
            {
                string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DoublePerksMod_Log.txt");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging failed: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(HeroDeveloper), "SelectPerks")]
    public static class PerkPatch
    {
        public static bool Prefix(HeroDeveloper __instance)
        {
            Hero hero = __instance.Hero;
            if (hero == null || hero.HeroDeveloper == null) return false;

            Log($"Starting double perks processing for {hero.Name}");

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
                        Log($"Added main perk: {perk.Name}");
                        TriggerPerkEffects(hero, perk);
                    }
                    if (perk.AlternativePerk != null && !hero.GetPerkValue(perk.AlternativePerk) && !addedPerks.Contains(perk.AlternativePerk))
                    {
                        hero.HeroDeveloper.AddPerk(perk.AlternativePerk);
                        addedPerks.Add(perk.AlternativePerk);
                        Log($"Added alternative perk: {perk.AlternativePerk.Name}");
                        TriggerPerkEffects(hero, perk.AlternativePerk);
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error adding perks: {ex.Message}");
                }
            }

            Log($"Finished processing double perks for {hero.Name}");
            return false;
        }

        private static void TriggerPerkEffects(Hero hero, PerkObject perk)
        {
            if (hero == null || perk == null) return;
            CampaignEventDispatcher.Instance.OnHeroGainedSkill(hero, perk.Skill, 0, true);
            Game.Current.EventManager.TriggerEvent(new PerkSelectedByPlayerEvent(perk));
            Log($"Triggered effects for perk: {perk.Name}");
        }

        private static void Log(string message)
        {
            try
            {
                string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DoublePerksMod_Log.txt");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging failed: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(Campaign), "OnGameLoaded")]
    public static class GameLoadPatch
    {
        public static void Postfix()
        {
            Log("Game loaded, checking perks for all heroes.");
            foreach (Hero hero in Hero.AllAliveHeroes)
            {
                if (hero.HeroDeveloper != null)
                {
                    var currentPerks = PerkObject.All.Where(p => hero.GetPerkValue(p)).ToList();
                    foreach (var perk in currentPerks)
                    {
                        if (perk.AlternativePerk != null && !hero.GetPerkValue(perk.AlternativePerk))
                        {
                            hero.HeroDeveloper.AddPerk(perk.AlternativePerk);
                            Log($"Added alternative perk: {perk.AlternativePerk.Name} (existing perk)");
                            TriggerPerkEffects(hero, perk.AlternativePerk);
                        }
                    }
                }
            }
            Log("Perk checking completed.");
        }

        private static void TriggerPerkEffects(Hero hero, PerkObject perk)
        {
            if (hero == null || perk == null) return;
            CampaignEventDispatcher.Instance.OnHeroGainedSkill(hero, perk.Skill, 0, true);
            Game.Current.EventManager.TriggerEvent(new PerkSelectedByPlayerEvent(perk));
            Log($"Triggered effects for perk: {perk.Name}");
        }

        private static void Log(string message)
        {
            try
            {
                string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DoublePerksMod_Log.txt");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging failed: {ex.Message}");
            }
        }
    }
}