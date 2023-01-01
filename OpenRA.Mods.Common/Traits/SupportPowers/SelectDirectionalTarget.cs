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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Orders;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Traits
{
	public class SelectDirectionalTarget : IOrderGenerator
	{
		const int MinDragThreshold = 20;
		const int MaxDragThreshold = 75;

		readonly string order;
		readonly SupportPowerManager manager;
		readonly string cursor;
		readonly string directionArrowPalette;

		readonly string[] arrows = { "arrow-t", "arrow-tl", "arrow-l", "arrow-bl", "arrow-b", "arrow-br", "arrow-r", "arrow-tr" };
		readonly Arrow[] directionArrows;

		CPos targetCell;
		int2 targetLocation;
		float2 dragDirection;
		bool activated;
		bool dragStarted;
		Arrow currentArrow;
		readonly MouseAttachmentWidget mouseAttachment;

		public SelectDirectionalTarget(World world, string order, SupportPowerManager manager, string cursor,
			string directionArrowAnimation, string directionArrowPalette)
		{
			this.order = order;
			this.manager = manager;
			this.cursor = cursor;

			this.directionArrowPalette = directionArrowPalette;

			directionArrows = LoadArrows(directionArrowAnimation, world, arrows.Length);
			mouseAttachment = Ui.Root.Get<MouseAttachmentWidget>("MOUSE_ATTATCHMENT");
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
				if (!activated && world.Map.Contains(cell))
				{
					targetCell = cell;
					targetLocation = mi.Location;
					activated = true;
					Game.Cursor.Lock();
				}

				yield break;
			}

			if (!activated)
				yield break;

			if (mi.Event == MouseInputEvent.Move)
			{
				dragDirection += mi.Delta;

				var angle = AngleOf(dragDirection);
				if (dragDirection.Length > MaxDragThreshold)
					dragDirection = -MaxDragThreshold * float2.FromAngle((float)(angle * (Math.PI / 180)));

				currentArrow = GetArrow(angle);

				mouseAttachment.SetAttachment(targetLocation, currentArrow.Sprite, directionArrowPalette);
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
			// Cancel the OG if we can't use the power
			if (!manager.Powers.TryGetValue(order, out var p) || !p.Active || !p.Ready)
				world.CancelInputMode();
		}

		void IOrderGenerator.SelectionChanged(World world, IEnumerable<Actor> selected) { }

		bool IsOutsideDragZone => dragStarted && dragDirection.Length > MinDragThreshold;

		IEnumerable<IRenderable> IOrderGenerator.Render(WorldRenderer wr, World world) { yield break; }

		IEnumerable<IRenderable> IOrderGenerator.RenderAboveShroud(WorldRenderer wr, World world) { yield break; }
		IEnumerable<IRenderable> IOrderGenerator.RenderAnnotations(WorldRenderer wr, World world) { yield break; }

		string IOrderGenerator.GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return world.Map.Contains(cell) ? cursor : "generic-blocked";
		}

		bool IOrderGenerator.HandleKeyPress(KeyInput e) { return false; }

		void IOrderGenerator.Deactivate()
		{
			if (activated)
			{
				mouseAttachment.Reset();
				Game.Cursor.Unlock();
			}
		}

		// Starting at (0, -1) and rotating in CCW
		static double AngleOf(float2 delta)
		{
			var radian = Math.Atan2(delta.Y, delta.X);
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
			public Sprite Sprite { get; }
			public double EndAngle { get; }
			public WAngle Direction { get; }

			public Arrow(Sprite sprite, double endAngle, WAngle direction)
			{
				Sprite = sprite;
				EndAngle = endAngle;
				Direction = direction;
			}
		}
	}
}
