#region Copyright & License Information
/*
 * Written by Boolbada of OP Mod.
 * Follows OpenRA's license, GPLv3 as follows:
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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

/*
 * Needs base engine modification.
 * In Selection.cs, I added Remove().
 *
 * The difference between Spawner (carrier logic) and this is that
 * carriers have units going in and out of the master actor for reload activities,
 * while MobSpawner doesn't, thus MobSpawner has much simpler code.
 */

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	[Desc("This actor can spawn actors.")]
	public class MobSpawnerInfo : ConditionalTraitInfo
	{
		[Desc("Actors to spawn")]
		public readonly string[] Actors = null;

		[Desc("Actors to spawn at creation. Set to 0 to start fully spawned.")]
		public readonly int InitialActorCount = 0;

		[Desc("Spawn regen delay, in ticks")]
		public readonly int SpawnReplaceDelay = 150;

		[Desc("Spawn at a member, not the nexus?")]
		public readonly bool ExitByBudding = true;

		[Desc("No new members spawn, if true.")]
		public readonly bool OneShot = false;

		[Desc("Can the slaves be controlled independently?")]
		public readonly bool SlavesHaveFreeWill = false;

		[Desc("This is a dummy spawner like cin C&C Generals and use virtual position and health.")]
		public readonly bool AggregateHealth = true;

		public readonly int AggregateHealthUpdateDelay = 17; // Just a visual parameter, Doesn't affect the game.

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (Actors == null || Actors.Length == 0)
				throw new YamlException("Actors is null or empty for MobSpawner for actor type {0}!".F(ai.Name));

			if (InitialActorCount > Actors.Length || InitialActorCount < 0)
				throw new YamlException("MobSpawner can't have more InitialActorCount than the actors defined!");
		}

		public override object Create(ActorInitializer init) { return new MobSpawner(init, this); }
	}

	public class MobSpawner : INotifyCreated, INotifyKilled, IResolveOrder,
		INotifyOwnerChanged, ITick, INotifyActorDisposing
	{
		class MobEntry
		{
			public string ActorName = null;
			public Actor Actor = null;
			public MobMemberSlave MobMemberSlave = null;
			public Health Health = null;
			public int MaxHealth = 0;

			public bool IsValid { get { return Actor != null && !Actor.IsDead; } }
		}

		public readonly MobSpawnerInfo Info;
		readonly Actor self;
		readonly MobEntry[] mobs;

		int spawnReplaceTicks = 0;
		IPositionable pos;
		Health health;

		// For non-nexus spawners.
		IFacing facing;
		ExitInfo[] exits;
		RallyPoint rallyPoint;

		public MobSpawner(ActorInitializer init, MobSpawnerInfo info)
		{
			self = init.Self;
			Info = info;

			// Initialize mob entry
			mobs = new MobEntry[info.Actors.Length];
			for (var i = 0; i < info.Actors.Length; i++)
			{
				mobs[i] = new MobEntry();
				mobs[i].ActorName = info.Actors[i].ToLowerInvariant();
				mobs[i].MaxHealth = self.World.Map.Rules.Actors[mobs[i].ActorName].TraitInfo<HealthInfo>().HP;
			}
		}

		public void Created(Actor self)
		{
			rallyPoint = self.TraitOrDefault<RallyPoint>();
			exits = self.Info.TraitInfos<ExitInfo>().ToArray();
			facing = self.TraitOrDefault<IFacing>();
			pos = self.Trait<IPositionable>();
			health = self.Trait<Health>();

			// Spawn initial load.
			int burst = Info.InitialActorCount > 0 ? Info.InitialActorCount : Info.Actors.Length;
			for (int i = 0; i < burst; i++)
				Replenish(self);
			hasSpawnedInitialLoad = true;
		}	

		public void ResolveOrder(Actor self, Order order)
		{
			if (Info.SlavesHaveFreeWill)
				return;

			switch (order.OrderString)
			{
				case "Stop":
					StopSlaves();
					break;
				case "Attack":
				case "ForceAttack":
					AssignTargetsToSlaves(Target.FromOrder(self.World, order));
					break;
				case "Move":
					MoveSlaves(order);
					break;
				case "AttackMove":
					AttackMoveSlaves(order);
					break;
				default:
					// Do nothing.
					break;
			}
		}

		public void Tick(Actor self)
		{
			// Regeneration
			if (spawnReplaceTicks > 0)
			{
				spawnReplaceTicks--;
				if (spawnReplaceTicks == 0)
				{
					if (Replenish(self))
						spawnReplaceTicks = Info.SpawnReplaceDelay;
				}
			}

			// I'm a virtual mob spawning nexus.
			if (Info.AggregateHealth)
			{
				SetNexusPosition(self);
				SetNexusHealth(self);
			}
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			self.World.AddFrameEndTask(w =>
			{
				foreach (var mob in mobs)
					mob.Actor.ChangeOwner(newOwner); // Under influence of mind control.
			});
		}

		public void Killed(Actor self, AttackInfo e)
		{
			// Kill slaves too, unless they have free will.
			if (Info.SlavesHaveFreeWill)
				return;

			foreach (var mob in mobs)
				if (!mob.Actor.IsDead)
					mob.Actor.Kill(e.Attacker);
		}

		public void Disposing(Actor self)
		{
			// Dispose slaves too, unless they have free will.
			if (Info.SlavesHaveFreeWill)
				return;

			foreach (var mob in mobs)
				if (mob.IsValid)
					mob.Actor.Dispose();
		}

		MobEntry SelectEntryToSpawn()
		{
			// If any thing is marked dead or null, that's a candidate.
			var candidates = mobs.Where(m => !m.IsValid);
			if (!candidates.Any())
				return null;

			return candidates.Random(self.World.SharedRandom);
		}

		// Replenish members.
		// Return true if replenishing happened.
		bool Replenish(Actor self)
		{
			if (Info.OneShot)
				return false;

			MobEntry entry = SelectEntryToSpawn();

			// All are alive and well.
			if (entry == null)
				return false;

			// Some members are missing. Create a new one.
			var unit = self.World.CreateActor(false, entry.ActorName,
				new TypeDictionary { new OwnerInit(self.Owner) });

			// Update mobs entry
			unit.Trait<MobMemberSlave>().LinkMaster(self, Info);
			entry.Actor = unit;
			entry.MobMemberSlave = unit.Trait<MobMemberSlave>();
			entry.Health = unit.Trait<Health>();

			SpawnIntoWorld(self, unit);

			return true;
		}

		bool hasSpawnedInitialLoad = false;

		void SpawnIntoWorld(Actor self, Actor slave)
		{
			var exit = ChooseExit(self);
			SetSpawnedFacing(slave, self, exit);

			WPos centerPosition = WPos.Zero;

			if (!hasSpawnedInitialLoad || !Info.ExitByBudding)
			{
				// Spawning from a solid actor...
				centerPosition = self.CenterPosition;
			}
			else
			{
				// Spawning from a virtual nexus: exit by an existing member.
				foreach (var mob in mobs)
					if (mob.IsValid && mob.Actor.IsInWorld)
					{
						centerPosition = mob.Actor.CenterPosition;
						break;
					}
			}

			// WPos.Zero implies this mob spawner is dead.
			if (centerPosition == WPos.Zero)
				return;

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
					rallyPoint.QueueRallyOrder(self, slave);
				else
				{
					slave.QueueActivity(mv.MoveTo(location, 2));
					// Move to a valid position, if no rally point.
				}

				w.Add(slave);
			});
		}

		public void SlaveKilled(Actor self, Actor slave)
		{
			if (self.IsDead)
				return;

			// No need to update mobs entry because Actor.IsDead marking is done automatically by the engine.
			// However, we need to check if all are dead when AggregateHealth.
			if (Info.AggregateHealth && mobs.All(m => !m.IsValid))
				self.Dispose();

			if (spawnReplaceTicks <= 0)
				spawnReplaceTicks = Info.SpawnReplaceDelay;
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

		void StopSlaves()
		{
			foreach (var mob in mobs)
			{
				if (!mob.IsValid)
					continue;

				mob.MobMemberSlave.Stop(mob.Actor);
			}
		}

		void AssignTargetsToSlaves(Target target)
		{
			foreach (var mob in mobs)
			{
				if (!mob.IsValid)
					continue;

				mob.MobMemberSlave.Attack(mob.Actor, target);
			}
		}

		void MoveSlaves(Order order)
		{
			foreach (var mob in mobs)
			{
				if (!mob.IsValid || !mob.Actor.IsInWorld)
					continue;

				mob.MobMemberSlave.Move(mob.Actor, order);
			}
		}

		void AttackMoveSlaves(Order order)
		{
			foreach (var mob in mobs)
			{
				if (!mob.IsValid || !mob.Actor.IsInWorld)
					continue;

				mob.MobMemberSlave.AttackMove(mob.Actor, order);
			}
		}

		void SetNexusPosition(Actor self)
		{
			int x = 0, y = 0, cnt = 0;
			foreach (var mob in mobs)
			{
				if (!mob.IsValid || !mob.Actor.IsInWorld)
					continue;

				var pos = mob.Actor.CenterPosition;
				x += pos.X;
				y += pos.Y;
				cnt++;
			}

			if (cnt == 0)
				return;

			var newPos = new WPos(x / cnt, y / cnt, 0);
			pos.SetPosition(self, newPos);
			pos.SetVisualPosition(self, newPos);
		}

		int aggregateHealthUpdateTicks = 0;

		void SetNexusHealth(Actor self)
		{
			if (!Info.AggregateHealth)
				return;

			if (aggregateHealthUpdateTicks > 0)
			{
				aggregateHealthUpdateTicks--;
				return;
			}

			aggregateHealthUpdateTicks = Info.AggregateHealthUpdateDelay;

			// Time to aggregate health.
			int maxHealth = 0;
			int h = 0;

			foreach (var mob in mobs)
			{
				maxHealth += mob.MaxHealth;

				if (!mob.IsValid)
					continue;

				h += mob.Health.HP;
			}

			// Apply the aggregate health.
			h = h * health.MaxHP / maxHealth;

			if (h > 0)
			{
				// Only do these when h > 0.
				// Nexus kill when wiped out is handled else where.
				// We can't set health. Inflict damage instead.
				health.InflictDamage(self, self, new Damage(-health.MaxHP), true); // fully heal
				health.InflictDamage(self, self, new Damage(health.MaxHP - h), true); // remove some health
			}
		}
	}
}
