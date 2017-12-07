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

using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Special case trait for actors that need to define targetable area and screen map bounds manually.")]
	public class CustomRenderSizeInfo : ITraitInfo, IAutoRenderSizeInfo
	{
		[FieldLoader.Require]
		public readonly int[] CustomBounds = null;

		public object Create(ActorInitializer init) { return new CustomRenderSize(this); }
	}

	public class CustomRenderSize : IAutoRenderSize, IMouseBounds
	{
		readonly CustomRenderSizeInfo info;
		public CustomRenderSize(CustomRenderSizeInfo info) { this.info = info; }

		public int2 RenderSize(Actor self)
		{
			return new int2(info.CustomBounds[0], info.CustomBounds[1]);
		}

		Rectangle IMouseBounds.MouseoverBounds(Actor self, WorldRenderer wr)
		{
			if (info.CustomBounds == null)
				return Rectangle.Empty;

			var size = new int2(info.CustomBounds[0], info.CustomBounds[1]);

			var offset = -size / 2;
			if (info.CustomBounds.Length > 2)
				offset += new int2(info.CustomBounds[2], info.CustomBounds[3]);

			var xy = wr.ScreenPxPosition(self.CenterPosition);
			return new Rectangle(xy.X, xy.Y, size.X, size.Y);
		}
	}
}
