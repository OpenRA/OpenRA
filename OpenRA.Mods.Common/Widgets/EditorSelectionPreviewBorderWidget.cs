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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class EditorSelectionPreviewBorderWidget : Widget
	{
		public int OriginX;
		public int OriginY;
		public int CellPixelSize;
		public List<CVec[]> CellRegions = [];

		// Must match TemplateBoundsOverlay.PlacedTileColor.
		static readonly Color BorderColor = Color.Yellow;

		public EditorSelectionPreviewBorderWidget()
		{ }

		protected EditorSelectionPreviewBorderWidget(EditorSelectionPreviewBorderWidget other)
			: base(other)
		{
			OriginX = other.OriginX;
			OriginY = other.OriginY;
			CellPixelSize = other.CellPixelSize;
			CellRegions = other.CellRegions;
		}

		public override EditorSelectionPreviewBorderWidget Clone() { return new EditorSelectionPreviewBorderWidget(this); }

		public void Clear()
		{
			OriginX = 0;
			OriginY = 0;
			CellPixelSize = 0;
			CellRegions.Clear();
		}

		public override void Draw()
		{
			if (CellRegions.Count == 0 || CellPixelSize <= 0)
				return;

			var cr = Game.Renderer.RgbaColorRenderer;
			foreach (var region in CellRegions)
				DrawCellRegionBorder(cr, region);
		}

		void DrawCellRegionBorder(RgbaColorRenderer cr, CVec[] region)
		{
			var cellSet = region.ToHashSet();
			var origin = RenderOrigin;

			foreach (var cell in cellSet)
			{
				var left = origin.X + OriginX + cell.X * CellPixelSize;
				var top = origin.Y + OriginY + cell.Y * CellPixelSize;
				var right = left + CellPixelSize;
				var bottom = top + CellPixelSize;

				if (!cellSet.Contains(cell + new CVec(0, -1)))
					cr.DrawLine(new float2(left, top), new float2(right, top), 1, BorderColor);

				if (!cellSet.Contains(cell + new CVec(1, 0)))
					cr.DrawLine(new float2(right, top), new float2(right, bottom), 1, BorderColor);

				if (!cellSet.Contains(cell + new CVec(0, 1)))
					cr.DrawLine(new float2(left, bottom), new float2(right, bottom), 1, BorderColor);

				if (!cellSet.Contains(cell + new CVec(-1, 0)))
					cr.DrawLine(new float2(left, top), new float2(left, bottom), 1, BorderColor);
			}
		}
	}
}
