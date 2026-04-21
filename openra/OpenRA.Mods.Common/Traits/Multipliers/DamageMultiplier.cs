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
	[Desc("Modifies the damage applied to this actor.",
		"Use 0 to make actor invulnerable.")]
	public class DamageMultiplierInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Percentage modifier to apply.")]
		public readonly int Modifier = 100;

		public override object Create(ActorInitializer init) { return new DamageMultiplier(this); }
	}

	public class DamageMultiplier : ConditionalTrait<DamageMultiplierInfo>, IDamageModifier
	{
		public DamageMultiplier(DamageMultiplierInfo info)
			: base(info) { }

		int IDamageModifier.GetDamageModifier(Actor attacker, Damage damage)
		{
			return IsTraitDisabled ? 100 : Info.Modifier;
		}
	}
}
