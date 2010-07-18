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
	public static class Timer
	{
		static Stopwatch sw = new Stopwatch();
		static double lastTime = 0;

		public static void Time( string message )
		{
			var time = sw.ElapsedTime();
			var dt = time - lastTime;
			if( dt > 0.0001 )
				Log.Write("perf", message, dt );
			lastTime = time;
		}
	}
}
