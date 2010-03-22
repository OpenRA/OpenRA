#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public class WorldRenderer
	{
		readonly World world;
		internal readonly TerrainRenderer terrainRenderer;
		internal readonly SpriteRenderer spriteRenderer;
		internal readonly LineRenderer lineRenderer;
		internal readonly UiOverlay uiOverlay;
		internal readonly Renderer renderer;
		internal readonly HardwarePalette palette;

		public static bool ShowUnitPaths = false;

		internal WorldRenderer(World world, Renderer renderer)
		{
			this.world = world;
			this.renderer = renderer;

			terrainRenderer = new TerrainRenderer(world, renderer, this);
			spriteRenderer = new SpriteRenderer(renderer, true);
			lineRenderer = new LineRenderer(renderer);
			uiOverlay = new UiOverlay(spriteRenderer);
			palette = new HardwarePalette(renderer, world.Map);
		}
		
		public int GetPaletteIndex(string name)
		{
			return palette.GetPaletteIndex(name);
		}

		public Palette GetPalette(string name)
		{
			return palette.GetPalette(name);
		}
		
		public void AddPalette(string name, Palette pal)
		{
			palette.AddPalette(name, pal);
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

		Rectangle GetBoundsRect()
		{
			if (!world.LocalPlayer.Shroud.HasGPS && world.LocalPlayer.Shroud.bounds.HasValue)
			{
				var r = world.LocalPlayer.Shroud.bounds.Value;

				var left = (int)(Game.CellSize * r.Left - Game.viewport.Location.X);
				var top = (int)(Game.CellSize * r.Top - Game.viewport.Location.Y);
				var right = left + (int)(Game.CellSize * r.Width);
				var bottom = top + (int)(Game.CellSize * r.Height);

				if (left < 0) left = 0;
				if (top < 0) top = 0;
				if (right > Game.viewport.Width) right = Game.viewport.Width;
				if (bottom > Game.viewport.Height) bottom = Game.viewport.Height;

				return new Rectangle(left, top, right - left, bottom - top);
			}
			else
				return new Rectangle(0, 0, Game.viewport.Width, Game.viewport.Height);
		}

		public void Draw()
		{
			var bounds = GetBoundsRect();
			renderer.Device.EnableScissor(bounds.Left, bounds.Top, bounds.Width, bounds.Height);

			terrainRenderer.Draw(Game.viewport);

			var comparer = new SpriteComparer();

			bounds.Offset((int)Game.viewport.Location.X, (int)Game.viewport.Location.Y);

			var renderables = world.Actors.SelectMany(a => a.Render())
				.OrderBy(r => r, comparer);

			DrawSpriteList(bounds, renderables);

			foreach (var e in world.Effects)
				DrawSpriteList(bounds, e.Render());

			uiOverlay.Draw(world);

			spriteRenderer.Flush();

			DrawBandBox();

			if (Game.controller.orderGenerator != null)
				Game.controller.orderGenerator.Render(world);

			world.LocalPlayer.Shroud.Draw(spriteRenderer);

			spriteRenderer.Flush();

			renderer.Device.DisableScissor();

			{
				var a = new float2( bounds.Left, bounds.Top );
				var b = new float2( bounds.Right - a.X, 0);
				var c = new float2(0, bounds.Bottom - a.Y);
				lineRenderer.DrawLine(a, a + b, Color.Red, Color.Red);
				lineRenderer.DrawLine(a + b, a + b + c, Color.Red, Color.Red);
				lineRenderer.DrawLine(a + b + c, a + c, Color.Red, Color.Red);
				lineRenderer.DrawLine(a, a + c, Color.Red, Color.Red);
			}

			lineRenderer.Flush();
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

			foreach (var u in world.SelectActorsInBox(selbox.Value.First, selbox.Value.Second))
				DrawSelectionBox(u, Color.Yellow, false);
		}

		public void DrawSelectionBox(Actor selectedUnit, Color c, bool drawHealthBar)
		{
			var bounds = selectedUnit.GetBounds(true);

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
				if (selectedUnit.Owner == world.LocalPlayer)
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

			var healthAmount = (float)selectedUnit.Health / selectedUnit.Info.Traits.Get<OwnedActorInfo>().HP;
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
			var group = Game.controller.selection.GetControlGroupForActor(selectedUnit);
			if (group == null) return;

			var pipImages = new Animation("pips");
			pipImages.PlayFetchIndex("groups", () => (int)group);
			pipImages.Tick();
			spriteRenderer.DrawSprite(pipImages.Image, basePosition + new float2(-8, 1), "chrome");
		}

		void DrawPips(Actor selectedUnit, float2 basePosition)
		{
			// If a mod wants to implement a unit with multiple pip sources, then they are placed on multiple rows
			var pipxyBase = basePosition + new float2(-12, -7); // Correct for the offset in the shp file
			var pipxyOffset = new float2(0, 0); // Correct for offset due to multiple columns/rows

			foreach (var pips in selectedUnit.traits.WithInterface<IPips>())
			{
				foreach (var pip in pips.GetPips(selectedUnit))
				{
					var pipImages = new Animation("pips");
					pipImages.PlayRepeating(pipStrings[(int)pip]);
					spriteRenderer.DrawSprite(pipImages.Image, pipxyBase + pipxyOffset, "chrome");
					pipxyOffset += new float2(4, 0);
					
					if (pipxyOffset.X+5 > selectedUnit.GetBounds(false).Width)
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
					if (tag == TagType.None)
						continue;
						
					var tagImages = new Animation("pips");
					tagImages.PlayRepeating(tagStrings[(int)tag]);
					spriteRenderer.DrawSprite(tagImages.Image, tagxyBase + tagxyOffset, "chrome");
					
					// Increment row
					tagxyOffset.Y += 8;
				}
			}
		}
	}
}
