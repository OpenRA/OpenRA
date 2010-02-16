#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRa.Network
{
	static class OrderIO
	{
		public static void Write(this Stream s, byte[] buf)
		{
			s.Write(buf, 0, buf.Length);
		}

		public static void WriteFrameData(this Stream s, IEnumerable<Order> orders, int frameNumber)
		{
			var bytes = Serialize( orders, frameNumber );
			s.Write( BitConverter.GetBytes( (int)bytes.Length ) );
			s.Write( bytes );
		}

		public static byte[] Serialize( this IEnumerable<Order> orders, int frameNumber )
		{
			var ms = new MemoryStream();
			ms.Write( BitConverter.GetBytes( frameNumber ) );
			foreach( var o in orders.Select( o => o.Serialize() ) )
				ms.Write( o );
			return ms.ToArray();
		}

		public static List<Order> ToOrderList(this byte[] bytes, World world)
		{
			var ms = new MemoryStream(bytes, 4, bytes.Length - 4);
			var reader = new BinaryReader(ms);
			var ret = new List<Order>();
			while( ms.Position < ms.Length )
			{
				var o = Order.Deserialize( world, reader );
				if( o != null )
					ret.Add( o );
			}
			return ret;
		}

		public static byte[] SerializeSync( this List<int> sync, int frameNumber )
		{
			var ms = new MemoryStream();
			using( var writer = new BinaryWriter( ms ) )
			{
				writer.Write( frameNumber );
				writer.Write( (byte)0x65 );
				foreach( var s in sync )
					writer.Write( s );
			}
			return ms.ToArray();
		}
	}
}
