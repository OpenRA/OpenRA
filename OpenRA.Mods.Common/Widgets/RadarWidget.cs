#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class RadarWidget : Widget, IDisposable
	{
		public readonly int ColorFog = Color.FromArgb(128, Color.Black).ToArgb();
		public readonly int ColorShroud = Color.Black.ToArgb();

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
		readonly IRadarTerrainLayer[] radarTerrainLayers;
		readonly bool isRectangularIsometric;
		readonly int cellWidth;
		readonly int previewWidth;
		readonly int previewHeight;
		readonly string worldDefaultCursor = ChromeMetrics.Get<string>("WorldDefaultCursor");

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
		Shroud shroud;
		PlayerRadarTerrain playerRadarTerrain;
		Player currentPlayer;

		public string SoundUp { get; private set; }
		public string SoundDown { get; private set; }

		[ObjectCreator.UseCtor]
		public RadarWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;

			radarPings = world.WorldActor.TraitOrDefault<RadarPings>();
			radarTerrainLayers = world.WorldActor.TraitsImplementing<IRadarTerrainLayer>().ToArray();
			isRectangularIsometric = world.Map.Grid.Type == MapGridType.RectangularIsometric;
			cellWidth = isRectangularIsometric ? 2 : 1;
			previewWidth = world.Map.MapSize.X;
			previewHeight = world.Map.MapSize.Y;
			if (isRectangularIsometric)
				previewWidth = 2 * previewWidth - 1;
		}

		void CellTerrainColorChanged(MPos uv)
		{
			UpdateTerrainColor(uv);
		}

		void CellTerrainColorChanged(CPos cell)
		{
			UpdateTerrainColor(cell.ToMPos(world.Map));
		}

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);

			// The four layers are stored in a 2x2 grid within a single texture
			radarSheet = new Sheet(SheetType.BGRA, new Size(2 * previewWidth, 2 * previewHeight).NextPowerOf2());
			radarSheet.CreateBuffer();
			radarData = radarSheet.GetData();

			MapBoundsChanged();

			var player = world.Type == WorldType.Regular ? world.LocalPlayer ?? world.RenderPlayer : null;
			SetPlayer(player, true);

			if (player == null)
			{
				// Set initial terrain data
				foreach (var uv in world.Map.AllCells.MapCoords)
					UpdateTerrainColor(uv);
			}

			world.RenderPlayerChanged += WorldOnRenderPlayerChanged;
		}

		void WorldOnRenderPlayerChanged(Player player)
		{
			SetPlayer(player);

			// Set initial terrain data
			foreach (var uv in world.Map.AllCells.MapCoords)
				UpdateTerrainColor(uv);
		}

		void SetPlayer(Player player, bool forceUpdate = false)
		{
			currentPlayer = player;

			var newShroud = player != null ? player.Shroud : null;

			if (newShroud != shroud)
			{
				if (shroud != null)
					shroud.OnShroudChanged -= UpdateShroudCell;

				if (newShroud != null)
				{
					newShroud.OnShroudChanged += UpdateShroudCell;
					foreach (var puv in world.Map.ProjectedCells)
						UpdateShroudCell(puv);
				}

				shroud = newShroud;
			}

			var newPlayerRadarTerrain =
				currentPlayer != null ? currentPlayer.PlayerActor.TraitOrDefault<PlayerRadarTerrain>() : null;

			if (forceUpdate || newPlayerRadarTerrain != playerRadarTerrain)
			{
				if (playerRadarTerrain != null)
					playerRadarTerrain.CellTerrainColorChanged -= CellTerrainColorChanged;
				else
				{
					world.Map.Tiles.CellEntryChanged -= CellTerrainColorChanged;
					foreach (var rtl in radarTerrainLayers)
						rtl.CellEntryChanged -= CellTerrainColorChanged;
				}

				if (newPlayerRadarTerrain != null)
					newPlayerRadarTerrain.CellTerrainColorChanged += CellTerrainColorChanged;
				else
				{
					world.Map.Tiles.CellEntryChanged += CellTerrainColorChanged;
					foreach (var rtl in radarTerrainLayers)
						rtl.CellEntryChanged += CellTerrainColorChanged;
				}

				playerRadarTerrain = newPlayerRadarTerrain;
			}
		}

		void MapBoundsChanged()
		{
			var map = world.Map;

			// The minimap is drawn in cell space, so we need to
			// unproject the bounds to find the extent of the map.
			// TODO: This attempt to find the map bounds accounting for projected cell heights is bogus.
			// When a map with height is involved, the bounds may not be optimal, this needs fixing.
			var projectedLeft = map.Bounds.Left;
			var projectedRight = map.Bounds.Right;
			var projectedTop = map.Bounds.Top;
			var projectedBottom = map.Bounds.Bottom;
			var top = int.MaxValue;
			var bottom = int.MinValue;
			var left = projectedLeft * cellWidth;
			var right = projectedRight * cellWidth;

			for (var x = projectedLeft; x < projectedRight; x++)
			{
				// Unprojects check can fail and return an empty list.
				// This happens when the map tile is outside the map projected space,
				// e.g. if a tile on the bottom edge has a height > 0.
				// Guard against this by using the map bounds as a fallback.
				var allTop = map.Unproject(new PPos(x, projectedTop));
				var allBottom = map.Unproject(new PPos(x, projectedBottom));

				if (allTop.Count > 0)
					top = Math.Min(top, allTop.MinBy(uv => uv.V).V);
				else
					top = map.Bounds.Top;

				if (allBottom.Count > 0)
					bottom = Math.Max(bottom, allBottom.MaxBy(uv => uv.V).V);
				else
					bottom = map.Bounds.Bottom;
			}

			var b = Rectangle.FromLTRB(left, top, right, bottom);
			var rb = RenderBounds;
			previewScale = Math.Min(rb.Width * 1f / b.Width, rb.Height * 1f / b.Height);
			previewOrigin = new int2((int)((rb.Width - previewScale * b.Width) / 2), (int)((rb.Height - previewScale * b.Height) / 2));
			mapRect = new Rectangle(previewOrigin.X, previewOrigin.Y, (int)(previewScale * b.Width), (int)(previewScale * b.Height));

			terrainSprite = new Sprite(radarSheet, b, TextureChannel.RGBA);
			shroudSprite = new Sprite(radarSheet, new Rectangle(b.Location + new Size(previewWidth, 0), b.Size), TextureChannel.RGBA);
			actorSprite = new Sprite(radarSheet, new Rectangle(b.Location + new Size(0, previewHeight), b.Size), TextureChannel.RGBA);
		}

		void UpdateTerrainColor(MPos uv)
		{
			var colorPair = playerRadarTerrain != null && playerRadarTerrain.IsInitialized ?
				playerRadarTerrain[uv] : PlayerRadarTerrain.GetColor(world.Map, radarTerrainLayers, uv);
			var leftColor = colorPair.Left;
			var rightColor = colorPair.Right;

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
			var cv = currentPlayer.Shroud.GetVisibility(puv);
			if (!cv.HasFlag(Shroud.CellVisibility.Explored))
				color = ColorShroud;
			else if (!cv.HasFlag(Shroud.CellVisibility.Visible))
				color = ColorFog;

			var stride = radarSheet.Size.Width;
			unsafe
			{
				fixed (byte* colorBytes = &radarData[0])
				{
					var colors = (int*)colorBytes;
					foreach (var iuv in world.Map.Unproject(puv))
					{
						if (isRectangularIsometric)
						{
							// Odd rows are shifted right by 1px
							var dx = iuv.V & 1;
							if (iuv.U + dx > 0)
								colors[iuv.V * stride + 2 * iuv.U + dx - 1 + previewWidth] = color;

							if (2 * iuv.U + dx < stride)
								colors[iuv.V * stride + 2 * iuv.U + dx + previewWidth] = color;
						}
						else
							colors[iuv.V * stride + iuv.U + previewWidth] = color;
					}
				}
			}
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
				return worldDefaultCursor;

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

			radarSheet.CommitBufferedData();

			var o = new float2(mapRect.Location.X, mapRect.Location.Y + world.Map.Bounds.Height * previewScale * (1 - radarMinimapHeight) / 2);
			var s = new float2(mapRect.Size.Width, mapRect.Size.Height * radarMinimapHeight);

			WidgetUtils.DrawSprite(terrainSprite, o, s);
			WidgetUtils.DrawSprite(actorSprite, o, s);

			if (shroud != null)
				WidgetUtils.DrawSprite(shroudSprite, o, s);

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
				Game.Sound.Play(SoundType.UI, enabled ? RadarOnlineSound : RadarOfflineSound);
			cachedEnabled = enabled;

			if (enabled)
			{
				// The actor layer is updated every tick
				var stride = radarSheet.Size.Width;
				Array.Clear(radarData, 4 * actorSprite.Bounds.Top * stride, 4 * actorSprite.Bounds.Height * stride);

				var cells = new List<(CPos Cell, Color Color)>();

				unsafe
				{
					fixed (byte* colorBytes = &radarData[0])
					{
						var colors = (int*)colorBytes;

						foreach (var t in world.ActorsWithTrait<IRadarSignature>())
						{
							if (!t.Actor.IsInWorld || world.FogObscures(t.Actor))
								continue;

							cells.Clear();
							t.Trait.PopulateRadarSignatureCells(t.Actor, cells);
							foreach (var cell in cells)
							{
								if (!world.Map.Contains(cell.Cell))
									continue;

								var uv = cell.Cell.ToMPos(world.Map.Grid.Type);
								var color = cell.Color.ToArgb();
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

			if (playerRadarTerrain != null)
				playerRadarTerrain.CellTerrainColorChanged -= CellTerrainColorChanged;

			world.RenderPlayerChanged -= WorldOnRenderPlayerChanged;
			Dispose();
		}

		public void Dispose()
		{
			radarSheet.Dispose();
		}
	}
}
