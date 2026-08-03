using System;
using UnityEngine;

namespace SimpleGame
{
    public sealed class EnemyHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 1;
        [SerializeField] private int currentHealth = 1;
        [SerializeField] private bool alive = true;

        public event Action<int, int> Changed;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsAlive => alive;

        public void Configure(int configuredMaxHealth)
        {
            maxHealth = Mathf.Max(1, configuredMaxHealth);
            currentHealth = maxHealth;
            alive = true;
            Changed?.Invoke(currentHealth, maxHealth);
        }

        public bool Apply(CombatResult result)
        {
            if (!alive || result.Damage <= 0)
            {
                return false;
            }

            currentHealth = Mathf.Max(0, currentHealth - result.Damage);
            alive = currentHealth > 0;
            Changed?.Invoke(currentHealth, maxHealth);

            return true;
        }
    }
}
