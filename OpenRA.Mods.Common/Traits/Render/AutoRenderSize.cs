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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Automatically calculates the screen map boundaries from the sprite size.")]
	public class AutoRenderSizeInfo : ITraitInfo, Requires<RenderSpritesInfo>, IAutoRenderSizeInfo
	{
		public object Create(ActorInitializer init) { return new AutoRenderSize(init.Self); }
	}

	public class AutoRenderSize : IAutoRenderSize, IMouseBounds
	{
		readonly RenderSprites rs;

		public AutoRenderSize(Actor self)
		{
			rs = self.Trait<RenderSprites>();
		}

		public int2 RenderSize(Actor self)
		{
			return rs.AutoRenderSize(self);
		}

		Rectangle IMouseBounds.MouseoverBounds(Actor self, WorldRenderer wr)
		{
			return self.TraitsImplementing<IAutoMouseBounds>()
				.Select(s => s.AutoMouseoverBounds(self, wr))
				.FirstOrDefault(r => !r.IsEmpty);
		}
	}
}
