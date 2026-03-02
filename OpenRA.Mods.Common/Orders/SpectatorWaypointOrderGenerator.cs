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
using OpenRA.Orders;

namespace OpenRA.Mods.Common.Orders
{
	// Spectator-only order generator: lets spectators draw a waypoint path on the map.
	// The path is broadcast over the network so all spectators see it.
	public class SpectatorWaypointOrderGenerator : IOrderGenerator
	{
		const int LineWidth = 2;
		const int MarkerSize = 3;

		readonly World world;
		readonly List<WPos> waypoints = [];
		WPos cursorPos;

		public SpectatorWaypointOrderGenerator(World world)
		{
			this.world = world;
		}

		public MouseButton ActionButton => MouseButton.Left;

		IEnumerable<Order> IOrderGenerator.Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			cursorPos = world.Map.CenterOfCell(cell);

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				waypoints.Add(cursorPos);
				return [];
			}

			if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Up)
				world.CancelInputMode();

			return [];
		}

		void IOrderGenerator.Tick(World world) { }

		IEnumerable<IRenderable> IOrderGenerator.Render(WorldRenderer wr, World world)
		{
			return SpriteRenderable.None;
		}

		IEnumerable<IRenderable> IOrderGenerator.RenderAboveShroud(WorldRenderer wr, World world)
		{
			return SpriteRenderable.None;
		}

		IEnumerable<IRenderable> IOrderGenerator.RenderAnnotations(WorldRenderer wr, World world)
		{
			if (waypoints.Count == 0)
				yield break;

			// Draw accumulated waypoints plus a live preview segment to the cursor.
			var points = new List<WPos>(waypoints) { cursorPos };
			yield return new TargetLineRenderable(points, Game.Settings.Player.Color, LineWidth, MarkerSize);
		}

		string IOrderGenerator.GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return "ability";
		}

		void IOrderGenerator.Deactivate()
		{
			if (waypoints.Count < 2)
				return;

			// Broadcast the waypoint path so all spectators receive and display it.
			// Prefix with the preferred color so each spectator's path is distinguishable.
			var colorPrefix = Game.Settings.Player.Color.ToString();
			world.IssueOrder(Order.FromTargetString("SpectatorWaypoint", colorPrefix + "|" + string.Join("|", waypoints.Select(p => p.ToString())), false));
		}

		bool IOrderGenerator.HandleKeyPress(KeyInput e) { return false; }

		void IOrderGenerator.SelectionChanged(World world, IEnumerable<Actor> selected) { }
	}
}
