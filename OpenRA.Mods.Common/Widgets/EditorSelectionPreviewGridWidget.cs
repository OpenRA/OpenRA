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
	public class EditorSelectionPreviewGridWidget : Widget
	{
		public int GridWidth;
		public int GridHeight;

		static readonly Color GridLineColor = Color.FromArgb(192, 160, 160, 160);

		public EditorSelectionPreviewGridWidget()
		{ }

		protected EditorSelectionPreviewGridWidget(EditorSelectionPreviewGridWidget other)
			: base(other)
		{
			GridWidth = other.GridWidth;
			GridHeight = other.GridHeight;
		}

		public override EditorSelectionPreviewGridWidget Clone() { return new EditorSelectionPreviewGridWidget(this); }

		public override void Draw()
		{
			if (GridWidth <= 0 || GridHeight <= 0)
				return;

			var bounds = RenderBounds;
			if (bounds.Width <= 0 || bounds.Height <= 0)
				return;

			var cr = Game.Renderer.RgbaColorRenderer;
			var cellW = bounds.Width / (float)GridWidth;
			var cellH = bounds.Height / (float)GridHeight;

			for (var x = 0; x <= GridWidth; x++)
			{
				var px = bounds.Left + (int)Math.Round(x * cellW);
				cr.DrawLine(new float2(px, bounds.Top), new float2(px, bounds.Bottom), 1, GridLineColor);
			}

			for (var y = 0; y <= GridHeight; y++)
			{
				var py = bounds.Top + (int)Math.Round(y * cellH);
				cr.DrawLine(new float2(bounds.Left, py), new float2(bounds.Right, py), 1, GridLineColor);
			}
		}
	}
}
