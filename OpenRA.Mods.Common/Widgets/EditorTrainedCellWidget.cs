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
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class EditorTrainedCellWidget : Widget
	{
		public Func<bool> IsTrained = () => false;
		public Color BorderColor = Color.FromArgb(0xFF228B22);
		public int BorderWidth = 3;

		public EditorTrainedCellWidget() { IgnoreMouseOver = true; }

		protected EditorTrainedCellWidget(EditorTrainedCellWidget other)
			: base(other)
		{
			IsTrained = other.IsTrained;
			BorderColor = other.BorderColor;
			BorderWidth = other.BorderWidth;
		}

		public override EditorTrainedCellWidget Clone() { return new EditorTrainedCellWidget(this); }

		public override bool EventBoundsContains(int2 location) => false;

		public override void Draw()
		{
			if (!IsTrained())
				return;

			var rb = RenderBounds;
			Game.Renderer.RgbaColorRenderer.DrawRect(
				new int2(rb.Left, rb.Top),
				new int2(rb.Right, rb.Bottom),
				BorderWidth,
				BorderColor);
		}
	}
}
