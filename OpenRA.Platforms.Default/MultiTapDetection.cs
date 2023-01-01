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

using System;
using OpenRA.Primitives;

namespace OpenRA.Platforms.Default
{
	static class MultiTapDetection
	{
		static readonly Cache<(Keycode Key, Modifiers Mods), TapHistory> KeyHistoryCache =
			new Cache<(Keycode, Modifiers), TapHistory>(_ => new TapHistory(DateTime.Now - TimeSpan.FromSeconds(1)));
		static readonly Cache<byte, TapHistory> ClickHistoryCache =
			new Cache<byte, TapHistory>(_ => new TapHistory(DateTime.Now - TimeSpan.FromSeconds(1)));

		public static int DetectFromMouse(byte button, int2 xy)
		{
			return ClickHistoryCache[button].GetTapCount(xy);
		}

		public static int InfoFromMouse(byte button)
		{
			return ClickHistoryCache[button].LastTapCount();
		}

		public static int DetectFromKeyboard(Keycode key, Modifiers mods)
		{
			return KeyHistoryCache[(key, mods)].GetTapCount(int2.Zero);
		}

		public static int InfoFromKeyboard(Keycode key, Modifiers mods)
		{
			return KeyHistoryCache[(key, mods)].LastTapCount();
		}
	}

	class TapHistory
	{
		public (DateTime Time, int2 Location) FirstRelease, SecondRelease, ThirdRelease;

		public TapHistory(DateTime now)
		{
			FirstRelease = SecondRelease = ThirdRelease = (now, int2.Zero);
		}

		static bool CloseEnough((DateTime Time, int2 Location) a, (DateTime Time, int2 Location) b)
		{
			return a.Time - b.Time < TimeSpan.FromMilliseconds(250)
				&& (a.Location - b.Location).Length < 4;
		}

		public int GetTapCount(int2 xy)
		{
			FirstRelease = SecondRelease;
			SecondRelease = ThirdRelease;
			ThirdRelease = (DateTime.Now, xy);

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
