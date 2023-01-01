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
	[Desc("Renders a debug overlay showing the abstract graph of the hierarchical pathfinder. Attach this to the world actor.")]
	public class HierarchicalPathFinderOverlayInfo : TraitInfo, Requires<PathFinderInfo>
	{
		public readonly string Font = "TinyBold";
		public readonly Color GroundLayerColor = Color.DarkOrange;
		public readonly Color CustomLayerColor = Color.Blue;
		public readonly Color GroundToCustomLayerColor = Color.Purple;
		public readonly Color AbstractNodeColor = Color.Red;

		public override object Create(ActorInitializer init) { return new HierarchicalPathFinderOverlay(this); }
	}

	public class HierarchicalPathFinderOverlay : IRenderAnnotations, IWorldLoaded, IChatCommand
	{
		const string CommandName = "hpf";

		[TranslationReference]
		const string CommandDescription = "description-hpf-debug-overlay";

		readonly HierarchicalPathFinderOverlayInfo info;
		readonly SpriteFont font;

		public bool Enabled { get; private set; }

		/// <summary>
		/// The Locomotor selected in the UI which the overlay will display.
		/// If null, will show the overlays for the currently selected units.
		/// </summary>
		public Locomotor Locomotor { get; set; }

		/// <summary>
		/// The blocking check selected in the UI which the overlay will display.
		/// </summary>
		public BlockedByActor Check { get; set; } = BlockedByActor.Immovable;

		public HierarchicalPathFinderOverlay(HierarchicalPathFinderOverlayInfo info)
		{
			this.info = info;
			font = Game.Renderer.Fonts[info.Font];
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			var console = w.WorldActor.TraitOrDefault<ChatCommands>();
			var help = w.WorldActor.TraitOrDefault<HelpCommand>();

			if (console == null || help == null)
				return;

			console.RegisterCommand(CommandName, this);
			help.RegisterHelp(CommandName, CommandDescription);
		}

		void IChatCommand.InvokeCommand(string name, string arg)
		{
			if (name == CommandName)
				Enabled ^= true;
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!Enabled)
				yield break;

			var pathFinder = self.Trait<PathFinder>();
			var visibleRegion = wr.Viewport.AllVisibleCells;
			var locomotors = Locomotor == null
				? self.World.Selection.Actors
					.Where(a => !a.Disposed)
					.Select(a => a.TraitOrDefault<Mobile>()?.Locomotor)
					.Where(l => l != null)
					.Distinct()
				: new[] { Locomotor };
			foreach (var locomotor in locomotors)
			{
				var (abstractGraph, abstractDomains) = pathFinder.GetOverlayDataForLocomotor(locomotor, Check);

				// Locomotor doesn't allow movement, nothing to display.
				if (abstractGraph == null || abstractDomains == null)
					continue;

				foreach (var connectionsFromOneNode in abstractGraph)
				{
					var nodeCell = connectionsFromOneNode.Key;
					var srcUv = (PPos)nodeCell.ToMPos(self.World.Map);
					foreach (var cost in connectionsFromOneNode.Value)
					{
						var destUv = (PPos)cost.Destination.ToMPos(self.World.Map);
						if (!visibleRegion.Contains(destUv) && !visibleRegion.Contains(srcUv))
							continue;

						var connection = new WPos[]
						{
							self.World.Map.CenterOfSubCell(cost.Destination, SubCell.FullCell),
							self.World.Map.CenterOfSubCell(nodeCell, SubCell.FullCell),
						};

						// Connections on the ground layer given in ground color.
						// Connections on any custom layers given in custom color.
						// Connections that allow a transition between layers given in transition color.
						Color lineColor;
						if (nodeCell.Layer == 0 && cost.Destination.Layer == 0)
							lineColor = info.GroundLayerColor;
						else if (nodeCell.Layer == cost.Destination.Layer)
							lineColor = info.CustomLayerColor;
						else
							lineColor = info.GroundToCustomLayerColor;
						yield return new TargetLineRenderable(connection, lineColor, 1, 2);

						var centerCell = new CPos(
							(cost.Destination.X + nodeCell.X) / 2,
							(cost.Destination.Y + nodeCell.Y) / 2);
						var centerPos = self.World.Map.CenterOfSubCell(centerCell, SubCell.FullCell);
						yield return new TextAnnotationRenderable(font, centerPos, 0, lineColor, cost.Cost.ToString());
					}
				}

				foreach (var domainForCell in abstractDomains)
				{
					var nodeCell = domainForCell.Key;
					var srcUv = (PPos)nodeCell.ToMPos(self.World.Map);
					if (!visibleRegion.Contains(srcUv))
						continue;

					// Show the abstract cell and its domain index.
					var nodePos = self.World.Map.CenterOfSubCell(nodeCell, SubCell.FullCell);
					yield return new TextAnnotationRenderable(
						font, nodePos, 0, info.AbstractNodeColor, $"{domainForCell.Value}: {nodeCell}");
				}
			}
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
	}
}
