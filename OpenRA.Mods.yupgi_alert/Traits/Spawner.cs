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
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.yupgi_alert.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.yupgi_alert.Traits
{
	[Desc("This actor can spawn actors.")]
	public class SpawnerInfo : ITraitInfo, Requires<IOccupySpaceInfo>
	{
		[Desc("Number of spawn units")]
		public readonly int Count = 0;

		[Desc("Spawn unit type")]
		public readonly string SpawnUnit;

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

		[Desc("Air units and ground units have different mobile trait so...")]
		// This can be computed but that requires a few cycles of cpu time XD
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
		public Actor s;
		public int RearmTicks;
	}

	public class Spawner : IPips, INotifyCreated, INotifyKilled,
		INotifyOwnerChanged, INotifyAddedToWorld, ITick, INotifySold, INotifyActorDisposing,
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
		//Aircraft aircraft;
		// Carriers don't need to land to spawn stuff!
		// I want to make this like Protoss Carrier.
		ConditionManager conditionManager;
		int loadingToken = ConditionManager.InvalidConditionToken;
		Stack<int> loadedTokens = new Stack<int>();

		CPos currentCell;
		public IEnumerable<CPos> CurrentAdjacentCells { get; private set; }
		public int SpawnCount { get { return spawns.Count; } }

		int regen_ticks = 0;
		int launchingToken = ConditionManager.InvalidConditionToken;

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
			var spawned = unit.Trait<Spawned>();
			spawned.Master = self; // let the spawned unit return to me for reloading and repair.

			var se = new SpawnEntry();
			se.s = unit;
			se.RearmTicks = 0;
			spawns.Add(se);
		}

		public void Created(Actor self)
		{
			//aircraft = self.TraitOrDefault<Aircraft>();
			// If I want the airbourne spawner to land, I need to revive this logic (as was in cargo.cs)
			conditionManager = self.Trait<ConditionManager>();
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
			foreach(var spawned in launched)
				// At this point, freshly launched one is in the launched list, too.
				spawned.Trait<Spawned>().AttackTarget(spawned, target);

			if (spawns.Count == 0)
				return;

			var s = Launch(self);
			if (s == null)
				return;

			if (Info.SpawnIsMissile)
				// Consider it dead right after launching, if missile so that
				// regeneration happens right after launching.
				SlaveKilled(self, s);

			var exit = ChooseExit(self);
			SetSpawnedFacing(s, self, exit);

			// give timed launching condition
			if (Info.LaunchingCondition != null)
				launchingToken = conditionManager.GrantCondition(self, Info.LaunchingCondition, Info.LaunchingTicks);

			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead)
					return;

				if (s.Disposed)
					return;

				var pos = s.Trait<IPositionable>();
				var spawn = self.CenterPosition;
				var spawn_offset = exit == null ? WVec.Zero : exit.SpawnOffset;
				pos.SetVisualPosition(s, self.CenterPosition + spawn_offset);
				s.CancelActivity(); // Reset any activity. May had an activity before entering the spawner.
				// Or might had been added by above foreach launched loop.
				if (Info.SpawnIsGroundUnit)
				{
					// Air unit doesn't require this for some reason :)
					// Without this, ground unit is immobile.
					//CPos exit = Target.FromActor(self).Positions.Select(p => w.Map.CellContaining(p)).Distinct().First();
					//CPos exit = Util.AdjacentCells(s.World, Target.FromActor(self)).First();
					//CPos exit = target.Positions.Select(p => w.Map.CellContaining(p)).Distinct().First();
					var move = s.Trait<IMove>();
					var mv = move.MoveIntoWorld(s, self.Location);
					if (mv != null)
						s.QueueActivity(mv);
				}
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
			// Tell launched slaves to come back and enter me.
			foreach (var s in launched)
			{
				s.Trait<Spawned>().EnterSpawner(s);
			}
		}

		public void SlaveKilled(Actor self, Actor slave)
		{
			if (self.IsDead || self.Disposed || sold)
				// Well, complicated. Killed() invokes slave.kill(), whichi invokes this logic.
				// That's a bad loop. Don't let it be a loop.
				return;

			if (launched.Contains(slave))
				launched.Remove(slave);

			regen_ticks = Info.RespawnTicks; // set clock so that regen happens.
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
				return result.s;
			}
			return null;
		}

		// Production.cs use random to select an exit.
		// Here, we choose one by round robin.
		// Start from -1 so that +1 logic below will make it 0.
		int exit_round_robin = -1;
		ExitInfo ChooseExit(Actor self)
		{
			if (exits.Length == 0)
				return null;
			exit_round_robin = (exit_round_robin + 1) % exits.Length;
			return exits[exit_round_robin];
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
			if (Info.SpawnIsMissile)
				// Missiles have its own facing code
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
			se.s = a;
			se.RearmTicks = Info.RearmTicks;
			spawns.Add(se);
		}

		public void Killed(Actor self, AttackInfo e)
		{
			foreach (var c in spawns)
				c.s.Kill(e.Attacker);
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
				se.s.Dispose();
			foreach (var c in launched)
				c.Dispose();

			spawns.Clear();
			launched.Clear();
		}

		public void Selling(Actor self) { }

		bool sold = false;
		public void Sold(Actor self)
		{
			sold = true;

			// Dispose slaved.
			foreach (var se in spawns)
				se.s.Dispose();
			spawns.Clear();

			// Kill launched.
			// For Shootable Missiles, they are already removed from launched array so it is still fine.
			foreach (var c in launched)
				if (!c.IsDead)
					c.Kill(self);
			launched.Clear();
		}

		void SpawnSlave(Actor s)
		{
			self.World.AddFrameEndTask(w =>
			{
				w.Add(s);
				s.Trait<IPositionable>().SetPosition(s, self.Location);

				// TODO: this won't work well for >1 actor as they should move towards the next enterable (sub) cell instead
			});
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			self.World.AddFrameEndTask(w =>
			{
				foreach (var s in spawns)
					s.s.ChangeOwner(newOwner); // Under influence of mind control.
				foreach (var s in launched) // Kill launched, they are not under influence.
					s.Kill(self);
			});
		}

		public void AddedToWorld(Actor self)
		{
			// Force location update to avoid issues when initial spawn is outside map
			currentCell = self.Location;
			CurrentAdjacentCells = GetAdjacentCells();
		}

		public void Tick(Actor self)
		{
			var cell = self.World.Map.CellContaining(self.CenterPosition);
			if (currentCell != cell)
			{
				currentCell = cell;
				CurrentAdjacentCells = GetAdjacentCells();
			}

			// Regeneration
			if (regen_ticks > 0)
			{
				regen_ticks--;
				if (regen_ticks == 0)
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
