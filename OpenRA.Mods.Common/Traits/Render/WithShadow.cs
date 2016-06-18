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
	[Desc("Clones the actor sprite with another palette below it.")]
	public class WithShadowInfo : UpgradableTraitInfo
	{
		[PaletteReference] public readonly string Palette = "shadow";

		[Desc("Shadow position offset relative to actor position (ground level).")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Shadow Z offset relative to actor sprite.")]
		public readonly int ZOffset = -5;

		public override object Create(ActorInitializer init) { return new WithShadow(this); }
	}

	public class WithShadow : UpgradableTrait<WithShadowInfo>, IRenderModifier
	{
		readonly WithShadowInfo info;

		public WithShadow(WithShadowInfo info)
			: base(info)
		{
			this.info = info;
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (IsTraitDisabled)
				return Enumerable.Empty<IRenderable>();

			if (self.IsDead || !self.IsInWorld)
				return Enumerable.Empty<IRenderable>();

			// Contrails shouldn't cast shadows
			var height = self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length;
			var shadowSprites = r.Where(s => !s.IsDecoration)
				.Select(a => a.WithPalette(wr.Palette(info.Palette))
					.OffsetBy(info.Offset - new WVec(0, 0, height))
					.WithZOffset(a.ZOffset + (height + info.ZOffset))
					.AsDecoration());

			return shadowSprites.Concat(r);
		}
	}
}
