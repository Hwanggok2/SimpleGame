using UnityEngine;

namespace SimpleGame
{
    public sealed class CriticalSystem : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float chance;
        [SerializeField, Range(0f, 1f)] private float cardIncrease = 0.1f;
        [SerializeField, Range(0f, 1f)] private float maximumChance = 0.7f;

        public float Chance => chance;

        public void Configure(float increasePerCard, float configuredMaximum)
        {
            cardIncrease = Mathf.Clamp01(increasePerCard);
            maximumChance = Mathf.Clamp01(configuredMaximum);
            chance = Mathf.Min(chance, maximumChance);
        }

        public bool Roll()
        {
            return UnityEngine.Random.value < chance;
        }

        public void AddCard()
        {
            chance = Mathf.Min(maximumChance, chance + cardIncrease);
        }
    }
}
