using Drova_Modding_API.GlobalFields;
using Drova_Modding_API.Systems.Spawning.Modules;
using Il2CppDrova.Utilities.LazyLoading;
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
    /// Factory for spawning bandit NPCs with varied weapon loadouts, randomized
    /// bandit-themed cosmetics, and difficulty-scaled equipment quality.
    /// </summary>
    public static class BanditCreator
    {
        // ── Weapon tables (indexed by BanditDifficulty) ────────────────────────

        // Daggers: crude knife → iron dagger → heart-seeker
        private static readonly string[] DaggerByDifficulty =
        [
            ItemReadableIds.Weapon.DaggerGroup.CoarseDaggerId,
            ItemReadableIds.Weapon.DaggerGroup.IronDaggerId,
            ItemReadableIds.Weapon.DaggerGroup.HeartSeekerId
        ];

        // Swords: coarse broadsword → broadsword → longsword
        private static readonly string[] SwordByDifficulty =
        [
            ItemReadableIds.Weapon.SwordGroup.CoarseBroadswordId,
            ItemReadableIds.Weapon.SwordGroup.BroadswordId,
            ItemReadableIds.Weapon.SwordGroup.LongswordId
        ];

        // Axes: old axe → war axe → great axe
        private static readonly string[] AxeByDifficulty =
        [
            ItemReadableIds.Weapon.AxeGroup.OldAxeId,
            ItemReadableIds.Weapon.AxeGroup.WarAxeId,
            ItemReadableIds.Weapon.AxeGroup.GreatAxeId
        ];

        // Spears: wooden spear → spear → hunting spear
        private static readonly string[] SpearByDifficulty =
        [
            ItemReadableIds.Weapon.SpearGroup.WoodenSpearId,
            ItemReadableIds.Weapon.SpearGroup.SpearId,
            ItemReadableIds.Weapon.SpearGroup.HuntingSpearId
        ];

        // Bows: simple bow → hunting bow → battle bow
        private static readonly string[] BowByDifficulty =
        [
            ItemReadableIds.Weapon.BowGroup.SimpleBowId,
            ItemReadableIds.Weapon.BowGroup.HuntingBowId,
            ItemReadableIds.Weapon.BowGroup.BattleBowId
        ];

        // Shields: small wooden → round shield → bronze shield
        private static readonly string[] ShieldByDifficulty =
        [
            ItemReadableIds.Weapon.ShieldGroup.SmallWoodenShieldId,
            ItemReadableIds.Weapon.ShieldGroup.RoundShieldId,
            ItemReadableIds.Weapon.ShieldGroup.BronzeShieldId
        ];

        // Slingshots: coarse sling → simple slingshot → battle slingshot
        private static readonly string[] SlingshotByDifficulty =
        [
            ItemReadableIds.Weapon.SlingGroup.CoarseSlingId,
            ItemReadableIds.Weapon.SlingGroup.SimpleSlingshotId,
            ItemReadableIds.Weapon.SlingGroup.BattleSlingshotId
        ];

        // Talent progression tracks (Easy => first, Normal => first two, Hard => first three)
        private static readonly string[] DaggerTalentsByDifficulty =
        [
            TalentReadableIds.Daggers.Talent00,
            TalentReadableIds.Daggers.Talent01,
            TalentReadableIds.Daggers.Talent02
        ];

        private static readonly string[] DaggerHardBonusTalents =
        [
            TalentReadableIds.Daggers.SurpriseAttack,
            TalentReadableIds.Daggers.GoingWithTheFlow,
            TalentReadableIds.Daggers.EfficientStrikes
        ];

        private static readonly string[] SwordTalentsByDifficulty =
        [
            TalentReadableIds.Sword.Talent00,
            TalentReadableIds.Sword.Talent01,
            TalentReadableIds.Sword.Talent02
        ];

        private static readonly string[] SwordHardBonusTalents =
        [
            TalentReadableIds.Sword.Chancer,
            TalentReadableIds.Sword.Accuracy,
            TalentReadableIds.Sword.EmpoweredBlows
        ];

        private static readonly string[] AxeTalentsByDifficulty =
        [
            TalentReadableIds.Axe.Talent00,
            TalentReadableIds.Axe.Talent01,
            TalentReadableIds.Axe.Talent02
        ];

        private static readonly string[] AxeHardBonusTalents =
        [
            TalentReadableIds.Axe.MightySwing,
            TalentReadableIds.Axe.BattleFrenzy,
            TalentReadableIds.Axe.FinalGrace
        ];

        private static readonly string[] SpearTalentsByDifficulty =
        [
            TalentReadableIds.Spear.Talent00,
            TalentReadableIds.Spear.Talent01,
            TalentReadableIds.Spear.Talent02
        ];

        private static readonly string[] SpearHardBonusTalents =
        [
            TalentReadableIds.Spear.PeakPerformance,
            TalentReadableIds.Spear.FocusedStrikes
        ];

        private static readonly string[] BowTalentsByDifficulty =
        [
            TalentReadableIds.Bow.Talent00,
            TalentReadableIds.Bow.Talent01,
            TalentReadableIds.Bow.Talent02
        ];

        private static readonly string[] BowHardBonusTalents =
        [
            TalentReadableIds.Bow.Quickdraw,
            TalentReadableIds.Bow.Bullseye
        ];

        private static readonly string[] ShieldTalentsByDifficulty =
        [
            TalentReadableIds.Shield.Talent00,
            TalentReadableIds.Shield.Talent01,
            TalentReadableIds.Shield.Talent02
        ];

        private static readonly string[] ShieldHardBonusTalents =
        [
            TalentReadableIds.Shield.BalanceBreaker,
            TalentReadableIds.Shield.IsThatAllYouGot
        ];

        private static readonly string[] SlingTalentsByDifficulty =
        [
            TalentReadableIds.Sling.Talent00,
            TalentReadableIds.Sling.Talent01,
            TalentReadableIds.Sling.Talent02
        ];

        private static readonly string[] SlingHardBonusTalents =
        [
            TalentReadableIds.Sling.DoubleTheLoad,
            TalentReadableIds.Sling.Concussion
        ];

        // ── Armour pools (indexed by BanditDifficulty) ─────────────────────────

        private static readonly string[][] ChestByDifficulty =
        [
            // Easy — rags
            [ItemReadableIds.Armor.ChestGroup.TunicRagged2Id, ItemReadableIds.Armor.ChestGroup.ShirtRaggedNameId, ItemReadableIds.Armor.ChestGroup.TunicNameId],
            // Normal — standard bandit
            [ItemReadableIds.Armor.ChestGroup.BanditT1Id, ItemReadableIds.Armor.ChestGroup.BanditT2Id, ItemReadableIds.Armor.ChestGroup.LeatherNameId],
            // Hard — heavy bandit / strong leather
            [ItemReadableIds.Armor.ChestGroup.BanditT4Id, ItemReadableIds.Armor.ChestGroup.LeatherStrongNameId, ItemReadableIds.Armor.ChestGroup.BanditT2Id]
        ];

        // ── Cosmetic pools (shared across all difficulties) ────────────────────

        private static readonly string[] HoodPool =
        [
            ItemReadableIds.Helmet.HoodGroup.BanditNameId,
            ItemReadableIds.Helmet.HoodGroup.BanditT4NameId,
            ItemReadableIds.Helmet.GeneralGroup.NoneId,           // some bandits go bare-headed
            ItemReadableIds.Helmet.LeathercapGroup.NameId,
            ItemReadableIds.Helmet.BarbarianGroup.NameId
        ];

        private static readonly string[] HairPool =
        [
            ItemReadableIds.Hair.GeneralGroup.BaldId,
            ItemReadableIds.Hair.BaldingGroup.N1Id,
            ItemReadableIds.Hair.BaldingGroup.N2Id,
            ItemReadableIds.Hair.ShortGroup.SpikyId,
            ItemReadableIds.Hair.ShortGroup.Spiky2Id,
            ItemReadableIds.Hair.GeneralGroup.ShaggyId,
            ItemReadableIds.Hair.ShortGroup.SidecutId
        ];

        private static readonly string[] BeardPool =
        [
            ItemReadableIds.Beard.FullGroup.ShortId,
            ItemReadableIds.Beard.FullGroup.Short1Id,
            ItemReadableIds.Beard.PartialGroup.BigId,
            ItemReadableIds.Beard.PartialGroup.SpikyId,
            ItemReadableIds.Beard.GeneralGroup.ForkedId,
            ItemReadableIds.Beard.GeneralGroup.SpikeyId,
            ItemReadableIds.Beard.MustacheGroup.BigId,
            ItemReadableIds.Beard.GeneralGroup.WhiskersId,
            ItemReadableIds.Beard.GeneralGroup.EmptyId
        ];

        // Deco slot 0 — face marks
        private static readonly string[] Deco0Pool =
        [
            ItemReadableIds.Deco.ScarGroup.CheekLeftId,
            ItemReadableIds.Deco.ScarGroup.CheekRightId,
            ItemReadableIds.Deco.ScarGroup.EyeLeftId,
            ItemReadableIds.Deco.ScarGroup.EyeRightId,
            ItemReadableIds.Deco.ScarGroup.ForeheadBigId,
            ItemReadableIds.Deco.GeneralGroup.EyepatchId,
            ItemReadableIds.Deco.GeneralGroup.NoneId,
            ItemReadableIds.Deco.DirtGroup.N0Id,
            ItemReadableIds.Deco.DirtGroup.N1Id
        ];

        // Deco slot 1 — extra grime / scars
        private static readonly string[] Deco1Pool =
        [
            ItemReadableIds.Deco1.ScarGroup.CheekLeftId,
            ItemReadableIds.Deco1.ScarGroup.EyeRightId,
            ItemReadableIds.Deco1.DirtGroup.N0Id,
            ItemReadableIds.Deco1.DirtGroup.N1Id,
            ItemReadableIds.Deco1.GeneralGroup.NoneId
        ];

        // ── Helpers ────────────────────────────────────────────────────────────

        private static string Pick(string[] pool, Random rng) => pool[rng.Next(pool.Length)];

        private static int DifficultyIndex(BanditDifficulty difficulty)
            => Math.Clamp((int)difficulty, 0, 2);

        private static int RollHealthForDifficulty(BanditDifficulty difficulty, Random rng)
        {
            return difficulty switch
            {
                BanditDifficulty.Easy => rng.Next(200, 401),
                BanditDifficulty.Normal => rng.Next(700, 901),
                BanditDifficulty.Hard => rng.Next(1700, 2501),
                _ => rng.Next(700, 901)
            };
        }

        private static NpcCreator WithBanditHealth(this NpcCreator creator, BanditDifficulty difficulty, Random rng)
            => creator.WithModule(new HealthPresetModule().With(RollHealthForDifficulty(difficulty, rng)));

        private static int XpForDifficulty(BanditDifficulty difficulty)
        {
            return difficulty switch
            {
                BanditDifficulty.Easy => 60,
                BanditDifficulty.Normal => 120,
                BanditDifficulty.Hard => 180,
                _ => 120
            };
        }

        private static NpcCreator WithBanditXp(this NpcCreator creator, BanditDifficulty difficulty)
            => creator.WithModule(new CustomStatsModule().WithExpOnDead(XpForDifficulty(difficulty)));

        private static NpcCreator WithScaledTalents(this NpcCreator creator, BanditDifficulty difficulty, string[] talentTrack)
        {
            if (talentTrack.Length == 0) return creator;

            var module = new TalentPresetModule();
            var maxIndex = Mathf.Clamp(DifficultyIndex(difficulty), 0, talentTrack.Length - 1);
            for (var i = 0; i <= maxIndex; i++)
            {
                module.With(talentTrack[i]);
            }

            return creator.WithModule(module);
        }

        private static NpcCreator WithHardOnlyTalents(this NpcCreator creator, BanditDifficulty difficulty, params string[] talentIds)
        {
            if (difficulty != BanditDifficulty.Hard || talentIds.Length == 0) return creator;

            var module = new TalentPresetModule();
            foreach (var talentId in talentIds)
            {
                module.With(talentId);
            }

            return creator.WithModule(module);
        }

        private static string[] MergeTalents(params string[][] talentGroups)
        {
            var merged = new List<string>();
            foreach (var group in talentGroups)
            {
                if (group.Length == 0) continue;
                merged.AddRange(group);
            }

            return merged.ToArray();
        }

        private static NpcCreator WithBanditCombatModules(this NpcCreator creator, BanditDifficulty difficulty, string[]? hardBonusTalents = null, params string[][] talentTracks)
        {
            foreach (var talentTrack in talentTracks)
            {
                creator = creator.WithScaledTalents(difficulty, talentTrack);
            }

            if (hardBonusTalents != null)
            {
                creator = creator.WithHardOnlyTalents(difficulty, hardBonusTalents);
            }

            // All bandits use the default spell preset template as baseline.
            return creator.WithModule(new FlowPresetModule().UseDefaultPresetTemplate());
        }

        /// <summary>
        /// Applies a randomised bandit-appropriate cosmetic look that scales with
        /// <paramref name="difficulty"/> (harder tiers use better armour pieces).
        /// </summary>
        private static NpcCreator WithBanditCosmetics(this NpcCreator creator, BanditDifficulty difficulty, Random rng)
        {
            int d = DifficultyIndex(difficulty);
            return creator
                .WithCosmetic(Pick(HoodPool, rng))
                .WithCosmetic(Pick(HairPool, rng))
                .WithCosmetic(Pick(BeardPool, rng))
                .WithCosmetic(Pick(Deco0Pool, rng))
                .WithCosmetic(Pick(Deco1Pool, rng))
                .WithItem(Pick(ChestByDifficulty[d], rng))
                ;
        }

        private static NpcCreator WithBanditRoutine(this NpcCreator creator)
        {
            return creator.WithModule(new RoutineModule());
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
            var rng = Random.Shared;
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(DaggerByDifficulty[DifficultyIndex(difficulty)])
                .WithBanditCombatModules(difficulty, DaggerHardBonusTalents, DaggerTalentsByDifficulty)
                .WithBanditHealth(difficulty, rng)
                .WithBanditXp(difficulty)
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
            var rng = Random.Shared;
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(SwordByDifficulty[DifficultyIndex(difficulty)])
                .WithBanditCombatModules(difficulty, SwordHardBonusTalents, SwordTalentsByDifficulty)
                .WithBanditHealth(difficulty, rng)
                .WithBanditXp(difficulty)
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
            var rng = Random.Shared;
            return new NpcCreator(name, position)
                .WithBanditRoutine()
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(AxeByDifficulty[DifficultyIndex(difficulty)])
                .WithBanditCombatModules(difficulty, AxeHardBonusTalents, AxeTalentsByDifficulty)
                .WithBanditHealth(difficulty, rng)
                .WithBanditXp(difficulty)
                .IsPlayerFriendly(false)
                .Create();
        }

        /// <summary>
        /// Lazy bandit armed with an axe. Uses the same template modules as <see cref="CreateAxeBandit"/>
        /// but applies them on each lazy actor load.
        /// </summary>
        /// <param name="name">NPC display name.</param>
        /// <param name="position">World spawn position.</param>
        /// <param name="difficulty">Equipment quality tier.</param>
        /// <param name="saveToLazyActorStore">If true, saves lazy actor metadata for restore.</param>
        public static LazyActor CreateAxeBanditLazy(string name, Vector2 position,
            BanditDifficulty difficulty = BanditDifficulty.Normal,
            bool saveToLazyActorStore = false)
        {
            var rng = Random.Shared;
            return new NpcCreator(name, position)
                .WithBanditRoutine()
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(AxeByDifficulty[DifficultyIndex(difficulty)])
                .WithBanditCombatModules(difficulty, AxeHardBonusTalents, AxeTalentsByDifficulty)
                .WithBanditHealth(difficulty, rng)
                .WithBanditXp(difficulty)
                .IsPlayerFriendly(false)
                .CreateLazy(saveToLazyActorStore);
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
            var rng = Random.Shared;
            int d = DifficultyIndex(difficulty);
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(SwordByDifficulty[d])
                .WithItem(ShieldByDifficulty[d])
                .WithBanditCombatModules(difficulty, MergeTalents(SwordHardBonusTalents, ShieldHardBonusTalents), SwordTalentsByDifficulty, ShieldTalentsByDifficulty)
                .WithBanditHealth(difficulty, rng)
                .WithBanditXp(difficulty)
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
            var rng = Random.Shared;
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(SpearByDifficulty[DifficultyIndex(difficulty)])
                .WithBanditCombatModules(difficulty, SpearHardBonusTalents, SpearTalentsByDifficulty)
                .WithBanditHealth(difficulty, rng)
                .WithBanditXp(difficulty)
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
            var rng = Random.Shared;
            int d = DifficultyIndex(difficulty);
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(SpearByDifficulty[d])
                .WithItem(ShieldByDifficulty[d])
                .WithBanditCombatModules(difficulty, MergeTalents(SpearHardBonusTalents, ShieldHardBonusTalents), SpearTalentsByDifficulty, ShieldTalentsByDifficulty)
                .WithBanditHealth(difficulty, rng)
                .WithBanditXp(difficulty)
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
            var rng = Random.Shared;
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(BowByDifficulty[DifficultyIndex(difficulty)])
                .WithItem(ItemReadableIds.Quiver.SimpleGroup.NameId)
                .WithBanditCombatModules(difficulty, BowHardBonusTalents, BowTalentsByDifficulty)
                .WithBanditHealth(difficulty, rng)
                .WithBanditXp(difficulty)
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
            var rng = Random.Shared;
            int d = DifficultyIndex(difficulty);
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(SpearByDifficulty[d])
                .WithItem(SlingshotByDifficulty[d])
                .WithBanditCombatModules(difficulty, MergeTalents(SpearHardBonusTalents, SlingHardBonusTalents), SpearTalentsByDifficulty, SlingTalentsByDifficulty)
                .WithBanditHealth(difficulty, rng)
                .WithBanditXp(difficulty)
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
            var rng = Random.Shared;
            int d = DifficultyIndex(difficulty);
            return new NpcCreator(name, position)
                .WithBanditCosmetics(difficulty, rng)
                .WithItem(SwordByDifficulty[d])
                .WithItem(SlingshotByDifficulty[d])
                .WithBanditCombatModules(difficulty, MergeTalents(SwordHardBonusTalents, SlingHardBonusTalents), SwordTalentsByDifficulty, SlingTalentsByDifficulty)
                .WithBanditHealth(difficulty, rng)
                .WithBanditXp(difficulty)
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
            return Random.Shared.Next(8) switch
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
