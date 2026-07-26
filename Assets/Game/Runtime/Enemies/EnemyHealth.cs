using System;
using UnityEngine;

namespace SimpleGame
{
    public sealed class EnemyHealth : MonoBehaviour
    {
        [SerializeField] private int accumulatedDamage;
        [SerializeField] private bool alive = true;

        public event Action Hit;
        public event Action Depleted;

        public int AccumulatedDamage => accumulatedDamage;
        public bool IsAlive => alive;

        public void ResetHealth()
        {
            accumulatedDamage = 0;
            alive = true;
        }

        public bool Apply(CombatResult result)
        {
            if (!alive || !result.CanDamage)
            {
                return false;
            }

            accumulatedDamage += result.Damage;
            Hit?.Invoke();
            if (accumulatedDamage >= result.RequiredDurability)
            {
                alive = false;
                Depleted?.Invoke();
            }

            return true;
        }
    }
}
