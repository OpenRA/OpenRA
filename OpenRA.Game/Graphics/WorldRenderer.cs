#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
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
		internal readonly UiOverlay uiOverlay;
		internal readonly HardwarePalette palette;

		internal WorldRenderer(World world)
		{
			this.world = world;

			terrainRenderer = new TerrainRenderer(world, this);
			uiOverlay = new UiOverlay();
			palette = new HardwarePalette(world.Map);
		}
		
		public void DrawLine(float2 start, float2 end, Color startColor, Color endColor)
		{
			Game.Renderer.LineRenderer.DrawLine(start,end,startColor,endColor);
		}

		public int GetPaletteIndex(string name) { return palette.GetPaletteIndex(name); }
		public Palette GetPalette(string name) { return palette.GetPalette(name); }
		public void AddPalette(string name, Palette pal) { palette.AddPalette(name, pal); }
		public void UpdatePalette(string name, Palette pal) { palette.UpdatePalette(name, pal); }
		
		void DrawSpriteList(IEnumerable<Renderable> images)
		{
			foreach (var image in images)
				Game.Renderer.SpriteRenderer.DrawSprite(image.Sprite, image.Pos, image.Palette);
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
			if (world.LocalPlayer != null && !world.LocalPlayer.Shroud.Disabled && world.LocalPlayer.Shroud.Bounds.HasValue)
			{
				var r = world.LocalPlayer.Shroud.Bounds.Value;

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

		Renderable[] worldSprites = { };
		public void Tick()
		{
			var bounds = GetBoundsRect();
			var comparer = new SpriteComparer();

			bounds.Offset((int)Game.viewport.Location.X, (int)Game.viewport.Location.Y);

			var actors = world.FindUnits(
				new float2(bounds.Left, bounds.Top),
				new float2(bounds.Right, bounds.Bottom));

			var renderables = actors.SelectMany(a => a.Render())
				.OrderBy(r => r, comparer);

			var effects = world.Effects.SelectMany(e => e.Render());

			worldSprites = renderables.Concat(effects).ToArray();
		}

		public void Draw()
		{
			var bounds = GetBoundsRect();
			Game.Renderer.Device.EnableScissor(bounds.Left, bounds.Top, bounds.Width, bounds.Height);

			terrainRenderer.Draw(Game.viewport);

			DrawSpriteList(worldSprites);
			uiOverlay.Draw(world);
			Game.Renderer.SpriteRenderer.Flush();
			DrawBandBox();

			if (Game.controller.orderGenerator != null)
				Game.controller.orderGenerator.Render(world);

			if (world.LocalPlayer != null)
				world.LocalPlayer.Shroud.Draw();

			Game.Renderer.SpriteRenderer.Flush();

			Game.Renderer.Device.DisableScissor();

			if (Game.Settings.IndexDebug)
			{
				bounds.Offset((int)Game.viewport.Location.X, (int)Game.viewport.Location.Y);
				DrawBins(bounds);
			}

			Game.Renderer.LineRenderer.Flush();
		}

		void DrawBox(RectangleF r, Color color)
		{
			var a = new float2(r.Left, r.Top);
			var b = new float2(r.Right - a.X, 0);
			var c = new float2(0, r.Bottom - a.Y);
			Game.Renderer.LineRenderer.DrawLine(a, a + b, color, color);
			Game.Renderer.LineRenderer.DrawLine(a + b, a + b + c, color, color);
			Game.Renderer.LineRenderer.DrawLine(a + b + c, a + c, color, color);
			Game.Renderer.LineRenderer.DrawLine(a, a + c, color, color);
		}

		void DrawBins(RectangleF bounds)
		{
			DrawBox(bounds, Color.Red);
			if (world.LocalPlayer != null)
				DrawBox(world.LocalPlayer.Shroud.Bounds.Value, Color.Blue);

			for (var j = 0; j < world.Map.MapSize.Y;
				j += world.WorldActor.Info.Traits.Get<SpatialBinsInfo>().BinSize)
			{
				Game.Renderer.LineRenderer.DrawLine(new float2(0, j * 24), new float2(world.Map.MapSize.X * 24, j * 24), Color.Black, Color.Black);
				Game.Renderer.LineRenderer.DrawLine(new float2(j * 24, 0), new float2(j * 24, world.Map.MapSize.Y * 24), Color.Black, Color.Black);
			}
		}

		void DrawBandBox()
		{
			var selbox = Game.controller.SelectionBox;
			if (selbox == null) return;

			var a = selbox.Value.First;
			var b = new float2(selbox.Value.Second.X - a.X, 0);
			var c = new float2(0, selbox.Value.Second.Y - a.Y);

			Game.Renderer.LineRenderer.DrawLine(a, a + b, Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(a + b, a + b + c, Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(a + b + c, a + c, Color.White, Color.White);
			Game.Renderer.LineRenderer.DrawLine(a, a + c, Color.White, Color.White);

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

			Game.Renderer.LineRenderer.DrawLine(xy, xy + new float2(4, 0), c, c);
			Game.Renderer.LineRenderer.DrawLine(xy, xy + new float2(0, 4), c, c);
			Game.Renderer.LineRenderer.DrawLine(Xy, Xy + new float2(-4, 0), c, c);
			Game.Renderer.LineRenderer.DrawLine(Xy, Xy + new float2(0, 4), c, c);

			Game.Renderer.LineRenderer.DrawLine(xY, xY + new float2(4, 0), c, c);
			Game.Renderer.LineRenderer.DrawLine(xY, xY + new float2(0, -4), c, c);
			Game.Renderer.LineRenderer.DrawLine(XY, XY + new float2(-4, 0), c, c);
			Game.Renderer.LineRenderer.DrawLine(XY, XY + new float2(0, -4), c, c);

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

			if (Game.Settings.PathDebug)
				DrawUnitPath(selectedUnit);
		}

		void DrawUnitPath(Actor selectedUnit)
		{
			var mobile = selectedUnit.traits.WithInterface<IMove>().FirstOrDefault();
			if (mobile != null)
			{
				var unit = selectedUnit.traits.Get<Unit>();
				var alt = (unit != null)? new float2(0, -unit.Altitude) : float2.Zero;
				var path = mobile.GetCurrentPath(selectedUnit);
				var start = selectedUnit.CenterLocation + alt;

				var c = Color.Green;

				foreach (var step in path)
				{
					var stp = step + alt;
					DrawLine(stp + new float2(-1, -1), stp + new float2(-1, 1), c, c);
					DrawLine(stp + new float2(-1, 1), stp + new float2(1, 1), c, c);
					DrawLine(stp + new float2(1, 1), stp + new float2(1, -1), c, c);
					DrawLine(stp + new float2(1, -1), stp + new float2(-1, -1), c, c);
					DrawLine(start, stp, c, c);
					start = stp;
				}
			}
		}

		void DrawHealthBar(Actor selectedUnit, float2 xy, float2 Xy)
		{
			var c = Color.Gray;
			Game.Renderer.LineRenderer.DrawLine(xy + new float2(0, -2), xy + new float2(0, -4), c, c);
			Game.Renderer.LineRenderer.DrawLine(Xy + new float2(0, -2), Xy + new float2(0, -4), c, c);

			var healthAmount = (float)selectedUnit.Health / selectedUnit.Info.Traits.Get<OwnedActorInfo>().HP;
			var healthColor = (healthAmount < selectedUnit.World.Defaults.ConditionRed) ? Color.Red
				: (healthAmount < selectedUnit.World.Defaults.ConditionYellow) ? Color.Yellow
				: Color.LimeGreen;

			var healthColor2 = Color.FromArgb(
				255,
				healthColor.R / 2,
				healthColor.G / 2,
				healthColor.B / 2);

			var z = float2.Lerp(xy, Xy, healthAmount);

			Game.Renderer.LineRenderer.DrawLine(z + new float2(0, -4), Xy + new float2(0, -4), c, c);
			Game.Renderer.LineRenderer.DrawLine(z + new float2(0, -2), Xy + new float2(0, -2), c, c);

			Game.Renderer.LineRenderer.DrawLine(xy + new float2(0, -3), z + new float2(0, -3), healthColor, healthColor);
			Game.Renderer.LineRenderer.DrawLine(xy + new float2(0, -2), z + new float2(0, -2), healthColor2, healthColor2);
			Game.Renderer.LineRenderer.DrawLine(xy + new float2(0, -4), z + new float2(0, -4), healthColor2, healthColor2);
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
			Game.Renderer.SpriteRenderer.DrawSprite(pipImages.Image, basePosition + new float2(-8, 1), "chrome");
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
					if (pipxyOffset.X+5 > selectedUnit.GetBounds(false).Width)
					{
						pipxyOffset.X = 0;
						pipxyOffset.Y -= 4;
					}
					var pipImages = new Animation("pips");
					pipImages.PlayRepeating(pipStrings[(int)pip]);
					Game.Renderer.SpriteRenderer.DrawSprite(pipImages.Image, pipxyBase + pipxyOffset, "chrome");
					pipxyOffset += new float2(4, 0);
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
					Game.Renderer.SpriteRenderer.DrawSprite(tagImages.Image, tagxyBase + tagxyOffset, "chrome");
					
					// Increment row
					tagxyOffset.Y += 8;
				}
			}
		}

		public void DrawLocus(Color c, int2[] cells)
		{
			var dict = cells.ToDictionary(a => a, a => 0);
			foreach (var t in dict.Keys)
			{
				if (!dict.ContainsKey(t + new int2(-1, 0)))
					Game.Renderer.LineRenderer.DrawLine(Game.CellSize * t, Game.CellSize * (t + new int2(0, 1)),
						c, c);
				if (!dict.ContainsKey(t + new int2(1, 0)))
					Game.Renderer.LineRenderer.DrawLine(Game.CellSize * (t + new int2(1, 0)), Game.CellSize * (t + new int2(1, 1)),
						c, c);
				if (!dict.ContainsKey(t + new int2(0, -1)))
					Game.Renderer.LineRenderer.DrawLine(Game.CellSize * t, Game.CellSize * (t + new int2(1, 0)),
						c, c);
				if (!dict.ContainsKey(t + new int2(0, 1)))
					Game.Renderer.LineRenderer.DrawLine(Game.CellSize * (t + new int2(0, 1)), Game.CellSize * (t + new int2(1, 1)),
						c, c);
			}
		}

		public void DrawRangeCircle(Color c, float2 location, int range)
		{
			var prev = location + Game.CellSize * range * float2.FromAngle(0);
			for (var i = 1; i <= 32; i++)
			{
				var pos = location + Game.CellSize * range * float2.FromAngle((float)(Math.PI * i) / 16);
				Game.Renderer.LineRenderer.DrawLine(prev, pos, c, c);
				prev = pos;
			}
		}
	}
}
