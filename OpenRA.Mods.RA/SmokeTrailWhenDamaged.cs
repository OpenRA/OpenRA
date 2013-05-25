#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class SmokeTrailWhenDamagedInfo : ITraitInfo, Requires<LocalCoordinatesModelInfo>
	{
		[Desc("Position relative to body")]
		public readonly WVec Offset = WVec.Zero;
		public readonly int Interval = 3;
		public readonly string Sprite = "smokey";

		public object Create(ActorInitializer init) { return new SmokeTrailWhenDamaged(init.self, this); }
	}

	class SmokeTrailWhenDamaged : ITick
	{
		ILocalCoordinatesModel coords;
		SmokeTrailWhenDamagedInfo info;
		int ticks;

		public SmokeTrailWhenDamaged(Actor self, SmokeTrailWhenDamagedInfo info)
		{
			this.info = info;
			coords = self.Trait<ILocalCoordinatesModel>();
		}

		public void Tick(Actor self)
		{
			if (--ticks <= 0)
			{
				var position = self.CenterPosition;
				if (position.Z > 0 && self.GetDamageState() >= DamageState.Heavy &&
				    !self.World.FogObscures(new CPos(position)))
				{
					var offset = info.Offset.Rotate(coords.QuantizeOrientation(self, self.Orientation));
					var pos = position + coords.LocalToWorld(offset);
					self.World.AddFrameEndTask(w => w.Add(new Smoke(w, pos, info.Sprite)));
				}

				ticks = info.Interval;
			}
		}
	}
}
