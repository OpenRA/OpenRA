using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.AI.Level1
{
    /// <summary>
    /// Develops a full base
    /// </summary>
    /// <remarks>
    /// The strategy here is to develop a full base including all technology building if possible. Specialized bases (e.g. harvesting bases or sea bases) are other strategies.
    ///
    /// Implemented:
    /// - locate mcv and perform initial deploy
    /// - build buildings
    /// - support ratio for derivate AIs
    /// 
    /// TODO
    /// - relocate mcv first to better location (space and smell of ore)
    /// - do not build on top of ore mines
    /// </remarks>
    internal class HackyAIBaseBuildingStrategy : IStrategy
    {
        #region Internal Fields
        private Actor playerActor;
        private Player aiPlayer;
        private PowerManager playerPower;
        // TODO: is this actor cacheable
        private Actor baseMcv;
        private CPos locationOfBase;
        private BaseBuilderEngine baseBuilderEngine;
        private BaseBuilderEngine baseDefenseBuilderEngine;

        private IDictionary<string, float> buildingFractions;
        #endregion

        #region Constructor
        /// <summary>
        /// Factorize the strategy
        /// </summary>
        /// <param name="buildingFractions">The fractions what type of buildings should be built.</param>
        public HackyAIBaseBuildingStrategy(IDictionary<string, float> buildingFractions)
        {
            baseBuilderEngine = new BaseBuilderEngine("Building", x => ChooseBuildingItem(x, true), ChooseBuildingLocation);
            baseDefenseBuilderEngine = new BaseBuilderEngine("Defense", x => ChooseBuildingItem(x, false), ChooseBuildingLocation);

            this.buildingFractions = buildingFractions;
        }
        #endregion

        /// <summary>
        /// Very late in each AI operation.
        /// </summary>
        public int OrderDuringTick
        {
            get { return 90; }
        }

        /// <summary>
        /// Receives a tick from the AI.
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="aiPlayer"></param>
        /// <param name="ticks">tick counter of the AI.</param>
        public void Tick(Actor playerActor, Player aiPlayer, int ticks)
        {
            if (playerActor == null)
            {
                throw new ArgumentNullException("playerActor");
            }
            if (aiPlayer == null)
            {
                throw new ArgumentNullException("aiPlayer");
            }

            // TODO: Initable? can actors be cached.
            this.playerActor = playerActor;
            this.aiPlayer = aiPlayer; 
            
            if (baseMcv == null)
            {
                playerPower = aiPlayer.PlayerActor.Trait<PowerManager>();

                InitialDeployMcv(playerActor, aiPlayer);
            }

            baseBuilderEngine.Tick(playerActor, aiPlayer, ticks);
            baseDefenseBuilderEngine.Tick(playerActor, aiPlayer, ticks);
        }

        #region Deployment of Base
        /// <summary>
        /// Deploy a mcv initially.
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="aiPlayer"></param>
        private void InitialDeployMcv(Actor playerActor, Player aiPlayer)
        {
            if (playerActor == null)
            {
                throw new ArgumentNullException("playerActor");
            }
            if (aiPlayer == null)
            {
                throw new ArgumentNullException("aiPlayer");
            }

            // find our mcv and deploy it
            Actor mcv = playerActor.World.Actors.FirstOrDefault(a => a.Owner == aiPlayer && a.HasTrait<BaseBuilding>());

            if (mcv != null)
            {
                locationOfBase = mcv.Location;

                // Don'position transform the mcv if it is a fact
                if (mcv.HasTrait<Mobile>())
                {
                    playerActor.World.IssueOrder(new Order("DeployTransform", mcv, false));

                    baseMcv = mcv;
                }
            }
            else
            {
                Game.Debug("AI: Can't find BaseBuildUnit.");
            }
        }
        #endregion

        #region Building Development
        /// <summary>
        /// Select an element according to the propertional amount considering the availability of energy.
        /// </summary>
        /// <param name="queue"></param>
        /// <param name="calculatePower">A flag whether the element should try building energy buildings.</param>
        /// <returns></returns>
        public ActorInfo ChooseBuildingItem(ProductionQueue queue, bool calculatePower)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            IEnumerable<ActorInfo> buildableThings = queue.BuildableItems();

            ActorInfo result = null;
            if (!HasAdequatePower())
            {
                if (calculatePower == true)
                {
                    /* find the best thing we can build which produces result */
                    result = buildableThings.Where(a => RetrievePowerProvidedBy(a) > 0)
                        .OrderByDescending(a => RetrievePowerProvidedBy(a)).FirstOrDefault();
                }
            }
            else
            {
                string[] myBuildings = playerActor.World
                    .ActorsWithTrait<Building>()
                    .Where(a => a.Actor.Owner == aiPlayer)
                    .Select(a => a.Actor.Info.Name).ToArray();

                foreach (KeyValuePair<string, float> fraction in buildingFractions)
                {
                    if (buildableThings.Any(b => b.Name == fraction.Key))
                    {
                        if (myBuildings.Count(a => a == fraction.Key) < fraction.Value * myBuildings.Length &&
                            playerPower.ExcessPower >= Rules.Info[fraction.Key].Traits.Get<BuildingInfo>().Power)
                        {
                            result = Rules.Info[fraction.Key];

                            break;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Retrieves the power provided by a building
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        private int RetrievePowerProvidedBy(ActorInfo building)
        {
            if (building == null)
            {
                throw new ArgumentNullException("building");
            }

            BuildingInfo buildingInfo = building.Traits.GetOrDefault<BuildingInfo>();

            int result = 0;

            if (buildingInfo != null)
            {
                result = buildingInfo.Power;
            }

            return result;
        }

        /// <summary>
        /// Checks whether there is enough energy (first priority in building).
        /// </summary>
        /// <returns></returns>
        private bool HasAdequatePower()
        {
            /* note: CNC `fact` provides a small amount of result. don'position get jammed because of that. */
            return playerPower.PowerProvided > 50 
                && playerPower.PowerProvided > playerPower.PowerDrained * Level1AIConstants.ExcessInPower;
        }


        /// <summary>
        /// Finds the location based on the hacky approach by just placing it to the closest place.
        /// </summary>
        /// <remarks>
        /// Returns null if it does not find a nice place.
        /// </remarks>
        /// <param name="actorType"></param>
        /// <returns></returns>
        public CPos? ChooseBuildingLocation(string actorType)
        {
            if (string.IsNullOrEmpty(actorType))
            {
                throw new ArgumentException("actorType cannot be empty", "actorType");
            }

            BuildingInfo buildingInfo = Rules.Info[actorType].Traits.Get<BuildingInfo>();

            CPos? result = null;

            foreach (CPos position in playerActor.World.FindTilesInCircle(locationOfBase, Level1AIConstants.MaxBaseDistance))
            {
                if (playerActor.World.CanPlaceBuilding(actorType, buildingInfo, position, null))
                {
                    if (buildingInfo.IsCloseEnoughToBase(playerActor.World, aiPlayer, actorType, position))
                    {
                        IEnumerable<CPos> occupiedCellsOfTentativeBuilding = Util.ExpandFootprint(FootprintUtils.Tiles(actorType, buildingInfo, position), false);

                        if (NoBuildingInfluence(playerActor, occupiedCellsOfTentativeBuilding))
                        {
                            result = position;

                            break;
                        }
                    }
                }
            }

            return result;		
        }

        /// <summary>
        /// TODO whatever building influence is but block to build there ;)
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="cells"></param>
        /// <returns></returns>
        private bool NoBuildingInfluence(Actor playerActor, IEnumerable<CPos> cells)
        {
            if (playerActor == null)
            {
                throw new ArgumentNullException("playerActor");
            }

            if (cells == null)
            {
                throw new ArgumentNullException("cells");
            }

            BuildingInfluence bi = playerActor.World.WorldActor.Trait<BuildingInfluence>();

            return cells.All(c => bi.GetBuildingAt(c) == null);
        }
        #endregion
    }
}
