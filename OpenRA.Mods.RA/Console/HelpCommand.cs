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
	public class HelpCommandInfo : TraitInfo<HelpCommand> { }

	public class HelpCommand : IChatCommand, IWorldLoaded
	{
		World world;
		ChatCommands console;
		Dictionary<string, string> helpDescriptions;

		public HelpCommand() 
		{
			helpDescriptions = new Dictionary<string, string>();
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			console = world.WorldActor.Trait<ChatCommands>();

			console.RegisterCommand("help", this);
			RegisterHelp("help", "provides useful info about various commands");
		}

		public void InvokeCommand(string name, string arg)
		{	
			Game.Debug("Here are the available commands:");
			
			foreach (var key in console.Commands.Keys)
			{
				var description = "";
				if (!helpDescriptions.TryGetValue(key, out description))
					description = "no description available.";

				Game.Debug("{0}: {1}", key, description);
			}
		}

		public void RegisterHelp(string name, string description)
		{
			helpDescriptions.Add(name, description);
		}
	}
}