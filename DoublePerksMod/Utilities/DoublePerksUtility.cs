using TaleWorlds.CampaignSystem;

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
}