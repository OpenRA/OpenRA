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
using OpenRA.Support;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Graphics
{
	public class Viewport
	{
		readonly float2 screenSize;
		float2 scrollPosition;
		readonly Renderer renderer;

		public float2 Location { get { return scrollPosition; } }

		public int Width { get { return (int)screenSize.X; } }
		public int Height { get { return (int)screenSize.Y; } }

		float cursorFrame = 0f;

		public static int TicksSinceLastMove = 0;
		public static int2 LastMousePos;

		public void Scroll(float2 delta)
		{
			scrollPosition = scrollPosition + delta;
		}

		public Viewport(float2 screenSize, int2 mapStart, int2 mapEnd, Renderer renderer)
		{
			this.screenSize = screenSize;
			this.renderer = renderer;

			this.scrollPosition = Game.CellSize* mapStart;
		}
		
		public void DrawRegions( World world )
		{
			Timer.Time( "DrawRegions start" );

			float2 r1 = new float2(2, -2) / screenSize;
			float2 r2 = new float2(-1, 1);

			renderer.BeginFrame(r1, r2, scrollPosition.ToInt2());
			world.WorldRenderer.Draw();
			Timer.Time( "worldRenderer: {0}" );

			Widget.DoDraw(world);
			Timer.Time( "widgets: {0}" );

			var cursorName = Widget.RootWidget.GetCursorOuter(Viewport.LastMousePos) ?? "default";
			var c = new Cursor(cursorName);
			c.Draw((int)cursorFrame, Viewport.LastMousePos + Location); 
			Timer.Time( "cursors: {0}" );

			renderer.RgbaSpriteRenderer.Flush();
			renderer.SpriteRenderer.Flush();
			renderer.WorldSpriteRenderer.Flush();

			renderer.EndFrame();
			Timer.Time( "endFrame: {0}" );
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
			scrollPosition = (Game.CellSize*loc - .5f * new float2(Width, Height)).ToInt2();
		}

		public void Center(IEnumerable<Actor> actors)
		{
			if (!actors.Any()) return;

			var avgPos = (1f / actors.Count()) * actors
				.Select(a => a.CenterLocation)
				.Aggregate((a, b) => a + b);

			scrollPosition = (avgPos - .5f * new float2(Width, Height)).ToInt2();
		}

		public Rectangle? ShroudBounds()
		{
			var localPlayer = Game.world.LocalPlayer;
			if (localPlayer == null) return null;
			if (localPlayer.Shroud.Disabled) return null;
			return localPlayer.Shroud.Bounds;
		}
	}
}
