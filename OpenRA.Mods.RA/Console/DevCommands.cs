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
	public class DevCommandsInfo : TraitInfo<DevCommands> { }

	public class DevCommands : IChatCommand, IWorldLoaded
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

			register("disableshroud", "toggles shroud.");
			register("givecash", "gives the default or specified amount of money.");
			register("instantbuild", "toggles instant building.");
			register("buildanywhere", "toggles you the ability to build anywhere.");
			register("unlimitedpower", "toggles infinite power.");
			register("enabletech", "toggles the ability to build everything.");
			register("instantcharge", "toggles instant support power charging.");
			register("all", "toggles all cheats and gives you some cash for your trouble.");
			register("crash", "crashes the game.");
			register("levelup", "adds a specified number of levels to the selected actors.");
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
				case "givecash":
					var order = new Order("DevGiveCash", world.LocalPlayer.PlayerActor, false);
					var cash = 0;

					if (int.TryParse(arg, out cash))
						order.ExtraData = (uint)cash;

					Game.Debug("Giving {0} credits to player {1}.", (cash == 0 ? "cheat default" : cash.ToString()), world.LocalPlayer.PlayerName);
					world.IssueOrder(order);

					break;

				case "disableshroud": IssueDevCommand(world, "DevShroudDisable"); break;
				case "instantbuild": IssueDevCommand(world, "DevFastBuild"); break;
				case "buildanywhere": IssueDevCommand(world, "DevBuildAnywhere"); break;
				case "unlimitedpower": IssueDevCommand(world, "DevUnlimitedPower"); break;
				case "enabletech": IssueDevCommand(world, "DevEnableTech"); break;
				case "instantcharge": IssueDevCommand(world, "DevFastCharge"); break;

				case "all":
					IssueDevCommand(world, "DevShroudDisable");
					IssueDevCommand(world, "DevFastBuild");
					IssueDevCommand(world, "DevBuildAnywhere");
					IssueDevCommand(world, "DevUnlimitedPower");
					IssueDevCommand(world, "DevEnableTech");
					IssueDevCommand(world, "DevFastCharge");
					IssueDevCommand(world, "DevGiveCash");
					break;

				case "crash":
					throw new DevException();
				
				case "levelup":
					var level = 0;
					
					if (!int.TryParse(arg, out level))
					{
						Game.Debug("{0} is not a valid number.", arg);
						break;
					}

					foreach (var actor in world.Selection.Actors)
					{
						if (actor == null)
							continue;

						world.AddFrameEndTask(w =>
						{
							var gainsExperience = actor.TraitOrDefault<GainsExperience>();
							if (gainsExperience != null)
								gainsExperience.GiveLevels(level);
						});
					}

					break;
			}
		}

		static void IssueDevCommand(World world, string command)
		{
			world.IssueOrder(new Order(command, world.LocalPlayer.PlayerActor, false));
		}

		class DevException : Exception { }
	}
}
