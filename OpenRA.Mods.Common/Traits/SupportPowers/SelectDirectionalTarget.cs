#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class SelectDirectionalTarget : IOrderGenerator
	{
		readonly string order;
		readonly SupportPowerManager manager;
		readonly string cursor;
		readonly string targetPlaceholderCursorPalette;
		readonly string directionArrowPalette;
		readonly Animation targetCursor;

		readonly string[] arrows = { "arrow-t", "arrow-tl", "arrow-l", "arrow-bl", "arrow-b", "arrow-br", "arrow-r", "arrow-tr" };
		readonly Arrow[] directionArrows;

		CPos targetCell;
		int2 targetLocation;
		int2 dragLocation;
		bool activated;
		bool dragStarted;
		bool hideMouse = true;
		Arrow currentArrow;

		public SelectDirectionalTarget(World world, string order, SupportPowerManager manager, string cursor, string targetPlaceholderCursorAnimation,
			string directionArrowAnimation, string targetPlaceholderCursorPalette, string directionArrowPalette)
		{
			this.order = order;
			this.manager = manager;
			this.cursor = cursor;
			this.targetPlaceholderCursorPalette = targetPlaceholderCursorPalette;
			this.directionArrowPalette = directionArrowPalette;

			targetCursor = new Animation(world, targetPlaceholderCursorAnimation);
			targetCursor.PlayRepeating("cursor");

			for (var i = 0; i < Game.Cursor.Frame; i++)
				targetCursor.Tick();

			directionArrows = LoadArrows(directionArrowAnimation, world, arrows.Length);
		}

		IEnumerable<Order> IOrderGenerator.Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
			{
				world.CancelInputMode();
				yield break;
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				if (!activated)
				{
					targetCell = cell;
					targetLocation = mi.Location;
					activated = true;
				}

				yield break;
			}

			if (!activated)
				yield break;

			if (mi.Event == MouseInputEvent.Move)
			{
				dragLocation = mi.Location;

				var angle = AngleBetween(targetLocation, dragLocation);
				currentArrow = GetArrow(angle);
				dragStarted = true;
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				yield return new Order(order, manager.Self, Target.FromCell(manager.Self.World, targetCell), false)
				{
					SuppressVisualFeedback = true,
					ExtraData = IsOutsideDragZone ? (uint)currentArrow.Direction.Facing : uint.MaxValue
				};

				world.CancelInputMode();
			}
		}

		void IOrderGenerator.Tick(World world)
		{
			targetCursor.Tick();

			// Cancel the OG if we can't use the power
			if (!manager.Powers.ContainsKey(order))
				world.CancelInputMode();
		}

		bool IsOutsideDragZone
		{
			get { return dragStarted && (dragLocation - targetLocation).Length > 20; }
		}

		IEnumerable<IRenderable> IOrderGenerator.Render(WorldRenderer wr, World world) { yield break; }

		IEnumerable<IRenderable> IOrderGenerator.RenderAboveShroud(WorldRenderer wr, World world)
		{
			if (!activated)
				return Enumerable.Empty<IRenderable>();

			var targetPalette = wr.Palette(targetPlaceholderCursorPalette);

			var location = activated ? targetLocation : Viewport.LastMousePos;
			var worldPx = wr.Viewport.ViewToWorldPx(location);
			var worldPos = wr.ProjectedPosition(worldPx);
			var renderables = new List<IRenderable>(targetCursor.Render(worldPos, WVec.Zero, -511, targetPalette, 1 / wr.Viewport.Zoom));

			if (IsOutsideDragZone)
			{
				var directionPalette = wr.Palette(directionArrowPalette);
				renderables.Add(new SpriteRenderable(currentArrow.Sprite, worldPos, WVec.Zero, -511, directionPalette, 1 / wr.Viewport.Zoom, true));
			}

			if (hideMouse)
			{
				hideMouse = false;
				Game.RunAfterTick(() => Game.HideCursor = true);
			}

			return renderables;
		}

		string IOrderGenerator.GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi) { return cursor; }

		void IOrderGenerator.Deactivate()
		{
			if (activated)
				Game.HideCursor = false;
		}

		// Starting at (0, -1) and rotating in CCW
		static double AngleBetween(int2 p1, int2 p2)
		{
			var radian = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
			var d = radian * (180 / Math.PI);
			if (d < 0.0)
				d += 360.0;
			var angle = 270.0 - d;
			if (angle < 0)
				angle += 360.0;

			return angle;
		}

		Arrow GetArrow(double degree)
		{
			var arrow = directionArrows.FirstOrDefault(d => d.EndAngle >= degree);
			return arrow ?? directionArrows[0];
		}

		Arrow[] LoadArrows(string cursorAnimation, World world, int noOfDividingPoints)
		{
			var points = new Arrow[noOfDividingPoints];
			var partAngle = 360 / noOfDividingPoints;
			var i1 = partAngle / 2d;

			for (var i = 0; i < noOfDividingPoints; i++)
			{
				var sprite = world.Map.Rules.Sequences.GetSequence(cursorAnimation, arrows[i]).GetSprite(0);

				var angle = i * partAngle;
				var direction = WAngle.FromDegrees(angle);
				var endAngle = angle + i1;

				points[i] = new Arrow(sprite, endAngle, direction);
			}

			return points;
		}

		class Arrow
		{
			public Sprite Sprite { get; private set; }
			public double EndAngle { get; private set; }
			public WAngle Direction { get; private set; }

			public Arrow(Sprite sprite, double endAngle, WAngle direction)
			{
				Sprite = sprite;
				EndAngle = endAngle;
				Direction = direction;
			}
		}
	}
}
