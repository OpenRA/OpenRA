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
using OpenRA.Mods.Common.Commands;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[IncludeStaticFluentReferences(typeof(TerrainGeometryOverlay))]
	[Desc("Renders a debug overlay showing the terrain cells. Attach this to the world actor.")]
	public class TerrainGeometryOverlayInfo : TraitInfo<TerrainGeometryOverlay> { }

	public class TerrainGeometryOverlay : IRenderAnnotations, IWorldLoaded, IChatCommand
	{
		public const string CommandName = "terrain-geometry";
		public const string OrderName = "DevTerrainGeometry";

		[FluentReference]
		const string CheatsDisabled = "notification-cheats-disabled";

		[FluentReference]
		const string CommandDescription = "description-terrain-geometry-overlay";

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

		public void InvokeCommand(string name, string arg)
		{
			if (name != CommandName)
				return;

			if (devMode == null || !devMode.Enabled)
			{
				TextNotificationsManager.Debug(FluentProvider.GetMessage(CheatsDisabled));
				return;
			}

			Enabled ^= true;

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
			var colors = wr.World.Map.Rules.TerrainInfo.HeightDebugColors;
			var lastColor = colors.Length - 1;
			var heightStep = map.Grid.TileScale / 2;
			var mouseCell = wr.Viewport.ViewToWorld(Viewport.LastMousePos).ToMPos(wr.World.Map);

			foreach (var uv in wr.Viewport.AllVisibleCells.CandidateMapCoords)
			{
				if (!map.Height.Contains(uv) || self.World.ShroudObscures(uv))
					continue;

				var height = (int)map.Height[uv];
				var r = map.Grid.Ramps[map.Ramp[uv]];
				var pos = map.CenterOfCell(uv.ToCPos(map)) - new WVec(0, 0, r.CenterHeightOffset);
				var width = uv == mouseCell ? 3 : 1;

				// Colors change between points, so render separately
				foreach (var p in r.Polygons)
				{
					for (var i = 0; i < p.Length; i++)
					{
						var j = (i + 1) % p.Length;
						var start = pos + p[i];
						var end = pos + p[j];
						var startColor = colors[Math.Min(lastColor, height + p[i].Z / heightStep)];
						var endColor = colors[Math.Min(lastColor, height + p[j].Z / heightStep)];
						yield return new LineAnnotationRenderable(start, end, width, startColor, endColor);
					}
				}
			}

			// Projected cell coordinates for the current cell
			var projectedCorners = map.Grid.Ramps[0].Corners;
			foreach (var puv in map.ProjectedCellsCovering(mouseCell))
			{
				var pos = map.CenterOfCell(((MPos)puv).ToCPos(map));
				for (var i = 0; i < 4; i++)
				{
					var j = (i + 1) % 4;
					var start = pos + projectedCorners[i] - new WVec(0, 0, pos.Z);
					var end = pos + projectedCorners[j] - new WVec(0, 0, pos.Z);
					yield return new LineAnnotationRenderable(start, end, 3, Color.Navy);
				}
			}
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
	}
}
