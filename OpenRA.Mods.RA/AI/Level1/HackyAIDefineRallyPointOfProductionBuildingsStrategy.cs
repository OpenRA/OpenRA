using System;
using System.Linq;
using OpenRA.Mods.RA.Buildings;
using XRandom = OpenRA.Thirdparty.Random;

namespace OpenRA.Mods.RA.AI.Level1
{
    /// <summary>
    /// Sets the rally points of production building (the location where the units go to after they are build) 
    /// and maintain them (e.g. build over the rally point)
    /// </summary>
    /// <remarks>
    /// The HackyAI does this a lot ... also used to scather the units among the base.
    /// </remarks>
    internal class HackyAIDefineRallyPointOfProductionBuildingsStrategy : IStrategy
    {
        #region Private Fields
        private XRandom random = new XRandom();
        private readonly BuildingInfo rallypointTestBuilding; // temporary hack
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize the rally point definer
        /// </summary>
        public HackyAIDefineRallyPointOfProductionBuildingsStrategy()
        {
            // temporary hack.
            this.rallypointTestBuilding = Rules.Info["fact"].Traits.Get<BuildingInfo>();
        }
        #endregion

        #region IStrategy Members
        /// <summary>
        /// Very late in the AI circle
        /// </summary>
        public int OrderDuringTick
        {
            get { return 80; }
        }

        /// <summary>
        /// Check during each AI cycle if still all rally points are valid.
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="aiPlayer"></param>
        /// <param name="ticks"></param>
        public void Tick(Actor playerActor, Player aiPlayer, int ticks)
        {
            SetRallyPointsForNewProductionBuildings(playerActor, aiPlayer);
        }
        #endregion

        #region Helper
        /// <summary>
        /// Check if each production building has a valid rally point (e.g. not build over)
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="aiPlayer"></param>
        private void SetRallyPointsForNewProductionBuildings(Actor playerActor, Player aiPlayer)
        {
            if (playerActor == null)
            {
                throw new ArgumentNullException("playerActor");
            }
            if (aiPlayer == null)
            {
                throw new ArgumentNullException("aiPlayer");
            }

            TraitPair<RallyPoint>[] buildings = playerActor.World.ActorsWithTrait<RallyPoint>()
                .Where(rp => rp.Actor.Owner == aiPlayer &&
                    !IsRallyPointValid(playerActor, rp.Trait.rallyPoint)).ToArray();

            if (buildings.Length > 0)
            {
                Game.Debug("Bot {0} needs to find rallypoints for {1} buildings.", aiPlayer.PlayerName, buildings.Length);
            }

            foreach (TraitPair<RallyPoint> a in buildings)
            {
                CPos newRallyPoint = ChooseRallyLocationNear(playerActor, a.Actor.Location);

                playerActor.World.IssueOrder(new Order("SetRallyPoint", a.Actor, false) { TargetLocation = newRallyPoint });
            }
        }

        //won'position work for shipyards...
        /// <summary>
        /// Take any cell close to the production building ignoring water etc.
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="startPos"></param>
        /// <returns></returns>
        private CPos ChooseRallyLocationNear(Actor playerActor, CPos startPos)
        {
            if (playerActor == null)
            {
                throw new ArgumentNullException("playerActor");
            }

            CPos[] possibleRallyPoints = playerActor.World.FindTilesInCircle(startPos, 8).Where(x => IsRallyPointValid(playerActor, x)).ToArray();

            CPos result;

            if (possibleRallyPoints.Length == 0)
            {
                Game.Debug("Bot Bug: No possible rallypoint near {0}", startPos);

                result = startPos;
            }
            else
            {
                result = possibleRallyPoints.Random(random);
            }

            return result;
        }

        /// <summary>
        /// Check whether the selected rally build is buildable (e.g. no water etc.)
        /// </summary>
        /// <param name="playerActor"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private bool IsRallyPointValid(Actor playerActor, CPos x)
        {
            // this is actually WRONG as soon as HackyAI is building units with a variety of
            // movement capabilities. (has always been wrong)
            return playerActor.World.IsCellBuildable(x, rallypointTestBuilding);
        }
        #endregion
    }
}
