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

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Yupgi_alert.Activities;
using OpenRA.Traits;

/*
 * Works without base engine modification?
 */

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	[Desc("This actor can spawn missile actors.")]
	public class MissileSpawnerMasterInfo : BaseSpawnerMasterInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant to self right after launching a spawned unit. (Used by V3 to make immobile.)")]
		public readonly string LaunchingCondition = null;

		[Desc("After this many ticks, we remove the condition.")]
		public readonly int LaunchingTicks = 15;

		[Desc("Pip color for the spawn count.")]
		public readonly PipType PipType = PipType.Yellow;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while spawned units are loaded.",
			"Condition can stack with multiple spawns.")]
		public readonly string LoadedCondition = null;

		[Desc("Conditions to grant when specified actors are contained inside the transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> SpawnContainConditions = new Dictionary<string, string>();

		[GrantedConditionReference]
		public IEnumerable<string> LinterSpawnContainConditions { get { return SpawnContainConditions.Values; } }

		public override object Create(ActorInitializer init) { return new MissileSpawnerMaster(init, this); }
	}

	public class MissileSpawnerMaster : BaseSpawnerMaster, IPips, ITick, INotifyAttack
	{
		// readonly Dictionary<string, Stack<int>> spawnContainTokens = new Dictionary<string, Stack<int>>();

		public new MissileSpawnerMasterInfo Info { get; private set; }

		ConditionManager conditionManager;

		// Stack<int> loadedTokens = new Stack<int>();

		int respawnTicks = 0;

		public MissileSpawnerMaster(ActorInitializer init, MissileSpawnerMasterInfo info) : base(init, info)
		{
			Info = info;
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			conditionManager = self.Trait<ConditionManager>();
		}

		public override void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			// Do nothing, because missiles can't be captured or mind controlled.
			return;
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		// The rate of fire of the dummy weapon determines the launch cycle as each shot
		// invokes Attacking()
		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled)
				return;

			if (a.Info.Name != Info.SpawnerArmamentName)
				return;

			// Issue retarget order for already launched ones
			foreach (var slave in SlaveEntries)
				if (slave.IsValid)
					slave.SpawnerSlave.Attack(slave.Actor, target);

			var se = GetLaunchable();
			if (se == null)
				return;

			// Launching condition is timed, so not saving the token.
			if (Info.LaunchingCondition != null)
				conditionManager.GrantCondition(self, Info.LaunchingCondition, Info.LaunchingTicks);

			// Program the trajectory.
			var sbm = se.Actor.Trait<ShootableBallisticMissile>();
			sbm.Target = Target.FromPos(target.CenterPosition);

			SpawnIntoWorld(self, se.Actor, self.CenterPosition);

			// Queue attack order, too.
			self.World.AddFrameEndTask(w =>
			{
				se.Actor.QueueActivity(new ShootableBallisticMissileFly(se.Actor, sbm.Target, sbm));

				// invalidate the slave entry so that slave will regen.
				se.Actor = null;
			});

			// Set clock so that regen happens.
			if (respawnTicks <= 0) // Don't interrupt an already running timer!
				respawnTicks = Info.RespawnTicks;
		}

		BaseSpawnerSlaveEntry GetLaunchable()
		{
			foreach (var se in SlaveEntries)
				if (se.IsValid)
					return se;

			return null;
		}

		public IEnumerable<PipType> GetPips(Actor self)
		{
			if (IsTraitDisabled)
				yield break;

			int inside = 0;
			foreach (var se in SlaveEntries)
				if (se.IsValid)
					inside++;

			for (var i = 0; i < Info.Actors.Length; i++)
			{
				if (i < inside)
					yield return Info.PipType;
				else
					yield return PipType.Transparent;
			}
		}

		public void Tick(Actor self)
		{
			if (respawnTicks > 0)
			{
				respawnTicks--;

				// Time to respawn someting.
				if (respawnTicks <= 0)
				{
					Replenish(self, SlaveEntries);

					// If there's something left to spawn, restart the timer.
					if (SelectEntryToSpawn(SlaveEntries) != null)
						respawnTicks = Info.RespawnTicks;
				}
			}
		}
	}
}
