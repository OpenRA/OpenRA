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
		public readonly WRange VisionRange = WRange.FromCells(10);

		public override object Create(ActorInitializer init) { return new AttackBomber(init.self, this); }
	}

	class AttackBomber : AttackBase, ISync, INotifyKilled
	{
		AttackBomberInfo info;
		Actor camera;
		[Sync] Target target;

		public AttackBomber(Actor self, AttackBomberInfo info)
			: base(self, info)
		{
			this.info = info;
			this.camera = null;
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);

			var facing = self.TraitOrDefault<IFacing>();
			var cp = self.CenterPosition;
			var bombTarget = Target.FromPos(cp - new WVec(0, 0, cp.Z));

			// Provide vision
			if (this.camera == null &&
				target.IsInRange(self.CenterPosition, this.info.VisionRange))
			{
				this.camera = self.World.CreateActor("camera", new TypeDictionary
				{
					new LocationInit(target.CenterPosition.ToCPos()),
					new OwnerInit(self.Owner),
				});
			}
			else if (this.camera != null &&
				!target.IsInRange(self.CenterPosition, this.info.VisionRange))
			{
				self.World.Remove(this.camera);
				this.camera = null;
			}

			// Bombs drop anywhere in range
			foreach (var a in Armaments.Where(a => a.Info.Name == info.Bombs))
			{
				if (!target.IsInRange(self.CenterPosition, a.Weapon.Range))
					continue;

				a.CheckFire(self, this, facing, bombTarget);
			}

			// Guns only fire when approaching the target
			var facingToTarget = Util.GetFacing(target.CenterPosition - self.CenterPosition, facing.Facing);
			if (Math.Abs(facingToTarget - facing.Facing) % 256 > info.FacingTolerance)
				return;

			foreach (var a in Armaments.Where(a => a.Info.Name == info.Guns))
			{
				if (!target.IsInRange(self.CenterPosition, a.Weapon.Range))
				    continue;

				var t = Target.FromPos(cp - new WVec(0, a.Weapon.Range.Range / 2, cp.Z).Rotate(WRot.FromFacing(facing.Facing)));
				a.CheckFire(self, this, facing, t);
			}
		}

		public void SetTarget(WPos pos) { target = Target.FromPos(pos); }

		public void Killed(Actor self, AttackInfo e)
		{
			if (this.camera != null)
			{
				self.World.Remove(this.camera);
				this.camera = null;
			}
		}

		public override Activity GetAttackActivity(Actor self, Target newTarget, bool allowMove)
		{
			// TODO: Player controlled units want this too!
			throw new NotImplementedException("CarpetBomb requires a scripted target");
		}
	}
}
