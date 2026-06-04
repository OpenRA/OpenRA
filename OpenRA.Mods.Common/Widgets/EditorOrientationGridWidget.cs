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
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class EditorOrientationGridWidget : Widget
	{
		static readonly string[] SlotGlyphs = ["↖", "↑", "↗", "←", "", "→", "↙", "↓", "↘"];
		static readonly CachedTransform<(bool, bool, bool, bool, bool), Sprite[]> PanelCache =
			WidgetUtils.GetCachedStatefulPanelImages("scrollitem");

		static readonly Color SelectionColor = Color.FromArgb(0xFF4CFF00);

		public bool RingCenterSlots = true;
		public bool MultiSelectMode;
		public Func<int?> GetSelectedSlot = () => null;
		public Func<IReadOnlyCollection<int>> GetSelectedSlots = () => [];
		public Action<int> OnSelectSlot = _ => { };

		public EditorOrientationGridWidget() { }

		protected EditorOrientationGridWidget(EditorOrientationGridWidget other)
			: base(other)
		{
			RingCenterSlots = other.RingCenterSlots;
			MultiSelectMode = other.MultiSelectMode;
			GetSelectedSlot = other.GetSelectedSlot;
			GetSelectedSlots = other.GetSelectedSlots;
			OnSelectSlot = other.OnSelectSlot;
		}

		public override EditorOrientationGridWidget Clone() => new(this);

		public override void Draw()
		{
			var bounds = RenderBounds;
			if (bounds.Width <= 0 || bounds.Height <= 0)
				return;

			var cellW = bounds.Width / 3;
			var cellH = bounds.Height / 3;
			var selected = GetSelectedSlot();
			var selectedSlots = GetSelectedSlots();
			var boldFont = Game.Renderer.Fonts["Bold"];
			var tinyFont = Game.Renderer.Fonts["Tiny"];

			for (var slot = 0; slot < 9; slot++)
			{
				var col = slot % 3;
				var row = slot / 3;
				var cell = new Rectangle(bounds.X + col * cellW, bounds.Y + row * cellH, cellW, cellH);
				var inner = cell.InflateBy(2, 2, -2, -2);

				if (RingCenterSlots && slot == EditorTileMetadata.RingHiddenSlot)
				{
					DrawCenterCell(cell, inner, selected, tinyFont);
					continue;
				}

				var isSelected = MultiSelectMode
					? selectedSlots.Contains(slot)
					: selected == slot;
				WidgetUtils.DrawPanel(inner, PanelCache.Update((false, false, false, false, isSelected)));

				if (isSelected)
					DrawSelectionBorder(inner);

				var glyph = SlotGlyphs[slot];
				if (string.IsNullOrEmpty(glyph))
					continue;

				var size = boldFont.Measure(glyph);
				var pos = new float2(
					cell.Left + (cellW - size.X) / 2,
					cell.Top + (cellH - size.Y) / 2);
				boldFont.DrawText(glyph, pos, Color.White);
			}

			var lineColor = Color.FromArgb(0x88FFFFFF);
			for (var i = 1; i < 3; i++)
			{
				var x = bounds.X + i * cellW;
				Game.Renderer.RgbaColorRenderer.DrawLine(new float3(x, bounds.Y, 0), new float3(x, bounds.Bottom, 0), 1, lineColor);
				var y = bounds.Y + i * cellH;
				Game.Renderer.RgbaColorRenderer.DrawLine(new float3(bounds.X, y, 0), new float3(bounds.Right, y, 0), 1, lineColor);
			}

			if (RingCenterSlots)
			{
				var centerX = bounds.X + cellW;
				var centerY = bounds.Y + cellH;
				Game.Renderer.RgbaColorRenderer.DrawLine(
					new float3(centerX, centerY + cellH / 2, 0),
					new float3(centerX + cellW, centerY + cellH / 2, 0),
					1,
					lineColor);
			}
		}

		static void DrawCenterCell(Rectangle cell, Rectangle inner, int? selected, SpriteFont tinyFont)
		{
			var horizSelected = selected == EditorTileMetadata.HorizontalSlot;
			var vertSelected = selected == EditorTileMetadata.VerticalSlot;
			var centerSelected = selected == EditorTileMetadata.RingHiddenSlot;
			var halfH = cell.Height / 2;
			var topInner = new Rectangle(inner.X, inner.Y, inner.Width, halfH - 2);
			var bottomInner = new Rectangle(inner.X, inner.Y + halfH + 2, inner.Width, halfH - 2);

			WidgetUtils.DrawPanel(topInner, PanelCache.Update((false, false, false, false, horizSelected || centerSelected)));
			WidgetUtils.DrawPanel(bottomInner, PanelCache.Update((false, false, false, false, vertSelected || centerSelected)));

			if (horizSelected)
				DrawSelectionBorder(topInner);
			else if (centerSelected)
				DrawSelectionBorder(inner);

			if (vertSelected)
				DrawSelectionBorder(bottomInner);

			DrawCenterLabel(tinyFont, "Horizontal", cell, top: true);
			DrawCenterLabel(tinyFont, "Vertical", cell, top: false);
		}

		static void DrawCenterLabel(SpriteFont font, string text, Rectangle cell, bool top)
		{
			var size = font.Measure(text);
			var y = top
				? cell.Y + (cell.Height / 2 - size.Y) / 2
				: cell.Y + cell.Height / 2 + (cell.Height / 2 - size.Y) / 2;
			var pos = new float2(cell.X + (cell.Width - size.X) / 2, y);
			font.DrawText(text, pos, Color.White);
		}

		static void DrawSelectionBorder(Rectangle inner)
		{
			Game.Renderer.RgbaColorRenderer.DrawRect(
				new int2(inner.Left, inner.Top),
				new int2(inner.Right, inner.Bottom),
				2,
				SelectionColor);
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Button != MouseButton.Left)
				return false;

			if (mi.Event == MouseInputEvent.Down)
				return TakeMouseFocus(mi);

			if (!HasMouseFocus || mi.Event != MouseInputEvent.Up)
				return false;

			YieldMouseFocus(mi);
			var bounds = RenderBounds;
			if (!bounds.Contains(mi.Location))
				return true;

			var cellW = Math.Max(1, bounds.Width / 3);
			var cellH = Math.Max(1, bounds.Height / 3);
			var x = ((mi.Location.X - bounds.X) * 3 / bounds.Width).Clamp(0, 2);
			var y = ((mi.Location.Y - bounds.Y) * 3 / bounds.Height).Clamp(0, 2);

			if (RingCenterSlots && x == 1 && y == 1)
			{
				var centerTop = bounds.Y + cellH;
				var slot = mi.Location.Y < centerTop + cellH / 2
					? EditorTileMetadata.HorizontalSlot
					: EditorTileMetadata.VerticalSlot;
				OnSelectSlot(slot);
			}
			else
				OnSelectSlot(y * 3 + x);

			return true;
		}
	}
}
