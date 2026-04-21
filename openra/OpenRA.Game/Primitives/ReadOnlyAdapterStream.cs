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
using System.Collections.Generic;
using System.IO;

namespace OpenRA.Primitives
{
	/// <summary>
	/// Provides a read-only buffering layer so data can be streamed from sources where reading arbitrary amounts of
	/// data is difficult.
	/// </summary>
	public abstract class ReadOnlyAdapterStream : Stream
	{
		readonly Queue<byte> data = new(1024);
		readonly Stream baseStream;
		bool baseStreamEmpty;

		protected ReadOnlyAdapterStream(Stream stream)
		{
			ArgumentNullException.ThrowIfNull(stream);
			if (!stream.CanRead)
				throw new ArgumentException("stream must be readable.", nameof(stream));

			baseStream = stream;
		}

		public sealed override bool CanSeek => false;
		public sealed override bool CanRead => true;
		public sealed override bool CanWrite => false;

		public override long Length => throw new NotSupportedException();

		public sealed override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public sealed override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
		public sealed override void SetLength(long value) { throw new NotSupportedException(); }
		public sealed override void WriteByte(byte value) { throw new NotSupportedException(); }
		public sealed override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
		public override void Write(ReadOnlySpan<byte> buffer) { throw new NotSupportedException(); }
		public sealed override void Flush() { throw new NotSupportedException(); }

		public sealed override int ReadByte()
		{
			if (data.Count > 0)
				return data.Dequeue();

			while (!baseStreamEmpty)
			{
				baseStreamEmpty = BufferData(baseStream, data);
				if (data.Count > 0)
					return data.Dequeue();
			}

			return -1;
		}

		public sealed override int Read(byte[] buffer, int offset, int count)
		{
			return Read(buffer.AsSpan(offset, count));
		}

		public override int Read(Span<byte> buffer)
		{
			var copied = 0;
			ConsumeData(buffer, ref copied);

			while (copied < buffer.Length && !baseStreamEmpty)
			{
				baseStreamEmpty = BufferData(baseStream, data);
				ConsumeData(buffer, ref copied);
			}

			return copied;
		}

		/// <summary>
		/// Reads data into a buffer, which will be used to satisfy <see cref="ReadByte()"/>,
		/// <see cref="Read(byte[], int, int)"/> and <see cref="Read(Span{byte})"/> calls.
		/// </summary>
		/// <param name="baseStream">The source stream from which bytes should be read.</param>
		/// <param name="data">The queue where bytes should be enqueued. Do not dequeue from this buffer.</param>
		/// <returns>Return true if all data has been read; otherwise, false.</returns>
		protected abstract bool BufferData(Stream baseStream, Queue<byte> data);

		void ConsumeData(Span<byte> buffer, ref int copied)
		{
			while (copied < buffer.Length && data.Count > 0)
				buffer[copied++] = data.Dequeue();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				baseStream.Dispose();
			base.Dispose(disposing);
		}
	}
}
