using OpenRA.FileFormats;

namespace OpenRA.Mods.RA.AI.Level1
{
    /// <summary>
    /// Statistics information collected by the AI.
    /// </summary>
    internal class Statistics
    {
        /// <summary>
        /// List of all enemies.
        /// </summary>
        public Cache<Player, StatisticsEnemy> Aggressors = new Cache<Player, StatisticsEnemy>(_ => new StatisticsEnemy());
    }
}
