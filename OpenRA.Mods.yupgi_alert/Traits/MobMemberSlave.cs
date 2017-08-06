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

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/*
 * Needs base engine modification.
 * 
 * In Selection.cs, I added Remove().
 */

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	[Desc("Can be slaved to a Mob spawner.")]
	class MobMemberSlaveInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new MobMemberSlave(init, this); }
	}

	class MobMemberSlave : INotifyCreated, INotifyKilled, INotifySelected
	{
		readonly MobMemberSlaveInfo info;

		MobSpawnerInfo mobSpawnerInfo;
		AttackBase[] attackBases;
		IMove[] moves;

		public Actor Master { get; private set; }

		public MobMemberSlave(ActorInitializer init, MobMemberSlaveInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			attackBases = self.TraitsImplementing<AttackBase>().ToArray();
			moves = self.TraitsImplementing<IMove>().ToArray();
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			// If killed, I tell my master that I'm gone.
			// Can happen, when built from build palette (w00t)
			if (Master == null || Master.IsDead)
				return;

			Master.Trait<MobSpawner>().SlaveKilled(Master, self);
		}

		public void LinkMaster(Actor master, MobSpawnerInfo mobSpawnerInfo)
		{
			Master = master;
			this.mobSpawnerInfo = mobSpawnerInfo;
		}

		public void Stop(Actor self)
		{
			self.CancelActivity();

			// And tell attack bases to stop attacking.
			if (attackBases.Length == 0)
				return;

			foreach (var ab in attackBases)
				if (!ab.IsTraitDisabled)
					ab.OnStopOrder(self);
		}

		Actor lastTarget = null;
		public void Attack(Actor self, Target target)
		{
			// Don't have to change target or alter current activity.
			if (lastTarget != null && lastTarget == target.Actor)
				return;

			if (!target.IsValidFor(self))
			{
				Stop(self);
				return;
			}

			foreach (var ab in attackBases)
			{
				if (ab.IsTraitDisabled)
					continue;

				if (target.Actor == null)
					ab.AttackTarget(target, false, true, true); // force fire on the ground.
				else if (target.Actor.Owner.Stances[self.Owner] == Stance.Ally)
					ab.AttackTarget(target, false, true, true); // force fire on ally.
				else
					/* Target deprives me of force fire information.
					 * This is a glitch if force fire weapon and normal fire are different, as in
					 * RA mod spies but won't matter too much for carriers.
					 */
					ab.AttackTarget(target, false, true, target.RequiresForceFire);
			}
		}

		public void Move(Actor self, Order order)
		{
			self.CancelActivity();

			// And tell attack bases to stop attacking.
			if (moves.Length == 0)
				return;

			foreach (var mv in moves)
				if (mv.IsTraitEnabled())
				{
					self.QueueActivity(mv.MoveTo(order.TargetLocation, 2));
					break;
				}
		}

		void INotifySelected.Selected(Actor self)
		{
			if (mobSpawnerInfo.SlavesHaveFreeWill)
				return;

			// I'm assuming these guys are selectable, both slave and the nexus.
			self.World.Selection.Remove(self.World, self);
			self.World.Selection.Add(self.World, Master);
		}
	}
}
