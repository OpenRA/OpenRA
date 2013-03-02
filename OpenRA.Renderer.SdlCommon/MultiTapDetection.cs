#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA;
using OpenRA.FileFormats;

public static class MultiTapDetection
{
	static Cache<string, TapHistory> KeyHistoryCache =
		new Cache<string, TapHistory>(_ => new TapHistory(DateTime.Now - TimeSpan.FromSeconds(1)));
	static Cache<byte, TapHistory> ClickHistoryCache =
		new Cache<byte, TapHistory>(_ => new TapHistory(DateTime.Now - TimeSpan.FromSeconds(1)));

	public static int DetectFromMouse(byte MBName, int2 xy)
	{
		var clickHistory = ClickHistoryCache[MBName];
		return clickHistory.GetTapCount(xy);
	}

	public static int InfoFromMouse(byte MBName)
	{
		var clickHistory = ClickHistoryCache[MBName];
		return clickHistory.LastTapCount();
	}

	public static int DetectFromKeyboard(string KeyName)
	{
		var keyHistory = KeyHistoryCache[KeyName];
		return keyHistory.GetTapCount(int2.Zero);
	}

	public static int InfoFromKeyboard(string KeyName)
	{
		var keyHistory = KeyHistoryCache[KeyName];
		return keyHistory.LastTapCount();
	}
}

class TapHistory
{
	public Pair<DateTime, int2> FirstRelease, SecondRelease, ThirdRelease;

	public TapHistory(DateTime now)
	{
		FirstRelease = SecondRelease = ThirdRelease = Pair.New( now, int2.Zero );
	}

	static bool CloseEnough(Pair<DateTime, int2> a, Pair<DateTime, int2> b)
	{
		return a.First - b.First < TimeSpan.FromMilliseconds( 250 )
			&& (a.Second - b.Second).Length < 4;
	}

	public int GetTapCount(int2 xy)
	{
		FirstRelease = SecondRelease;
		SecondRelease = ThirdRelease;
		ThirdRelease = Pair.New(DateTime.Now, xy);

		if (!CloseEnough(ThirdRelease, SecondRelease)) return 1;
		if (!CloseEnough(SecondRelease, FirstRelease)) return 2;
		return 3;
	}

	public int LastTapCount()
	{
		if (!CloseEnough(ThirdRelease, SecondRelease)) return 1;
		if (!CloseEnough(SecondRelease, FirstRelease)) return 2;
		return 3;
	}
}