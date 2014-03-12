#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AttackBomberInfo : AttackBaseInfo
	{
		[Desc("Armament name")]
		public readonly string Bombs = "primary";

		[Desc("Armament name")]
		public readonly string Guns = "secondary";
		public readonly int FacingTolerance = 2;

		public override object Create(ActorInitializer init) { return new AttackBomber(init.self, this); }
	}

	class AttackBomber : AttackBase, ISync, INotifyRemovedFromWorld
	{
		AttackBomberInfo info;
		[Sync] Target target;
		[Sync] bool inAttackRange;

		public event Action<Actor> OnRemovedFromWorld = self => { };
		public event Action<Actor> OnEnteredAttackRange = self => { };
		public event Action<Actor> OnExitedAttackRange = self => { };

		public AttackBomber(Actor self, AttackBomberInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);

			var cp = self.CenterPosition;
			var bombTarget = Target.FromPos(cp - new WVec(0, 0, cp.Z));
			var wasInAttackRange = inAttackRange;
			inAttackRange = false;

			// Bombs drop anywhere in range
			foreach (var a in Armaments.Where(a => a.Info.Name == info.Bombs))
			{
				if (!target.IsInRange(self.CenterPosition, a.Weapon.Range))
					continue;

				inAttackRange = true;
				a.CheckFire(self, this, facing.Value, bombTarget);
			}

			// Guns only fire when approaching the target
			var f = facing.Value.Facing;
			var facingToTarget = Util.GetFacing(target.CenterPosition - self.CenterPosition, f);
			if (Math.Abs(facingToTarget - f) % 256 <= info.FacingTolerance)
			{
				foreach (var a in Armaments.Where(a => a.Info.Name == info.Guns))
				{
					if (!target.IsInRange(self.CenterPosition, a.Weapon.Range))
					    continue;

					var t = Target.FromPos(cp - new WVec(0, a.Weapon.Range.Range / 2, cp.Z).Rotate(WRot.FromFacing(f)));
					inAttackRange = true;
					a.CheckFire(self, this, facing.Value, t);
				}
			}

			if (inAttackRange && !wasInAttackRange)
				OnEnteredAttackRange(self);

			if (!inAttackRange && wasInAttackRange)
				OnExitedAttackRange(self);
		}

		public void SetTarget(WPos pos) { target = Target.FromPos(pos); }

		public void RemovedFromWorld(Actor self)
		{
			OnRemovedFromWorld(self);
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			throw new NotImplementedException("AttackBomber requires a scripted target");
		}
	}
}
