using UnityEngine;

namespace SimpleGame
{
    public sealed class EnemyStateMachine : MonoBehaviour
    {
        private const float ShieldDirectionLockDuration = 0.8f;

        private EnemyBase owner;
        private bool shielding;
        private int pendingShieldSign;
        private float shieldDirectionLockedUntil;

        public void Configure(EnemyBase enemy)
        {
            owner = enemy;
            ResetAfterReposition();
        }

        public void ResetAfterReposition()
        {
            shielding = false;
            pendingShieldSign = 0;
            shieldDirectionLockedUntil = 0f;
        }

        private void Update()
        {
            if (owner == null ||
                !owner.IsAlive ||
                owner.Session == null ||
                !owner.Session.IsPlaying)
            {
                return;
            }

            PlayerRoot player = owner.Session.Player;
            if (player == null || !player.IsAlive)
            {
                owner.StopMoving();
                return;
            }

            if (owner.Archetype == EnemyArchetype.Boss)
            {
                owner.BossAttack.Tick(player);
                return;
            }

            Vector2 playerPosition = player.transform.position;
            if (owner.Archetype == EnemyArchetype.Shield)
            {
                TickShield(playerPosition);
                return;
            }

            if (owner.Attack.IsBusy)
            {
                owner.Attack.Tick();
                return;
            }

            float distance = Vector2.Distance(
                transform.position,
                playerPosition);
            if (distance <= owner.Definition.AttackRange &&
                owner.Attack.CanStart)
            {
                owner.Attack.Begin(player);
                return;
            }

            owner.MoveTowards(playerPosition);
        }

        private void TickShield(Vector2 playerPosition)
        {
            float distance = Vector2.Distance(
                transform.position,
                playerPosition);
            if (distance > owner.Definition.ApproachRange)
            {
                shielding = false;
                pendingShieldSign = 0;
                owner.MoveTowards(playerPosition);
                return;
            }

            if (!shielding)
            {
                shielding = true;
                shieldDirectionLockedUntil =
                    Time.time + ShieldDirectionLockDuration;
                owner.GuardCurrentDirection();
                return;
            }

            int currentSign = GetHorizontalSign(owner.Facing.Direction);
            int desiredSign = GetHorizontalSign(
                playerPosition - (Vector2)transform.position);
            if (desiredSign == 0 || desiredSign == currentSign)
            {
                pendingShieldSign = 0;
                owner.GuardCurrentDirection();
                return;
            }

            if (pendingShieldSign != desiredSign)
            {
                pendingShieldSign = desiredSign;
                shieldDirectionLockedUntil =
                    Time.time + ShieldDirectionLockDuration;
            }

            if (Time.time >= shieldDirectionLockedUntil)
            {
                owner.GuardTowardsImmediate(playerPosition);
                pendingShieldSign = 0;
                return;
            }

            owner.GuardCurrentDirection();
        }

        private static int GetHorizontalSign(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) <= 0.01f)
            {
                return 0;
            }

            return direction.x < 0f ? -1 : 1;
        }
    }
}
