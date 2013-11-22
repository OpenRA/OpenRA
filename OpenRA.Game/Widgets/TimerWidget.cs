#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public class TimerWidget : LabelWidget
	{
		public override void Draw()
		{
			var font = Game.Renderer.Fonts[Font];
			var rb = RenderBounds;
			var color = GetColor();
			var contrast = GetContrastColor();
			
			var s = WidgetUtils.FormatTime(Game.LocalTick) + (Game.orderManager.world.Paused?" (paused)":"");
			var pos = new float2(rb.Left - font.Measure(s).X / 2, rb.Top);
			if (Contrast)
				font.DrawTextWithContrast(s, pos, color, contrast, 1);
			else
				font.DrawText(s, pos, color);
		}
	}
}

