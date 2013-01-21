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

namespace OpenRA.Mods.RA.Missions
{
	public class CountdownTimer
	{
		public int TicksLeft;

		public event Action<CountdownTimer> OnExpired = t => { };
		public event Action<CountdownTimer> OnOneMinuteRemaining = t => { };
		public event Action<CountdownTimer> OnTwoMinutesRemaining = t => { };
		public event Action<CountdownTimer> OnThreeMinutesRemaining = t => { };
		public event Action<CountdownTimer> OnFourMinutesRemaining = t => { };
		public event Action<CountdownTimer> OnFiveMinutesRemaining = t => { };
		public event Action<CountdownTimer> OnTenMinutesRemaining = t => { };
		public event Action<CountdownTimer> OnTwentyMinutesRemaining = t => { };
		public event Action<CountdownTimer> OnThirtyMinutesRemaining = t => { };
		public event Action<CountdownTimer> OnFortyMinutesRemaining = t => { };

		public CountdownTimer(int ticksLeft, Action<CountdownTimer> onExpired, bool withNotifications)
		{
			TicksLeft = ticksLeft;
			OnExpired += onExpired;
			if (withNotifications)
			{
				OnOneMinuteRemaining += t => Sound.Play("1minr.aud");
				OnTwoMinutesRemaining += t => Sound.Play("2minr.aud");
				OnThreeMinutesRemaining += t => Sound.Play("3minr.aud");
				OnFourMinutesRemaining += t => Sound.Play("4minr.aud");
				OnFiveMinutesRemaining += t => Sound.Play("5minr.aud");
				OnTenMinutesRemaining += t => Sound.Play("10minr.aud");
				OnTwentyMinutesRemaining += t => Sound.Play("20minr.aud");
				OnThirtyMinutesRemaining += t => Sound.Play("30minr.aud");
				OnFortyMinutesRemaining += t => Sound.Play("40minr.aud");
			}
		}

		public void Tick()
		{
			if (TicksLeft <= 0) return;

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
