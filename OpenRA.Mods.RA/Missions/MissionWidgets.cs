#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Missions
{
	public class CountdownTimerWidget : Widget
	{
		public CountdownTimer Timer;
		public string Format;

		public CountdownTimerWidget(CountdownTimer timer, string format)
		{
			Timer = timer;
			Format = format;
		}

		public override void Draw()
		{
			if (!IsVisible()) return;

			// TODO: Don't hardcode the screen position
			var font = Game.Renderer.Fonts["Bold"];
			var text = Format.F(WidgetUtils.FormatTime(Timer.TicksLeft));
			var pos = new float2(Game.Renderer.Resolution.Width * 0.5f - font.Measure(text).X / 2, Game.Renderer.Resolution.Height * 0.1f);
			font.DrawTextWithContrast(text, pos, Timer.TicksLeft <= 25 * 60 && Game.LocalTick % 50 < 25 ? Color.Red : Color.White, Color.Black, 1);
		}
	}

	public class InfoWidget : Widget
	{
		public string Text;

		public InfoWidget(string text) { Text = text; }

		public override void Draw()
		{
			if (!IsVisible()) return;

			// TODO: Don't hardcode the screen position
			var font = Game.Renderer.Fonts["Bold"];
			var pos = new float2(Game.Renderer.Resolution.Width * 0.5f - font.Measure(Text).X / 2, Game.Renderer.Resolution.Height * 0.1f);
			font.DrawTextWithContrast(Text, pos, Color.White, Color.Black, 1);
		}
	}
}
