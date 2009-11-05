using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using IjwFramework.Types;
using System.Collections.Generic;
using OpenRa.Game.Traits;

namespace OpenRa.Game.Graphics
{
	class WorldRenderer
	{
		public readonly TerrainRenderer terrainRenderer;
		public readonly SpriteRenderer spriteRenderer;
        public readonly LineRenderer lineRenderer;
		public readonly Region region;
		public readonly UiOverlay uiOverlay;
		readonly Renderer renderer;

		public static bool ShowUnitPaths = false;

		public WorldRenderer(Renderer renderer)
		{
			terrainRenderer = new TerrainRenderer( renderer, Game.map );

			// TODO: this is layout policy. it belongs at a higher level than this.
			region = Region.Create(Game.viewport, DockStyle.Left,
				Game.viewport.Width - 128, Draw, 
                Game.controller.HandleMouseInput);		

			Game.viewport.AddRegion(region);

			this.renderer = renderer;
			spriteRenderer = new SpriteRenderer(renderer, true);
            lineRenderer = new LineRenderer(renderer);
			uiOverlay = new UiOverlay(spriteRenderer);
		}

		void DrawSpriteList(Player owner, RectangleF rect, 
			IEnumerable<Pair<Sprite, float2>> images)
		{
			foreach (var image in images)
			{
				var loc = image.Second;

				if (loc.X > rect.Right || loc.X < rect.Left 
					- image.First.bounds.Width)
					continue;
				if (loc.Y > rect.Bottom || loc.Y < rect.Top 
					- image.First.bounds.Height)
					continue;

				spriteRenderer.DrawSprite(image.First, loc, 
					(owner != null) ? owner.Palette : 0);
			}
		}

		public void Draw()
		{
			terrainRenderer.Draw( Game.viewport );

			var rect = new RectangleF((region.Position + Game.viewport.Location).ToPointF(), 
                region.Size.ToSizeF());

			foreach (Actor a in Game.world.Actors.OrderBy( u => u.CenterLocation.Y ))
				DrawSpriteList(a.Owner, rect, a.Render());

			foreach (var a in Game.world.Actors
				.Where(u => u.traits.Contains<Traits.RenderWarFactory>())
				.Select(u => u.traits.Get<Traits.RenderWarFactory>()))
				DrawSpriteList(a.self.Owner, rect, a.RenderRoof(a.self));		/* RUDE HACK */

			foreach (IEffect e in Game.world.Effects)
				DrawSpriteList(e.Owner, rect, e.Render());

            uiOverlay.Draw();

			spriteRenderer.Flush();

            var selbox = Game.controller.SelectionBox;
            if (selbox != null)
            {
                var a = selbox.Value.First;
                var b = new float2(selbox.Value.Second.X - a.X, 0);
                var c = new float2(0, selbox.Value.Second.Y - a.Y);

                lineRenderer.DrawLine(a, a + b, Color.White, Color.White);
                lineRenderer.DrawLine(a + b, a + b + c, Color.White, Color.White);
                lineRenderer.DrawLine(a + b + c, a + c, Color.White, Color.White);
                lineRenderer.DrawLine(a, a + c, Color.White, Color.White);

                foreach (var u in Game.SelectUnitsInBox(selbox.Value.First, selbox.Value.Second))
                    DrawSelectionBox(u, Color.Yellow, false);
            }

            var uog = Game.controller.orderGenerator as UnitOrderGenerator;
            if (uog != null)
				foreach( var a in uog.selection )
	                DrawSelectionBox(a, Color.White, true);
            
            lineRenderer.Flush();

			renderer.DrawText(string.Format("RenderFrame {0} ({2:F1} ms)\nTick {1} ({3:F1} ms)\nOre ({4:F1} ms)\n$ {5}", 
				Game.RenderFrame, Game.orderManager.FrameNumber,
				Game.RenderTime * 1000, 
				Game.TickTime * 1000,
				Game.OreTime * 1000,
				Game.LocalPlayer.Cash), new int2(5, 5), Color.White);
		}

        void DrawSelectionBox(Actor selectedUnit, Color c, bool drawHealthBar)
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

			if (drawHealthBar)
			{
				c = Color.Gray;
				lineRenderer.DrawLine(xy + new float2(0, -2), xy + new float2(0, -4), c, c);
				lineRenderer.DrawLine(Xy + new float2(0, -2), Xy + new float2(0, -4), c, c);

				var healthAmount = (float)selectedUnit.Health / selectedUnit.unitInfo.Strength;
				var healthColor = (healthAmount < Rules.General.ConditionRed) ? Color.Red
					: (healthAmount < Rules.General.ConditionYellow) ? Color.Yellow
					: Color.LimeGreen;

				var healthColor2 = Color.FromArgb(
					255,
					healthColor.R / 2,
					healthColor.G / 2,
					healthColor.B / 2);

				var z = float2.Lerp(xy, Xy, healthAmount);

				lineRenderer.DrawLine(z + new float2(0, -4), Xy + new float2(0,-4), c, c);
				lineRenderer.DrawLine(z + new float2(0, -2), Xy + new float2(0, -2), c, c);

				lineRenderer.DrawLine(xy + new float2(0, -3), 
					z + new float2(0, -3), 
					healthColor, healthColor);

				lineRenderer.DrawLine(xy + new float2(0, -2),
					z + new float2(0, -2),
					healthColor2, healthColor2);

				lineRenderer.DrawLine(xy + new float2(0, -4),
					z + new float2(0, -4),
					healthColor2, healthColor2);
			}

			if (ShowUnitPaths)
			{
				var mobile = selectedUnit.traits.GetOrDefault<Mobile>();
				if (mobile != null)
				{
					var path = mobile.GetCurrentPath();
					var start = selectedUnit.Location;

					foreach (var step in path)
					{
						lineRenderer.DrawLine(
							Game.CellSize * start + new float2(12, 12),
							Game.CellSize * step + new float2(12, 12),
							Color.Red, Color.Red);
						start = step;
					}
				}
			}
        }
	}
}
