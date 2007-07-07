namespace OpenRa.Game
{
	/// <summary>
	/// Provides access to the current time, frame time, and frame rate.
	/// </summary>
	public static class Clock
	{
		/// <summary>
		/// Performs one-time initialization for the clock service
		/// </summary>
		static Clock()
		{
			startTime = System.Environment.TickCount;
			StartFrame();
		}

		public static void Reset()
		{
			frameCount = 0;
			lastFrameCount = 0;
			lastFrameRate = 0;
			startTime = System.Environment.TickCount;
			StartFrame();
		}

		#region StartFrame

		/// <summary>
		/// Indicates to the clock that a new frame has begun. This should be called exactly once per frame.
		/// </summary>
		public static void StartFrame() 
		{
			int ticks = System.Environment.TickCount - startTime;
			frameStartTime = ticks / 1000.0;

			frameCount++;

			if( Time > nextFrameRateUpdateTime )
			{
				// set next update time
				nextFrameRateUpdateTime += (1.0 / FramerateUpdateFrequency);
				// average between last and current frame, to make spikes less severe
				const int OldFramerateWeight = 10;
				const int NewFramerateWeight = 1;
				int newFrameRate = (frameCount - lastFrameCount) * FramerateUpdateFrequency;
				lastFrameRate = (lastFrameRate * OldFramerateWeight + NewFramerateWeight * newFrameRate) / (OldFramerateWeight + NewFramerateWeight );
				lastFrameCount = frameCount;
			}
		}

		#endregion

		#region Private Implementation Details

		// number of framerate updates per second
		private const int FramerateUpdateFrequency = 10;
		
		// the time at which the application started
		private static int startTime;
		// the time at which this frame began
		private static double frameStartTime;
		// total number of frames rendered
		private static int frameCount = 0;
		// next time to update fps count
		private static double nextFrameRateUpdateTime = 0;
		// frame count at most recent fps calculation
		private static int lastFrameCount = 0;
		// most recently calculated fps
		private static int lastFrameRate = 0;

		#endregion

		#region Properties

		/// <summary>
		/// The time the current frame started
		/// </summary>
		public static double Time 
		{
			get { return frameStartTime; }
		}

		/// <summary>
		/// The number of frames rendered since the engine started
		/// </summary>
		public static int FrameCount
		{
			get { return frameCount; }
		}

		/// <summary>
		/// The most recent frame-rate
		/// </summary>
		public static int FrameRate
		{
			get { return lastFrameRate; }
		}

		/// <summary>
		/// The frame time corresponding to the most recent frame-rate
		/// </summary>
		public static double FrameTime
		{
			get { return 1.0 / lastFrameRate; }
		}

		#endregion
	}
}
