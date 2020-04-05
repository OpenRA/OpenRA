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
using System.Collections.Generic;

namespace OpenRA.Support
{
	public class NetHistory
	{
		public const int DefaultNetHistoryLength = 100;

		public static int CurrentClientId { get; protected set; }
		public static int NetHistoryLength { get; protected set; }

		private static int head = 0, currentLength;

		private static NetHistoryFrame[] frameHistory;

		public static void Restart(int currentClientId, int historyLength = DefaultNetHistoryLength)
		{
			CurrentClientId = currentClientId;
			NetHistoryLength = historyLength;
			frameHistory = new NetHistoryFrame[NetHistoryLength];
			head = 0;
			currentLength = 0;
		}

		public static void Tick(NetHistoryFrame netHistoryFrame)
		{
			frameHistory[head] = netHistoryFrame;
			if (++head >= NetHistoryLength)
				head = 0;
			if (++currentLength >= NetHistoryLength)
				currentLength = NetHistoryLength - 1;
		}

		public static IEnumerable<NetHistoryFrame> GetHistory()
		{
			for (int i = head - 1; i >= head - currentLength; i--)
				yield return frameHistory[i < 0 ? i + NetHistoryLength : i];
		}
	}

	public class NetHistoryFrame
	{
		public int NetFrameNumber;
		public Dictionary<int, int> ClientBufferSizes;
		public bool Ticked;
		public int CatchUpNetFrames;
		public int MeasuredLatency;
		public double MeasuredJitter;
		public int PeakJitter;

		public int CurrentClientBufferSize
		{
			get { return ClientBufferSizes[NetHistory.CurrentClientId]; }
		}

		public NetHistoryFrame(int netFrameNumber,
			bool ticked,
			int catchUpNetFrames,
			Dictionary<int, int> clientBufferSizes,
			int measuredLatency,
			double measuredJitter,
			int peakJitter)
		{
			NetFrameNumber = netFrameNumber;
			Ticked = ticked;
			CatchUpNetFrames = catchUpNetFrames;
			ClientBufferSizes = clientBufferSizes;
			MeasuredLatency = measuredLatency;
			MeasuredJitter = measuredJitter;
			PeakJitter = peakJitter;
		}
	}
}
