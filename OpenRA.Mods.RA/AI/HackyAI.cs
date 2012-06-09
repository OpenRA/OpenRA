#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;
using XRandom = OpenRA.Thirdparty.Random;

//TODO:
// effectively clear the area around the production buildings' spawn points.
// don't spam the build unit button, only queue one unit then wait for the backoff period.
//    just make the build unit action only occur once every second.

// later:
// don't build units randomly, have a method to it.
// explore spawn points methodically
// once you find a player, attack the player instead of spawn points.

namespace OpenRA.Mods.RA.AI
{
	class HackyAIInfo : IBotInfo, ITraitInfo
	{
		public readonly string Name = "Unnamed Bot";
		public readonly int SquadSize = 8;
		public readonly int AssignRolesInterval = 20;
		public readonly string RallypointTestBuilding = "fact";		// temporary hack to maintain previous rallypoint behavior.
		public readonly string[] UnitQueues = {"Vehicle", "Infantry", "Plane"};
		public readonly bool ShouldRepairBuildings = true;

		string IBotInfo.Name { get { return this.Name; } }

		[FieldLoader.LoadUsing("LoadUnits")]
		public readonly Dictionary<string, float> UnitsToBuild = null;

		[FieldLoader.LoadUsing("LoadBuildings")]
		public readonly Dictionary<string, float> BuildingFractions = null;

		static object LoadActorList(MiniYaml y, string field)
		{
			return y.NodesDict[field].Nodes.ToDictionary(
				t => t.Key,
				t => FieldLoader.GetValue<float>(field, t.Value.Value));
		}

		static object LoadUnits(MiniYaml y) { return LoadActorList(y, "UnitsToBuild"); }
		static object LoadBuildings(MiniYaml y) { return LoadActorList(y, "BuildingFractions"); }

		public object Create(ActorInitializer init) { return new HackyAI(this); }
	}

	/* a pile of hacks, which control a local player on the host. */

	class Enemy { public int Aggro; }

	class HackyAI : ITick, IBot, INotifyDamage
	{
		bool enabled;
		public int ticks;
		public Player p;
		PowerManager playerPower;
		readonly BuildingInfo rallypointTestBuilding;		// temporary hack
		readonly HackyAIInfo Info;

		Cache<Player,Enemy> aggro = new Cache<Player, Enemy>( _ => new Enemy() );
		int2 baseCenter;
		XRandom random = new XRandom(); //we do not use the synced random number generator.
		BaseBuilder[] builders;

		const int MaxBaseDistance = 20;
		public const int feedbackTime = 30;		// ticks; = a bit over 1s. must be >= netlag.

		public World world { get { return p.PlayerActor.World; } }
		IBotInfo IBot.Info { get { return this.Info; } }

		public HackyAI(HackyAIInfo Info)
		{
			this.Info = Info;
			// temporary hack.
			this.rallypointTestBuilding = Rules.Info[Info.RallypointTestBuilding].Traits.Get<BuildingInfo>();
		}

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
			builders = new BaseBuilder[] {
				new BaseBuilder( this, "Building", q => ChooseBuildingToBuild(q, true) ),
				new BaseBuilder( this, "Defense", q => ChooseBuildingToBuild(q, false) ) };
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

		ActorInfo ChooseBuildingToBuild(ProductionQueue queue, bool buildPower)
		{
			var buildableThings = queue.BuildableItems();

			if (!HasAdequatePower())	/* try to maintain 20% excess power */
			{
				if (!buildPower) return null;

				/* find the best thing we can build which produces power */
				return buildableThings.Where(a => GetPowerProvidedBy(a) > 0)
					.OrderByDescending(a => GetPowerProvidedBy(a)).FirstOrDefault();
			}

			var myBuildings = p.World
				.ActorsWithTrait<Building>()
				.Where( a => a.Actor.Owner == p )
				.Select(a => a.Actor.Info.Name).ToArray();

			foreach (var frac in Info.BuildingFractions)
				if (buildableThings.Any(b => b.Name == frac.Key))
					if (myBuildings.Count(a => a == frac.Key) < frac.Value * myBuildings.Length &&
						playerPower.ExcessPower >= Rules.Info[frac.Key].Traits.Get<BuildingInfo>().Power)
						return Rules.Info[frac.Key];

			return null;
		}

		bool NoBuildingsUnder(IEnumerable<int2> cells)
		{
			var bi = world.WorldActor.Trait<BuildingInfluence>();
			return cells.All(c => bi.GetBuildingAt(c) == null);
		}

		public int2? ChooseBuildLocation(string actorType)
		{
			var bi = Rules.Info[actorType].Traits.Get<BuildingInfo>();

			for (var k = 0; k < MaxBaseDistance; k++)
				foreach (var t in world.FindTilesInCircle(baseCenter, k))
					if (world.CanPlaceBuilding(actorType, bi, t, null))
						if (bi.IsCloseEnoughToBase(world, p, actorType, t))
							if (NoBuildingsUnder(Util.ExpandFootprint(
								FootprintUtils.Tiles(actorType, bi, t), false)))
								return t;

			return null;		// i don't know where to put it.
		}

		public void Tick(Actor self)
		{
			if (!enabled)
				return;

			ticks++;

			if (ticks == 1)
				DeployMcv(self);

			if (ticks % feedbackTime == 0)
				foreach (var q in Info.UnitQueues)
					BuildRandom(q);

			AssignRolesToIdleUnits(self);
			SetRallyPointsForNewProductionBuildings(self);

			foreach (var b in builders)
				b.Tick();
		}

		//hacks etc sigh mess.
		//A bunch of hardcoded lists to keep track of which units are doing what.
		List<Actor> unitsHangingAroundTheBase = new List<Actor>();
		List<Actor> attackForce = new List<Actor>();
		int2? attackTarget;

		//Units that the ai already knows about. Any unit not on this list needs to be given a role.
		List<Actor> activeUnits = new List<Actor>();

		int2? ChooseEnemyTarget()
		{
			var liveEnemies = world.Players
				.Where(q => p != q && p.Stances[q] == Stance.Enemy)
				.Where(q => p.WinState == WinState.Undefined && q.WinState == WinState.Undefined);

			var leastLikedEnemies = liveEnemies
				.GroupBy(e => aggro[e].Aggro)
				.OrderByDescending(g => g.Key)
				.FirstOrDefault();

			if (leastLikedEnemies == null)
				return null;

			var enemy = leastLikedEnemies != null ? leastLikedEnemies.Random(random) : null;

			/* pick something worth attacking owned by that player */
			var targets = world.Actors
				.Where(a => a.Owner == enemy && a.HasTrait<IOccupySpace>());
			Actor target=null;

			if (targets.Count()>0)
				target = targets.Random(random);

			if (target == null)
			{
				/* Assume that "enemy" has nothing. Cool off on attacks. */
				aggro[enemy].Aggro = aggro[enemy].Aggro / 2 - 1;
				Log.Write("debug", "Bot {0} couldn't find target for player {1}", this.p.ClientIndex, enemy.ClientIndex);

				return null;
			}

			/* bump the aggro slightly to avoid changing our mind */
			if (leastLikedEnemies.Count() > 1)
				aggro[enemy].Aggro++;

			return target.Location;
		}

		int assignRolesTicks = 0;

		void AssignRolesToIdleUnits(Actor self)
		{
			//HACK: trim these lists -- we really shouldn't be hanging onto all this state
			//when it's invalidated so easily, but that's Matthew/Alli's problem.
			activeUnits.RemoveAll(a => a.Destroyed);
			unitsHangingAroundTheBase.RemoveAll(a => a.Destroyed);
			attackForce.RemoveAll(a => a.Destroyed);

			if (--assignRolesTicks > 0)
				return;
			else
				assignRolesTicks = Info.AssignRolesInterval;

			var newUnits = self.World.ActorsWithTrait<IMove>()
				.Where(a => a.Actor.Owner == p && !a.Actor.HasTrait<BaseBuilding>()
					&& !activeUnits.Contains(a.Actor))
					.Select(a => a.Actor).ToArray();

			foreach (var a in newUnits)
			{
				BotDebug("AI: Found a newly built unit");
				if (a.HasTrait<Harvester>())
					world.IssueOrder( new Order( "Harvest", a, false ) );
				else
					unitsHangingAroundTheBase.Add(a);

				activeUnits.Add(a);
			}
			
			/* Create an attack force when we have enough units around our base. */
			// (don't bother leaving any behind for defense.)
			
			int randomizedSquadSize = Info.SquadSize - 4 + random.Next(200);
						
			if (unitsHangingAroundTheBase.Count >= randomizedSquadSize)
			{
				BotDebug("Launch an attack.");

				if (attackForce.Count == 0)
				{
					attackTarget = ChooseEnemyTarget();
					if (attackTarget == null)
						return;

					foreach (var a in unitsHangingAroundTheBase)
						if (TryToMove(a, attackTarget.Value, true))
							attackForce.Add(a);

					unitsHangingAroundTheBase.Clear();
				}
			}

			// If we have any attackers, let them scan for enemy units and stop and regroup if they spot any
			if (attackForce.Count > 0)
			{
				bool foundEnemy = false;
				foreach (var a1 in attackForce)
				{
					var enemyUnits = world.FindUnitsInCircle(a1.CenterLocation, Game.CellSize * 10)
						.Where(unit => p.Stances[unit.Owner] == Stance.Enemy).ToList();

					if (enemyUnits.Count > 0)
					{
						// Found enemy units nearby.
						foundEnemy = true;
						var enemy = enemyUnits.ClosestTo( a1.CenterLocation );

						// Check how many own units we have gathered nearby...
						var ownUnits = world.FindUnitsInCircle(a1.CenterLocation, Game.CellSize * 2)
							.Where(unit => unit.Owner == p).ToList();
						if (ownUnits.Count < randomizedSquadSize)
						{
							// Not enough to attack. Send more units.
							world.IssueOrder(new Order("Stop", a1, false));

							foreach (var a2 in attackForce)
								if (a2 != a1)
									world.IssueOrder(new Order("AttackMove", a2, false) { TargetLocation = a1.Location });
						}
						else
						{
							// We have gathered sufficient units. Attack the nearest enemy unit.
							foreach (var a2 in attackForce)
								world.IssueOrder(new Order("Attack", a2, false) { TargetActor = enemy });
						}
						return;
					}
				}

				if (!foundEnemy)
				{
					attackTarget = ChooseEnemyTarget();
					if (attackTarget != null)
						foreach (var a in attackForce)
							TryToMove(a, attackTarget.Value, true);
				}
			}
		}

		bool IsRallyPointValid(int2 x)
		{
			// this is actually WRONG as soon as HackyAI is building units with a variety of
			// movement capabilities. (has always been wrong)
			return world.IsCellBuildable(x, rallypointTestBuilding);
		}

		void SetRallyPointsForNewProductionBuildings(Actor self)
		{
			var buildings = self.World.ActorsWithTrait<RallyPoint>()
				.Where(rp => rp.Actor.Owner == p &&
					!IsRallyPointValid(rp.Trait.rallyPoint)).ToArray();

			if (buildings.Length > 0)
				BotDebug("Bot {0} needs to find rallypoints for {1} buildings.",
					p.PlayerName, buildings.Length);


			foreach (var a in buildings)
			{
				int2 newRallyPoint = ChooseRallyLocationNear(a.Actor.Location);
				world.IssueOrder(new Order("SetRallyPoint", a.Actor, false) { TargetLocation = newRallyPoint });
			}
		}

		//won't work for shipyards...
		int2 ChooseRallyLocationNear(int2 startPos)
		{
			var possibleRallyPoints = world.FindTilesInCircle(startPos, 8).Where(IsRallyPointValid).ToArray();
			if (possibleRallyPoints.Length == 0)
			{
				BotDebug("Bot Bug: No possible rallypoint near {0}", startPos);
				return startPos;
			}

			return possibleRallyPoints.Random(random);
		}

		int2? ChooseDestinationNear(Actor a, int2 desiredMoveTarget)
		{
			var move = a.TraitOrDefault<IMove>();
			if (move == null) return null;

			int2 xy;
			int loopCount = 0; //avoid infinite loops.
			int range = 2;
			do
			{
				//loop until we find a valid move location
				xy = new int2(desiredMoveTarget.X + random.Next(-range, range), desiredMoveTarget.Y + random.Next(-range, range));
				loopCount++;
				range = Math.Max(range, loopCount / 2);
				if (loopCount > 10) return null;
			} while (!move.CanEnterCell(xy) && xy != a.Location);

			return xy;
		}

		//try very hard to find a valid move destination near the target.
		//(Don't accept a move onto the subject's current position. maybe this is already not allowed? )
		bool TryToMove(Actor a, int2 desiredMoveTarget, bool attackMove)
		{
			var xy = ChooseDestinationNear(a, desiredMoveTarget);
			if (xy == null)
				return false;
			world.IssueOrder(new Order(attackMove ? "AttackMove" : "Move", a, false) { TargetLocation = xy.Value });
			return true;
		}

		void DeployMcv(Actor self)
		{
			/* find our mcv and deploy it */
			var mcv = self.World.Actors
				.FirstOrDefault(a => a.Owner == p && a.HasTrait<BaseBuilding>());

			if (mcv != null)
			{
				baseCenter = mcv.Location;
				world.IssueOrder(new Order("DeployTransform", mcv, false));
			}
			else
					BotDebug("AI: Can't find BaseBuildUnit.");
		}

		internal IEnumerable<ProductionQueue> FindQueues(string category)
		{
			return world.ActorsWithTrait<ProductionQueue>()
				.Where(a => a.Actor.Owner == p && a.Trait.Info.Type == category)
				.Select(a => a.Trait);
		}

		//Build a random unit of the given type. Not going to be needed once there is actual AI...
		void BuildRandom(string category)
		{
			// Pick a free queue
			var queue = FindQueues( category ).FirstOrDefault( q => q.CurrentItem() == null );
			if (queue == null)
				return;

			var unit = ChooseRandomUnitToBuild(queue);
			if (unit != null && Info.UnitsToBuild.Any( u => u.Key == unit.Name ))
				world.IssueOrder(Order.StartProduction(queue.self, unit.Name, 1));
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!enabled) return;

			if (Info.ShouldRepairBuildings && self.HasTrait<RepairableBuilding>())
				if (e.DamageState > DamageState.Light && e.PreviousDamageState <= DamageState.Light)
				{
					BotDebug("Bot noticed damage {0} {1}->{2}, repairing.",
						self, e.PreviousDamageState, e.DamageState);
					world.IssueOrder(new Order("RepairBuilding", self.Owner.PlayerActor, false)
						{ TargetActor = self });
				}

			if (e.Attacker != null && e.Damage > 0)
				aggro[e.Attacker.Owner].Aggro += e.Damage;
		}
	}
}