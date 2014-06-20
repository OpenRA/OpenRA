#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OpenRA.Graphics
{
	[Flags]
	public enum ScrollDirection { None = 0, Up = 1, Left = 2, Down = 4, Right = 8 }

	public static class ViewportExts
	{
		public static bool Includes(this ScrollDirection d, ScrollDirection s)
		{
			return (d & s) == s;
		}

		public static ScrollDirection Set(this ScrollDirection d, ScrollDirection s, bool val)
		{
			return (d.Includes(s) != val) ? d ^ s : d;
		}
	}

	public class Viewport
	{
		readonly WorldRenderer worldRenderer;

		// Map bounds (world-px)
		readonly Rectangle mapBounds;

		// Viewport geometry (world-px)
		public int2 CenterLocation { get; private set; }

		public WPos CenterPosition { get { return worldRenderer.Position(CenterLocation); } }

		public int2 TopLeft { get { return CenterLocation - viewportSize / 2; } }
		public int2 BottomRight { get { return CenterLocation + viewportSize / 2; } }
		int2 viewportSize;
		CellRegion cells;
		bool cellsDirty = true;

		float zoom = 1f;
		public float Zoom
		{
			get
			{
				return zoom;
			}

			set
			{
				zoom = value;
				viewportSize = (1f / zoom * new float2(Game.Renderer.Resolution)).ToInt2();
				cellsDirty = true;
			}
		}

		public static int TicksSinceLastMove = 0;
		public static int2 LastMousePos;

		public ScrollDirection GetBlockedDirections()
		{
			var ret = ScrollDirection.None;
			if (CenterLocation.Y <= mapBounds.Top)
				ret |= ScrollDirection.Up;
			if (CenterLocation.X <= mapBounds.Left)
				ret |= ScrollDirection.Left;
			if (CenterLocation.Y >= mapBounds.Bottom)
				ret |= ScrollDirection.Down;
			if (CenterLocation.X >= mapBounds.Right)
				ret |= ScrollDirection.Right;

			return ret;
		}

		public Viewport(WorldRenderer wr, Map map)
		{
			worldRenderer = wr;

			// Calculate map bounds in world-px
			var b = map.Bounds;
			var tl = wr.ScreenPxPosition(new CPos(b.Left, b.Top).TopLeft);
			var br = wr.ScreenPxPosition(new CPos(b.Right, b.Bottom).BottomRight);
			mapBounds = Rectangle.FromLTRB(tl.X, tl.Y, br.X, br.Y);

			CenterLocation = (tl + br) / 2;
			Zoom = Game.Settings.Graphics.PixelDouble ? 2 : 1;
		}

		public int2 ViewToWorldPx(int2 view) { return (1f / Zoom * view.ToFloat2()).ToInt2() + TopLeft; }
		public int2 WorldToViewPx(int2 world) { return (Zoom * (world - TopLeft).ToFloat2()).ToInt2(); }

		public void Center(IEnumerable<Actor> actors)
		{
			if (!actors.Any())
				return;

			Center(actors.Select(a => a.CenterPosition).Average());
		}

		public void Center(WPos pos)
		{
			CenterLocation = worldRenderer.ScreenPxPosition(pos).Clamp(mapBounds);
			cellsDirty = true;
		}

		public void Scroll(float2 delta, bool ignoreBorders)
		{
			// Convert scroll delta from world-px to viewport-px
			CenterLocation += (1f / Zoom * delta).ToInt2();
			cellsDirty = true;

			if (!ignoreBorders)
				CenterLocation = CenterLocation.Clamp(mapBounds);
		}

		// Rectangle (in viewport coords) that contains things to be drawn
		static readonly Rectangle ScreenClip = Rectangle.FromLTRB(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height);
		public Rectangle ScissorBounds
		{
			get
			{
				var ctl = VisibleCells.TopLeft.TopLeft;
				var cbr = VisibleCells.BottomRight.BottomRight;
				var tl = WorldToViewPx(worldRenderer.ScreenPxPosition(ctl)).Clamp(ScreenClip);
				var br = WorldToViewPx(worldRenderer.ScreenPxPosition(cbr)).Clamp(ScreenClip);
				return Rectangle.FromLTRB(tl.X, tl.Y, br.X, br.Y);
			}
		}

		public CellRegion VisibleCells
		{
			get
			{
				if (cellsDirty)
				{
					// Calculate the intersection of the visible rectangle and the map.
					var map = worldRenderer.world.Map;
					var tl = map.Clamp(worldRenderer.Position(TopLeft).ToCPos() - new CVec(1, 1));
					var br = map.Clamp(worldRenderer.Position(BottomRight).ToCPos());

					cells = new CellRegion(tl, br);
					cellsDirty = false;
				}

				return cells;
			}
		}
	}
}