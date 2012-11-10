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
		public CountdownTimer Timer { get; set; }
		public string Format { get; set; }
		public float2 Position { get; set; }

		public CountdownTimerWidget(CountdownTimer timer, string format, float2 position)
		{
			Timer = timer;
			Format = format;
			Position = position;
		}

		public override void Draw()
		{
			if (!IsVisible())
			{
				return;
			}
			var font = Game.Renderer.Fonts["Bold"];
			var text = Format.F(WidgetUtils.FormatTime(Timer.TicksLeft));
			font.DrawTextWithContrast(text, Position, Timer.TicksLeft <= 25 * 10 && Game.LocalTick % 50 < 25 ? Color.Red : Color.White, Color.Black, 1);
		}
	}

	public class InfoWidget : Widget
	{
		public string Text { get; set; }
		public float2 Position { get; set; }

		public InfoWidget(string text, float2 position)
		{
			Text = text;
			Position = position;
		}

		public override void Draw()
		{
			if (!IsVisible())
			{
				return;
			}
			var font = Game.Renderer.Fonts["Bold"];
			font.DrawTextWithContrast(Text, Position, Color.White, Color.Black, 1);
		}
	}
}
