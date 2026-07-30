using UnityEngine;

namespace SimpleGame
{
    public sealed class BossAttackModule : MonoBehaviour
    {
        private EnemyBase owner;
        private CharacterSpriteAnimator characterAnimation;
        [SerializeField] private SpriteRenderer indicator;
        private float cycleStartedAt = -1f;
        private bool damageApplied;
        private int nextPatternSequence;
        private BossAttackPattern activePattern;
        private Vector2 lockedOrigin;
        private Vector2 lockedDirection = Vector2.right;

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

            if (cycleStartedAt < 0f)
            {
                BossAttackPattern nextPattern = BossAttackPatterns.Get(
                    owner.Definition.EnemyId,
                    nextPatternSequence);
                if (player.IsAlive &&
                    Vector2.Distance(owner.transform.position, player.transform.position) <=
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
    }
}
