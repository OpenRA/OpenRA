#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Support;
using System.Drawing;

namespace OpenRA.Graphics
{
	interface IHandleInput
	{
		bool HandleInput(World world, MouseInput mi);
	}

	class Viewport
	{
		readonly float2 screenSize;
		float2 scrollPosition;
		readonly Renderer renderer;

		public float2 Location { get { return scrollPosition; } }

		public int Width { get { return (int)screenSize.X; } }
		public int Height { get { return (int)screenSize.Y; } }

		SpriteRenderer cursorRenderer;
		int2 mousePos;
		float cursorFrame = 0f;

		public void Scroll(float2 delta)
		{
			scrollPosition = scrollPosition + delta;
		}

		public IEnumerable<IHandleInput> regions { get { return new IHandleInput[] { Game.chrome, Game.controller }; } }

		public Viewport(float2 screenSize, int2 mapStart, int2 mapEnd, Renderer renderer)
		{
			this.screenSize = screenSize;
			this.renderer = renderer;
			cursorRenderer = renderer.SpriteRenderer;

			this.scrollPosition = Game.CellSize* mapStart;
		}
		
		ConnectionState lastConnectionState = ConnectionState.PreConnecting;
		bool gameWasStarted = false;
		public void DrawRegions( World world )
		{
			Timer.Time( "DrawRegions start" );

			world.WorldRenderer.palette.Update(
				world.WorldActor.traits.WithInterface<IPaletteModifier>());

			float2 r1 = new float2(2, -2) / screenSize;
			float2 r2 = new float2(-1, 1);

			renderer.BeginFrame(r1, r2, scrollPosition.ToInt2());
			world.WorldRenderer.Draw();
			Timer.Time( "worldRenderer: {0}" );
			if( Game.orderManager.GameStarted && world.LocalPlayer != null)
			{
				if (!gameWasStarted)
				{
					Chrome.rootWidget.OpenWindow("INGAME_ROOT");
					gameWasStarted = true;
				}
				
				Game.chrome.Draw( world );	
			}
			else
			{
				// Still hacky, but at least it uses widgets
				// TODO: Clean up the logic of this beast
				// TODO: Have a proper "In main menu" state
				ConnectionState state = Game.orderManager.Connection.ConnectionState;
				if (state != lastConnectionState)
				{
					switch( Game.orderManager.Connection.ConnectionState )
					{
						case ConnectionState.PreConnecting:
							Chrome.rootWidget.GetWidget("MAINMENU_BG").Visible = true;
							Chrome.rootWidget.GetWidget("CONNECTING_BG").Visible = false;
							Chrome.rootWidget.GetWidget("CONNECTION_FAILED_BG").Visible = false;
							break;
						case ConnectionState.Connecting:
							Chrome.rootWidget.GetWidget("MAINMENU_BG").Visible = false;
							Chrome.rootWidget.GetWidget("CONNECTING_BG").Visible = true;
							Chrome.rootWidget.GetWidget("CONNECTION_FAILED_BG").Visible = false;
							break;
						case ConnectionState.NotConnected:
							Chrome.rootWidget.GetWidget("MAINMENU_BG").Visible = false;
							Chrome.rootWidget.GetWidget("CONNECTING_BG").Visible = false;
							Chrome.rootWidget.GetWidget("CONNECTION_FAILED_BG").Visible = true;
							break;
						case ConnectionState.Connected:
							Chrome.rootWidget.GetWidget("MAINMENU_BG").Visible = false;
							Chrome.rootWidget.GetWidget("CONNECTING_BG").Visible = false;
							Chrome.rootWidget.GetWidget("CONNECTION_FAILED_BG").Visible = false;
							break;
					}
				
					// TODO: Kill this (hopefully!) soon
					if (state == ConnectionState.Connected)
						Chrome.rootWidget.OpenWindow( "SERVER_LOBBY" );
				}
				
				lastConnectionState = state;

			}
			Game.chrome.DrawWidgets(world);
			if( Chrome.rootWidget.GetWidget( "SERVER_LOBBY" ).Visible )
				Game.chrome.DrawLobby();
			else if( Chrome.rootWidget.GetWidget( "MAP_CHOOSER" ).Visible )
				Game.chrome.DrawMapChooser();

			Timer.Time( "widgets: {0}" );

			var cursorName = Game.chrome.HitTest(mousePos) ? "default" : Game.controller.ChooseCursor( world );
			var c = new Cursor(cursorName);
			cursorRenderer.DrawSprite(c.GetSprite((int)cursorFrame), mousePos + Location - c.GetHotspot(), "cursor");
			Timer.Time( "cursors: {0}" );

			renderer.RgbaSpriteRenderer.Flush();
			renderer.SpriteRenderer.Flush();
			renderer.WorldSpriteRenderer.Flush();

			renderer.EndFrame();
			Timer.Time( "endFrame: {0}" );
		}

		public void Tick()
		{
			cursorFrame += 0.5f;
		}

		IHandleInput dragRegion = null;
		public void DispatchMouseInput(World world, MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Move)
				mousePos = mi.Location;

			if (dragRegion != null) {
				dragRegion.HandleInput( world, mi );
				if (mi.Event == MouseInputEvent.Up) dragRegion = null;
				return;
			}

			dragRegion = regions.FirstOrDefault(r => r.HandleInput(world, mi));
			if (mi.Event != MouseInputEvent.Down)
				dragRegion = null;
		}

		public float2 ViewToWorld(MouseInput mi)
		{
			return (1 / 24.0f) * (new float2(mi.Location.X, mi.Location.Y) + Location);
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

		public void GoToStartLocation( Player player )
		{
			Center( player.World.Queries.OwnedBy[ player ].WithTrait<Selectable>().Select( a => a.Actor ) );
		}

		public Rectangle? ShroudBounds()
		{
			var localPlayer = Game.world.LocalPlayer;
			if (localPlayer == null) return null;
			return localPlayer.Shroud.Bounds;
		}
	}
}
