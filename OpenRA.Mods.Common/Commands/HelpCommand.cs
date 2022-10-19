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

		[TranslationReference]
		static readonly string AvailableCommands = "available-commands";

		[TranslationReference]
		static readonly string NoDescription = "no-description";

		[TranslationReference]
		static readonly string HelpDescription = "help-description";

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
			RegisterHelp("help", HelpDescription);
		}

		public void InvokeCommand(string name, string arg)
		{
			TextNotificationsManager.Debug(Game.ModData.Translation.GetString(AvailableCommands));

			foreach (var key in console.Commands.Keys)
			{
				if (!helpDescriptions.TryGetValue(key, out var description))
					description = Game.ModData.Translation.GetString(NoDescription);

				TextNotificationsManager.Debug($"{key}: {description}");
			}
		}

		public void RegisterHelp(string name, string description)
		{
			helpDescriptions[name] = Game.ModData.Translation.GetString(description);
		}
	}
}
