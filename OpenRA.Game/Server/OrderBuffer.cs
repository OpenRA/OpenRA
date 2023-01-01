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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace OpenRA.Server
{
	public class OrderBuffer
	{
		const int NumberOfFrames = 20;
		const int Interval = 1000;

		// Limit the TickScale to maximum of 10%
		const float MaxTickScale = 1.1f;

		const int EmptyValue = -1;

		Stopwatch gameTimer;
		long nextUpdate = 0;

		readonly ConcurrentDictionary<int, long> timestamps = new ConcurrentDictionary<int, long>();
		readonly ConcurrentDictionary<int, Queue<long>> deltas = new ConcurrentDictionary<int, Queue<long>>();

		int timestep;
		int ticksPerInterval;
		int baselinePlayer;
		List<int> players;

		public void AddOrderTimestamp(int playerIndex)
		{
			timestamps[playerIndex] = gameTimer.ElapsedMilliseconds;

			if (timestamps.Values.All(t => t != EmptyValue))
			{
				var baseline = timestamps[baselinePlayer];

				foreach (var (p, q) in timestamps)
				{
					var dt = baseline - q;

					var playerDeltas = deltas[p];
					playerDeltas.Enqueue(dt);

					if (playerDeltas.Count > NumberOfFrames)
						playerDeltas.Dequeue();

					timestamps[p] = EmptyValue;
				}
			}
		}

		public void Start(GameSpeed gameSpeed, IEnumerable<int> players)
		{
			timestep = gameSpeed.Timestep;
			ticksPerInterval = Interval / timestep;

			this.players = players.ToList();
			baselinePlayer = this.players.First();

			foreach (var player in this.players)
			{
				timestamps.TryAdd(player, EmptyValue);
				deltas.TryAdd(player, new Queue<long>());
			}

			gameTimer = Stopwatch.StartNew();
			nextUpdate = gameTimer.ElapsedMilliseconds + Interval;
		}

		public IEnumerable<(int PlayerIndex, float TickScale)> GetTickScales()
		{
			var now = gameTimer.ElapsedMilliseconds;
			if (now < nextUpdate)
				yield break;

			nextUpdate = now + Interval;

			if (deltas.IsEmpty)
				yield break;

			if (deltas.Values.Any(q => q.Count != NumberOfFrames))
				yield break;

			var medians = deltas.Select(d => (PlayerIndex: d.Key, Delta: Median(d.Value.ToArray()))).ToList();

			// We need to check if we have a connection slower than our baseline and then use that as our offset.
			var minValue = medians.MinBy(p => p.Delta).Delta;
			var offset = minValue < 0 ? Math.Abs(minValue) : 0;

			foreach (var (playerIndex, delta) in medians)
			{
				var deltaPerTick = (delta + offset) / (float)ticksPerInterval;

				var tickScale = (timestep + deltaPerTick) / timestep;

				var adjustedTickScale = tickScale.Clamp(1f, MaxTickScale);

				yield return (playerIndex, adjustedTickScale);
			}
		}

		long Median(long[] a)
		{
			Array.Sort(a);
			var n = a.Length;

			if (n % 2 != 0)
				return a[n / 2];

			return (a[(n - 1) / 2] + a[n / 2]) / 2;
		}

		public void RemovePlayer(int player)
		{
			players.Remove(player);
			if (player == baselinePlayer && players.Count > 0)
			{
				var newBaseline = players.First();
				Interlocked.Exchange(ref baselinePlayer, newBaseline);
			}

			timestamps.TryRemove(player, out _);
			deltas.TryRemove(player, out _);
		}
	}
}
