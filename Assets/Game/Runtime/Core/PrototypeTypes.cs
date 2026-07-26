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

    public readonly struct CombatResult
    {
        public CombatResult(
            int damage,
            int requiredDurability,
            PlayerAttackReaction playerReaction)
        {
            Damage = damage;
            RequiredDurability = requiredDurability;
            PlayerReaction = playerReaction;
        }

        public int Damage { get; }
        public int RequiredDurability { get; }
        public PlayerAttackReaction PlayerReaction { get; }
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
            int score)
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
                    5),
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
                    5),
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
                    0),
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
                    25),
                _ => throw new ArgumentOutOfRangeException(nameof(archetype), archetype, null)
            };
        }
    }
}
