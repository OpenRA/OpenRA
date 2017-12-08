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
	[Desc("Special case trait for actors that need to define screen map bounds manually.")]
	public class CustomRenderBoundsInfo : ITraitInfo
	{
		[FieldLoader.Require]
		public readonly int[] Bounds = null;

		public object Create(ActorInitializer init) { return new CustomRenderBounds(this); }
	}

	public class CustomRenderBounds : IRender
	{
		readonly CustomRenderBoundsInfo info;
		public CustomRenderBounds(CustomRenderBoundsInfo info) { this.info = info; }

		Rectangle IRender.AutoRenderBounds(Actor self)
		{
			var size = new int2(info.Bounds[0], info.Bounds[1]);
			var offset = -size / 2;
			if (info.Bounds.Length > 2)
				offset += new int2(info.Bounds[2], info.Bounds[3]);

			return new Rectangle(offset.X, offset.Y, size.X, size.Y);
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			return SpriteRenderable.None;
		}
	}
}
