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
	[Desc("This actor can transport slave actors.")]
	public class SpawnerInfo : ITraitInfo, Requires<IOccupySpaceInfo>
	{
		[Desc("Number of slave units")]
		public readonly int Count = 0;

		[Desc("Slave unit type")]
		public readonly string SlaveUnit;

		[Desc("Slave regen delay, in ticks")]
		public readonly int RegenTicks = 150;

		[Desc("Slave rearm delay, in ticks")]
		public readonly int RearmTicks = 150;

		[Desc("Air units and ground units have different mobile trait so...")]
		// This can be computed but that requires a few cycles of cpu time XD
		public readonly bool SlaveIsGroundUnit = false;

		[Desc("Pip color of the slaved unit.")]
		public readonly PipType PipType = PipType.Yellow;

		[Desc("Terrain types that this actor is allowed to eject actors onto. Leave empty for all terrain types.")]
		public readonly HashSet<string> UnloadTerrainTypes = new HashSet<string>();

		[Desc("Insta-repair spawners when they return?")]
		public readonly bool InstaRepair = true;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while waiting for cargo to load.")]
		public readonly string LoadingCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while passengers are loaded.",
			"Condition can stack with multiple passengers.")]
		public readonly string LoadedCondition = null;

		[Desc("Conditions to grant when specified actors are loaded inside the transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> PassengerConditions = new Dictionary<string, string>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterPassengerConditions { get { return PassengerConditions.Values; } }

		public object Create(ActorInitializer init) { return new Spawner(init, this); }
	}

	class SlaveEntry
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

		readonly List<SlaveEntry> slaves = new List<SlaveEntry>(); // contained
		// keep track of launched ones so spawner can call them in or designate another target.
		readonly HashSet<Actor> launched = new HashSet<Actor>();
		readonly Dictionary<string, Stack<int>> passengerTokens = new Dictionary<string, Stack<int>>();
		readonly Lazy<IFacing> facing;
		readonly bool checkTerrainType;
		readonly ExitInfo[] exits;
		//Aircraft aircraft;
		// Carriers don't need to land to spawn stuff!
		// I want to make this like Protoss Carrier.
		ConditionManager conditionManager;
		int loadingToken = ConditionManager.InvalidConditionToken;
		Stack<int> loadedTokens = new Stack<int>();

		CPos currentCell;
		public IEnumerable<CPos> CurrentAdjacentCells { get; private set; }
		public bool Unloading { get; internal set; }
		//public IEnumerable<Actor> Slaves { get { return slaves; } }
		public int SlaveCount { get { return slaves.Count; } }

		int regen_ticks = -1; // -1: ticking disabled.

		public Spawner(ActorInitializer init, SpawnerInfo info)
		{
			self = init.Self;
			Info = info;
			Unloading = false;
			checkTerrainType = info.UnloadTerrainTypes.Count > 0;

			// Fill slaves.
			for (var i = 0; i < info.Count; i++)
			{
				Replenish(self);
			}

			facing = Exts.Lazy(self.TraitOrDefault<IFacing>);
			exits = self.Info.TraitInfos<ExitInfo>().ToArray();
		}

		void Replenish(Actor self)
		{
			var unit = self.World.CreateActor(false, Info.SlaveUnit.ToLowerInvariant(),
				new TypeDictionary { new OwnerInit(self.Owner) });
			var spawned = unit.Trait<Spawned>();
			spawned.Master = self; // let the spawned unit return to me for reloading and repair.

			var se = new SlaveEntry();
			se.s = unit;
			se.RearmTicks = 0;
			slaves.Add(se);
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

			if (slaves.Count == 0)
				return;

			var s = Launch(self);
			if (s == null)
				return;

			var exit = ChooseExit(self);
			SetSpawnedFacing(s, self, exit);

			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead)
					return;

				if (s.Disposed)
					return;

				var pos = s.Trait<IPositionable>();
				var spawn = self.CenterPosition;
				pos.SetVisualPosition(s, self.CenterPosition + exit.SpawnOffset);
				s.CancelActivity(); // Reset any activity. May had an activity before entering the spawner.
				// Or might had been added by above foreach launched loop.
				if (Info.SlaveIsGroundUnit)
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

			regen_ticks = Info.RegenTicks; // set clock so that regen happens.
		}

		Actor PopLaunchable(Actor self)
		{
			SlaveEntry result = null;
			foreach (var se in slaves)
			{
				if (se.RearmTicks <= 0)
				{
					result = se;
					break;
				}
			}

			if (result != null)
			{
				slaves.Remove(result);
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

			var passengerFacing = spawned.TraitOrDefault<IFacing>();
			if (passengerFacing != null)
				passengerFacing.Facing = (facing.Value.Facing + exit.Facing) % 256;

			foreach (var t in spawned.TraitsImplementing<Turreted>())
				t.TurretFacing = (facing.Value.Facing + exit.Facing) % 256;
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			var numPips = Info.Count;

			for (var i = 0; i < numPips; i++)
				yield return GetPipAt(i);
		}

		PipType GetPipAt(int i)
		{
			if (i < slaves.Count)
				return Info.PipType;
			else
				return PipType.Transparent;
		}

		public void Load(Actor self, Actor a)
		{
			if (launched.Contains(a))
				launched.Remove(a);

			string passengerCondition;
			if (conditionManager != null && Info.PassengerConditions.TryGetValue(a.Info.Name, out passengerCondition))
				passengerTokens.GetOrAdd(a.Info.Name).Push(conditionManager.GrantCondition(self, passengerCondition));

			if (conditionManager != null && !string.IsNullOrEmpty(Info.LoadedCondition))
				loadedTokens.Push(conditionManager.GrantCondition(self, Info.LoadedCondition));

			// Set up rearm.
			var se = new SlaveEntry();
			se.s = a;
			se.RearmTicks = Info.RearmTicks;
			slaves.Add(se);
		}

		public void Killed(Actor self, AttackInfo e)
		{
			foreach (var c in slaves)
				c.s.Kill(e.Attacker);
			foreach (var c in launched)
			{
				if (!c.IsDead)
					c.Kill(e.Attacker);
			}

			slaves.Clear();
			launched.Clear();
		}

		public void Disposing(Actor self)
		{
			foreach (var se in slaves)
				se.s.Dispose();
			foreach (var c in launched)
				c.Dispose();

			slaves.Clear();
			launched.Clear();
		}

		public void Selling(Actor self) { }

		bool sold = false;
		public void Sold(Actor self)
		{
			sold = true;

			// Dispose slaved.
			foreach (var se in slaves)
				se.s.Dispose();
			slaves.Clear();

			// Kill launched.
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
				foreach (var s in slaves)
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
				regen_ticks--;
			if (regen_ticks == 0)
			{
				regen_ticks = -1;

				while (slaves.Count + launched.Count < Info.Count)
					Replenish(self);
			}

			// Rearm
			foreach (var se in slaves)
			{
				if (se.RearmTicks > 0)
					se.RearmTicks--;
			}
		}
	}
}
