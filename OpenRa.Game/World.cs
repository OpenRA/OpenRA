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

		double lastTime = Environment.TickCount / 1000.0;

		void Draw()
		{
			double t = Environment.TickCount / 1000.0;
			double dt = t - lastTime;
			lastTime = t;

			Range<float2> range = new Range<float2>(viewport.Location, viewport.Location + viewport.Size);

			foreach (Actor a in actors)
			{
				a.Tick( dt );

				Sprite[] images = a.CurrentImages;

				if( a.location.X > range.End.X || a.location.X < range.Start.X - images[ 0 ].bounds.Width )
					continue;

				if (a.location.Y > range.End.Y || a.location.Y < range.Start.Y - images[0].bounds.Height)
					continue;

				foreach( Sprite image in images )
					spriteRenderer.DrawSprite(image, a.location, a.palette);
			}

			spriteRenderer.Flush();
		}
	}
}
