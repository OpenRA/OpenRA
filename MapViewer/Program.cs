using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using OpenRa.FileFormats;

namespace MapViewer
{
	class Program
	{
		static Stream GetFile()
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.RestoreDirectory = true;
			ofd.Filter = "Map files (*.ini;*.mpr)|*.ini;*.mpr";

			return (DialogResult.OK == ofd.ShowDialog()) ? ofd.OpenFile() : null;
		}

		static byte ReadByte( Stream s )
		{
			int ret = s.ReadByte();
			if( ret == -1 )
				throw new NotImplementedException ();
			return (byte)ret;
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

			Map map = new Map( iniFile );

			Console.WriteLine( "Name: {0}", map.Title );

			IniSection basic = iniFile.GetSection( "Basic" );
			Console.WriteLine( "Official: {0}", basic.GetValue( "Official", "no" ) );

			Console.WriteLine( "Theater: {0}", map.Theater );
			Console.WriteLine( "X: {0} Y: {1} Width: {2} Height: {3}",
				map.XOffset, map.YOffset, map.Width, map.Height );

			foreach( TileReference r in map.MapTiles )
				Console.WriteLine( "{0:x4}.{1:x2} ", r.tile, r.image );
		}
	}
}
