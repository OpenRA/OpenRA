#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Primitives;
using FS = OpenRA.FileSystem.FileSystem;

namespace OpenRA.Mods.Cnc.FileSystem
{
	public class AudioBagLoader : IPackageLoader
	{
		sealed class BagFile : IReadOnlyPackage
		{
			public string Name { get; private set; }
			public IEnumerable<string> Contents { get { return index.Keys; } }

			readonly Stream s;
			readonly Dictionary<string, IdxEntry> index;

			public BagFile(Stream s, List<IdxEntry> entries, string filename)
			{
				Name = filename;
				this.s = s;

				index = entries.ToDictionaryWithConflictLog(x => x.Filename,
					"{0} (bag format)".F(filename),
					null, x => "(offs={0}, len={1})".F(x.Offset, x.Length));
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
					waveHeaderMemoryStream.WriteArray(Encoding.ASCII.GetBytes("RIFF"));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes(entry.Length + 36));
					waveHeaderMemoryStream.WriteArray(Encoding.ASCII.GetBytes("WAVE"));
					waveHeaderMemoryStream.WriteArray(Encoding.ASCII.GetBytes("fmt "));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes(16));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes((short)1));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes((short)channels));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes(entry.SampleRate));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes(2 * channels * entry.SampleRate));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes((short)(2 * channels)));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes((short)16));
					waveHeaderMemoryStream.WriteArray(Encoding.ASCII.GetBytes("data"));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes(entry.Length));
				}

				if ((entry.Flags & 8) > 0)
				{
					// IMA ADPCM
					var samplesPerChunk = (2 * (entry.ChunkSize - 4)) + 1;
					var bytesPerSec = (int)Math.Floor(((double)(2 * entry.ChunkSize) / samplesPerChunk) * ((double)entry.SampleRate / 2));
					var chunkSize = entry.ChunkSize > entry.Length ? entry.Length : entry.ChunkSize;

					waveHeaderMemoryStream.WriteArray(Encoding.ASCII.GetBytes("RIFF"));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes(entry.Length + 52));
					waveHeaderMemoryStream.WriteArray(Encoding.ASCII.GetBytes("WAVE"));
					waveHeaderMemoryStream.WriteArray(Encoding.ASCII.GetBytes("fmt "));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes(20));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes((short)17));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes((short)channels));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes(entry.SampleRate));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes(bytesPerSec));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes((short)chunkSize));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes((short)4));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes((short)2));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes((short)samplesPerChunk));
					waveHeaderMemoryStream.WriteArray(Encoding.ASCII.GetBytes("fact"));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes(4));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes(4 * entry.Length));
					waveHeaderMemoryStream.WriteArray(Encoding.ASCII.GetBytes("data"));
					waveHeaderMemoryStream.WriteArray(BitConverter.GetBytes(entry.Length));
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

			public IReadOnlyPackage OpenPackage(string filename, FS context)
			{
				// Not implemented
				return null;
			}

			public void Dispose()
			{
				s.Dispose();
			}
		}

		bool IPackageLoader.TryParsePackage(Stream s, string filename, FS context, out IReadOnlyPackage package)
		{
			if (!filename.EndsWith(".bag", StringComparison.InvariantCultureIgnoreCase))
			{
				package = null;
				return false;
			}

			// A bag file is always accompanied with an .idx counterpart
			// For example: audio.bag requires the audio.idx file
			var indexFilename = Path.ChangeExtension(filename, ".idx");
			List<IdxEntry> entries  = null;

			try
			{
				// Build the index and dispose the stream, it is no longer needed after this
				using (var indexStream = context.Open(indexFilename))
					entries = new IdxReader(indexStream).Entries;
			}
			catch
			{
				package = null;
				return false;
			}

			package = new BagFile(s, entries, filename);
			return true;
		}
	}
}
