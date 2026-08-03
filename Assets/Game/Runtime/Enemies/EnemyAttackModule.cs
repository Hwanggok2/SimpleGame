using UnityEngine;

namespace SimpleGame
{
    public sealed class EnemyAttackModule : MonoBehaviour
    {
        public const float RangedWarningWidth = 0.32f;
        public const float MeleeSideWidthMultiplier = 1.5f;
        private const float AttackHitPadding = 0.35f;

        private EnemyBase owner;
        private IPrototypeDamageTarget currentTarget;
        [SerializeField] private SpriteRenderer indicator;
        [SerializeField]
        [Tooltip(
            "Ground range visual. Half of its largest world scale is " +
            "used as the ranged attack distance.")]
        private SpriteRenderer rangedAttackRange;
        [SerializeField] private RangedArrowProjectile projectilePrefab;
        private float hitAt;
        private float nextReadyAt;
        private float facingLockedUntil;
        private bool windingUp;
        private Vector2 lockedOrigin;
        private Vector2 lockedDirection = Vector2.right;

        public bool IsBusy => windingUp || Time.time < facingLockedUntil;
        public IPrototypeDamageTarget CurrentTarget => currentTarget;
        public bool CanStart => !IsBusy && Time.time >= nextReadyAt;
        public float AttackRange
        {
            get
            {
                if (rangedAttackRange != null)
                {
                    Vector3 scale = rangedAttackRange.transform.lossyScale;
                    return Mathf.Max(
                        0.1f,
                        Mathf.Max(
                            Mathf.Abs(scale.x),
                            Mathf.Abs(scale.y)) * 0.5f);
                }

                return owner?.Definition != null
                    ? Mathf.Max(0.1f, owner.Definition.AttackRange)
                    : 0.1f;
            }
        }

        public void ConfigureIndicator(SpriteRenderer configuredIndicator)
        {
            indicator = configuredIndicator;
        }

        public void ConfigureProjectile(
            RangedArrowProjectile configuredProjectilePrefab)
        {
            projectilePrefab = configuredProjectilePrefab;
        }

        public void ConfigureRangedAttackRange(
            SpriteRenderer configuredRange)
        {
            rangedAttackRange = configuredRange;
        }

        public void Configure(EnemyBase enemy)
        {
            owner = enemy;
            hitAt = 0f;
            nextReadyAt = 0f;
            facingLockedUntil = 0f;
            windingUp = false;
            currentTarget = null;
            if (indicator == null)
            {
                Debug.LogError(
                    "Enemy prefab requires a preconfigured attack warning.",
                    this);
                return;
            }

            indicator.enabled = false;
            if (enemy.Archetype == EnemyArchetype.Ranged &&
                projectilePrefab == null)
            {
                Debug.LogError(
                    "Ranged enemy prefab requires an arrow projectile.",
                    this);
            }

            if (enemy.Archetype == EnemyArchetype.Ranged)
            {
                EnsureRangedAttackRange();
            }
        }

        public void Begin(IPrototypeDamageTarget target)
        {
            if (!CanStart || target == null)
            {
                return;
            }

            currentTarget = target;
            lockedOrigin = transform.position;
            Vector2 offset =
                (Vector2)target.TargetTransform.position - lockedOrigin;
            lockedDirection = offset.sqrMagnitude > 0.0001f
                ? offset.normalized
                : owner.Facing.Direction;
            owner.FaceTowardsImmediate(target.TargetTransform.position);

            hitAt = Time.time + owner.Definition.AttackWindup;
            windingUp = true;
            ConfigureIndicatorForCurrentAttack();
        }

        public bool Tick()
        {
            if (!windingUp)
            {
                if (Time.time < facingLockedUntil)
                {
                    owner.StopMoving();
                    return true;
                }

                return false;
            }

            if (currentTarget == null || !currentTarget.IsAlive)
            {
                Cancel();
                return false;
            }

            if (Time.time < hitAt)
            {
                owner.StopMoving();
                return true;
            }

            owner.PlayAttackFacingDirection();
            if (owner.Archetype == EnemyArchetype.Ranged)
            {
                FireProjectile();
            }
            else if (IsInsideMeleeArea(
                         lockedOrigin,
                         lockedDirection,
                         currentTarget.TargetTransform.position,
                         AttackRange))
            {
                currentTarget.ReceiveDamage(
                    owner.Definition.CalculateAttackDamage(owner.Level));
            }

            windingUp = false;
            indicator.enabled = false;
            facingLockedUntil =
                Time.time + owner.Definition.PostAttackFacingLock;
            nextReadyAt =
                Time.time + Mathf.Max(0.1f, owner.Definition.AttackCooldown);
            currentTarget = null;
            return Time.time < facingLockedUntil;
        }

        public static bool IsInsideMeleeArea(
            Vector2 origin,
            Vector2 forward,
            Vector2 targetPosition,
            float attackRange)
        {
            Vector2 safeForward = forward.sqrMagnitude > 0.0001f
                ? forward.normalized
                : Vector2.right;
            Vector2 side = new(-safeForward.y, safeForward.x);
            Vector2 offset = targetPosition - origin;
            float forwardDistance = Vector2.Dot(offset, safeForward);
            float sideDistance = Mathf.Abs(Vector2.Dot(offset, side));
            float safeRange = Mathf.Max(0f, attackRange);
            return forwardDistance >= 0f &&
                forwardDistance <= safeRange + AttackHitPadding &&
                sideDistance <=
                    safeRange * MeleeSideWidthMultiplier * 0.5f +
                    AttackHitPadding;
        }

        public void Cancel()
        {
            windingUp = false;
            facingLockedUntil = 0f;
            currentTarget = null;
            if (indicator != null)
            {
                indicator.enabled = false;
            }
        }

        public void SetGameplayVisualsVisible(bool visible)
        {
            if (rangedAttackRange != null)
            {
                rangedAttackRange.enabled =
                    visible && owner != null &&
                    owner.Archetype == EnemyArchetype.Ranged;
            }
        }

        private void FireProjectile()
        {
            if (projectilePrefab == null ||
                currentTarget is not PlayerRoot player)
            {
                return;
            }

            Vector2 origin =
                (Vector2)transform.position +
                lockedDirection * (owner.CollisionRadius + 0.08f);
            RangedArrowProjectile projectile = Instantiate(
                projectilePrefab,
                origin,
                Quaternion.identity);
            projectile.Launch(
                player,
                origin,
                lockedDirection,
                owner.Definition.CalculateAttackDamage(owner.Level),
                AttackRange + AttackHitPadding);
        }

        private void ConfigureIndicatorForCurrentAttack()
        {
            float range = AttackRange;
            float width = owner.Archetype == EnemyArchetype.Ranged
                ? RangedWarningWidth
                : range * MeleeSideWidthMultiplier;
            Vector2 center = lockedOrigin + lockedDirection * (range * 0.5f);
            indicator.transform.SetPositionAndRotation(
                center,
                Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Atan2(lockedDirection.y, lockedDirection.x) *
                    Mathf.Rad2Deg));
            indicator.transform.localScale =
                new Vector3(range, width, 1f);
            indicator.enabled = true;
        }

        private void EnsureRangedAttackRange()
        {
            if (rangedAttackRange == null)
            {
                Debug.LogError(
                    "Ranged enemy prefab requires a preconfigured attack range visual.",
                    this);
                return;
            }

            rangedAttackRange.transform.localPosition = Vector3.zero;
            rangedAttackRange.transform.localRotation = Quaternion.identity;
            rangedAttackRange.enabled = true;
        }
    }
}
