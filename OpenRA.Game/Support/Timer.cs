#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Support
{
	public static class Timer
	{
		static Stopwatch sw = new Stopwatch();
		static System.TimeSpan lastTime;

		public static void Time( string message )
		{
			var time = sw.Elapsed;
			var dt = time - lastTime;
			if( dt.TotalSeconds > 0.0001 )
				Log.Write("perf", message, dt.TotalSeconds );
			lastTime = time;
		}
	}
}
