using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Support
{
	public static class Timer
	{
		static Stopwatch sw = new Stopwatch();
		static double lastTime = 0;

		public static void Time( string message )
		{
			var time = sw.ElapsedTime();
			Log.Write( message, time - lastTime );
			lastTime = time;
		}
	}
}
