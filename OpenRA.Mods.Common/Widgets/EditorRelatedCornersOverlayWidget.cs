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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	/// <summary>Mini 3x3 corner map shown inside opposite list cells.</summary>
	public class EditorRelatedCornersOverlayWidget : Widget
	{
		static readonly Color DotColor = Color.FromArgb(0xFF4CFF00);
		static readonly Color GridColor = Color.FromArgb(0x55FFFFFF);

		public Func<IEnumerable<int>> GetCornerSlots = () => [];

		public EditorRelatedCornersOverlayWidget() { }

		protected EditorRelatedCornersOverlayWidget(EditorRelatedCornersOverlayWidget other)
			: base(other)
		{
			GetCornerSlots = other.GetCornerSlots;
		}

		public override EditorRelatedCornersOverlayWidget Clone() => new(this);

		public override void Draw()
		{
			var bounds = RenderBounds;
			if (bounds.Width <= 4 || bounds.Height <= 4)
				return;

			var size = Math.Min(bounds.Width, bounds.Height);
			var originX = bounds.Right - size;
			var originY = bounds.Bottom - size;
			var cellW = size / 3;
			var cellH = size / 3;

			for (var i = 1; i < 3; i++)
			{
				var x = originX + i * cellW;
				Game.Renderer.RgbaColorRenderer.DrawLine(
					new float3(x, originY, 0), new float3(x, originY + size, 0), 1, GridColor);
				var y = originY + i * cellH;
				Game.Renderer.RgbaColorRenderer.DrawLine(
					new float3(originX, y, 0), new float3(originX + size, y, 0), 1, GridColor);
			}

			foreach (var slot in GetCornerSlots())
			{
				if (slot < 0 || slot >= 9)
					continue;

				var col = slot % 3;
				var row = slot / 3;
				var cx = originX + col * cellW + cellW / 2;
				var cy = originY + row * cellH + cellH / 2;
				Game.Renderer.RgbaColorRenderer.DrawRect(
					new int2(cx - 2, cy - 2),
					new int2(cx + 3, cy + 3),
					1,
					DotColor);
			}
		}
	}
}
