using System.Runtime.InteropServices;

namespace OpenRa.Support
{
	public class Stopwatch
	{
		[DllImport("kernel32.dll")]
		static extern bool QueryPerformanceCounter(out long value);
		[DllImport("kernel32.dll")]
		static extern bool QueryPerformanceFrequency(out long frequency);

		long freq, start;

		public Stopwatch()
		{
			QueryPerformanceFrequency(out freq);
			QueryPerformanceCounter(out start);
		}

		public double ElapsedTime()
		{
			long current;
			QueryPerformanceCounter(out current);

			return (current - start) / (double)freq;
		}

		public void Reset()
		{
			QueryPerformanceCounter(out start);
		}
	}
}
