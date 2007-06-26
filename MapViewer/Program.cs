using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace MapViewer
{
	class Program
	{
		static Stream GetFile()
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.RestoreDirectory = true;
			ofd.Filter = "Map files (*.ini)|*.ini";

			return (DialogResult.OK == ofd.ShowDialog()) ? ofd.OpenFile() : null;
		}

		static void Main(string[] args)
		{
			Stream s = GetFile();
			if (s == null)
			{
				Console.WriteLine("Fail");
				return;
			}

			IniFile iniFile = new IniFile(s);
			Console.WriteLine("Done.");

			IniSection basic = iniFile.GetSection("Basic");
			Console.WriteLine("Name: {0}", basic.GetValue("Name", "(null)"));
			Console.WriteLine("Official: {0}", basic.GetValue("Official", "no"));

			IniSection map = iniFile.GetSection("Map");
			Console.WriteLine("Theater: {0}", map.GetValue("Theater", "TEMPERATE"));
			Console.WriteLine("X: {0} Y: {1} Width: {2} Height: {3}",
				map.GetValue("X", "0"), map.GetValue("Y", "0"),
				map.GetValue("Width", "0"), map.GetValue("Height", "0"));
		}
	}
}
