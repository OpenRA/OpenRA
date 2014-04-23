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
using System.Linq;

namespace OpenRA.Support
{
	public class PerfTimer : IDisposable
	{
		readonly Stopwatch sw = new Stopwatch();
		readonly string Name;

		// Hacks to give the output a tree-like structure
		static System.Threading.ThreadLocal<int> depth = new System.Threading.ThreadLocal<int>();
		static System.Threading.ThreadLocal<string> prevHeader = new System.Threading.ThreadLocal<string>();

		public PerfTimer(string name)
		{
			if (prevHeader.Value != null)
			{
				Log.Write("perf", prevHeader.Value);
				prevHeader.Value = null;
			}

			this.Name = name;

			prevHeader.Value = string.Format("{0}{1}", Indentation, this.Name);
			depth.Value++;
		}

		private static string Indentation
		{
			get
			{
				var d = depth.Value;
				if (d == 1)
					return "|   ";
				else if (d <= 0)
					return string.Empty;
				else
					return string.Concat(Enumerable.Repeat("|   ", depth.Value));
			}
		}

		public void Dispose()
		{
			string format;
			if (prevHeader.Value == null)
			{
				format = "{0}: {2} ms";
			}
			else
			{
				format = "{0}{1}: {2} ms";
				prevHeader.Value = null;
			}
			depth.Value--;
			Log.Write("perf", format, Indentation, this.Name, Math.Round(this.sw.Elapsed.TotalMilliseconds));
		}
	}
}
