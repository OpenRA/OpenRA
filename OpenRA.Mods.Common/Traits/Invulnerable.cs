#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This unit cannot be damaged.")]
	class InvulnerableInfo : TraitInfo<Invulnerable> { }

	class Invulnerable : IDamageModifier
	{
		public int GetDamageModifier(Actor attacker, IWarhead warhead) { return 0; }
	}
}
