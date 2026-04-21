#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Modifies the movement speed of this actor.")]
	public class SpeedMultiplierInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Percentage modifier to apply.")]
		public readonly int Modifier = 100;

		public override object Create(ActorInitializer init) { return new SpeedMultiplier(this); }
	}

	public class SpeedMultiplier : ConditionalTrait<SpeedMultiplierInfo>, ISpeedModifier
	{
		public SpeedMultiplier(SpeedMultiplierInfo info)
			: base(info) { }

		int ISpeedModifier.GetSpeedModifier() { return IsTraitDisabled ? 100 : Info.Modifier; }
	}
}
