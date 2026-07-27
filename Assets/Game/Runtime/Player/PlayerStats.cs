using UnityEngine;

namespace SimpleGame
{
    public sealed class PlayerStats : MonoBehaviour
    {
        [SerializeField] private float moveSpeedBonus;
        [SerializeField] private float attackRangeBonus;
        private PlayerDefinition definition;

        public float AttackRange =>
            definition != null
                ? definition.AttackRange + attackRangeBonus
                : PlayerController.DefaultAttackRange;
        public float RearAttackMultiplier =>
            definition != null ? definition.RearAttackMultiplier : 3f;
        public float MoveSpeed =>
            definition != null
                ? definition.BaseMoveSpeed + moveSpeedBonus
                : PlayerMovement.DefaultMoveSpeed;
        public float PathEnemyApproachSpeedMultiplier =>
            definition != null
                ? definition.PathEnemyApproachSpeedMultiplier
                : 1.1f;
        public float PostKillEscapeSpeedMultiplier =>
            definition != null
                ? definition.PostKillEscapeSpeedMultiplier
                : 1.2f;
        public float MoveArrivalTolerance =>
            definition != null
                ? definition.MoveArrivalTolerance
                : PlayerMovement.DefaultArrivalTolerance;

        public void Configure(PlayerDefinition configuredDefinition)
        {
            definition = configuredDefinition;
            moveSpeedBonus = 0f;
            attackRangeBonus = 0f;
        }

        public float GetAttackPower(int playerLevel)
        {
            return definition != null
                ? definition.CalculateAttackPower(playerLevel)
                : 1f;
        }

        public void AddMoveSpeed(float amount)
        {
            moveSpeedBonus += Mathf.Max(0f, amount);
        }

        public void AddAttackRange(float amount)
        {
            attackRangeBonus = Mathf.Max(
                0f,
                attackRangeBonus + amount);
        }

    }
}
