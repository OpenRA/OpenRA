#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	[Desc("Modifies the bale values delivered to this refinery.")]
	public class RefineryResourceMultiplierInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Percentage modifier to apply.")]
		public readonly int Modifier = 100;

		public override object Create(ActorInitializer init) { return new RefineryResourceMultiplier(this); }
	}

	public class RefineryResourceMultiplier : ConditionalTrait<RefineryResourceMultiplierInfo>, Requires<RefineryInfo>
	{
		public RefineryResourceMultiplier(RefineryResourceMultiplierInfo info)
			: base(info) { }

		public int GetModifier() { return IsTraitDisabled ? 100 : Info.Modifier; }
	}
}
