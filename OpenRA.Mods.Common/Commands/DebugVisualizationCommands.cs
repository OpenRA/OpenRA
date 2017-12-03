#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Commands
{
	[Desc("Enables visualization commands via the chatbox. Attach this to the world actor.")]
	public class DebugVisualizationCommandsInfo : TraitInfo<DebugVisualizationCommands> { }

	public class DebugVisualizationCommands : IChatCommand, IWorldLoaded
	{
		DebugVisualizations debugVis;
		DeveloperMode devMode;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			var world = w;
			debugVis = world.WorldActor.TraitOrDefault<DebugVisualizations>();

			if (world.LocalPlayer != null)
				devMode = world.LocalPlayer.PlayerActor.Trait<DeveloperMode>();

			if (debugVis == null)
				return;

			var console = world.WorldActor.Trait<ChatCommands>();
			var help = world.WorldActor.Trait<HelpCommand>();

			Action<string, string> register = (name, helpText) =>
			{
				console.RegisterCommand(name, this);
				help.RegisterHelp(name, helpText);
			};

			register("showcombatgeometry", "toggles combat geometry overlay.");
			register("showrendergeometry", "toggles render geometry overlay.");
			register("showscreenmap", "toggles screen map overlay.");
			register("showdepthbuffer", "toggles depth buffer overlay.");
			register("showactortags", "toggles actor tags overlay.");
		}

		public void InvokeCommand(string name, string arg)
		{
			switch (name)
			{
				case "showcombatgeometry":
					debugVis.CombatGeometry ^= true;
					break;

				case "showrendergeometry":
					debugVis.RenderGeometry ^= true;
					break;

				case "showscreenmap":
					if (devMode == null || devMode.Enabled)
						debugVis.ScreenMap ^= true;
					break;

				case "showdepthbuffer":
					debugVis.DepthBuffer ^= true;
					break;

				case "showactortags":
					debugVis.ActorTags ^= true;
					break;
			}
		}
	}
}
