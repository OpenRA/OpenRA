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
		const string ActorTagsOverlayDescription = "description-actor-tags-overlay";

		[FluentReference]
		const string TargetLinesDescription = "description-target-lines";

		[FluentReference]
		const string TargetLinesDefault = "notification-target-lines-default";

		[FluentReference]
		const string TargetLinesAllPlayers = "notification-target-lines-all-players";

		[FluentReference]
		const string TargetLinesAlways = "notification-target-lines-always";

		public static class Commands
		{
			public const string CombatGeometry = "combat-geometry";
			public const string RenderGeometry = "render-geometry";
			public const string ScreenMap = "screen-map";
			public const string DepthBuffer = "depth-buffer";
			public const string ActorTags = "actor-tags";
			public const string TargetLines = "target-lines";
		}

		public static class Orders
		{
			public const string CombatGeometry = "DevCombatGeometry";
			public const string RenderGeometry = "DevRenderGeometry";
			public const string ScreenMap = "DevScreenMap";
			public const string DepthBuffer = "DevDepthBuffer";
			public const string ActorTags = "DevActorTags";
			public const string TargetLines = "DevTargetLines";
		}

		readonly Dictionary<string,
			(string Description, Action<HandlerContext> Handler)>
			commandHandlers = new()
			{
				{ Commands.CombatGeometry, (CombatGeometryDescription, CombatGeometry) },
				{ Commands.RenderGeometry, (RenderGeometryDescription, RenderGeometry) },
				{ Commands.ScreenMap, (ScreenMapOverlayDescription, ScreenMap) },
				{ Commands.DepthBuffer, (DepthBufferDescription, DepthBuffer) },
				{ Commands.ActorTags, (ActorTagsOverlayDescription, ActorTags) },
				{ Commands.TargetLines, (TargetLinesDescription, TargetLines) },
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

		static void CombatGeometry(HandlerContext context)
		{
			context.DebugVis.CombatGeometry ^= true;

			context.SendCheatNotification(context.DebugVis.CombatGeometry, Orders.CombatGeometry);
		}

		static void RenderGeometry(HandlerContext context)
		{
			context.DebugVis.RenderGeometry ^= true;

			context.SendCheatNotification(context.DebugVis.RenderGeometry, Orders.RenderGeometry);
		}

		static void ScreenMap(HandlerContext context)
		{
			context.DebugVis.ScreenMap ^= true;

			context.SendCheatNotification(context.DebugVis.ScreenMap, Orders.ScreenMap);
		}

		static void DepthBuffer(HandlerContext context)
		{
			context.DebugVis.DepthBuffer ^= true;

			context.SendCheatNotification(context.DebugVis.DepthBuffer, Orders.DepthBuffer);
		}

		static void ActorTags(HandlerContext context)
		{
			context.DebugVis.ActorTags ^= true;

			context.SendCheatNotification(context.DebugVis.ActorTags, Orders.ActorTags);
		}

		static void TargetLines(HandlerContext context)
		{
			var value = context.Argument?.ToLowerInvariant()?.Trim();
			if (string.IsNullOrEmpty(value))
				value = context.DebugVis.TargetLines == DebugTargetLines.Default ? "always" : "default";

			string key;
			switch (value)
			{
				case "all-players":
					context.DebugVis.TargetLines = DebugTargetLines.AllPlayers;
					key = TargetLinesAllPlayers;
					break;
				case "always":
					context.DebugVis.TargetLines = DebugTargetLines.AlwaysAllPlayers;
					key = TargetLinesAlways;
					break;
				default:
					context.DebugVis.TargetLines = DebugTargetLines.Default;
					key = TargetLinesDefault;
					break;
			}

			TextNotificationsManager.Debug(FluentProvider.GetMessage(key));
		}

		public void InvokeCommand(string name, string arg)
		{
			if (!commandHandlers.TryGetValue(name, out var command))
				return;

			if (devMode == null || !devMode.Enabled)
			{
				TextNotificationsManager.Debug(FluentProvider.GetMessage(CheatsDisabled));
				return;
			}

			var context = new HandlerContext(world, debugVis, arg);
			command.Handler(context);
		}

		sealed record class HandlerContext(World World, DebugVisualizations DebugVis, string Argument)
		{
			public void SendCheatNotification(bool enabled, string cheatName)
			{
				var notification = enabled ? CheatEnabled : CheatDisabled;
				var playerName = World.LocalPlayer != null ? World.LocalPlayer.ResolvedPlayerName : "";
				TextNotificationsManager.Debug(FluentProvider.GetMessage(notification,
					"cheat", cheatName,
					"player", playerName));
			}
		}
	}
}
