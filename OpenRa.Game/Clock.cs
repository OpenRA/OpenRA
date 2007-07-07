using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace OpenRa.Game
{
	public static class Clock
	{
		[DllImport("kernel32.dll")]
		static extern bool QueryPerformanceCounter(out long value);
		[DllImport("kernel32.dll")]
		static extern bool QueryPerformanceFrequency(out long frequency);

		static int frameCount = 0;
		static long frequency;
		static long lastTime;
		static double frameTime = 0;
		static double totalTime = 0;
		const int FramerateUpdateFrequency = 10;
		static int lastFrameRate = 0;
		static int lastFrameCount = 0;
		static double nextFrameRateUpdateTime = 0;

		static Clock()
		{
			QueryPerformanceFrequency(out frequency);
			QueryPerformanceCounter(out lastTime);
		}

		public static void Reset()
		{
			totalTime = 0;
		}

		public static void StartFrame()
		{
			long time;
			QueryPerformanceCounter(out time);

			frameTime = (double)(time - lastTime) / (double)frequency;
			totalTime += frameTime;

			lastTime = time;

			frameCount++;

			if (totalTime > nextFrameRateUpdateTime)
			{
				nextFrameRateUpdateTime += (1.0 / FramerateUpdateFrequency);
				const int OldFramerateWeight = 20;
				const int NewFramerateWeight = 1;
				int newFrameRate = (frameCount - lastFrameCount) * FramerateUpdateFrequency;
				lastFrameRate = (lastFrameRate * OldFramerateWeight + NewFramerateWeight * newFrameRate) 
					/ (OldFramerateWeight + NewFramerateWeight);
				lastFrameCount = frameCount;
			}
		}

		public static double Time
		{
			get { return totalTime; }
		}

		public static int FrameRate
		{
			get { return lastFrameRate; }
		}

		public static double FrameTime
		{
			get { return frameTime; }
		}
	}
}
