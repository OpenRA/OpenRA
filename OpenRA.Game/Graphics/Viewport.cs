#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Graphics
{
	public class Viewport
	{
		readonly int2 screenSize;
		int2 scrollPosition;
		readonly Renderer renderer;
		readonly Rectangle adjustedMapBounds;

		public float2 Location { get { return scrollPosition; } }

		public int Width { get { return screenSize.X; } }
		public int Height { get { return screenSize.Y; } }

		float cursorFrame = 0f;

		public static int TicksSinceLastMove = 0;
		public static int2 LastMousePos;

		public void Scroll(float2 delta)
		{
			this.Scroll(delta, false);
		}
		
		public void Scroll(float2 delta, bool ignoreBorders)
		{
			var d = delta.ToInt2();
			var newScrollPosition = scrollPosition + d;
			
			if(!ignoreBorders)
				newScrollPosition = this.NormalizeScrollPosition(newScrollPosition);

			scrollPosition = newScrollPosition;
		}
		
		private int2 NormalizeScrollPosition(int2 newScrollPosition)
		{
			return newScrollPosition.Clamp(adjustedMapBounds);
		}
		
		public ScrollDirection GetBlockedDirections()
		{
			ScrollDirection blockedDirections = ScrollDirection.None;
			if(scrollPosition.Y <= adjustedMapBounds.Top)
				blockedDirections = blockedDirections.Set(ScrollDirection.Up, true);
			if(scrollPosition.X <= adjustedMapBounds.Left)
				blockedDirections = blockedDirections.Set(ScrollDirection.Left, true);
			if(scrollPosition.Y >= adjustedMapBounds.Bottom)
				blockedDirections = blockedDirections.Set(ScrollDirection.Down, true);
		  	if(scrollPosition.X >= adjustedMapBounds.Right)
				blockedDirections = blockedDirections.Set(ScrollDirection.Right, true);
			
			return blockedDirections;
		}

		public Viewport(int2 screenSize, Rectangle mapBounds, Renderer renderer)
		{
			this.screenSize = screenSize;
			this.renderer = renderer;
			this.adjustedMapBounds = new Rectangle(Game.CellSize*mapBounds.X - screenSize.X/2,
			                                       Game.CellSize*mapBounds.Y - screenSize.Y/2,
			                                       Game.CellSize*mapBounds.Width,
			                                       Game.CellSize*mapBounds.Height);
			this.scrollPosition = new int2(adjustedMapBounds.Location) + new int2(adjustedMapBounds.Size)/2;
		}
		
		public void DrawRegions( WorldRenderer wr, IInputHandler inputHandler )
		{
			renderer.BeginFrame(scrollPosition);
			
			wr.Draw();
			Widget.DoDraw( wr );
			var cursorName = Widget.RootWidget.GetCursorOuter(Viewport.LastMousePos) ?? "default";
			new Cursor(cursorName).Draw(wr, (int)cursorFrame, Viewport.LastMousePos + Location); 

			renderer.EndFrame( inputHandler );
		}

		public void Tick()
		{
			cursorFrame += 0.5f;
		}

		public float2 ViewToWorld(int2 loc)
		{
			return (1f / Game.CellSize) * (loc.ToFloat2() + Location);
		}
		public float2 ViewToWorld(MouseInput mi)
		{
			return ViewToWorld(mi.Location);
		}
		
		public void Center(float2 loc)
		{
			scrollPosition = this.NormalizeScrollPosition((Game.CellSize*loc - screenSize / 2).ToInt2());
		}

		public void Center(IEnumerable<Actor> actors)
		{
			if (!actors.Any()) return;

			var avgPos = (1f / actors.Count()) * actors
				.Select(a => a.CenterLocation)
				.Aggregate((a, b) => a + b);

			scrollPosition = this.NormalizeScrollPosition((avgPos.ToInt2() - screenSize / 2));
		}
		
		public Rectangle ViewBounds(World world)
		{
			var r = WorldBounds(world);
			var left = (int)(Game.CellSize * r.Left - Game.viewport.Location.X);
			var top = (int)(Game.CellSize * r.Top - Game.viewport.Location.Y);
			var right = left + (int)(Game.CellSize * r.Width);
			var bottom = top + (int)(Game.CellSize * r.Height);
			
			if (left < 0) left = 0;
			if (top < 0) top = 0;
			if (right > Game.viewport.Width) right = Game.viewport.Width;
			if (bottom > Game.viewport.Height) bottom = Game.viewport.Height;
			return new Rectangle(left, top, right - left, bottom - top);
		}

		int2 cachedScroll = new int2(int.MaxValue, int.MaxValue);
		Rectangle cachedRect;
		
		public Rectangle WorldBounds(World world)
		{
			if (cachedScroll != scrollPosition)
			{
				int2 boundary = new int2(1,1); // Add a curtain of cells around the viewport to account for rounding errors
				var tl = ViewToWorld(int2.Zero).ToInt2() - boundary;
				var br = ViewToWorld(new int2(Width, Height)).ToInt2() + boundary;
				cachedRect = Rectangle.Intersect(Rectangle.FromLTRB(tl.X, tl.Y, br.X, br.Y), world.Map.Bounds);
				cachedScroll = scrollPosition;
			}
			
			var b = world.LocalShroud.Bounds;
			return (b.HasValue) ? Rectangle.Intersect(cachedRect, b.Value) : cachedRect;
		}
	}
}