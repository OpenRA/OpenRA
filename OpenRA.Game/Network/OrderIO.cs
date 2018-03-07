#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;

namespace OpenRA.Network
{
	public static class OrderIO
	{
		public static List<Order> ToOrderList(this byte[] bytes, World world)
		{
			var ms = new MemoryStream(bytes, 4, bytes.Length - 4);
			var reader = new BinaryReader(ms);
			var ret = new List<Order>();
			while (ms.Position < ms.Length)
			{
				var o = Order.Deserialize(world, reader);
				if (o != null)
					ret.Add(o);
			}

			return ret;
		}

		public static byte[] SerializeSync(int sync)
		{
			var ms = new MemoryStream(1 + 4);
			using (var writer = new BinaryWriter(ms))
			{
				writer.Write((byte)0x65);
				writer.Write(sync);
			}

			return ms.GetBuffer();
		}

		public static int2 ReadInt2(this BinaryReader r)
		{
			var x = r.ReadInt32();
			var y = r.ReadInt32();
			return new int2(x, y);
		}

		public static void Write(this BinaryWriter w, int2 p)
		{
			w.Write(p.X);
			w.Write(p.Y);
		}

		public static void Write(this BinaryWriter w, CPos cell)
		{
			w.Write(cell.X);
			w.Write(cell.Y);
			w.Write(cell.Layer);
		}

		public static void Write(this BinaryWriter w, WPos pos)
		{
			w.Write(pos.X);
			w.Write(pos.Y);
			w.Write(pos.Z);
		}
	}
}
