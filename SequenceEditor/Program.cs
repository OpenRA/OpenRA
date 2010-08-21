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
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using OpenRA.FileFormats;

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

				var reader = new ShpReader(FileSystem.OpenWithExts(shp, ".tem", ".sno", ".int", ".shp"));
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

			if (args.Length != 3)
			{
				MessageBox.Show( "usage: SequenceEditor mod[,mod]* sequences-file.xml palette.pal");
				return;
			}

			var mods = args[0].Split(',');
			var manifest = new Manifest(mods);
			FileSystem.LoadFromManifest( manifest );

			XmlFilename = args[1];
			Doc = new XmlDocument(); 
			Doc.Load(XmlFilename);

			var tempPal = new Palette(FileSystem.Open(args[2]), true);
			Pal = tempPal;

			UnitName = GetTextForm.GetString("Unit to edit?", "e1");
			if (string.IsNullOrEmpty(UnitName))
				return;

			LoadAndResolve(UnitName); 

			var xpath = string.Format("//unit[@name=\"{0}\"]/sequence", UnitName);
			foreach (XmlElement e in Doc.SelectNodes(xpath))
			{
				if (e.HasAttribute("src"))
					LoadAndResolve(e.GetAttribute("src"));
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
