using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Orders;
using OpenRa.Traits;
using OpenRa.Network;

namespace OpenRa.Graphics
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
			cursorRenderer = new SpriteRenderer(renderer, true);

			this.scrollPosition = Game.CellSize* mapStart;
		}

		public void DrawRegions( World world )
		{
			world.WorldRenderer.palette.Update(world.Queries.WithTraitMultiple<IPaletteModifier>().Select(t=>t.Trait));

			float2 r1 = new float2(2, -2) / screenSize;
			float2 r2 = new float2(-1, 1);

			renderer.BeginFrame(r1, r2, scrollPosition);

			if( Game.orderManager.GameStarted )
			{
				world.WorldRenderer.Draw();
				Game.chrome.Draw( world );

				if( Game.orderManager.Connection.ConnectionState == ConnectionState.NotConnected )
					Game.chrome.DrawDialog("Connection lost.");
			}
			else
			{
				// what a hack. as soon as we have some real chrome stuff...

				switch( Game.orderManager.Connection.ConnectionState )
				{
					case ConnectionState.Connecting:
						Game.chrome.DrawDialog("Connecting to {0}:{1}...".F( Game.Settings.NetworkHost, Game.Settings.NetworkPort ));
						break;
					case ConnectionState.NotConnected:
						Game.chrome.DrawDialog("Connection failed.");
						break;
					case ConnectionState.Connected:
						Game.chrome.DrawLobby( world );
						break;
				}
			}

			var c = Game.chrome.HitTest(mousePos) ? Cursor.Default : Game.controller.ChooseCursor( world );
			cursorRenderer.DrawSprite(c.GetSprite((int)cursorFrame), mousePos + Location - c.GetHotspot(), 0);
			cursorRenderer.Flush();

			renderer.EndFrame();
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
	}
}
