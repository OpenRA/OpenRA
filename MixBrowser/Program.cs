using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace MixBrowser
{
	class Program
	{
		static string GetFilename(string[] args)
		{
			if (args.Length == 0)
			{
				OpenFileDialog ofd = new OpenFileDialog();
				ofd.RestoreDirectory = true;
				ofd.Filter = "MIX files (*.mix)|*.mix|All Files (*.*)|*.*";

				return DialogResult.OK == ofd.ShowDialog() ? ofd.FileName : null;
			}

			return args[0];
		}

		static void Main(string[] args)
		{
			string fn = GetFilename(args);
			if (fn == null)
			{
				Console.WriteLine("FAIL");
				return;
			}

			Stream s = File.Open(fn, FileMode.Open, FileAccess.Read);
			BinaryReader reader = new BinaryReader(s);

			uint signature = reader.ReadUInt32();

			if (0 == (signature & ~(uint)(MixFileFlags.Checksum | MixFileFlags.Encrypted)))
			{
				Console.WriteLine("{0} - Red Alert MIX", Path.GetFileName(fn));
				if (0 != (signature & (uint)MixFileFlags.Checksum))
					Console.WriteLine("Checksum: YES");
				if (0 != (signature & (uint)MixFileFlags.Encrypted))
				{
					Console.WriteLine("Encrypted: YES");

					//get the blowfish key
					byte[] bfkey = MixDecrypt.MixDecrypt.BlowfishKey(reader.ReadBytes(80));

					Blowfish fish = new Blowfish(bfkey);

					uint[] data = { reader.ReadUInt32(), reader.ReadUInt32() };
					uint[] decrypted = fish.Decrypt(data);

					MemoryStream ms = new MemoryStream();
					BinaryWriter w = new BinaryWriter(ms);
					foreach (uint u in decrypted)
						w.Write(u);
					w.Flush();
					ms.Seek(0, SeekOrigin.Begin);

					BinaryReader reader2 = new BinaryReader(ms);

					ushort numfiles2 = reader2.ReadUInt16();
					uint datasize2 = reader2.ReadUInt32();

					Console.WriteLine("{0} files - {1} KB", numfiles2, datasize2 >> 10);

					return;
				}
			}
			else
				Console.WriteLine("{0} - Tiberian Dawn MIX", Path.GetFileName(fn) );

			s.Seek(0, SeekOrigin.Begin);
			reader = new BinaryReader(s);

			ushort numfiles = reader.ReadUInt16();
			uint datasize = reader.ReadUInt32();
			Console.WriteLine("{0} files - {1} KB", numfiles, datasize >> 10);

			List<MixEntry> index = new List<MixEntry>();
			for (ushort i = 0; i < numfiles; i++)
				index.Add(new MixEntry(reader));

			if (File.Exists("files.txt"))
				foreach (string filename in File.ReadAllLines("files.txt"))
					MixEntry.AddStandardName(filename);
			else
				Console.WriteLine("-- files.txt doesnt exist --");

			foreach (MixEntry e in index)
				Console.WriteLine(e);
		}
	}

	[Flags]
	enum MixFileFlags : uint
	{
		Checksum = 0x10000,
		Encrypted = 0x20000,
	}
}
