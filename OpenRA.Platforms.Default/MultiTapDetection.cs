#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Primitives;

namespace OpenRA.Platforms.Default
{
	static class MultiTapDetection
	{
		static Cache<Keycode, TapHistory> keyHistoryCache =
			new Cache<Keycode, TapHistory>(_ => new TapHistory(DateTime.Now - TimeSpan.FromSeconds(1)));
		static Cache<byte, TapHistory> clickHistoryCache =
			new Cache<byte, TapHistory>(_ => new TapHistory(DateTime.Now - TimeSpan.FromSeconds(1)));

		public static int DetectFromMouse(byte button, int2 xy)
		{
			return clickHistoryCache[button].GetTapCount(xy);
		}

		public static int InfoFromMouse(byte button)
		{
			return clickHistoryCache[button].LastTapCount();
		}

		public static int DetectFromKeyboard(Keycode key)
		{
			return keyHistoryCache[key].GetTapCount(int2.Zero);
		}

		public static int InfoFromKeyboard(Keycode key)
		{
			return keyHistoryCache[key].LastTapCount();
		}
	}

	class TapHistory
	{
		public Pair<DateTime, int2> FirstRelease, SecondRelease, ThirdRelease;

		public TapHistory(DateTime now)
		{
			FirstRelease = SecondRelease = ThirdRelease = Pair.New(now, int2.Zero);
		}

		static bool CloseEnough(Pair<DateTime, int2> a, Pair<DateTime, int2> b)
		{
			return a.First - b.First < TimeSpan.FromMilliseconds(250)
				&& (a.Second - b.Second).Length < 4;
		}

		public int GetTapCount(int2 xy)
		{
			FirstRelease = SecondRelease;
			SecondRelease = ThirdRelease;
			ThirdRelease = Pair.New(DateTime.Now, xy);

			if (!CloseEnough(ThirdRelease, SecondRelease))
				return 1;
			if (!CloseEnough(SecondRelease, FirstRelease))
				return 2;

			return 3;
		}

		public int LastTapCount()
		{
			if (!CloseEnough(ThirdRelease, SecondRelease))
				return 1;
			if (!CloseEnough(SecondRelease, FirstRelease))
				return 2;

			return 3;
		}
	}
}