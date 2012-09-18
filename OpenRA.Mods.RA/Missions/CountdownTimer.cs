#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Missions
{
	public class CountdownTimer
	{
		public int TicksLeft { get; set; }

		public Action<CountdownTimer> OnExpired { get; set; }
		public Action<CountdownTimer> OnOneMinuteRemaining { get; set; }
		public Action<CountdownTimer> OnTwoMinutesRemaining { get; set; }
		public Action<CountdownTimer> OnThreeMinutesRemaining { get; set; }
		public Action<CountdownTimer> OnFourMinutesRemaining { get; set; }
		public Action<CountdownTimer> OnFiveMinutesRemaining { get; set; }
		public Action<CountdownTimer> OnTenMinutesRemaining { get; set; }
		public Action<CountdownTimer> OnTwentyMinutesRemaining { get; set; }
		public Action<CountdownTimer> OnThirtyMinutesRemaining { get; set; }
		public Action<CountdownTimer> OnFortyMinutesRemaining { get; set; }

		public CountdownTimer(int ticksLeft, Action<CountdownTimer> onExpired)
		{
			TicksLeft = ticksLeft;
			OnExpired = onExpired;
			OnOneMinuteRemaining = t => Sound.Play("1minr.aud");
			OnTwoMinutesRemaining = t => Sound.Play("2minr.aud");
			OnThreeMinutesRemaining = t => Sound.Play("3minr.aud");
			OnFourMinutesRemaining = t => Sound.Play("4minr.aud");
			OnFiveMinutesRemaining = t => Sound.Play("5minr.aud");
			OnTenMinutesRemaining = t => Sound.Play("10minr.aud");
			OnTwentyMinutesRemaining = t => Sound.Play("20minr.aud");
			OnThirtyMinutesRemaining = t => Sound.Play("30minr.aud");
			OnFortyMinutesRemaining = t => Sound.Play("40minr.aud");
		}

		public void Tick()
		{
			if (TicksLeft > 0)
			{
				TicksLeft--;
				switch (TicksLeft)
				{
					case 1500 * 00: OnExpired(this); break;
					case 1500 * 01: OnOneMinuteRemaining(this); break;
					case 1500 * 02: OnTwoMinutesRemaining(this); break;
					case 1500 * 03: OnThreeMinutesRemaining(this); break;
					case 1500 * 04: OnFourMinutesRemaining(this); break;
					case 1500 * 05: OnFiveMinutesRemaining(this); break;
					case 1500 * 10: OnTenMinutesRemaining(this); break;
					case 1500 * 20: OnTwentyMinutesRemaining(this); break;
					case 1500 * 30: OnThirtyMinutesRemaining(this); break;
					case 1500 * 40: OnFortyMinutesRemaining(this); break;
				}
			}
		}
	}

	public class CountdownTimerWidget : Widget
	{
		public CountdownTimer CountdownTimer { get; set; }
		public string Header { get; set; }
		public float2 Position { get; set; }

		public CountdownTimerWidget(CountdownTimer countdownTimer, string header, float2 position)
		{
			CountdownTimer = countdownTimer;
			Header = header;
			Position = position;
		}

		public override void Draw()
		{
			if (!IsVisible())
			{
				return;
			}
			var font = Game.Renderer.Fonts["Bold"];
			var text = "{0}: {1}".F(Header, WidgetUtils.FormatTime(CountdownTimer.TicksLeft));
			font.DrawTextWithContrast(text, Position, CountdownTimer.TicksLeft == 0 && Game.LocalTick % 60 >= 30 ? Color.Red : Color.White, Color.Black, 1);
		}
	}
}
