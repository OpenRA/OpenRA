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

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Mods.yupgi_alert.Traits;

namespace OpenRA.Mods.Yupgi_alert.Activities
{
	public class TractionFallToEarth : Activity
	{
		readonly Tractable tractable;

		int fallSpeed = 0;

		public TractionFallToEarth(Actor self, Tractable tractable)
		{
			IsInterruptible = false;
			this.tractable = tractable;
		}

		public override Activity Tick(Actor self)
		{
			if (self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length <= 0)
			{
				if (tractable.Info.ExplosionWeapon != null)
				{
					// Use .FromPos since this actor is killed. Cannot use Target.FromActor
					tractable.Info.ExplosionWeapon.Impact(Target.FromPos(self.CenterPosition), self, Enumerable.Empty<int>());
				}

				tractable.RevokeTractingCondition(self);

				var health = self.Trait<Health>();
				health.InflictDamage(self, self, new Damage(health.MaxHP * tractable.Info.DamageFactor / 100), false);
				return null;
			}

			var move = new WVec(0, 0, fallSpeed);
			fallSpeed -= tractable.Info.FallGravity.Length;

			var pos = self.CenterPosition + move;
			if (pos.Z < 0)
				tractable.SetPosition(self, new WPos(pos.X, pos.Y, 0));
			else
				tractable.SetPosition(self, pos);

			return this;
		}
	}
}
