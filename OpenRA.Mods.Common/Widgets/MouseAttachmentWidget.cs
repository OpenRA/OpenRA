#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class MouseAttachmentWidget : Widget
	{
		public bool ClickThrough = true;

		Sprite sprite;
		readonly WorldRenderer worldRenderer;
		string palette;
		int2 location;

		[ObjectCreator.UseCtor]
		public MouseAttachmentWidget(WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
		}

		public override void Draw()
		{
			if (sprite != null && palette != null)
			{
				var scale = Game.Cursor is SoftwareCursor && CursorProvider.CursorViewportZoomed ? 2 : 1;
				var directionPalette = worldRenderer.Palette(palette);
				WidgetUtils.DrawSHPCentered(sprite, ChildOrigin, directionPalette, scale);
			}
		}

		public void SetAttachment(int2 location, Sprite sprite, string palette)
		{
			this.sprite = sprite;
			this.location = location;
			this.palette = palette;
		}

		public void Reset()
		{
			sprite = null;
			palette = null;
		}

		public override int2 ChildOrigin { get { return location; } }
	}
}
