#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class RadarWidget : Widget, IDisposable
	{
		public string WorldInteractionController = null;
		public int AnimationLength = 5;
		public string RadarOnlineSound = null;
		public string RadarOfflineSound = null;
		public Func<bool> IsEnabled = () => true;
		public Action AfterOpen = () => { };
		public Action AfterClose = () => { };
		public Action<float> Animating = _ => { };

		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly RadarPings radarPings;
		readonly bool isRectangularIsometric;
		readonly int cellWidth;
		readonly int previewWidth;
		readonly int previewHeight;

		readonly HashSet<PPos> dirtyShroudCells = new HashSet<PPos>();

		float radarMinimapHeight;
		int frame;
		bool hasRadar;
		bool cachedEnabled;

		float previewScale = 0;
		int2 previewOrigin = int2.Zero;
		Rectangle mapRect = Rectangle.Empty;

		Sheet radarSheet;
		byte[] radarData;

		Sprite terrainSprite;
		Sprite actorSprite;
		Sprite shroudSprite;
		Shroud renderShroud;

		[ObjectCreator.UseCtor]
		public RadarWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			radarPings = world.WorldActor.TraitOrDefault<RadarPings>();

			isRectangularIsometric = world.Map.Grid.Type == MapGridType.RectangularIsometric;
			cellWidth = isRectangularIsometric ? 2 : 1;
			previewWidth = world.Map.MapSize.X;
			previewHeight = world.Map.MapSize.Y;
			if (isRectangularIsometric)
				previewWidth = 2 * previewWidth - 1;
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			// The four layers are stored in a 2x2 grid within a single texture
			radarSheet = new Sheet(SheetType.BGRA, new Size(2 * previewWidth, 2 * previewHeight).NextPowerOf2());
			radarSheet.CreateBuffer();
			radarData = radarSheet.GetData();

			MapBoundsChanged();

			// Set initial terrain data
			foreach (var cell in world.Map.AllCells)
				UpdateTerrainCell(cell);

			world.Map.Tiles.CellEntryChanged += UpdateTerrainCell;
			world.Map.CustomTerrain.CellEntryChanged += UpdateTerrainCell;
		}

		void MapBoundsChanged()
		{
			var map = world.Map;

			// The minimap is drawn in cell space, so we need to
			// unproject the bounds to find the extent of the map.
			var projectedLeft = map.Bounds.Left;
			var projectedRight = map.Bounds.Right;
			var projectedTop = map.Bounds.Top;
			var projectedBottom = map.Bounds.Bottom;
			var top = int.MaxValue;
			var bottom = int.MinValue;
			var left = map.Bounds.Left * cellWidth;
			var right = map.Bounds.Right * cellWidth;

			for (var x = projectedLeft; x < projectedRight; x++)
			{
				var allTop = map.Unproject(new PPos(x, projectedTop));
				var allBottom = map.Unproject(new PPos(x, projectedBottom));
				if (allTop.Any())
					top = Math.Min(top, allTop.MinBy(uv => uv.V).V);

				if (allBottom.Any())
					bottom = Math.Max(bottom, allBottom.MinBy(uv => uv.V).V);
			}

			var b = Rectangle.FromLTRB(left, top, right, bottom);
			var rb = RenderBounds;
			previewScale = Math.Min(rb.Width * 1f / b.Width, rb.Height * 1f / b.Height);
			previewOrigin = new int2((int)((rb.Width - previewScale * b.Width) / 2), (int)((rb.Height - previewScale * b.Height) / 2));
			mapRect = new Rectangle(previewOrigin.X, previewOrigin.Y, (int)(previewScale * b.Width), (int)(previewScale * b.Height));

			terrainSprite = new Sprite(radarSheet, b, TextureChannel.Alpha);
			shroudSprite = new Sprite(radarSheet, new Rectangle(b.Location + new Size(previewWidth, 0), b.Size), TextureChannel.Alpha);
			actorSprite = new Sprite(radarSheet, new Rectangle(b.Location + new Size(0, previewHeight), b.Size), TextureChannel.Alpha);
		}

		void UpdateTerrainCell(CPos cell)
		{
			var uv = cell.ToMPos(world.Map);

			if (!world.Map.CustomTerrain.Contains(uv))
				return;

			var custom = world.Map.CustomTerrain[uv];
			int leftColor, rightColor;
			if (custom == byte.MaxValue)
			{
				var type = world.Map.Rules.TileSet.GetTileInfo(world.Map.Tiles[uv]);
				leftColor = type != null ? type.LeftColor.ToArgb() : Color.Black.ToArgb();
				rightColor = type != null ? type.RightColor.ToArgb() : Color.Black.ToArgb();
			}
			else
				leftColor = rightColor = world.Map.Rules.TileSet[custom].Color.ToArgb();

			var stride = radarSheet.Size.Width;

			unsafe
			{
				fixed (byte* colorBytes = &radarData[0])
				{
					var colors = (int*)colorBytes;
					if (isRectangularIsometric)
					{
						// Odd rows are shifted right by 1px
						var dx = uv.V & 1;
						if (uv.U + dx > 0)
							colors[uv.V * stride + 2 * uv.U + dx - 1] = leftColor;

						if (2 * uv.U + dx < stride)
							colors[uv.V * stride + 2 * uv.U + dx] = rightColor;
					}
					else
						colors[uv.V * stride + uv.U] = leftColor;
				}
			}
		}

		void UpdateShroudCell(PPos puv)
		{
			var color = 0;
			var rp = world.RenderPlayer;
			if (rp != null)
			{
				if (!rp.Shroud.IsExplored(puv))
					color = Color.Black.ToArgb();
				else if (!rp.Shroud.IsVisible(puv))
					color = Color.FromArgb(128, Color.Black).ToArgb();
			}

			var stride = radarSheet.Size.Width;
			unsafe
			{
				fixed (byte* colorBytes = &radarData[0])
				{
					var colors = (int*)colorBytes;
					foreach (var uv in world.Map.Unproject(puv))
					{
						if (isRectangularIsometric)
						{
							// Odd rows are shifted right by 1px
							var dx = uv.V & 1;
							if (uv.U + dx > 0)
								colors[uv.V * stride + 2 * uv.U + dx - 1 + previewWidth] = color;

							if (2 * uv.U + dx < stride)
								colors[uv.V * stride + 2 * uv.U + dx + previewWidth] = color;
						}
						else
							colors[uv.V * stride + uv.U + previewWidth] = color;
					}
				}
			}
		}

		void MarkShroudDirty(IEnumerable<PPos> projectedCellsChanged)
		{
			// PERF: Many cells in the shroud change every tick. We only track the changes here and defer the real work
			// we need to do until we render. This allows us to avoid wasted work.
			dirtyShroudCells.UnionWith(projectedCellsChanged);
		}

		public override string GetCursor(int2 pos)
		{
			if (world == null || !hasRadar)
				return null;

			var cell = MinimapPixelToCell(pos);
			var worldPixel = worldRenderer.ScreenPxPosition(world.Map.CenterOfCell(cell));
			var location = worldRenderer.Viewport.WorldToViewPx(worldPixel);

			var mi = new MouseInput
			{
				Location = location,
				Button = Game.Settings.Game.MouseButtonPreference.Action,
				Modifiers = Game.GetModifierKeys()
			};

			var cursor = world.OrderGenerator.GetCursor(world, cell, worldPixel, mi);
			if (cursor == null)
				return "default";

			return Game.ModData.CursorProvider.HasCursorSequence(cursor + "-minimap") ? cursor + "-minimap" : cursor;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (!mapRect.Contains(mi.Location))
				return false;

			if (!hasRadar)
				return true;

			var cell = MinimapPixelToCell(mi.Location);
			var pos = world.Map.CenterOfCell(cell);
			if ((mi.Event == MouseInputEvent.Down || mi.Event == MouseInputEvent.Move)
				&& mi.Button == Game.Settings.Game.MouseButtonPreference.Cancel)
			{
				worldRenderer.Viewport.Center(pos);
			}

			if (mi.Event == MouseInputEvent.Down && mi.Button == Game.Settings.Game.MouseButtonPreference.Action)
			{
				// fake a mousedown/mouseup here
				var location = worldRenderer.Viewport.WorldToViewPx(worldRenderer.ScreenPxPosition(pos));
				var fakemi = new MouseInput
				{
					Event = MouseInputEvent.Down,
					Button = Game.Settings.Game.MouseButtonPreference.Action,
					Modifiers = mi.Modifiers,
					Location = location
				};

				if (WorldInteractionController != null)
				{
					var controller = Ui.Root.Get<WorldInteractionControllerWidget>(WorldInteractionController);
					controller.HandleMouseInput(fakemi);
					fakemi.Event = MouseInputEvent.Up;
					controller.HandleMouseInput(fakemi);
				}
			}

			return true;
		}

		public override void Draw()
		{
			if (world == null)
				return;

			if (renderShroud != null)
			{
				foreach (var cell in dirtyShroudCells)
					UpdateShroudCell(cell);
				dirtyShroudCells.Clear();
			}

			radarSheet.CommitBufferedData();

			var o = new float2(mapRect.Location.X, mapRect.Location.Y + world.Map.Bounds.Height * previewScale * (1 - radarMinimapHeight) / 2);
			var s = new float2(mapRect.Size.Width, mapRect.Size.Height * radarMinimapHeight);

			var rsr = Game.Renderer.RgbaSpriteRenderer;
			rsr.DrawSprite(terrainSprite, o, s);
			rsr.DrawSprite(actorSprite, o, s);

			if (renderShroud != null)
				rsr.DrawSprite(shroudSprite, o, s);

			// Draw viewport rect
			if (hasRadar)
			{
				var tl = CellToMinimapPixel(world.Map.CellContaining(worldRenderer.ProjectedPosition(worldRenderer.Viewport.TopLeft)));
				var br = CellToMinimapPixel(world.Map.CellContaining(worldRenderer.ProjectedPosition(worldRenderer.Viewport.BottomRight)));

				Game.Renderer.EnableScissor(mapRect);
				DrawRadarPings();
				Game.Renderer.RgbaColorRenderer.DrawRect(tl, br, 1, Color.White);
				Game.Renderer.DisableScissor();
			}
		}

		void DrawRadarPings()
		{
			if (radarPings == null)
				return;

			foreach (var radarPing in radarPings.Pings.Where(e => e.IsVisible()))
			{
				var c = radarPing.Color;
				var pingCell = world.Map.CellContaining(radarPing.Position);
				var points = radarPing.Points(CellToMinimapPixel(pingCell)).ToArray();
				Game.Renderer.RgbaColorRenderer.DrawPolygon(points, 2, c);
			}
		}

		public override void Tick()
		{
			// Enable/Disable the radar
			var enabled = IsEnabled();
			if (enabled != cachedEnabled)
				Game.Sound.Play(enabled ? RadarOnlineSound : RadarOfflineSound);
			cachedEnabled = enabled;

			if (enabled)
			{
				var rp = world.RenderPlayer;
				var newRenderShroud = rp != null ? rp.Shroud : null;
				if (newRenderShroud != renderShroud)
				{
					if (renderShroud != null)
						renderShroud.CellsChanged -= MarkShroudDirty;

					if (newRenderShroud != null)
					{
						// Redraw the full shroud sprite
						MarkShroudDirty(world.Map.AllCells.MapCoords.Select(uv => (PPos)uv));

						// Update the notification binding
						newRenderShroud.CellsChanged += MarkShroudDirty;
					}

					renderShroud = newRenderShroud;
				}

				// The actor layer is updated every tick
				var stride = radarSheet.Size.Width;
				Array.Clear(radarData, 4 * actorSprite.Bounds.Top * stride, 4 * actorSprite.Bounds.Height * stride);

				unsafe
				{
					fixed (byte* colorBytes = &radarData[0])
					{
						var colors = (int*)colorBytes;

						foreach (var t in world.ActorsWithTrait<IRadarSignature>())
						{
							if (!t.Actor.IsInWorld || world.FogObscures(t.Actor))
								continue;

							foreach (var cell in t.Trait.RadarSignatureCells(t.Actor))
							{
								if (!world.Map.Contains(cell.First))
									continue;

								var uv = cell.First.ToMPos(world.Map.Grid.Type);
								var color = cell.Second.ToArgb();
								if (isRectangularIsometric)
								{
									// Odd rows are shifted right by 1px
									var dx = uv.V & 1;
									if (uv.U + dx > 0)
										colors[(uv.V + previewHeight) * stride + 2 * uv.U + dx - 1] = color;

									if (2 * uv.U + dx < stride)
										colors[(uv.V + previewHeight) * stride + 2 * uv.U + dx] = color;
								}
								else
									colors[(uv.V + previewHeight) * stride + uv.U] = color;
							}
						}
					}
				}
			}

			var targetFrame = enabled ? AnimationLength : 0;
			hasRadar = enabled && frame == AnimationLength;
			if (frame == targetFrame)
				return;

			frame += enabled ? 1 : -1;
			radarMinimapHeight = float2.Lerp(0, 1, (float)frame / AnimationLength);

			Animating(frame * 1f / AnimationLength);

			// Update map rectangle for event handling
			var ro = RenderOrigin;
			mapRect = new Rectangle(previewOrigin.X + ro.X, previewOrigin.Y + ro.Y, mapRect.Width, mapRect.Height);

			// Animation is complete
			if (frame == targetFrame)
			{
				if (enabled)
					AfterOpen();
				else
					AfterClose();
			}
		}

		int2 CellToMinimapPixel(CPos p)
		{
			var uv = p.ToMPos(world.Map);
			var dx = (int)(previewScale * cellWidth * (uv.U - world.Map.Bounds.Left));
			var dy = (int)(previewScale * (uv.V - world.Map.Bounds.Top));

			// Odd rows are shifted right by 1px
			if (isRectangularIsometric && (uv.V & 1) == 1)
				dx += 1;

			return new int2(mapRect.X + dx, mapRect.Y + dy);
		}

		CPos MinimapPixelToCell(int2 p)
		{
			var u = (int)((p.X - mapRect.X) / (previewScale * cellWidth)) + world.Map.Bounds.Left;
			var v = (int)((p.Y - mapRect.Y) / previewScale) + world.Map.Bounds.Top;
			return new MPos(u, v).ToCPos(world.Map);
		}

		public override void Removed()
		{
			base.Removed();
			world.Map.Tiles.CellEntryChanged -= UpdateTerrainCell;
			world.Map.CustomTerrain.CellEntryChanged -= UpdateTerrainCell;
			Dispose();
		}

		public void Dispose()
		{
			radarSheet.Dispose();
		}
	}
}
