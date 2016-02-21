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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Special case trait for actors that need to define targetable area and screen map bounds manually.")]
	public class CustomSelectionSizeInfo : ITraitInfo
	{
		[FieldLoader.Require]
		public readonly int[] CustomBounds = null;

		public object Create(ActorInitializer init) { return new CustomSelectionSize(this); }
	}

	public class CustomSelectionSize : IAutoSelectionSize
	{
		readonly CustomSelectionSizeInfo info;
		public CustomSelectionSize(CustomSelectionSizeInfo info) { this.info = info; }

		public int2 SelectionSize(Actor self)
		{
			return new int2(info.CustomBounds[0], info.CustomBounds[1]);
		}
	}
}
