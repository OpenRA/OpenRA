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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Modifies the range of weapons fired by this actor.")]
	public class RangeMultiplierInfo : ConditionalTraitInfo, IRangeModifierInfo
	{
		[FieldLoader.Require]
		[Desc("Percentage modifier to apply.")]
		public readonly int Modifier = 100;

		[Desc("Higher priority modifiers are applied first.")]
		public readonly int Priority = 0;

		public override object Create(ActorInitializer init) { return new RangeMultiplier(this); }

		int IRangeModifierInfo.GetRangeModifierDefault() { return EnabledByDefault ? Modifier : 100; }
	}

	public class RangeMultiplier : ConditionalTrait<RangeMultiplierInfo>, IRangeModifier
	{
		public RangeMultiplier(RangeMultiplierInfo info)
			: base(info) { }

		IModifier IRangeModifier.GetRangeModifier()
		{
			var modifier = new Modifier
			{
				Type = ModifierType.Relative,
				Priority = Info.Priority,
				Value = IsTraitDisabled ? 100 : Info.Modifier
			};

			return modifier;
		}
	}
}
