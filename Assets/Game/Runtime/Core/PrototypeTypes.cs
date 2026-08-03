using System;
using UnityEngine;

namespace SimpleGame
{
    public enum EnemyArchetype
    {
        Melee,
        Ranged,
        Shield,
        Boss
    }

    public enum AttackSide
    {
        Front,
        Rear
    }

    public enum GameRunState
    {
        Playing,
        Paused,
        CardSelection,
        GameOver,
        Clear,
        DifficultySelection
    }

    public enum GameDifficulty
    {
        Easy,
        Normal
    }

    public enum PlayerAttackReaction
    {
        None,
        Recoil
    }

    public enum CombatFeedbackLevel
    {
        None,
        NormalHit,
        DefeatingHit,
        FrontRecoil,
        CriticalHit
    }

    public enum EnemyThreatLevel
    {
        OneHit,
        ThreeFrontOneRear,
        Dangerous
    }

    public enum LevelUpCardEffectType
    {
        StatModifier,
        UpgradeRank,
        Fusion
    }

    public enum PlayerStatId
    {
        CriticalChance,
        MaxHp,
        MoveSpeed,
        AttackRange,
        Piercing,
        Sever,
        HitHeal,
        StaticCharge,
        MovingSlash,
        ShieldBypass,
        FlyingSwordCount,
        FlyingSwordHitCount,
        FilthThrow,
        FlyingSwordPiercingFusion,
        FlyingSwordStaticFusion,
        StaticFilthFusion
    }

    public enum StatOperation
    {
        Add
    }

    public readonly struct CombatResult
    {
        public CombatResult(
            int damage,
            int targetMaxHealth,
            PlayerAttackReaction playerReaction)
        {
            Damage = damage;
            TargetMaxHealth = targetMaxHealth;
            PlayerReaction = playerReaction;
        }

        public int Damage { get; }
        public int TargetMaxHealth { get; }
        public PlayerAttackReaction PlayerReaction { get; }
    }

    public readonly struct PlayerAttackExecution
    {
        public PlayerAttackExecution(
            CombatResult primaryResult,
            bool primaryDamageApplied,
            bool anyDamageApplied,
            bool critical,
            AttackSide primarySide,
            bool piercingAllowed)
        {
            PrimaryResult = primaryResult;
            PrimaryDamageApplied = primaryDamageApplied;
            AnyDamageApplied = anyDamageApplied;
            Critical = critical;
            PrimarySide = primarySide;
            PiercingAllowed = piercingAllowed;
        }

        public CombatResult PrimaryResult { get; }
        public bool PrimaryDamageApplied { get; }
        public bool AnyDamageApplied { get; }
        public bool Critical { get; }
        public AttackSide PrimarySide { get; }
        public bool PiercingAllowed { get; }
    }

    public static class ProgressionCurve
    {
        public const float LinearLevelWeight = 0.18f;
        public const float EnemyAttackGrowthRate = 0.45f;
        public const float EnemyMoveSpeedGrowthPerLevel = 0.0125f;
        public const float MaximumEnemyMoveSpeedMultiplier = 1.6f;
        public const int MaximumEnemyAttackDamage = 8;
        public const int MaximumPlayerLevel = 50;
        public const float ExperienceQuadraticWeight = 0.025f;

        public static float CalculateAdditiveStat(
            float baseValue,
            float growthRate,
            int level)
        {
            float levelOffset = Mathf.Max(0, level - 1);
            float curve =
                Mathf.Sqrt(levelOffset) +
                LinearLevelWeight * levelOffset;
            return Mathf.Max(0f, baseValue) +
                Mathf.Max(0f, growthRate) * curve;
        }

        public static int RoundPositiveStat(float value)
        {
            if (value <= 0f)
            {
                return 0;
            }

            return Mathf.Max(1, Mathf.FloorToInt(value + 0.5f));
        }

        public static int CalculateEnemyAttackDamage(
            int baseDamage,
            int level)
        {
            if (baseDamage <= 0)
            {
                return 0;
            }

            float levelOffset = Mathf.Max(0, level - 1);
            int scaledDamage = Mathf.CeilToInt(
                baseDamage +
                EnemyAttackGrowthRate * Mathf.Sqrt(levelOffset));
            return Mathf.Clamp(
                scaledDamage,
                0,
                MaximumEnemyAttackDamage);
        }

        public static float CalculateEnemyMoveSpeed(
            float baseMoveSpeed,
            int level)
        {
            int levelOffset = Mathf.Max(0, level - 1);
            float multiplier = Mathf.Min(
                MaximumEnemyMoveSpeedMultiplier,
                1f + EnemyMoveSpeedGrowthPerLevel * levelOffset);
            return Mathf.Max(0f, baseMoveSpeed) * multiplier;
        }

        public static float CalculateWaveHealthMultiplier(
            int waveNumber)
        {
            int safeWaveNumber = Mathf.Max(1, waveNumber);
            return safeWaveNumber switch
            {
                >= 56 => 9.5f,
                >= 48 => 7.5f,
                >= 40 => 6f,
                >= 32 => 4.8f,
                >= 24 => 3.8f,
                >= 20 => 3f,
                >= 16 => 2.4f,
                >= 12 => 1.9f,
                >= 8 => 1.5f,
                >= 5 => 1.2f,
                _ => 1f
            };
        }

        public static int CalculateRequiredExperience(int level)
        {
            if (level >= MaximumPlayerLevel)
            {
                return 0;
            }

            int safeLevel = Mathf.Max(1, level);
            int levelOffset = safeLevel - 1;
            return 6 +
                2 * safeLevel +
                Mathf.FloorToInt(
                    ExperienceQuadraticWeight *
                    levelOffset *
                    levelOffset);
        }
    }

    [Serializable]
    public sealed class EnemyDefinition
    {
        [SerializeField] private string enemyId;
        [SerializeField] private EnemyArchetype archetype;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float attackRange;
        [SerializeField] private int attackDamage;
        [SerializeField] private float attackWindup;
        [SerializeField] private float attackActiveDuration;
        [SerializeField] private float attackCooldown;
        [SerializeField] private float attackAreaRadius;
        [SerializeField] private float approachRange;
        [SerializeField] private float facingTurnDelay;
        [SerializeField] private float postAttackFacingLock;
        [SerializeField] private int killExperience;
        [SerializeField] private int score;
        [SerializeField] private float baseMaxHp;
        [SerializeField] private float hpGrowthRate;
        [SerializeField] private int levelDifficultyOffset;
        [SerializeField] private string combatProfileId;
        [SerializeField] private bool showHpBar;

        public EnemyDefinition(
            string enemyId,
            EnemyArchetype archetype,
            float moveSpeed,
            float attackRange,
            int attackDamage,
            float attackWindup,
            float attackActiveDuration,
            float attackCooldown,
            float attackAreaRadius,
            float approachRange,
            float facingTurnDelay,
            float postAttackFacingLock,
            int killExperience,
            int score,
            float baseMaxHp,
            float hpGrowthRate,
            int levelDifficultyOffset,
            string combatProfileId,
            bool showHpBar)
        {
            this.enemyId = enemyId;
            this.archetype = archetype;
            this.moveSpeed = moveSpeed;
            this.attackRange = attackRange;
            this.attackDamage = attackDamage;
            this.attackWindup = attackWindup;
            this.attackActiveDuration = attackActiveDuration;
            this.attackCooldown = attackCooldown;
            this.attackAreaRadius = attackAreaRadius;
            this.approachRange = approachRange;
            this.facingTurnDelay = facingTurnDelay;
            this.postAttackFacingLock = postAttackFacingLock;
            this.killExperience = killExperience;
            this.score = score;
            this.baseMaxHp = baseMaxHp;
            this.hpGrowthRate = hpGrowthRate;
            this.levelDifficultyOffset = levelDifficultyOffset;
            this.combatProfileId = combatProfileId;
            this.showHpBar = showHpBar;
        }

        public string EnemyId => enemyId;
        public EnemyArchetype Archetype => archetype;
        public float MoveSpeed => moveSpeed;
        public float AttackRange => attackRange;
        public int AttackDamage => attackDamage;
        public float AttackWindup => attackWindup;
        public float AttackActiveDuration => attackActiveDuration;
        public float AttackCooldown => attackCooldown;
        public float AttackAreaRadius => attackAreaRadius;
        public float ApproachRange => approachRange;
        public float FacingTurnDelay => facingTurnDelay;
        public float PostAttackFacingLock => postAttackFacingLock;
        public int KillExperience => killExperience;
        public int Score => score;
        public float BaseMaxHp => baseMaxHp;
        public float HpGrowthRate => hpGrowthRate;
        public int LevelDifficultyOffset => levelDifficultyOffset;
        public string CombatProfileId => combatProfileId;
        public bool ShowHpBar => showHpBar;
        public bool AllowsEnemyOverlap =>
            PrototypeEnemyDefinitions.IsFlyingEye(enemyId);
        public bool BlocksFrontAttacks =>
            archetype == EnemyArchetype.Shield ||
            PrototypeEnemyDefinitions.IsSkeletonBoss(enemyId);

        public int CalculateMaxHealth(
            int enemyLevel,
            int waveNumber = 1)
        {
            int effectiveLevel = Mathf.Max(
                1,
                enemyLevel - levelDifficultyOffset);
            float levelHealth =
                ProgressionCurve.CalculateAdditiveStat(
                    baseMaxHp,
                    hpGrowthRate,
                    effectiveLevel);
            return ProgressionCurve.RoundPositiveStat(
                levelHealth *
                ProgressionCurve.CalculateWaveHealthMultiplier(
                    waveNumber));
        }

        public int CalculateAttackDamage(int enemyLevel)
        {
            return ProgressionCurve.CalculateEnemyAttackDamage(
                attackDamage,
                enemyLevel);
        }

        public float CalculateMoveSpeed(int enemyLevel)
        {
            return ProgressionCurve.CalculateEnemyMoveSpeed(
                moveSpeed,
                enemyLevel);
        }
    }

    public static class PrototypeEnemyDefinitions
    {
        public const string ShieldSkeletonId = "ShieldSkeleton";
        public const string GoblinBossId = "GoblinBoss";
        public const string MushroomBossId = "MushroomBoss";
        public const string FlyingEyeId = "FlyingEye";
        public const string FlyingEyeBossId = "FlyingEyeBoss";
        public const string SkeletonBossId = "SkeletonBoss";

        public static string GetEnemyId(EnemyArchetype archetype)
        {
            return archetype switch
            {
                EnemyArchetype.Melee => "GoblinMelee",
                EnemyArchetype.Ranged => "GoblinRanged",
                EnemyArchetype.Shield => ShieldSkeletonId,
                EnemyArchetype.Boss => GoblinBossId,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(archetype),
                    archetype,
                    null)
            };
        }

        public static string GetDisplayName(EnemyArchetype archetype)
        {
            return archetype switch
            {
                EnemyArchetype.Melee => "근접 고블린",
                EnemyArchetype.Ranged => "원거리 고블린",
                EnemyArchetype.Shield => "방패병",
                EnemyArchetype.Boss => "고블린 우두머리",
                _ => "적"
            };
        }

        public static string GetDisplayName(
            string enemyId,
            EnemyArchetype archetype)
        {
            return enemyId switch
            {
                MushroomBossId => "머쉬룸 보스",
                FlyingEyeId => "플라잉 아이",
                FlyingEyeBossId => "플라잉 아이 보스",
                SkeletonBossId => "스켈레톤 보스",
                _ => GetDisplayName(archetype)
            };
        }

        public static bool IsMushroomBoss(string enemyId)
        {
            return string.Equals(
                enemyId,
                MushroomBossId,
                StringComparison.Ordinal);
        }

        public static bool IsFlyingEye(string enemyId)
        {
            return string.Equals(
                    enemyId,
                    FlyingEyeId,
                    StringComparison.Ordinal) ||
                string.Equals(
                    enemyId,
                    FlyingEyeBossId,
                    StringComparison.Ordinal);
        }

        public static bool IsSkeletonBoss(string enemyId)
        {
            return string.Equals(
                enemyId,
                SkeletonBossId,
                StringComparison.Ordinal);
        }

        public static EnemyDefinition Create(EnemyArchetype archetype)
        {
            return archetype switch
            {
                EnemyArchetype.Melee => new EnemyDefinition(
                    GetEnemyId(archetype),
                    archetype,
                    0.85f,
                    0.85f,
                    2,
                    0.55f,
                    0f,
                    1.5f,
                    0f,
                    0f,
                    0.5f,
                    0f,
                    1,
                    5,
                    3f,
                    0.85f,
                    0,
                    "StandardMelee",
                    true),
                EnemyArchetype.Ranged => new EnemyDefinition(
                    GetEnemyId(archetype),
                    archetype,
                    0.6f,
                    2.25f,
                    2,
                    0.8f,
                    0f,
                    2f,
                    0f,
                    0f,
                    0.5f,
                    1f,
                    1,
                    7,
                    3f,
                    0.85f,
                    1,
                    "StandardRanged",
                    true),
                EnemyArchetype.Shield => new EnemyDefinition(
                    GetEnemyId(archetype),
                    archetype,
                    0.7f,
                    0f,
                    0,
                    0f,
                    0f,
                    0f,
                    0f,
                    2.25f,
                    0.5f,
                    0f,
                    2,
                    8,
                    3f,
                    0.85f,
                    0,
                    "Shield",
                    true),
                EnemyArchetype.Boss => new EnemyDefinition(
                    GetEnemyId(archetype),
                    archetype,
                    0.75f,
                    2.6f,
                    4,
                    1f,
                    0.33f,
                    2f,
                    1.35f,
                    0f,
                    0.5f,
                    0f,
                    8,
                    50,
                    38f,
                    8.5f,
                    0,
                    "Boss",
                    true),
                _ => throw new ArgumentOutOfRangeException(nameof(archetype), archetype, null)
            };
        }

        public static EnemyDefinition CreateMushroomBoss()
        {
            return new EnemyDefinition(
                MushroomBossId,
                EnemyArchetype.Boss,
                0.72f,
                2.6f,
                4,
                0.87f,
                0.33f,
                1.87f,
                1.5f,
                0f,
                0.5f,
                0f,
                10,
                70,
                45f,
                9.5f,
                0,
                "BossMushroom",
                true);
        }

        public static EnemyDefinition CreateFlyingEye()
        {
            return new EnemyDefinition(
                FlyingEyeId,
                EnemyArchetype.Melee,
                1.28f,
                0.95f,
                1,
                0.65f,
                0f,
                1.8f,
                0f,
                0f,
                0.5f,
                0f,
                2,
                8,
                3f,
                0.75f,
                0,
                "FlyingMelee",
                true);
        }

        public static EnemyDefinition CreateFlyingEyeBoss()
        {
            return new EnemyDefinition(
                FlyingEyeBossId,
                EnemyArchetype.Boss,
                0.88f,
                3.6f,
                4,
                0.6f,
                0.33f,
                1.67f,
                1.6f,
                0f,
                0.5f,
                0f,
                12,
                90,
                50f,
                10f,
                0,
                "BossFlyingEye",
                true);
        }

        public static EnemyDefinition CreateSkeletonBoss()
        {
            return new EnemyDefinition(
                SkeletonBossId,
                EnemyArchetype.Boss,
                0.8f,
                3f,
                4,
                0.8f,
                0.33f,
                2f,
                1.5f,
                0f,
                0.5f,
                0f,
                15,
                120,
                55f,
                10.5f,
                0,
                "BossSkeleton",
                true);
        }
    }
}
