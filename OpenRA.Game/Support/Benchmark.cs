#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
			public int Tick { get; private set; }
			public double Value { get; private set; }

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
				Log.AddChannel(name, "{0}{1}.csv".F(prefix, name));
				Log.Write(name, "tick,time [ms]");

				foreach (var point in sample.Value)
					Log.Write(name, "{0},{1}".F(point.Tick,  point.Value));
			}
		}

		public void Reset()
		{
			samples.Clear();
		}
	}
}
