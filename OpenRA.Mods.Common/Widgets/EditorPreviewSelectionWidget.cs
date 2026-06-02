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
	public enum EditorPreviewSelectionKind { None, Primary, Secondary }

	public class EditorPreviewSelectionWidget : Widget
	{
		static readonly Color PrimaryColor = Color.FromArgb(0xFF4CFF00);
		static readonly Color SecondaryColor = Color.FromArgb(0xFF3399FF);

		public Func<EditorPreviewSelectionKind> GetSelection = () => EditorPreviewSelectionKind.None;

		public EditorPreviewSelectionWidget() { IgnoreMouseOver = true; }

		protected EditorPreviewSelectionWidget(EditorPreviewSelectionWidget other)
			: base(other)
		{
			GetSelection = other.GetSelection;
			IgnoreMouseOver = true;
		}

		public override EditorPreviewSelectionWidget Clone() => new(this);

		public override bool EventBoundsContains(int2 location) => false;

		public override void Draw()
		{
			var kind = GetSelection();
			if (kind == EditorPreviewSelectionKind.None)
				return;

			var color = kind == EditorPreviewSelectionKind.Primary ? PrimaryColor : SecondaryColor;
			var rb = RenderBounds;
			Game.Renderer.RgbaColorRenderer.DrawRect(
				new int2(rb.Left, rb.Top),
				new int2(rb.Right, rb.Bottom),
				2,
				color);
		}
	}
}
