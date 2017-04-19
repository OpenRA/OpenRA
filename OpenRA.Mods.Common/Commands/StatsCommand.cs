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

using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Commands
{
	[Desc("Prints a summary of player statistics upon game over.")]
	public class StatsCommandInfo : TraitInfo<StatsCommand>, Requires<ChatCommandsInfo> { }

	public class StatsCommand : IChatCommand, IWorldLoaded
	{
		World world;

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;

			var console = world.WorldActor.Trait<ChatCommands>();
			console.RegisterCommand("stats", this);

			var help = world.WorldActor.TraitOrDefault<HelpCommand>();
			if (help != null)
				help.RegisterHelp("stats", "prints player statistic summaries at the end of the game");
		}

		void IChatCommand.InvokeCommand(string name, string arg)
		{
			if (!world.IsGameOver || world.LocalPlayer == null || name != "stats")
				return;

			var players = world.Players.Where(a => !a.NonCombatant);

			var stats = players.ToDictionary(p => p, p => p.PlayerActor.Trait<PlayerStatistics>());
			Game.Debug("{0} caused the greatest destruction.".F(stats.MaxBy(p => p.Value.UnitsKilled + p.Value.BuildingsKilled).Key.PlayerName));
			Game.Debug("{0} lost most units.".F(stats.MaxBy(p => p.Value.UnitsDead).Key.PlayerName));

			var resources = players.ToDictionary(p => p, p => p.PlayerActor.Trait<PlayerResources>());
			Game.Debug("{0} had the largest income.".F(resources.MaxBy(p => p.Value.Earned).Key.PlayerName));
			Game.Debug("{0} spent the most.".F(resources.MaxBy(p => p.Value.Spent).Key.PlayerName));
		}
	}
}