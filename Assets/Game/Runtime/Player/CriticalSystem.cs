using System;
using UnityEngine;

namespace SimpleGame
{
    public sealed class CriticalSystem : MonoBehaviour
    {
        public const float CardIncrease = 0.1f;
        public const float MaximumChance = 0.7f;

        [SerializeField, Range(0f, MaximumChance)] private float chance;

        public event Action<float> Changed;
        public float Chance => chance;

        public bool Roll()
        {
            return UnityEngine.Random.value < chance;
        }

        public void AddCard()
        {
            chance = Mathf.Min(MaximumChance, chance + CardIncrease);
            Changed?.Invoke(chance);
        }
    }
}
