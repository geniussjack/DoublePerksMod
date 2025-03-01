using MCM.Abstractions.Attributes;
using MCM.Abstractions.Base.Global;
using MCM.Abstractions.Attributes.v1;

namespace DoublePerksMod.Settings
{
    public class DoublePerksSettings : AttributeGlobalSettings<DoublePerksSettings>
    {
        /// <summary>
        /// Gets or sets whether the player can take both perks.
        /// </summary>
        [SettingProperty("Enable for Player", HintText = "Allow player to take both perks.")]
        [SettingPropertyGroup("Double Perks Options")]
        public bool EnableForPlayer { get; set; } = true;

        /// <summary>
        /// Gets or sets whether lords can take both perks.
        /// </summary>
        [SettingProperty("Enable for Lords", HintText = "Allow lords to take both perks.")]
        [SettingPropertyGroup("Double Perks Options")]
        public bool EnableForLords { get; set; } = true;

        /// <summary>
        /// Gets or sets whether clan members can take both perks.
        /// </summary>
        [SettingProperty("Enable for Clan Members", HintText = "Allow player's clan members to take both perks.")]
        [SettingPropertyGroup("Double Perks Options")]
        public bool EnableForClanMembers { get; set; } = true;

        /// <summary>
        /// Gets or sets whether other characters can take both perks.
        /// </summary>
        [SettingProperty("Enable for Others", HintText = "Allow other characters to take both perks.")]
        [SettingPropertyGroup("Double Perks Options")]
        public bool EnableForOthers { get; set; } = true;

        /// <summary>
        /// Gets or sets whether rulers can take both perks.
        /// </summary>
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
}