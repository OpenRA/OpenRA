#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.FileSystem
{
	public sealed class BagFile : IReadOnlyPackage
	{
		public string Name { get; private set; }
		public IEnumerable<string> Contents { get { return index.Keys; } }

		readonly Stream s;
		readonly Dictionary<string, IdxEntry> index;

		public BagFile(FileSystem context, string filename)
		{
			Name = filename;

			// A bag file is always accompanied with an .idx counterpart
			// For example: audio.bag requires the audio.idx file
			var indexFilename = Path.ChangeExtension(filename, ".idx");

			// Build the index and dispose the stream, it is no longer needed after this
			List<IdxEntry> entries;
			using (var indexStream = context.Open(indexFilename))
				entries = new IdxReader(indexStream).Entries;

			index = entries.ToDictionaryWithConflictLog(x => x.Filename,
				"{0} (bag format)".F(filename),
				null, x => "(offs={0}, len={1})".F(x.Offset, x.Length));

			s = context.Open(filename);
		}

		public Stream GetStream(string filename)
		{
			IdxEntry entry;
			if (!index.TryGetValue(filename, out entry))
				return null;

			s.Seek(entry.Offset, SeekOrigin.Begin);

			var waveHeaderMemoryStream = new MemoryStream();

			var channels = (entry.Flags & 1) > 0 ? 2 : 1;

			if ((entry.Flags & 2) > 0)
			{
				// PCM
				waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("RIFF"));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes(entry.Length + 36));
				waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("WAVE"));
				waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("fmt "));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes(16));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)1));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)channels));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes(entry.SampleRate));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes(2 * channels * entry.SampleRate));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)(2 * channels)));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)16));
				waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("data"));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes(entry.Length));
			}

			if ((entry.Flags & 8) > 0)
			{
				// IMA ADPCM
				var samplesPerChunk = (2 * (entry.ChunkSize - 4)) + 1;
				var bytesPerSec = (int)Math.Floor(((double)(2 * entry.ChunkSize) / samplesPerChunk) * ((double)entry.SampleRate / 2));
				var chunkSize = entry.ChunkSize > entry.Length ? entry.Length : entry.ChunkSize;

				waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("RIFF"));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes(entry.Length + 52));
				waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("WAVE"));
				waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("fmt "));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes(20));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)17));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)channels));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes(entry.SampleRate));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes(bytesPerSec));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)chunkSize));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)4));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)2));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)samplesPerChunk));
				waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("fact"));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes(4));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes(4 * entry.Length));
				waveHeaderMemoryStream.Write(Encoding.ASCII.GetBytes("data"));
				waveHeaderMemoryStream.Write(BitConverter.GetBytes(entry.Length));
			}

			waveHeaderMemoryStream.Seek(0, SeekOrigin.Begin);

			// Construct a merged stream
			var mergedStream = new MergedStream(waveHeaderMemoryStream, s);
			mergedStream.SetLength(waveHeaderMemoryStream.Length + entry.Length);

			return mergedStream;
		}

		public bool Contains(string filename)
		{
			return index.ContainsKey(filename);
		}

		public void Dispose()
		{
			s.Dispose();
		}
	}
}
