using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

namespace OpenRa.Game
{
	class Packet : IComparable<Packet>
	{
		IPEndPoint address;
		int frame;
		byte[] data;

		public int Frame { get { return frame; } }

		Packet(IPEndPoint address, byte[] data)
		{
			this.address = address;

			using (MemoryStream ms = new MemoryStream(data))
			using (BinaryReader reader = new BinaryReader(ms))
			{
				frame = reader.ReadInt32();
				this.data = reader.ReadBytes(data.Length - 4);
			}
		}

		public static Packet FromReceivedData(IPEndPoint sender, byte[] data) { return new Packet(sender, data); }

		public int CompareTo(Packet other) { return frame.CompareTo(other.frame); }
	}
}
