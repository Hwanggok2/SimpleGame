using UnityEngine;

namespace SimpleGame
{
    public sealed class EnemyHealth : MonoBehaviour
    {
        [SerializeField] private int accumulatedDamage;
        [SerializeField] private bool alive = true;

        public int AccumulatedDamage => accumulatedDamage;
        public bool IsAlive => alive;

        public void ResetHealth()
        {
            accumulatedDamage = 0;
            alive = true;
        }

        public bool Apply(CombatResult result)
        {
            if (!alive || result.Damage <= 0)
            {
                return false;
            }

            accumulatedDamage += result.Damage;
            if (accumulatedDamage >= result.RequiredDurability)
            {
                alive = false;
            }

            return true;
        }
    }
}
