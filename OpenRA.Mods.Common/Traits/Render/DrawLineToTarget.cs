#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Renders target lines between order waypoints.")]
	public class DrawLineToTargetInfo : TraitInfo
	{
		[Desc("Delay (in milliseconds) before the target lines disappear.")]
		public readonly int Delay = 2400;

		[Desc("Width (in pixels) of the target lines.")]
		public readonly int LineWidth = 1;

		[Desc("Width (in pixels) of the queued target lines.")]
		public readonly int QueuedLineWidth = 1;

		[Desc("Width (in pixels) of the end node markers.")]
		public readonly int MarkerWidth = 2;

		[Desc("Width (in pixels) of the queued end node markers.")]
		public readonly int QueuedMarkerWidth = 2;

		public override object Create(ActorInitializer init) { return new DrawLineToTarget(init.Self, this); }
	}

	public class DrawLineToTarget : IRenderAboveShroud, IRenderAnnotationsWhenSelected, INotifySelected
	{
		readonly DrawLineToTargetInfo info;
		readonly List<IRenderable> renderableCache = new List<IRenderable>();
		long lifetime;

		public DrawLineToTarget(Actor self, DrawLineToTargetInfo info)
		{
			this.info = info;
		}

		public void ShowTargetLines(Actor self)
		{
			if (Game.Settings.Game.TargetLines < TargetLinesType.Automatic || self.IsIdle)
				return;

			// Reset the order line timeout.
			lifetime = Game.RunTime + info.Delay;
		}

		void INotifySelected.Selected(Actor self)
		{
			ShowTargetLines(self);
		}

		bool ShouldRender(Actor self)
		{
			if (!self.Owner.IsAlliedWith(self.World.LocalPlayer) || Game.Settings.Game.TargetLines == TargetLinesType.Disabled)
				return false;

			// Players want to see the lines when in waypoint mode.
			var force = Game.GetModifierKeys().HasModifier(Modifiers.Shift) || self.World.OrderGenerator is ForceModifiersOrderGenerator;

			return force || Game.RunTime <= lifetime;
		}

		IEnumerable<IRenderable> IRenderAboveShroud.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (!ShouldRender(self))
				return Enumerable.Empty<IRenderable>();

			return RenderAboveShroud(self, wr);
		}

		IEnumerable<IRenderable> RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			var pal = wr.Palette(TileSet.TerrainPaletteInternalName);
			var a = self.CurrentActivity;
			for (; a != null; a = a.NextActivity)
				if (!a.IsCanceling)
					foreach (var n in a.TargetLineNodes(self))
						if (n.Tile != null && n.Target.Type != TargetType.Invalid)
							yield return new SpriteRenderable(n.Tile, n.Target.CenterPosition, WVec.Zero, -511, pal, 1f, true, TintModifiers.IgnoreWorldTint);
		}

		bool IRenderAboveShroud.SpatiallyPartitionable { get { return false; } }

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!ShouldRender(self))
				return Enumerable.Empty<IRenderable>();

			renderableCache.Clear();
			var prev = self.CenterPosition;
			var a = self.CurrentActivity;
			for (; a != null; a = a.NextActivity)
			{
				if (a.IsCanceling)
					continue;

				foreach (var n in a.TargetLineNodes(self))
				{
					if (n.Target.Type != TargetType.Invalid && n.Tile == null)
					{
						var lineWidth = renderableCache.Count > 0 ? info.QueuedLineWidth : info.LineWidth;
						var markerWidth = renderableCache.Count > 0 ? info.QueuedMarkerWidth : info.MarkerWidth;

						var pos = n.Target.CenterPosition;
						renderableCache.Add(new TargetLineRenderable(new[] { prev, pos }, n.Color, lineWidth, markerWidth));
						prev = pos;
					}
				}
			}

			if (renderableCache.Count == 0)
				return Enumerable.Empty<IRenderable>();

			// Reverse draw order so target markers are drawn on top of the next line
			renderableCache.Reverse();
			return renderableCache.ToArray();
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable { get { return false; } }
	}

	public static class LineTargetExts
	{
		public static void ShowTargetLines(this Actor self)
		{
			// Target lines are only automatically shown for the owning player
			// Spectators and allies must use the force-display modifier
			if (self.Owner != self.World.LocalPlayer)
				return;

			// Draw after frame end so that all the queueing of activities are done before drawing.
			var line = self.TraitOrDefault<DrawLineToTarget>();
			if (line != null)
				self.World.AddFrameEndTask(w => line.ShowTargetLines(self));
		}
	}
}
