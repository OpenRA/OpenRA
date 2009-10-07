using System.Drawing;
using System.Windows.Forms;

namespace OpenRa.Game.Graphics
{
	class WorldRenderer
	{
		public readonly SpriteRenderer spriteRenderer;
		public readonly World world;
		public readonly Region region;
		public readonly UiOverlay uiOverlay;

		public WorldRenderer(Renderer renderer, World world)
		{
			// TODO: this is layout policy. it belongs at a higher level than this.

			region = Region.Create(world.game.viewport, DockStyle.Left,
				world.game.viewport.Width - 128, Draw, 
                world.game.controller.HandleMouseInput);		

			world.game.viewport.AddRegion(region);

			spriteRenderer = new SpriteRenderer(renderer, true);
			uiOverlay = new UiOverlay(spriteRenderer, world.game);
			this.world = world;
		}

		public void Draw()
		{
			var rect = new RectangleF(region.Location.ToPointF(), region.Size.ToSizeF());

			foreach (Actor a in world.Actors)
			{
				var images = a.CurrentImages;

				foreach( var image in images )
				{
					var loc = image.Second;

					if( loc.X > rect.Right || loc.X < rect.Left - image.First.bounds.Width )
						continue;
					if( loc.Y > rect.Bottom || loc.Y < rect.Top - image.First.bounds.Height )
						continue;

					spriteRenderer.DrawSprite( image.First, loc, ( a.owner != null ) ? a.owner.Palette : 0 );
				}
			}

			spriteRenderer.Flush();
		}
	}
}
