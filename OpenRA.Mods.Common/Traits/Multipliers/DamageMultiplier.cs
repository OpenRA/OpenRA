#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Damage taken by this actor is multiplied based on upgrade level.",
		"Decrease to increase actor's apparent strength.",
		"Use 0 to make actor invulnerable.")]
	public class DamageMultiplierInfo : UpgradeMultiplierTraitInfo
	{
		public override object Create(ActorInitializer init) { return new DamageMultiplier(this, init.Self.Info.Name); }
	}

	public class DamageMultiplier : UpgradeMultiplierTrait, IDamageModifier
	{
		public DamageMultiplier(DamageMultiplierInfo info, string actorType)
			: base(info, "DamageMultiplier", actorType) { }

		public int GetDamageModifier(Actor attacker, IWarhead warhead) { return GetModifier(); }
	}
}
