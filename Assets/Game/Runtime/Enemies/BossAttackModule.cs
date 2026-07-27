using UnityEngine;

namespace SimpleGame
{
    public sealed class BossAttackModule : MonoBehaviour
    {
        private EnemyBase owner;
        [SerializeField] private SpriteRenderer indicator;
        private float cycleStartedAt = -1f;
        private bool damageApplied;

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
                    "Boss prefab requires a preconfigured attack warning.",
                    this);
                return;
            }

            indicator.transform.localPosition = new Vector3(0f, -1.5f, 0f);
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
            if (cycleStartedAt < 0f)
            {
                if (player.IsAlive &&
                    Vector2.Distance(owner.transform.position, player.transform.position) <=
                    owner.Definition.AttackRange)
                {
                    cycleStartedAt = Time.time;
                    damageApplied = false;
                    indicator.enabled = true;
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
                owner.MoveTowards(player.transform.position);
                return;
            }

            if (elapsed < activeEndsAt)
            {
                if (!damageApplied)
                {
                    damageApplied = true;
                    owner.PlayAttack(player.transform.position);
                    Vector2 attackCenter = indicator.transform.position;
                    if (player.IsAlive &&
                        Vector2.Distance(player.transform.position, attackCenter) <=
                        owner.Definition.AttackAreaRadius)
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
    }
}
