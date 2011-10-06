#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Tao.Sdl;
using OpenRA;
using OpenRA.FileFormats;

public static class MultiTapDetection
{
	public static bool MultiTapDetected = false;
	public static string VirtualKeyNameOfDetectedMultiTap = "";
	public static int MouseButtonTapsCounted = 1;

	static Cache<string, TapHistory> KeyHistoryCache = new Cache<string, TapHistory>(_ => new TapHistory(DateTime.Now - TimeSpan.FromSeconds(1)));
	static Cache<byte, TapHistory> ClickHistoryCache = new Cache<byte, TapHistory>(_ => new TapHistory(DateTime.Now - TimeSpan.FromSeconds(1)));


	public static void DetectFromMouse(byte MBName, int2 xy)
	{
		var clickHistory = ClickHistoryCache[MBName];

		clickHistory.FirstRelease = clickHistory.SecondRelease;
		clickHistory.SecondRelease = clickHistory.ThirdRelease;
		clickHistory.ThirdRelease.First = DateTime.Now;
		clickHistory.ThirdRelease.Second = xy;

		TimeSpan DurationAfterSecondRelease = clickHistory.ThirdRelease.First - clickHistory.SecondRelease.First;
		TimeSpan DurationAfterFirstRelease = clickHistory.SecondRelease.First - clickHistory.FirstRelease.First;

		if ((DurationAfterSecondRelease.TotalMilliseconds < SystemInformation.DoubleClickTime)
			&& ((clickHistory.ThirdRelease.Second - clickHistory.SecondRelease.Second).Length < 4)
			&& ((clickHistory.ThirdRelease.Second - clickHistory.SecondRelease.Second).Length < 4))
		{
			MultiTapDetected = true;
			MouseButtonTapsCounted = 2;

			if ((DurationAfterFirstRelease.TotalMilliseconds < SystemInformation.DoubleClickTime)
				&& ((clickHistory.SecondRelease.Second - clickHistory.FirstRelease.Second).Length < 4)
				&& ((clickHistory.SecondRelease.Second - clickHistory.FirstRelease.Second).Length < 4))
			{
				MouseButtonTapsCounted = 3;
			}
		}
		else
		{
			MultiTapDetected = false;
			MouseButtonTapsCounted = 1;
		}

	}

	public static void DetectFromKeyboard(string KeyName)
	{
		var keyHistory = KeyHistoryCache[KeyName];

		keyHistory.FirstRelease = keyHistory.SecondRelease;
		keyHistory.SecondRelease = keyHistory.ThirdRelease;
		keyHistory.ThirdRelease.First = DateTime.Now;

		TimeSpan DurationAfterSecondRelease = keyHistory.ThirdRelease.First - keyHistory.SecondRelease.First;
		TimeSpan DurationAfterFirstRelease = keyHistory.SecondRelease.First - keyHistory.FirstRelease.First;

		if (DurationAfterSecondRelease.TotalMilliseconds < SystemInformation.DoubleClickTime)
		{
			MultiTapDetected = true;
			VirtualKeyNameOfDetectedMultiTap = "DoubleTapOf_" + KeyName;

			if (DurationAfterFirstRelease.TotalMilliseconds < SystemInformation.DoubleClickTime)
			{
				VirtualKeyNameOfDetectedMultiTap = "TripleTapOf_" + KeyName;
			}
		}
		else
		{
			MultiTapDetected = false;
			VirtualKeyNameOfDetectedMultiTap = "";
		}
	}
}

class TapHistory
{
	public Pair<DateTime, int2> FirstRelease;
	public Pair<DateTime, int2> SecondRelease;
	public Pair<DateTime, int2> ThirdRelease;

	public TapHistory(DateTime now)
	{
		this.FirstRelease.First = this.SecondRelease.First = this.ThirdRelease.First = now;
	}
}