#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class SmokeTrailWhenDamagedInfo : ITraitInfo, Requires<IBodyOrientationInfo>
	{
		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;
		public readonly int Interval = 3;
		public readonly string Sprite = "smokey";
		public readonly DamageState MinDamage = DamageState.Heavy;

		public object Create(ActorInitializer init) { return new SmokeTrailWhenDamaged(init.self, this); }
	}

	class SmokeTrailWhenDamaged : ITick
	{
		IBodyOrientation body;
		SmokeTrailWhenDamagedInfo info;
		int ticks;

		public SmokeTrailWhenDamaged(Actor self, SmokeTrailWhenDamagedInfo info)
		{
			this.info = info;
			body = self.Trait<IBodyOrientation>();
		}

		public void Tick(Actor self)
		{
			if (--ticks <= 0)
			{
				var position = self.CenterPosition;
				if (position.Z > 0 && self.GetDamageState() >= info.MinDamage && !self.World.FogObscures(self))
				{
					var offset = info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation));
					var pos = position + body.LocalToWorld(offset);
					self.World.AddFrameEndTask(w => w.Add(new Smoke(w, pos, info.Sprite)));
				}

				ticks = info.Interval;
			}
		}
	}
}
