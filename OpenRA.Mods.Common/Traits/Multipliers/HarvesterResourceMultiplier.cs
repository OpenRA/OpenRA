#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	[Desc("Modifies the bale values of this harvester.")]
	public class HarvesterResourceMultiplierInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Percentage modifier to apply.")]
		public readonly int Modifier = 100;

		public override object Create(ActorInitializer init) { return new HarvesterResourceMultiplier(this); }
	}

	public class HarvesterResourceMultiplier : ConditionalTrait<HarvesterResourceMultiplierInfo>, Requires<HarvesterInfo>
	{
		public HarvesterResourceMultiplier(HarvesterResourceMultiplierInfo info)
			: base(info) { }

		public int GetModifier() { return IsTraitDisabled ? 100 : Info.Modifier; }
	}
}
