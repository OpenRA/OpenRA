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
	readonly struct KeyTapHistory
	{
		public readonly Keycode Key;
		public readonly byte TapCount;
		public readonly Modifiers Modifiers;
		public readonly DateTime LastReleaseExpireTime;

		public KeyTapHistory(Keycode key, Modifiers modifiers, DateTime expireTime, int count)
		{
			Key = key;
			Modifiers = modifiers;
			LastReleaseExpireTime = expireTime;
			TapCount = (byte)count;
		}
	}

	static class MultiKeyTapDetection
	{
		const int MaxConcurrentKeysTapped = 6;
		const int MaxTapsPerKey = 3;
		const int FindResultAllExpired = -1;
		const int FindResultNotFound = -2;

		static readonly TimeSpan MaxDurationBetweenTaps = TimeSpan.FromMilliseconds(250);
		static readonly KeyTapHistory[] History = Enumerable.Repeat(
			new KeyTapHistory(Keycode.UNKNOWN, Modifiers.None, DateTime.Now - (MaxDurationBetweenTaps * 2), 0), MaxConcurrentKeysTapped).ToArray();

		static DateTime lastTapExpireTime = DateTime.Now - (MaxDurationBetweenTaps * 2);

		static int Find(Keycode key, DateTime now)
		{
			// No recent taps.
			if (now > lastTapExpireTime)
				return FindResultAllExpired;

			for (var i = 0; i < MaxConcurrentKeysTapped; i++)
				if (History[i].Key == key)
					return i;

			return FindResultNotFound;
		}

		static int FindUpdateIndex(DateTime now)
		{
			// First expired or otherwise oldest.
			var minTime = History[0].LastReleaseExpireTime;
			var index = 0;
			for (var i = 1; i < MaxConcurrentKeysTapped && minTime >= now; i++)
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

		public static int Detect(Keycode key, Modifiers mods)
		{
			var now = DateTime.Now;
			var count = 0;
			var index = Find(key, now);
			if (index >= 0)
			{
				// Detects multi taps when both key and modifiers are equal across taps.
				ref var el = ref History[index];
				if (el.Modifiers == mods && now <= el.LastReleaseExpireTime)
					count = (int)el.TapCount;
			}
			else if (index == FindResultAllExpired)
				index = 0;
			else
				index = FindUpdateIndex(now);

			lastTapExpireTime = now + MaxDurationBetweenTaps;
			History[index] = new KeyTapHistory(key, mods, lastTapExpireTime, count < MaxTapsPerKey ? ++count : count);
			return count;
		}

		public static int Info(Keycode key, Modifiers mods)
		{
			var now = DateTime.Now;
			var index = Find(key, now);
			if (index < 0)
				return 0;

			ref var el = ref History[index];
			return (el.Modifiers == mods && now <= el.LastReleaseExpireTime) ? el.TapCount : 0;
		}
	}
}
