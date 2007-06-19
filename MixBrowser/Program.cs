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

			MixFile file = new MixFile(fn);

			if (File.Exists("files.txt"))
				foreach (string filename in File.ReadAllLines("files.txt"))
					MixEntry.AddStandardName(filename);
			else
				Console.WriteLine("-- files.txt doesnt exist --");

			foreach (MixEntry e in file.Content)
				Console.WriteLine(e);

			try
			{
				Stream s = file.GetContent("rules.ini");
				StreamReader reader = new StreamReader(s);

				while( !reader.EndOfStream )
					Console.WriteLine(reader.ReadLine());
			}
			catch (FileNotFoundException)
			{
				Console.WriteLine("FAIL at finding rules.ini");
			}
		}
	}
}
