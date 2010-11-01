#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

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
		readonly int2 mapStart;
		readonly int2 mapEnd;

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
			var topLeftBorder = Game.CellSize* mapStart;
			var bottomRightBorder = Game.CellSize* mapEnd;
		  
			if(newScrollPosition.Y < topLeftBorder.Y - screenSize.Y/2)
				newScrollPosition.Y = topLeftBorder.Y - screenSize.Y/2;
			if(newScrollPosition.X < topLeftBorder.X - screenSize.X/2)
				newScrollPosition.X = topLeftBorder.X - screenSize.X/2;
			if(newScrollPosition.Y > bottomRightBorder.Y - screenSize.Y/2)
				newScrollPosition.Y = bottomRightBorder.Y - screenSize.Y/2;
			if(newScrollPosition.X > bottomRightBorder.X - screenSize.X/2)
				newScrollPosition.X = bottomRightBorder.X - screenSize.X/2;
			
			return newScrollPosition;
		}
		
		public ScrollDirection GetBlockedDirections()
		{
			int2 topLeftBorder = (Game.CellSize* mapStart);
			int2 bottomRightBorder = (Game.CellSize* mapEnd);
			
			ScrollDirection blockedDirections = ScrollDirection.None;
			
			if(scrollPosition.Y <= topLeftBorder.Y - screenSize.Y/2)
				blockedDirections = blockedDirections.Set(ScrollDirection.Up, true);
			if(scrollPosition.X <= topLeftBorder.X - screenSize.X/2)
				blockedDirections = blockedDirections.Set(ScrollDirection.Left, true);
			if(scrollPosition.Y >= bottomRightBorder.Y - screenSize.Y/2)
				blockedDirections = blockedDirections.Set(ScrollDirection.Down, true);
		  	if(scrollPosition.X >= bottomRightBorder.X - screenSize.X/2)
				blockedDirections = blockedDirections.Set(ScrollDirection.Right, true);
			
			return blockedDirections;
		}

		public Viewport(int2 screenSize, int2 mapStart, int2 mapEnd, Renderer renderer)
		{
			this.screenSize = screenSize;
			this.renderer = renderer;
			this.mapStart = mapStart;
			this.mapEnd = mapEnd;

			this.scrollPosition = Game.CellSize* mapStart;
		}
		
		public void DrawRegions( WorldRenderer wr, IInputHandler inputHandler )
		{
			renderer.BeginFrame(scrollPosition);
			wr.Draw();

			Widget.DoDraw( wr );

			var cursorName = Widget.RootWidget.GetCursorOuter(Viewport.LastMousePos) ?? "default";
			var c = new Cursor(cursorName);
			c.Draw(wr, (int)cursorFrame, Viewport.LastMousePos + Location); 

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

		public Rectangle ShroudBounds( World world )
		{
			var localPlayer = world.LocalPlayer;
			if( localPlayer == null ) return world.Map.Bounds;
			if( localPlayer.Shroud.Disabled ) return world.Map.Bounds;
			if( !localPlayer.Shroud.Bounds.HasValue ) return world.Map.Bounds;
			return Rectangle.Intersect( localPlayer.Shroud.Bounds.Value, world.Map.Bounds );
		}
		
		public Rectangle ViewBounds()
		{
			int2 boundary = new int2(1,1); // Add a curtain of cells around the viewport to account for rounding errors
			var tl = ViewToWorld(int2.Zero).ToInt2() - boundary;
			var br = ViewToWorld(new int2(Width, Height)).ToInt2() + boundary;
			return Rectangle.FromLTRB(tl.X, tl.Y, br.X, br.Y);
		}
	}
}