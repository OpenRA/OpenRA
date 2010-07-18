#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class TeslaInstantKillsInfo : TraitInfo<TeslaInstantKills> { }

	class TeslaInstantKills : IDamageModifier
	{
		public float GetDamageModifier( WarheadInfo warhead )
		{
			if( warhead != null && warhead.InfDeath == 5 )
				return 1000f;
			return 1f;
		}
	}
}
