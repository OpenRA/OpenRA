#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;
using XRandom = OpenRA.Thirdparty.Random;
using OpenRA.FileFormats;


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
	class HackyAIInfo : ITraitInfo
	{
		[FieldLoader.LoadUsing( "LoadUnits" )]
		public readonly Dictionary<string, float> UnitsToBuild;

		[FieldLoader.LoadUsing( "LoadBuildings" )]
		public readonly Dictionary<string, float> BuildingFractions;
		
		static object LoadUnits( MiniYaml y )
		{
			Dictionary<string,float> ret = new Dictionary<string, float>();
			foreach (var t in y.NodesDict["UnitsToBuild"].Nodes)
				ret.Add(t.Key, (float)FieldLoader.GetValue("units", typeof(float), t.Value.Value));			
			return ret;
		}
		
		static object LoadBuildings( MiniYaml y )
		{
			Dictionary<string,float> ret = new Dictionary<string, float>();
			foreach (var t in y.NodesDict["BuildingFractions"].Nodes)
				ret.Add(t.Key, (float)FieldLoader.GetValue("units", typeof(float), t.Value.Value));			
			return ret;
		}
		
		public object Create(ActorInitializer init) { return new HackyAI(this); }
	}

	/* a pile of hacks, which control a local player on the host. */

	class HackyAI : ITick, IBot
	{
		bool enabled;
		int ticks;
		Player p;
		PowerManager playerPower;
		
		int2 baseCenter;
        XRandom random = new XRandom(); //we do not use the synced random number generator.

		World world { get { return p.PlayerActor.World; } }

		readonly HackyAIInfo Info;
		public HackyAI(HackyAIInfo Info)
		{
			this.Info = Info;
		}
				
		enum BuildState
		{
			ChooseItem,
			WaitForProduction,
			WaitForFeedback,
		}

		int lastThinkTick = 0;

		const int MaxBaseDistance = 15;

		BuildState bstate = BuildState.WaitForFeedback;
        BuildState dstate = BuildState.WaitForFeedback;

		public static void BotDebug(string s, params object[] args)
		{
			if (Game.Settings.Debug.BotDebug)
				Game.Debug(s, args);
		}

		/* called by the host's player creation code */
		public void Activate(Player p)
		{
			this.p = p;
			enabled = true;
			playerPower = p.PlayerActor.Trait<PowerManager>();
		}

		int GetPowerProvidedBy(ActorInfo building)
		{
			var bi = building.Traits.GetOrDefault<BuildingInfo>();
			if (bi == null) return 0;
			return bi.Power;
		}

        ActorInfo ChooseRandomUnitToBuild(ProductionQueue queue)
        {
            var buildableThings = queue.BuildableItems();
            if (buildableThings.Count() == 0) return null;
            return buildableThings.ElementAtOrDefault(random.Next(buildableThings.Count()));
        }

		bool HasAdequatePower()
		{
			/* note: CNC `fact` provides a small amount of power. don't get jammed because of that. */
			return playerPower.PowerProvided > 50 && 
				playerPower.PowerProvided > playerPower.PowerDrained * 1.2;
		}

		ActorInfo ChooseBuildingToBuild(ProductionQueue queue)
		{
			var buildableThings = queue.BuildableItems();

			if (!HasAdequatePower())	/* try to maintain 20% excess power */
			{
				/* find the best thing we can build which produces power */
				var best = buildableThings.Where(a => GetPowerProvidedBy(a) > 0)
					.OrderByDescending(a => GetPowerProvidedBy(a)).FirstOrDefault();

				if (best != null)
				{
					BotDebug("AI: Need more power, so {0} is best choice.", best.Name);
					return best;
				}
				else
					BotDebug("AI: Need more power, but can't build anything that produces it.");
			}

			var myBuildings = p.World.Queries.OwnedBy[p].WithTrait<Building>()
				.Select( a => a.Actor.Info.Name ).ToArray();
            
            
			foreach (var frac in Info.BuildingFractions)
				if (buildableThings.Any(b => b.Name == frac.Key))
					if (myBuildings.Count(a => a == frac.Key) < frac.Value * myBuildings.Length)
						return Rules.Info[frac.Key];

			return null;
		}

        ActorInfo ChooseDefenseToBuild(ProductionQueue queue)
        {
            var buildableThings = queue.BuildableItems();

            var myBuildings = p.World.Queries.OwnedBy[p].WithTrait<Building>()
                .Select(a => a.Actor.Info.Name).ToArray();

            foreach (var frac in Info.BuildingFractions)
                if (buildableThings.Any(b => b.Name == frac.Key))
                    if (myBuildings.Count(a => a == frac.Key) < frac.Value * myBuildings.Length)
                        return Rules.Info[frac.Key];

            return null;
        }

		int2? ChooseBuildLocation(ProductionItem item)
		{
			var bi = Rules.Info[item.Item].Traits.Get<BuildingInfo>();

			for (var k = 0; k < MaxBaseDistance; k++)
				foreach (var t in world.FindTilesInCircle(baseCenter, k))
					if (world.CanPlaceBuilding(item.Item, bi, t, null))
						if (world.IsCloseEnoughToBase(p, item.Item, bi, t))
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
            BuildDefense();
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

		bool IsHumanPlayer(Player p)
		{
			/* hack hack: this actually detects 'is not HackyAI' -- possibly actually a good thing. */
			var hackyAI = p.PlayerActor.Trait<HackyAI>();
			return !hackyAI.enabled;
		}

		bool HasHumanPlayers()
		{
			return p.World.players.Any(a => !a.Value.IsBot && !a.Value.NonCombatant);
		}
		int2? ChooseEnemyTarget()
		{
			// Criteria for picking an enemy:
			// 1. not ourself.
			// 2. human.
			// 3. not dead.

			var possibleTargets = world.WorldActor.Trait<MPStartLocations>().Start
					.Where(kv => kv.Key != p && (!HasHumanPlayers()|| IsHumanPlayer(kv.Key))
						&& p.WinState == WinState.Undefined)
					.Select(kv => kv.Value);

			return possibleTargets.Any() ? possibleTargets.Random(random) : (int2?) null;
		}

        void AssignRolesToIdleUnits(Actor self)
        {
			//HACK: trim these lists -- we really shouldn't be hanging onto all this state
			//when it's invalidated so easily, but that's Matthew/Alli's problem.
			activeUnits.RemoveAll(a => a.Destroyed);
			unitsHangingAroundTheBase.RemoveAll(a => a.Destroyed);
			attackForce.RemoveAll(a => a.Destroyed);
			activeProductionBuildings.RemoveAll(a => a.Destroyed);

            // don't select harvesters.
            var newUnits = self.World.Queries.OwnedBy[p]
                .Where(a => a.HasTrait<IMove>() && a.Info != Rules.Info["harv"]
                    && !activeUnits.Contains(a)).ToArray();

            foreach (var a in newUnits)
            {
				BotDebug("AI: Found a newly built unit");
                unitsHangingAroundTheBase.Add(a);
                activeUnits.Add(a);
            }

            /* Create an attack force when we have enough units around our base. */
            // (don't bother leaving any behind for defense.)
            if (unitsHangingAroundTheBase.Count > 8)
            {
                BotDebug("Launch an attack.");

				var attackTarget = ChooseEnemyTarget();
				if (attackTarget == null)
					return;

				foreach (var a in unitsHangingAroundTheBase)
					if (TryToMove(a, attackTarget.Value))
						attackForce.Add(a);

                unitsHangingAroundTheBase.Clear();
            }
        }

        private void SetRallyPointsForNewProductionBuildings(Actor self)
        {
            var newProdBuildings = self.World.Queries.OwnedBy[p]
                .Where(a => (a.TraitOrDefault<RallyPoint>() != null
                    && !activeProductionBuildings.Contains(a)
                    )).ToArray(); 

            foreach (var a in newProdBuildings)
            {
                activeProductionBuildings.Add(a);
                int2 newRallyPoint = ChooseRallyLocationNear(a.Location);
                newRallyPoint.X += 4;
                newRallyPoint.Y += 4;
                world.IssueOrder(new Order("SetRallyPoint", a, newRallyPoint));
            }
        }

        //won't work for shipyards...
        private int2 ChooseRallyLocationNear(int2 startPos)
        {
            Random r = new Random();
            foreach (var t in world.FindTilesInCircle(startPos, 8))
                if (world.IsCellBuildable(t, false) && t != startPos && r.Next(64) == 0)
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
            world.IssueOrder(new Order("Move", a, xy));
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
                world.IssueOrder(new Order("DeployTransform", mcv));
            }
            else
                BotDebug("AI: Can't find the MCV.");
        }

        //Build a random unit of the given type. Not going to be needed once there is actual AI...
        private void BuildRandom(string category)
        {
			// Pick a free queue
			var queue = world.Queries.WithTraitMultiple<ProductionQueue>()
				.Where(a => a.Actor.Owner == p &&
				       a.Trait.Info.Type == category &&
				       a.Trait.CurrentItem() == null)
				.Select(a => a.Trait)
				.FirstOrDefault();
			
			if (queue == null)
				return;
			
			var unit = ChooseRandomUnitToBuild(queue);
            Boolean found = false;
            if (unit != null)
            {
                foreach (var un in Info.UnitsToBuild)
                {
                    if (un.Key == unit.Name)
                    {
                        found = true;
                        break;
                    }
                }

                if (found == true)
                {
                    world.IssueOrder(Order.StartProduction(queue.self, unit.Name, 1));
                }
            }
        }

        private void BuildBuildings()
        {
            // Pick a free queue
			var queue = world.Queries.WithTraitMultiple<ProductionQueue>()
                .Where(a => a.Actor.Owner == p && a.Trait.Info.Type == "Building")
				.Select(a => a.Trait)
				.FirstOrDefault();
			
			if (queue == null)
				return;
			
			var currentBuilding = queue.CurrentItem();
            switch (bstate)
            {
                case BuildState.ChooseItem:
                    {
                        var item = ChooseBuildingToBuild(queue);
                        if (item == null)
                        {
                            bstate = BuildState.WaitForFeedback;
                            lastThinkTick = ticks;
                        }
                        else
                        {
                            BotDebug("AI: Starting production of {0}".F(item.Name));
                            bstate = BuildState.WaitForProduction;
                            world.IssueOrder(Order.StartProduction(queue.self, item.Name, 1));
                        }
                    }
                    break;

                case BuildState.WaitForProduction:
                    if (currentBuilding == null) return;	/* let it happen.. */

                    else if (currentBuilding.Paused)
                        world.IssueOrder(Order.PauseProduction(queue.self, currentBuilding.Item, false));
                    else if (currentBuilding.Done)
                    {
                        bstate = BuildState.WaitForFeedback;
                        lastThinkTick = ticks;

                        /* place the building */
                        var location = ChooseBuildLocation(currentBuilding);
                        if (location == null)
                        {
							BotDebug("AI: Nowhere to place {0}".F(currentBuilding.Item));
                            world.IssueOrder(Order.CancelProduction(queue.self, currentBuilding.Item, 1));
                        }
                        else
                        {
                            world.IssueOrder(new Order("PlaceBuilding", p.PlayerActor, location.Value, currentBuilding.Item));
                        }
                    }
                    break;

                case BuildState.WaitForFeedback:
                    if (ticks - lastThinkTick > feedbackTime)
                        bstate = BuildState.ChooseItem;
                    break;
            }
        }

        private void BuildDefense()
        {
            // Pick a free queue
            var queue = world.Queries.WithTraitMultiple<ProductionQueue>()
                .Where(a => a.Actor.Owner == p && a.Trait.Info.Type == "Defense")
                .Select(a => a.Trait)
                .FirstOrDefault();

            if (queue == null)
                return;

            var currentBuilding = queue.CurrentItem();
            switch (dstate)
            {
                case BuildState.ChooseItem:
                    {
                        var item = ChooseDefenseToBuild(queue);
                        if (item == null)
                        {
                            dstate = BuildState.WaitForFeedback;
                            lastThinkTick = ticks;
                        }
                        else
                        {
							BotDebug("AI: Starting production of {0}".F(item.Name));
                            dstate = BuildState.WaitForProduction;
                            world.IssueOrder(Order.StartProduction(queue.self, item.Name, 1));
                        }
                    }
                    break;

                case BuildState.WaitForProduction:
                    if (currentBuilding == null) return;	/* let it happen.. */

                    else if (currentBuilding.Paused)
                        world.IssueOrder(Order.PauseProduction(queue.self, currentBuilding.Item, false));
                    else if (currentBuilding.Done)
                    {
                        dstate = BuildState.WaitForFeedback;
                        lastThinkTick = ticks;

                        /* place the building */
                        var location = ChooseBuildLocation(currentBuilding);
                        if (location == null)
                        {
							BotDebug("AI: Nowhere to place {0}".F(currentBuilding.Item));
                            world.IssueOrder(Order.CancelProduction(queue.self, currentBuilding.Item, 1));
                        }
                        else
                        {
                            world.IssueOrder(new Order("PlaceBuilding", p.PlayerActor, location.Value, currentBuilding.Item));
                        }
                    }
                    break;

                case BuildState.WaitForFeedback:
                    if (ticks - lastThinkTick > feedbackTime)
                        dstate = BuildState.ChooseItem;
                    break;
            }
        }
	}
}
