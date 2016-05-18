#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using Eluant;
using OpenRA.Mods.Common.Traits;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptPropertyGroup("Player")]
	public class PlayerStatsProperties : ScriptPlayerProperties, Requires<PlayerStatisticsInfo>
	{
		readonly PlayerStatistics stats;

		public PlayerStatsProperties(ScriptContext context, Player player)
			: base(context, player)
		{
			stats = player.PlayerActor.Trait<PlayerStatistics>();
		}

		[Desc("The combined value of units killed by this player.")]
		public int KillsCost { get { return stats.KillsCost; } }

		[Desc("The combined value of all units lost by this player.")]
		public int DeathsCost { get { return stats.DeathsCost; } }

		[Desc("The total number of units killed by this player.")]
		public int UnitsKilled { get { return stats.UnitsKilled; } }

		[Desc("The total number of units lost by this player.")]
		public int UnitsLost { get { return stats.UnitsDead; } }

		[Desc("The total number of buildings killed by this player.")]
		public int BuildingsKilled { get { return stats.BuildingsKilled; } }

		[Desc("The total number of buildings lost by this player.")]
		public int BuildingsLost { get { return stats.BuildingsDead; } }
	}
}