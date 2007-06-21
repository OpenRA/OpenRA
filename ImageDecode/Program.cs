using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

namespace ImageDecode
{
	public static class Program
	{
		[STAThread]
		public static void Main()
		{
			ShpReader.Read( File.OpenRead( "stek.shp" ) );
		}
	}
}
