#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Commands
{
	[TraitLocation(SystemActors.World)]
	[Desc("Shows a list of available commands in the chatbox. Attach this to the world actor.")]
	public class HelpCommandInfo : TraitInfo<HelpCommand> { }

	public class HelpCommand : IChatCommand, IWorldLoaded
	{
		readonly Dictionary<string, string> helpDescriptions;

		World world;
		ChatCommands console;

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
			TextNotificationsManager.Debug("Here are the available commands:");

			foreach (var key in console.Commands.Keys)
			{
				if (!helpDescriptions.TryGetValue(key, out var description))
					description = "no description available.";

				TextNotificationsManager.Debug("{0}: {1}", key, description);
			}
		}

		public void RegisterHelp(string name, string description)
		{
			helpDescriptions[name] = description;
		}
	}
}
