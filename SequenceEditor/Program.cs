using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using OpenRa.FileFormats;
using System.Xml;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SequenceEditor
{
	static class Program
	{
		static string XmlFilename;
		public static string UnitName;
		public static XmlDocument Doc;
		public static Dictionary<string, Bitmap[]> Shps = new Dictionary<string, Bitmap[]>();
		public static Palette Pal;
		public static Dictionary<string, Sequence> Sequences = new Dictionary<string, Sequence>();

		public static void LoadAndResolve( string shp )
		{
			try
			{
				if (Shps.ContainsKey(shp)) return;

				var reader = new ShpReader(FileSystem.OpenWithExts(shp, ".shp", ".tem", ".sno", ".int"));
				Shps[shp] = reader.Select(ih =>
					{
						var bmp = new Bitmap(reader.Width, reader.Height);
						for (var j = 0; j < bmp.Height; j++)
							for (var i = 0; i < bmp.Width; i++)
								bmp.SetPixel(i, j, Pal.GetColor(ih.Image[j * bmp.Width + i]));
						return bmp;
					}).ToArray();
			}
			catch { }
		}

		public static void Save()
		{
			var e = Doc.SelectSingleNode(string.Format("//unit[@name=\"{0}\"]", UnitName)) as XmlElement;
			if (e == null)
			{
				e = Doc.CreateElement("unit");
				e.SetAttribute( "name", UnitName );
				e = Doc.SelectSingleNode("sequences").AppendChild(e) as XmlElement;
			}

			while (e.HasChildNodes) e.RemoveChild(e.FirstChild);	/* what a fail */

			foreach (var s in Sequences)
			{
				var seqnode = Doc.CreateElement("sequence");
				seqnode.SetAttribute("name", s.Key);
				seqnode.SetAttribute("start", s.Value.start.ToString());
				seqnode.SetAttribute("length", s.Value.length.ToString());
				if (s.Value.shp != UnitName)
					seqnode.SetAttribute("src", s.Value.shp);

				e.AppendChild(seqnode);
			}

			Doc.Save(XmlFilename);
		}

		[STAThread]
		static void Main( string[] args )
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			try
			{
				FileSystem.MountDefault( true );
			}
			catch( FileNotFoundException fnf )
			{
				if( fnf.FileName != "expand2.mix" )
					throw new InvalidOperationException( "Unable to load MIX files" );
			}

			XmlFilename = args.FirstOrDefault( x => x.EndsWith(".xml") ) ?? "sequences.xml";
			Doc = new XmlDocument(); 
			Doc.Load(XmlFilename);

			Pal = new Palette(FileSystem.Open("temperat.pal"));

			UnitName = args.FirstOrDefault( x => !x.EndsWith(".xml") );
			if (UnitName == null)
				UnitName = GetTextForm.GetString("Unit to edit?", "e1");
			if (UnitName == null)
				return;

			LoadAndResolve(UnitName); 

			var xpath = string.Format("//unit[@name=\"{0}\"]/sequence", UnitName);
			foreach (XmlElement e in Doc.SelectNodes(xpath))
			{
				if (e.HasAttribute("src"))
				{
					var src = e.GetAttribute("src");
					LoadAndResolve(src);
				}
				Sequences[e.GetAttribute("name")] = new Sequence(e);
			}

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
			start = int.Parse(e.GetAttribute("start"));
			shp = e.GetAttribute("src");
			if (shp == "") shp = Program.UnitName;
			var a = e.GetAttribute("length");

			length = (a == "*")
				? Program.Shps[shp].Length - start
				: ((a == "") ? 1 : int.Parse(a));
		}

		public Sequence() { }
	}
}
