using System.Drawing;
using System;
using System.Linq;
using System.Windows.Forms;
using IjwFramework.Types;
using System.Collections.Generic;
using OpenRa.Game.Traits;
using OpenRa.Game.Support;
using OpenRa.Game.Effects;

namespace OpenRa.Game.Graphics
{
	class WorldRenderer
	{
		public readonly TerrainRenderer terrainRenderer;
		public readonly SpriteRenderer spriteRenderer;
		public readonly LineRenderer lineRenderer;
		public readonly UiOverlay uiOverlay;
		public readonly Renderer renderer;

		public static bool ShowUnitPaths = false;

		public WorldRenderer(Renderer renderer)
		{
			terrainRenderer = new TerrainRenderer(renderer, Rules.Map);

			this.renderer = renderer;
			spriteRenderer = new SpriteRenderer(renderer, true);
			lineRenderer = new LineRenderer(renderer);
			uiOverlay = new UiOverlay(spriteRenderer);
		}

		void DrawSpriteList(RectangleF rect,
			IEnumerable<Renderable> images)
		{
			foreach (var image in images)
			{
				var loc = image.Pos;

				if (loc.X > rect.Right || loc.X < rect.Left - image.Sprite.bounds.Width)
					continue;
				if (loc.Y > rect.Bottom || loc.Y < rect.Top - image.Sprite.bounds.Height)
					continue;

				spriteRenderer.DrawSprite(image.Sprite, loc, image.Palette);
			}
		}

		class SpriteComparer : IComparer<Renderable>
		{
			public int Compare(Renderable x, Renderable y)
			{
				var result = x.ZOffset.CompareTo(y.ZOffset);
				if (result == 0)
					result = x.Pos.Y.CompareTo(y.Pos.Y);

				return result;
			}
		}

		public void Draw()
		{
			terrainRenderer.Draw(Game.viewport);

			var comparer = new SpriteComparer();

			var rect = new RectangleF(
				Game.viewport.Location.ToPointF(),
				new SizeF( Game.viewport.Width, Game.viewport.Height ));

			/* todo: cull to screen again */
			var renderables = Game.world.Actors.SelectMany(a => a.Render())
				.OrderBy(r => r, comparer);

			foreach (var r in renderables)
				spriteRenderer.DrawSprite(r.Sprite, r.Pos, r.Palette);

			foreach (var e in Game.world.Effects)
				DrawSpriteList(rect, e.Render());

			uiOverlay.Draw();

			spriteRenderer.Flush();

			DrawBandBox();

			if (Game.controller.orderGenerator != null)
				Game.controller.orderGenerator.Render();

			lineRenderer.Flush();
			spriteRenderer.Flush();
		}

		void DrawBandBox()
		{
			var selbox = Game.controller.SelectionBox;
			if (selbox == null) return;

			var a = selbox.Value.First;
			var b = new float2(selbox.Value.Second.X - a.X, 0);
			var c = new float2(0, selbox.Value.Second.Y - a.Y);

			lineRenderer.DrawLine(a, a + b, Color.White, Color.White);
			lineRenderer.DrawLine(a + b, a + b + c, Color.White, Color.White);
			lineRenderer.DrawLine(a + b + c, a + c, Color.White, Color.White);
			lineRenderer.DrawLine(a, a + c, Color.White, Color.White);

			foreach (var u in Game.SelectActorsInBox(selbox.Value.First, selbox.Value.Second))
				DrawSelectionBox(u, Color.Yellow, false);
		}

		public void DrawSelectionBox(Actor selectedUnit, Color c, bool drawHealthBar)
		{
			var bounds = selectedUnit.Bounds;

			var xy = new float2(bounds.Left, bounds.Top);
			var Xy = new float2(bounds.Right, bounds.Top);
			var xY = new float2(bounds.Left, bounds.Bottom);
			var XY = new float2(bounds.Right, bounds.Bottom);

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
				DrawHealthBar(selectedUnit, xy, Xy);
				DrawControlGroup(selectedUnit, xy);

				// Only display pips and tags to the owner
				if (selectedUnit.Owner == Game.LocalPlayer)
				{
					DrawPips(selectedUnit, xY);
					DrawTags(selectedUnit, new float2(.5f * (bounds.Left + bounds.Right ), xy.Y));
				}
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

		void DrawHealthBar(Actor selectedUnit, float2 xy, float2 Xy)
		{
			var c = Color.Gray;
			lineRenderer.DrawLine(xy + new float2(0, -2), xy + new float2(0, -4), c, c);
			lineRenderer.DrawLine(Xy + new float2(0, -2), Xy + new float2(0, -4), c, c);

			var healthAmount = (float)selectedUnit.Health / selectedUnit.Info.Strength;
			var healthColor = (healthAmount < Rules.General.ConditionRed) ? Color.Red
				: (healthAmount < Rules.General.ConditionYellow) ? Color.Yellow
				: Color.LimeGreen;

			var healthColor2 = Color.FromArgb(
				255,
				healthColor.R / 2,
				healthColor.G / 2,
				healthColor.B / 2);

			var z = float2.Lerp(xy, Xy, healthAmount);

			lineRenderer.DrawLine(z + new float2(0, -4), Xy + new float2(0, -4), c, c);
			lineRenderer.DrawLine(z + new float2(0, -2), Xy + new float2(0, -2), c, c);

			lineRenderer.DrawLine(xy + new float2(0, -3), z + new float2(0, -3), healthColor, healthColor);
			lineRenderer.DrawLine(xy + new float2(0, -2), z + new float2(0, -2), healthColor2, healthColor2);
			lineRenderer.DrawLine(xy + new float2(0, -4), z + new float2(0, -4), healthColor2, healthColor2);
		}

		// depends on the order of pips in TraitsInterfaces.cs!
		static readonly string[] pipStrings = { "pip-empty", "pip-green", "pip-yellow", "pip-red", "pip-gray" };
		static readonly string[] tagStrings = { "", "tag-fake", "tag-primary" };

		void DrawControlGroup(Actor selectedUnit, float2 basePosition)
		{
			var group = Game.controller.GetControlGroupForActor(selectedUnit);
			if (group == null) return;

			var pipImages = new Animation("pips");
			pipImages.PlayFetchIndex("groups", () => (int)group);
			pipImages.Tick();
			spriteRenderer.DrawSprite(pipImages.Image, basePosition + new float2(-8, 1), PaletteType.Chrome);
		}

		void DrawPips(Actor selectedUnit, float2 basePosition)
		{
			// If a mod wants to implement a unit with multiple pip sources, then they are placed on multiple rows
			var pipxyBase = basePosition + new float2(-12, -7); // Correct for the offset in the shp file
			var pipxyOffset = new float2(0, 0); // Correct for offset due to multiple columns/rows

			foreach (var pips in selectedUnit.traits.WithInterface<IPips>())
			{
				foreach (var pip in pips.GetPips())
				{
					var pipImages = new Animation("pips");
					pipImages.PlayRepeating(pipStrings[(int)pip]);
					spriteRenderer.DrawSprite(pipImages.Image, pipxyBase + pipxyOffset, PaletteType.Chrome);
					pipxyOffset += new float2(4, 0);
					
					if (pipxyOffset.X+5 > selectedUnit.SelectedSize.X)
					{
						pipxyOffset.X = 0;
						pipxyOffset.Y -= 4;
					}
				}
				// Increment row
				pipxyOffset.X = 0;
				pipxyOffset.Y -= 5;
			}
		}
		
		void DrawTags(Actor selectedUnit, float2 basePosition)
		{
			// If a mod wants to implement a unit with multiple tags, then they are placed on multiple rows
			var tagxyBase = basePosition + new float2(-16, 2); // Correct for the offset in the shp file
			var tagxyOffset = new float2(0, 0); // Correct for offset due to multiple rows

			foreach (var tags in selectedUnit.traits.WithInterface<ITags>())
			{
				foreach (var tag in tags.GetTags())
				{
					var tagImages = new Animation("pips");
					tagImages.PlayRepeating(tagStrings[(int)tag]);
					spriteRenderer.DrawSprite(tagImages.Image, tagxyBase + tagxyOffset, PaletteType.Chrome);
					
					// Increment row
					tagxyOffset.Y += 8;
				}
			}
		}
	}
}
