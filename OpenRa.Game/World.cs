using System;
using System.Collections.Generic;
using System.Text;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.Drawing;
using System.Windows.Forms;

namespace OpenRa.Game
{
	class World
	{
		List<Actor> actors = new List<Actor>();
		List<Action<World>> frameEndActions = new List<Action<World>>();
		SpriteRenderer spriteRenderer;
		Game game;
		Region region;
		public IOrderGenerator orderGenerator;
		public UiOverlay uiOverlay;

		public World(Renderer renderer, Game game)
		{
			region = Region.Create(game.viewport, DockStyle.Left, game.viewport.Width - 128, Draw, WorldClicked);
			this.game = game;
			game.viewport.AddRegion(region);
			
			spriteRenderer = new SpriteRenderer(renderer, true);

			uiOverlay = new UiOverlay(spriteRenderer, game);
		}

		public void Add(Actor a) { actors.Add(a); }
		public void Remove( Actor a ) { actors.Remove( a ); }
		public void AddFrameEndTask( Action<World> a ) { frameEndActions.Add( a ); }

		int lastTime = Environment.TickCount;

		void WorldClicked(object sender, MouseEventArgs e)
		{
			float2 xy = (1 / 24.0f) * (new float2(e.Location) + game.viewport.Location);
			if (orderGenerator != null)
			{
				IOrder order = orderGenerator.Order(game, new int2((int)xy.X, (int)xy.Y));
				game.Issue(order);
			}
		}

		void Draw()
		{
			int t = Environment.TickCount;
			int dt = t - lastTime;
			lastTime = t;

			Range<float2> range = new Range<float2>(region.Location, region.Location + region.Size);
			
			foreach (Actor a in actors)
			{
				a.Tick( game, dt );

				Sprite[] images = a.CurrentImages;
				float2 loc = a.RenderLocation;

				if( loc.X > range.End.X || loc.X < range.Start.X - images[ 0 ].bounds.Width )
					continue;

				if( loc.Y > range.End.Y || loc.Y < range.Start.Y - images[ 0 ].bounds.Height )
					continue;

				foreach( Sprite image in images )
					spriteRenderer.DrawSprite(image, loc, (a.owner != null) ? a.owner.Palette : 0);
			}

			foreach( Action<World> a in frameEndActions )
			{
				a( this );
			}
			frameEndActions.Clear();

			uiOverlay.Draw();
			spriteRenderer.Flush();
		}
	}
}
