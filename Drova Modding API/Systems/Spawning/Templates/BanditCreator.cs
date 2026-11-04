using System.Diagnostics.CodeAnalysis;
using Drova_Modding_API.Access;
using Drova_Modding_API.GlobalFields;
using Drova_Modding_API.Systems.Spawning.Modules;
using Il2CppDrova;
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
        /// <summary>Coarse / improvised weapons, ragged armor.</summary>
        Easy = 0,

        /// <summary>Standard bandit weapons and T1/T2 bandit armor.</summary>
        Normal = 1,

        /// <summary>Quality weapons, T4 armor, and heavier shields.</summary>
        Hard = 2
    }

    /// <summary>
    /// Factory for spawning bandit NPCs with varied weapon loadouts, randomized
    /// bandit-themed cosmetics, and difficulty-scaled equipment quality.
    /// </summary>
    public static class BanditCreator
    {
        private static readonly string[] DaggerByDifficulty =
        [
            ItemReadableIds.Weapon.DaggerGroup.CoarseDaggerId,
            ItemReadableIds.Weapon.DaggerGroup.IronDaggerId,
            ItemReadableIds.Weapon.DaggerGroup.HeartSeekerId
        ];

        private static readonly string[] SwordByDifficulty =
        [
            ItemReadableIds.Weapon.SwordGroup.CoarseBroadswordId,
            ItemReadableIds.Weapon.SwordGroup.BroadswordId,
            ItemReadableIds.Weapon.SwordGroup.LongswordId
        ];

        private static readonly string[] AxeByDifficulty =
        [
            ItemReadableIds.Weapon.AxeGroup.OldAxeId,
            ItemReadableIds.Weapon.AxeGroup.WarAxeId,
            ItemReadableIds.Weapon.AxeGroup.GreatAxeId
        ];

        private static readonly string[] SpearByDifficulty =
        [
            ItemReadableIds.Weapon.SpearGroup.WoodenSpearId,
            ItemReadableIds.Weapon.SpearGroup.SpearId,
            ItemReadableIds.Weapon.SpearGroup.PartisanId
        ];

        private static readonly string[] BowByDifficulty =
        [
            ItemReadableIds.Weapon.BowGroup.SimpleBowId,
            ItemReadableIds.Weapon.BowGroup.HuntingBowId,
            ItemReadableIds.Weapon.BowGroup.BattleBowId
        ];

        private static readonly string[] ShieldByDifficulty =
        [
            ItemReadableIds.Weapon.ShieldGroup.SmallWoodenShieldId,
            ItemReadableIds.Weapon.ShieldGroup.RoundShieldId,
            ItemReadableIds.Weapon.ShieldGroup.TheWallId
        ];

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

        private static readonly string[][] ChestByDifficulty =
        [
            // Easy — rags
            [ItemReadableIds.Armor.ChestGroup.TunicRagged2Id, ItemReadableIds.Armor.ChestGroup.ShirtRaggedNameId, ItemReadableIds.Armor.ChestGroup.TunicNameId],
            // Normal — standard bandit
            [ItemReadableIds.Armor.ChestGroup.BanditT1Id, ItemReadableIds.Armor.ChestGroup.BanditT2Id, ItemReadableIds.Armor.ChestGroup.LeatherNameId, ItemReadableIds.Armor.ChestGroup.LeatherStrongNameId],
            // Hard — heavy bandit / strong leather
            [ItemReadableIds.Armor.ChestGroup.BanditT4Id]
        ];

        private static readonly string[] HoodPool =
        [
            ItemReadableIds.Helmet.HoodGroup.BanditNameId,
            ItemReadableIds.Helmet.HoodGroup.BanditT4NameId,
            ItemReadableIds.Helmet.GeneralGroup.NoneId,
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

        private static readonly string[] Deco1Pool =
        [
            ItemReadableIds.Deco1.ScarGroup.CheekLeftId,
            ItemReadableIds.Deco1.ScarGroup.EyeRightId,
            ItemReadableIds.Deco1.DirtGroup.N0Id,
            ItemReadableIds.Deco1.DirtGroup.N1Id,
            ItemReadableIds.Deco1.GeneralGroup.NoneId
        ];

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

            return creator.WithModule(new FlowPresetModule().UseDefaultPresetTemplate());
        }

        /// <summary>
        /// Applies a randomized bandit-appropriate cosmetic look that scales with
        /// <paramref name="difficulty"/> (harder tiers use better armor pieces).
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

        private static NpcCreator WithBanditRoutine(this NpcCreator creator, Vector2 position)
            => creator.WithModule(new RoutineModule().With(position));

        private static NpcCreator WithCustomEntityInfo(this NpcCreator creator)
        {
            var entityInfo = EntityInfo.CreateUndefined();
            return creator.WithLazyEntityInfo(entityInfo);
        }

        private static NpcCreator CreateLazyBanditBase(string name, Vector2 position, BanditDifficulty difficulty, Random rng)
            => new NpcCreator(name, position)
                .WithBanditRoutine(position)
                .WithCustomEntityInfo()
                .WithBanditCosmetics(difficulty, rng);

        /// <summary>The prefix of a bandit's spawn recipe, which <see cref="ActorSpawnAccess.TrySpawnFromAsset"/> rebuilds on another machine.</summary>
        internal const string RecipePrefix = "bandit:";

        private enum Loadout
        {
            Dagger,
            Sword,
            Axe,
            SwordShield,
            Spear,
            SpearShield,
            Bow,
            SpearSlingshot,
            SwordSlingshot,
        }

        /// <summary>
        /// Lazy bandit armed with a dagger. Difficulty controls the blade quality.
        /// </summary>
        public static LazyActor CreateDaggerBanditLazy(string name, Vector2 position, BanditDifficulty difficulty = BanditDifficulty.Normal, bool saveToLazyActorStore = false)
            => SpawnLazy(Loadout.Dagger, name, position, difficulty, saveToLazyActorStore);

        /// <summary>
        /// Lazy bandit armed with a sword. Difficulty controls the blade quality.
        /// </summary>
        public static LazyActor CreateSwordBanditLazy(string name, Vector2 position, BanditDifficulty difficulty = BanditDifficulty.Normal, bool saveToLazyActorStore = false)
            => SpawnLazy(Loadout.Sword, name, position, difficulty, saveToLazyActorStore);

        /// <summary>
        /// Lazy bandit armed with an axe. Difficulty controls the axe quality.
        /// </summary>
        public static LazyActor CreateAxeBanditLazy(string name, Vector2 position, BanditDifficulty difficulty = BanditDifficulty.Normal, bool saveToLazyActorStore = false)
            => SpawnLazy(Loadout.Axe, name, position, difficulty, saveToLazyActorStore);

        /// <summary>
        /// Lazy bandit armed with a sword and shield. Difficulty controls equipment quality.
        /// </summary>
        public static LazyActor CreateSwordShieldBanditLazy(string name, Vector2 position, BanditDifficulty difficulty = BanditDifficulty.Normal, bool saveToLazyActorStore = false)
            => SpawnLazy(Loadout.SwordShield, name, position, difficulty, saveToLazyActorStore);

        /// <summary>
        /// Lazy bandit armed with a spear. Difficulty controls the spear quality.
        /// </summary>
        public static LazyActor CreateSpearBanditLazy(string name, Vector2 position, BanditDifficulty difficulty = BanditDifficulty.Normal, bool saveToLazyActorStore = false)
            => SpawnLazy(Loadout.Spear, name, position, difficulty, saveToLazyActorStore);

        /// <summary>
        /// Lazy bandit armed with a spear and shield. Difficulty controls equipment quality.
        /// </summary>
        public static LazyActor CreateSpearShieldBanditLazy(string name, Vector2 position, BanditDifficulty difficulty = BanditDifficulty.Normal, bool saveToLazyActorStore = false)
            => SpawnLazy(Loadout.SpearShield, name, position, difficulty, saveToLazyActorStore);

        /// <summary>
        /// Lazy bandit armed with a bow and quiver. Difficulty controls bow quality.
        /// </summary>
        public static LazyActor CreateBowBanditLazy(string name, Vector2 position, BanditDifficulty difficulty = BanditDifficulty.Normal, bool saveToLazyActorStore = false)
            => SpawnLazy(Loadout.Bow, name, position, difficulty, saveToLazyActorStore);

        /// <summary>
        /// Lazy bandit carrying a spear and a slingshot (mixed melee/ranged).
        /// Difficulty controls weapon quality.
        /// </summary>
        public static LazyActor CreateSpearSlingshotBanditLazy(string name, Vector2 position, BanditDifficulty difficulty = BanditDifficulty.Normal, bool saveToLazyActorStore = false)
            => SpawnLazy(Loadout.SpearSlingshot, name, position, difficulty, saveToLazyActorStore);

        /// <summary>
        /// Lazy bandit carrying a sword and a slingshot (mixed melee/ranged).
        /// Difficulty controls weapon quality.
        /// </summary>
        public static LazyActor CreateSwordSlingshotBanditLazy(string name, Vector2 position, BanditDifficulty difficulty = BanditDifficulty.Normal, bool saveToLazyActorStore = false)
            => SpawnLazy(Loadout.SwordSlingshot, name, position, difficulty, saveToLazyActorStore);

        /// <summary>
        /// Spawns a lazy bandit with a randomly chosen loadout from all available variants.
        /// </summary>
        public static LazyActor CreateRandomBanditLazy(
            string name,
            Vector2 position,
            BanditDifficulty difficulty = BanditDifficulty.Normal,
            bool saveToLazyActorStore = false)
        {
            return SpawnLazy((Loadout)Random.Shared.Next(Enum.GetValues<Loadout>().Length), name, position, difficulty, saveToLazyActorStore);
        }

        /// <summary>
        /// Rolls the bandit from a seed and records it by a recipe of loadout, difficulty and seed, so a co-op
        /// teammate builds the same bandit instead of the bare human template.
        /// </summary>
        private static LazyActor SpawnLazy(Loadout loadout, string name, Vector2 position, BanditDifficulty difficulty, bool saveToLazyActorStore)
        {
            int seed = Random.Shared.Next();
            LazyActor bandit = Build(loadout, name, position, difficulty, seed).CreateLazy(saveToLazyActorStore);
            ActorSpawnAccess.RecordLazySpawn(bandit, $"{RecipePrefix}{(int)loadout}:{(int)difficulty}:{seed}");
            return bandit;
        }

        /// <summary>Builds the bandit a recipe from <see cref="SpawnLazy"/> describes, as a plain actor.</summary>
        internal static bool TryBuild(string recipe, Vector2 position, [NotNullWhen(true)] out Actor? actor)
        {
            actor = null;
            string[] parts = recipe.Substring(RecipePrefix.Length).Split(':');
            if (parts.Length != 3 || !int.TryParse(parts[0], out int loadout) || !Enum.IsDefined((Loadout)loadout)
                || !int.TryParse(parts[1], out int difficulty) || !int.TryParse(parts[2], out int seed))
            {
                return false;
            }

            actor = Build((Loadout)loadout, "Bandit", position, (BanditDifficulty)difficulty, seed).Create().GetComponent<Actor>();
            return actor != null;
        }

        private static NpcCreator Build(Loadout loadout, string name, Vector2 position, BanditDifficulty difficulty, int seed)
        {
            Random rng = new(seed);
            int d = DifficultyIndex(difficulty);
            NpcCreator creator = CreateLazyBanditBase(name, position, difficulty, rng);
            creator = loadout switch
            {
                Loadout.Dagger => creator
                    .WithItem(DaggerByDifficulty[d])
                    .WithBanditCombatModules(difficulty, DaggerHardBonusTalents, DaggerTalentsByDifficulty),
                Loadout.Sword => creator
                    .WithItem(SwordByDifficulty[d])
                    .WithBanditCombatModules(difficulty, SwordHardBonusTalents, SwordTalentsByDifficulty),
                Loadout.Axe => creator
                    .WithItem(AxeByDifficulty[d])
                    .WithBanditCombatModules(difficulty, AxeHardBonusTalents, AxeTalentsByDifficulty),
                Loadout.SwordShield => creator
                    .WithItem(SwordByDifficulty[d])
                    .WithItem(ShieldByDifficulty[d])
                    .WithBanditCombatModules(difficulty, MergeTalents(SwordHardBonusTalents, ShieldHardBonusTalents), SwordTalentsByDifficulty, ShieldTalentsByDifficulty),
                Loadout.Spear => creator
                    .WithItem(SpearByDifficulty[d])
                    .WithBanditCombatModules(difficulty, SpearHardBonusTalents, SpearTalentsByDifficulty),
                Loadout.SpearShield => creator
                    .WithItem(SpearByDifficulty[d])
                    .WithItem(ShieldByDifficulty[d])
                    .WithBanditCombatModules(difficulty, MergeTalents(SpearHardBonusTalents, ShieldHardBonusTalents), SpearTalentsByDifficulty, ShieldTalentsByDifficulty),
                Loadout.Bow => creator
                    .WithItem(BowByDifficulty[d])
                    .WithItem(ItemReadableIds.Quiver.SimpleGroup.NameId)
                    .WithBanditCombatModules(difficulty, BowHardBonusTalents, BowTalentsByDifficulty),
                Loadout.SpearSlingshot => creator
                    .WithItem(SpearByDifficulty[d])
                    .WithItem(SlingshotByDifficulty[d])
                    .WithBanditCombatModules(difficulty, MergeTalents(SpearHardBonusTalents, SlingHardBonusTalents), SpearTalentsByDifficulty, SlingTalentsByDifficulty),
                _ => creator
                    .WithItem(SwordByDifficulty[d])
                    .WithItem(SlingshotByDifficulty[d])
                    .WithBanditCombatModules(difficulty, MergeTalents(SwordHardBonusTalents, SlingHardBonusTalents), SwordTalentsByDifficulty, SlingTalentsByDifficulty),
            };

            return creator
                .WithBanditHealth(difficulty, rng)
                .WithBanditXp(difficulty)
                .IsPlayerFriendly(false);
        }
    }
}
