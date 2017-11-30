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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Special case trait for actors that need to define targetable area and screen map bounds manually.")]
	public class CustomRenderBoundsInfo : ITraitInfo, IAutoRenderSizeInfo
	{
		[FieldLoader.Require]
		public readonly int[] Bounds = null;

		public object Create(ActorInitializer init) { return new CustomRenderBounds(this); }
	}

	public class CustomRenderBounds : IAutoRenderSize
	{
		readonly CustomRenderBoundsInfo info;
		public CustomRenderBounds(CustomRenderBoundsInfo info) { this.info = info; }

		int2 IAutoRenderSize.RenderSize(Actor self)
		{
			return new int2(info.Bounds[0], info.Bounds[1]);
		}
	}
}
