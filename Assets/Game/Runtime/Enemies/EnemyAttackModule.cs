using UnityEngine;

namespace SimpleGame
{
    public sealed class EnemyAttackModule : MonoBehaviour
    {
        private EnemyBase owner;
        private IPrototypeDamageTarget currentTarget;
        [SerializeField] private SpriteRenderer indicator;
        private float hitAt;
        private float nextReadyAt;
        private bool windingUp;

        public bool IsBusy => windingUp;
        public IPrototypeDamageTarget CurrentTarget => currentTarget;
        public bool CanStart => !windingUp && Time.time >= nextReadyAt;

        public void ConfigureIndicator(SpriteRenderer configuredIndicator)
        {
            indicator = configuredIndicator;
        }

        public void Configure(EnemyBase enemy)
        {
            owner = enemy;
            if (indicator == null)
            {
                Debug.LogError(
                    "Enemy prefab requires a preconfigured attack warning.",
                    this);
                return;
            }

            indicator.transform.localScale =
                Vector3.one * enemy.Definition.AttackRange * 2f;
            indicator.enabled = false;
        }

        public void Begin(IPrototypeDamageTarget target)
        {
            if (!CanStart || target == null)
            {
                return;
            }

            currentTarget = target;
            owner.FaceTowards(target.TargetTransform.position);
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
                owner.PlayAttack(currentTarget.TargetTransform.position);
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
