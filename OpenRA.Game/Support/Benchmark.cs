#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Support
{
	class Benchmark
	{
		readonly string prefix;
		readonly Dictionary<string, List<BenchmarkPoint>> samples = new Dictionary<string, List<BenchmarkPoint>>();

		public Benchmark(string prefix)
		{
			this.prefix = prefix;
		}

		public void Tick(int localTick)
		{
			foreach (var item in PerfHistory.Items)
				samples.GetOrAdd(item.Key).Add(new BenchmarkPoint(localTick, item.Value.LastValue));
		}

		class BenchmarkPoint
		{
			public int Tick { get; }
			public double Value { get; }

			public BenchmarkPoint(int tick, double value)
			{
				Tick = tick;
				Value = value;
			}
		}

		public void Write()
		{
			foreach (var sample in samples)
			{
				var name = sample.Key;
				Log.AddChannel(name, $"{prefix}{name}.csv");
				Log.Write(name, "tick,time [ms]");

				foreach (var point in sample.Value)
					Log.Write(name, $"{point.Tick},{point.Value}");
			}
		}

		public void Reset()
		{
			samples.Clear();
		}
	}
}
