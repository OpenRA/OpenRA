#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	[Desc("Damage taken by this actor is multiplied based on upgrade level.",
		"Decrease to increase actor's apparent strength.",
		"Use 0 to make actor invulnerable.")]
	public class DamageMultiplierInfo : UpgradeMultiplierTraitInfo, ITraitInfo
	{
		public DamageMultiplierInfo()
			: base(new string [] { "damage" }, new int[] { 91, 87, 83, 65 }) { }

		public object Create(ActorInitializer init) { return new DamageMultiplier(this); }
	}

	public class DamageMultiplier : UpgradeMultiplierTrait, IDamageModifier
	{
		public DamageMultiplier(DamageMultiplierInfo info)
			: base(info) { }

		public int GetDamageModifier(Actor attacker, DamageWarhead warhead) { return GetModifier(); }
	}
}
