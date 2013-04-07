#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;

namespace OpenRA.Widgets
{
	public class TimerWidget : Widget
	{
		public override void Draw()
		{
			var font = Game.Renderer.Fonts["Title"];
			var rb = RenderBounds;
			
			var s = WidgetUtils.FormatTime(Game.LocalTick) + (Game.orderManager.world.Paused?" (paused)":"");
			var pos = new float2(rb.Left - font.Measure(s).X / 2, rb.Top);
			font.DrawTextWithContrast(s, pos, Color.White, Color.Black, 1);
		}
	}
}

