#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Globalization;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Commands
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

			register("disableshroud", "toggles shroud and minimap.");
			register("givecash", "gives the default or specified amount of money.");
			register("givecashall", "gives the default or specified amount of money to all players and ai.");
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
			if (world.LocalPlayer == null)
				return;

			if (!world.AllowDevCommands)
			{
				Game.Debug("Cheats are disabled.");
				return;
			}

			switch (name)
			{
				case "givecash":
					var givecashorder = new Order("DevGiveCash", world.LocalPlayer.PlayerActor, false);
					int cash;
					int.TryParse(arg, out cash);

					givecashorder.ExtraData = (uint)cash;
					Game.Debug("Giving {0} credits to player {1}.", cash == 0 ? "cheat default" : cash.ToString(CultureInfo.InvariantCulture), world.LocalPlayer.PlayerName);
					world.IssueOrder(givecashorder);

					break;

				case "givecashall":
					int.TryParse(arg, out cash);

					foreach (var player in world.Players.Where(p => !p.NonCombatant))
					{
						var givecashall = new Order("DevGiveCash", player.PlayerActor, false);
						givecashall.ExtraData = (uint)cash;
						Game.Debug("Giving {0} credits to player {1}.", cash == 0 ? "cheat default" : cash.ToString(CultureInfo.InvariantCulture), player.PlayerName);
						world.IssueOrder(givecashall);
					}

					break;

				case "disableshroud": IssueDevCommand(world, "DevShroudDisable"); break;
				case "instantbuild": IssueDevCommand(world, "DevFastBuild"); break;
				case "buildanywhere": IssueDevCommand(world, "DevBuildAnywhere"); break;
				case "unlimitedpower": IssueDevCommand(world, "DevUnlimitedPower"); break;
				case "enabletech": IssueDevCommand(world, "DevEnableTech"); break;
				case "instantcharge": IssueDevCommand(world, "DevFastCharge"); break;

				case "all":
					IssueDevCommand(world, "DevAll");
					break;

				case "crash":
					throw new DevException();

				case "levelup":
					var level = 0;
					int.TryParse(arg, out level);

					foreach (var actor in world.Selection.Actors)
					{
						if (actor.IsDead || actor.Destroyed)
							continue;

						var leveluporder = new Order("DevLevelUp", actor, false);
						leveluporder.ExtraData = (uint)level;

						if (actor.HasTrait<GainsExperience>())
							world.IssueOrder(leveluporder);
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
