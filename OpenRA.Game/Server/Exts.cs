#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

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
