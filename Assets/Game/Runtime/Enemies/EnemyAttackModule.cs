using UnityEngine;

namespace SimpleGame
{
    public sealed class EnemyAttackModule : MonoBehaviour
    {
        private EnemyBase owner;
        private IPrototypeDamageTarget currentTarget;
        private SpriteRenderer indicator;
        private float hitAt;
        private float nextReadyAt;
        private bool windingUp;

        public bool IsBusy => windingUp;
        public IPrototypeDamageTarget CurrentTarget => currentTarget;
        public bool CanStart => !windingUp && Time.time >= nextReadyAt;

        public void Configure(EnemyBase enemy)
        {
            owner = enemy;
            indicator = PrototypeVisualFactory.CreateSprite(
                transform,
                "AttackWarning",
                new Color(1f, 0f, 0f, 0.34f),
                new Vector2(enemy.Definition.AttackRange * 2f, enemy.Definition.AttackRange * 2f),
                5);
            indicator.enabled = false;
        }

        public void Begin(IPrototypeDamageTarget target)
        {
            if (!CanStart || target == null)
            {
                return;
            }

            currentTarget = target;
            hitAt = Time.time + owner.Definition.AttackWindup;
            windingUp = true;
            indicator.enabled = true;
        }

        public bool Tick()
        {
            if (!windingUp)
            {
                return false;
            }

            if (currentTarget == null || !currentTarget.IsAlive)
            {
                Cancel();
                return false;
            }

            if (Time.time < hitAt)
            {
                return true;
            }

            float distance = Vector2.Distance(
                transform.position,
                currentTarget.TargetTransform.position);
            if (distance <= owner.Definition.AttackRange + 0.35f)
            {
                currentTarget.ReceiveDamage(owner.Definition.AttackDamage);
            }

            windingUp = false;
            indicator.enabled = false;
            nextReadyAt = Time.time + Mathf.Max(
                0.1f,
                owner.Definition.AttackCooldown - owner.Definition.AttackWindup);
            currentTarget = null;
            return false;
        }

        public void Cancel()
        {
            windingUp = false;
            currentTarget = null;
            if (indicator != null)
            {
                indicator.enabled = false;
            }
        }
    }
}
