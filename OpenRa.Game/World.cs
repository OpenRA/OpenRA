using System;
using System.Collections.Generic;
using System.Text;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.Drawing;

namespace OpenRa.Game
{
	class World
	{
		List<Actor> actors = new List<Actor>();
		SpriteRenderer spriteRenderer;

		public World(Renderer renderer) { spriteRenderer = new SpriteRenderer(renderer, true); }

		public void Add(Actor a) { actors.Add(a); }

		public void Draw(Renderer renderer, Viewport viewport)
		{
			Range<float2> range = new Range<float2>(viewport.Location, viewport.Size);

			foreach (Actor a in actors)
			{
				Sprite[] images = a.CurrentImages;

				if (images == null)
					continue;

				if (a.location.X > range.End.X || a.location.X < range.Start.X - images[0].bounds.Width)
					continue;

				if (a.location.Y > range.End.Y || a.location.Y < range.Start.Y - images[0].bounds.Height)
					continue;

				foreach (Sprite image in images)
					spriteRenderer.DrawSprite(image, a.location, a.palette);
			}

			spriteRenderer.Flush();
		}
	}
}
