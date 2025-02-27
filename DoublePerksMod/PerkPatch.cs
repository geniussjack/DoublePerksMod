using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;

namespace DoublePerksMod
{
    [HarmonyPatch(typeof(SkillVM), "OnPerkSelectionOver")]
    public class PerkSelectionPatch
    {
        /// <summary>
        /// Modifies perk selection behavior to automatically select an alternative perk if conditions are met.
        /// Returns false to prevent the original method from executing.
        /// </summary>
        /// <param name="__instance">The SkillVM instance handling perk selection.</param>
        /// <param name="perk">The selected PerkVM object.</param>
        /// <returns>False to skip the original method.</returns>
        private static bool Prefix(SkillVM __instance, PerkVM perk)
        {
            if (perk.AlternativeType == 0 || perk.PerkState != 3)
                return false;

            var alternativePerk = __instance.Perks.SingleOrDefault(p => p.Perk == perk.Perk.AlternativePerk);
            if (alternativePerk != null)
            {
                alternativePerk.PerkState = 3;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(HeroDeveloper), "SelectPerks")]
    public static class PerkPatch
    {
        /// <summary>
        /// Overrides perk selection to grant heroes both main and alternative perks if eligible.
        /// Logs the process and handles exceptions, returning false to skip the original method.
        /// </summary>
        /// <param name="__instance">The HeroDeveloper instance managing the hero's perks.</param>
        /// <returns>False to prevent the original method from executing.</returns>
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
                        Log($"Added main perk: {perk.Name} for {hero.Name}");
                    }
                    if (perk.AlternativePerk != null && !hero.GetPerkValue(perk.AlternativePerk) && !addedPerks.Contains(perk.AlternativePerk))
                    {
                        hero.HeroDeveloper.AddPerk(perk.AlternativePerk);
                        addedPerks.Add(perk.AlternativePerk);
                        Log($"Added alternative perk: {perk.AlternativePerk.Name} for {hero.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error adding perks for {hero.Name}: {ex.Message}");
                }
            }

            Log($"Finished processing double perks for {hero.Name}");
            return false;
        }

        /// <summary>
        /// Writes a message with a timestamp to a log file on the desktop for debugging purposes.
        /// </summary>
        /// <param name="message">The message to log.</param>
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
        /// <summary>
        /// Checks and adds alternative perks for all alive heroes after the game loads.
        /// Logs the process for debugging and verification.
        /// </summary>
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
                            Log($"Added alternative perk: {perk.AlternativePerk.Name} for {hero.Name} (existing perk)");
                        }
                    }
                }
            }
            Log("Perk checking completed.");
        }

        /// <summary>
        /// Writes a message with a timestamp to a log file on the desktop for debugging purposes.
        /// </summary>
        /// <param name="message">The message to log.</param>
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