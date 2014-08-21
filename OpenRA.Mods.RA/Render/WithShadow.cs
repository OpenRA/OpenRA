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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Air;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	[Desc("Clones the aircraft sprite with another palette below it.")]
	class WithShadowInfo : ITraitInfo
	{
		public readonly string Palette = "shadow";

		public object Create(ActorInitializer init) { return new WithShadow(this); }
	}

	class WithShadow : IRenderModifier
	{
		WithShadowInfo info;

		public WithShadow(WithShadowInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			var ios = self.Trait<IOccupySpace>();

			/* rude hack */
			var flying = ios.CenterPosition.Z > 0;
			var visualOffset = (ios is Helicopter && flying)
				? (int)Math.Abs((self.ActorID + Game.LocalTick) / 5 % 4 - 1) - 1 : 0;

			// Contrails shouldn't cast shadows
			var shadowSprites = r.Where(s => !s.IsDecoration)
				.Select(a => a.WithPalette(wr.Palette(info.Palette))
				.OffsetBy(new WVec(0, 0, -a.Pos.Z))
				.WithZOffset(a.ZOffset + a.Pos.Z)
				.AsDecoration());

			var worldVisualOffset = new WVec(0,0,-43*visualOffset);
			var flyingSprites = !flying ? r :
				r.Select(a => a.OffsetBy(worldVisualOffset));

			return shadowSprites.Concat(flyingSprites);
		}
	}
}
