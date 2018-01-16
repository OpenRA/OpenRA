#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.IO;

namespace OpenRA.Primitives
{
	public class MergedStream : Stream
	{
		public Stream Stream1 { get; set; }
		public Stream Stream2 { get; set; }

		long VirtualLength { get; set; }
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
			VirtualLength = value;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int bytesRead;

			if (position >= Stream1.Length)
				bytesRead = Stream2.Read(buffer, offset, count);
			else if (count > Stream1.Length)
			{
				bytesRead = Stream1.Read(buffer, offset, (int)Stream1.Length);
				bytesRead += Stream2.Read(buffer, (int)Stream1.Length, count - (int)Stream1.Length);
			}
			else
				bytesRead = Stream1.Read(buffer, offset, count);

			position += bytesRead;

			return bytesRead;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (position >= Stream1.Length)
				Stream2.Write(buffer, offset - (int)Stream1.Length, count);
			else
				Stream1.Write(buffer, offset, count);
		}

		public override bool CanRead
		{
			get { return Stream1.CanRead && Stream2.CanRead; }
		}

		public override bool CanSeek
		{
			get { return Stream1.CanSeek && Stream2.CanSeek; }
		}

		public override bool CanWrite
		{
			get { return Stream1.CanWrite && Stream2.CanWrite; }
		}

		public override long Length
		{
			get { return VirtualLength; }
		}

		public override long Position
		{
			get { return position; }
			set { Seek(value, SeekOrigin.Begin); }
		}
	}
}
