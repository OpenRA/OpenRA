using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using OpenRa.FileFormats;
using System.Xml;
using System.Drawing;
using System.Drawing.Imaging;

namespace SequenceEditor
{
	static class Program
	{
		public static string UnitName;
		public static XmlDocument Doc;
		public static Dictionary<string, Bitmap[]> Shps = new Dictionary<string, Bitmap[]>();
		public static Palette Pal;
		public static Dictionary<string, Sequence> Sequences = new Dictionary<string, Sequence>();

		static Bitmap[] LoadAndResolve( string shp )
		{
			var reader = new ShpReader(FileSystem.Open(shp + ".shp"));
			return reader.Select(ih =>
				{
					var bmp = new Bitmap(reader.Width, reader.Height);
					for (var j = 0; j < bmp.Height; j++)
						for (var i = 0; i < bmp.Width; i++)
							bmp.SetPixel(i, j, Pal.GetColor(ih.Image[j * bmp.Width + i]));
					return bmp;
				}).ToArray();
		}

		[STAThread]
		static void Main( string[] args )
		{
			FileSystem.Mount(new Folder("./"));
			var packages = new[] { "redalert", "conquer", "hires", "general", "local" };

			foreach( var p in packages )
				FileSystem.Mount( new Package( p + ".mix" ));

			Doc = new XmlDocument(); 
			Doc.Load("sequences.xml");

			Pal = new Palette(FileSystem.Open("temperat.pal"));

			UnitName = args.First();
			Shps[UnitName] = LoadAndResolve(UnitName);

			/* todo: load supplemental SHPs */

			var xpath = string.Format("//unit[@name=\"{0}\"]/sequence", UnitName);
			foreach (XmlElement e in Doc.SelectNodes(xpath))
			{
				if (e.HasAttribute("src"))
				{
					var src = e.GetAttribute("src");
					if (!Shps.ContainsKey(src))
						Shps[src] = LoadAndResolve(src);
				}
				Sequences[e.GetAttribute("name")] = new Sequence(e);
			}

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
		}
	}

	class Sequence
	{
		public int start;
		public int length;
		public string shp;

		public Sequence(XmlElement e)
		{

		}
	}
}
