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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Commands;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Renders a visualization overlay showing how the pathfinder searches for paths. Attach this to the world actor.")]
	public class PathFinderOverlayInfo : TraitInfo, Requires<PathFinderInfo>
	{
		public readonly string Font = "TinyBold";
		public readonly Color TargetLineColor = Color.Red;
		public readonly Color AbstractColor1 = Color.Lime;
		public readonly Color AbstractColor2 = Color.PaleGreen;
		public readonly Color LocalColor1 = Color.Yellow;
		public readonly Color LocalColor2 = Color.LightYellow;
		public readonly bool ShowCosts = true;

		public override object Create(ActorInitializer init) { return new PathFinderOverlay(this); }
	}

	public class PathFinderOverlay : IRenderAnnotations, IWorldLoaded, IChatCommand
	{
		const string CommandName = "path-debug";

		[TranslationReference]
		const string CommandDescription = "description-path-debug-overlay";

		sealed class Record : PathSearch.IRecorder, IEnumerable<(CPos Source, CPos Destination, int CostSoFar, int EstimatedRemainingCost)>
		{
			readonly Dictionary<CPos, (CPos Source, int CostSoFar, int EstimatedRemainingCost)> edges
				= new Dictionary<CPos, (CPos Source, int CostSoFar, int EstimatedRemainingCost)>();

			public void Add(CPos source, CPos destination, int costSoFar, int estimatedRemainingCost)
			{
				edges[destination] = (source, costSoFar, estimatedRemainingCost);
			}

			public IEnumerator<(CPos Source, CPos Destination, int CostSoFar, int EstimatedRemainingCost)> GetEnumerator()
			{
				return edges
					.Select(kvp => (kvp.Value.Source, kvp.Key, kvp.Value.CostSoFar, kvp.Value.EstimatedRemainingCost))
					.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}

		readonly PathFinderOverlayInfo info;
		readonly SpriteFont font;
		public bool Enabled { get; private set; }

		Actor forActor;
		bool record;
		CPos[] sourceCells;
		CPos? targetCell;

		Record abstractEdges1;
		Record abstractEdges2;
		Record localEdges1;
		Record localEdges2;

		public PathFinderOverlay(PathFinderOverlayInfo info)
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

		public void NewRecording(Actor actor, IEnumerable<CPos> sources, CPos? target)
		{
			if (!Enabled)
			{
				forActor = null;
				record = false;
				return;
			}

			if (!actor.World.Selection.Contains(actor))
			{
				record = false;
				return;
			}

			abstractEdges1 = null;
			abstractEdges2 = null;
			localEdges1 = null;
			localEdges2 = null;
			sourceCells = sources.ToArray();
			targetCell = target;
			forActor = actor;
			record = true;
		}

		public PathSearch.IRecorder RecordAbstractEdges(Actor actor)
		{
			if (!record || forActor != actor)
				return null;
			if (abstractEdges1 == null)
				return abstractEdges1 = new Record();
			if (abstractEdges2 == null)
				return abstractEdges2 = new Record();
			throw new InvalidOperationException("Maximum two records permitted.");
		}

		public PathSearch.IRecorder RecordLocalEdges(Actor actor)
		{
			if (!record || forActor != actor)
				return null;
			if (localEdges1 == null)
				return localEdges1 = new Record();
			if (localEdges2 == null)
				return localEdges2 = new Record();
			throw new InvalidOperationException("Maximum two records permitted.");
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!Enabled || forActor == null)
				yield break;

			foreach (var sourceCell in sourceCells)
				yield return new TargetLineRenderable(new[]
				{
					self.World.Map.CenterOfSubCell(sourceCell, SubCell.FullCell),
					self.World.Map.CenterOfSubCell(targetCell ?? sourceCell, SubCell.FullCell),
				}, info.TargetLineColor, 8, 8);

			foreach (var line in RenderEdges(self, abstractEdges1, 8, 6, info.AbstractColor1))
				yield return line;

			foreach (var line in RenderEdges(self, abstractEdges2, 6, 4, info.AbstractColor2))
				yield return line;

			foreach (var line in RenderEdges(self, localEdges1, 5, 3, info.LocalColor1))
				yield return line;

			foreach (var line in RenderEdges(self, localEdges2, 4, 2, info.LocalColor2))
				yield return line;

			if (!info.ShowCosts)
				yield break;

			const int HorizontalOffset = 320;
			const int VerticalOffset = 512;

			foreach (var line in RenderCosts(self, abstractEdges1, new WVec(-HorizontalOffset, -VerticalOffset, 0), font, info.AbstractColor1))
				yield return line;

			foreach (var line in RenderCosts(self, abstractEdges2, new WVec(-HorizontalOffset, VerticalOffset, 0), font, info.AbstractColor2))
				yield return line;

			foreach (var line in RenderCosts(self, localEdges1, new WVec(HorizontalOffset, -VerticalOffset, 0), font, info.LocalColor1))
				yield return line;

			foreach (var line in RenderCosts(self, localEdges2, new WVec(HorizontalOffset, VerticalOffset, 0), font, info.LocalColor2))
				yield return line;
		}

		static IEnumerable<IRenderable> RenderEdges(Actor self, Record edges, int nodeSize, int edgeSize, Color color)
		{
			if (edges == null)
				yield break;

			var customColor = CustomLayerColor(color);
			foreach (var (source, destination, _, _) in edges)
				yield return new TargetLineRenderable(new[]
				{
					self.World.Map.CenterOfSubCell(source, SubCell.FullCell) + CustomLayerOffset(source),
					self.World.Map.CenterOfSubCell(destination, SubCell.FullCell) + CustomLayerOffset(destination),
				}, destination.Layer == 0 ? color : customColor, edgeSize, nodeSize);
		}

		static IEnumerable<IRenderable> RenderCosts(Actor self, Record edges, WVec textOffset, SpriteFont font, Color color)
		{
			if (edges == null)
				yield break;

			var customColor = CustomLayerColor(color);
			foreach (var (_, destination, costSoFar, estimatedRemainingCost) in edges)
			{
				var centerPos = self.World.Map.CenterOfSubCell(destination, SubCell.FullCell) +
					CustomLayerOffset(destination) + textOffset;
				yield return new TextAnnotationRenderable(font, centerPos, 0,
					destination.Layer == 0 ? color : customColor,
					$"{costSoFar}|{estimatedRemainingCost}|{costSoFar + estimatedRemainingCost}");
			}
		}

		static Color CustomLayerColor(Color original)
		{
			(var a, var h, var s, var v) = original.ToAhsv();
			return Color.FromAhsv(a, h, s, v * .7f);
		}

		static WVec CustomLayerOffset(CPos cell)
		{
			return cell.Layer == 0
				? WVec.Zero
				: new WVec(0, -352, 0);
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
	}
}
