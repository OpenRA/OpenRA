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
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Commands
{
	[TraitLocation(SystemActors.World)]
	[IncludeStaticFluentReferences(typeof(DebugVisualizationCommands))]
	[Desc("Enables visualization commands via the chatbox. Attach this to the world actor.")]
	public class DebugVisualizationCommandsInfo : TraitInfo<DebugVisualizationCommands> { }

	public class DebugVisualizationCommands : IChatCommand, IWorldLoaded
	{
		[FluentReference]
		const string CheatsDisabled = "notification-cheats-disabled";

		[FluentReference("cheat", "player")]
		const string CheatEnabled = "notification-cheat-enabled";

		[FluentReference("cheat", "player")]
		const string CheatDisabled = "notification-cheat-disabled";

		[FluentReference]
		const string CombatGeometryDescription = "description-combat-geometry";

		[FluentReference]
		const string RenderGeometryDescription = "description-render-geometry";

		[FluentReference]
		const string ScreenMapOverlayDescription = "description-screen-map-overlay";

		[FluentReference]
		const string DepthBufferDescription = "description-depth-buffer";

		[FluentReference]
		const string ActorTagsOverlayDescripition = "description-actor-tags-overlay";

		public static class Commands
		{
			public const string CombatGeometry = "combat-geometry";
			public const string RenderGeometry = "render-geometry";
			public const string ScreenMap = "screen-map";
			public const string DepthBuffer = "depth-buffer";
			public const string ActorTags = "actor-tags";
		}

		public static class Orders
		{
			public const string CombatGeometry = "DevCombatGeometry";
			public const string RenderGeometry = "DevRenderGeometry";
			public const string ScreenMap = "DevScreenMap";
			public const string DepthBuffer = "DevDepthBuffer";
			public const string ActorTags = "DevActorTags";
		}

		readonly Dictionary<string,
			(string Description, Action<DebugVisualizations> Handler, string CheatName, Func<DebugVisualizations, bool> GetState)>
			commandHandlers = new()
			{
				{ Commands.CombatGeometry, (CombatGeometryDescription, CombatGeometry, Orders.CombatGeometry, d => d.CombatGeometry) },
				{ Commands.RenderGeometry, (RenderGeometryDescription, RenderGeometry, Orders.RenderGeometry, d => d.RenderGeometry) },
				{ Commands.ScreenMap, (ScreenMapOverlayDescription, ScreenMap, Orders.ScreenMap, d => d.ScreenMap) },
				{ Commands.DepthBuffer, (DepthBufferDescription, DepthBuffer, Orders.DepthBuffer, d => d.DepthBuffer) },
				{ Commands.ActorTags, (ActorTagsOverlayDescripition, ActorTags, Orders.ActorTags, d => d.ActorTags) },
			};

		DebugVisualizations debugVis;
		DeveloperMode devMode;
		World world;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			debugVis = world.WorldActor.TraitOrDefault<DebugVisualizations>();
			devMode = world.LocalPlayer?.PlayerActor.Trait<DeveloperMode>();

			if (debugVis == null || devMode == null)
				return;

			var console = world.WorldActor.Trait<ChatCommands>();
			var help = world.WorldActor.Trait<HelpCommand>();

			foreach (var command in commandHandlers)
			{
				if (command.Key == Commands.DepthBuffer && !w.Map.Grid.EnableDepthBuffer)
					continue;

				console.RegisterCommand(command.Key, this);
				help.RegisterHelp(command.Key, command.Value.Description);
			}
		}

		static void CombatGeometry(DebugVisualizations debugVis)
		{
			debugVis.CombatGeometry ^= true;
		}

		static void RenderGeometry(DebugVisualizations debugVis)
		{
			debugVis.RenderGeometry ^= true;
		}

		static void ScreenMap(DebugVisualizations debugVis)
		{
			debugVis.ScreenMap ^= true;
		}

		static void DepthBuffer(DebugVisualizations debugVis)
		{
			debugVis.DepthBuffer ^= true;
		}

		static void ActorTags(DebugVisualizations debugVis)
		{
			debugVis.ActorTags ^= true;
		}

		public void InvokeCommand(string name, string _)
		{
			if (!commandHandlers.TryGetValue(name, out var command))
				return;

			if (devMode == null || !devMode.Enabled)
			{
				TextNotificationsManager.Debug(FluentProvider.GetMessage(CheatsDisabled));
				return;
			}

			command.Handler(debugVis);
			SendNotification(command.GetState(debugVis), command.CheatName);
		}

		void SendNotification(bool enabled, string cheatName)
		{
			var notification = enabled ? CheatEnabled : CheatDisabled;
			var playerName = world.LocalPlayer != null ? world.LocalPlayer.ResolvedPlayerName : "";
			TextNotificationsManager.Debug(FluentProvider.GetMessage(notification,
				"cheat", cheatName,
				"player", playerName));
		}
	}
}
