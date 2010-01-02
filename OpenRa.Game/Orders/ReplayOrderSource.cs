using System;
using System.Collections.Generic;
using System.IO;

namespace OpenRa.Game.Orders
{
	class ReplayOrderSource : IOrderSource
	{
		BinaryReader replayReader;
		public ReplayOrderSource(string replayFilename)
		{
			replayReader = new BinaryReader(File.Open(replayFilename, FileMode.Open));
		}

		public void SendLocalOrders(int localFrame, List<Order> localOrders) { }

		public List<Order> OrdersForFrame(int frameNumber)
		{
			if (frameNumber == 0)
				return new List<Order>();

			try
			{
				var len = replayReader.ReadInt32() - 4;
				var frame = replayReader.ReadInt32();
				var ret = replayReader.ReadBytes(len).ToOrderList();

				if (frameNumber != frame)
					throw new InvalidOperationException("Attempted time-travel in OrdersForFrame (replay)");

				return ret;
			}
			catch (EndOfStreamException)
			{
				return new List<Order>();
			}
		}

		public bool IsReadyForFrame(int frameNumber)
		{
			return true;
		}
	}
}
