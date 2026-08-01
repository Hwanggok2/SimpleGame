namespace SimpleGame
{
    public readonly struct PauseDetailsData
    {
        public PauseDetailsData(
            string playerOverview,
            string accountOverview,
            string stats,
            string skills)
        {
            PlayerOverview = playerOverview ?? string.Empty;
            AccountOverview = accountOverview ?? string.Empty;
            Stats = stats ?? string.Empty;
            Skills = skills ?? string.Empty;
        }

        public string PlayerOverview { get; }
        public string AccountOverview { get; }
        public string Stats { get; }
        public string Skills { get; }
    }
}
