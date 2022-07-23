#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;

namespace OpenRA.Platforms.Default
{
	readonly struct MouseTapHistory
	{
		public readonly byte Button;
		public readonly byte TapCount;
		public readonly int2 LastLocation;
		public readonly DateTime LastReleaseExpireTime;

		public MouseTapHistory(byte button, int2 lastLocation, DateTime expireTime, int count)
		{
			Button = button;
			LastLocation = lastLocation;
			LastReleaseExpireTime = expireTime;
			TapCount = (byte)count;
		}
	}

	static class MultiMouseTapDetection
	{
		const int MaxConcurrentButtonsTapped = 3;
		const int MaxTapsPerButton = 3;
		const int MaxLocationDistance = 4;
		const int FindResultAllExpired = -1;
		const int FindResultNotFound = -2;

		static readonly TimeSpan MaxDurationBetweenTaps = TimeSpan.FromMilliseconds(250);
		static readonly MouseTapHistory[] History = Enumerable.Repeat(
			new MouseTapHistory((byte)MouseButton.None, new int2(0, 0), DateTime.Now - (MaxDurationBetweenTaps * 2), 0), MaxConcurrentButtonsTapped).ToArray();

		static DateTime lastTapExpireTime = DateTime.Now - (MaxDurationBetweenTaps * 2);

		static int Find(byte button, DateTime now)
		{
			// No recent taps.
			if (now > lastTapExpireTime)
				return FindResultAllExpired;

			for (var i = 0; i < MaxConcurrentButtonsTapped; i++)
				if (History[i].Button == button)
					return i;

			return FindResultNotFound;
		}

		static int FindUpdateIndex(DateTime now)
		{
			// First expired or otherwise oldest.
			var minTime = History[0].LastReleaseExpireTime;
			var index = 0;
			for (var i = 1; i < MaxConcurrentButtonsTapped && minTime >= now; i++)
			{
				ref var el = ref History[i];
				if (el.LastReleaseExpireTime < minTime)
				{
					minTime = el.LastReleaseExpireTime;
					index = i;
				}
			}

			return index;
		}

		public static int Detect(byte button, int2 xy)
		{
			var now = DateTime.Now;
			var count = 0;
			var index = Find(button, now);
			if (index >= 0)
			{
				ref var el = ref History[index];
				if (now <= el.LastReleaseExpireTime && (xy - el.LastLocation).LengthSquared < MaxLocationDistance * MaxLocationDistance)
					count = (int)el.TapCount;
			}
			else if (index == FindResultAllExpired)
				index = 0;
			else
				index = FindUpdateIndex(now);

			lastTapExpireTime = now + MaxDurationBetweenTaps;
			History[index] = new MouseTapHistory(button, xy, lastTapExpireTime, count < MaxTapsPerButton ? ++count : count);
			return count;
		}

		public static int Info(byte button)
		{
			var now = DateTime.Now;
			var index = Find(button, now);
			if (index < 0)
				return 0;

			ref var el = ref History[index];
			return now <= el.LastReleaseExpireTime ? el.TapCount : 0;
		}
	}
}
