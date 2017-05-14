#region Copyright & License Information
/*
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
using OpenRA.Activities;
using OpenRA.Mods.yupgi_alert.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.yupgi_alert.Activities
{
	public class ShootableBallisticMissileFly : Activity
	{
		readonly ShootableBallisticMissile sbm;
		readonly ShootableBallisticMissileInfo sbmInfo;
		readonly WPos initPos;
		readonly WPos targetPos;
		int length;
		int ticks;

		public ShootableBallisticMissileFly(Actor self, Target t)
		{
			sbm = self.Trait<ShootableBallisticMissile>();
			sbmInfo = self.Info.TraitInfo<ShootableBallisticMissileInfo>();
			initPos = self.CenterPosition;
			targetPos = t.CenterPosition; // fixed position == no homing
			length = Math.Max((targetPos - initPos).Length / sbmInfo.Speed, 1);
		}

		// Givein pitch in angle, compute effective yaw (=facing)
		int GetEffectiveFacing(int pitch)
		{
			return 0;
		}

		public void FlyToward(Actor self, ShootableBallisticMissile sbm)
		{
			var pos = WPos.LerpQuadratic(initPos, targetPos, sbmInfo.LaunchAngle, ticks, length);
			sbm.SetPosition(self, pos);
		}

		public override Activity Tick(Actor self)
		{
			var d = targetPos - self.CenterPosition;

			// The next move would overshoot, so consider it close enough
			var move = sbm.FlyStep(sbm.Facing);
			if (d.HorizontalLengthSquared < move.HorizontalLengthSquared)
				// Destruct so that Explodes will be called
				return new CallFunc(() => self.Kill(self));

			FlyToward(self, sbm);
			ticks++;
			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(targetPos);
		}
	}
}
