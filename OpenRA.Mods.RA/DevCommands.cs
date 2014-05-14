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

			console.RegisterCommand("disableshroud", this);
			console.RegisterCommand("givecash", this);
			console.RegisterCommand("instantbuild", this);
			console.RegisterCommand("buildanywhere", this);
			console.RegisterCommand("unlimitedpower", this);
			console.RegisterCommand("enabletech", this);
			console.RegisterCommand("instantcharge", this);

			help.RegisterHelp("disableshroud", "toggles shroud.");
			help.RegisterHelp("givecash", "gives the default or specified amount of money.");
			help.RegisterHelp("instantbuild", "toggles instant building.");
			help.RegisterHelp("buildanywhere", "toggles you the ability to build anywhere.");
			help.RegisterHelp("unlimitedpower", "toggles infinite power.");
			help.RegisterHelp("enabletech", "toggles the ability to build everything.");
			help.RegisterHelp("instantcharge", "toggles instant support power charging.");
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
			}
		}

		void IssueDevCommand(World world, string command)
		{
			world.IssueOrder(new Order(command, world.LocalPlayer.PlayerActor, false));
		}
	}
}
