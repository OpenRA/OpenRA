#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.FileFormats
{
	public class InstallShieldPackage : IFolder
	{
		readonly Dictionary<uint, PackageEntry> index;
		readonly long dataStart;
		readonly Stream s;
		int priority;

		public InstallShieldPackage(string filename, int priority)
		{
			this.priority = priority;
			s = FileSystem.Open(filename);

			// Parse package header
			BinaryReader reader = new BinaryReader(s);
			uint signature = reader.ReadUInt32();
			if (signature != 0x8C655D13)
				throw new InvalidDataException("Not an Installshield package");
			
			reader.ReadBytes(8);
			var FileCount = reader.ReadUInt16();
			reader.ReadBytes(4);
			var ArchiveSize = reader.ReadUInt32();
			reader.ReadBytes(19);
			var TOCAddress = reader.ReadInt32();
			
			Console.WriteLine("SETUP.Z:");
			Console.WriteLine("FileCount: {0}", FileCount);
			Console.WriteLine("Archive Size: {0}", ArchiveSize);
			
			// Parse the directory list
			s.Seek(TOCAddress, SeekOrigin.Begin);
			BinaryReader TOCreader = new BinaryReader(s);
			while (s.Position < s.Length)
				ParseDirectory(TOCreader);
		}

		void ParseDirectory(BinaryReader reader)
		{
			// Parse directory header
			var FileCount = reader.ReadUInt16();
			var ChunkSize = reader.ReadUInt16();
			var NameLength = reader.ReadUInt16();
			var DirName = new String(reader.ReadChars(NameLength));
			
			// Skip to the end of the chunk
			reader.ReadBytes(ChunkSize - NameLength - 6);
			
			Console.WriteLine("Directory `{0}': {1}", DirName, FileCount);
			// Parse files
			for (var i = 0; i < FileCount; i++)
				ParseFile(reader);
		}
		
		void ParseFile(BinaryReader reader)
		{
			reader.ReadBytes(7);
			var CompressedSize = reader.ReadUInt32();
			reader.ReadBytes(12);
			var ChunkSize = reader.ReadUInt16();
			reader.ReadBytes(4);
			var NameLength = reader.ReadByte();
			var FileName = new String(reader.ReadChars(NameLength));

			// Skip to the end of the chunk
			reader.ReadBytes(ChunkSize - NameLength - 30);
			
			Console.WriteLine("\t{0}: {1}", FileName, CompressedSize);
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
	}
}
