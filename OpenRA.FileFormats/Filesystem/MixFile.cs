#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.FileFormats
{
	public interface IFolder
	{
		Stream GetContent(string filename);
		bool Exists(string filename);
		IEnumerable<uint> AllFileHashes();
		void Write(Dictionary<string, byte[]> contents);
		int Priority { get; }
	}

	public class MixFile : IFolder
	{
		readonly Dictionary<uint, PackageEntry> index;
		readonly long dataStart;
		readonly Stream s;
		int priority;

		// Save a mix to disk with the given contents
		public MixFile(string filename, int priority, Dictionary<string, byte[]> contents)
		{
			this.priority = priority;
			if (File.Exists(filename))
				File.Delete(filename);

			s = File.Create(filename);

			// TODO: Add a local mix database.dat for compatibility with XCC Mixer
			Write(contents);
		}

		byte checksum(Stream s, long start, long end)
		{
			s.Seek(start, SeekOrigin.Begin);
			var r = new BinaryReader(s);

			var c = r.ReadByte();
			while (r.BaseStream.Position < end)
				c ^= r.ReadByte();

			return c;
		}

		public MixFile(string filename, int priority)
		{
			this.priority = priority;
			s = FileSystem.Open(filename);
			var initialPosition = s.Position;

			// Detect format type
			var reader = new BinaryReader(s);
			var isCncMix = reader.ReadUInt16() != 0;
			var isEncrypted = false;

			// The C&C mix format doesn't contain any flags or encryption
			if (isCncMix)
				s.Seek(0, SeekOrigin.Begin);
			else
				isEncrypted = (reader.ReadUInt16() & 0x2) != 0;

			var header = isEncrypted ? DecryptHeader(s) : s;
			index = ParseHeader(header).ToDictionaryWithConflictLog(x => x.Hash,
				"{0} ({1} format, Encrypted: {2}, Header start: {3}, Header end: {4}, Header checksum: {5})".F(
					filename, (isCncMix ? "C&C" : "RA/TS/RA2"), isEncrypted, initialPosition, s.Position, checksum(s, initialPosition, s.Position)),
			    null, x => "(offs={0}, len={1})".F(x.Offset, x.Length)
			);

			dataStart = s.Position;
		}

		List<PackageEntry> ParseHeader(Stream s)
		{
			var items = new List<PackageEntry>();
			var reader = new BinaryReader(s);
			var numFiles = reader.ReadUInt16();
			/*uint dataSize = */reader.ReadUInt32();

			for (var i = 0; i < numFiles; i++)
				items.Add(new PackageEntry(reader));

			return items;
		}

		MemoryStream DecryptHeader(Stream s)
		{
			var reader = new BinaryReader(s);

			// Decrypt blowfish key
			var keyblock = reader.ReadBytes(80);
			var blowfishKey = new BlowfishKeyProvider().DecryptKey(keyblock);
			var fish = new Blowfish(blowfishKey);

			// Decrypt first block to work out the header length
			var headerStart = s.Position;
			var ms = Decrypt(ReadBlocks(s, headerStart, 1), fish);
			var numFiles = new BinaryReader(ms).ReadUInt16();

			// Decrypt the full header - round bytes up to a full block
			var blockCount = (13 + numFiles*PackageEntry.Size)/8;
			return Decrypt(ReadBlocks(s, headerStart, blockCount), fish);
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

		uint[] ReadBlocks(Stream s, long offset, int count)
		{
			s.Seek(offset, SeekOrigin.Begin);
			var r = new BinaryReader(s);

			// A block is a single encryption unit (represented as two 32-bit integers)
			var ret = new uint[2*count];
			for (var i = 0; i < ret.Length; i++)
				ret[i] = r.ReadUInt32();

			return ret;
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
			return GetContent(PackageEntry.HashFilename(filename));
		}

		public IEnumerable<uint> AllFileHashes()
		{
			return index.Keys;
		}

		public bool Exists(string filename)
		{
			return index.ContainsKey(PackageEntry.HashFilename(filename));
		}


		public int Priority
		{
			get { return 1000 + priority; }
		}

		public void Write(Dictionary<string, byte[]> contents)
		{
			// Cannot modify existing mixfile - rename existing file and
			// create a new one with original content plus modifications
			FileSystem.Unmount(this);

			// TODO: Add existing data to the contents list
			if (index.Count > 0)
				throw new NotImplementedException("Updating mix files unfinished");

			// Construct a list of entries for the file header
			uint dataSize = 0;
			var items = new List<PackageEntry>();
			foreach (var kv in contents)
			{
				var length = (uint)kv.Value.Length;
				var hash = PackageEntry.HashFilename(Path.GetFileName(kv.Key));
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
