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
using System.Reflection;
using System.Linq;
using System.IO;

namespace OpenRA
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

		public static IEnumerable<string> GetNamespaces(this Assembly a)
		{
			return a.GetTypes().Select(t => t.Namespace).Distinct().Where(n => n != null);
		}

		public static string ReadAllText(this Stream s)
		{
			using (s)
			using (var sr = new StreamReader(s))
				return sr.ReadToEnd();
		}

		public static byte[] ReadAllBytes(this Stream s)
		{
			using (s)
			{
				var data = new byte[s.Length - s.Position];
				s.Read(data, 0, data.Length);
				return data;
			}
		}

		public static IEnumerable<string> ReadAllLines(this Stream s)
		{
			using (var sr = new StreamReader(s))
				for (; ; )
				{
					var line = sr.ReadLine();
					if (line == null)
						yield break;
					else
						yield return line;
				}
		}
	}
}
