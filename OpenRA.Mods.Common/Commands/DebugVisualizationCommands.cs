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

using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Commands
{
	[TraitLocation(SystemActors.World)]
	[Desc("Enables visualization commands via the chatbox. Attach this to the world actor.")]
	public class DebugVisualizationCommandsInfo : TraitInfo<DebugVisualizationCommands> { }

	public class DebugVisualizationCommands : IChatCommand, IWorldLoaded
	{
		[TranslationReference]
		static readonly string CombatGeometryDescription = "combat-geometry-description";

		[TranslationReference]
		static readonly string RenderGeometryDescription = "render-geometry-description";

		[TranslationReference]
		static readonly string ScreenMapOverlayDescription = "screen-map-overlay-description";

		[TranslationReference]
		static readonly string DepthBufferDescription = "depth-buffer-description";

		[TranslationReference]
		static readonly string ActorTagsOverlayDescripition = "actor-tags-overlay-description";

		readonly IDictionary<string, (string Description, Action<DebugVisualizations, DeveloperMode> Handler)> commandHandlers = new Dictionary<string, (string Description, Action<DebugVisualizations, DeveloperMode> Handler)>
		{
			{ "combat-geometry", (CombatGeometryDescription, CombatGeometry) },
			{ "render-geometry", (RenderGeometryDescription, RenderGeometry) },
			{ "screen-map", (ScreenMapOverlayDescription, ScreenMap) },
			{ "depth-buffer", (DepthBufferDescription, DepthBuffer) },
			{ "actor-tags", (ActorTagsOverlayDescripition, ActorTags) },
		};

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

			foreach (var command in commandHandlers)
			{
				if (command.Key == "depth-buffer" && !w.Map.Grid.EnableDepthBuffer)
					continue;

				console.RegisterCommand(command.Key, this);
				help.RegisterHelp(command.Key, command.Value.Description);
			}
		}

		static void CombatGeometry(DebugVisualizations debugVis, DeveloperMode devMode)
		{
			debugVis.CombatGeometry ^= true;
		}

		static void RenderGeometry(DebugVisualizations debugVis, DeveloperMode devMode)
		{
			debugVis.RenderGeometry ^= true;
		}

		static void ScreenMap(DebugVisualizations debugVis, DeveloperMode devMode)
		{
			if (devMode == null || devMode.Enabled)
				debugVis.ScreenMap ^= true;
		}

		static void DepthBuffer(DebugVisualizations debugVis, DeveloperMode devMode)
		{
			debugVis.DepthBuffer ^= true;
		}

		static void ActorTags(DebugVisualizations debugVis, DeveloperMode devMode)
		{
			debugVis.ActorTags ^= true;
		}

		public void InvokeCommand(string name, string arg)
		{
			if (commandHandlers.TryGetValue(name, out var command))
				command.Handler(debugVis, devMode);
		}
	}
}
