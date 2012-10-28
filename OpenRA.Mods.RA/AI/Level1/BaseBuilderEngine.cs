using System;
using System.Linq;

namespace OpenRA.Mods.RA.AI.Level1
{
    /// <summary>
    /// Internalilze the complete interactive behavior towards the build queues etc.
    /// </summary>
    internal class BaseBuilderEngine
    {
        #region Internal Structures
        /// <summary>
        /// The internal state machine
        /// </summary>
        private enum BuildState
        {
            /// <summary>
            /// AI can actively choose an element
            /// </summary>
            ChooseItem,

            /// <summary>
            /// The builder waits for completion of a building
            /// </summary>
            WaitForProduction,

            /// <summary>
            /// The AI is thinking ;)
            /// </summary>
            WaitForFeedback
        }
        #endregion

        #region Private Fields
        /// <summary>
        /// The token in the state machine
        /// </summary>
        private BuildState state = BuildState.WaitForFeedback;

        /// <summary>
        /// The delegate to choose the next position to place a build element
        /// </summary>
        private Func<string, CPos?> chooseBuildLocation;

        /// <summary>
        /// The delegate to choose the next element to build
        /// </summary>
        private Func<ProductionQueue, ActorInfo> chooseItem;

        /// <summary>
        /// The area which elements are build
        /// </summary>
        private string category;

        /// <summary>
        /// The time when the AI thought the last time
        /// </summary>
        private int lastThinkTick;
        #endregion

        /// <summary>
        /// Initialize a base builder engine.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="chooseItem"></param>
        /// <param name="chooseBuildLocation"></param>
        public BaseBuilderEngine(string category, Func<ProductionQueue, ActorInfo> chooseItem, Func<string, CPos?> chooseBuildLocation)
        {
            this.category = category;
            this.chooseItem = chooseItem;
            this.chooseBuildLocation = chooseBuildLocation;
        }

        /// <summary>
        /// Analyze for each tick whether a element is ready, the AI has to decide what to build next or if it should idle for a while.
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="player"></param>
        /// <param name="currentAiTick"></param>
        public void Tick(Actor playerActor, Player player, int currentAiTick)
        {
            // Pick a free queue
            ProductionQueue queue = playerActor.World.ActorsWithTrait<ProductionQueue>()
                .Where(a => a.Actor.Owner == player && a.Trait.Info.Type == category)
                .Select(a => a.Trait)
                .FirstOrDefault();

            if (queue != null)
            {
                switch (state)
                {
                    case BuildState.ChooseItem:
                        ChooseItem(playerActor, currentAiTick, queue);
                        break;

                    case BuildState.WaitForProduction:
                        WaitForProduction(playerActor, player, currentAiTick, queue);
                        break;

                    case BuildState.WaitForFeedback:
                        WaitForFeedback(currentAiTick);
                        break;
                }
            }
        }

        /// <summary>
        /// Regularly change the state machine state to query AI for next element to build.
        /// </summary>
        /// <param name="currentAiTick"></param>
        private void WaitForFeedback(int currentAiTick)
        {
            if (currentAiTick - lastThinkTick > Level1AIConstants.FeedbackTime)
            {
                state = BuildState.ChooseItem;
            }
        }

        /// <summary>
        /// Waits for the production to build the item. If no production output, do nothing.
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="player"></param>
        /// <param name="currentAiTick"></param>
        /// <param name="queue"></param>
        private void WaitForProduction(Actor playerActor, Player player, int currentAiTick, ProductionQueue queue)
        {
            if (playerActor == null)
            {
                throw new ArgumentNullException("playerActor");
            }
            if (player == null)
            {
                throw new ArgumentNullException("player");
            }
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            if (this.chooseBuildLocation == null)
            {
                throw new InvalidOperationException("BaseBuilderEngine.chooseBuildLocation is not set");
            }

            ProductionItem currentBuilding = queue.CurrentItem();

            // we got something
            if (currentBuilding != null)
            {
                // unpause if paused
                if (currentBuilding.Paused)
                {
                    playerActor.World.IssueOrder(Order.PauseProduction(queue.self, currentBuilding.Item, false));
                }
                // if done, place it
                else if (currentBuilding.Done)
                {
                    state = BuildState.WaitForFeedback;

                    lastThinkTick = currentAiTick;

                    // place the building
                    CPos? location = chooseBuildLocation(currentBuilding.Item);

                    if (location == null)
                    {
                        Game.Debug(string.Format("AI: Nowhere to place {0}", currentBuilding.Item));

                        playerActor.World.IssueOrder(Order.CancelProduction(queue.self, currentBuilding.Item, 1));
                    }
                    else
                    {
                        playerActor.World.IssueOrder(new Order("PlaceBuilding", player.PlayerActor, false)
                        {
                            TargetLocation = location.Value,
                            TargetString = currentBuilding.Item
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Asks the AI for the next item to build
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="currentAiTick"></param>
        /// <param name="queue"></param>
        private void ChooseItem(Actor playerActor, int currentAiTick, ProductionQueue queue)
        {
            if (playerActor == null)
            {
                throw new ArgumentNullException("playerActor");
            }

            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            if (this.chooseItem == null)
            {
                throw new InvalidOperationException("BaseBuilderEngine.chooseItem is not set");
            }

            ActorInfo item = chooseItem(queue);

            if (item == null)
            {
                state = BuildState.WaitForFeedback;

                lastThinkTick = currentAiTick;
            }
            else
            {
                state = BuildState.WaitForProduction;

                Game.Debug(string.Format("AI: Starting production of {0}", item.Name));

                playerActor.World.IssueOrder(Order.StartProduction(queue.self, item.Name, 1));
            }
        }
    }
}
