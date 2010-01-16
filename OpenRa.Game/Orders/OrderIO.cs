using System;
using System.Collections.Generic;
using System.IO;

namespace OpenRa.Orders
{
	static class OrderIO
	{
		static void Write(this Stream s, byte[] buf)
		{
			s.Write(buf, 0, buf.Length);
		}

		public static void WriteFrameData(this Stream s, IEnumerable<Order> orders, int frameNumber)
		{
			var ms = new MemoryStream();
			ms.Write(BitConverter.GetBytes(frameNumber));
			foreach (var order in orders)
				ms.Write(order.Serialize());

			s.Write(BitConverter.GetBytes((int)ms.Length));
			ms.WriteTo(s);
		}

		public static List<Order> ToOrderList(this byte[] bytes)
		{
			var ms = new MemoryStream(bytes);
			var reader = new BinaryReader(ms);
			var ret = new List<Order>();
			while (ms.Position < ms.Length)
				ret.Add(Order.Deserialize(reader));
			return ret;
		}
	}
}
