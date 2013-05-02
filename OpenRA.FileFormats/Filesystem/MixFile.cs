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
		readonly bool isRmix, isEncrypted;
		readonly long dataStart;
		readonly Stream s;
		int priority;

		// Create a new MixFile
		public MixFile(string filename, int priority, Dictionary<string, byte[]> contents)
		{
			this.priority = priority;
			if (File.Exists(filename))
				File.Delete(filename);

			s = File.Create(filename);
			Write(contents);
		}

		public MixFile(string filename, int priority)
		{
			this.priority = priority;
			s = FileSystem.Open(filename);

			BinaryReader reader = new BinaryReader(s);
			uint signature = reader.ReadUInt32();

			isRmix = 0 == (signature & ~(uint)(MixFileFlags.Checksum | MixFileFlags.Encrypted));

			if (isRmix)
			{
				isEncrypted = 0 != (signature & (uint)MixFileFlags.Encrypted);
				if (isEncrypted)
				{
					index = ParseRaHeader(s, out dataStart).ToDictionaryWithConflictLog(x => x.Hash,
						"MixFile.RaHeader of {0}".F(filename), null, x => "(offs={0}, len={1})".F(x.Offset, x.Length)
					);
					return;
				}
			}
			else
				s.Seek( 0, SeekOrigin.Begin );

			isEncrypted = false;
			index = ParseTdHeader(s, out dataStart).ToDictionaryWithConflictLog(x => x.Hash,
				"MixFile.TdHeader of {0}".F(filename), null, x => "(offs={0}, len={1})".F(x.Offset, x.Length)
			);
		}

		const long headerStart = 84;

		List<PackageEntry> ParseRaHeader(Stream s, out long dataStart)
		{
			BinaryReader reader = new BinaryReader(s);
			byte[] keyblock = reader.ReadBytes(80);
			byte[] blowfishKey = new BlowfishKeyProvider().DecryptKey(keyblock);

			uint[] h = ReadBlocks(reader, 1);

			Blowfish fish = new Blowfish(blowfishKey);
			MemoryStream ms = Decrypt( h, fish );
			BinaryReader reader2 = new BinaryReader(ms);

			ushort numFiles = reader2.ReadUInt16();
			reader2.ReadUInt32(); /*datasize*/

			s.Position = headerStart;
			reader = new BinaryReader(s);

			// Round up to the next full block
			int blockCount = (13 + numFiles*PackageEntry.Size)/8;
			h = ReadBlocks(reader, blockCount);
			ms = Decrypt( h, fish );

			dataStart = headerStart + 8*blockCount;

			long ds;
			return ParseTdHeader( ms, out ds );
		}

		static MemoryStream Decrypt( uint[] h, Blowfish fish )
		{
			uint[] decrypted = fish.Decrypt( h );

			MemoryStream ms = new MemoryStream();
			BinaryWriter writer = new BinaryWriter( ms );
			foreach( uint t in decrypted )
				writer.Write( t );
			writer.Flush();

			ms.Position = 0;
			return ms;
		}

		uint[] ReadBlocks(BinaryReader r, int count)
		{
			// A block is a single encryption unit (represented as two 32-bit integers)
			uint[] ret = new uint[2*count];
			for (int i = 0; i < ret.Length; i++)
				ret[i] = r.ReadUInt32();

			return ret;
		}

		List<PackageEntry> ParseTdHeader(Stream s, out long dataStart)
		{
			List<PackageEntry> items = new List<PackageEntry>();

			BinaryReader reader = new BinaryReader(s);
			ushort numFiles = reader.ReadUInt16();
			/*uint dataSize = */reader.ReadUInt32();

			for (int i = 0; i < numFiles; i++)
				items.Add(new PackageEntry(reader));

			dataStart = s.Position;
			return items;
		}

		public Stream GetContent(uint hash)
		{
			PackageEntry e;
			if (!index.TryGetValue(hash, out e))
				return null;

			s.Seek( dataStart + e.Offset, SeekOrigin.Begin );
			byte[] data = new byte[ e.Length ];
			s.Read( data, 0, (int)e.Length );
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
				uint length = (uint)kv.Value.Length;
				uint hash = PackageEntry.HashFilename(Path.GetFileName(kv.Key));
				items.Add(new PackageEntry(hash, dataSize, length));
				dataSize += length;
			}

			// Write the new file
			s.Seek(0,SeekOrigin.Begin);
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

	[Flags]
	enum MixFileFlags : uint
	{
		Checksum = 0x10000,
		Encrypted = 0x20000,
	}
}
