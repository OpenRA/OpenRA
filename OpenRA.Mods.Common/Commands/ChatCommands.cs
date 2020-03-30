#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Commands
{
	[Desc("Enables commands triggered by typing them into the chatbox. Attach this to the world actor.")]
	public class ChatCommandsInfo : TraitInfo<ChatCommands> { }

	public class ChatCommands : INotifyChat
	{
		public Dictionary<string, IChatCommand> Commands { get; private set; }

		public ChatCommands()
		{
			Commands = new Dictionary<string, IChatCommand>();
		}

		public bool OnChat(string playername, string message)
		{
			if (message.StartsWith("/"))
			{
				var name = message.Substring(1).Split(' ')[0].ToLowerInvariant();
				var command = Commands.FirstOrDefault(x => x.Key == name);

				if (command.Value != null)
					command.Value.InvokeCommand(name.ToLowerInvariant(), message.Substring(1 + name.Length).Trim());
				else
					Game.Debug("{0} is not a valid command.", name);

				return false;
			}

			return true;
		}

		public void RegisterCommand(string name, IChatCommand command)
		{
			// Override possible duplicates instead of crashing.
			Commands[name.ToLowerInvariant()] = command;
		}
	}

	public interface IChatCommand
	{
		void InvokeCommand(string command, string arg);
	}
}
