using Random = System.Random;
using UnityEngine;

namespace Drova_Modding_API.Systems.Spawning.Templates
{
    /// <summary>
    /// Difficulty tier used to scale a bandit's equipment quality.
    /// </summary>
    public enum BanditDifficulty
    {
        /// <summary>Coarse / improvised weapons, ragged armour.</summary>
        Easy = 0,

        /// <summary>Standard bandit weapons and T1/T2 bandit armour.</summary>
        Normal = 1,

        /// <summary>Quality weapons, T4 armour and heavier shields.</summary>
        Hard = 2
    }

    /// <summary>
    /// Factory for spawning bandit NPCs with varied weapon loadouts, randomised
    /// bandit-themed cosmetics, and difficulty-scaled equipment quality.
    /// </summary>
    public static class BanditCreator
    {
        // ── Weapon tables (indexed by BanditDifficulty) ────────────────────────

        // Daggers: crude knife → iron dagger → heart-seeker
        private static readonly string[] DaggerByDifficulty =
        [
            "weapon_dagger_coarseDagger",
            "weapon_dagger_ironDagger",
            "weapon_dagger_heartSeeker"
        ];

        // Swords: coarse broadsword → broadsword → longsword
        private static readonly string[] SwordByDifficulty =
        [
            "weapon_sword_coarseBroadsword",
            "weapon_sword_broadsword",
            "weapon_sword_longsword"
        ];

        // Axes: old axe → war axe → great axe
        private static readonly string[] AxeByDifficulty =
        [
            "weapon_axe_oldAxe",
            "weapon_axe_warAxe",
            "weapon_axe_greatAxe"
        ];

        // Spears: wooden spear → spear → hunting spear
        private static readonly string[] SpearByDifficulty =
        [
            "weapon_spear_woodenSpear",
            "weapon_spear_spear",
            "weapon_spear_huntingSpear"
        ];

        // Bows: simple bow → hunting bow → battle bow
        private static readonly string[] BowByDifficulty =
        [
            "weapon_bow_simpleBow",
            "weapon_bow_huntingBow",
            "weapon_bow_battleBow"
        ];

        // Shields: small wooden → round shield → bronze shield
        private static readonly string[] ShieldByDifficulty =
        [
            "weapon_shield_smallWoodenShield",
            "weapon_shield_roundShield",
            "weapon_shield_bronzeShield"
        ];

        // Slingshots: coarse sling → simple slingshot → battle slingshot
        private static readonly string[] SlingshotByDifficulty =
        [
            "weapon_sling_coarseSling",
            "weapon_sling_simpleSlingshot",
            "weapon_sling_battleSlingshot"
        ];

        // ── Armour pools (indexed by BanditDifficulty) ─────────────────────────

        private static readonly string[][] ChestByDifficulty =
        [
            // Easy — rags
            ["armor_chest_tunic_ragged_2", "armor_chest_shirt_ragged_name", "armor_chest_tunic_name"],
            // Normal — standard bandit
            ["armor_chest_bandit_T1", "armor_chest_bandit_T2", "armor_chest_leather_name"],
            // Hard — heavy bandit / strong leather
            ["armor_chest_bandit_T4", "armor_chest_leather_strong_name", "armor_chest_bandit_T2"]
        ];

        // ── Cosmetic pools (shared across all difficulties) ────────────────────

        private static readonly string[] HoodPool =
        [
            "helmet_hood_bandit_name",
            "helmet_hood_bandit_t4_name",
            "helmet_none",           // some bandits go bare-headed
            "helmet_leathercap_name",
            "helmet_barbarian_name"
        ];

        private static readonly string[] HairPool =
        [
            "hair_bald",
            "hair_balding_1",
            "hair_balding_2",
            "hair_short_spiky",
            "hair_short_spiky_2",
            "hair_shaggy",
            "hair_short_sidecut"
        ];

        private static readonly string[] BeardPool =
        [
            "beard_full_short",
            "beard_full_short_1",
            "beard_partial_big",
            "beard_partial_spiky",
            "beard_forked",
            "beard_spikey",
            "beard_mustache_big",
            "beard_whiskers",
            "beard_empty"
        ];

        // Deco slot 0 — face marks
        private static readonly string[] Deco0Pool =
        [
            "deco_scar_cheek_left",
            "deco_scar_cheek_right",
            "deco_scar_eye_left",
            "deco_scar_eye_right",
            "deco_scar_forehead_big",
            "deco_eyepatch",
            "deco_none",
            "deco_dirt_0",
            "deco_dirt_1"
        ];

        // Deco slot 1 — extra grime / scars
        private static readonly string[] Deco1Pool =
        [
            "deco1_scar_cheek_left",
            "deco1_scar_eye_right",
            "deco1_dirt_0",
            "deco1_dirt_1",
            "deco1_none"
        ];

        // ── Helpers ────────────────────────────────────────────────────────────

        private static string Pick(string[] pool, Random rng) => pool[rng.Next(pool.Length)];

        private static int DifficultyIndex(BanditDifficulty difficulty)
            => Math.Clamp((int)difficulty, 0, 2);

        /// <summary>
        /// Applies a randomised bandit-appropriate cosmetic look that scales with
        /// <paramref name="difficulty"/> (harder tiers use better armour pieces).
        /// </summary>
        private static NpcCreator WithBanditCosmetics(this NpcCreator creator, BanditDifficulty difficulty, Random rng)
        {
            int d = DifficultyIndex(difficulty);
            return creator
                .WithCosmetic(Pick(HoodPool, rng))
                .WithCosmetic(Pick(ChestByDifficulty[d], rng))
                .WithCosmetic(Pick(HairPool, rng))
                .WithCosmetic(Pick(BeardPool, rng))
                .WithCosmetic(Pick(Deco0Pool, rng))
                .WithCosmetic(Pick(Deco1Pool, rng));
        }

        // ── Public factory methods ─────────────────────────────────────────────

        /// <summary>
        /// Bandit armed with a dagger. Difficulty controls the blade quality.
        /// </summary>
        /// <param name="name">NPC display name.</param>
        /// <param name="position">World spawn position.</param>
        /// <param name="difficulty">Equipment quality tier.</param>
        public static GameObject CreateDaggerBandit(string name, Vector2 position,
            BanditDifficulty difficulty = BanditDifficulty.Normal)
        {
            var rng = new Random();
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(DaggerByDifficulty[DifficultyIndex(difficulty)])
                .IsPlayerFriendly(false)
                .Create();
        }

        /// <summary>
        /// Bandit armed with a sword. Difficulty controls the blade quality.
        /// </summary>
        /// <param name="name">NPC display name.</param>
        /// <param name="position">World spawn position.</param>
        /// <param name="difficulty">Equipment quality tier.</param>
        public static GameObject CreateSwordBandit(string name, Vector2 position,
            BanditDifficulty difficulty = BanditDifficulty.Normal)
        {
            var rng = new Random();
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(SwordByDifficulty[DifficultyIndex(difficulty)])
                .IsPlayerFriendly(false)
                .Create();
        }

        /// <summary>
        /// Bandit armed with an axe. Difficulty controls the axe quality.
        /// </summary>
        /// <param name="name">NPC display name.</param>
        /// <param name="position">World spawn position.</param>
        /// <param name="difficulty">Equipment quality tier.</param>
        public static GameObject CreateAxeBandit(string name, Vector2 position,
            BanditDifficulty difficulty = BanditDifficulty.Normal)
        {
            var rng = new Random();
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(AxeByDifficulty[DifficultyIndex(difficulty)])
                .IsPlayerFriendly(false)
                .Create();
        }

        /// <summary>
        /// Bandit armed with a sword and shield. Difficulty controls equipment quality.
        /// </summary>
        /// <param name="name">NPC display name.</param>
        /// <param name="position">World spawn position.</param>
        /// <param name="difficulty">Equipment quality tier.</param>
        public static GameObject CreateSwordShieldBandit(string name, Vector2 position,
            BanditDifficulty difficulty = BanditDifficulty.Normal)
        {
            var rng = new Random();
            int d = DifficultyIndex(difficulty);
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(SwordByDifficulty[d])
                .WithItem(ShieldByDifficulty[d])
                .IsPlayerFriendly(false)
                .Create();
        }

        /// <summary>
        /// Bandit armed with a spear. Difficulty controls the spear quality.
        /// </summary>
        /// <param name="name">NPC display name.</param>
        /// <param name="position">World spawn position.</param>
        /// <param name="difficulty">Equipment quality tier.</param>
        public static GameObject CreateSpearBandit(string name, Vector2 position,
            BanditDifficulty difficulty = BanditDifficulty.Normal)
        {
            var rng = new Random();
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(SpearByDifficulty[DifficultyIndex(difficulty)])
                .IsPlayerFriendly(false)
                .Create();
        }

        /// <summary>
        /// Bandit armed with a spear and shield. Difficulty controls equipment quality.
        /// </summary>
        /// <param name="name">NPC display name.</param>
        /// <param name="position">World spawn position.</param>
        /// <param name="difficulty">Equipment quality tier.</param>
        public static GameObject CreateSpearShieldBandit(string name, Vector2 position,
            BanditDifficulty difficulty = BanditDifficulty.Normal)
        {
            var rng = new Random();
            int d = DifficultyIndex(difficulty);
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(SpearByDifficulty[d])
                .WithItem(ShieldByDifficulty[d])
                .IsPlayerFriendly(false)
                .Create();
        }

        /// <summary>
        /// Bandit armed with a bow and quiver. Difficulty controls bow quality.
        /// </summary>
        /// <param name="name">NPC display name.</param>
        /// <param name="position">World spawn position.</param>
        /// <param name="difficulty">Equipment quality tier.</param>
        public static GameObject CreateBowBandit(string name, Vector2 position,
            BanditDifficulty difficulty = BanditDifficulty.Normal)
        {
            var rng = new Random();
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(BowByDifficulty[DifficultyIndex(difficulty)])
                .WithItem("quiver_simple_name")
                .IsPlayerFriendly(false)
                .Create();
        }

        /// <summary>
        /// Bandit carrying a spear and a slingshot (mixed melee/ranged).
        /// Difficulty controls weapon quality.
        /// </summary>
        /// <param name="name">NPC display name.</param>
        /// <param name="position">World spawn position.</param>
        /// <param name="difficulty">Equipment quality tier.</param>
        public static GameObject CreateSpearSlingshotBandit(string name, Vector2 position,
            BanditDifficulty difficulty = BanditDifficulty.Normal)
        {
            var rng = new Random();
            int d = DifficultyIndex(difficulty);
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(SpearByDifficulty[d])
                .WithItem(SlingshotByDifficulty[d])
                .IsPlayerFriendly(false)
                .Create();
        }

        /// <summary>
        /// Bandit carrying a sword and a slingshot (mixed melee/ranged).
        /// Difficulty controls weapon quality.
        /// </summary>
        /// <param name="name">NPC display name.</param>
        /// <param name="position">World spawn position.</param>
        /// <param name="difficulty">Equipment quality tier.</param>
        public static GameObject CreateSwordSlingshotBandit(string name, Vector2 position,
            BanditDifficulty difficulty = BanditDifficulty.Normal)
        {
            var rng = new Random();
            int d = DifficultyIndex(difficulty);
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(SwordByDifficulty[d])
                .WithItem(SlingshotByDifficulty[d])
                .IsPlayerFriendly(false)
                .Create();
        }

        /// <summary>
        /// Spawns a bandit with a randomly chosen loadout from all available variants.
        /// </summary>
        /// <param name="name">NPC display name.</param>
        /// <param name="position">World spawn position.</param>
        /// <param name="difficulty">Equipment quality tier applied to all weapon choices.</param>
        public static GameObject CreateRandomBandit(string name, Vector2 position,
            BanditDifficulty difficulty = BanditDifficulty.Normal)
        {
            return new Random().Next(8) switch
            {
                0 => CreateDaggerBandit(name, position, difficulty),
                1 => CreateSwordBandit(name, position, difficulty),
                2 => CreateAxeBandit(name, position, difficulty),
                3 => CreateSwordShieldBandit(name, position, difficulty),
                4 => CreateSpearBandit(name, position, difficulty),
                5 => CreateSpearShieldBandit(name, position, difficulty),
                6 => CreateBowBandit(name, position, difficulty),
                7 => CreateSpearSlingshotBandit(name, position, difficulty),
                _ => CreateSwordBandit(name, position, difficulty)
            };
        }
    }
}
