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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Scales the actor's move speed to simulate low gravity. Mobile already",
		"multiplies its base speed by every ISpeedModifier trait, so this needs no",
		"engine change. Being a ConditionalTrait it can be gated per unit / upgrade.")]
	public class GravitySpeedModifierInfo : ConditionalTraitInfo
	{
		[Desc("Speed multiplier as a percentage. >100 = faster (low-g bounding strides),",
			"<100 = slower (heavy suit / high-g). 100 = unchanged.")]
		public readonly int Modifier = 140;

		public override object Create(ActorInitializer init) { return new GravitySpeedModifier(this); }
	}

	public class GravitySpeedModifier : ConditionalTrait<GravitySpeedModifierInfo>, ISpeedModifier
	{
		public GravitySpeedModifier(GravitySpeedModifierInfo info)
			: base(info) { }

		int ISpeedModifier.GetSpeedModifier()
		{
			return IsTraitDisabled ? 100 : Info.Modifier;
		}
	}
}
