#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace OpenRA
{
	public static class StreamExts
	{
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
			{
				for (;;)
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
}
