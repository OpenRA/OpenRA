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
using OpenRA.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.FileSystem
{
	public sealed class MixFile : IReadOnlyPackage
	{
		public string Name { get; private set; }
		public IEnumerable<string> Contents { get { return index.Keys; } }

		readonly Dictionary<string, PackageEntry> index;
		readonly long dataStart;
		readonly Stream s;
		readonly FileSystem context;

		public MixFile(FileSystem context, string filename)
		{
			Name = filename;
			this.context = context;

			s = context.Open(filename);
			try
			{
				// Detect format type
				var isCncMix = s.ReadUInt16() != 0;

				// The C&C mix format doesn't contain any flags or encryption
				var isEncrypted = false;
				if (!isCncMix)
					isEncrypted = (s.ReadUInt16() & 0x2) != 0;

				List<PackageEntry> entries;
				if (isEncrypted)
				{
					long unused;
					entries = ParseHeader(DecryptHeader(s, 4, out dataStart), 0, out unused);
				}
				else
					entries = ParseHeader(s, isCncMix ? 0 : 4, out dataStart);

				index = ParseIndex(entries.ToDictionaryWithConflictLog(x => x.Hash,
					"{0} ({1} format, Encrypted: {2}, DataStart: {3})".F(filename, isCncMix ? "C&C" : "RA/TS/RA2", isEncrypted, dataStart),
					null, x => "(offs={0}, len={1})".F(x.Offset, x.Length)));
			}
			catch (Exception)
			{
				Dispose();
				throw;
			}
		}

		Dictionary<string, PackageEntry> ParseIndex(Dictionary<uint, PackageEntry> entries)
		{
			var classicIndex = new Dictionary<string, PackageEntry>();
			var crcIndex = new Dictionary<string, PackageEntry>();
			var allPossibleFilenames = new HashSet<string>();

			// Try and find a local mix database
			var dbNameClassic = PackageEntry.HashFilename("local mix database.dat", PackageHashType.Classic);
			var dbNameCRC = PackageEntry.HashFilename("local mix database.dat", PackageHashType.CRC32);
			foreach (var kv in entries)
			{
				if (kv.Key == dbNameClassic || kv.Key == dbNameCRC)
				{
					var db = new XccLocalDatabase(GetContent(kv.Value));
					foreach (var e in db.Entries)
						allPossibleFilenames.Add(e);

					break;
				}
			}

			// Load the global mix database
			// TODO: This should be passed to the mix file ctor
			if (context.Exists("global mix database.dat"))
			{
				using (var db = new XccGlobalDatabase(context.Open("global mix database.dat")))
				{
					foreach (var e in db.Entries)
						allPossibleFilenames.Add(e);
				}
			}

			foreach (var filename in allPossibleFilenames)
			{
				var classicHash = PackageEntry.HashFilename(filename, PackageHashType.Classic);
				var crcHash = PackageEntry.HashFilename(filename, PackageHashType.CRC32);
				PackageEntry e;

				if (entries.TryGetValue(classicHash, out e))
					classicIndex.Add(filename, e);

				if (entries.TryGetValue(crcHash, out e))
					crcIndex.Add(filename, e);
			}

			var bestIndex = crcIndex.Count > classicIndex.Count ? crcIndex : classicIndex;

			var unknown = entries.Count - bestIndex.Count;
			if (unknown > 0)
				Log.Write("debug", "{0}: failed to resolve filenames for {1} unknown hashes".F(Name, unknown));

			return bestIndex;
		}

		static List<PackageEntry> ParseHeader(Stream s, long offset, out long headerEnd)
		{
			s.Seek(offset, SeekOrigin.Begin);
			var numFiles = s.ReadUInt16();
			/*uint dataSize = */s.ReadUInt32();

			var items = new List<PackageEntry>();
			for (var i = 0; i < numFiles; i++)
				items.Add(new PackageEntry(s));

			headerEnd = offset + 6 + numFiles * PackageEntry.Size;
			return items;
		}

		static MemoryStream DecryptHeader(Stream s, long offset, out long headerEnd)
		{
			s.Seek(offset, SeekOrigin.Begin);

			// Decrypt blowfish key
			var keyblock = s.ReadBytes(80);
			var blowfishKey = new BlowfishKeyProvider().DecryptKey(keyblock);
			var fish = new Blowfish(blowfishKey);

			// Decrypt first block to work out the header length
			var ms = Decrypt(ReadBlocks(s, offset + 80, 1), fish);
			var numFiles = ms.ReadUInt16();

			// Decrypt the full header - round bytes up to a full block
			var blockCount = (13 + numFiles * PackageEntry.Size) / 8;
			headerEnd = offset + 80 + blockCount * 8;

			return Decrypt(ReadBlocks(s, offset + 80, blockCount), fish);
		}

		static MemoryStream Decrypt(uint[] h, Blowfish fish)
		{
			var decrypted = fish.Decrypt(h);

			var ms = new MemoryStream();
			var writer = new BinaryWriter(ms);
			foreach (var t in decrypted)
				writer.Write(t);
			writer.Flush();

			ms.Seek(0, SeekOrigin.Begin);
			return ms;
		}

		static uint[] ReadBlocks(Stream s, long offset, int count)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset", "Non-negative number required.");

			if (count < 0)
				throw new ArgumentOutOfRangeException("count", "Non-negative number required.");

			if (offset + (count * 2) > s.Length)
				throw new ArgumentException("Bytes to read {0} and offset {1} greater than stream length {2}.".F(count * 2, offset, s.Length));

			s.Seek(offset, SeekOrigin.Begin);

			// A block is a single encryption unit (represented as two 32-bit integers)
			var ret = new uint[2 * count];
			for (var i = 0; i < ret.Length; i++)
				ret[i] = s.ReadUInt32();

			return ret;
		}

		public Stream GetContent(PackageEntry entry)
		{
			Stream parentStream;
			var offset = dataStart + entry.Offset + SegmentStream.GetOverallNestedOffset(s, out parentStream);
			var path = ((FileStream)parentStream).Name;
			return new SegmentStream(File.OpenRead(path), offset, entry.Length);
		}

		public Stream GetStream(string filename)
		{
			PackageEntry e;
			if (!index.TryGetValue(filename, out e))
				return null;

			return GetContent(e);
		}

		public IReadOnlyDictionary<string, PackageEntry> Index
		{
			get
			{
				var absoluteIndex = index.ToDictionary(e => e.Key, e => new PackageEntry(e.Value.Hash, (uint)(e.Value.Offset + dataStart), e.Value.Length));
				return new ReadOnlyDictionary<string, PackageEntry>(absoluteIndex);
			}
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
