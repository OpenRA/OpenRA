#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.Support
{
	public class Stopwatch
	{
		System.Diagnostics.Stopwatch sw;
		public Stopwatch ()
		{
			Reset();
		}

		public double ElapsedTime()
		{
			return sw.Elapsed.TotalMilliseconds / 1000.0;
		}

		public void Reset()
		{
			sw = System.Diagnostics.Stopwatch.StartNew();
		}
	}
}
