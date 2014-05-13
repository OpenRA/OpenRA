#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class DevCommandsInfo : TraitInfo<DevCommands> { }

	public class DevCommands : IChatCommand, IWorldLoaded
	{
		World world;

		public DevCommands() { }

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			var console = world.WorldActor.Trait<ChatCommands>();
			var help = world.WorldActor.Trait<HelpCommand>();

			console.RegisterCommand("shroud", this);
			console.RegisterCommand("give", this);
			console.RegisterCommand("instabuild", this);
			console.RegisterCommand("buildrange", this);
			console.RegisterCommand("power", this);
			console.RegisterCommand("tech", this);

			help.RegisterHelp("shroud", "enables or disables shroud.");
			help.RegisterHelp("give", "gives the default or specified amount of money.");
			help.RegisterHelp("instabuild", "enables or disables instant building");
			help.RegisterHelp("buildrange", "allows or disallows you to build out of your build-range");
			help.RegisterHelp("power", "enables or disables infinite power");
			help.RegisterHelp("tech", "gives or takes the ability to build everything");
		}

		public void InvokeCommand(string name, string arg)
		{
			if (!world.LobbyInfo.GlobalSettings.AllowCheats)
			{
				Game.Debug("Cheats are disabled.");
				return;
			}

			switch (name)
			{
				case "give":
					var order = new Order("DevGiveCash", world.LocalPlayer.PlayerActor, false);
					var cash = 0;

					if (int.TryParse(arg, out cash))
						order.ExtraData = (uint)cash;

					Game.Debug("Giving {0} credits to player {1}.", (cash == 0 ? "cheat default" : cash.ToString()), world.LocalPlayer.PlayerName);
					world.IssueOrder(order);

					break;

				case "shroud": IssueDevCommand(world, "DevShroudDisable"); break;
				case "instabuild": IssueDevCommand(world, "DevFastBuild"); break;
				case "buildrange": IssueDevCommand(world, "DevBuildAnywhere"); break;
				case "power": IssueDevCommand(world, "DevUnlimitedPower"); break;
				case "tech": IssueDevCommand(world, "DevEnableTech"); break;
				case "support": IssueDevCommand(world, "DevFastCharge"); break;
			}
		}

		void IssueDevCommand(World world, string command)
		{
			world.IssueOrder(new Order(command, world.LocalPlayer.PlayerActor, false));
		}
	}
}
