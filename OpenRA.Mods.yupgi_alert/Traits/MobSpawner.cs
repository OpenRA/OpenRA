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

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

/*
 * Needs base engine modification.
 * In AttackOmni.cs, SetTarget() made public.
 * In Mobile.cs, OccupySpace (true by default) added. Aircraft trait for a dummy unit wasn't the greatest idea as they fly over anything.
 * Move.cs, uses my PR which isn't in bleed yet. (PR to make Move use parent child activity)
 *
 * The difference between Spawner (carrier logic) and this is that
 * carriers have units going in and out of the master actor for reload activities,
 * while MobSpawner doesn't, thus MobSpawner has much simpler code.
 */

/*
 * The code is very similar to Spawner.cs.
 * Sometimes it is neater to have a duplicate than to have wrong inheirtances.
 */

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	// What to do when master is killed or mind controlled
	public enum MobMemberDisposal
	{
		DoNothing,
		KillSlaves,
		GiveSlavesToAttacker
	}

	[Desc("This actor can spawn actors.")]
	public class MobSpawnerInfo : BaseSpawnerMasterInfo
	{
		[Desc("Spawn regen delay, in ticks")]
		public readonly int SpawnReplaceDelay = 150;

		[Desc("Spawn at a member, not the nexus?")]
		public readonly bool ExitByBudding = true;

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

			if (InitialActorCount > Actors.Length || InitialActorCount < -1)
				throw new YamlException("MobSpawner can't have more InitialActorCount than the actors defined!");

			if (InitialActorCount == 0 && AggregateHealth == true)
				throw new YamlException("You can't have InitialActorCount == 0 and AggregateHealth");
		}

		public override object Create(ActorInitializer init) { return new MobSpawner(init, this); }
	}

	public class MobSpawner : BaseSpawnerMaster, INotifyCreated, INotifyOwnerChanged, ITick,
		INotifyActorDisposing, IResolveOrder, INotifyAttack
	{
		public class MobEntry
		{
			public string ActorName = null;
			public Actor Actor = null;
			public MobMemberSlave MobMemberSlave = null;
			public Health Health = null;
			public int MaxHealth = 0;

			public bool IsValid { get { return Actor != null && !Actor.IsDead; } }
		}

		public new MobSpawnerInfo Info { get; private set; }

		readonly Actor self;
		public MobEntry[] Mobs { get; private set; }

		int spawnReplaceTicks = 0;
		IPositionable position;
		Aircraft aircraft;
		Health health;

		public MobSpawner(ActorInitializer init, MobSpawnerInfo info) : base(init, info)
		{
			self = init.Self;
			Info = info;

			// Initialize mob entry
			Mobs = new MobEntry[info.Actors.Length];
			for (var i = 0; i < info.Actors.Length; i++)
			{
				Mobs[i] = new MobEntry();
				Mobs[i].ActorName = info.Actors[i].ToLowerInvariant();
				Mobs[i].MaxHealth = self.World.Map.Rules.Actors[Mobs[i].ActorName].TraitInfo<HealthInfo>().HP;
			}
		}

		protected override void Created(Actor self)
		{
			position = self.TraitOrDefault<IPositionable>();
			health = self.Trait<Health>();
			aircraft = self.TraitOrDefault<Aircraft>();

			base.Created(self);
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
				default:
					break;
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (Info.SlavesHaveFreeWill)
				return;

			AssignTargetsToSlaves(self, target);
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

			if (!Info.SlavesHaveFreeWill)
				AssignSlaveActivity(self);
		}
	
		MobEntry SelectEntryToSpawn()
		{
			// If any thing is marked dead or null, that's a candidate.
			var candidates = Mobs.Where(m => !m.IsValid);
			if (!candidates.Any())
				return null;

			return candidates.Random(self.World.SharedRandom);
		}

		// Replenish members.
		// Return true if replenishing happened.
		bool Replenish(Actor self)
		{
			if (Info.NoRegeneration)
				return false;

			MobEntry entry = SelectEntryToSpawn();

			// All are alive and well.
			if (entry == null)
				return false;

			// Some members are missing. Create a new one.
			var unit = self.World.CreateActor(false, entry.ActorName,
				new TypeDictionary { new OwnerInit(self.Owner) });

			// Update mobs entry
			entry.MobMemberSlave = unit.Trait<MobMemberSlave>();
			entry.Actor = unit;
			entry.Health = unit.Trait<Health>();

			entry.MobMemberSlave.LinkMaster(self, this);
			SpawnIntoWorld(self, unit);

			return true;
		}

		bool hasSpawnedInitialLoad = false;

		void SpawnIntoWorld(Actor self, Actor slave)
		{
			WPos centerPosition = WPos.Zero;

			if (!hasSpawnedInitialLoad || !Info.ExitByBudding)
			{
				// Spawning from a solid actor...
				centerPosition = self.CenterPosition;
			}
			else
			{
				// Spawning from a virtual nexus: exit by an existing member.
				foreach (var mob in Mobs)
					if (mob.IsValid && mob.Actor.IsInWorld)
					{
						centerPosition = mob.Actor.CenterPosition;
						break;
					}
			}

			// WPos.Zero implies this mob spawner is dead.
			if (centerPosition == WPos.Zero)
				return;

			SpawnIntoWorld(self, slave, centerPosition);
		}

		public void SlaveKilled(Actor self, Actor slave)
		{
			if (self.IsDead)
				return;

			// No need to update mobs entry because Actor.IsDead marking is done automatically by the engine.
			// However, we need to check if all are dead when AggregateHealth.
			if (Info.AggregateHealth && Mobs.All(m => !m.IsValid))
				self.Dispose();

			if (spawnReplaceTicks <= 0)
				spawnReplaceTicks = Info.SpawnReplaceDelay;
		}

		void AssignTargetsToSlaves(Actor self, Target target)
		{
			foreach (var mob in Mobs)
			{
				if (!mob.IsValid)
					continue;

				mob.MobMemberSlave.Attack(mob.Actor, target);
			}
		}

		void MoveSlaves(Actor self)
		{
			var targets = self.CurrentActivity.GetTargets(self);
			if (!targets.Any())
				return;

			var location = self.World.Map.CellContaining(targets.First().CenterPosition);

			foreach (var mob in Mobs)
			{
				if (!mob.IsValid || !mob.Actor.IsInWorld)
					continue;

				if (mob.Actor.Location == location)
					continue;

				if (!mob.MobMemberSlave.IsMoving)
				{
					mob.Actor.CancelActivity();
					mob.MobMemberSlave.Move(mob.Actor, location);
				}
			}
		}

		CPos lastAttackMoveLocation;
		void AttackMoveSlaves(Actor self)
		{
			var targets = self.CurrentActivity.GetTargets(self);
			if (!targets.Any())
				return;

			var location = self.World.Map.CellContaining(targets.First().CenterPosition);

			if (lastAttackMoveLocation == location)
				return;

			lastAttackMoveLocation = location;

			foreach (var mob in Mobs)
			{
				if (!mob.IsValid || !mob.Actor.IsInWorld)
					continue;

				mob.MobMemberSlave.AttackMove(mob.Actor, location);
			}
		}

		void SetNexusPosition(Actor self)
		{
			int x = 0, y = 0, cnt = 0;
			foreach (var mob in Mobs)
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

			var newPos = new WPos(x / cnt, y / cnt, aircraft != null ? aircraft.Info.CruiseAltitude.Length : 0);
			if (aircraft == null)
				position.SetPosition(self, newPos); // breaks arrival detection of the aircraft if we set position.
			position.SetVisualPosition(self, newPos);
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

			foreach (var mob in Mobs)
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

		void AssignSlaveActivity(Actor self)
		{
			if (Info.SlavesHaveFreeWill)
				return;

			if (self.CurrentActivity is Move || self.CurrentActivity is HeliFly)
				MoveSlaves(self);
			else if (self.CurrentActivity is AttackMoveActivity)
				AttackMoveSlaves(self);
			else if (self.CurrentActivity is AttackOmni.SetTarget)
				AssignTargetsToSlaves(self, self.CurrentActivity.GetTargets(self).First());
		}
	}
}
