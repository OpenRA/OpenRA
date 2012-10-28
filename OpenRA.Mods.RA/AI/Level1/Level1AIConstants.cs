namespace OpenRA.Mods.RA.AI.Level1
{
    /// <summary>
    /// Level 1 AI Constants. Some where part of the HackyAI Info but where never used.
    /// </summary>
    internal static class Level1AIConstants
    {
        /// <summary>
        /// The amount of time the AI thinks after a job is done.
        /// </summary>
        public const int FeedbackTime = 30;

        /// <summary>
        /// Maximum distance of base.
        /// </summary>
        public const int MaxBaseDistance = 20;

        /// <summary>
        /// The percentage of drained energy should be available.
        /// </summary>
        public const float ExcessInPower = 1.2f;

        /// <summary>
        /// The interval in which roles are assigned to the units (in ticks)
        /// </summary>
        public const int AssignRolesInterval = 20;
    }
}
