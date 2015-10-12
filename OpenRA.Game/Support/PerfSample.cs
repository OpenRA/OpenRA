﻿#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using OpenRA.Primitives;

namespace OpenRA.Support
{
	public struct PerfSample : IDisposable
	{
		readonly string item;
		readonly long ticks;

		public PerfSample(string item)
		{
			this.item = item;
			ticks = Stopwatch.GetTimestamp();
		}

		public void Dispose()
		{
			PerfHistory.Increment(item, 1000.0 * Math.Max(0, Stopwatch.GetTimestamp() - ticks) / Stopwatch.Frequency);
		}
	}
}