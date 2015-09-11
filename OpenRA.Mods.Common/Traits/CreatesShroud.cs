#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Hides area from enemies under shroud.", "Provides \"shroud\" range type.")]
	public class CreatesShroudInfo : RevealsShroudInfo, IRanged, IProvidesRangesInfo
	{
		[FieldLoader.Ignore] public readonly IEnumerable<IRanged> AsRanges;

		public CreatesShroudInfo() { AsRanges = new IRanged[] { this }; }

		public override object Create(ActorInitializer init) { return new CreatesShroud(init.Self, this); }
		public WDist GetMaximumRange(ActorInfo ai, World w) { return Range; }
		public WDist GetMinimumRange(ActorInfo ai, World w) { return WDist.Zero; }
		public bool ProvidesRanges(string type, string variant, ActorInfo ai, World w) { return type == "shroud"; }
		public IEnumerable<IRanged> GetRanges(string type, string variant, ActorInfo ai, World w) { yield return this; }
	}

	public class CreatesShroud : RevealsShroud, IProvidesRanges
	{
		readonly IEnumerable<IRanged> asRanges;

		public bool ProvidesRanges(string type, string variant) { return type == "shroud"; }
		public IEnumerable<IRanged> GetRanges(string type, string variant) { return asRanges; }
		public CreatesShroud(Actor self, CreatesShroudInfo info)
			: base(self, info)
		{
			asRanges = info.AsRanges;
			addCellsToPlayerShroud = (p, uv) => p.Shroud.AddProjectedShroudGeneration(self, uv);
			removeCellsFromPlayerShroud = p => p.Shroud.RemoveShroudGeneration(self);
			isDisabled = () => self.IsDisabled();
		}
	}
}