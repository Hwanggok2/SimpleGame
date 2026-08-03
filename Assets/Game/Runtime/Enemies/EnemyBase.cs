using System.Collections;
using UnityEngine;
using TMPro;

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
        [SerializeField] private SpriteRenderer approachRangeRenderer;
        [SerializeField] private SpriteRenderer facingMarker;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private EnemyHealthBar healthBar;
        [SerializeField] private Transform damagePopupAnchor;

        private Collider2D hitCollider;
        private CircleCollider2D circleCollider;
        private Coroutine deathRoutine;
        private EnemyWorldService enemyWorld;
        private PrototypeEnemyFactory spawnFactory;

        public abstract EnemyArchetype Archetype { get; }
        public int Level => level;
        public int WaveNumber { get; private set; } = 1;
        public EnemyDefinition Definition { get; private set; }
        public EnemyFacing Facing => facing;
        public EnemyMovement Movement => movement;
        public EnemyAttackModule Attack => attack;
        public BossAttackModule BossAttack => bossAttack;
        public int CurrentHealth =>
            health != null ? health.CurrentHealth : 0;
        public int MaxHealth =>
            health != null ? health.MaxHealth : 0;
        public bool IsAlive => health != null && health.IsAlive;
        public Transform DamagePopupAnchor => damagePopupAnchor;
        public Vector3 DamagePopupPosition =>
            damagePopupAnchor != null
                ? damagePopupAnchor.position
                : transform.position + Vector3.up * 1.25f;
        public bool AllowsEnemyOverlap =>
            Definition != null && Definition.AllowsEnemyOverlap;
        public float CollisionRadius
        {
            get
            {
                if (circleCollider == null)
                {
                    circleCollider =
                        hitCollider as CircleCollider2D ??
                        GetComponent<CircleCollider2D>();
                }

                if (circleCollider == null)
                {
                    return 0f;
                }

                Vector3 scale = circleCollider.transform.lossyScale;
                return circleCollider.radius *
                    Mathf.Max(
                        Mathf.Abs(scale.x),
                        Mathf.Abs(scale.y));
            }
        }
        public PrototypeGameSession Session { get; private set; }
        public uint SpawnGeneration { get; private set; }

        public void ConfigureVisuals(
            SpriteRenderer configuredApproachRange,
            SpriteRenderer configuredFacingMarker,
            TMP_Text configuredLevelLabel,
            EnemyHealthBar configuredHealthBar)
        {
            approachRangeRenderer = configuredApproachRange;
            facingMarker = configuredFacingMarker;
            if (facingMarker != null)
            {
                facingMarker.enabled = false;
            }

            levelLabel = configuredLevelLabel;
            healthBar = configuredHealthBar;
        }

        public void ConfigureDamagePopupAnchor(
            Transform configuredDamagePopupAnchor)
        {
            damagePopupAnchor = configuredDamagePopupAnchor;
        }

        public void Configure(
            PrototypeEnemyFactory configuredFactory,
            PrototypeGameSession session,
            EnemyWorldService configuredEnemyWorld,
            int enemyLevel,
            int waveNumber,
            EnemyDefinition definition)
        {
            spawnFactory = configuredFactory;
            Session = session;
            enemyWorld = configuredEnemyWorld;
            level = Mathf.Max(1, enemyLevel);
            WaveNumber = Mathf.Max(1, waveNumber);
            Definition = definition;
            SpawnGeneration =
                SpawnGeneration == uint.MaxValue
                    ? 1u
                    : SpawnGeneration + 1u;

            health = GetComponent<EnemyHealth>();
            facing = GetComponent<EnemyFacing>();
            movement = GetComponent<EnemyMovement>();
            stateMachine = GetComponent<EnemyStateMachine>();
            attack = GetComponent<EnemyAttackModule>();
            bossAttack = GetComponent<BossAttackModule>();
            characterAnimation = GetComponent<CharacterSpriteAnimator>();
            hitCollider = GetComponent<Collider2D>();
            circleCollider = hitCollider as CircleCollider2D;
            if (characterAnimation == null)
            {
                Debug.LogError(
                    $"{Archetype} prefab requires CharacterSpriteAnimator.",
                    this);
                return;
            }

            if (deathRoutine != null)
            {
                StopCoroutine(deathRoutine);
                deathRoutine = null;
            }

            health.Configure(
                Definition.CalculateMaxHealth(
                    level,
                    WaveNumber));
            healthBar?.Bind(health, Definition.ShowHpBar);
            facing.Configure(Definition.FacingTurnDelay);
            characterAnimation.Revive();
            if (hitCollider != null)
            {
                hitCollider.enabled = true;
            }

            SetGameplayVisualsVisible(true);
            BuildVisual();
            movement.Configure(
                Definition.CalculateMoveSpeed(level),
                characterAnimation);
            attack?.Configure(this);
            bossAttack?.Configure(this);
            stateMachine.Configure(this);
        }

        internal void PrepareForPool()
        {
            if (deathRoutine != null)
            {
                StopCoroutine(deathRoutine);
                deathRoutine = null;
            }

            movement?.StopImmediately();
            attack?.Cancel();
            bossAttack?.Cancel();
            stateMachine?.ResetAfterReposition();
            if (hitCollider != null)
            {
                hitCollider.enabled = false;
            }

            if (Definition != null)
            {
                SetGameplayVisualsVisible(false);
            }

            spawnFactory = null;
            Session = null;
            enemyWorld = null;
            Definition = null;
        }

        public void MoveTowards(Vector2 position)
        {
            facing.Face(position);
            movement.StepTowards(position, facing.Direction);
            enemyWorld?.SeparateEnemy(this);
        }

        public void DashStraightStep(
            Vector2 destination,
            Vector2 direction,
            float speedMultiplier)
        {
            movement.StepTowards(
                destination,
                direction,
                speedMultiplier);
            enemyWorld?.NotifyPositionChanged(this);
        }

        public void FaceTowards(Vector2 position)
        {
            facing.Face(position);
            characterAnimation.Face(facing.Direction);
            movement.Stop();
        }

        public void FaceTowardsImmediate(Vector2 position)
        {
            facing.FaceImmediate(position);
            characterAnimation.Face(facing.Direction);
            movement.Stop();
        }

        public void StopMoving()
        {
            movement.Stop();
        }

        public void GuardTowards(Vector2 position)
        {
            facing.Face(position);
            characterAnimation.SetGuard(facing.Direction);
        }

        public void GuardCurrentDirection()
        {
            movement.Stop();
            characterAnimation.SetGuard(facing.Direction);
        }

        public void GuardTowardsImmediate(Vector2 position)
        {
            facing.FaceImmediate(position);
            characterAnimation.SetGuard(facing.Direction);
        }

        public void PlayAttack(Vector2 targetPosition)
        {
            characterAnimation.PlayAttack(
                targetPosition - (Vector2)transform.position);
        }

        public void PlayAttackFacingDirection()
        {
            characterAnimation.PlayAttack(facing.Direction);
        }

        public void RefreshLevelLabel()
        {
            if (levelLabel == null ||
                Session?.Player?.Progression == null)
            {
                return;
            }

            levelLabel.color = CombatResolver.GetThreatLevel(
                Definition,
                MaxHealth,
                Session.Player.AttackPower,
                Session.Player.RearAttackMultiplier) switch
            {
                EnemyThreatLevel.OneHit => Color.green,
                EnemyThreatLevel.ThreeFrontOneRear => Color.white,
                _ => Color.red
            };
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

            int previousHealth = health.CurrentHealth;
            bool damaged = health.Apply(result);
            int appliedDamage =
                previousHealth - health.CurrentHealth;
            Vector2 hitDirection =
                (Vector2)attacker.transform.position -
                (Vector2)transform.position;

            if (appliedDamage > 0)
            {
                Session.ShowDamagePopup(
                    DamagePopupPosition,
                    appliedDamage,
                    critical
                        ? DamagePopupStyle.Critical
                        : DamagePopupStyle.Dealt);
            }

            string fallbackEnemyName =
                PrototypeEnemyDefinitions.GetDisplayName(
                    Definition.EnemyId,
                    Archetype);
            string enemyName = Session.GetString(
                GameStringIds.EnemyName(Definition.EnemyId),
                fallbackEnemyName);
            string sideText = side == AttackSide.Front
                ? Session.GetString(
                    GameStringIds.CombatSideFront,
                    "정면")
                : Session.GetString(
                    GameStringIds.CombatSideRear,
                    "후면");
            string resultText = damaged
                ? Session.FormatString(
                    critical
                        ? GameStringIds.HintEnemyCriticalDamageFormat
                        : GameStringIds.HintEnemyDamageFormat,
                    critical
                        ? "{0} 레벨 {1}: {2} 치명타 피해 {3:0.##}"
                        : "{0} 레벨 {1}: {2} 피해 {3:0.##}",
                    enemyName,
                    level,
                    sideText,
                    result.Damage)
                : Session.FormatString(
                    GameStringIds.HintEnemyFrontBlockFormat,
                    "{0} 레벨 {1}: 정면 방어",
                    enemyName,
                    level);
            Session.ShowHint(resultText);

            if (!health.IsAlive)
            {
                BeginDeath(hitDirection, critical);
                return damaged;
            }

            if (damaged)
            {
                characterAnimation.PlayHurt(hitDirection);
            }

            return damaged;
        }

        public void ApplyContinuePush(
            Vector2 position,
            Vector2 playerPosition,
            int damage,
            float duration)
        {
            if (!IsAlive)
            {
                return;
            }

            attack?.Cancel();
            bossAttack?.Cancel();
            movement.StopImmediately();
            stateMachine.ResetAfterReposition();

            Vector2 startPosition = transform.position;
            bool damaged = health.Apply(new CombatResult(
                Mathf.Max(0, damage),
                MaxHealth,
                PlayerAttackReaction.None));
            Vector2 hitDirection = playerPosition - startPosition;
            if (!health.IsAlive)
            {
                BeginDeath(hitDirection);
                return;
            }

            Vector2 destination = enemyWorld != null
                ? enemyWorld.FindOpenEnemyPosition(
                    position,
                    EnemyWorldService.GetColliderRadius(this),
                    this,
                    AllowsEnemyOverlap)
                : position;
            movement.Knockback(
                destination,
                duration,
                ResolveContinuePushOverlap);
            if (damaged)
            {
                characterAnimation.PlayHurt(hitDirection);
            }
        }

        public void Reposition(Vector2 position, Vector2 targetPosition)
        {
            attack?.Cancel();
            bossAttack?.Cancel();
            movement.StopImmediately();
            transform.position = enemyWorld != null
                ? enemyWorld.FindOpenEnemyPosition(
                    position,
                    EnemyWorldService.GetColliderRadius(this),
                    this,
                    AllowsEnemyOverlap)
                : position;
            enemyWorld?.NotifyPositionChanged(this);
            facing.FaceImmediate(targetPosition);
            stateMachine.ResetAfterReposition();
        }

        private void ResolveContinuePushOverlap()
        {
            enemyWorld?.SeparateEnemy(this);
        }

        private void BeginDeath(
            Vector2 hitDirection,
            bool critical = false)
        {
            movement.StopImmediately();
            attack?.Cancel();
            bossAttack?.Cancel();
            if (hitCollider != null)
            {
                hitCollider.enabled = false;
            }

            SetGameplayVisualsVisible(false);
            float deathDuration =
                characterAnimation.PlayDeath(hitDirection);
            Session.OnEnemyDefeated(this, critical);
            deathRoutine = StartCoroutine(
                DeactivateAfterDeath(deathDuration));
        }

        private void BuildVisual()
        {
            float size = Archetype == EnemyArchetype.Boss ? 1.75f : 0.82f;
            if (Archetype == EnemyArchetype.Shield)
            {
                if (approachRangeRenderer == null)
                {
                    Debug.LogError(
                        "Shield prefab requires a preconfigured approach range.",
                        this);
                }
                else
                {
                    approachRangeRenderer.transform.localScale =
                        Vector3.one * Definition.ApproachRange * 2f;
                }
            }

            if (!characterAnimation.IsConfigured)
            {
                Debug.LogError(
                    $"{Archetype} prefab has no configured Animator or SpriteRenderer.",
                    this);
            }

            if (facingMarker == null || levelLabel == null)
            {
                Debug.LogError(
                    $"{Archetype} prefab requires marker and level visuals.",
                    this);
                return;
            }

            facingMarker.transform.localPosition = new Vector3(0f, -size * 0.55f, 0f);
            levelLabel.transform.localPosition =
                new Vector3(0f, size * 0.82f, 0f);
            string fallbackDisplayName =
                PrototypeEnemyDefinitions.GetDisplayName(
                    Definition.EnemyId,
                    Archetype);
            string displayName = Session != null
                ? Session.GetString(
                    GameStringIds.EnemyName(Definition.EnemyId),
                    fallbackDisplayName)
                : fallbackDisplayName;
            levelLabel.text = Session != null
                ? Session.FormatString(
                    GameStringIds.EnemyLevelLabelFormat,
                    "{0} 레벨 {1}",
                    displayName,
                    level)
                : $"{displayName} 레벨 {level}";
            RefreshLevelLabel();
        }

        private void SetGameplayVisualsVisible(bool visible)
        {
            attack?.SetGameplayVisualsVisible(visible);
            if (approachRangeRenderer != null)
            {
                approachRangeRenderer.enabled = visible;
            }

            if (facingMarker != null)
            {
                facingMarker.enabled = false;
            }

            if (levelLabel != null)
            {
                levelLabel.enabled = visible;
            }

            healthBar?.SetVisible(visible && Definition.ShowHpBar);
        }

        private IEnumerator DeactivateAfterDeath(float delay)
        {
            yield return new WaitForSeconds(delay);
            deathRoutine = null;
            PrototypeEnemyFactory factory = spawnFactory;
            if (factory != null)
            {
                factory.Recycle(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

}
