#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;

namespace OpenRA.Server
{
	class ServerOrder
	{
		public readonly string Name;
		public readonly string Data;

		public ServerOrder(string name, string data)
		{
			Name = name;
			Data = data;
		}

		public static ServerOrder Deserialize(BinaryReader r)
		{
			byte b;
			switch (b = r.ReadByte())
			{
				case 0xbf:
				case 0xff:
					Console.WriteLine("This isn't a server order.");
					return null;

				case 0xfe:
					{
						var name = r.ReadString();
						var data = r.ReadString();

						return new ServerOrder(name, data);
					}

				default:
					throw new NotImplementedException(b.ToString("x2"));
			}
		}

		public byte[] Serialize()
		{
			var ms = new MemoryStream(1 + Name.Length + 1 + Data.Length + 1);
			var bw = new BinaryWriter(ms);

			bw.Write((byte)0xfe);
			bw.Write(Name);
			bw.Write(Data);
			return ms.ToArray();
		}
	}
}
