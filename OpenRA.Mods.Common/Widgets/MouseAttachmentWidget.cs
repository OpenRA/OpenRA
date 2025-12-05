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

using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class MouseAttachmentWidget : Widget
	{
		public bool ClickThrough = true;

		ISpriteSequence sprite;
		readonly WorldRenderer worldRenderer;
		readonly GraphicSettings graphicSettings;
		int2 location;

		[ObjectCreator.UseCtor]
		public MouseAttachmentWidget(WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
			graphicSettings = Game.Settings.Graphics;
		}

		public override void Draw()
		{
			if (sprite != null)
			{
				var directionPalette = worldRenderer.Palette(sprite.GetPalette());

				// Cursor is rendered in native window coordinates
				// Apply same scaling rules as hardware cursors
				var scale = (graphicSettings.CursorDouble ? 2 : 1) * (Game.Renderer.NativeWindowScale > 1.5f ? 2 : 1);
				WidgetUtils.DrawSpriteCentered(sprite.GetSprite(0), directionPalette, ChildOrigin, scale / Game.Renderer.WindowScale);
			}
		}

		public void SetAttachment(int2 location, ISpriteSequence sprite)
		{
			this.sprite = sprite;
			this.location = location;
		}

		public void Reset()
		{
			sprite = null;
		}

		public override int2 ChildOrigin => location;
	}
}
