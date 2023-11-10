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
using System.IO;

namespace OpenRA.Primitives
{
	public class MergedStream : Stream
	{
		public Stream Stream1 { get; set; }
		public Stream Stream2 { get; set; }

		long VirtualLength { get; }
		long position;

		public MergedStream(Stream stream1, Stream stream2)
		{
			Stream1 = stream1;
			Stream2 = stream2;

			VirtualLength = Stream1.Length + Stream2.Length;
		}

		public override void Flush()
		{
			Stream1.Flush();
			Stream2.Flush();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
				case SeekOrigin.Begin:
					position = offset;
					break;
				case SeekOrigin.Current:
					position += offset;
					break;
				case SeekOrigin.End:
					position = Length;
					position += offset;
					break;
			}

			if (position >= Stream1.Length)
				position = Stream1.Length + Stream2.Seek(offset - Stream1.Length, SeekOrigin.Begin);
			else
				position = Stream1.Seek(offset, SeekOrigin.Begin);

			return position;
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override int ReadByte()
		{
			int value;

			if (position >= Stream1.Length)
				value = Stream2.ReadByte();
			else
				value = Stream1.ReadByte();

			position++;

			return value;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return Read(buffer.AsSpan(offset, count));
		}

		public override int Read(Span<byte> buffer)
		{
			int bytesRead;

			if (position >= Stream1.Length)
				bytesRead = Stream2.Read(buffer);
			else if (buffer.Length > Stream1.Length)
			{
				bytesRead = Stream1.Read(buffer[..(int)Stream1.Length]);
				bytesRead += Stream2.Read(buffer[(int)Stream1.Length..]);
			}
			else
				bytesRead = Stream1.Read(buffer);

			position += bytesRead;

			return bytesRead;
		}

		public override void WriteByte(byte value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			throw new NotSupportedException();
		}

		public override bool CanRead => Stream1.CanRead && Stream2.CanRead;

		public override bool CanSeek => Stream1.CanSeek && Stream2.CanSeek;

		public override bool CanWrite => false;

		public override long Length => VirtualLength;

		public override long Position
		{
			get => position;
			set => Seek(value, SeekOrigin.Begin);
		}
	}
}
