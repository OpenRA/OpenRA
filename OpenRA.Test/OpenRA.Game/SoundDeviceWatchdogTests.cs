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

using NUnit.Framework;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class SoundDeviceWatchdogTests
	{
		[Test]
		public void NormalFrames_DoNotReopen()
		{
			var w = new SoundDeviceWatchdog();
			for (var t = 0; t < 60000; t += 16)
				Assert.That(w.ShouldReopen(t, true, t * 1000000L), Is.False);
		}

		[Test]
		public void LongGap_ReopensEvenWhenReportedConnected()
		{
			var w = new SoundDeviceWatchdog();
			w.ShouldReopen(1000, true, 1);
			Assert.That(w.ShouldReopen(1000 + SoundDeviceWatchdog.SleepGapMs, true, 2), Is.True);
		}

		[Test]
		public void Disconnected_Reopens()
		{
			var w = new SoundDeviceWatchdog();
			Assert.That(w.ShouldReopen(1000, false, 1), Is.True);
		}

		[Test]
		public void Failure_RetriesAtIntervalThenStops()
		{
			var w = new SoundDeviceWatchdog();
			w.ShouldReopen(0, true, 0);
			const long Wake = SoundDeviceWatchdog.SleepGapMs;
			Assert.That(w.ShouldReopen(Wake, true, 1), Is.True);
			Assert.That(w.ShouldReopen(Wake + 16, true, 2), Is.False, "throttled");

			var attempts = 1;
			for (var i = 1; i <= 20; i++)
				if (w.ShouldReopen(Wake + i * SoundDeviceWatchdog.RetryIntervalMs, true, i + 2))
					attempts++;

			Assert.That(attempts, Is.EqualTo(SoundDeviceWatchdog.MaxRetries));
		}

		[Test]
		public void ClockStall_ReopensWhileFramesKeepRunning()
		{
			// Lid closed but no real sleep: frames continue and the device says it is connected.
			var w = new SoundDeviceWatchdog();
			var reopened = false;
			for (var t = 0; t <= SoundDeviceWatchdog.ClockStallMs + 32; t += 16)
				reopened |= w.ShouldReopen(t, true, 5);

			Assert.That(reopened, Is.True);
			Assert.That(w.LastReason, Does.Contain("clock"));
		}

		[Test]
		public void PermanentlyStalledClock_GivesUp()
		{
			var w = new SoundDeviceWatchdog();
			var attempts = 0;
			for (var t = 0; t < 600000; t += 16)
			{
				if (w.ShouldReopen(t, true, 5))
				{
					attempts++;
					w.ReportReopened();
				}
			}

			Assert.That(attempts, Is.EqualTo(SoundDeviceWatchdog.MaxRetries));
		}

		[Test]
		public void OutputChange_ReopensAfterSettling()
		{
			var w = new SoundDeviceWatchdog();
			w.ShouldReopen(1000, true, 1);
			w.NotifyOutputChanged(1000);
			Assert.That(w.ShouldReopen(1016, true, 2), Is.False, "still settling");
			Assert.That(w.ShouldReopen(1000 + SoundDeviceWatchdog.OutputChangeSettleMs, true, 3), Is.True);
			Assert.That(w.LastReason, Does.Contain("output"));
		}

		[Test]
		public void Success_StopsRetrying()
		{
			var w = new SoundDeviceWatchdog();
			w.ShouldReopen(0, true, 0);
			Assert.That(w.ShouldReopen(SoundDeviceWatchdog.SleepGapMs, true, 1), Is.True);
			w.ReportReopened();
			Assert.That(w.ShouldReopen(SoundDeviceWatchdog.SleepGapMs + SoundDeviceWatchdog.RetryIntervalMs, true, 2), Is.False);
		}
	}
}
