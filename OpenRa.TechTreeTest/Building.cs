using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;
using System.IO;

namespace OpenRa.TechTreeTest
{
	[Flags]
	public enum Race
	{
		None = 0,
		Allies = 1,
		Soviet = 2
	}

	class Building : IRAUnit
	{
		readonly string friendlyName;
		readonly string tag;

		public string FriendlyName
		{
			get { return friendlyName; }
		}

		public string Tag
		{
			get { return tag; }
		}

		string[] prerequisites;

		public string[] Prerequisites
		{
			get { return prerequisites; }
			set { prerequisites = value; }
		}

		int techLevel;

		public int TechLevel
		{
			get { return techLevel; }
			set { techLevel = value; }
		}

		Race owner;

		public Race Owner
		{
			get { return owner; }
			set { owner = value; }
		}

		public Building(string tag, string friendlyName)
		{
			this.friendlyName = friendlyName;
			this.tag = tag;
		}

		public bool ShouldMakeBuildable(IEnumerable<string> buildings)
		{
			if (techLevel > 10 || techLevel < 0)
				return false;

			if (prerequisites.Length == 0)
				return true;

			List<string> p = new List<string>(prerequisites);
			foreach (string b in buildings)
				p.Remove(b);

			return p.Count == 0;
		}

		public bool ShouldMakeUnbuildable(IEnumerable<string> buildings)
		{
			if (prerequisites.Length == 0)
				return false;

			List<string> p = new List<string>(prerequisites);
			foreach (string b in buildings)
				p.Remove(b);

			return p.Count == prerequisites.Length;
		}

		bool buildable = false;
		public bool Buildable { get { return buildable; } }

		public void CheckPrerequisites(IEnumerable<string> buildings, Race currentRace)
		{
			if ((buildable && ShouldMakeUnbuildable(buildings)) || !((owner & currentRace) == currentRace))
				buildable = false;
			else if (!buildable && ShouldMakeBuildable(buildings))
				buildable = true;
		}

		Bitmap icon;
		public Bitmap Icon
		{
			get { return icon ?? (icon = LoadIcon(tag)); }
		}

		static Package package = new Package("../../../hires.mix");
		static Palette palette = new Palette( File.OpenRead("../../../temperat.pal"));

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
	}
}
