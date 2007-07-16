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
		Renderer renderer;
		Viewport viewport;

		public World(Renderer renderer, Viewport viewport)
		{
			this.renderer = renderer;
			this.viewport = viewport;
			viewport.AddRegion(Region.Create(viewport, DockStyle.Left, viewport.Width - 128, Draw));
			spriteRenderer = new SpriteRenderer(renderer, true);
		}

		public void Add(Actor a) { actors.Add(a); }
		public void Remove( Actor a ) { actors.Remove( a ); }
		public void AddFrameEndTask( Action<World> a ) { frameEndActions.Add( a ); }

		double lastTime = Environment.TickCount / 1000.0;

		void Draw()
		{
			double t = Environment.TickCount / 1000.0;
			double dt = t - lastTime;
			lastTime = t;

			Range<float2> range = new Range<float2>(viewport.Location, viewport.Location + viewport.Size);

			foreach (Actor a in actors)
			{
				a.Tick( this, dt );

				Sprite[] images = a.CurrentImages;

				if( a.renderLocation.X > range.End.X || a.renderLocation.X < range.Start.X - images[ 0 ].bounds.Width )
					continue;

				if (a.renderLocation.Y > range.End.Y || a.renderLocation.Y < range.Start.Y - images[0].bounds.Height)
					continue;

				foreach( Sprite image in images )
					spriteRenderer.DrawSprite(image, a.renderLocation, a.palette);
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
