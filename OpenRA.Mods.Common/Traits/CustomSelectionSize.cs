#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Special case trait for invisible, unselectable actors like bridge huts.",
	"Gives actor targetable area for special cases like C4 and engineer repair.",
	"This trait conflicts with AutoSelectionSize so you cannot use both, and doesn't support custom offsets.")]
	public class CustomSelectionSizeInfo : ITraitInfo
	{
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
