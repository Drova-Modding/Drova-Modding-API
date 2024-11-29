namespace Drova_Modding_API.Systems
{
    /**
     * Regions in the game.
     */
    public enum Region
    {
        /**
         * The red tower.
         */
        RedTower,
        /**
         * The mine.
         */
        Mine,
        /**
         * A cave.
         */
        Cave,
        /**
         * The city.
         */
        City,
        /**
         * The spider dungeon.
         */
        SpiderDungeon,
        /**
         * The Auwald.
         */
        Auwald,
        /**
         * The Nemeton.
         */
        Nemeton,
        /**
         * The entry to the Nemeton.
         */
        EntryNemeton,
        /**
         * The intro.
         */
        Intro,
        /**
         * The ruins.
         */
        Ruins,
        /**
         * The tavern.
         */
        Tavern,
        /**
         * The city dungeon.
         */
        CityDungeon,
        /**
         * The death moor.
         */
        DeathMoor,
        /**
         * The academy.
         */
        Academy,
        /**
         * The forest.
         */
        Forest,
        /**
         * The library.
         */
        Library,
        /**
         * The friendly moor.
         */
        FriendlyMoor,
        /**
         * The Mother? idk what this is.
         */
        Mutter,
        /**
         * The glowing forest.
         */
        Leuchtwald,
        /**
         * The river.
         */
        River,
        /**
         * The rooten moor.
         */
        RootenMoor,
        /**
         * The wood camp.
         */
        WoodCamp,
        /**
         * The ruins camp.
         */
        RuinsCamp,
        /**
         * The ruin under.
         */
        RuinUnder,
        /**
         * The mage camp.
         */
        Magecamp,
        /**
         * The ruin explorer.
         */
        Ruinexplorer,
        /**
         * The ruin smuglers.
         */
        RuinSchmuggler,
        /**
         * The Hain.
         */
        Hain,
        /**
         * The Heath.
         */
        Heide,
        /**
         * The Abyss.
         */
        Schlund,
        /**
         * The overworld or a cave Enum, basically a default value.
         */
        Overworld_Or_Cave
    }

    /// <summary>
    /// Extensions for the region enum.
    /// </summary>
    public static class RegionExtensions
    {
        /**
         * Get the name of a region.
         */
        public static string GetRegionName(this Region region)
        {
            return region switch
            {
                Region.RedTower => "RedTower",
                Region.Mine => "Mine",
                Region.Cave => "Cave",
                Region.City => "City",
                Region.SpiderDungeon => "SpiderDungeon",
                Region.Auwald => "Auwald",
                Region.Nemeton => "Nemeton",
                Region.EntryNemeton => "EntryNemeton",
                Region.Intro => "Intro",
                Region.Ruins => "Ruins",
                Region.Tavern => "Tavern",
                Region.CityDungeon => "CityDungeon",
                Region.DeathMoor => "DeathMoor",
                Region.Academy => "Academy",
                Region.Forest => "Forest",
                Region.Library => "Library",
                Region.FriendlyMoor => "FriendlyMoor",
                Region.Mutter => "Mutter",
                Region.Leuchtwald => "Leuchtwald",
                Region.River => "River",
                Region.RootenMoor => "RootenMoor",
                Region.WoodCamp => "WoodCamp",
                Region.RuinsCamp => "RuinsCamp",
                Region.RuinUnder => "RuinUnder",
                Region.Magecamp => "Magecamp",
                Region.Ruinexplorer => "Ruinexplorer",
                Region.RuinSchmuggler => "RuinSchmuggler",
                Region.Hain => "Hain",
                Region.Heide => "Heide",
                Region.Schlund => "Schlund",
                _ => "Overworld_Or_Cave"
            };
        }

        /**
         * Get a region by its name.
         */
        public static Region GetRegionByName(string name)
        {
            return name.ToLower() switch
            {
                "redtower" => Region.RedTower,
                "mine" => Region.Mine,
                "cave" => Region.Cave,
                "city" => Region.City,
                "spiderdungeon" => Region.SpiderDungeon,
                "auwald" => Region.Auwald,
                "nemeton" => Region.Nemeton,
                "entrynemeton" => Region.EntryNemeton,
                "intro_fog" => Region.Intro,
                "intro_real" => Region.Intro,
                "ruins" => Region.Ruins,
                "tavern" => Region.Tavern,
                "citydungeon" => Region.CityDungeon,
                "deathmoor" => Region.DeathMoor,
                "academy" => Region.Academy,
                "forest" => Region.Forest,
                "library" => Region.Library,
                "friendlymoor" => Region.FriendlyMoor,
                "mutter" => Region.Mutter,
                "leuchtwald" => Region.Leuchtwald,
                "river" => Region.River,
                "rootenmoor" => Region.RootenMoor,
                "woodcamp" => Region.WoodCamp,
                "ruinscamp" => Region.RuinsCamp,
                "ruinunder" => Region.RuinUnder,
                "magecamp" => Region.Magecamp,
                "ruinexplorer" => Region.Ruinexplorer,
                "ruinschmuggler" => Region.RuinSchmuggler,
                "hain" => Region.Hain,
                "heide" => Region.Heide,
                "schlund" => Region.Schlund,
                _ => Region.Overworld_Or_Cave
            };
        }
    }
}
