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
using System.Text;

namespace OpenRA
{
	public static class StreamExts
	{
		public static byte[] ReadBytes(this Stream s, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException("count", "Non-negative number required.");

			var buf = new byte[count];
			if (s.Read(buf, 0, count) < count)
				throw new EndOfStreamException();

			return buf;
		}

		public static int Peek(this Stream s)
		{
			var buf = new byte[1];
			if (s.Read(buf, 0, 1) == 0)
				return -1;

			s.Seek(s.Position - 1, SeekOrigin.Begin);
			return buf[0];
		}

		public static byte ReadUInt8(this Stream s)
		{
			return s.ReadBytes(1)[0];
		}

		public static ushort ReadUInt16(this Stream s)
		{
			return BitConverter.ToUInt16(s.ReadBytes(2), 0);
		}

		public static short ReadInt16(this Stream s)
		{
			return BitConverter.ToInt16(s.ReadBytes(2), 0);
		}

		public static uint ReadUInt32(this Stream s)
		{
			return BitConverter.ToUInt32(s.ReadBytes(4), 0);
		}

		public static int ReadInt32(this Stream s)
		{
			return BitConverter.ToInt32(s.ReadBytes(4), 0);
		}

		public static float ReadFloat(this Stream s)
		{
			return BitConverter.ToSingle(s.ReadBytes(4), 0);
		}

		public static double ReadDouble(this Stream s)
		{
			return BitConverter.ToDouble(s.ReadBytes(8), 0);
		}

		public static string ReadASCII(this Stream s, int length)
		{
			return new string(Encoding.ASCII.GetChars(s.ReadBytes(length)));
		}

		public static string ReadASCIIZ(this Stream s)
		{
			var bytes = new List<byte>();
			var buf = new byte[1];

			for (;;)
			{
				if (s.Read(buf, 0, 1) < 1)
					throw new EndOfStreamException();

				if (buf[0] == 0)
					break;

				bytes.Add(buf[0]);
			}

			return new string(Encoding.ASCII.GetChars(bytes.ToArray()));
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
