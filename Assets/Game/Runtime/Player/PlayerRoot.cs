using System.Collections;
using TMPro;
using UnityEngine;

namespace SimpleGame
{
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(CriticalSystem))]
    [RequireComponent(typeof(PlayerProgression))]
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(PlayerCombatAbilities))]
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerRoot : MonoBehaviour, IPrototypeDamageTarget
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private CriticalSystem critical;
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private PlayerCombatAbilities combatAbilities;
        [SerializeField] private CharacterSpriteAnimator characterAnimation;
        [SerializeField] private SpriteRenderer attackRangeRenderer;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private string playerId = "LightBandit";

        private const float FrontRecoilDistance = 1.2f;
        private const float FrontRecoilMoveDuration = 0.18f;
        private const float FrontRecoilInputLockDuration = 0.5f;

        private Coroutine inputLockRoutine;
        private int moveSpeedCardLevel;

        public HealthComponent Health => health;
        public PlayerMovement Movement => movement;
        public CriticalSystem Critical => critical;
        public PlayerProgression Progression => progression;
        public PlayerCombatAbilities CombatAbilities => combatAbilities;
        public float AttackPower =>
            stats.GetAttackPower(progression.Level);
        public float RearAttackMultiplier =>
            stats.RearAttackMultiplier;
        public float MoveSpeed => stats.MoveSpeed;
        public float AttackRange => stats.AttackRange;
        public float PathEnemyApproachSpeedMultiplier =>
            stats.PathEnemyApproachSpeedMultiplier;
        public float PostKillEscapeSpeedMultiplier =>
            stats.PostKillEscapeSpeedMultiplier;
        public float MoveArrivalTolerance =>
            stats.MoveArrivalTolerance;
        public Transform TargetTransform => transform;
        public bool IsAlive => health != null && health.IsAlive;
        public bool IsInputLocked { get; private set; }

        public void ConfigureVisuals(
            SpriteRenderer configuredAttackRange,
            TMP_Text configuredLevelLabel)
        {
            attackRangeRenderer = configuredAttackRange;
            levelLabel = configuredLevelLabel;
        }

        public void Configure(
            PrototypeGameSession session,
            EnemyWorldService enemyWorld,
            Camera worldCamera,
            LevelExperienceTable experienceTable,
            GlobalBalance globalBalance,
            PlayerBalanceTable playerBalance)
        {
            health = GetComponent<HealthComponent>();
            movement = GetComponent<PlayerMovement>();
            critical = GetComponent<CriticalSystem>();
            progression = GetComponent<PlayerProgression>();
            controller = GetComponent<PlayerController>();
            stats = GetComponent<PlayerStats>();
            combatAbilities = GetComponent<PlayerCombatAbilities>();
            if (combatAbilities == null)
            {
                combatAbilities = gameObject.AddComponent<
                    PlayerCombatAbilities>();
            }
            characterAnimation = GetComponent<CharacterSpriteAnimator>();
            if (characterAnimation == null || stats == null)
            {
                Debug.LogError(
                    "Player prefab requires CharacterSpriteAnimator " +
                    "and PlayerStats.",
                    this);
                return;
            }

            if (playerBalance == null ||
                !playerBalance.TryGet(
                    playerId,
                    out PlayerDefinition definition))
            {
                Debug.LogError(
                    $"Player balance not found: {playerId}",
                    this);
                return;
            }

            stats.Configure(definition);
            health.Configure(definition.BaseMaxHp);
            progression.Configure(experienceTable);
            critical.Configure(
                globalBalance.CriticalChancePerCard,
                globalBalance.MaximumCriticalChance);
            critical.Add(definition.BaseCriticalChance);
            combatAbilities.Configure(this, enemyWorld);
            moveSpeedCardLevel = 0;
            movement.SetMaximumSpeedActive(false);
            BuildVisual();
            movement.Configure(
                stats.MoveSpeed,
                characterAnimation);
            controller.Configure(
                this,
                session,
                enemyWorld,
                worldCamera,
                stats.AttackRange);
        }

        public bool ApplyCard(LevelUpCardDefinition card)
        {
            if (card == null || card.Operation != StatOperation.Add)
            {
                return false;
            }

            switch (card.TargetStat)
            {
                case PlayerStatId.CriticalChance:
                    critical.Add(card.Value);
                    break;
                case PlayerStatId.MaxHp:
                    health.IncreaseMaximum(
                        Mathf.RoundToInt(card.Value),
                        true);
                    break;
                case PlayerStatId.MoveSpeed:
                    stats.AddMoveSpeed(card.Value);
                    movement.SetMoveSpeed(stats.MoveSpeed);
                    moveSpeedCardLevel = Mathf.Min(
                        card.MaxStack,
                        moveSpeedCardLevel + 1);
                    movement.SetMaximumSpeedActive(
                        moveSpeedCardLevel >= card.MaxStack);
                    break;
                case PlayerStatId.AttackRange:
                    stats.AddAttackRange(card.Value);
                    controller.SetAttackRange(stats.AttackRange);
                    RefreshAttackRangeVisual();
                    break;
                case PlayerStatId.Piercing:
                case PlayerStatId.Sever:
                case PlayerStatId.HitHeal:
                case PlayerStatId.StaticCharge:
                case PlayerStatId.MovingSlash:
                case PlayerStatId.ShieldBypass:
                    return combatAbilities.ApplyCard(card);
                default:
                    return false;
            }

            return true;
        }

        public PlayerAttackExecution AttackEnemy(
            EnemyBase enemy,
            bool criticalHit,
            bool allowPiercing)
        {
            return combatAbilities.ExecuteNormalAttack(
                enemy,
                criticalHit,
                allowPiercing);
        }

        public bool ApplySkillHit(
            EnemyBase enemy,
            float damageMultiplier)
        {
            return combatAbilities.ApplySkillHit(
                enemy,
                damageMultiplier);
        }

        public void TrySpawnMovingSlash(Vector2 movementDirection)
        {
            combatAbilities.TrySpawnMovingSlash(movementDirection);
        }

        public void ReceiveDamage(int amount)
        {
            if (!health.ApplyDamage(amount))
            {
                return;
            }

            if (health.IsAlive)
            {
                characterAnimation.PlayHurt(Vector2.zero);
                return;
            }

            controller.CancelCommand();
            movement.StopKnockback();
            IsInputLocked = true;
            characterAnimation.PlayDeath(Vector2.zero);
        }

        public void PlayAttack(Vector2 targetPosition)
        {
            characterAnimation.PlayAttack(
                targetPosition - (Vector2)transform.position);
        }

        public void RestoreAfterContinue()
        {
            gameObject.SetActive(true);
            health.RestoreFull();
            movement.StopKnockback();
            controller.CancelCommand();
            if (inputLockRoutine != null)
            {
                StopCoroutine(inputLockRoutine);
                inputLockRoutine = null;
            }

            IsInputLocked = false;
            characterAnimation.Revive();
        }

        public void ApplyFrontRecoil(Vector2 enemyPosition)
        {
            Vector2 direction = (Vector2)transform.position - enemyPosition;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.down;
            }

            Vector2 destination =
                (Vector2)transform.position +
                direction.normalized * FrontRecoilDistance;
            controller.CancelCommand();
            movement.Knockback(destination, FrontRecoilMoveDuration);
            characterAnimation.PlayHurt(
                enemyPosition - (Vector2)transform.position);
            LockInput(FrontRecoilInputLockDuration);
        }

        public void LockInput(float seconds)
        {
            if (gameObject.activeInHierarchy)
            {
                if (inputLockRoutine != null)
                {
                    StopCoroutine(inputLockRoutine);
                }

                inputLockRoutine = StartCoroutine(LockRoutine(seconds));
            }
        }

        private IEnumerator LockRoutine(float seconds)
        {
            IsInputLocked = true;
            yield return new WaitForSeconds(seconds);
            IsInputLocked = false;
            inputLockRoutine = null;
        }

        private void BuildVisual()
        {
            if (attackRangeRenderer == null || levelLabel == null)
            {
                Debug.LogError(
                    "Player prefab requires preconfigured range and level visuals.",
                    this);
                return;
            }

            RefreshAttackRangeVisual();
            levelLabel.text = "플레이어";

            if (!characterAnimation.IsConfigured)
            {
                Debug.LogError(
                    "Player prefab has no configured Animator or SpriteRenderer.",
                    this);
            }
        }

        private void RefreshAttackRangeVisual()
        {
            if (attackRangeRenderer != null && stats != null)
            {
                attackRangeRenderer.transform.localScale =
                    Vector3.one * stats.AttackRange * 2f;
            }
        }
    }
}
