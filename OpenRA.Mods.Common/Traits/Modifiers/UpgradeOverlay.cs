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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Display a colored overlay when a timed upgrade is active.")]
	public class UpgradeOverlayInfo : UpgradableTraitInfo
	{
		[Desc("Palette to use when rendering the overlay")]
		[PaletteReference] public readonly string Palette = "invuln";

		public override object Create(ActorInitializer init) { return new UpgradeOverlay(this); }
	}

	public class UpgradeOverlay : UpgradableTrait<UpgradeOverlayInfo>, IRenderModifier
	{
		public UpgradeOverlay(UpgradeOverlayInfo info)
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
	}
}