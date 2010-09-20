#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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

		public static void Write(this Stream s, byte[] data)
		{
			s.Write(data, 0, data.Length);
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

		public static bool HasAttribute<T>(this MemberInfo mi)
		{
			return mi.GetCustomAttributes(typeof(T), true).Length != 0;
		}

		public static T[] GetCustomAttributes<T>( this MemberInfo mi, bool inherit )
			where T : class
		{
			return (T[])mi.GetCustomAttributes( typeof( T ), inherit );
		}

		public static T[] GetCustomAttributes<T>( this ParameterInfo mi )
			where T : class
		{
			return (T[])mi.GetCustomAttributes( typeof( T ), true );
		}
	}
}
