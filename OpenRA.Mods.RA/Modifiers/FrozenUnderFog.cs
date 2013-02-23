#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class FrozenUnderFogInfo : TraitInfo<FrozenUnderFog> {}

	class FrozenUnderFog : IRenderModifier, IVisibilityModifier
	{
		public bool IsVisible(Shroud s, Actor self)
		{
			return Shroud.GetVisOrigins(self).Any(o => s.IsVisible(o));
		}

		Renderable[] cache = { };
		public IEnumerable<Renderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<Renderable> r)
		{
			if (IsVisible(self.World.RenderedShroud, self))
				cache = r.ToArray();
			return cache;
		}
	}
}
