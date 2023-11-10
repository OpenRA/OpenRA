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
using NVorbis;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.AudioLoaders
{
	public class OggLoader : ISoundLoader
	{
		bool ISoundLoader.TryParseSound(Stream stream, out ISoundFormat sound)
		{
			try
			{
				sound = new OggFormat(stream);
				return true;
			}
			catch
			{
				// Unsupported file
			}

			sound = null;
			return false;
		}
	}

	public sealed class OggFormat : ISoundFormat
	{
		public int SampleBits => 16;
		public int Channels => reader.Channels;
		public int SampleRate => reader.SampleRate;
		public float LengthInSeconds { get; }
		public Stream GetPCMInputStream() { return new OggStream(new OggFormat(this)); }
		public void Dispose() { reader.Dispose(); }

		readonly VorbisReader reader;
		readonly Stream stream;

		public OggFormat(Stream stream)
		{
			var startPosition = stream.Position;
			try
			{
				this.stream = stream;
				reader = new VorbisReader(stream, false);
				LengthInSeconds = (float)reader.TotalTime.TotalSeconds;
			}
			finally
			{
				stream.Position = startPosition;
			}
		}

		OggFormat(OggFormat cloneFrom)
		{
			stream = SegmentStream.CreateWithoutOwningStream(cloneFrom.stream, 0, (int)cloneFrom.stream.Length);
			reader = new VorbisReader(stream, false)
			{
				// Tell NVorbis to clip samples so we don't have to range-check during reading.
				ClipSamples = true
			};
		}

		public class OggStream : Stream
		{
			readonly OggFormat format;

			float[] conversionBuffer;

			public OggStream(OggFormat format)
			{
				this.format = format;
			}

			public override bool CanRead => true;
			public override bool CanSeek => false;
			public override bool CanWrite => false;

			public override long Length => format.reader.TotalSamples;

			public override long Position
			{
				get => format.reader.SamplePosition;
				set => throw new NotImplementedException();
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				return Read(buffer.AsSpan(offset, count));
			}

			public override int Read(Span<byte> buffer)
			{
				// Adjust count so it is in 16-bit samples instead of bytes.
				var count = buffer.Length / 2;

				// Make sure we don't have an odd count.
				count -= count % format.reader.Channels;

				var floatBuffer = EnsureArraySize(ref conversionBuffer, count);

				// Let NVorbis do the actual reading.
				var samples = format.reader.ReadSamples(floatBuffer);

				// Move the data back to the request buffer and convert to 16-bit signed samples for OpenAL.
				for (var i = 0; i < samples; i++)
				{
					var conversion = (short)(floatBuffer[i] * 32767);
					buffer[i * 2 + 0] = (byte)(conversion & 255);
					buffer[i * 2 + 1] = (byte)(conversion >> 8);
				}

				// Adjust count back to bytes.
				return samples * 2;
			}

			public override void Flush() { throw new NotImplementedException(); }
			public override long Seek(long offset, SeekOrigin origin) { throw new NotImplementedException(); }
			public override void SetLength(long value) { throw new NotImplementedException(); }
			public override void Write(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }
			public override void Write(ReadOnlySpan<byte> buffer) { throw new NotImplementedException(); }

			protected override void Dispose(bool disposing)
			{
				if (disposing)
					format.reader.Dispose();

				base.Dispose(disposing);
			}

			static Span<float> EnsureArraySize(ref float[] array, int desiredSize)
			{
				if (array == null || array.Length < desiredSize)
					array = new float[desiredSize];
				return array.AsSpan(..desiredSize);
			}
		}
	}
}
