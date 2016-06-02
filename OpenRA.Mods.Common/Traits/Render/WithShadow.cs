#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Clones the aircraft sprite with another palette below it.")]
	class WithShadowInfo : ITraitInfo
	{
		[PaletteReference] public readonly string Palette = "shadow";

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
			// Contrails shouldn't cast shadows
			var height = self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length;
			var shadowSprites = r.Where(s => !s.IsDecoration)
				.Select(a => a.WithPalette(wr.Palette(info.Palette))
					.OffsetBy(new WVec(0, 0, -height))
					.WithZOffset(a.ZOffset + height)
					.AsDecoration());

			return shadowSprites.Concat(r);
		}
	}
}
