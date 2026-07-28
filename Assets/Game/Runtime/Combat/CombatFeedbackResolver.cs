namespace SimpleGame
{
    public static class CombatFeedbackResolver
    {
        public static CombatFeedbackLevel Resolve(
            bool damageApplied,
            bool targetDefeated,
            bool critical,
            PlayerAttackReaction playerReaction)
        {
            if (damageApplied && critical)
            {
                return CombatFeedbackLevel.CriticalHit;
            }

            if (damageApplied && targetDefeated)
            {
                return CombatFeedbackLevel.DefeatingHit;
            }

            if (playerReaction == PlayerAttackReaction.Recoil)
            {
                return CombatFeedbackLevel.FrontRecoil;
            }

            return damageApplied
                ? CombatFeedbackLevel.NormalHit
                : CombatFeedbackLevel.None;
        }
    }
}
