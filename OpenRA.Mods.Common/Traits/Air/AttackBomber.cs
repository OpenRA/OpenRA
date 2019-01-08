#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class AttackBomberInfo : AttackBaseInfo
	{
		public readonly int FacingTolerance = 2;

		public override object Create(ActorInitializer init) { return new AttackBomber(init.Self, this); }
	}

	public class AttackBomber : AttackBase, ITick, ISync, INotifyRemovedFromWorld
	{
		readonly AttackBomberInfo info;
		[Sync] Target target;
		[Sync] bool inAttackRange;
		[Sync] bool facingTarget = true;

		public event Action<Actor> OnRemovedFromWorld = self => { };
		public event Action<Actor> OnEnteredAttackRange = self => { };
		public event Action<Actor> OnExitedAttackRange = self => { };

		public AttackBomber(Actor self, AttackBomberInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		void ITick.Tick(Actor self)
		{
			var dat = self.World.Map.DistanceAboveTerrain(target.CenterPosition);
			target = Target.FromPos(target.CenterPosition - new WVec(WDist.Zero, WDist.Zero, dat));
			var wasInAttackRange = inAttackRange;
			var wasFacingTarget = facingTarget;

			inAttackRange = false;

			var f = facing.Facing;
			var delta = target.CenterPosition - self.CenterPosition;
			var facingToTarget = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : f;
			facingTarget = Math.Abs(facingToTarget - f) % 256 <= info.FacingTolerance;

			foreach (var a in Armaments)
			{
				if (!target.IsInRange(self.CenterPosition, a.MaxRange()))
					continue;

				inAttackRange = true;
				a.CheckFire(self, facing, target);
			}

			// Actors without armaments may want to trigger an action when it passes the target
			if (!Armaments.Any())
				inAttackRange = !wasInAttackRange && !facingTarget && wasFacingTarget;

			if (inAttackRange && !wasInAttackRange)
				OnEnteredAttackRange(self);

			if (!inAttackRange && wasInAttackRange)
				OnExitedAttackRange(self);
		}

		public void SetTarget(World w, WPos pos) { target = Target.FromPos(pos); }

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			OnRemovedFromWorld(self);
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove, bool forceAttack)
		{
			throw new NotImplementedException("AttackBomber requires a scripted target");
		}
	}
}
