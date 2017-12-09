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

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Display a colored overlay when a timed condition is active.")]
	public class WithColoredOverlayInfo : ConditionalTraitInfo
	{
		[Desc("Palette to use when rendering the overlay")]
		[PaletteReference] public readonly string Palette = "invuln";

		public override object Create(ActorInitializer init) { return new WithColoredOverlay(this); }
	}

	public class WithColoredOverlay : ConditionalTrait<WithColoredOverlayInfo>, IRenderModifier
	{
		public WithColoredOverlay(WithColoredOverlayInfo info)
			: base(info) { }

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (IsTraitDisabled)
				return r;

			return ModifiedRender(self, wr, r);
		}

		IEnumerable<IRenderable> ModifiedRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			foreach (var a in r)
			{
				yield return a;

				if (!a.IsDecoration)
					yield return a.WithPalette(wr.Palette(Info.Palette))
						.WithZOffset(a.ZOffset + 1)
						.AsDecoration();
			}
		}

		IEnumerable<Rectangle> IRenderModifier.ModifyScreenBounds(Actor self, WorldRenderer wr, IEnumerable<Rectangle> bounds)
		{
			return bounds;
		}
	}
}