#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class SmokeTrailWhenDamagedInfo : TraitInfo, Requires<BodyOrientationInfo>
	{
		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		public readonly int Interval = 3;

		public readonly string Sprite = "smokey";

		[SequenceReference(nameof(Sprite))]
		public readonly string Sequence = "idle";

		public readonly string Palette = "effect";

		public readonly DamageState MinDamage = DamageState.Heavy;

		public override object Create(ActorInitializer init) { return new SmokeTrailWhenDamaged(init.Self, this); }
	}

	class SmokeTrailWhenDamaged : ITick
	{
		readonly SmokeTrailWhenDamagedInfo info;
		readonly BodyOrientation body;
		readonly Func<WAngle> getFacing;
		int ticks;

		public SmokeTrailWhenDamaged(Actor self, SmokeTrailWhenDamagedInfo info)
		{
			this.info = info;
			body = self.Trait<BodyOrientation>();
			getFacing = RenderSprites.MakeFacingFunc(self);
		}

		void ITick.Tick(Actor self)
		{
			if (--ticks <= 0)
			{
				var position = self.CenterPosition;
				if (position.Z > 0 && self.GetDamageState() >= info.MinDamage && !self.World.FogObscures(self))
				{
					var offset = info.Offset.Rotate(body.QuantizeOrientation(self, self.Orientation));
					var pos = position + body.LocalToWorld(offset);
					self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(pos, getFacing(), w, info.Sprite, info.Sequence, info.Palette)));
				}

				ticks = info.Interval;
			}
		}
	}
}
