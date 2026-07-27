using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleGame
{
    [Serializable]
    public sealed class PlayerDefinition
    {
        [SerializeField] private string playerId;
        [SerializeField] private int startLevel;
        [SerializeField] private int baseMaxHp;
        [SerializeField] private float baseAttackPower;
        [SerializeField] private float attackGrowthMultiplier;
        [SerializeField] private float rearAttackMultiplier;
        [SerializeField] private float baseMoveSpeed;
        [SerializeField] private float pathEnemyApproachSpeedMultiplier;
        [SerializeField] private float postKillEscapeSpeedMultiplier;
        [SerializeField] private float moveArrivalTolerance;
        [SerializeField] private float attackRange;
        [SerializeField] private float baseCriticalChance;
        [SerializeField] private bool enabled;

        public PlayerDefinition(
            string playerId,
            int startLevel,
            int baseMaxHp,
            float baseAttackPower,
            float attackGrowthMultiplier,
            float rearAttackMultiplier,
            float baseMoveSpeed,
            float pathEnemyApproachSpeedMultiplier,
            float postKillEscapeSpeedMultiplier,
            float moveArrivalTolerance,
            float attackRange,
            float baseCriticalChance,
            bool enabled)
        {
            this.playerId = playerId;
            this.startLevel = startLevel;
            this.baseMaxHp = baseMaxHp;
            this.baseAttackPower = baseAttackPower;
            this.attackGrowthMultiplier = attackGrowthMultiplier;
            this.rearAttackMultiplier = rearAttackMultiplier;
            this.baseMoveSpeed = baseMoveSpeed;
            this.pathEnemyApproachSpeedMultiplier =
                pathEnemyApproachSpeedMultiplier;
            this.postKillEscapeSpeedMultiplier =
                postKillEscapeSpeedMultiplier;
            this.moveArrivalTolerance = moveArrivalTolerance;
            this.attackRange = attackRange;
            this.baseCriticalChance = baseCriticalChance;
            this.enabled = enabled;
        }

        public string PlayerId => playerId;
        public int StartLevel => startLevel;
        public int BaseMaxHp => baseMaxHp;
        public float RearAttackMultiplier => rearAttackMultiplier;
        public float BaseMoveSpeed => baseMoveSpeed;
        public float PathEnemyApproachSpeedMultiplier =>
            pathEnemyApproachSpeedMultiplier;
        public float PostKillEscapeSpeedMultiplier =>
            postKillEscapeSpeedMultiplier;
        public float MoveArrivalTolerance => moveArrivalTolerance;
        public float AttackRange => attackRange;
        public float BaseCriticalChance => baseCriticalChance;
        public bool Enabled => enabled;

        public float CalculateAttackPower(int level)
        {
            return baseAttackPower * Mathf.Pow(
                Mathf.Max(1f, attackGrowthMultiplier),
                Mathf.Max(0, level - 1));
        }
    }

    [CreateAssetMenu(
        fileName = "PlayerBalanceTable",
        menuName = "SimpleGame/Data/Player Balance Table")]
    public sealed class PlayerBalanceTable : ScriptableObject
    {
        [SerializeField] private List<PlayerDefinition> definitions = new();

        public IReadOnlyList<PlayerDefinition> Definitions => definitions;

        public bool TryGet(
            string playerId,
            out PlayerDefinition definition)
        {
            foreach (PlayerDefinition candidate in definitions)
            {
                if (candidate != null &&
                    candidate.Enabled &&
                    string.Equals(
                        candidate.PlayerId,
                        playerId,
                        StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public void Configure(IEnumerable<PlayerDefinition> values)
        {
            definitions = new List<PlayerDefinition>(values);
        }
    }
}
