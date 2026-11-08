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

namespace OpenRA
{
	/// <summary>
	/// Decides when the audio device should be reopened. Pure logic so it can be unit tested.
	/// Three signals: the device reports a disconnect, frames resume after a gap (the machine
	/// slept), or the device clock stops advancing (the mixer is dead, which macOS can cause
	/// after sleep without reporting a disconnect or stopping our frames).
	/// </summary>
	public sealed class SoundDeviceWatchdog
	{
		public const int SleepGapMs = 3000;
		public const int ClockStallMs = 2000;
		public const int RetryIntervalMs = 1000;
		public const int MaxRetries = 10;

		// A newly selected output (especially Bluetooth) is not usable the instant it is announced.
		public const int OutputChangeSettleMs = 500;

		long lastFrameMs = -1;
		long lastClockNs = -1;
		long clockChangedMs;
		long nextAttemptMs;
		int retriesLeft;
		int stallAttempts;

		public string LastReason { get; private set; }

		public bool ShouldReopen(long nowMs, bool connected, long deviceClockNs)
		{
			if (deviceClockNs != lastClockNs)
			{
				lastClockNs = deviceClockNs;
				clockChangedMs = nowMs;
				stallAttempts = 0;
			}

			var slept = lastFrameMs >= 0 && nowMs - lastFrameMs >= SleepGapMs;
			if (slept)
			{
				// The clock could not have advanced while frames were not running to see it.
				LastReason = $"frame gap of {nowMs - lastFrameMs} ms";
				clockChangedMs = nowMs;
				Arm(nowMs);
			}

			lastFrameMs = nowMs;

			if (retriesLeft == 0 && nowMs >= nextAttemptMs)
			{
				if (!connected)
				{
					LastReason = "device reported disconnected";
					Arm(nowMs);
				}
				else if (nowMs - clockChangedMs >= ClockStallMs && stallAttempts < MaxRetries)
				{
					LastReason = $"device clock stalled for {nowMs - clockChangedMs} ms";
					stallAttempts++;
					clockChangedMs = nowMs;
					Arm(nowMs);
				}
			}

			if (retriesLeft == 0 || nowMs < nextAttemptMs)
				return false;

			retriesLeft--;
			nextAttemptMs = nowMs + RetryIntervalMs;
			return true;
		}

		public void NotifyOutputChanged(long nowMs)
		{
			LastReason = "default output changed";
			Arm(nowMs + OutputChangeSettleMs);
		}

		void Arm(long nowMs)
		{
			retriesLeft = MaxRetries;
			nextAttemptMs = nowMs;
		}

		public void ReportReopened()
		{
			retriesLeft = 0;
		}
	}
}
