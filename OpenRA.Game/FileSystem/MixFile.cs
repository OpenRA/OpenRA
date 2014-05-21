#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
using OpenRA.FileFormats;

namespace OpenRA.FileSystem
{
	public class MixFile : IFolder
	{
		readonly Dictionary<uint, PackageEntry> index;
		readonly long dataStart;
		readonly Stream s;
		readonly int priority;
		readonly string filename;
		readonly PackageHashType type;

		// Save a mix to disk with the given contents
		public MixFile(string filename, int priority, Dictionary<string, byte[]> contents)
		{
			this.filename = filename;
			this.priority = priority;
			this.type = PackageHashType.Classic;

			if (File.Exists(filename))
				File.Delete(filename);

			s = File.Create(filename);
			index = new Dictionary<uint, PackageEntry>();
			contents.Add("local mix database.dat", new XccLocalDatabase(contents.Keys.Append("local mix database.dat")).Data());
			Write(contents);
		}

		public MixFile(string filename, PackageHashType type, int priority)
		{
			this.filename = filename;
			this.priority = priority;
			this.type = type;
			s = GlobalFileSystem.Open(filename);

			// Detect format type
			s.Seek(0, SeekOrigin.Begin);
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

			index = entries.ToDictionaryWithConflictLog(x => x.Hash,
				"{0} ({1} format, Encrypted: {2}, DataStart: {3})".F(filename, (isCncMix ? "C&C" : "RA/TS/RA2"), isEncrypted, dataStart),
				null, x => "(offs={0}, len={1})".F(x.Offset, x.Length)
			);
		}

		static List<PackageEntry> ParseHeader(Stream s, long offset, out long headerEnd)
		{
			s.Seek(offset, SeekOrigin.Begin);
			var numFiles = s.ReadUInt16();
			/*uint dataSize = */s.ReadUInt32();

			var items = new List<PackageEntry>();
			for (var i = 0; i < numFiles; i++)
				items.Add(new PackageEntry(s));

			headerEnd = offset + 6 + numFiles*PackageEntry.Size;
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
			var blockCount = (13 + numFiles*PackageEntry.Size)/8;
			headerEnd = offset + 80 + blockCount*8;

			return Decrypt(ReadBlocks(s, offset + 80, blockCount), fish);
		}

		static MemoryStream Decrypt(uint[] h, Blowfish fish)
		{
			var decrypted = fish.Decrypt(h);

			var ms = new MemoryStream();
			var writer = new BinaryWriter(ms);
			foreach(var t in decrypted)
				writer.Write(t);
			writer.Flush();

			ms.Seek(0, SeekOrigin.Begin);
			return ms;
		}

		static uint[] ReadBlocks(Stream s, long offset, int count)
		{
			s.Seek(offset, SeekOrigin.Begin);

			// A block is a single encryption unit (represented as two 32-bit integers)
			var ret = new uint[2*count];
			for (var i = 0; i < ret.Length; i++)
				ret[i] = s.ReadUInt32();

			return ret;
		}

		uint? FindMatchingHash(string filename)
		{
			var hash = PackageEntry.HashFilename(filename, type);
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

		public Stream GetContent(uint hash)
		{
			PackageEntry e;
			if (!index.TryGetValue(hash, out e))
				return null;

			s.Seek(dataStart + e.Offset, SeekOrigin.Begin);
			var data = new byte[e.Length];
			s.Read(data, 0, (int)e.Length);
			return new MemoryStream(data);
		}

		public Stream GetContent(string filename)
		{
			var hash = FindMatchingHash(filename);
			return hash.HasValue ? GetContent(hash.Value) : null;
		}

		static readonly uint[] Nothing = {};
		public IEnumerable<uint> ClassicHashes()
		{
			if (type == PackageHashType.Classic)
				return index.Keys;

			return Nothing;
		}

		public IEnumerable<uint> CrcHashes()
		{
			if (type == PackageHashType.CRC32)
				return index.Keys;

			return Nothing;
		}

		public IEnumerable<string> AllFileNames()
		{
			var lookup = new Dictionary<uint, string>();
			if (Exists("local mix database.dat"))
			{
				var db = new XccLocalDatabase(GetContent("local mix database.dat"));
				foreach (var e in db.Entries)
				{
					var hash = PackageEntry.HashFilename(e, type);
					if (!lookup.ContainsKey(hash))
						lookup.Add(hash, e);
				}
			}

			if (GlobalFileSystem.Exists("global mix database.dat"))
			{
				var db = new XccGlobalDatabase(GlobalFileSystem.Open("global mix database.dat"));
				foreach (var e in db.Entries)
				{
					var hash = PackageEntry.HashFilename(e, type);
					if (!lookup.ContainsKey(hash))
						lookup.Add(hash, e);
				}
			}

			return index.Keys.Select(k => lookup.ContainsKey(k) ? lookup[k] : "{0:X}".F(k));
		}

		public bool Exists(string filename)
		{
			return FindMatchingHash(filename).HasValue;
		}

		public int Priority { get { return 1000 + priority; } }
		public string Name { get { return filename; } }
		
		public void Write(Dictionary<string, byte[]> contents)
		{
			// Cannot modify existing mixfile - rename existing file and
			// create a new one with original content plus modifications
			GlobalFileSystem.Unmount(this);

			// TODO: Add existing data to the contents list
			if (index.Count > 0)
				throw new NotImplementedException("Updating mix files unfinished");

			// Construct a list of entries for the file header
			uint dataSize = 0;
			var items = new List<PackageEntry>();
			foreach (var kv in contents)
			{
				var length = (uint)kv.Value.Length;
				var hash = PackageEntry.HashFilename(Path.GetFileName(kv.Key), type);
				items.Add(new PackageEntry(hash, dataSize, length));
				dataSize += length;
			}

			// Write the new file
			s.Seek(0, SeekOrigin.Begin);
			using (var writer = new BinaryWriter(s))
			{
				// Write file header
				writer.Write((ushort)items.Count);
				writer.Write(dataSize);
				foreach (var item in items)
					item.Write(writer);

				writer.Flush();

				// Copy file data
				foreach (var file in contents)
					s.Write(file.Value);
			}
		}
	}
}
