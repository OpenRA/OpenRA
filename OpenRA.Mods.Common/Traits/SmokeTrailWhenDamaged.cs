#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
	class SmokeTrailWhenDamagedInfo : ITraitInfo, Requires<BodyOrientationInfo>
	{
		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;
		public readonly int Interval = 3;
		public readonly string Sprite = "smokey";
		[SequenceReference("Sprite")] public readonly string Sequence = "idle";
		public readonly string Palette = "effect";
		public readonly DamageState MinDamage = DamageState.Heavy;

		public object Create(ActorInitializer init) { return new SmokeTrailWhenDamaged(init.Self, this); }
	}

	class SmokeTrailWhenDamaged : ITick
	{
		readonly SmokeTrailWhenDamagedInfo info;
		readonly BodyOrientation body;
		int ticks;

		public SmokeTrailWhenDamaged(Actor self, SmokeTrailWhenDamagedInfo info)
		{
			this.info = info;
			body = self.Trait<BodyOrientation>();
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
					self.World.AddFrameEndTask(w => w.Add(new Smoke(w, pos, info.Sprite, info.Palette, info.Sequence)));
				}

				ticks = info.Interval;
			}
		}
	}
}
