using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace OpenRa.FileFormats
{
	public class Package
	{
		readonly string filename;
		readonly List<PackageEntry> index;
		readonly bool isRmix, isEncrypted;

		public ICollection<PackageEntry> Content
		{
			get { return index.AsReadOnly(); }
		}

		public Package(string filename)
		{
			this.filename = filename;
			using (Stream s = File.OpenRead(filename))
			{
				BinaryReader reader = new BinaryReader(s);
				uint signature = reader.ReadUInt32();

				isRmix = 0 == (signature & ~(uint)(MixFileFlags.Checksum | MixFileFlags.Encrypted));

				if (isRmix)
				{
					isEncrypted = 0 != (signature & (uint)MixFileFlags.Encrypted);
					index = ParseRaHeader(s);
				}
				else
				{
					isEncrypted = false;
					s.Seek(0, SeekOrigin.Begin);
					index = ParseTdHeader(s);
				}
			}
		}

		List<PackageEntry> ParseRaHeader(Stream s)
		{
			if (!isEncrypted)
			{
				Console.WriteLine("RA, not encrypted");
				return ParseTdHeader(s);
			}

			long headerStart = 84;
			BinaryReader reader = new BinaryReader(s);
			byte[] keyblock = reader.ReadBytes(80);
			byte[] blowfishKey = MixDecrypt.MixDecrypt.BlowfishKey(keyblock);

			uint[] h = ReadUints(reader, 2);

			Blowfish fish = new Blowfish(blowfishKey);
			uint[] decrypted = fish.Decrypt(h);

			MemoryStream ms = new MemoryStream();
			BinaryWriter writer = new BinaryWriter(ms);
			foreach (uint t in decrypted)
				writer.Write(t);
			writer.Flush();

			ms.Position = 0;
			BinaryReader reader2 = new BinaryReader(ms);

			ushort numFiles = reader2.ReadUInt16();
			uint datasize = reader2.ReadUInt32();

			Console.WriteLine("{0} files, {1} kb", numFiles, datasize >> 10);

			s.Position = headerStart;
			reader = new BinaryReader(s);

			h = ReadUints(reader, 2 + numFiles * PackageEntry.Size / 4);
			decrypted = fish.Decrypt(h);

			ms = new MemoryStream();
			writer = new BinaryWriter(ms);
			foreach (uint t in decrypted)
				writer.Write(t);
			writer.Flush();

			ms.Position = 0;

			return ParseTdHeader(ms);
		}

		uint[] ReadUints(BinaryReader r, int count)
		{
			uint[] ret = new uint[count];
			for (int i = 0; i < ret.Length; i++)
				ret[i] = r.ReadUInt32();

			return ret;
		}

		List<PackageEntry> ParseTdHeader(Stream s)
		{
			List<PackageEntry> items = new List<PackageEntry>();

			BinaryReader reader = new BinaryReader(s);
			ushort numFiles = reader.ReadUInt16();
			uint dataSize = reader.ReadUInt32();

			for (int i = 0; i < numFiles; i++)
				items.Add(new PackageEntry(reader));

			return items;
		}

		public Stream GetContent(uint hash)
		{
			foreach( PackageEntry e in index )
				if (e.Hash == hash)
				{
					using (Stream s = File.OpenRead(filename))
					{
						s.Seek(2 + 4 + (isRmix ? 4 : 0), SeekOrigin.Begin);

						s.Seek(2, SeekOrigin.Current);	//dword align

						if (isEncrypted)
							s.Seek(80, SeekOrigin.Current);

						s.Seek(index.Count * PackageEntry.Size + e.Offset, SeekOrigin.Current);
						byte[] data = new byte[ e.Length ];
						s.Read( data, 0, (int)e.Length );
						return new MemoryStream(data);
					}
				}

			throw new FileNotFoundException();
		}

		public Stream GetContent(string filename)
		{
			return GetContent(PackageEntry.HashFilename(filename));
		}

	}

	[Flags]
	enum MixFileFlags : uint
	{
		Checksum = 0x10000,
		Encrypted = 0x20000,
	}
}
