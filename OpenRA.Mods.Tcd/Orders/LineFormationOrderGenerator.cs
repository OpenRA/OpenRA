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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Tcd.Formations;
using OpenRA.Primitives;

namespace OpenRA.Mods.Tcd.Orders
{
	// Press and drag to draw a straight line; the selected units space themselves
	// along it on release.
	//
	// This deliberately derives from OrderGenerator rather than UnitOrderGenerator.
	// WorldInteractionControllerWidget forwards every mouse event to a generator that
	// is not a UnitOrderGenerator, but only right-button releases to one that is - so
	// a subclass of UnitOrderGenerator can never see a drag. See docs/ENGINE-NOTES.md.
	public sealed class LineFormationOrderGenerator : OrderGenerator
	{
		protected override MouseActionType ActionType => MouseActionType.ConfirmOrder;

		readonly bool oneShot;
		WPos? anchor;

		// oneShot is how the button behaves: draw one line and put the tool away.
		// Holding the key instead keeps it armed until the key comes back up.
		public LineFormationOrderGenerator(World world, bool oneShot)
			: base(world)
		{
			this.oneShot = oneShot;
		}

		public override IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == CancelButton && mi.Event == MouseInputEvent.Down)
			{
				world.CancelInputMode();
				return [];
			}

			if (mi.Button != ActionButton)
				return [];

			if (mi.Event == MouseInputEvent.Down)
				anchor = world.Map.CenterOfCell(cell);
			else if (mi.Event == MouseInputEvent.Up && anchor != null)
			{
				var from = anchor.Value;
				anchor = null;
				Place(world, from, world.Map.CenterOfCell(cell));

				if (oneShot)
					world.CancelInputMode();
			}

			return [];
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			// Handled in Order, which sees the press as well as the release.
			return [];
		}

		static List<Actor> Members(World world)
		{
			return world.Selection.Actors
				.Where(a => a.Owner == world.LocalPlayer && a.IsInWorld && !a.IsDead && a.TraitOrDefault<Mobile>() != null)
				.ToList();
		}

		static void Place(World world, WPos from, WPos to)
		{
			var members = Members(world);
			if (members.Count == 0)
				return;

			var slots = FormationPath.Distribute([from, to], members.Count, closed: false);

			// Walk the line in order so units take the nearest slot rather than crossing.
			var direction = to - from;
			var ordered = members
				.OrderBy(a => Along(a.CenterPosition - from, direction))
				.ToList();

			long x = 0;
			long y = 0;
			foreach (var s in slots)
			{
				x += s.X;
				y += s.Y;
			}

			var midpoint = new WPos((int)(x / slots.Length), (int)(y / slots.Length), 0);
			var offsets = slots.Select(s => s - midpoint).ToArray();

			var placed = FormationPlanner.IssueMoves(world, ordered, offsets, midpoint, WRot.FromYaw(WAngle.Zero));
			if (placed > 0)
				TextNotificationsManager.Debug($"Line formation: {placed} units.");
		}

		static long Along(WVec v, WVec direction)
		{
			return (long)v.X * direction.X + (long)v.Y * direction.Y;
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { return []; }

		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { return []; }

		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world)
		{
			if (anchor == null)
				yield break;

			var count = Members(world).Count;
			if (count == 0)
				yield break;

			var cursor = world.Map.CenterOfCell(wr.Viewport.ViewToWorld(Viewport.LastMousePos));
			yield return new LineAnnotationRenderable(anchor.Value, cursor, 1, Color.White);

			foreach (var slot in FormationPath.Distribute([anchor.Value, cursor], count, closed: false))
				yield return new CircleAnnotationRenderable(slot, new WDist(160), 1, Color.White);
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return "move";
		}
	}
}
