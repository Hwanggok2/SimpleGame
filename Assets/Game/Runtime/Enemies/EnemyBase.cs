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

            health.ResetHealth();
            movement.Configure(Definition.MoveSpeed);
            attack?.Configure(this);
            bossAttack?.Configure(this);
            stateMachine.Configure(this);
            BuildVisual();
        }

        public void MoveTowards(Vector2 position)
        {
            facing.Face(position);
            movement.StepTowards(position);
        }

        public void ReceivePlayerAttack(
            CombatResult result,
            PlayerRoot attacker,
            AttackSide side,
            bool critical)
        {
            if (!IsAlive)
            {
                return;
            }

            bool damaged = health.Apply(result);
            string resultText = damaged
                ? $"{Archetype} Lv.{level}: {side} {(critical ? "CRIT " : string.Empty)}-{result.Damage}"
                : $"{Archetype} Lv.{level}: FRONT IMMUNE";
            Session.ShowHint(resultText);

            if (!health.IsAlive)
            {
                Session.OnEnemyDefeated(this);
                gameObject.SetActive(false);
                return;
            }

            stateMachine.OnPlayerHit(attacker);
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
            PrototypeVisualFactory.CreateSprite(
                transform,
                "EnemyVisual",
                Definition.Color,
                new Vector2(size, size),
                20);

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
