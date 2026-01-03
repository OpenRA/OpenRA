#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
		public readonly uint ColorFog = Color.FromArgb(128, Color.Black).ToArgb();
		public readonly uint ColorShroud = Color.Black.ToArgb();

		public string WorldInteractionController = null;
		public int AnimationLength = 5;
		public string RadarOnlineSound = null;
		public string RadarOfflineSound = null;
		public string SoundUp;
		public string SoundDown;
		public Func<bool> IsEnabled = () => true;
		public Action AfterOpen = () => { };
		public Action AfterClose = () => { };
		public Action<float> Animating = _ => { };

		readonly ModData modData;
		readonly World world;
		readonly WorldRenderer worldRenderer;
		readonly RadarPings radarPings;
		readonly IRadarTerrainLayer[] radarTerrainLayers;
		readonly bool isRectangularIsometric;
		readonly int cellWidth;
		readonly int previewWidth;
		readonly int previewHeight;
		readonly string worldSelectCursor = ChromeMetrics.Get<string>("WorldSelectCursor");
		readonly string worldDefaultCursor = ChromeMetrics.Get<string>("WorldDefaultCursor");
		readonly GameSettings gameSettings;

		float radarMinimapHeight;
		int frame;
		bool hasRadar;
		bool cachedEnabled;
		bool isMinimapMoving;
		MouseButton minimapMoveButton;

		float previewScale = 0;
		int2 previewOrigin = int2.Zero;
		Rectangle mapRect = Rectangle.Empty;

		Sheet radarSheet;
		byte[] radarData;

		Sprite terrainSprite;
		Sprite actorSprite;
		Sprite shroudSprite;
		Sprite shroudBorderSprite;
		Shroud shroud;
		PlayerRadarTerrain playerRadarTerrain;
		Player currentPlayer;

		public bool ShowShroudBorders { get; set; }
		public bool ShowFogBorders { get; set; }
		public bool HideExploredAreasOnMinimap { get; set; }

		[ObjectCreator.UseCtor]
		public RadarWidget(ModData modData, World world, WorldRenderer worldRenderer)
		{
			this.modData = modData;
			this.world = world;
			this.worldRenderer = worldRenderer;
			gameSettings = Game.Settings.Game;

			radarPings = world.WorldActor.TraitOrDefault<RadarPings>();
			radarTerrainLayers = world.WorldActor.TraitsImplementing<IRadarTerrainLayer>().ToArray();
			isRectangularIsometric = world.Map.Grid.Type == MapGridType.RectangularIsometric;
			cellWidth = isRectangularIsometric ? 2 : 1;
			previewWidth = world.Map.MapSize.Width;
			previewHeight = world.Map.MapSize.Height;
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

			var newShroud = player?.Shroud;

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

			var newPlayerRadarTerrain = currentPlayer?.PlayerActor.TraitOrDefault<PlayerRadarTerrain>();

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
			shroudBorderSprite = new Sprite(radarSheet, new Rectangle(b.Location + new Size(previewWidth, previewHeight), b.Size), TextureChannel.RGBA);
		}

		void UpdateTerrainColor(MPos uv)
		{
			var (leftColor, rightColor) = playerRadarTerrain != null && playerRadarTerrain.IsInitialized ?
				playerRadarTerrain[uv] : PlayerRadarTerrain.GetColor(world.Map, radarTerrainLayers, uv);

			var stride = radarSheet.Size.Width;

			unsafe
			{
				fixed (byte* colorBytes = &radarData[0])
				{
					var colors = (uint*)colorBytes;
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
			var color = 0u;
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
					var colors = (uint*)colorBytes;
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

void UpdateBordersLayer()
{
	var stride = radarSheet.Size.Width;

	// Get players to show borders for based on dropdown selection
	// "Everyone" player (All Players option) or null (Disable Shroud) means show all playable players
	// A specific player means show only that player's borders
	Player[] playersToShow;
	var isAllPlayersMode = currentPlayer == null || currentPlayer.InternalName == "Everyone";
	if (isAllPlayersMode)
		playersToShow = world.Players.Where(p => !p.NonCombatant && p.Playable).ToArray();
	else
		playersToShow = new[] { currentPlayer };

	var neighborOffsets = new[] { new PPos(1, 0), new PPos(-1, 0), new PPos(0, 1), new PPos(0, -1) };

	unsafe
	{
		fixed (byte* colorBytes = &radarData[0])
		{
			var colors = (uint*)colorBytes;

			// Clear only the border layer quadrant (bottom-right), not the actor layer (bottom-left)
			// The border sprite starts at (previewWidth, previewHeight) in the texture
			var bounds = shroudBorderSprite.Bounds;
			for (var y = bounds.Top; y < bounds.Bottom; y++)
			{
				for (var x = bounds.Left; x < bounds.Right; x++)
				{
					colors[y * stride + x] = 0;
				}
			}

			foreach (var player in playersToShow)
			{
					var playerShroud = player.Shroud;
					var playerColor = player.Color.ToArgb();

					foreach (var puv in world.Map.ProjectedCells)
					{
						var isExplored = playerShroud.IsExplored(puv);
						var isVisible = playerShroud.IsVisible(puv);

						var isShroudBorder = false;
						var isFogBorder = false;

						// Check each neighbor for border detection
						foreach (var offset in neighborOffsets)
						{
							var neighbor = new PPos(puv.U + offset.U, puv.V + offset.V);
							if (!world.Map.Contains(neighbor))
								continue;

							// Shroud border: explored cell adjacent to unexplored cell
							// (same logic as gamefield: draw border on the explored side)
							if (ShowShroudBorders && isExplored && !playerShroud.IsExplored(neighbor))
							{
								isShroudBorder = true;
								break;
							}

							// Fog border: visible cell adjacent to explored-but-not-visible cell
							// (same logic as gamefield: draw border on the visible side)
							if (ShowFogBorders && isVisible)
							{
								var neighborExplored = playerShroud.IsExplored(neighbor);
								var neighborVisible = playerShroud.IsVisible(neighbor);
								if (neighborExplored && !neighborVisible)
								{
									isFogBorder = true;
									break;
								}
							}
						}

						if (!isShroudBorder && !isFogBorder)
							continue;

						// Draw the border pixel with the player's color
						foreach (var iuv in world.Map.Unproject(puv))
						{
							if (isRectangularIsometric)
							{
								// Odd rows are shifted right by 1px
								var dx = iuv.V & 1;
								if (iuv.U + dx > 0)
									colors[(iuv.V + previewHeight) * stride + 2 * iuv.U + dx - 1 + previewWidth] = playerColor;

								if (2 * iuv.U + dx < stride)
									colors[(iuv.V + previewHeight) * stride + 2 * iuv.U + dx + previewWidth] = playerColor;
							}
							else
								colors[(iuv.V + previewHeight) * stride + iuv.U + previewWidth] = playerColor;
						}
					}
				}
			}
		}
	}

		public override string GetCursor(int2 pos)
		{
			if (world == null || !hasRadar)
				return null;

			var worldPos = MinimapPixelToWorldCoords(pos).ToInt2();
			var wpos = new WPos(worldPos.X, worldPos.Y, 0);
			var cell = world.Map.CellContaining(wpos);

			var worldPixel = worldRenderer.ScreenPxPosition(wpos);
			var location = worldRenderer.Viewport.WorldToViewPx(worldPixel);
			var mi = new MouseInput
			{
				Location = location,
				Button = world.OrderGenerator.ActionButton,
				Modifiers = Game.GetModifierKeys()
			};

			var cursor = world.OrderGenerator.GetCursor(world, cell, worldPixel, mi);

			// We can't select through the minimap in Mouse Control Types other than Classic,
			// as they move the minimap on left click, so don't show the selection cursor for them
			if (cursor == null || (gameSettings.MouseControlStyle != MouseControlStyle.Classic && cursor == worldSelectCursor))
				cursor = worldDefaultCursor;

			return modData.Cursors.ContainsKey(cursor + "-minimap") ? cursor + "-minimap" : cursor;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (!mapRect.Contains(mi.Location))
				return false;

			if (!hasRadar)
				return true;

			var worldCoords = MinimapPixelToWorldCoords(mi.Location);
			if ((mi.Event == MouseInputEvent.Down && mi.Button != world.OrderGenerator.ActionButton) ||
				(mi.Event == MouseInputEvent.Move && isMinimapMoving && mi.Button == minimapMoveButton))
			{
				worldRenderer.Viewport.Center(worldCoords);
				isMinimapMoving = true;
				minimapMoveButton = mi.Button;
			}
			else if (mi.Event == MouseInputEvent.Down && WorldInteractionController != null)
			{
				var worldPos = worldCoords.ToInt2();
				var wpos = new WPos(worldPos.X, worldPos.Y, 0);

				// fake a mousedown/mouseup here
				var location = worldRenderer.Viewport.WorldToViewPx(worldRenderer.ScreenPxPosition(wpos));
				var fakemi = new MouseInput
				{
					Event = MouseInputEvent.Down,
					Button = mi.Button,
					Modifiers = mi.Modifiers,
					Location = location,
				};

				var controller = Ui.Root.Get<WorldInteractionControllerWidget>(WorldInteractionController);
				controller.HandleMouseInput(fakemi);
				fakemi.Event = MouseInputEvent.Up;
				controller.HandleMouseInput(fakemi);
			}
			else if (mi.Event == MouseInputEvent.Up && mi.Button == minimapMoveButton)
			{
				isMinimapMoving = false;
				minimapMoveButton = MouseButton.None;
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

		var bordersEnabled = ShowShroudBorders || ShowFogBorders;

		if (bordersEnabled)
		{
			// When borders are enabled, draw in this order: terrain -> shroud -> actors -> borders
			// This way the shroud masks the terrain, but actors are drawn on top and remain visible
			WidgetUtils.DrawSprite(terrainSprite, o, s);

			if (shroud != null)
				WidgetUtils.DrawSprite(shroudSprite, o, s);

			WidgetUtils.DrawSprite(actorSprite, o, s);
			WidgetUtils.DrawSprite(shroudBorderSprite, o, s);
		}
		else
		{
			// Normal drawing order: terrain -> actors -> shroud
			WidgetUtils.DrawSprite(terrainSprite, o, s);
			WidgetUtils.DrawSprite(actorSprite, o, s);

			if (shroud != null)
				WidgetUtils.DrawSprite(shroudSprite, o, s);
		}

		// Mask to show only currently visible areas (hide already explored areas)
		if (HideExploredAreasOnMinimap)
			DrawVisibleAreaMask(o, s);

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

void DrawVisibleAreaMask(float2 origin, float2 size)
{
	// Get players to show based on dropdown selection
	// Draw black overlay over all non-visible areas (both unexplored and explored but not visible)	
	Player[] playersToShow;
	var isAllPlayersMode = currentPlayer == null || currentPlayer.InternalName == "Everyone";
	if (isAllPlayersMode)
		playersToShow = world.Players.Where(p => !p.NonCombatant && p.Playable).ToArray();
	else
		playersToShow = new[] { currentPlayer };

	if (playersToShow.Length == 0)
		return;

	var stride = radarSheet.Size.Width;
	var blackColor = Color.Black.ToArgb();
	var transparentColor = Color.Transparent.ToArgb();
	var neighborOffsets = new[] { new PPos(1, 0), new PPos(-1, 0), new PPos(0, 1), new PPos(0, -1) };

	unsafe
	{
		fixed (byte* colorBytes = &radarData[0])
		{
			var colors = (uint*)colorBytes;

			// Initialize only the border layer quadrant (bottom-right) to black
			// This avoids clearing the actor layer (bottom-left)
			var bounds = shroudBorderSprite.Bounds;
			for (var y = bounds.Top; y < bounds.Bottom; y++)
			{
				for (var x = bounds.Left; x < bounds.Right; x++)
				{
					colors[y * stride + x] = blackColor;
				}
			}

				// Make visible cells transparent
				foreach (var puv in world.Map.ProjectedCells)
				{
					var isVisibleByAny = false;

					// Check if this cell is visible by any of the selected players
					foreach (var player in playersToShow)
					{
						if (player.Shroud.IsVisible(puv))
						{
							isVisibleByAny = true;
							break;
						}
					}

					// If visible by any selected player, make it transparent
					if (isVisibleByAny)
					{
						foreach (var iuv in world.Map.Unproject(puv))
						{
							if (isRectangularIsometric)
							{
								// Odd rows are shifted right by 1px
								var dx = iuv.V & 1;
								if (iuv.U + dx > 0)
									colors[(iuv.V + previewHeight) * stride + 2 * iuv.U + dx - 1 + previewWidth] = transparentColor;

								if (2 * iuv.U + dx < stride)
									colors[(iuv.V + previewHeight) * stride + 2 * iuv.U + dx + previewWidth] = transparentColor;
							}
							else
								colors[(iuv.V + previewHeight) * stride + iuv.U + previewWidth] = transparentColor;
						}
					}
				}

				// Redraw colored borders on top of the fog mask when border options are enabled
				// This is necessary because the fog mask overwrites the shroudBorderSprite layer
				if (ShowShroudBorders || ShowFogBorders)
				{
					foreach (var player in playersToShow)
					{
							var playerShroud = player.Shroud;
							var playerColor = player.Color.ToArgb();

							foreach (var puv in world.Map.ProjectedCells)
							{
								var isExplored = playerShroud.IsExplored(puv);
								var isVisible = playerShroud.IsVisible(puv);

								var isShroudBorder = false;
								var isFogBorder = false;

								// Check each neighbor for border detection
								foreach (var offset in neighborOffsets)
								{
									var neighbor = new PPos(puv.U + offset.U, puv.V + offset.V);
									if (!world.Map.Contains(neighbor))
										continue;

									// Shroud border: explored cell adjacent to unexplored cell
									// (same logic as gamefield: draw border on the explored side)
									if (ShowShroudBorders && isExplored && !playerShroud.IsExplored(neighbor))
									{
										isShroudBorder = true;
										break;
									}

									// Fog border: visible cell adjacent to explored-but-not-visible cell
									// (same logic as gamefield: draw border on the visible side)
									if (ShowFogBorders && isVisible)
									{
										var neighborExplored = playerShroud.IsExplored(neighbor);
										var neighborVisible = playerShroud.IsVisible(neighbor);
										if (neighborExplored && !neighborVisible)
										{
											isFogBorder = true;
											break;
										}
									}
								}

								if (!isShroudBorder && !isFogBorder)
									continue;

								// Draw the border pixel with the player's color
								foreach (var iuv in world.Map.Unproject(puv))
								{
									if (isRectangularIsometric)
									{
										// Odd rows are shifted right by 1px
										var dx = iuv.V & 1;
										if (iuv.U + dx > 0)
											colors[(iuv.V + previewHeight) * stride + 2 * iuv.U + dx - 1 + previewWidth] = playerColor;

										if (2 * iuv.U + dx < stride)
											colors[(iuv.V + previewHeight) * stride + 2 * iuv.U + dx + previewWidth] = playerColor;
									}
									else
										colors[(iuv.V + previewHeight) * stride + iuv.U + previewWidth] = playerColor;
								}
							}
						}
					}
				}
			}

			// Draw the mask sprite
			WidgetUtils.DrawSprite(shroudBorderSprite, origin, size);
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
			var cells = new List<(CPos Cell, Color Color)>();

			unsafe
			{
				fixed (byte* colorBytes = &radarData[0])
				{
					var colors = (uint*)colorBytes;

					// Clear only the actor layer quadrant (bottom-left), not the border layer (bottom-right)
					var actorBounds = actorSprite.Bounds;
					for (var y = actorBounds.Top; y < actorBounds.Bottom; y++)
					{
						for (var x = actorBounds.Left; x < actorBounds.Right; x++)
						{
							colors[y * stride + x] = 0;
						}
					}

				// When colored borders are enabled, show player actors regardless of fog
				// but keep fog check for neutral actors (resources, terrain elements)
				var bordersEnabled = ShowShroudBorders || ShowFogBorders;

				// "Everyone" player (All Players option) or null (Disable Shroud) means show all players
				var isAllPlayersMode = currentPlayer == null || currentPlayer.InternalName == "Everyone";

				foreach (var t in world.ActorsWithTrait<IRadarSignature>())
				{
					if (!t.Actor.IsInWorld)
						continue;

					// Only ignore fog for actors belonging to playable players (units, buildings)
					// Keep fog check for neutral actors (ore, gems, terrain elements)
					var isPlayerActor = t.Actor.Owner.Playable && !t.Actor.Owner.NonCombatant;

					// When borders are enabled and a specific player is selected, only show that player's actors
					// When "All Players" or "Disable Shroud" is selected, show all player actors
					var isSelectedPlayerActor = isAllPlayersMode || t.Actor.Owner == currentPlayer;
					var ignoreFogForThisActor = bordersEnabled && isPlayerActor && isSelectedPlayerActor;

					if (!ignoreFogForThisActor && world.FogObscures(t.Actor))
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

			// Update borders layer when enabled via Map Discovery checkboxes
			if (ShowShroudBorders || ShowFogBorders)
				UpdateBordersLayer();
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
				dx++;

			return new int2(mapRect.X + dx, mapRect.Y + dy);
		}

		float2 MinimapPixelToWorldCoords(int2 pixel)
		{
			var u = (pixel.X - mapRect.X) / (previewScale * cellWidth) + world.Map.Bounds.Left;
			var v = (pixel.Y - mapRect.Y) / previewScale + world.Map.Bounds.Top;

			if (world.Map.Grid.Type == MapGridType.Rectangular)
			{
				return new float2(1024 * u + 512, 1024 * v + 512);
			}
			else
			{
				var y = v / 2.0f - u;
				var x = v - y;
				return new float2(724 * (x - y), 724 * (x + y));
			}
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
