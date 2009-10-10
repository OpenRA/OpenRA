using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OpenRa.Game.Graphics
{
	class WorldRenderer
	{
		public readonly SpriteRenderer spriteRenderer;
        public readonly LineRenderer lineRenderer;
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
            lineRenderer = new LineRenderer(renderer);
			uiOverlay = new UiOverlay(spriteRenderer, world.game);
			this.world = world;
		}

		public void Draw()
		{
			var rect = new RectangleF((region.Position + world.game.viewport.Location).ToPointF(), 
                region.Size.ToSizeF());

			foreach (Actor a in world.Actors)
			{
				var images = a.Render();

				foreach( var image in images )
				{
					var loc = image.Second;

					if( loc.X > rect.Right || loc.X < rect.Left - image.First.bounds.Width )
						continue;
					if( loc.Y > rect.Bottom || loc.Y < rect.Top - image.First.bounds.Height )
						continue;

					spriteRenderer.DrawSprite( image.First, loc, ( a.Owner != null ) ? a.Owner.Palette : 0 );
				}
			}

            uiOverlay.Draw();

			spriteRenderer.Flush();

            var selbox = world.game.controller.SelectionBox();
            if (selbox != null)
            {
                var a = selbox.Value.First;
                var b = new float2(selbox.Value.Second.X - a.X, 0);
                var c = new float2(0, selbox.Value.Second.Y - a.Y);

                lineRenderer.DrawLine(a, a + b, Color.White, Color.White);
                lineRenderer.DrawLine(a + b, a + b + c, Color.White, Color.White);
                lineRenderer.DrawLine(a + b + c, a + c, Color.White, Color.White);
                lineRenderer.DrawLine(a, a + c, Color.White, Color.White);

                foreach (var u in Controller.FindUnits(world.game, selbox.Value.First, selbox.Value.Second))
                    DrawSelectionBox(u, Color.Yellow);
            }

            var selection = world.game.controller.orderGenerator as UnitOrderGenerator;
            if (selection != null)
				foreach( var a in world.Actors.Intersect(selection.selection) )		/* make sure we don't grab actors that are dead */
	                DrawSelectionBox(a, Color.White);

            
            lineRenderer.Flush();
		}

        void DrawSelectionBox(Actor selectedUnit, Color c)
        {
            var center = selectedUnit.CenterLocation;
            var size = selectedUnit.SelectedSize;

            var xy = center - 0.5f * size;
            var XY = center + 0.5f * size;
            var Xy = new float2(XY.X, xy.Y);
            var xY = new float2(xy.X, XY.Y);

            lineRenderer.DrawLine(xy, xy + new float2(4, 0), c, c);
            lineRenderer.DrawLine(xy, xy + new float2(0, 4), c, c);
            lineRenderer.DrawLine(Xy, Xy + new float2(-4, 0), c, c);
            lineRenderer.DrawLine(Xy, Xy + new float2(0, 4), c, c);

            lineRenderer.DrawLine(xY, xY + new float2(4, 0), c, c);
            lineRenderer.DrawLine(xY, xY + new float2(0, -4), c, c);
            lineRenderer.DrawLine(XY, XY + new float2(-4, 0), c, c);
            lineRenderer.DrawLine(XY, XY + new float2(0, -4), c, c);
        }
	}
}
