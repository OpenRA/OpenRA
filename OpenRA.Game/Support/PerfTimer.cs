#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;

namespace OpenRA.Support
{
	public class PerfTimer : IDisposable
	{
		readonly Stopwatch sw = new Stopwatch();
		readonly string Name;
		static System.Threading.ThreadLocal<int> depth = new System.Threading.ThreadLocal<int>();

		public PerfTimer(string name)
		{
			this.Name = name;
			depth.Value++;
		}

		public void Dispose()
		{
			string indentation;

			if (--depth.Value >= 0)
				indentation = new string('\t', depth.Value);
			else
			{
				depth.Value = 0;
				indentation = string.Empty;
			}

			Log.Write("perf", "{0}{1}: {2} ms", indentation, this.Name, Math.Round(this.sw.Elapsed.TotalMilliseconds));
		}
	}
}
