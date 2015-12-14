#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
	public sealed class BagFile : IFolder
	{
		static readonly uint[] Nothing = { };

		readonly string bagFilename;
		readonly Stream s;
		readonly int bagFilePriority;
		readonly Dictionary<uint, IdxEntry> index;
		readonly FileSystem context;

		public BagFile(FileSystem context, string filename, int priority)
		{
			bagFilename = filename;
			bagFilePriority = priority;
			this.context = context;

			// A bag file is always accompanied with an .idx counterpart
			// For example: audio.bag requires the audio.idx file
			var indexFilename = Path.ChangeExtension(filename, ".idx");

			// Build the index and dispose the stream, it is no longer needed after this
			List<IdxEntry> entries;
			using (var indexStream = context.Open(indexFilename))
				entries = new IdxReader(indexStream).Entries;

			index = entries.ToDictionaryWithConflictLog(x => x.Hash,
				"{0} (bag format)".F(filename),
				null, x => "(offs={0}, len={1})".F(x.Offset, x.Length));

			s = context.Open(filename);
		}

		public int Priority { get { return 1000 + bagFilePriority; } }
		public string Name { get { return bagFilename; } }

		public Stream GetContent(uint hash)
		{
			IdxEntry entry;
			if (!index.TryGetValue(hash, out entry))
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
				waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)WavLoader.WaveType.Pcm));
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
				waveHeaderMemoryStream.Write(BitConverter.GetBytes((short)WavLoader.WaveType.ImaAdpcm));
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

		uint? FindMatchingHash(string filename)
		{
			var hash = IdxEntry.HashFilename(filename, PackageHashType.CRC32);
			if (index.ContainsKey(hash))
				return hash;

			// Maybe we were given a raw hash?
			uint raw;
			if (!uint.TryParse(filename, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out raw))
				return null;

			if ("{0:X}".F(raw) == filename && index.ContainsKey(raw))
				return raw;

			return null;
		}

		public Stream GetContent(string filename)
		{
			var hash = FindMatchingHash(filename);
			return hash.HasValue ? GetContent(hash.Value) : null;
		}

		public bool Exists(string filename)
		{
			return FindMatchingHash(filename).HasValue;
		}

		public IEnumerable<uint> ClassicHashes()
		{
			return Nothing;
		}

		public IEnumerable<uint> CrcHashes()
		{
			return index.Keys;
		}

		public IEnumerable<string> AllFileNames()
		{
			var lookup = new Dictionary<uint, string>();
			if (context.Exists("global mix database.dat"))
			{
				var db = new XccGlobalDatabase(context.Open("global mix database.dat"));
				foreach (var e in db.Entries)
				{
					var hash = IdxEntry.HashFilename(e, PackageHashType.CRC32);
					if (!lookup.ContainsKey(hash))
						lookup.Add(hash, e);
				}
			}

			return index.Keys.Select(k => lookup.ContainsKey(k) ? lookup[k] : "{0:X}".F(k));
		}

		public void Write(Dictionary<string, byte[]> contents)
		{
			context.Unmount(this);
			throw new NotImplementedException("Updating bag files unsupported");
		}

		public void Dispose()
		{
			s.Dispose();
		}
	}
}
