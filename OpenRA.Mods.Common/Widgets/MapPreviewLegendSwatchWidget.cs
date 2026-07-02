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
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class MapPreviewLegendSwatchWidget : Widget
	{
		readonly Sprite spawnUnclaimed;
		readonly SpriteFont spawnFont;
		readonly Color spawnColor;
		readonly Color spawnContrastColor;
		readonly int2 spawnLabelOffset;

		public Func<Color> GetColor = () => Color.White;
		public Func<bool> GetIsSpawnPoint = () => false;
		public Func<bool> GetIsHighlighted = () => false;
		public Action OnClick;

		public MapPreviewLegendSwatchWidget()
		{
			spawnUnclaimed = ChromeProvider.GetImage("lobby-bits", "spawn-unclaimed");
			spawnFont = Game.Renderer.Fonts[ChromeMetrics.Get<string>("SpawnFont")];
			spawnColor = ChromeMetrics.Get<Color>("SpawnColor");
			spawnContrastColor = ChromeMetrics.Get<Color>("SpawnContrastColor");
			spawnLabelOffset = ChromeMetrics.Get<int2>("SpawnLabelOffset");
		}

		protected MapPreviewLegendSwatchWidget(MapPreviewLegendSwatchWidget other)
			: base(other)
		{
			spawnUnclaimed = ChromeProvider.GetImage("lobby-bits", "spawn-unclaimed");
			spawnFont = Game.Renderer.Fonts[ChromeMetrics.Get<string>("SpawnFont")];
			spawnColor = ChromeMetrics.Get<Color>("SpawnColor");
			spawnContrastColor = ChromeMetrics.Get<Color>("SpawnContrastColor");
			spawnLabelOffset = ChromeMetrics.Get<int2>("SpawnLabelOffset");
			GetColor = other.GetColor;
			GetIsSpawnPoint = other.GetIsSpawnPoint;
			GetIsHighlighted = other.GetIsHighlighted;
			OnClick = other.OnClick;
		}

		public override MapPreviewLegendSwatchWidget Clone() { return new MapPreviewLegendSwatchWidget(this); }

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Down && OnClick != null)
			{
				OnClick();
				return true;
			}

			return false;
		}

		public override void Draw()
		{
			if (GetIsSpawnPoint())
			{
				var rb = RenderBounds;
				if (GetIsHighlighted())
				{
					var border = new Rectangle(rb.X - 2, rb.Y - 2, rb.Width + 4, rb.Height + 4);
					WidgetUtils.FillRectWithColor(border, Color.LimeGreen);
				}

				var spriteSize = spawnUnclaimed.Size.XY.ToInt2();
				var spriteOrigin = new int2(
					rb.X + (rb.Width - spriteSize.X) / 2,
					rb.Y + (rb.Height - spriteSize.Y) / 2);

				WidgetUtils.DrawSprite(spawnUnclaimed, spriteOrigin);

				var letter = "A";
				var center = spriteOrigin + spriteSize / 2;
				var textOffset = spawnFont.Measure(letter) / 2 + spawnLabelOffset;
				spawnFont.DrawTextWithContrast(letter, center - textOffset, spawnColor, spawnContrastColor, 1);
				return;
			}

			var bounds = RenderBounds;
			if (GetIsHighlighted())
			{
				var border = new Rectangle(bounds.X - 2, bounds.Y - 2, bounds.Width + 4, bounds.Height + 4);
				WidgetUtils.FillRectWithColor(border, Color.LimeGreen);
			}

			WidgetUtils.FillRectWithColor(bounds, GetColor());
		}
	}
}
