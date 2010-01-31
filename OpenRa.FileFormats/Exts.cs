using System;
using System.Collections.Generic;

namespace OpenRa
{
	public static class Exts
	{
		public static string F(this string fmt, params object[] args)
		{
			return string.Format(fmt, args);
		}

		public static void Do<T>( this IEnumerable<T> e, Action<T> fn )
		{
			foreach( var ee in e )
				fn( ee );
		}
	}
}
