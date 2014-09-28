#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Enables developer cheats via the chatbox. Attach this to the world actor.")]
	public class WorldCommandsInfo : TraitInfo<WorldCommands> { }

	public class WorldCommands : IChatCommand, IWorldLoaded
	{
		World world;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			var console = world.WorldActor.Trait<ChatCommands>();
			var help = world.WorldActor.Trait<HelpCommand>();

			Action<string, string> register = (name, helpText) =>
			{
				console.RegisterCommand(name, this);
				help.RegisterHelp(name, helpText);
			};

			register("grid", "toggles cell debug overlay.");
			register("grid full", "cycles fullcell grid rendering.");
			register("grid half", "cycles halfcell grid rendering.");
			register("grid above", "draws grid above actors.");
			register("grid below", "draws grid below actors.");
			register("grid type", "cycles grid type (fullmap vs mouse radius).");
			register("grid range <integer>", "sets the grid mouse radius.");
			register("grid reset", "resets grid options to yaml values.");
		}

		public void InvokeCommand(string name, string arg)
		{
			if (!world.AllowDevCommands)
			{
				Game.Debug("Cheats are disabled.");
				return;
			}

			switch (name)
			{
				case "grid":
					var os = "DevCellDebug";

					if (!string.IsNullOrEmpty(arg))
						os = os + ".{0}".F(arg.Substring(1));

					IssueDevCommand(world, os);
					break;
			}
		}

		static void IssueDevCommand(World world, string command)
		{
			world.IssueOrder(new Order(command, world.WorldActor, false));
		}
	}
}
