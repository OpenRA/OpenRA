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
	class HiddenUnderFogInfo : TraitInfo<HiddenUnderFog> {}

	class HiddenUnderFog : IRenderModifier, IVisibilityModifier
	{
		public bool IsVisible(Actor self, Player byPlayer)
		{
			return byPlayer == null || Shroud.GetVisOrigins(self).Any(o => byPlayer.Shroud.IsVisible(o));
		}

		static Renderable[] Nothing = { };
		public IEnumerable<Renderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<Renderable> r)
		{
			return IsVisible(self, self.World.RenderPlayer) ? r : Nothing;
		}
	}
}
