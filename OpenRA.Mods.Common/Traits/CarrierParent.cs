#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can spawn actors.")]
	public class CarrierParentInfo : BaseSpawnerParentInfo
	{
		[Desc("Spawn rearm delay, in ticks")]
		public readonly int RearmTicks = 150;

		[GrantedConditionReference]
		[Desc("The condition to grant to self right after launching a spawned unit.")]
		public readonly string LaunchingCondition = null;

		[Desc("After this many ticks, we remove the condition.")]
		public readonly int LaunchingTicks = 15;

		[Desc("Instantly repair children when they return?")]
		public readonly bool InstantRepair = true;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while spawned units are loaded.",
			"Condition can stack with multiple spawns.")]
		public readonly string LoadedCondition = null;

		public override object Create(ActorInitializer init) { return new CarrierParent(init, this); }
	}

	public class CarrierParent : BaseSpawnerParent, ITick, INotifyAttack, INotifyBecomingIdle
	{
		class CarrierChildEntry : BaseSpawnerChildEntry
		{
			public int RearmTicks = 0;
			public bool IsLaunched = false;
			public new CarrierChild SpawnerChild;
		}

		readonly Dictionary<string, Stack<int>> spawnContainTokens = new Dictionary<string, Stack<int>>();
		readonly CarrierParentInfo info;
		readonly Stack<int> loadedTokens = new Stack<int>();

		CarrierChildEntry[] childEntries;

		int respawnTicks = 0;

		int launchCondition = Actor.InvalidConditionToken;
		int launchConditionTicks;

		public CarrierParent(ActorInitializer init, CarrierParentInfo info)
			: base(init, info)
		{
			this.info = info;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			var burst = Info.InitialActorCount == -1 ? Info.Actors.Length : Info.InitialActorCount;
			for (var i = 0; i < burst; i++)
				Replenish(self, ChildEntries);
		}

		public override BaseSpawnerChildEntry[] CreateChildEntries(BaseSpawnerParentInfo info)
		{
			childEntries = new CarrierChildEntry[info.Actors.Length];

			for (var i = 0; i < childEntries.Length; i++)
				childEntries[i] = new CarrierChildEntry();

			return childEntries;
		}

		public override void InitializeChildEntry(Actor child, BaseSpawnerChildEntry entry)
		{
			var carrierChildEntry = entry as CarrierChildEntry;
			base.InitializeChildEntry(child, carrierChildEntry);

			carrierChildEntry.RearmTicks = 0;
			carrierChildEntry.IsLaunched = false;
			carrierChildEntry.SpawnerChild = child.Trait<CarrierChild>();
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled)
				return;

			if (!Info.ArmamentNames.Contains(a.Info.Name))
				return;

			// Issue retarget order for already launched ones
			foreach (var child in childEntries)
				if (child.IsLaunched && child.IsValid)
					child.SpawnerChild.Attack(child.Actor, target);

			var carrierChildEntry = GetLaunchable();
			if (carrierChildEntry == null)
				return;

			carrierChildEntry.IsLaunched = true;

			if (info.LaunchingCondition != null)
			{
				if (launchCondition == Actor.InvalidConditionToken)
					launchCondition = self.GrantCondition(info.LaunchingCondition);

				launchConditionTicks = info.LaunchingTicks;
			}

			SpawnIntoWorld(self, carrierChildEntry.Actor, self.CenterPosition);

			Stack<int> spawnContainToken;
			if (spawnContainTokens.TryGetValue(a.Info.Name, out spawnContainToken) && spawnContainToken.Any())
				self.RevokeCondition(spawnContainToken.Pop());

			if (loadedTokens.Any())
				self.RevokeCondition(loadedTokens.Pop());

			self.World.AddFrameEndTask(w =>
			{
				// The actor might had been trying to do something before entering the carrier.
				// Cancel whatever it was trying to do.
				carrierChildEntry.SpawnerChild.Stop(carrierChildEntry.Actor);

				carrierChildEntry.SpawnerChild.Attack(carrierChildEntry.Actor, target);
			});
		}

		void INotifyBecomingIdle.OnBecomingIdle(Actor self)
		{
			Recall();
		}

		void Recall()
		{
			foreach (var carrierChildEntry in childEntries)
				if (carrierChildEntry.IsLaunched && carrierChildEntry.IsValid)
					carrierChildEntry.SpawnerChild.EnterSpawner(carrierChildEntry.Actor);
		}

		public override void OnChildKilled(Actor self, Actor child)
		{
			// Set clock so that regeneration happens.
			if (respawnTicks <= 0)
				respawnTicks = Info.RespawnTicks;
		}

		CarrierChildEntry GetLaunchable()
		{
			foreach (var carrierChildEntry in childEntries)
				if (carrierChildEntry.RearmTicks <= 0 && !carrierChildEntry.IsLaunched && carrierChildEntry.IsValid)
					return carrierChildEntry;

			return null;
		}

		public void PickupChild(Actor self, Actor child)
		{
			if (info.InstantRepair)
			{
				var health = child.Trait<Health>();
				child.InflictDamage(child, new Damage(-health.MaxHP));
			}

			CarrierChildEntry childEntry = null;
			foreach (var carrierChildEntry in childEntries)
			{
				if (carrierChildEntry.Actor == child)
				{
					childEntry = carrierChildEntry;
					break;
				}
			}

			if (childEntry == null)
				throw new InvalidOperationException("An actor that isn't my child entered me?");

			childEntry.IsLaunched = false;

			childEntry.RearmTicks = Util.ApplyPercentageModifiers(info.RearmTicks, reloadModifiers.Select(rm => rm.GetReloadModifier()));

			if (!string.IsNullOrEmpty(info.LoadedCondition))
				loadedTokens.Push(self.GrantCondition(info.LoadedCondition));
		}

		public override void Replenish(Actor self, BaseSpawnerChildEntry entry)
		{
			base.Replenish(self, entry);

			if (!string.IsNullOrEmpty(info.LoadedCondition))
				loadedTokens.Push(self.GrantCondition(info.LoadedCondition));
		}

		void ITick.Tick(Actor self)
		{
			if (launchCondition != Actor.InvalidConditionToken && --launchConditionTicks < 0)
				launchCondition = self.RevokeCondition(launchCondition);

			if (respawnTicks > 0)
			{
				respawnTicks--;

				if (respawnTicks <= 0)
				{
					Replenish(self, childEntries);

					// If there's something left to spawn, restart the timer.
					if (SelectEntryToSpawn(childEntries) != null)
						respawnTicks = Util.ApplyPercentageModifiers(Info.RespawnTicks, reloadModifiers.Select(rm => rm.GetReloadModifier()));
				}
			}

			foreach (var carrierChildEntry in childEntries)
				if (carrierChildEntry.RearmTicks > 0)
					carrierChildEntry.RearmTicks--;
		}
	}
}
