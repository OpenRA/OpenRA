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

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Commands
{
	[TraitLocation(SystemActors.World)]
	[IncludeStaticFluentReferences(typeof(PlayerCommands))]
	[Desc("Allows the player to whisper to another player, pause or surrender the game via the chatbox. Attach this to the world actor.")]
	public class PlayerCommandsInfo : TraitInfo<PlayerCommands> { }

	public class PlayerCommands : IChatCommand, IWorldLoaded
	{
		[FluentReference]
		const string PauseDescription = "description-pause-description";

		[FluentReference]
		const string SurrenderDescription = "description-surrender-description";

		[FluentReference]
		const string WhisperDescription = "description-whisper-description";

		[FluentReference]
		const string WhisperInvalidPlayer = "notification-whisper-invalid-player";

		World world;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			var console = world.WorldActor.Trait<ChatCommands>();
			var help = world.WorldActor.Trait<HelpCommand>();

			console.RegisterCommand("whisper", this);
			help.RegisterHelp("whisper", WhisperDescription);

			console.RegisterCommand("pause", this);
			help.RegisterHelp("pause", PauseDescription);

			console.RegisterCommand("surrender", this);
			help.RegisterHelp("surrender", SurrenderDescription);
		}

		public void InvokeCommand(string name, string arg)
		{
			switch (name)
			{
				case "whisper":
				{
					var matches = world.LobbyInfo.Clients
						.Where(c => !c.IsBot && !string.IsNullOrEmpty(c.Name)
							&& arg.StartsWith(c.Name, StringComparison.Ordinal)
							&& (arg.Length == c.Name.Length || arg[c.Name.Length] == ' '))
						.OrderByDescending(c => c.Name.Length)
						.ToList();

					var target = matches.FirstOrDefault();
					if (target == null || matches.Count(c => c.Name == target.Name) > 1)
					{
						TextNotificationsManager.Debug(FluentProvider.GetMessage(WhisperInvalidPlayer));
						break;
					}

					world.IssueOrder(new Order("Whisper", null, false)
					{
						IsImmediate = true,
						TargetString = arg[target.Name.Length..].Trim(),
						ExtraData = (uint)target.Index
					});

					break;
				}

				case "pause":
					if (Game.IsHost || (world.LocalPlayer != null && world.LocalPlayer.WinState != WinState.Lost))
						world.SetPauseState(!world.Paused);

					break;
				case "surrender":
					if (world.LocalPlayer != null)
						world.IssueOrder(new Order("Surrender", world.LocalPlayer.PlayerActor, false));

					break;
			}
		}
	}
}
