using System;
using UnityEngine;

namespace SimpleGame
{
    public sealed class HealthComponent : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private int currentHealth = 10;
        private float invulnerableUntil;

        public event Action Depleted;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsAlive => currentHealth > 0;
        public bool IsInvulnerable => Time.time < invulnerableUntil;

        public void Configure(int value)
        {
            maxHealth = Mathf.Max(1, value);
            currentHealth = maxHealth;
            invulnerableUntil = 0f;
        }

        public bool ApplyDamage(int amount)
        {
            if (!IsAlive || IsInvulnerable || amount <= 0)
            {
                return false;
            }

            currentHealth = Mathf.Max(0, currentHealth - amount);
            if (currentHealth == 0)
            {
                Depleted?.Invoke();
            }

            return true;
        }

        public void RestoreFull()
        {
            currentHealth = maxHealth;
        }

        public void RestoreFraction(float fraction)
        {
            currentHealth = Mathf.Clamp(
                Mathf.CeilToInt(maxHealth * fraction),
                1,
                maxHealth);
        }

        public void MakeInvulnerable(float seconds)
        {
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
        }
    }

    public interface IPrototypeDamageTarget
    {
        Transform TargetTransform { get; }
        bool IsAlive { get; }
        void ReceiveDamage(int amount);
    }
}
