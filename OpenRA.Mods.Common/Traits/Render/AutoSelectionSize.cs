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

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Automatically calculates the targetable area and screen map boundaries from the sprite size.")]
	public class AutoSelectionSizeInfo : ITraitInfo, Requires<RenderSpritesInfo>, IAutoSelectionSizeInfo
	{
		public object Create(ActorInitializer init) { return new AutoSelectionSize(this); }
	}

	public class AutoSelectionSize : IAutoSelectionSize
	{
		public AutoSelectionSize(AutoSelectionSizeInfo info) { }

		public int2 SelectionSize(Actor self)
		{
			var rs = self.Trait<RenderSprites>();
			return rs.AutoSelectionSize(self);
		}
	}
}
