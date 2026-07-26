using UnityEngine;

namespace SimpleGame
{
    public sealed class EnemyStateMachine : MonoBehaviour
    {
        private EnemyBase owner;
        private IPrototypeDamageTarget target;
        private bool aggroPlayer;
        private float nextTargetSampleAt;
        private Vector2 sampledTargetPosition;

        public void Configure(EnemyBase enemy)
        {
            owner = enemy;
            aggroPlayer = enemy.Archetype == EnemyArchetype.Shield;
            nextTargetSampleAt = 0f;
        }

        public void OnPlayerHit(PlayerRoot player)
        {
            if (owner.Archetype == EnemyArchetype.Boss)
            {
                return;
            }

            if (owner.Archetype == EnemyArchetype.Ranged &&
                owner.Attack != null &&
                owner.Attack.IsBusy &&
                owner.Attack.CurrentTarget is CastleRoot)
            {
                owner.Attack.Cancel();
            }

            aggroPlayer = true;
            target = player;
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

            if (owner.Archetype == EnemyArchetype.Boss)
            {
                owner.BossAttack.Tick(owner.Session.Player, owner.Session.Castle);
                return;
            }

            if (owner.Archetype == EnemyArchetype.Shield)
            {
                if (owner.Session.Player.IsAlive)
                {
                    Vector2 playerPosition = owner.Session.Player.transform.position;
                    float shieldDistance = Vector2.Distance(
                        transform.position,
                        playerPosition);
                    if (shieldDistance > owner.Definition.ApproachRange)
                    {
                        owner.MoveTowards(playerPosition);
                        if (Vector2.Distance(
                                transform.position,
                                playerPosition) <=
                            owner.Definition.ApproachRange)
                        {
                            owner.GuardTowards(playerPosition);
                        }
                    }
                    else
                    {
                        owner.GuardTowards(playerPosition);
                    }
                }
                else
                {
                    owner.StopMoving();
                }

                return;
            }

            SelectTarget();
            if (target == null || !target.IsAlive)
            {
                owner.StopMoving();
                return;
            }

            if (owner.Attack.IsBusy)
            {
                owner.Attack.Tick();
                return;
            }

            float distance = Vector2.Distance(
                transform.position,
                target.TargetTransform.position);
            if (distance <= owner.Definition.AttackRange && owner.Attack.CanStart)
            {
                owner.Attack.Begin(target);
                return;
            }

            if (Time.time >= nextTargetSampleAt)
            {
                sampledTargetPosition = target.TargetTransform.position;
                nextTargetSampleAt = Time.time + 0.7f;
            }

            owner.MoveTowards(sampledTargetPosition);
        }

        private void SelectTarget()
        {
            PlayerRoot player = owner.Session.Player;
            if (aggroPlayer && player.IsAlive)
            {
                target = player;
                return;
            }

            aggroPlayer = false;
            target = owner.Session.Castle;
        }
    }
}
