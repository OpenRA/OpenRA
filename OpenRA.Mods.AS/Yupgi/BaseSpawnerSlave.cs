#region Copyright & License Information
/*
 * Modded by Boolbada of OP Mod.
 * Modded from cargo.cs but a lot changed.
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/*
Works without base engine modification.
Will work even better if the PR is merged
*/

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	[Desc("Can be slaved to a SpawnerMaster.")]
	public class BaseSpawnerSlaveInfo : ITraitInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant to slaves when the master actor is killed.")]
		public readonly string MasterDeadCondition = null;

		[Desc("Can these actors be mind controlled or captured?")]
		public readonly bool AllowOwnerChange = false;

		public virtual object Create(ActorInitializer init) { return new BaseSpawnerSlave(init, this); }
	}

	public class BaseSpawnerSlave : INotifyCreated, INotifyKilled, INotifyOwnerChanged
	{
		protected AttackBase[] attackBases;
		protected ConditionManager conditionManager;

		readonly BaseSpawnerSlaveInfo info;

		public bool HasFreeWill = false;

		int masterDeadToken = ConditionManager.InvalidConditionToken;
		BaseSpawnerMaster spawnerMaster = null;

		public Actor Master { get; private set; }

		public BaseSpawnerSlave(ActorInitializer init, BaseSpawnerSlaveInfo info)
		{
			this.info = info;
		}

		public virtual void Created(Actor self)
		{
			attackBases = self.TraitsImplementing<AttackBase>().ToArray();
			conditionManager = self.Trait<ConditionManager>();
		}

		public void Killed(Actor self, AttackInfo e)
		{
			if (Master == null || Master.IsDead)
				return;

			spawnerMaster.OnSlaveKilled(Master, self);
		}

		public virtual void LinkMaster(Actor self, Actor master, BaseSpawnerMaster spawnerMaster)
		{
			Master = master;
			this.spawnerMaster = spawnerMaster;
		}

		bool TargetSwitched(Target lastTarget, Target newTarget)
		{
			if (newTarget.Type != lastTarget.Type)
				return true;

			if (newTarget.Type == TargetType.Terrain)
				return newTarget.CenterPosition != lastTarget.CenterPosition;

			if (newTarget.Type == TargetType.Actor)
				return lastTarget.Actor != newTarget.Actor;

			return false;
		}

		// Stop what self was doing.
		public void Stop(Actor self)
		{
			// Drop the target so that Attack() feels the need to assign target for this slave.
			lastTarget = Target.Invalid;

			self.CancelActivity();

			// And tell attack bases to stop attacking.
			foreach (var ab in attackBases)
				if (!ab.IsTraitDisabled)
					ab.OnStopOrder(self);
		}

		// Make this actor attack a target.
		Target lastTarget;
		public void Attack(Actor self, Target target)
		{
			// Don't have to change target or alter current activity.
			if (!TargetSwitched(lastTarget, target))
				return;

			if (!target.IsValidFor(self))
			{
				Stop(self);
				return;
			}

			lastTarget = target;

			foreach (var ab in attackBases)
			{
				if (ab.IsTraitDisabled)
					continue;

				if (target.Actor == null)
					ab.AttackTarget(target, false, true, true); // force fire on the ground.
				else if (target.Actor.Owner.Stances[self.Owner] == Stance.Ally)
					ab.AttackTarget(target, false, true, true); // force fire on ally.
				else if (target.Actor.Owner.Stances[self.Owner] == Stance.Neutral)
					ab.AttackTarget(target, false, true, true); // force fire on neutral.
				else
					/* Target deprives me of force fire information.
					 * This is a glitch if force fire weapon and normal fire are different, as in
					 * RA mod spies but won't matter too much for carriers. */
					ab.AttackTarget(target, false, true, target.RequiresForceFire);
			}
		}

		// DUMMY FUNCTION to suppress masterDeadToken assigned but unused warning (== error for Travis).
		void OnNewMaster(Actor self, Actor master)
		{
			conditionManager.RevokeCondition(self, masterDeadToken);
		}

		public virtual void OnMasterKilled(Actor self, Actor attacker, SpawnerSlaveDisposal disposal)
		{
			// Grant MasterDead condition.
			if (conditionManager != null && !string.IsNullOrEmpty(info.MasterDeadCondition))
				masterDeadToken = conditionManager.GrantCondition(self, info.MasterDeadCondition);

			switch (disposal)
			{
				case SpawnerSlaveDisposal.KillSlaves:
					self.Kill(attacker);
					break;
				case SpawnerSlaveDisposal.GiveSlavesToAttacker:
					self.CancelActivity();
					self.ChangeOwner(attacker.Owner);
					break;
				case SpawnerSlaveDisposal.DoNothing:
				// fall through
				default:
					break;
			}
		}

		// What if the master gets mind controlled?
		public virtual void OnMasterOwnerChanged(Actor self, Player oldOwner, Player newOwner, SpawnerSlaveDisposal disposal)
		{
			switch (disposal)
			{
				case SpawnerSlaveDisposal.KillSlaves:
					self.Kill(self);
					break;
				case SpawnerSlaveDisposal.GiveSlavesToAttacker:
					self.CancelActivity();
					self.ChangeOwner(newOwner);
					break;
				case SpawnerSlaveDisposal.DoNothing:
				// fall through
				default:
					break;
			}
		}

		// What if the slave gets mind controlled?
		// Slaves aren't good without master so, kill it.
		public virtual void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			// In this case, the slave will be disposed, one way or other.
			if (Master == null || !Master.IsDead)
				return;

			// This function got triggered because the master got mind controlled and
			// thus triggered slave.ChangeOwner().
			// In this case, do nothing.
			if (Master.Owner == newOwner)
				return;

			// These are independent, so why not let it be controlled?
			if (info.AllowOwnerChange)
				return;

			self.Kill(self);
		}
	}
}
