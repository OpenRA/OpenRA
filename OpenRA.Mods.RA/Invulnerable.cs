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
using OpenRA.Mods.Common;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("This unit cannot be damaged (while this trait is enabled).")]
	public class InvulnerableInfo : ConditionalTraitInfo, ITraitInfo
	{
		public object Create(ActorInitializer init) { return new Invulnerable(this); }
	}

	public class Invulnerable : ConditionalTrait<InvulnerableInfo>, IDamageModifier
	{
		public Invulnerable(InvulnerableInfo info)
			: base (info) { }

		public int GetDamageModifier(Actor attacker, DamageWarhead warhead)
		{
			return IsTraitDisabled ? 100 : 0;
		}
	}
}
