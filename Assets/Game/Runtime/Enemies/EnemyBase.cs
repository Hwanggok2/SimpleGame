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

        private Collider2D hitCollider;
        private Coroutine deathRoutine;

        public abstract EnemyArchetype Archetype { get; }
        public int Level => level;
        public EnemyDefinition Definition { get; private set; }
        public EnemyFacing Facing => facing;
        public EnemyMovement Movement => movement;
        public EnemyAttackModule Attack => attack;
        public BossAttackModule BossAttack => bossAttack;
        public bool IsAlive => health != null && health.IsAlive;
        public PrototypeGameSession Session { get; private set; }

        public void ConfigureVisuals(
            SpriteRenderer configuredApproachRange,
            SpriteRenderer configuredFacingMarker,
            TMP_Text configuredLevelLabel)
        {
            approachRangeRenderer = configuredApproachRange;
            facingMarker = configuredFacingMarker;
            levelLabel = configuredLevelLabel;
        }

        public void Configure(
            PrototypeGameSession session,
            int enemyLevel,
            EnemyDefinition definition)
        {
            Session = session;
            level = Mathf.Max(1, enemyLevel);
            Definition = definition;

            health = GetComponent<EnemyHealth>();
            facing = GetComponent<EnemyFacing>();
            movement = GetComponent<EnemyMovement>();
            stateMachine = GetComponent<EnemyStateMachine>();
            attack = GetComponent<EnemyAttackModule>();
            bossAttack = GetComponent<BossAttackModule>();
            characterAnimation = GetComponent<CharacterSpriteAnimator>();
            hitCollider = GetComponent<Collider2D>();
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

            health.ResetHealth();
            characterAnimation.Revive();
            facing.Configure(Definition.FacingTurnDelay);
            if (hitCollider != null)
            {
                hitCollider.enabled = true;
            }

            SetGameplayVisualsVisible(true);
            BuildVisual();
            movement.Configure(Definition.MoveSpeed, characterAnimation);
            attack?.Configure(this);
            bossAttack?.Configure(this);
            stateMachine.Configure(this);
        }

        public void MoveTowards(Vector2 position)
        {
            facing.Face(position);
            movement.StepTowards(position, facing.Direction);
        }

        public void FaceTowards(Vector2 position)
        {
            facing.Face(position);
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

        public void PlayAttack(Vector2 targetPosition)
        {
            characterAnimation.PlayAttack(
                targetPosition - (Vector2)transform.position);
        }

        public void RefreshLevelLabel()
        {
            if (levelLabel == null ||
                Session?.Player?.Progression == null)
            {
                return;
            }

            levelLabel.color = CombatResolver.GetThreatLevel(
                Archetype,
                Session.Player.Progression.Level,
                level) switch
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

            bool damaged = health.Apply(result);
            Vector2 hitDirection =
                (Vector2)attacker.transform.position -
                (Vector2)transform.position;

            string resultText = damaged
                ? $"{Archetype} Lv.{level}: {side} {(critical ? "CRIT " : string.Empty)}-{result.Damage}"
                : $"{Archetype} Lv.{level}: FRONT IMMUNE";
            Session.ShowHint(resultText);

            if (!health.IsAlive)
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
                Session.OnEnemyDefeated(this);
                deathRoutine = StartCoroutine(
                    DeactivateAfterDeath(deathDuration));
                return damaged;
            }

            if (damaged)
            {
                characterAnimation.PlayHurt(hitDirection);
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
            levelLabel.text = $"{Definition.EnemyId} Lv.{level}";
            RefreshLevelLabel();
        }

        private void SetGameplayVisualsVisible(bool visible)
        {
            if (approachRangeRenderer != null)
            {
                approachRangeRenderer.enabled = visible;
            }

            if (facingMarker != null)
            {
                facingMarker.enabled = visible;
            }

            if (levelLabel != null)
            {
                levelLabel.enabled = visible;
            }
        }

        private IEnumerator DeactivateAfterDeath(float delay)
        {
            yield return new WaitForSeconds(delay);
            deathRoutine = null;
            gameObject.SetActive(false);
        }
    }

}
