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
        public EnemyDefinition(
            EnemyArchetype archetype,
            float moveSpeed,
            float attackRange,
            int attackDamage,
            float attackWindup,
            float attackCooldown,
            float approachRange,
            Color color)
        {
            Archetype = archetype;
            MoveSpeed = moveSpeed;
            AttackRange = attackRange;
            AttackDamage = attackDamage;
            AttackWindup = attackWindup;
            AttackCooldown = attackCooldown;
            ApproachRange = approachRange;
            Color = color;
        }

        public EnemyArchetype Archetype { get; }
        public float MoveSpeed { get; }
        public float AttackRange { get; }
        public int AttackDamage { get; }
        public float AttackWindup { get; }
        public float AttackCooldown { get; }
        public float ApproachRange { get; }
        public Color Color { get; }
    }

    public static class PrototypeEnemyDefinitions
    {
        public static EnemyDefinition Create(EnemyArchetype archetype)
        {
            return archetype switch
            {
                EnemyArchetype.Melee => new EnemyDefinition(
                    archetype, 0.7f, 0.85f, 2, 0.55f, 1.5f, 0f,
                    new Color(0.9f, 0.25f, 0.18f)),
                EnemyArchetype.Ranged => new EnemyDefinition(
                    archetype, 0.55f, 2.25f, 2, 0.8f, 2.2f, 0f,
                    new Color(0.55f, 0.18f, 0.85f)),
                EnemyArchetype.Shield => new EnemyDefinition(
                    archetype, 0.6f, 0f, 0, 0f, 0f, 2.25f,
                    new Color(0.15f, 0.45f, 0.9f)),
                EnemyArchetype.Boss => new EnemyDefinition(
                    archetype, 0.42f, 2.6f, 4, 1.5f, 3f, 0f,
                    new Color(0.55f, 0.05f, 0.05f)),
                _ => throw new ArgumentOutOfRangeException(nameof(archetype), archetype, null)
            };
        }
    }
}
