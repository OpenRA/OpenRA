using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using OpenRa.FileFormats;
using System.IO;

namespace OpenRa.TechTreeTest
{
	class Unit : IRAUnit
	{
		bool buildable;
		public bool Buildable { get { return buildable; } }

		public void CheckPrerequisites(IEnumerable<string> units, Race currentRace)
		{
			if ((buildable && ShouldMakeUnbuildable(units)) || !((owner & currentRace) == currentRace))
				buildable = false;
			else if (!buildable && ShouldMakeBuildable(units))
				buildable = true; ;
		}

		readonly string friendlyName; 
		public string FriendlyName { get { return friendlyName; } }

		Bitmap icon;
		public Bitmap Icon
		{
			get { return icon ?? (icon = LoadIcon(tag)); }
		}

		static Package package = new Package("../../../hires.mix");
		static Palette palette = new Palette(File.OpenRead("../../../temperat.pal"));

		static Bitmap LoadIcon(string tag)
		{
			string filename = tag + "icon.shp";

			try
			{
				Stream s = package.GetContent(filename);
				ShpReader reader = new ShpReader(s);
				foreach (ImageHeader h in reader)
					return BitmapBuilder.FromBytes(h.Image, reader.Width, reader.Height, palette);

				return null;
			}
			catch (FileNotFoundException) { return LoadIcon("dog"); }
		}

		Race owner;
		public Race Owner
		{
			get { return owner; }
			set { owner = value; }
		}

		string[] prerequisites;
		public string[] Prerequisites
		{
			get { return prerequisites; }
			set { prerequisites = value; }
		}

		public bool ShouldMakeBuildable(IEnumerable<string> units)
		{
			if (techLevel > 10 || techLevel < 0)
				return false;

			if (prerequisites.Length == 0)
				return true;

			List<string> p = new List<string>(prerequisites);
			foreach (string b in units)
				p.Remove(b);

			return p.Count == 0;
		}

		public bool ShouldMakeUnbuildable(IEnumerable<string> units)
		{
			if (prerequisites.Length == 0)
				return false;

			List<string> p = new List<string>(prerequisites);
			foreach (string b in units)
				p.Remove(b);

			return p.Count == prerequisites.Length;
		}

		readonly string tag;
		public string Tag
		{
			get { return tag; }
		}

		int techLevel;
		public int TechLevel
		{
			get { return techLevel; }
			set { techLevel = value; }
		}

		public Unit(string tag, string friendlyName)
		{
			this.friendlyName = friendlyName;
			this.tag = tag;
		}
	}
}
