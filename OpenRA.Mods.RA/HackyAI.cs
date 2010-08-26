using System.Linq;
using OpenRA.Traits;
using System;
using System.Collections.Generic;


//TODO:
// [y] never give harvesters orders
// maybe move rally points when a rally point gets blocked (by units or buildings)
// Don't send attack forces to your own spawn point
// effectively clear the area around the production buildings' spawn points.
// don't spam the build unit button, only queue one unit then wait for the backoff period.
//    just make the build unit action only occur once every second.
// build defense buildings

// later:
// don't build units randomly, have a method to it.
// explore spawn points methodically
// once you find a player, attack the player instead of spawn points.

namespace OpenRA.Mods.RA
{
	class HackyAIInfo : TraitInfo<HackyAI> { }

	/* a pile of hacks, which control a local player on the host. */

	class HackyAI : ITick, IBot
	{
		bool enabled;
		int ticks;
		Player p;
		PlayerResources playerResources;
		int2 baseCenter;
        Random random = new Random(); //we do not use the synced random number generator.

		Dictionary<string, float> buildingFractions = new Dictionary<string, float>
		{
			{ "proc", .2f },
			{ "barr", .05f },
			{ "tent", .05f },
			{ "weap", .05f },
			{ "atek", .01f },
			{ "stek", .01f },
			{ "silo", .05f },
			{ "fix", .01f },
			{ "hpad", .01f },
			{ "afld", .01f },
			{ "dome", .01f },
		};
		
		enum BuildState
		{
			ChooseItem,
			WaitForProduction,
			WaitForFeedback,
		}

		int lastThinkTick = 0;

		const int MaxBaseDistance = 15;

		BuildState state = BuildState.WaitForFeedback;

		/* called by the host's player creation code */
		public void Activate(Player p)
		{
			this.p = p;
			enabled = true;
		
			playerResources = p.PlayerActor.Trait<PlayerResources>();
		}

		int GetPowerProvidedBy(string building)
		{
			var bi = Rules.Info[building].Traits.GetOrDefault<BuildingInfo>();
			if (bi == null) return 0;
			return bi.Power;
		}

        string ChooseRandomUnitToBuild(string category)
        {
            var buildableThings = Rules.TechTree.BuildableItems(p, category).ToArray();
            if (buildableThings.Length == 0) return null;
            return buildableThings[random.Next(buildableThings.Length)];
        }

		string ChooseBuildingToBuild()
		{
			var buildableThings = Rules.TechTree.BuildableItems(p, "Building").ToArray();

			if (playerResources.PowerProvided <= playerResources.PowerDrained * 1.2)	/* try to maintain 20% excess power */
			{
				/* find the best thing we can build which produces power */
				var best = buildableThings.Where(a => GetPowerProvidedBy(a) > 0)
					.OrderByDescending(a => GetPowerProvidedBy(a)).FirstOrDefault();

				if (best != null)
				{
					Game.Debug("AI: Need more power, so {0} is best choice.".F(best));
					return best;
				}
				else
					Game.Debug("AI: Need more power, but can't build anything that produces it.");
			}

			var myBuildings = p.World.Queries.OwnedBy[p].WithTrait<Building>()
				.Select( a => a.Actor.Info.Name ).ToArray();

			foreach (var frac in buildingFractions)
				if (buildableThings.Contains(frac.Key))
					if (myBuildings.Count(a => a == frac.Key) < frac.Value * myBuildings.Length)
						return frac.Key;

			return null;
		}

		int2? ChooseBuildLocation(ProductionItem item)
		{
			var bi = Rules.Info[item.Item].Traits.Get<BuildingInfo>();

			for (var k = 0; k < MaxBaseDistance; k++)
				foreach (var t in Game.world.FindTilesInCircle(baseCenter, k))
					if (Game.world.CanPlaceBuilding(item.Item, bi, t, null))
						if (Game.world.IsCloseEnoughToBase(p, item.Item, bi, t))
							return t;

			return null;		// i don't know where to put it.
		}

		const int feedbackTime = 30;		// ticks; = a bit over 1s. must be >= netlag.

		public void Tick(Actor self)
		{
			if (!enabled)
				return;

			ticks++;

            if (ticks == 10)
            {
                DeployMcv(self);
            }

            if (ticks % feedbackTime == 0)
            {
                //about once every second, perform unintelligent cleanup tasks.
                //e.g. ClearAreaAroundSpawnPoints();
                //e.g. start repairing damaged buildings.
                BuildRandom("Vehicle");
                BuildRandom("Infantry");
                BuildRandom("Plane");
            }

            AssignRolesToIdleUnits(self);
            SetRallyPointsForNewProductionBuildings(self);


            BuildBuildings();
            //build Defense
            //build Ship
		}

        //hacks etc sigh mess.
        //A bunch of hardcoded lists to keep track of which units are doing what.
        List<Actor> unitsHangingAroundTheBase = new List<Actor>();
        List<Actor> attackForce = new List<Actor>();

        //Units that the ai already knows about. Any unit not on this list needs to be given a role.
        List<Actor> activeUnits = new List<Actor>();

        //This is purely to identify production buildings that don't have a rally point set.
        List<Actor> activeProductionBuildings = new List<Actor>();

        private void AssignRolesToIdleUnits(Actor self)
        {
			//HACK: trim these lists -- we really shouldn't be hanging onto all this state
			//when it's invalidated so easily, but that's Matthew/Alli's problem.
			activeUnits.RemoveAll(a => a.Destroyed);
			unitsHangingAroundTheBase.RemoveAll(a => a.Destroyed);
			attackForce.RemoveAll(a => a.Destroyed);
			activeProductionBuildings.RemoveAll(a => a.Destroyed);

            //don't select harvesters.
            var newUnits = self.World.Queries.OwnedBy[p]
                .Where(a => ((a.Info.Category == "Infantry" || a.Info.Category == "Vehicle")
                    && a.Info != Rules.Info["harv"]
                    && !activeUnits.Contains(a))).ToArray();

            foreach (var a in newUnits)
            {
                Game.Debug("AI: Found a newly built unit");
                unitsHangingAroundTheBase.Add(a);
                activeUnits.Add(a);
            }

            /* Create an attack force when we have enough units around our base. */
            // (don't bother leaving any behind for defense.)
            if (unitsHangingAroundTheBase.Count > 5)
            {
                Game.Debug("Launch an attack.");
                int2[] spawnPoints = Game.world.Map.SpawnPoints.ToArray();
                // At the start of the game, all you can do is investigate each spawn point
                // until you learn where some other players are.
                // this sometimes sends an attack to the bot's own spawn point,
                // which is a leading cause of blocking the spawn points :(
                int2 attackTarget = spawnPoints[random.Next(0, spawnPoints.Length)];
                foreach (var a in unitsHangingAroundTheBase)
					if (TryToMove(a, attackTarget))
						attackForce.Add(a);
                unitsHangingAroundTheBase.Clear();
            }
        }

        private void SetRallyPointsForNewProductionBuildings(Actor self)
        {
            var newProdBuildings = self.World.Queries.OwnedBy[p]
                .Where(a => (a.Info.Category == "Building"
                    && a.TraitOrDefault<RallyPoint>() != null
                    && !activeProductionBuildings.Contains(a))).ToArray();

            foreach (var a in newProdBuildings)
            {
                activeProductionBuildings.Add(a);
                int2 newRallyPoint = ChooseRallyLocationNear(a.Location);
                Game.IssueOrder(new Order("SetRallyPoint", a, newRallyPoint));
            }
        }

        //won't work for shipyards...
        private int2 ChooseRallyLocationNear(int2 startPos)
        {
            foreach (var t in Game.world.FindTilesInCircle(startPos, 6))
                if (Game.world.IsCellBuildable(t, false) && t != startPos)
                        return t;

            return startPos;		// i don't know where to put it.
        }

        //try very hard to find a valid move destination near the target.
        //(Don't accept a move onto the subject's current position. maybe this is already not allowed? )
        private bool TryToMove(Actor a, int2 desiredMoveTarget)
        {
			if (!a.HasTrait<IMove>())
				return false;

            int2 xy;
            int loopCount = 0; //avoid infinite loops.
            int range = 2;
            do
            {
                //loop until we find a valid move location
                xy = new int2(desiredMoveTarget.X + random.Next(-range, range), desiredMoveTarget.Y + random.Next(-range, range));
                loopCount++;
                range = Math.Max(range, loopCount / 2);
                if (loopCount > 10) return false;
            } while (!a.Trait<IMove>().CanEnterCell(xy) && xy != a.Location);
            Game.IssueOrder(new Order("Move", a, xy));
            return true;
        }

        private void DeployMcv(Actor self)
        {
			/* find our mcv and deploy it */
            var mcv = self.World.Queries.OwnedBy[p]
                .FirstOrDefault(a => a.Info == Rules.Info["mcv"]);

            if (mcv != null)
            {
                baseCenter = mcv.Location;
                Game.IssueOrder(new Order("DeployTransform", mcv));
            }
            else
                Game.Debug("AI: Can't find the MCV.");
        }

        //Build a random unit of the given type. Not going to be needed once there is actual AI...
        private void BuildRandom(string category)
        {
			// Pick a free queue
			var queue = Game.world.Queries.WithTraitMultiple<ProductionQueue>()
				.Where(a => a.Actor.Owner == p &&
				       a.Trait.Info.Type == category &&
				       a.Trait.CurrentItem() == null)
				.Select(a => a.Trait)
				.FirstOrDefault();
			
			if (queue == null)
				return;
			
			var unit = ChooseRandomUnitToBuild(category);
			if (unit != null)
			{
				Game.IssueOrder(Order.StartProduction(queue.self, unit, 1));
			}
        }

        private void BuildBuildings()
        {
            // Pick a free queue
			var queue = Game.world.Queries.WithTraitMultiple<ProductionQueue>()
				.Where(a => a.Actor.Owner == p && a.Trait.Info.Type == "Building")
				.Select(a => a.Trait)
				.FirstOrDefault();
			
			var currentBuilding = queue.CurrentItem();
            switch (state)
            {
                case BuildState.ChooseItem:
                    {
                        var item = ChooseBuildingToBuild();
                        if (item == null)
                        {
                            state = BuildState.WaitForFeedback;
                            lastThinkTick = ticks;
                        }
                        else
                        {
                            state = BuildState.WaitForProduction;
                            Game.IssueOrder(Order.StartProduction(queue.self, item, 1));
                        }
                    }
                    break;

                case BuildState.WaitForProduction:
                    if (currentBuilding == null) return;	/* let it happen.. */

                    else if (currentBuilding.Paused)
                        Game.IssueOrder(Order.PauseProduction(queue.self, currentBuilding.Item, false));
                    else if (currentBuilding.Done)
                    {
                        state = BuildState.WaitForFeedback;
                        lastThinkTick = ticks;

                        /* place the building */
                        var location = ChooseBuildLocation(currentBuilding);
                        if (location == null)
                        {
                            Game.Debug("AI: Nowhere to place {0}".F(currentBuilding.Item));
                            Game.IssueOrder(Order.CancelProduction(queue.self, currentBuilding.Item));
                        }
                        else
                        {
                            Game.IssueOrder(new Order("PlaceBuilding", p.PlayerActor, location.Value, currentBuilding.Item));
                        }
                    }
                    break;

                case BuildState.WaitForFeedback:
                    if (ticks - lastThinkTick > feedbackTime)
                        state = BuildState.ChooseItem;
                    break;
            }
        }
	}
}
