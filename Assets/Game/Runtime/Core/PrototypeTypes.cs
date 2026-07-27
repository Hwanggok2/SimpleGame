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
        Clear
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
        UpgradeRank
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
        ShieldBypass
    }

    public enum StatOperation
    {
        Add
    }

    public readonly struct CombatResult
    {
        public CombatResult(
            float damage,
            float targetMaxHealth,
            PlayerAttackReaction playerReaction)
        {
            Damage = damage;
            TargetMaxHealth = targetMaxHealth;
            PlayerReaction = playerReaction;
        }

        public float Damage { get; }
        public float TargetMaxHealth { get; }
        public PlayerAttackReaction PlayerReaction { get; }
    }

    public readonly struct PlayerAttackExecution
    {
        public PlayerAttackExecution(
            CombatResult primaryResult,
            bool primaryDamageApplied,
            bool anyDamageApplied,
            bool critical)
        {
            PrimaryResult = primaryResult;
            PrimaryDamageApplied = primaryDamageApplied;
            AnyDamageApplied = anyDamageApplied;
            Critical = critical;
        }

        public CombatResult PrimaryResult { get; }
        public bool PrimaryDamageApplied { get; }
        public bool AnyDamageApplied { get; }
        public bool Critical { get; }
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
        [SerializeField] private float hpGrowthMultiplier;
        [SerializeField] private int levelDifficultyOffset;
        [SerializeField] private int oneHitPlayerLevelAdvantage;
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
            float hpGrowthMultiplier,
            int levelDifficultyOffset,
            int oneHitPlayerLevelAdvantage,
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
            this.hpGrowthMultiplier = hpGrowthMultiplier;
            this.levelDifficultyOffset = levelDifficultyOffset;
            this.oneHitPlayerLevelAdvantage =
                oneHitPlayerLevelAdvantage;
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
        public float HpGrowthMultiplier => hpGrowthMultiplier;
        public int LevelDifficultyOffset => levelDifficultyOffset;
        public int OneHitPlayerLevelAdvantage =>
            oneHitPlayerLevelAdvantage;
        public string CombatProfileId => combatProfileId;
        public bool ShowHpBar => showHpBar;

        public float CalculateMaxHealth(int enemyLevel)
        {
            int effectiveLevel = Mathf.Max(
                1,
                enemyLevel - levelDifficultyOffset);
            return Mathf.Max(
                1f,
                baseMaxHp * Mathf.Pow(
                    Mathf.Max(1f, hpGrowthMultiplier),
                    effectiveLevel - 1));
        }

        public int CalculateAttackDamage(int enemyLevel)
        {
            return Mathf.Max(
                0,
                Mathf.CeilToInt(
                    attackDamage *
                    (1f + 0.05f * Mathf.Max(0, enemyLevel - 1))));
        }

        public bool IsOneHitTarget(int playerLevel, int enemyLevel)
        {
            return oneHitPlayerLevelAdvantage >= 0 &&
                playerLevel - enemyLevel >= oneHitPlayerLevelAdvantage;
        }
    }

    public static class PrototypeEnemyDefinitions
    {
        public static string GetEnemyId(EnemyArchetype archetype)
        {
            return archetype switch
            {
                EnemyArchetype.Melee => "GoblinMelee",
                EnemyArchetype.Ranged => "GoblinRanged",
                EnemyArchetype.Shield => "ShieldSkeleton",
                EnemyArchetype.Boss => "GoblinBoss",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(archetype),
                    archetype,
                    null)
            };
        }

        public static EnemyDefinition Create(EnemyArchetype archetype)
        {
            return archetype switch
            {
                EnemyArchetype.Melee => new EnemyDefinition(
                    GetEnemyId(archetype),
                    archetype,
                    0.7f,
                    0.85f,
                    2,
                    0.55f,
                    0f,
                    1.5f,
                    0f,
                    0f,
                    0.5f,
                    0f,
                    2,
                    5,
                    3f,
                    1.7f,
                    0,
                    1,
                    "StandardMelee",
                    true),
                EnemyArchetype.Ranged => new EnemyDefinition(
                    GetEnemyId(archetype),
                    archetype,
                    0.55f,
                    2.25f,
                    2,
                    0.8f,
                    0f,
                    2f,
                    0f,
                    0f,
                    0.5f,
                    1f,
                    2,
                    5,
                    3f,
                    1.7f,
                    1,
                    0,
                    "StandardRanged",
                    true),
                EnemyArchetype.Shield => new EnemyDefinition(
                    GetEnemyId(archetype),
                    archetype,
                    0.6f,
                    0f,
                    0,
                    0f,
                    0f,
                    0f,
                    0f,
                    2.25f,
                    0.5f,
                    0f,
                    0,
                    0,
                    3f,
                    1.7f,
                    0,
                    2,
                    "Shield",
                    true),
                EnemyArchetype.Boss => new EnemyDefinition(
                    GetEnemyId(archetype),
                    archetype,
                    0.42f,
                    2.6f,
                    4,
                    1.5f,
                    0.5f,
                    3f,
                    1.35f,
                    0f,
                    0.5f,
                    0f,
                    5,
                    25,
                    15f,
                    1.7f,
                    0,
                    -1,
                    "Boss",
                    true),
                _ => throw new ArgumentOutOfRangeException(nameof(archetype), archetype, null)
            };
        }
    }
}
