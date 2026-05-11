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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Commands;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[IncludeStaticFluentReferences(typeof(CellTriggerOverlay))]
	[Desc("Renders a debug overlay showing the script triggers. Attach this to the world actor.")]
	public class CellTriggerOverlayInfo : TraitInfo
	{
		public readonly string Font = "BigBold";

		public readonly Color Color = Color.Red;

		public override object Create(ActorInitializer init) { return new CellTriggerOverlay(this); }
	}

	public class CellTriggerOverlay : IRenderAnnotations, IWorldLoaded, IChatCommand
	{
		public const string CommandName = "triggers";
		public const string OrderName = "DevTriggers";

		[FluentReference]
		const string CheatsDisabled = "notification-cheats-disabled";

		[FluentReference]
		const string CommandDescription = "description-cell-triggers-overlay";

		[FluentReference("cheat", "player")]
		const string CheatEnabled = "notification-cheat-enabled";

		[FluentReference("cheat", "player")]
		const string CheatDisabled = "notification-cheat-disabled";

		bool enabled;

		readonly SpriteFont font;
		readonly Color color;

		DeveloperMode devMode;
		World world;

		public CellTriggerOverlay(CellTriggerOverlayInfo info)
		{
			font = Game.Renderer.Fonts[info.Font];
			color = info.Color;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			var console = w.WorldActor.TraitOrDefault<ChatCommands>();
			var help = w.WorldActor.TraitOrDefault<HelpCommand>();
			devMode = world.LocalPlayer?.PlayerActor.Trait<DeveloperMode>();

			if (console == null || help == null || devMode == null)
				return;

			console.RegisterCommand(CommandName, this);
			help.RegisterHelp(CommandName, CommandDescription);
		}

		void IChatCommand.InvokeCommand(string name, string arg)
		{
			if (name != CommandName)
				return;

			if (devMode == null || !devMode.Enabled)
			{
				TextNotificationsManager.Debug(FluentProvider.GetMessage(CheatsDisabled));
				return;
			}

			enabled ^= true;

			var notification = enabled ? CheatEnabled : CheatDisabled;
			var playerName = world.LocalPlayer != null ? world.LocalPlayer.ResolvedPlayerName : "";
			TextNotificationsManager.Debug(FluentProvider.GetMessage(notification,
				"cheat", OrderName,
				"player", playerName));
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!enabled)
				yield break;

			var triggerPositions = wr.World.ActorMap.TriggerPositions().ToHashSet();

			foreach (var uv in wr.Viewport.VisibleCellsInsideBounds.CandidateMapCoords)
			{
				if (self.World.ShroudObscures(uv))
					continue;

				var cell = uv.ToCPos(wr.World.Map);
				if (!triggerPositions.Contains(cell))
					continue;

				var center = wr.World.Map.CenterOfCell(cell);
				yield return new TextAnnotationRenderable(font, center, 1024, color, "T");
			}
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
	}
}
