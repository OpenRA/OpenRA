using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.AI.Level1
{
    /// <summary>
    /// A first level of AI development
    /// </summary>
    /// <remarks>
    /// Targets:
    /// - Only be able to make decisions like a human (e.g. you can know what the smoke has given to you already)
    /// - Plugable for strategies (like a human, the AI follows a set of parallel running strategies)
    /// </remarks>
    internal class Level1AI : ITick, IBot, INotifyDamage
    {
        #region Private Fields
        /// <summary>
        /// The information describing the AI actor.
        /// </summary>
        private Level1AIInfo info;

        /// <summary>
        /// The AI is enabled for a player
        /// </summary>
        private bool aiEnabled = false;

        /// <summary>
        /// The player which is managed by this AI.
        /// </summary>
        private Player player;

        /// <summary>
        /// Counting the ticks received by the game engine to make dependent on time
        /// </summary>
        private int ticks;

        /// <summary>
        /// The list of all strategies currently followed by this AI.
        /// </summary>
        private List<IStrategy> activeStrategies;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a Level1AI. The factory is the Level1AIInfo
        /// </summary>
        /// <param name="info"></param>
        public Level1AI(Level1AIInfo info)
        {
            this.info = info;
        }
        #endregion

        #region IBot Members
        /// <summary>
        /// Intialize the AI for the provided player.
        /// </summary>
        /// <param name="player">The player which should be managed by the AI</param>
        public void Activate(Player player)
        {
            if (player == null)
            {
                throw new ArgumentNullException("player");
            }

            this.player = player;

            aiEnabled = true;

            Statistics statistics = new Statistics();

            activeStrategies = new List<IStrategy>()
            {
                new HackyAIBaseBuildingStrategy(info.BuildingFractions),
                new HackyAIUnitBuildingStrategy(info.UnitsToBuild),
                new HackyAIDamageRepairStrategy(info.ShouldRepairBuildings, statistics),
                new HackyAIDefineRallyPointOfProductionBuildingsStrategy(),
                new HackyAIAttackStrategy(info.SquadSize, statistics),
            }
                .OrderBy(x => x.OrderDuringTick)
                .ToList();
        }

        /// <summary>
        /// The BotInfo for this IBot
        /// </summary>
        public IBotInfo Info
        {
            get { return info; }
        }
        #endregion

        #region ITick Members
        /// <summary>
        /// A tick from the game engine to compute the AI behavior.
        /// </summary>
        /// <remarks>
        /// self is the player's actor
        /// </remarks>
        /// <param name="self"></param>
        public void Tick(Actor self)
        {
            if (self == null)
            {
                throw new ArgumentNullException("self");
            }

            if (aiEnabled)
            {
                IncreaseTicks();

                foreach (IStrategy strategy in activeStrategies)
                {
                    strategy.Tick(self, player, ticks);
                }
            }
        }
        #endregion

        #region INotifyDamage Members
        /// <summary>
        /// Forward the damage notification to the damage management strategies.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="e"></param>
        public void Damaged(Actor self, AttackInfo e)
        {
            if (aiEnabled)
            {
                foreach (IStrategy strategy in activeStrategies)
                {
                    IDamageStrategy damageStrategy = strategy as IDamageStrategy;

                    if (damageStrategy != null)
                    {
                        damageStrategy.Damaged(self, e);
                    }
                }
            }
        }
        #endregion

        #region Helper
        /// <summary>
        /// Increase the tick counter for this AI.
        /// </summary>
        private void IncreaseTicks()
        {
            ticks++;
        }

        #endregion
    }
}
