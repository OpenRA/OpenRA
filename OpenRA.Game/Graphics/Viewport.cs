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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Widgets;
using OpenRA.Support;

namespace OpenRA.Graphics
{
	[Flags]
	public enum ScrollDirection { None = 0, Up = 1, Left = 2, Down = 4, Right = 8 }

	public class Viewport
	{
		readonly int2 screenSize;
		readonly Renderer renderer;
		readonly Rectangle mapBounds;
		Rectangle scrollLimits;
		int2 scrollPosition;

		// Top-left of the viewport, in world-px units
		public float2 Location { get { return scrollPosition; } }
		public float2 CenterLocation { get { return scrollPosition + 0.5f/Zoom*screenSize.ToFloat2(); } }

		public Rectangle WorldRect
		{
			get
			{
				return new Rectangle(scrollPosition.X / Game.CellSize,
									 scrollPosition.Y / Game.CellSize,
									 (int)(screenSize.X / Zoom / Game.CellSize),
									 (int)(screenSize.Y / Zoom / Game.CellSize));
			}
		}

		public int Width { get { return screenSize.X; } }
		public int Height { get { return screenSize.Y; } }

		float zoom = 1f;
		public float Zoom
		{
			get
			{
				return zoom;
			}
			set
			{
				var oldCenter = CenterLocation;
				zoom = value;

				// Update scroll limits
				var viewTL = (Game.CellSize*new float2(mapBounds.Left, mapBounds.Top)).ToInt2();
				var viewBR = (Game.CellSize*new float2(mapBounds.Right, mapBounds.Bottom)).ToInt2();
				var border = (.5f/Zoom * screenSize.ToFloat2()).ToInt2();
				scrollLimits = Rectangle.FromLTRB(viewTL.X - border.X,
											  viewTL.Y - border.Y,
											  viewBR.X - border.X,
											  viewBR.Y - border.Y);
				// Re-center viewport
				scrollPosition = NormalizeScrollPosition((oldCenter - 0.5f / Zoom * screenSize.ToFloat2()).ToInt2());
			}
		}

		float cursorFrame = 0f;

		public static int TicksSinceLastMove = 0;
		public static int2 LastMousePos;

		public void Scroll(float2 delta)
		{
			Scroll(delta, false);
		}

		public void Scroll(float2 delta, bool ignoreBorders)
		{
			// Convert from world-px to viewport-px
			var d = (1f/Zoom*delta).ToInt2();
			var newScrollPosition = scrollPosition + d;

			if(!ignoreBorders)
				newScrollPosition = NormalizeScrollPosition(newScrollPosition);

			scrollPosition = newScrollPosition;
		}

		int2 NormalizeScrollPosition(int2 newScrollPosition)
		{
			return newScrollPosition.Clamp(scrollLimits);
		}

		public ScrollDirection GetBlockedDirections()
		{
			var ret = ScrollDirection.None;
			if(scrollPosition.Y <= scrollLimits.Top) ret |= ScrollDirection.Up;
			if(scrollPosition.X <= scrollLimits.Left) ret |= ScrollDirection.Left;
			if(scrollPosition.Y >= scrollLimits.Bottom) ret |= ScrollDirection.Down;
			if(scrollPosition.X >= scrollLimits.Right) ret |= ScrollDirection.Right;
			return ret;
		}

		public Viewport(int2 screenSize, Rectangle mapBounds, Renderer renderer)
		{
			this.screenSize = screenSize;
			this.renderer = renderer;
			this.mapBounds = mapBounds;

			Zoom = Game.Settings.Graphics.PixelDouble ? 2 : 1;
			scrollPosition = new int2(scrollLimits.Location) + new int2(scrollLimits.Size)/2;
		}

		public void DrawRegions( WorldRenderer wr, IInputHandler inputHandler )
		{
			renderer.BeginFrame(scrollPosition, Zoom);
			if (wr != null)
				wr.Draw();

			using( new PerfSample("render_widgets") )
			{
				Ui.Draw();
				var cursorName = Ui.Root.GetCursorOuter(Viewport.LastMousePos) ?? "default";
				CursorProvider.DrawCursor(renderer, cursorName, Viewport.LastMousePos, (int)cursorFrame);
			}

			using( new PerfSample("render_flip") )
			{
				renderer.EndFrame( inputHandler );
			}
		}

		public void Tick()
		{
			cursorFrame += 0.5f;
		}

		// Convert from viewport coords to cell coords (not px)
		public CPos ViewToWorld(MouseInput mi) { return ViewToWorld(mi.Location); }
		public CPos ViewToWorld(int2 loc)
		{
			return (CPos)( (1f / Game.CellSize) * (1f/Zoom*loc.ToFloat2() + Location) ).ToInt2();
		}

		public PPos ViewToWorldPx(int2 loc) { return (PPos)(1f/Zoom*loc.ToFloat2() + Location).ToInt2(); }
		public PPos ViewToWorldPx(MouseInput mi) { return ViewToWorldPx(mi.Location); }

		public void Center(float2 loc)
		{
			scrollPosition = NormalizeScrollPosition((Game.CellSize * loc - 1f/(2*Zoom)*screenSize.ToFloat2()).ToInt2());
		}

		public void Center(IEnumerable<Actor> actors)
		{
			if (!actors.Any()) return;

			var avgPos = actors
				.Select(a => (PVecInt)a.CenterLocation)
				.Aggregate((a, b) => a + b) / actors.Count();
			scrollPosition = NormalizeScrollPosition((avgPos.ToFloat2() - (1f / (2 * Zoom) * screenSize.ToFloat2())).ToInt2());
		}

		// Rectangle (in viewport coords) that contains things to be drawn
		public Rectangle ViewBounds(World world)
		{
			var r = WorldBounds(world);
			var origin = Location.ToInt2();
			var left = Math.Max(0, Game.CellSize * r.Left - origin.X)*Zoom;
			var top = Math.Max(0, Game.CellSize * r.Top - origin.Y)*Zoom;
			var right = Math.Min((Game.CellSize * r.Right - origin.X) * Zoom, Width);
			var bottom = Math.Min((Game.CellSize * r.Bottom - origin.Y) * Zoom, Height);

			return Rectangle.FromLTRB((int)left, (int)top, (int)right, (int)bottom);
		}

		int2 cachedScroll = new int2(int.MaxValue, int.MaxValue);
		Rectangle cachedRect;

		// Rectangle (in cell coords) of cells that are currently visible on the screen
		public Rectangle WorldBounds(World world)
		{
			if (cachedScroll != scrollPosition)
			{
				var boundary = new int2(1,1); // Add a curtain of cells around the viewport to account for rounding errors
				var tl = ViewToWorld(int2.Zero).ToInt2() - boundary;
				var br = ViewToWorld(new int2(Width, Height)).ToInt2() + boundary;

				cachedRect = Rectangle.Intersect(Rectangle.FromLTRB(tl.X, tl.Y, br.X, br.Y), world.Map.Bounds);
				cachedScroll = scrollPosition;
			}

			var b = world.RenderedShroud.Bounds;
			return (b.HasValue) ? Rectangle.Intersect(cachedRect, b.Value) : cachedRect;
		}
	}

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
}