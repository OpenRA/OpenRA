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
		readonly Queue<byte> data = new Queue<byte>(1024);
		readonly Stream baseStream;
		bool baseStreamEmpty;

		protected ReadOnlyAdapterStream(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
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
			var copied = 0;
			ConsumeData(buffer, offset, count, ref copied);

			while (copied < count && !baseStreamEmpty)
			{
				baseStreamEmpty = BufferData(baseStream, data);
				ConsumeData(buffer, offset, count, ref copied);
			}

			return copied;
		}

		/// <summary>
		/// Reads data into a buffer, which will be used to satisfy <see cref="ReadByte()"/> and
		/// <see cref="Read(byte[], int, int)"/> calls.
		/// </summary>
		/// <param name="baseStream">The source stream from which bytes should be read.</param>
		/// <param name="data">The queue where bytes should be enqueued. Do not dequeue from this buffer.</param>
		/// <returns>Return true if all data has been read; otherwise, false.</returns>
		protected abstract bool BufferData(Stream baseStream, Queue<byte> data);

		void ConsumeData(byte[] buffer, int offset, int count, ref int copied)
		{
			while (copied < count && data.Count > 0)
				buffer[offset + copied++] = data.Dequeue();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				baseStream.Dispose();
			base.Dispose(disposing);
		}
	}
}
