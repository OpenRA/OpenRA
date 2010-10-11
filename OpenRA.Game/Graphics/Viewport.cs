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
		readonly float2 screenSize;
		float2 scrollPosition;
		readonly Renderer renderer;
		readonly int2 mapStart;
		readonly int2 mapEnd;

		public float2 Location { get { return scrollPosition; } }

		public int Width { get { return (int)screenSize.X; } }
		public int Height { get { return (int)screenSize.Y; } }

		float cursorFrame = 0f;

		public static int TicksSinceLastMove = 0;
		public static int2 LastMousePos;

		public void Scroll(float2 delta)
		{
			this.Scroll(delta, false);
		}
		
		public void Scroll(float2 delta, bool ignoreBorders)
		{
			float2 newScrollPosition = scrollPosition + delta;
			
			if(!ignoreBorders)
				newScrollPosition = this.NormalizeScrollPosition(newScrollPosition);

			scrollPosition = newScrollPosition;
		}
		
		private float2 NormalizeScrollPosition(float2 newScrollPosition)
		{
			float2 topLeftBorder = (Game.CellSize* mapStart).ToFloat2();
			float2 bottomRightBorder = (Game.CellSize* mapEnd).ToFloat2();
		  
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

		public Viewport(float2 screenSize, int2 mapStart, int2 mapEnd, Renderer renderer)
		{
			this.screenSize = screenSize;
			this.renderer = renderer;
			this.mapStart = mapStart;
			this.mapEnd = mapEnd;

			this.scrollPosition = Game.CellSize* mapStart;
		}
		
		public void DrawRegions( WorldRenderer wr, World world )
		{
			renderer.BeginFrame(scrollPosition);
			wr.Draw();

			Widget.DoDraw();

			var cursorName = Widget.RootWidget.GetCursorOuter(Viewport.LastMousePos) ?? "default";
			var c = new Cursor(cursorName);
			c.Draw(wr, (int)cursorFrame, Viewport.LastMousePos + Location); 

			renderer.EndFrame();
		}

		public void RefreshPalette()
		{
			Game.world.WorldRenderer.palette.Update(
				Game.world.WorldActor.TraitsImplementing<IPaletteModifier>());
		}

		public void Tick()
		{
			cursorFrame += 0.5f;
			RefreshPalette();
		}

		public float2 ViewToWorld(int2 loc)
		{
			return (1f / Game.CellSize) * (loc.ToFloat2() + Location);
		}
		public float2 ViewToWorld(MouseInput mi)
		{
			return ViewToWorld(mi.Location);
		}
		
		public void Center(int2 loc)
		{
			scrollPosition = this.NormalizeScrollPosition(Game.CellSize*loc - .5f * new float2(Width, Height));
		}

		public void Center(IEnumerable<Actor> actors)
		{
			if (!actors.Any()) return;

			var avgPos = (1f / actors.Count()) * actors
				.Select(a => a.CenterLocation)
				.Aggregate((a, b) => a + b);

			scrollPosition = this.NormalizeScrollPosition((avgPos - .5f * new float2(Width, Height)));
		}

		public Rectangle? ShroudBounds()
		{
			var localPlayer = Game.world.LocalPlayer;
			if (localPlayer == null) return null;
			if (localPlayer.Shroud.Disabled) return null;
			return localPlayer.Shroud.Bounds;
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