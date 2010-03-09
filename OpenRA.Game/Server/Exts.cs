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

using System.Collections.Generic;
using System.IO;
using System.Linq;

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
