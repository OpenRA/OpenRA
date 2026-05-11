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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Commands;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[IncludeStaticFluentReferences(typeof(ActorMapOverlay))]
	[Desc("Renders a debug overlay showing the actor influence map. Attach this to the world actor.")]
	public class ActorMapOverlayInfo : TraitInfo<ActorMapOverlay>, Requires<ActorMapInfo> { }

	public class ActorMapOverlay : IRenderAnnotations, IWorldLoaded, IChatCommand
	{
		public const string CommandName = "actor-map";
		public const string OrderName = "DevActorMap";

		[FluentReference]
		const string CheatsDisabled = "notification-cheats-disabled";

		[FluentReference]
		const string CommandDescription = "description-actor-map-overlay";

		[FluentReference("cheat", "player")]
		const string CheatEnabled = "notification-cheat-enabled";

		[FluentReference("cheat", "player")]
		const string CheatDisabled = "notification-cheat-disabled";

		public bool Enabled;

		DeveloperMode devMode;
		World world;

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

			Enabled = !Enabled;

			var notification = Enabled ? CheatEnabled : CheatDisabled;
			var playerName = world.LocalPlayer != null ? world.LocalPlayer.ResolvedPlayerName : "";
			TextNotificationsManager.Debug(FluentProvider.GetMessage(notification,
				"cheat", OrderName,
				"player", playerName));
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!Enabled)
				yield break;

			var map = wr.World.Map;

			// Only get actors in actor map.
			// Cull to visible cells while we are at it.
			var actorsInBox = new HashSet<Actor>(100);
			foreach (var mpos in wr.Viewport.AllVisibleCells.CandidateMapCoords)
				actorsInBox.UnionWith(wr.World.ActorMap.GetActorsAt(mpos.ToCPos(map)));

			foreach (var actor in actorsInBox)
			{
				if (actor.OccupiesSpace is not IOccupySpace space)
					continue;

				foreach (var (cell, subCell) in space.OccupiedCells())
				{
					var pos = map.CenterOfSubCell(cell, subCell);
					var fullCell = subCell == SubCell.FullCell;

					Color color;
					color = fullCell ? Color.Red : wr.World.ActorMap.HasFreeSubCell(cell, false) ? Color.Yellow : Color.Orange;

					var ramp = map.Ramp[cell];
					var corners = map.Grid.Ramps[ramp].Corners;
					for (var i = 0; i < 4; i++)
					{
						var j = (i + 1) % 4;
						var start = pos + corners[i] / (fullCell ? 1 : 2);
						var end = pos + corners[j] / (fullCell ? 1 : 2);
						yield return new LineAnnotationRenderable(start, end, 2, color);
					}
				}
			}
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
	}
}
