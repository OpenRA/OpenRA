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
