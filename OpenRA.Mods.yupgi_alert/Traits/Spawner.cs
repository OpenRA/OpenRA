#region Copyright & License Information
/*
 * Modded by Boolbada of OP Mod.
 * Modded from cargo.cs but a lot changed.
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

/*
Needs base engine modifications...

For slave miners:
But the docking procedure may need to change to fit your needs.
In OP Mod, docking changed for Harvester.cs and related files to that
these slaves can "dock" to any adjacent cells near the master.

For airborne carriers:
Those spawned aircrafts do work without any base engine modifcation.
However, land.cs modified so that they will "land" mid air.
Track readonly WDist landHeight; for related changes.

EnterSpawner needs modifications too, as it inherits Enter.cs
and uses its internal variables.
*/

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	[Desc("This actor can spawn actors.")]
	public class SpawnerInfo : ITraitInfo, Requires<IOccupySpaceInfo>
	{
		[Desc("Number of spawn units")]
		public readonly int Count = 0;

		[Desc("Spawn unit type")]
		public readonly string SpawnUnit;

		[Desc("The armament which will trigger the spawning. (== \"Name:\" tag of Armament, not @tag!)")]
		[WeaponReference]
		public readonly string SpawnerArmamentName = "primary";

		[Desc("Spawned will not take any orders from the spawner?")]
		public readonly bool IndependentSpawned = false;

		[Desc("Spawn is a missile that dies and not return.")]
		public readonly bool SpawnIsMissile = false;

		[Desc("Spawn regen delay, in ticks")]
		public readonly int RespawnTicks = 150;

		[Desc("Spawn rearm delay, in ticks")]
		public readonly int RearmTicks = 150;

		[GrantedConditionReference]
		[Desc("The condition to grant to self right after launching a spawned unit. (Used by V3 to make immobile.)")]
		public readonly string LaunchingCondition = null;

		[Desc("After this many ticks, we remove the condition.")]
		public readonly int LaunchingTicks = 15;

		// This can be computed but this should be faster.
		[Desc("Air units and ground units have different mobile trait so...")]
		public readonly bool SpawnIsGroundUnit = false;

		[Desc("Pip color for the spawn count.")]
		public readonly PipType PipType = PipType.Yellow;

		[Desc("Insta-repair spawners when they return?")]
		public readonly bool InstaRepair = true;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while spawned units are loaded.",
			"Condition can stack with multiple spawns.")]
		public readonly string LoadedCondition = null;

		[Desc("Conditions to grant when specified actors are contained inside the transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> SpawnContainConditions = new Dictionary<string, string>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterSpawnContainConditions { get { return SpawnContainConditions.Values; } }

		public object Create(ActorInitializer init) { return new Spawner(init, this); }
	}

	class SpawnEntry
	{
		public Actor Spawned;
		public int RearmTicks;
	}

	public class Spawner : IPips, INotifyCreated, INotifyKilled,
		INotifyOwnerChanged, ITick, INotifySold, INotifyActorDisposing,
		INotifyAttack, INotifyBecomingIdle
	{
		public readonly SpawnerInfo Info;
		readonly Actor self;

		readonly List<SpawnEntry> spawns = new List<SpawnEntry>(); // contained

		// keep track of launched ones so spawner can call them in or designate another target.
		readonly HashSet<Actor> launched = new HashSet<Actor>();

		readonly Dictionary<string, Stack<int>> spawnContainTokens = new Dictionary<string, Stack<int>>();
		readonly Lazy<IFacing> facing;
		readonly ExitInfo[] exits;
		ConditionManager conditionManager;
		Stack<int> loadedTokens = new Stack<int>();

		// Aircraft aircraft;
		// Carriers don't need to land to spawn stuff!
		// I want to make this like Protoss Carrier.
		// When we need that control you need this back.
		// int launchingToken = ConditionManager.InvalidConditionToken;
		int regenTicks = 0;

		RallyPoint rallyPoint;

		public Spawner(ActorInitializer init, SpawnerInfo info)
		{
			self = init.Self;
			Info = info;

			// Fill spawnees.
			for (var i = 0; i < info.Count; i++)
				Replenish(self);

			facing = Exts.Lazy(self.TraitOrDefault<IFacing>);
			exits = self.Info.TraitInfos<ExitInfo>().ToArray();
		}

		void Replenish(Actor self)
		{
			var unit = self.World.CreateActor(false, Info.SpawnUnit.ToLowerInvariant(),
				new TypeDictionary { new OwnerInit(self.Owner) });

			unit.Trait<Spawned>().LinkMaster(self, Info.IndependentSpawned);

			var se = new SpawnEntry();
			se.Spawned = unit;
			se.RearmTicks = 0;
			spawns.Add(se);
		}

		public void Created(Actor self)
		{
			conditionManager = self.Trait<ConditionManager>();
			rallyPoint = self.TraitOrDefault<RallyPoint>();
		}

		IEnumerable<CPos> GetAdjacentCells()
		{
			return Util.AdjacentCells(self.World, Target.FromActor(self)).Where(c => self.Location != c);
		}

		public bool CanLoad(Actor self, Actor a)
		{
			return true; // can always load slaves, unless the airbourne carrier has to land (not implemented)
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		// The rate of fire of the dummy weapon determines the launch cycle as each shot
		// invokes Attacking()
		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (a.Info.Name != Info.SpawnerArmamentName)
				return;

			// Issue retarget order for already launched ones
			if (!Info.IndependentSpawned)
				foreach (var spawned in launched)
					spawned.Trait<Spawned>().AttackTarget(spawned, target);

			if (spawns.Count == 0)
				return;

			var s = Launch(self);
			if (s == null)
				return;

			/*
			 * Commenting out, not in use at the moment.
			// grant timed launching condition...
			//if (Info.LaunchingCondition != null)
			//	launchingToken = conditionManager.GrantCondition(self, Info.LaunchingCondition, Info.LaunchingTicks);
			*/

			var exit = ChooseExit(self);
			SetSpawnedFacing(s, self, exit);

			if (Info.SpawnIsMissile)
			{
				// Consider it dead right after launching, if missile so that
				// regeneration happens right after launching.
				SlaveKilled(self, s);

				// The problem with AddFrameEndTask is that Target could be dead in next frame!
				// Not a big deal for ordinary spawns but for missile spawns, it still has to fly somewhere.
				// The solution is to cache the position
				s.Trait<ShootableBallisticMissile>().Target = Target.FromPos(target.CenterPosition);
			}

			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead || self.Disposed)
					return;

				var spawn_offset = exit == null ? WVec.Zero : exit.SpawnOffset;
				s.Trait<IPositionable>().SetVisualPosition(s, self.CenterPosition + spawn_offset);

				s.CancelActivity(); // Reset any activity. May had an activity before entering the spawner.

				// Move into world, if not. Ground units get stuck without this.
				if (Info.SpawnIsGroundUnit)
				{
					var mv = s.Trait<IMove>(); // .MoveIntoWorld(s, self.Location);
					s.QueueActivity(mv.MoveIntoWorld(s, self.World.Map.CellContaining(self.CenterPosition + spawn_offset)));
					if (rallyPoint != null)
						rallyPoint.QueueRallyOrder(self, s);
				}

				if (!Info.IndependentSpawned)
					s.Trait<Spawned>().AttackTarget(s, target);
				w.Add(s);
			});
		}

		public virtual void OnBecomingIdle(Actor self)
		{
			Recall(self);
		}

		void Recall(Actor self)
		{
			if (Info.IndependentSpawned)
				return;

			// Tell launched slaves to come back and enter me.
			foreach (var s in launched)
				s.Trait<Spawned>().EnterSpawner(s);
		}

		public void SlaveKilled(Actor self, Actor slave)
		{
			// Complicated. Killed() invokes slave.kill(), which invokes this logic.
			// == infinite loop. Lets break it.
			if (self.IsDead || self.Disposed || sold)
				return;

			if (launched.Contains(slave))
				launched.Remove(slave);

			regenTicks = Info.RespawnTicks; // set clock so that regen happens.
		}

		Actor PopLaunchable(Actor self)
		{
			SpawnEntry result = null;
			foreach (var se in spawns)
			{
				if (se.RearmTicks <= 0)
				{
					result = se;
					break;
				}
			}

			if (result != null)
			{
				spawns.Remove(result);
				return result.Spawned;
			}

			return null;
		}

		// Production.cs use random to select an exit.
		// Here, we choose one by round robin.
		// Start from -1 so that +1 logic below will make it 0.
		int exitRoundRobin = -1;
		ExitInfo ChooseExit(Actor self)
		{
			if (exits.Length == 0)
				return null;
			exitRoundRobin = (exitRoundRobin + 1) % exits.Length;
			return exits[exitRoundRobin];
		}

		public Actor Launch(Actor self)
		{
			var a = PopLaunchable(self);
			if (a == null)
				return null;
			launched.Add(a);
			return a;
		}

		void SetSpawnedFacing(Actor spawned, Actor spawner, ExitInfo exit)
		{
			if (facing.Value == null)
				return;

			// Missiles have its own facing code
			if (Info.SpawnIsMissile)
				return;

			var launch_angle = exit != null ? exit.Facing : 0;

			var spawnFacing = spawned.TraitOrDefault<IFacing>();
			if (spawnFacing != null)
				spawnFacing.Facing = (facing.Value.Facing + launch_angle) % 256;

			foreach (var t in spawned.TraitsImplementing<Turreted>())
				t.TurretFacing = (facing.Value.Facing + launch_angle) % 256;
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			var numPips = Info.Count;

			for (var i = 0; i < numPips; i++)
				yield return GetPipAt(i);
		}

		PipType GetPipAt(int i)
		{
			if (i < spawns.Count)
				return Info.PipType;
			else
				return PipType.Transparent;
		}

		public void Load(Actor self, Actor a)
		{
			if (launched.Contains(a))
				launched.Remove(a);

			string spawnContainCondition;
			if (conditionManager != null && Info.SpawnContainConditions.TryGetValue(a.Info.Name, out spawnContainCondition))
				spawnContainTokens.GetOrAdd(a.Info.Name).Push(conditionManager.GrantCondition(self, spawnContainCondition));

			if (conditionManager != null && !string.IsNullOrEmpty(Info.LoadedCondition))
				loadedTokens.Push(conditionManager.GrantCondition(self, Info.LoadedCondition));

			// Set up rearm.
			var se = new SpawnEntry();
			se.Spawned = a;
			se.RearmTicks = Info.RearmTicks;
			spawns.Add(se);
		}

		public void Killed(Actor self, AttackInfo e)
		{
			// kill stuff inside
			foreach (var c in spawns)
				c.Spawned.Kill(e.Attacker);
		
			// kill stuff outside
			foreach (var c in launched)
			{
				if (!c.IsDead)
					c.Kill(e.Attacker);
			}

			spawns.Clear();
			launched.Clear();
		}

		public void Disposing(Actor self)
		{
			foreach (var se in spawns)
				se.Spawned.Dispose();
			foreach (var c in launched)
				c.Dispose();

			spawns.Clear();
			launched.Clear();
		}

		public void Selling(Actor self) { }

		bool sold = false;
		public void Sold(Actor self)
		{
			if (sold)
				return;
			sold = true;

			// Dispose slaved.
			foreach (var se in spawns)
				se.Spawned.Dispose();
			spawns.Clear();

			// Kill launched.
			// For Shootable Missiles, they are already removed from launched array so it is still fine.
			foreach (var c in launched)
				if (!c.IsDead)
					c.Kill(self);
			launched.Clear();
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			self.World.AddFrameEndTask(w =>
			{
				foreach (var s in spawns)
					s.Spawned.ChangeOwner(newOwner); // Under influence of mind control.
				foreach (var s in launched) // Kill launched, they are not under influence.
					s.Kill(self);
			});
		}

		public void Tick(Actor self)
		{
			// Regeneration
			if (regenTicks > 0)
			{
				regenTicks--;
				if (regenTicks == 0)
					while (spawns.Count + launched.Count < Info.Count)
						Replenish(self);
			}

			// Rearm
			foreach (var se in spawns)
			{
				if (se.RearmTicks > 0)
					se.RearmTicks--;
			}
		}
	}
}
