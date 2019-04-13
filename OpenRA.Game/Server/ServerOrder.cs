#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
		public readonly uint ExtraData;

		public ServerOrder(string name, string data, uint extraData = 0)
		{
			Name = name;
			Data = data;
			ExtraData = extraData;
		}

		public static ServerOrder Deserialize(BinaryReader r)
		{
			byte b;
			switch (b = r.ReadByte())
			{
				case 0xbf:
					// Silently ignore disconnect notifications
					return null;
				case 0xff:
					Console.WriteLine("This isn't a server order.");
					return null;

				case 0xfe:
					{
						var name = r.ReadString();
						var flags = (OrderFields)r.ReadByte();
						var data = flags.HasField(OrderFields.TargetString) ? r.ReadString() : null;
						var extraData = flags.HasField(OrderFields.ExtraData) ? r.ReadUInt32() : 0;

						return new ServerOrder(name, data, extraData);
					}

				default:
					throw new NotImplementedException(b.ToString("x2"));
			}
		}

		public byte[] Serialize()
		{
			var ms = new MemoryStream(1 + Name.Length + 1 + 1 + Data.Length + 1 + 4);
			var bw = new BinaryWriter(ms);

			OrderFields fields = 0;
			if (Data != null)
				fields |= OrderFields.TargetString;

			if (ExtraData != 0)
				fields |= OrderFields.ExtraData;

			bw.Write((byte)0xfe);
			bw.Write(Name);
			bw.Write((byte)fields);

			if (fields.HasField(OrderFields.TargetString))
				bw.Write(Data);

			if (fields.HasField(OrderFields.ExtraData))
				bw.Write(ExtraData);

			return ms.ToArray();
		}
	}
}
