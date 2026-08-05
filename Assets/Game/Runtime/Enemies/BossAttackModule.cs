using UnityEngine;

namespace SimpleGame
{
    public sealed class BossAttackModule : MonoBehaviour
    {
        public const float DashTriggerDistance = 6f;
        public const float DefaultDashSpeedMultiplier = 5f;
        public const float DashWindup = 0.35f;
        public const float DashCooldown = 3f;
        public const int DashDamage = 100;
        public const float DashStunDuration = 0.6f;

        private enum DashPhase
        {
            None,
            Windup,
            Moving
        }

        private EnemyBase owner;
        private CharacterSpriteAnimator characterAnimation;
        [SerializeField] private SpriteRenderer indicator;
        [SerializeField, Min(0.1f)]
        [Tooltip("Boss move-speed multiplier used while dashing.")]
        private float dashSpeedMultiplier = DefaultDashSpeedMultiplier;
        private float cycleStartedAt = -1f;
        private bool damageApplied;
        private int nextPatternSequence;
        private BossAttackPattern activePattern;
        private Vector2 lockedOrigin;
        private Vector2 lockedDirection = Vector2.right;
        private Vector2 dashDestination;
        private float dashPhaseStartedAt;
        private float nextDashReadyAt;
        private bool dashHitApplied;
        private DashPhase dashPhase;

        public float DashSpeedMultiplier =>
            Mathf.Max(0.1f, dashSpeedMultiplier);

        public void ConfigureIndicator(SpriteRenderer configuredIndicator)
        {
            indicator = configuredIndicator;
        }

        public void Configure(EnemyBase enemy)
        {
            owner = enemy;
            characterAnimation = enemy != null
                ? enemy.GetComponent<CharacterSpriteAnimator>()
                : null;
            nextPatternSequence = 0;
            Cancel();
            nextDashReadyAt = Time.time + 0.5f;
            if (indicator == null)
            {
                Debug.LogError(
                    "Boss prefab requires a preconfigured attack warning.",
                    this);
                return;
            }

            indicator.enabled = false;
        }

        public void Cancel()
        {
            cycleStartedAt = -1f;
            damageApplied = false;
            dashPhase = DashPhase.None;
            dashHitApplied = false;
            if (indicator != null)
            {
                indicator.enabled = false;
            }
        }

        public void Tick(PlayerRoot player)
        {
            if (owner == null ||
                owner.Definition == null ||
                player == null)
            {
                return;
            }

            if (dashPhase != DashPhase.None)
            {
                TickDash(player);
                return;
            }

            if (cycleStartedAt < 0f)
            {
                float distance = Vector2.Distance(
                    owner.transform.position,
                    player.transform.position);
                if (player.IsAlive &&
                    distance >= DashTriggerDistance &&
                    Time.time >= nextDashReadyAt)
                {
                    BeginDash(player);
                    return;
                }

                BossAttackPattern nextPattern = BossAttackPatterns.Get(
                    owner.Definition.EnemyId,
                    nextPatternSequence);
                if (player.IsAlive &&
                    distance <=
                    nextPattern.EngagementRange)
                {
                    BeginAttack(player, nextPattern);
                }
                else
                {
                    owner.MoveTowards(player.transform.position);
                }

                return;
            }

            float elapsed = Time.time - cycleStartedAt;
            float activeEndsAt =
                owner.Definition.AttackWindup +
                owner.Definition.AttackActiveDuration;
            if (elapsed < owner.Definition.AttackWindup)
            {
                indicator.enabled = true;
                owner.StopMoving();
                return;
            }

            if (elapsed < activeEndsAt)
            {
                if (!damageApplied)
                {
                    damageApplied = true;
                    AdvancePattern();
                    characterAnimation?.PlayAttack(
                        lockedDirection,
                        activePattern.AnimationVariant);
                    if (player.IsAlive &&
                        activePattern.Contains(
                            lockedOrigin,
                            lockedDirection,
                            player.transform.position))
                    {
                        player.ReceiveDamage(
                            owner.Definition.CalculateAttackDamage(
                                owner.Level));
                    }
                }

                return;
            }

            indicator.enabled = false;
            if (elapsed < owner.Definition.AttackCooldown)
            {
                owner.MoveTowards(player.transform.position);
                return;
            }

            cycleStartedAt = -1f;
        }

        private void AdvancePattern()
        {
            nextPatternSequence =
                nextPatternSequence == int.MaxValue
                    ? 0
                    : nextPatternSequence + 1;
        }

        private void BeginAttack(
            PlayerRoot player,
            BossAttackPattern pattern)
        {
            lockedOrigin = owner.transform.position;
            Vector2 direction =
                (Vector2)player.transform.position - lockedOrigin;
            lockedDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : owner.Facing.Direction;
            activePattern = pattern;
            cycleStartedAt = Time.time;
            damageApplied = false;

            owner.FaceTowardsImmediate(player.transform.position);
            owner.StopMoving();
            ConfigureIndicator(pattern);
        }

        private void ConfigureIndicator(BossAttackPattern pattern)
        {
            Vector2 center = pattern.GetCenter(
                lockedOrigin,
                lockedDirection);
            indicator.transform.SetPositionAndRotation(
                center,
                Quaternion.Euler(
                    0f,
                    0f,
                    pattern.GetRotationDegrees(lockedDirection)));
            Vector2 size = pattern.IndicatorSize;
            indicator.transform.localScale =
                new Vector3(size.x, size.y, 1f);
            indicator.enabled = true;
        }

        private void BeginDash(PlayerRoot player)
        {
            lockedOrigin = owner.transform.position;
            dashDestination = player.transform.position;
            Vector2 offset = dashDestination - lockedOrigin;
            lockedDirection = offset.sqrMagnitude > 0.0001f
                ? offset.normalized
                : owner.Facing.Direction;
            dashPhase = DashPhase.Windup;
            dashPhaseStartedAt = Time.time;
            dashHitApplied = false;
            owner.FaceTowardsImmediate(dashDestination);
            owner.StopMoving();
            ConfigureDashIndicator(offset.magnitude);
        }

        private void TickDash(PlayerRoot player)
        {
            if (dashPhase == DashPhase.Windup)
            {
                owner.StopMoving();
                if (Time.time - dashPhaseStartedAt < DashWindup)
                {
                    return;
                }

                indicator.enabled = false;
                dashPhase = DashPhase.Moving;
            }

            Vector2 start = owner.transform.position;
            owner.DashStraightStep(
                dashDestination,
                lockedDirection,
                DashSpeedMultiplier);
            Vector2 end = owner.transform.position;
            if (!dashHitApplied &&
                player.IsAlive &&
                RangedArrowProjectile.SegmentHitsCircle(
                    start,
                    end,
                    player.transform.position,
                    owner.CollisionRadius + 0.45f))
            {
                dashHitApplied = true;
                player.ReceiveDamage(DashDamage);
                player.ApplyStun(DashStunDuration);
            }

            if ((dashDestination - end).sqrMagnitude > 0.0001f)
            {
                return;
            }

            dashPhase = DashPhase.None;
            nextDashReadyAt = Time.time + DashCooldown;
            owner.StopMoving();
        }

        private void ConfigureDashIndicator(float distance)
        {
            float width = Mathf.Max(
                0.5f,
                owner.CollisionRadius * 1.5f);
            Vector2 center =
                lockedOrigin + lockedDirection * (distance * 0.5f);
            indicator.transform.SetPositionAndRotation(
                center,
                Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Atan2(lockedDirection.y, lockedDirection.x) *
                    Mathf.Rad2Deg));
            indicator.transform.localScale = new Vector3(
                Mathf.Max(0.1f, distance),
                width,
                1f);
            indicator.enabled = true;
        }
    }
}
