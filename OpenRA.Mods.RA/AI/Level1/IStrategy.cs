namespace OpenRA.Mods.RA.AI.Level1
{
    /// <summary>
    /// Individual strategy working as part of the AI.
    /// </summary>
    /// <remarks>
    /// Dev: Never saw a better usage of a strategy pattern.
    /// </remarks>
    internal interface IStrategy
    {
        /// <summary>
        /// The order within the tick.
        /// </summary>
        int OrderDuringTick { get; }

        /// <summary>
        /// Provides the strategy a calculation momenet within the AI.
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="aiPlayer"></param>
        /// <param name="ticks"></param>
        void Tick(Actor playerActor, Player aiPlayer, int ticks);
    }
}
