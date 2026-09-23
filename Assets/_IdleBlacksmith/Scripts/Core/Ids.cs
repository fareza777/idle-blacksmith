namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Canonical content ids. These are the keys shared by GameConfig defs, save data and
    /// managers, so they must never drift — reference the constants, never raw strings.
    /// </summary>
    public static class BuildingId
    {
        public const string Smithy = "smithy";
        public const string Mine = "mine";
        public const string Market = "market";
        public const string Gate = "gate";
        public const string Sanctum = "sanctum";
        public const string Furnace = "furnace";
        public const string Storehouse = "storehouse";

        public static readonly string[] All = { Smithy, Mine, Market, Gate, Sanctum, Furnace, Storehouse };
    }

    public static class RecipeId
    {
        public const string Copper = "copper";
        public const string Iron = "iron";
        public const string Steel = "steel";
        public const string EmberAxe = "emberaxe";
        public const string Silver = "silver";
        public const string Mithril = "mithril";
        public const string Dragonsteel = "dragonsteel";
        public const string Frostbrand = "frostbrand";
        public const string Voidreaver = "voidreaver";

        public static readonly string[] All = { Copper, Iron, Steel, EmberAxe, Silver, Frostbrand, Mithril, Dragonsteel, Voidreaver };
    }

    public static class RuneId
    {
        public const string Flame = "rune_flame";
        public const string Haste = "rune_haste";
        public const string Fortune = "rune_fortune";
        public const string Wealth = "rune_wealth";

        public static readonly string[] All = { Flame, Haste, Fortune, Wealth };
    }

    public static class TalentId
    {
        public const string EmberHeat = "ember_heat";
        public const string DeepVeins = "deep_veins";
        public const string QuickHands = "quick_hands";
        public const string SilverTongue = "silver_tongue";
        public const string LuckyStrike = "lucky_strike";
        public const string NightShift = "night_shift";
        public const string MasterMerchant = "master_merchant";
        public const string RichVeins = "rich_veins";
        public const string GuildContacts = "guild_contacts";
        public const string RuneMastery = "rune_mastery";

        public static readonly string[] All =
        {
            EmberHeat, DeepVeins, QuickHands, SilverTongue, LuckyStrike,
            NightShift, MasterMerchant, RichVeins, GuildContacts, RuneMastery,
        };
    }

    public static class UpgradeId
    {
        public const string Craft = "craft";
        public const string Carry = "carry";
        public const string Rack = "rack";
        public const string OreCap = "orecap";
        public const string Luck = "luck";
        public const string Charm = "charm";
    }
}
