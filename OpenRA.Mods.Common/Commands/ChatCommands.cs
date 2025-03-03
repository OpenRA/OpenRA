#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Commands
{
	[TraitLocation(SystemActors.World)]
	[Desc("Enables commands triggered by typing them into the chatbox. Attach this to the world actor.")]
	public class ChatCommandsInfo : TraitInfo<ChatCommands> { }

	public class ChatCommands : INotifyChat
	{
		[FluentReference("name")]
		const string InvalidCommand = "notification-invalid-command";

		public Dictionary<string, IChatCommand> Commands { get; }

		public ChatCommands()
		{
			Commands = [];
		}

		public bool OnChat(string playername, string message)
		{
			if (message.StartsWith('/'))
			{
				var name = message[1..].Split(' ')[0].ToLowerInvariant();

				if (Commands.TryGetValue(name, out var command))
					command.InvokeCommand(name, message[(1 + name.Length)..].Trim());
				else
					TextNotificationsManager.Debug(FluentProvider.GetMessage(InvalidCommand, "name", name));

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
