#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.Common
{
	public class HiddenUnderFogInfo : TraitInfo<HiddenUnderFog> { }

	public class HiddenUnderFog : IRenderModifier, IVisibilityModifier
	{
		public bool IsVisible(Actor self, Player byPlayer)
		{
			return byPlayer == null || Shroud.GetVisOrigins(self).Any(o => byPlayer.Shroud.IsVisible(o));
		}

		public IEnumerable<IRenderable> ModifyRender(Actor self, WorldRenderer wr, IEnumerable<IRenderable> r)
		{
			return IsVisible(self, self.World.RenderPlayer) ? r : SpriteRenderable.None;
		}
	}
}
