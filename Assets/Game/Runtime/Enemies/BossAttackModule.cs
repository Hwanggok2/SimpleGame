using UnityEngine;

namespace SimpleGame
{
    public sealed class BossAttackModule : MonoBehaviour
    {
        private EnemyBase owner;
        private SpriteRenderer indicator;
        private float cycleStartedAt = -1f;
        private bool damageApplied;

        public void Configure(EnemyBase enemy)
        {
            owner = enemy;
            indicator = PrototypeVisualFactory.CreateSprite(
                transform,
                "BossAttackWarning",
                new Color(1f, 0.05f, 0.05f, 0.42f),
                new Vector2(2.4f, 2.4f),
                6);
            indicator.transform.localPosition = new Vector3(0f, -1.5f, 0f);
            indicator.enabled = false;
        }

        public void Tick(PlayerRoot player, CastleRoot castle)
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
                    owner.MoveTowards(castle.transform.position);
                }

                return;
            }

            float elapsed = Time.time - cycleStartedAt;
            if (elapsed < 1.5f)
            {
                indicator.enabled = true;
                owner.MoveTowards(castle.transform.position);
                return;
            }

            if (elapsed < 2f)
            {
                if (!damageApplied)
                {
                    damageApplied = true;
                    owner.PlayAttack(player.transform.position);
                    Vector2 attackCenter = indicator.transform.position;
                    if (player.IsAlive &&
                        Vector2.Distance(player.transform.position, attackCenter) <= 1.35f)
                    {
                        player.ReceiveDamage(owner.Definition.AttackDamage);
                    }
                }

                return;
            }

            indicator.enabled = false;
            if (elapsed < 3f)
            {
                owner.MoveTowards(castle.transform.position);
                return;
            }

            cycleStartedAt = -1f;
        }
    }
}
