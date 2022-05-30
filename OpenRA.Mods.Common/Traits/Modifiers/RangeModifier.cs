#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Modifies the range of weapons fired by this actor by a flat amount.")]
	public class RangeModifierInfo : ConditionalTraitInfo, IFlatRangeModifierInfo
	{
		[FieldLoader.Require]
		[Desc("Amount to increase to range (negative to decrease).")]
		public readonly WDist Modifier = WDist.Zero;

		public override object Create(ActorInitializer init) { return new RangeModifier(this); }

		WDist IFlatRangeModifierInfo.GetRangeModifierDefault() { return EnabledByDefault ? Modifier : WDist.Zero; }
	}

	public class RangeModifier : ConditionalTrait<RangeModifierInfo>, IFlatRangeModifier
	{
		public RangeModifier(RangeModifierInfo info)
			: base(info) { }

		WDist IFlatRangeModifier.GetRangeModifier() { return IsTraitDisabled ? WDist.Zero : Info.Modifier; }
	}
}
