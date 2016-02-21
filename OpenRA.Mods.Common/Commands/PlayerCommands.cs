#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Commands
{
	[Desc("Allows the player to pause or surrender the game via the chatbox. Attach this to the world actor.")]
	public class PlayerCommandsInfo : TraitInfo<PlayerCommands> { }

	public class PlayerCommands : IChatCommand, IWorldLoaded
	{
		World world;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			var console = world.WorldActor.Trait<ChatCommands>();
			var help = world.WorldActor.Trait<HelpCommand>();

			console.RegisterCommand("pause", this);
			help.RegisterHelp("pause", "pause or unpause the game");
			console.RegisterCommand("surrender", this);
			help.RegisterHelp("surrender", "self-destruct everything and lose the game");
		}

		public void InvokeCommand(string name, string arg)
		{
			if (world.LocalPlayer == null)
				return;

			switch (name)
			{
				case "pause":
					world.IssueOrder(new Order("PauseGame", null, false)
					{
						TargetString = world.Paused ? "UnPause" : "Pause"
					});
					break;
				case "surrender":
					world.IssueOrder(new Order("Surrender", world.LocalPlayer.PlayerActor, false));
					break;
			}
		}
	}
}