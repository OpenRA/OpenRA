#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
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
		IEnumerable<uint> AllFileHashes();
	}

	public class Package : IFolder
	{
		readonly Dictionary<uint, PackageEntry> index;
		readonly bool isRmix, isEncrypted;
		readonly long dataStart;
		readonly Stream s;

		public Package(string filename)
		{
			s = FileSystem.Open(filename);

			BinaryReader reader = new BinaryReader(s);
			uint signature = reader.ReadUInt32();

			isRmix = 0 == (signature & ~(uint)(MixFileFlags.Checksum | MixFileFlags.Encrypted));

			if (isRmix)
			{
				isEncrypted = 0 != (signature & (uint)MixFileFlags.Encrypted);
				if( isEncrypted )
				{
					index = ParseRaHeader( s, out dataStart ).ToDictionary(x => x.Hash);
					return;
				}
			}
			else
				s.Seek( 0, SeekOrigin.Begin );

			isEncrypted = false;
			index = ParseTdHeader(s, out dataStart).ToDictionary(x => x.Hash);
		}

		const long headerStart = 84;

		List<PackageEntry> ParseRaHeader(Stream s, out long dataStart)
		{
			BinaryReader reader = new BinaryReader(s);
			byte[] keyblock = reader.ReadBytes(80);
			byte[] blowfishKey = new BlowfishKeyProvider().DecryptKey(keyblock);

			uint[] h = ReadUints(reader, 2);

			Blowfish fish = new Blowfish(blowfishKey);
			MemoryStream ms = Decrypt( h, fish );
			BinaryReader reader2 = new BinaryReader(ms);

			ushort numFiles = reader2.ReadUInt16();
			uint datasize = reader2.ReadUInt32();

			s.Position = headerStart;
			reader = new BinaryReader(s);

			int byteCount = 6 + numFiles * PackageEntry.Size;
			h = ReadUints( reader, ( byteCount + 3 ) / 4 );

			ms = Decrypt( h, fish );

			dataStart = headerStart + byteCount + ( ( ~byteCount + 1 ) & 7 );

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

		uint[] ReadUints(BinaryReader r, int count)
		{
			uint[] ret = new uint[count];
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
		
		public static void CreateMix(string filename, List<string> contents)
		{
			// Construct a list of entries for the file header
			ushort numFiles = 0;
			uint dataSize = 0;
			List<PackageEntry> items = new List<PackageEntry>();
			foreach (var file in contents)
			{				
				uint length = (uint) new FileInfo(file).Length;
				uint hash = PackageEntry.HashFilename(Path.GetFileName(file));
				items.Add(new PackageEntry(hash, dataSize, length));
				dataSize += length;
				numFiles++;
			}
			
			
			Stream s = new FileStream(filename, FileMode.Create);
			var writer = new BinaryWriter(s);
			// Write file header
			writer.Write(numFiles);
			writer.Write(dataSize);
			foreach(var item in items)
				item.Write(writer);
			
			writer.Flush();
			
			// Copy file data
			foreach (var file in contents)
			{
				var f = File.Open(file,FileMode.Open);
				CopyStream(f,s);
				f.Close();
			}
			
			writer.Close();
			s.Close();
		}
		
		static void CopyStream (Stream readStream, Stream writeStream)
		{
   			var Length = 256;
			Byte[] buffer = new Byte[Length];
   			int bytesRead = readStream.Read(buffer,0,Length);

 			while( bytesRead > 0 ) 
    		{
        		writeStream.Write(buffer,0,bytesRead);
        		bytesRead = readStream.Read(buffer,0,Length);
    		}
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
	}

	[Flags]
	enum MixFileFlags : uint
	{
		Checksum = 0x10000,
		Encrypted = 0x20000,
	}
}
