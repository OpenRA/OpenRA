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
using System.Runtime.InteropServices;
using System.Text;

namespace OpenRA
{
	public static class StreamExts
	{
		public static void ReadBytes(this Stream s, Span<byte> dest)
		{
			while (dest.Length > 0)
			{
				var bytesRead = s.Read(dest);
				if (bytesRead == 0)
					throw new EndOfStreamException();

				dest = dest[bytesRead..];
			}
		}

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
				var bytesRead = s.Read(buffer, offset, count);
				if (bytesRead == 0)
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
			Span<byte> buffer = stackalloc byte[2];
			s.ReadBytes(buffer);
			return BitConverter.ToUInt16(buffer);
		}

		public static short ReadInt16(this Stream s)
		{
			Span<byte> buffer = stackalloc byte[2];
			s.ReadBytes(buffer);
			return BitConverter.ToInt16(buffer);
		}

		public static uint ReadUInt32(this Stream s)
		{
			Span<byte> buffer = stackalloc byte[4];
			s.ReadBytes(buffer);
			return BitConverter.ToUInt32(buffer);
		}

		public static int ReadInt32(this Stream s)
		{
			Span<byte> buffer = stackalloc byte[4];
			s.ReadBytes(buffer);
			return BitConverter.ToInt32(buffer);
		}

		public static void Write(this Stream s, int value)
		{
			Span<byte> buffer = stackalloc byte[4];
			BitConverter.TryWriteBytes(buffer, value);
			s.Write(buffer);
		}

		public static void Write(this Stream s, long value)
		{
			Span<byte> buffer = stackalloc byte[8];
			BitConverter.TryWriteBytes(buffer, value);
			s.Write(buffer);
		}

		public static void Write(this Stream s, ulong value)
		{
			Span<byte> buffer = stackalloc byte[8];
			BitConverter.TryWriteBytes(buffer, value);
			s.Write(buffer);
		}

		public static void Write(this Stream s, float value)
		{
			Span<byte> buffer = stackalloc byte[4];
			BitConverter.TryWriteBytes(buffer, value);
			s.Write(buffer);
		}

		public static float ReadSingle(this Stream s)
		{
			Span<byte> buffer = stackalloc byte[4];
			s.ReadBytes(buffer);
			return BitConverter.ToSingle(buffer);
		}

		public static double ReadDouble(this Stream s)
		{
			Span<byte> buffer = stackalloc byte[8];
			s.ReadBytes(buffer);
			return BitConverter.ToDouble(buffer);
		}

		public static string ReadASCII(this Stream s, int length)
		{
			Span<byte> buffer = length < 128 ? stackalloc byte[length] : new byte[length];
			s.ReadBytes(buffer);
			return Encoding.ASCII.GetString(buffer);
		}

		public static string ReadASCIIZ(this Stream s)
		{
			var bytes = new List<byte>();
			byte b;
			while ((b = s.ReadUInt8()) != 0)
				bytes.Add(b);

			return Encoding.ASCII.GetString(CollectionsMarshal.AsSpan(bytes));
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
					while ((read = sr.Read(buffer, offset, buffer.Length - offset)) != 0)
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

		/// <summary>
		/// The string is assumed to be length-prefixed, as written by <see cref="WriteLengthPrefixedString"/>.
		/// </summary>
		public static string ReadLengthPrefixedString(this Stream s, Encoding encoding, int maxLength)
		{
			var length = s.ReadInt32();
			if (length > maxLength)
				throw new InvalidOperationException($"The length of the string ({length}) is longer than the maximum allowed ({maxLength}).");

			Span<byte> buffer = length < 128 ? stackalloc byte[length] : new byte[length];
			s.ReadBytes(buffer);
			return encoding.GetString(buffer);
		}

		/// <summary>
		/// Writes a length-prefixed string using the specified encoding and returns the number of bytes written.
		/// </summary>
		public static int WriteLengthPrefixedString(this Stream s, Encoding encoding, string text)
		{
			byte[] bytes;

			if (!string.IsNullOrEmpty(text))
				bytes = encoding.GetBytes(text);
			else
				bytes = [];

			s.Write(bytes.Length);
			s.Write(bytes);

			return 4 + bytes.Length;
		}
	}
}
