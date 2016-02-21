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
	[Desc("Use together with CanPowerDown/RequiresPower on buildings or Husk for vehicles.")]
	public class DisabledOverlayInfo : TraitInfo<DisabledOverlay> { }

	public class DisabledOverlay : IRenderModifier
	{
		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			if (!self.IsDisabled())
				return r;

			return ModifiedRender(self, wr, r);
		}

		IEnumerable<IRenderable> ModifiedRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			foreach (var a in r)
			{
				yield return a;

				if (!a.IsDecoration)
					yield return a.WithPalette(wr.Palette("disabled"))
						.WithZOffset(a.ZOffset + 1)
						.AsDecoration();
			}
		}
	}
}