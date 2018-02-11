#region Copyright & License Information
/*
 * Written by Boolbada of OP Mod.
 * Follows GPLv3 License as the OpenRA engine:
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	// What to do when master is killed or mind controlled
	public enum SpawnerSlaveDisposal
	{
		DoNothing,
		KillSlaves,
		GiveSlavesToAttacker
	}

	public class BaseSpawnerSlaveEntry
	{
		public string ActorName = null;
		public Actor Actor = null;
		public BaseSpawnerSlave SpawnerSlave = null;

		public bool IsValid { get { return Actor != null && !Actor.IsDead; } }
	}

	[Desc("This actor can spawn actors.")]
	public class BaseSpawnerMasterInfo : ConditionalTraitInfo
	{
		[Desc("Spawn these units. Define this like paradrop support power.")]
		public readonly string[] Actors;

		[Desc("Slave actors to contain upon creation. Set to -1 to start with full slaves.")]
		public readonly int InitialActorCount = -1;

		[Desc("The armament which will trigger the slaves to attack the target. (== \"Name:\" tag of Armament, not @tag!)")]
		[WeaponReference]
		public readonly string SpawnerArmamentName = "primary";

		[Desc("What happens to the slaves when the master is killed?")]
		public readonly SpawnerSlaveDisposal SlaveDisposalOnKill = SpawnerSlaveDisposal.KillSlaves;

		[Desc("What happens to the slaves when the master is mind controlled?")]
		public readonly SpawnerSlaveDisposal SlaveDisposalOnOwnerChange = SpawnerSlaveDisposal.GiveSlavesToAttacker;

		[Desc("Only spawn initial load of slaves?")]
		public readonly bool NoRegeneration = false;

		[Desc("Spawn all slaves at once when regenerating slaves, instead of one by one?")]
		public readonly bool SpawnAllAtOnce = false;

		[Desc("Spawn regen delay, in ticks")]
		public readonly int RespawnTicks = 150;

		// This can be computed but this should be faster.
		[Desc("Air units and ground units have different mobile trait so...")]
		public readonly bool SpawnIsGroundUnit = false;

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (Actors == null || Actors.Length == 0)
				throw new YamlException("Actors is null or empty for a spawner trait in actor type {0}!".F(ai.Name));

			if (InitialActorCount > Actors.Length)
				throw new YamlException("InitialActorCount can't be larger than the actors defined! (Actor type = {0})".F(ai.Name));

			if (InitialActorCount < -1)
				throw new YamlException("InitialActorCount must be -1 or non-negative. Actor type = {0}".F(ai.Name));
		}

		public override object Create(ActorInitializer init) { return new BaseSpawnerMaster(init, this); }
	}

	public class BaseSpawnerMaster : ConditionalTrait<BaseSpawnerMasterInfo>, INotifyCreated, INotifyKilled, INotifyOwnerChanged
	{
		readonly Actor self;
		protected readonly BaseSpawnerSlaveEntry[] SlaveEntries;

		IFacing facing;
		ExitInfo[] exits;
		RallyPoint rallyPoint;

		public BaseSpawnerMaster(ActorInitializer init, BaseSpawnerMasterInfo info) : base(info)
		{
			self = init.Self;

			// Initialize slave entries (doesn't instantiate the slaves yet)
			SlaveEntries = CreateSlaveEntries(info);

			for (var i = 0; i < info.Actors.Length; i++)
			{
				var entry = SlaveEntries[i];
				entry.ActorName = info.Actors[i].ToLowerInvariant();
			}
		}

		public virtual BaseSpawnerSlaveEntry[] CreateSlaveEntries(BaseSpawnerMasterInfo info)
		{
			var slaveEntries = new BaseSpawnerSlaveEntry[info.Actors.Length];

			for (int i = 0; i < slaveEntries.Length; i++)
				slaveEntries[i] = new BaseSpawnerSlaveEntry();

			return slaveEntries;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			facing = self.TraitOrDefault<IFacing>();
			exits = self.Info.TraitInfos<ExitInfo>().ToArray();
			rallyPoint = self.TraitOrDefault<RallyPoint>();

			// Spawn initial load.
			int burst = Info.InitialActorCount == -1 ? Info.Actors.Length : Info.InitialActorCount;
			for (int i = 0; i < burst; i++)
				Replenish(self, SlaveEntries);
		}

		/// <summary>
		/// Replenish destoyed slaves or create new ones from nothing.
		/// Follows policy defined by Info.OneShotSpawn.
		/// </summary>
		/// <returns>true when a new slave actor is created.</returns>
		public void Replenish(Actor self, BaseSpawnerSlaveEntry[] slaveEntries)
		{
			if (Info.SpawnAllAtOnce)
			{
				foreach (var se in slaveEntries)
					if (!se.IsValid)
						Replenish(self, se);
			}
			else
			{
				BaseSpawnerSlaveEntry entry = SelectEntryToSpawn(slaveEntries);

				// All are alive and well.
				if (entry == null)
					return;

				Replenish(self, entry);
			}
		}

		/// <summary>
		/// Replenish one slave entry.
		/// </summary>
		/// <returns>true when a new slave actor is created.</returns>
		public void Replenish(Actor self, BaseSpawnerSlaveEntry entry)
		{
			if (entry.IsValid)
				throw new InvalidOperationException("Replenish must not be run on a valid entry!");

			// Some members are missing. Create a new one.
			var slave = self.World.CreateActor(false, entry.ActorName,
				new TypeDictionary { new OwnerInit(self.Owner) });

			// Initialize slave entry
			InitializeSlaveEntry(slave, entry);
			entry.SpawnerSlave.LinkMaster(entry.Actor, self, this);
		}

		/// <summary>
		/// Slave entry initializer function.
		/// Override this function from derived classes to initialize their own specific stuff.
		/// </summary>
		public virtual void InitializeSlaveEntry(Actor slave, BaseSpawnerSlaveEntry entry)
		{
			entry.Actor = slave;
			entry.SpawnerSlave = slave.Trait<BaseSpawnerSlave>();
		}

		protected BaseSpawnerSlaveEntry SelectEntryToSpawn(BaseSpawnerSlaveEntry[] slaveEntries)
		{
			// If any thing is marked dead or null, that's a candidate.
			var candidates = slaveEntries.Where(m => !m.IsValid);
			if (!candidates.Any())
				return null;

			return candidates.Random(self.World.SharedRandom);
		}

		public virtual void Killed(Actor self, AttackInfo e)
		{
			// Notify slaves.
			foreach (var se in SlaveEntries)
				if (se.IsValid)
					se.SpawnerSlave.OnMasterKilled(se.Actor, e.Attacker, Info.SlaveDisposalOnKill);
		}

		public virtual void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			// Owner thing is so difficult and confusing, I'm expecting bugs.
			self.World.AddFrameEndTask(w =>
			{
				foreach (var se in SlaveEntries)
					if (se.IsValid)
						se.SpawnerSlave.OnMasterOwnerChanged(self, oldOwner, newOwner, Info.SlaveDisposalOnOwnerChange);
			});
		}

		public virtual void Disposing(Actor self)
		{
			// Just dispose them regardless of slave disposal options.
			foreach (var se in SlaveEntries)
				if (se.IsValid)
					se.Actor.Dispose();
		}

		public void SpawnIntoWorld(Actor self, Actor slave, WPos centerPosition)
		{
			var exit = ChooseExit(self);
			SetSpawnedFacing(slave, self, exit);

			self.World.AddFrameEndTask(w =>
			{
				if (self.IsDead)
					return;

				var spawnOffset = exit == null ? WVec.Zero : exit.SpawnOffset;
				slave.Trait<IPositionable>().SetVisualPosition(slave, centerPosition + spawnOffset);

				var location = self.World.Map.CellContaining(centerPosition + spawnOffset);

				var mv = slave.Trait<IMove>();
				slave.QueueActivity(mv.MoveIntoWorld(slave, location));

				// Move to rally point if any.
				if (rallyPoint != null)
					slave.QueueActivity(mv.MoveTo(rallyPoint.Location, 2));
				else
				{
					// Move to a valid position, if no rally point.
					slave.QueueActivity(mv.MoveTo(location, 2));
				}

				w.Add(slave);
			});
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

		void SetSpawnedFacing(Actor spawned, Actor spawner, ExitInfo exit)
		{
			int facingOffset = facing == null ? 0 : facing.Facing;

			var exitFacing = exit != null ? exit.Facing : 0;

			var spawnFacing = spawned.TraitOrDefault<IFacing>();
			if (spawnFacing != null)
				spawnFacing.Facing = (facingOffset + exitFacing) % 256;

			foreach (var t in spawned.TraitsImplementing<Turreted>())
				t.TurretFacing = (facingOffset + exitFacing) % 256;
		}

		public void StopSlaves()
		{
			foreach (var se in SlaveEntries)
			{
				if (!se.IsValid)
					continue;

				se.SpawnerSlave.Stop(se.Actor);
			}
		}

		public virtual void OnSlaveKilled(Actor self, Actor slave) { }
	}
}
