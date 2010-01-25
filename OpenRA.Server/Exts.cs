using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace OpenRA.Server
{
	static class Exts
	{
		public static void Write( this Stream s, byte[] data )
		{
			s.Write( data, 0, data.Length );
		}

		public static byte[] Read( this Stream s, int len )
		{
			var data = new byte[ len ];
			s.Read( data, 0, len );
			return data;
		}

		public static IEnumerable<T> Except<T>( this IEnumerable<T> ts, T t )
		{
			return ts.Except( new[] { t } );
		}
	}
}
