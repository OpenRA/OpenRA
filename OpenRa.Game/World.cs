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
		Viewport viewport;
		public ISelectable myUnit;

		public World(Renderer renderer, Viewport viewport)
		{
			this.viewport = viewport;
			viewport.AddRegion(Region.Create(viewport, DockStyle.Left, viewport.Width - 128, Draw));
			spriteRenderer = new SpriteRenderer(renderer, true);
		}

		public void Add(Actor a) { actors.Add(a); }
		public void Remove( Actor a ) { actors.Remove( a ); }
		public void AddFrameEndTask( Action<World> a ) { frameEndActions.Add( a ); }

		double lastTime = Environment.TickCount / 1000.0;

		void Draw( Game game )
		{
			double t = Environment.TickCount / 1000.0;
			double dt = t - lastTime;
			lastTime = t;

			Range<float2> range = new Range<float2>(viewport.Location, viewport.Location + viewport.Size);

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
					spriteRenderer.DrawSprite(image, loc, a.palette);
			}

			foreach( Action<World> a in frameEndActions )
			{
				a( this );
			}
			frameEndActions.Clear();

			spriteRenderer.Flush();
		}
	}
}
