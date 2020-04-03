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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Network
{
	public interface ILatencyReporter
	{
		int Latency { get; }
		double Jitter { get; }
		int PeakJitter { get; }
		bool IsLocal { get; }
	}

	public interface ILatencyTracker
	{
		void TrackSend(int frame);
		void TrackAck(int frame);
	}

	public class OrderLatencyTracker : ILatencyReporter, ILatencyTracker
	{
		const int MaxBufferSize = 1000;

		const int DefaultJitterLength = 1000;
		const int DefaultPeakJitterHold = 1000;

		struct JitterEntry
		{
			public long EntryTime;
			public int Jitter;
		}

		readonly ConcurrentQueue<JitterEntry> jitterEntries = new ConcurrentQueue<JitterEntry>();

		struct FrameToAck
		{
			public long EntryTime;
			public int Frame; // Not strictly required, used for sanity checking
		}

		readonly ConcurrentQueue<FrameToAck> framesToAck = new ConcurrentQueue<FrameToAck>();

		int lastLatency;
		readonly int jitterLength;
		readonly int peakJitterHold;

		public OrderLatencyTracker(int jitterLength = DefaultJitterLength, int peakJitterHold = DefaultPeakJitterHold)
		{
			this.jitterLength = jitterLength;
			this.peakJitterHold = peakJitterHold;
		}

		public void TrackSend(int frame)
		{
			if (framesToAck.Count > MaxBufferSize)
				return;

			framesToAck.Enqueue(new FrameToAck
			{
				EntryTime = Game.RunTime,
				Frame = frame
			});
		}

		public void TrackAck(int frame)
		{
			var currentTime = Game.RunTime;

			FrameToAck entryToAck;
			if (!framesToAck.TryDequeue(out entryToAck))
				return; // May happen if buffer is empty, if acks out of sync, an exception will get thrown later

			if (frame < entryToAck.Frame)
			{
				return; // We probably had to drop this frame to avoid overloading the buffer
			}

			if (frame > entryToAck.Frame)
			{
				throw new InvalidOperationException("Missed an ack");
			}

			var currentLatency = (int)(currentTime - entryToAck.EntryTime);

			jitterEntries.Enqueue(new JitterEntry
			{
				EntryTime = currentTime,
				Jitter = currentLatency - lastLatency
			});

			lastLatency = currentLatency;
		}

		public int Latency
		{
			get
			{
				FrameToAck frame;
				if (!framesToAck.TryPeek(out frame))
					return lastLatency;

				var currentLatency = (int)(Game.RunTime - frame.EntryTime);
				if (currentLatency > lastLatency)
				{
					return currentLatency;
				}
				else
					return lastLatency;
			}
		}

		void DropIrrelevantJitterPackets()
		{
			var absoluteCutoff = Game.RunTime - Math.Max(jitterLength, peakJitterHold);

			JitterEntry jitter;
			if (!jitterEntries.TryPeek(out jitter))
				return;

			while (jitter.EntryTime < absoluteCutoff)
			{
				JitterEntry dummy;
				if (!jitterEntries.TryDequeue(out dummy))
					return;
				if (!jitterEntries.TryPeek(out jitter))
					return;
			}
		}

		public double Jitter
		{
			get
			{
				DropIrrelevantJitterPackets();
				var currentLatency = Latency;
				var expiryTime = Game.RunTime - jitterLength;

				var selected = 0;
				var jitterSum = jitterEntries.Where(x => x.EntryTime >= expiryTime).Select(x =>
				{
					selected++;
					return Math.Abs(x.Jitter);
				}).Sum();

				var currentJitter = (double)(jitterSum + currentLatency - lastLatency) / (selected + 1);
				if (selected == 0)
					return currentJitter;

				var realJitter = (double)jitterSum / selected;
				return currentJitter > realJitter ? currentJitter : realJitter;
			}
		}

		public int PeakJitter
		{
			get
			{
				DropIrrelevantJitterPackets();

				var currentJitter = Latency - lastLatency;
				var expiryTime = Game.RunTime - peakJitterHold;

				return Math.Max(
					jitterEntries.Where(x => x.EntryTime >= expiryTime)
						.Select(x => x.Jitter).DefaultIfEmpty(0).Max(),
					currentJitter);
			}
		}

		public bool IsLocal
		{
			get { return false; }
		}
	}

	public class EmptyLatencyReporter : ILatencyReporter
	{
		public static readonly ILatencyReporter Instance = new EmptyLatencyReporter();

		public int Latency { get { return 0; } }
		public double Jitter { get { return 0; } }
		public int PeakJitter { get { return 0; } }
		public bool IsLocal { get { return true; } }
	}
}
