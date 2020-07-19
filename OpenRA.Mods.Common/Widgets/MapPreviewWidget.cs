#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class SpawnOccupant
	{
		public readonly Color Color;
		public readonly string PlayerName;
		public readonly int Team;
		public readonly string Faction;
		public readonly int SpawnPoint;

		public SpawnOccupant(Session.Client client)
		{
			Color = client.Color;
			PlayerName = client.Name;
			Team = client.Team;
			Faction = client.Faction;
			SpawnPoint = client.SpawnPoint;
		}

		public SpawnOccupant(GameInformation.Player player)
		{
			Color = player.Color;
			PlayerName = player.Name;
			Team = player.Team;
			Faction = player.FactionId;
			SpawnPoint = player.SpawnPoint;
		}

		public SpawnOccupant(GameClient player, bool suppressFaction)
		{
			Color = player.Color;
			PlayerName = player.Name;
			Team = player.Team;
			Faction = !suppressFaction ? player.Faction : null;
			SpawnPoint = player.SpawnPoint;
		}
	}

	public class MapPreviewWidget : Widget
	{
		static readonly int[] NoDisabledSpawnPoints = Array.Empty<int>();

		public readonly bool IgnoreMouseInput = false;
		public readonly bool ShowSpawnPoints = true;

		public readonly string TooltipContainer;
		public readonly string TooltipTemplate = "SPAWN_TOOLTIP";
		readonly Lazy<TooltipContainerWidget> tooltipContainer;

		readonly Sprite spawnClaimed, spawnUnclaimed, spawnDisabled;
		readonly SpriteFont spawnFont;
		readonly Color spawnColor, spawnContrastColor;
		readonly int2 spawnLabelOffset;

		public Func<MapPreview> Preview = () => null;
		public Func<Dictionary<int, SpawnOccupant>> SpawnOccupants = () => new Dictionary<int, SpawnOccupant>();
		public Func<IEnumerable<int>> DisabledSpawnPoints = () => NoDisabledSpawnPoints;
		public Action<MouseInput> OnMouseDown = _ => { };
		public int TooltipSpawnIndex = -1;
		public bool ShowUnoccupiedSpawnpoints = true;

		Rectangle mapRect;
		float previewScale = 0;
		Sprite minimap;

		public MapPreviewWidget()
		{
			tooltipContainer = Exts.Lazy(() => Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			spawnClaimed = ChromeProvider.GetImage("lobby-bits", "spawn-claimed");
			spawnUnclaimed = ChromeProvider.GetImage("lobby-bits", "spawn-unclaimed");
			spawnDisabled = ChromeProvider.GetImage("lobby-bits", "spawn-disabled") ?? spawnUnclaimed;
			spawnFont = Game.Renderer.Fonts[ChromeMetrics.Get<string>("SpawnFont")];
			spawnColor = ChromeMetrics.Get<Color>("SpawnColor");
			spawnContrastColor = ChromeMetrics.Get<Color>("SpawnContrastColor");
			spawnLabelOffset = ChromeMetrics.Get<int2>("SpawnLabelOffset");
		}

		protected MapPreviewWidget(MapPreviewWidget other)
			: base(other)
		{
			Preview = other.Preview;

			IgnoreMouseInput = other.IgnoreMouseInput;
			ShowSpawnPoints = other.ShowSpawnPoints;
			TooltipTemplate = other.TooltipTemplate;
			TooltipContainer = other.TooltipContainer;
			SpawnOccupants = other.SpawnOccupants;

			tooltipContainer = Exts.Lazy(() => Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));

			spawnClaimed = ChromeProvider.GetImage("lobby-bits", "spawn-claimed");
			spawnUnclaimed = ChromeProvider.GetImage("lobby-bits", "spawn-unclaimed");
			spawnFont = Game.Renderer.Fonts[ChromeMetrics.Get<string>("SpawnFont")];
			spawnColor = ChromeMetrics.Get<Color>("SpawnColor");
			spawnContrastColor = ChromeMetrics.Get<Color>("SpawnContrastColor");
			spawnLabelOffset = ChromeMetrics.Get<int2>("SpawnLabelOffset");
		}

		public override Widget Clone() { return new MapPreviewWidget(this); }

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (IgnoreMouseInput)
				return base.HandleMouseInput(mi);

			if (mi.Event != MouseInputEvent.Down)
				return false;

			OnMouseDown(mi);
			return true;
		}

		public override void MouseEntered()
		{
			if (TooltipContainer != null)
				tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs()
					{
						{ "preview", this },
						{ "showUnoccupiedSpawnpoints", ShowUnoccupiedSpawnpoints }
					});
		}

		public override void MouseExited()
		{
			if (TooltipContainer != null)
				tooltipContainer.Value.RemoveTooltip();
		}

		public int2 ConvertToPreview(CPos cell, MapGridType gridType)
		{
			var preview = Preview();
			var point = cell.ToMPos(gridType);
			var cellWidth = gridType == MapGridType.RectangularIsometric ? 2 : 1;
			var dx = (int)(previewScale * cellWidth * (point.U - preview.Bounds.Left));
			var dy = (int)(previewScale * (point.V - preview.Bounds.Top));

			// Odd rows are shifted right by 1px
			if ((point.V & 1) == 1)
				dx += 1;

			return new int2(mapRect.X + dx, mapRect.Y + dy);
		}

		public override void Draw()
		{
			var preview = Preview();
			if (preview == null)
				return;

			// Stash a copy of the minimap to ensure consistency
			// (it may be modified by another thread)
			minimap = preview.GetMinimap();
			if (minimap == null)
				return;

			// Update map rect
			previewScale = Math.Min(RenderBounds.Width / minimap.Size.X, RenderBounds.Height / minimap.Size.Y);
			var w = (int)(previewScale * minimap.Size.X);
			var h = (int)(previewScale * minimap.Size.Y);
			var x = RenderBounds.X + (RenderBounds.Width - w) / 2;
			var y = RenderBounds.Y + (RenderBounds.Height - h) / 2;
			mapRect = new Rectangle(x, y, w, h);

			Game.Renderer.RgbaSpriteRenderer.DrawSprite(minimap, new float2(mapRect.Location), new float2(mapRect.Size));

			TooltipSpawnIndex = -1;
			if (ShowSpawnPoints)
			{
				var spawnPoints = preview.SpawnPoints;
				var occupants = SpawnOccupants();
				var disabledSpawnPoints = DisabledSpawnPoints();
				var gridType = preview.GridType;
				for (var i = 0; i < spawnPoints.Length; i++)
				{
					var p = spawnPoints[i];

					// Spawn numbers are 1 indexed with 0 meaning "random spawn".
					var occupied = occupants.TryGetValue(i + 1, out var occupant);
					var disabled = disabledSpawnPoints.Contains(i + 1);
					var pos = ConvertToPreview(p, gridType);

					var sprite = disabled ? spawnDisabled : occupied ? spawnClaimed : spawnUnclaimed;
					var offset = sprite.Size.XY.ToInt2() / 2;

					if (((pos - Viewport.LastMousePos).ToFloat2() / offset.ToFloat2()).LengthSquared <= 1)
						TooltipSpawnIndex = spawnPoints.IndexOf(p) + 1;

					if (disabled)
					{
						Game.Renderer.RgbaSpriteRenderer.DrawSprite(spawnDisabled, pos - offset);
						continue;
					}

					if (occupied)
						WidgetUtils.FillEllipseWithColor(new Rectangle(pos.X - offset.X + 1, pos.Y - offset.Y + 1, (int)sprite.Size.X - 2, (int)sprite.Size.Y - 2), occupant.Color);

					Game.Renderer.RgbaSpriteRenderer.DrawSprite(sprite, pos - offset);

					var number = Convert.ToChar('A' + spawnPoints.IndexOf(p)).ToString();
					var textOffset = spawnFont.Measure(number) / 2 + spawnLabelOffset;

					spawnFont.DrawTextWithContrast(number, pos - textOffset, spawnColor, spawnContrastColor, 1);
				}
			}
		}

		public bool Loaded { get { return minimap != null; } }
	}
}
