using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.RA.AI.Level1
{
    /// <summary>
    /// The Unit Building strategy of the Hacky AI
    /// </summary>
    internal class HackyAIUnitBuildingStrategy : IStrategy
    {
        #region Private fields
        /// <summary>
        /// The in-game random number generator
        /// </summary>
        private OpenRA.Thirdparty.Random random = new OpenRA.Thirdparty.Random(); //we do not use the synced random number generator.
        /// <summary>
        /// the fractions of units to build
        /// </summary>
        private IDictionary<string, float> unitsToBuild;
        #endregion

        #region Constructor
        /// <summary>
        /// Factorize this strategy
        /// </summary>
        /// <param name="unitsToBuild"></param>
        public HackyAIUnitBuildingStrategy(IDictionary<string, float> unitsToBuild)
        {
            this.unitsToBuild = unitsToBuild;
        }
        #endregion

        #region IStrategy Members
        /// <summary>
        /// Early in each AI operation.
        /// </summary>
        public int OrderDuringTick
        {
            get { return 40; }
        }

        /// <summary>
        /// Build in each feedback iteration one new unit
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="aiPlayer"></param>
        /// <param name="ticks"></param>
        public void Tick(Actor playerActor, Player aiPlayer, int ticks)
        {
            if (ticks % Level1AIConstants.FeedbackTime == 0)
            {
                BuildRandom(playerActor, aiPlayer, "Vehicle");
                BuildRandom(playerActor, aiPlayer, "Infantry");
            }
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Build a random unit of the given type. Not going to be needed once there is actual AI...
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="aiPlayer"></param>
        /// <param name="category"></param>
        private void BuildRandom(Actor playerActor, Player aiPlayer, string category)
        {
            if (playerActor == null)
            {
                throw new ArgumentNullException("playerActor");
            }
            if (aiPlayer == null)
            {
                throw new ArgumentNullException("aiPlayer");
            }
            if (string.IsNullOrEmpty(category))
            {
                throw new ArgumentException("category cannot be empty", "category");
            }

            // Pick a free queue
            ProductionQueue queue = playerActor.World.ActorsWithTrait<ProductionQueue>()
                .Where(a => a.Actor.Owner == aiPlayer && a.Trait.Info.Type == category)
				.Select(a => a.Trait)
                .FirstOrDefault(q => q.CurrentItem() == null);

            if (queue != null)
            {
                ActorInfo unit = ChooseRandomUnitToBuild(queue);

                if (unit != null && unitsToBuild.Any(u => u.Key == unit.Name))
                {
                    playerActor.World.IssueOrder(Order.StartProduction(queue.self, unit.Name, 1));
                }
            }
        }

        /// <summary>
        /// Chooses a unit based on a random generator.
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        private ActorInfo ChooseRandomUnitToBuild(ProductionQueue queue)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            IEnumerable<ActorInfo> buildableThings = queue.BuildableItems();

            ActorInfo result = null;

            if (buildableThings.Any())
            {
                result = buildableThings.ElementAtOrDefault(random.Next(buildableThings.Count()));
            }

            return result;
        }
        #endregion
    }
}
