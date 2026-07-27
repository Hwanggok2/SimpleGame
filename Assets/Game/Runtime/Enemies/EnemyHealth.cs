using System;
using UnityEngine;

namespace SimpleGame
{
    public sealed class EnemyHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 1f;
        [SerializeField] private float currentHealth = 1f;
        [SerializeField] private bool alive = true;

        public event Action<float, float> Changed;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsAlive => alive;

        public void Configure(float configuredMaxHealth)
        {
            maxHealth = Mathf.Max(1f, configuredMaxHealth);
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

            currentHealth = Mathf.Max(0f, currentHealth - result.Damage);
            alive = currentHealth > 0.0001f;
            Changed?.Invoke(currentHealth, maxHealth);

            return true;
        }
    }
}
