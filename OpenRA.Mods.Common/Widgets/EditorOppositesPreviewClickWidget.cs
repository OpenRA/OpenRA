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
	public class EditorOppositesPreviewClickWidget : Widget
	{
		public int GridWidth = 3;
		public int GridHeight = 3;
		public Action<int> OnClickSlot = _ => { };

		public EditorOppositesPreviewClickWidget() { }

		protected EditorOppositesPreviewClickWidget(EditorOppositesPreviewClickWidget other)
			: base(other)
		{
			GridWidth = other.GridWidth;
			GridHeight = other.GridHeight;
			OnClickSlot = other.OnClickSlot;
		}

		public override EditorOppositesPreviewClickWidget Clone()
		{
			return new EditorOppositesPreviewClickWidget(this);
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
			if (!bounds.Contains(mi.Location) || GridWidth <= 0 || GridHeight <= 0)
				return true;

			var x = ((mi.Location.X - bounds.X) * GridWidth / Math.Max(1, bounds.Width)).Clamp(0, GridWidth - 1);
			var y = ((mi.Location.Y - bounds.Y) * GridHeight / Math.Max(1, bounds.Height)).Clamp(0, GridHeight - 1);
			OnClickSlot(y * GridWidth + x);
			return true;
		}
	}
}
