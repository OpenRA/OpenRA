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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Tcd.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Tcd.Orders
{
	// The mod's default order generator. Behaves exactly like the engine's, except
	// while a formation-drawing key is held: then the order button draws a shape
	// instead of issuing orders. Set as DefaultOrderGenerator in mods/ra/mod.yaml.
	public class TcdUnitOrderGenerator : UnitOrderGenerator
	{
		FormationCapture capture;

		public TcdUnitOrderGenerator(World world)
			: base(world) { }

		FormationCapture Capture(World world)
		{
			if (capture != null)
				return capture;

			capture = world.WorldActor.TraitOrDefault<FormationCapture>();
			return capture;
		}

		static bool Drawing(FormationCapture state)
		{
			return state != null && state.Mode != FormationCaptureMode.None;
		}

		public override IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var state = Capture(world);
			if (!Drawing(state) || mi.Button != ActionButton)
				return base.Order(world, cell, worldPixel, mi);

			// WorldInteractionControllerWidget only routes the right button to the order
			// generator on Up - Down and Move never arrive here - so a click has to be
			// recorded on release.
			if (mi.Event == MouseInputEvent.Up)
				state.AddPoint(world.Map.CenterOfCell(cell));

			// Swallow the click so it never becomes a move order.
			return [];
		}

		public override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var state = Capture(world);
			if (!Drawing(state))
				return base.GetCursor(world, cell, worldPixel, mi);

			return "move";
		}

		public override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
		{
			foreach (var r in base.RenderAnnotations(wr, world))
				yield return r;

			var state = Capture(world);
			if (!Drawing(state))
				yield break;

			var points = state.Points;
			if (points.Count == 0)
				yield break;

			for (var i = 1; i < points.Count; i++)
				yield return new LineAnnotationRenderable(points[i - 1], points[i], 1, Color.White);

			if (state.Closed)
				yield return new LineAnnotationRenderable(points[^1], points[0], 1, Color.White);

			foreach (var p in points)
				yield return new CircleAnnotationRenderable(p, new WDist(160), 1, Color.White);

			// Rubber band showing where the next segment would go.
			var cursor = world.Map.CenterOfCell(wr.Viewport.ViewToWorld(Viewport.LastMousePos));
			yield return new LineAnnotationRenderable(points[^1], cursor, 1, Color.White);
		}
	}
}
