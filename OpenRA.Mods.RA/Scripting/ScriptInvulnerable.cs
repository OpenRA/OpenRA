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

namespace OpenRA.Mods.RA
{
	[Desc("Allows map scripts to make this actor invulnerable via actor.Invulnerable = true.")]
	class ScriptInvulnerableInfo : TraitInfo<ScriptInvulnerable> {}

	class ScriptInvulnerable : IDamageModifier
	{
		public bool Invulnerable = false;

		public float GetDamageModifier(Actor attacker, WarheadInfo warhead)
		{
			return Invulnerable ? 0.0f : 1.0f;
		}
	}
}
