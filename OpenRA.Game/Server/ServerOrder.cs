#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;

namespace OpenRA.Server
{
	class ServerOrder
	{
		public readonly OrderCode Order;
		public readonly string Data;

		public ServerOrder(OrderCode order, string data)
		{
			Order = order;
			Data = data;
		}

		public static ServerOrder Deserialize(BinaryReader r)
		{
			byte b;
			switch (b = r.ReadByte())
			{
				case 0xff:
					Console.WriteLine("This isn't a server order.");
					return null;

				case 0xfe:
					{
						var name = (OrderCode)r.ReadByte();
						var data = r.ReadString();

						return new ServerOrder(name, data);
					}

				default:
					throw new NotImplementedException(b.ToString("x2"));
			}
		}

		public byte[] Serialize()
		{
			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);

			bw.Write((byte)0xfe);
			bw.Write((byte)Order);
			bw.Write(Data);
			return ms.ToArray();
		}
	}
}
