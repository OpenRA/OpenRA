#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenRA
{
	public static class StreamExts
	{
		public static byte[] ReadBytes(this Stream s, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
			var buffer = new byte[count];
			s.ReadBytes(buffer, 0, count);
			return buffer;
		}

		public static void ReadBytes(this Stream s, byte[] buffer, int offset, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
			while (count > 0)
			{
				int bytesRead;
				if ((bytesRead = s.Read(buffer, offset, count)) == 0)
					throw new EndOfStreamException();
				offset += bytesRead;
				count -= bytesRead;
			}
		}

		public static int Peek(this Stream s)
		{
			var b = s.ReadByte();
			if (b == -1)
				return -1;
			s.Seek(-1, SeekOrigin.Current);
			return (byte)b;
		}

		public static byte ReadUInt8(this Stream s)
		{
			var b = s.ReadByte();
			if (b == -1)
				throw new EndOfStreamException();
			return (byte)b;
		}

		public static ushort ReadUInt16(this Stream s)
		{
			return (ushort)(s.ReadUInt8() | s.ReadUInt8() << 8);
		}

		public static short ReadInt16(this Stream s)
		{
			return (short)(s.ReadUInt8() | s.ReadUInt8() << 8);
		}

		public static uint ReadUInt32(this Stream s)
		{
			return (uint)(s.ReadUInt8() | s.ReadUInt8() << 8 | s.ReadUInt8() << 16 | s.ReadUInt8() << 24);
		}

		public static int ReadInt32(this Stream s)
		{
			return s.ReadUInt8() | s.ReadUInt8() << 8 | s.ReadUInt8() << 16 | s.ReadUInt8() << 24;
		}

		public static void Write(this Stream s, int value)
		{
			s.WriteArray(BitConverter.GetBytes(value));
		}

		public static void Write(this Stream s, float value)
		{
			s.WriteArray(BitConverter.GetBytes(value));
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
			byte b;
			while ((b = s.ReadUInt8()) != 0)
				bytes.Add(b);
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
				if (s.CanSeek)
					return s.ReadBytes((int)(s.Length - s.Position));

				var bytes = new List<byte>();
				var buffer = new byte[1024];
				int count;
				while ((count = s.Read(buffer, 0, buffer.Length)) > 0)
					bytes.AddRange(buffer.Take(count));
				return bytes.ToArray();
			}
		}

		// Note: renamed from Write() to avoid being aliased by
		// System.IO.Stream.Write(System.ReadOnlySpan) (which is not implemented in Mono)
		public static void WriteArray(this Stream s, byte[] data)
		{
			s.Write(data, 0, data.Length);
		}

		public static IEnumerable<string> ReadAllLines(this Stream s)
		{
			string line;
			using (var sr = new StreamReader(s))
				while ((line = sr.ReadLine()) != null)
					yield return line;
		}

		/// <summary>
		/// Streams each line of characters from a stream, exposing the line as <see cref="ReadOnlyMemory{T}"/>.
		/// The memory lifetime is only valid during that iteration. Advancing the iteration invalidates the memory.
		/// Consumers should call <see cref="ReadOnlyMemory{T}.Span"/> on each line and otherwise avoid operating on
		/// the memory to ensure they meet the lifetime restrictions.
		/// </summary>
		public static IEnumerable<ReadOnlyMemory<char>> ReadAllLinesAsMemory(this Stream s)
		{
			var buffer = ArrayPool<char>.Shared.Rent(128);
			try
			{
				using (var sr = new StreamReader(s))
				{
					var offset = 0;
					int read;
					while ((read = sr.ReadBlock(buffer, offset, buffer.Length - offset)) != 0)
					{
						offset += read;

						var consumedIndex = 0;
						int newlineIndex;
						while ((newlineIndex = Array.IndexOf(buffer, '\n', offset - read, read)) != -1)
						{
							if (newlineIndex > 0 && buffer[newlineIndex - 1] == '\r')
								yield return buffer.AsMemory(consumedIndex, newlineIndex - consumedIndex - 1);
							else
								yield return buffer.AsMemory(consumedIndex, newlineIndex - consumedIndex);

							var afterNewlineIndex = newlineIndex + 1;
							read = offset - afterNewlineIndex;
							consumedIndex = afterNewlineIndex;
						}

						if (consumedIndex > 0)
						{
							Array.Copy(buffer, consumedIndex, buffer, 0, offset - consumedIndex);
							offset = read;
						}

						if (offset == buffer.Length)
						{
							var newBuffer = ArrayPool<char>.Shared.Rent(buffer.Length * 2);
							Array.Copy(buffer, newBuffer, buffer.Length);
							ArrayPool<char>.Shared.Return(buffer);
							buffer = newBuffer;
						}
					}

					if (offset > 0)
						yield return buffer.AsMemory(0, offset);
				}
			}
			finally
			{
				ArrayPool<char>.Shared.Return(buffer);
			}
		}

		// The string is assumed to be length-prefixed, as written by WriteString()
		public static string ReadString(this Stream s, Encoding encoding, int maxLength)
		{
			var length = s.ReadInt32();
			if (length > maxLength)
				throw new InvalidOperationException($"The length of the string ({length}) is longer than the maximum allowed ({maxLength}).");

			return encoding.GetString(s.ReadBytes(length));
		}

		// Writes a length-prefixed string using the specified encoding and returns
		// the number of bytes written.
		public static int WriteString(this Stream s, Encoding encoding, string text)
		{
			byte[] bytes;

			if (!string.IsNullOrEmpty(text))
				bytes = encoding.GetBytes(text);
			else
				bytes = Array.Empty<byte>();

			s.Write(bytes.Length);
			s.WriteArray(bytes);

			return 4 + bytes.Length;
		}
	}
}
