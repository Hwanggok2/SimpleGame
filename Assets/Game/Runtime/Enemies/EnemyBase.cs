using UnityEngine;

namespace SimpleGame
{
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(EnemyFacing))]
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(EnemyStateMachine))]
    public abstract class EnemyBase : MonoBehaviour
    {
        [SerializeField] private int level = 1;
        [SerializeField] private EnemyHealth health;
        [SerializeField] private EnemyFacing facing;
        [SerializeField] private EnemyMovement movement;
        [SerializeField] private EnemyStateMachine stateMachine;
        [SerializeField] private EnemyAttackModule attack;
        [SerializeField] private BossAttackModule bossAttack;
        [SerializeField] private CharacterSpriteAnimator characterAnimation;

        public abstract EnemyArchetype Archetype { get; }
        public int Level => level;
        public EnemyDefinition Definition { get; private set; }
        public EnemyFacing Facing => facing;
        public EnemyMovement Movement => movement;
        public EnemyAttackModule Attack => attack;
        public BossAttackModule BossAttack => bossAttack;
        public bool IsAlive => health != null && health.IsAlive;
        public PrototypeGameSession Session { get; private set; }

        public void Configure(PrototypeGameSession session, int enemyLevel)
        {
            Session = session;
            level = Mathf.Max(1, enemyLevel);
            Definition = PrototypeEnemyDefinitions.Create(Archetype);

            health = GetComponent<EnemyHealth>();
            facing = GetComponent<EnemyFacing>();
            movement = GetComponent<EnemyMovement>();
            stateMachine = GetComponent<EnemyStateMachine>();
            attack = GetComponent<EnemyAttackModule>();
            bossAttack = GetComponent<BossAttackModule>();
            characterAnimation = GetComponent<CharacterSpriteAnimator>();
            if (characterAnimation == null)
            {
                characterAnimation = gameObject.AddComponent<CharacterSpriteAnimator>();
            }

            health.ResetHealth();
            BuildVisual();
            movement.Configure(Definition.MoveSpeed, characterAnimation);
            attack?.Configure(this);
            bossAttack?.Configure(this);
            stateMachine.Configure(this);
        }

        public void MoveTowards(Vector2 position)
        {
            facing.Face(position);
            movement.StepTowards(position);
        }

        public void FaceTowards(Vector2 position)
        {
            facing.Face(position);
            characterAnimation.Face(position - (Vector2)transform.position);
            movement.Stop();
        }

        public void StopMoving()
        {
            movement.Stop();
        }

        public void GuardTowards(Vector2 position)
        {
            facing.Face(position);
            characterAnimation.SetGuard(
                position - (Vector2)transform.position);
        }

        public void PlayAttack(Vector2 targetPosition)
        {
            characterAnimation.PlayAttack(
                targetPosition - (Vector2)transform.position);
        }

        public bool ReceivePlayerAttack(
            CombatResult result,
            PlayerRoot attacker,
            AttackSide side,
            bool critical)
        {
            if (!IsAlive)
            {
                return false;
            }

            bool damaged = health.Apply(result);
            if (damaged)
            {
                characterAnimation.PlayHurt(
                    (Vector2)attacker.transform.position -
                    (Vector2)transform.position);
            }

            string resultText = damaged
                ? $"{Archetype} Lv.{level}: {side} {(critical ? "CRIT " : string.Empty)}-{result.Damage}"
                : $"{Archetype} Lv.{level}: FRONT IMMUNE";
            Session.ShowHint(resultText);

            if (!health.IsAlive)
            {
                Session.OnEnemyDefeated(this);
                gameObject.SetActive(false);
                return damaged;
            }

            stateMachine.OnPlayerHit(attacker);
            return damaged;
        }

        public void ApplyContinueKnockback(MapBounds bounds, Vector2 castlePosition)
        {
            Vector2 direction = (Vector2)transform.position - castlePosition;
            Vector2 edge = bounds.GetBoundaryPoint(transform.position, direction);
            Vector2 destination = Archetype == EnemyArchetype.Boss
                ? Vector2.Lerp(transform.position, edge, 0.5f)
                : edge;
            movement.Knockback(destination, 0.55f);
        }

        private void BuildVisual()
        {
            float size = Archetype == EnemyArchetype.Boss ? 1.35f : 0.82f;
            if (Archetype == EnemyArchetype.Shield)
            {
                PrototypeVisualFactory.CreateSprite(
                    transform,
                    "ShieldApproachRange",
                    new Color(0.15f, 0.8f, 0.95f, 0.18f),
                    Vector2.one * Definition.ApproachRange * 2f,
                    4);
            }

            Transform visualTransform = new GameObject("EnemyVisual").transform;
            visualTransform.SetParent(transform, false);
            SpriteRenderer renderer =
                visualTransform.gameObject.AddComponent<SpriteRenderer>();
            renderer.color = Color.white;
            renderer.sortingOrder = 20;
            visualTransform.localScale = Vector3.one *
                (Archetype == EnemyArchetype.Boss ? 1.8f : 1.25f);
            bool visualConfigured = Archetype == EnemyArchetype.Shield
                ? characterAnimation.ConfigureSkeleton(renderer)
                : characterAnimation.ConfigureGoblin(renderer);
            if (!visualConfigured)
            {
                Debug.LogWarning(
                    $"{Archetype} animation sprites were not found under Resources.",
                    this);
                renderer.sprite = PrototypeVisualFactory.SquareSprite;
                renderer.color = Definition.Color;
                visualTransform.localScale = Vector3.one * size;
            }

            SpriteRenderer facingMarker = PrototypeVisualFactory.CreateSprite(
                transform,
                "FacingMarker",
                Color.yellow,
                new Vector2(0.18f, 0.35f),
                24);
            facingMarker.transform.localPosition = new Vector3(0f, -size * 0.55f, 0f);

            PrototypeVisualFactory.CreateWorldLabel(
                transform,
                $"{Archetype} Lv.{level}",
                new Vector3(0f, size * 0.82f, 0f),
                2.3f,
                26);
        }
    }

    public sealed class MeleeEnemy : EnemyBase
    {
        public override EnemyArchetype Archetype => EnemyArchetype.Melee;
    }

    public sealed class RangedEnemy : EnemyBase
    {
        public override EnemyArchetype Archetype => EnemyArchetype.Ranged;
    }

    public sealed class ShieldEnemy : EnemyBase
    {
        public override EnemyArchetype Archetype => EnemyArchetype.Shield;
    }

    public sealed class BossEnemy : EnemyBase
    {
        public override EnemyArchetype Archetype => EnemyArchetype.Boss;
    }
}
