namespace SimpleGame
{
    public static class CombatFeedbackResolver
    {
        public static CombatFeedbackLevel Resolve(
            bool damageApplied,
            bool critical,
            PlayerAttackReaction playerReaction)
        {
            if (damageApplied && critical)
            {
                return CombatFeedbackLevel.CriticalHit;
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
