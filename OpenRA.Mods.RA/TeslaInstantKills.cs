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
	public class TeslaInstantKillsInfo : ITraitInfo
	{ 
		[Desc("InfDeath that leads to instant kill.")]
		public readonly string InfDeath = "6";

		public object Create(ActorInitializer init) { return new TeslaInstantKills(this); }
	}

	public class TeslaInstantKills : IDamageModifier
	{
		TeslaInstantKillsInfo info;

		public TeslaInstantKills(TeslaInstantKillsInfo info) { this.info = info; }

		public float GetDamageModifier(Actor attacker, WarheadInfo warhead)
		{
			if( warhead != null && warhead.InfDeath == info.InfDeath )
				return 1000f;
			return 1f;
		}
	}
}
