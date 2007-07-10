using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using OpenRa.FileFormats;
using System.IO;

namespace OpenRa.TechTreeTest
{
	class Item
	{
		public Item(string tag, string friendlyName, IniSection section, bool isStructure)
		{
			this.tag = tag;
			this.friendlyName = friendlyName;

			owner = ParseOwner(section);
			techLevel = ParseTechLevel(section);
			prerequisites = ParsePrerequisites(section);
		}

		static int ParseTechLevel(IniSection section)
		{
			return int.Parse(section.GetValue("TechLevel", "-1"));
		}

		static string[] ParsePrerequisites(IniSection section)
		{
			return section.GetValue("Prerequisite", "").ToUpper().Split(
				new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		}

		static Race ParseOwner(IniSection section)
		{
			if (section.GetValue("DoubleOwned", "No") == "Yes")
				return Race.Allies | Race.Soviet;

			Race race = Race.None;
			string[] frags = section.GetValue("Owner", "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (string s in frags)
				race |= (Race)Enum.Parse(typeof(Race), s, true);

			return race;
		}

		public readonly string tag, friendlyName;
		readonly int techLevel;
		readonly Race owner;
		readonly string[] prerequisites;

		bool ShouldMakeBuildable(IEnumerable<string> buildings)
		{
			if (techLevel < 0)
				return false;

			if (prerequisites.Length == 0)
				return true;

			List<string> p = new List<string>(prerequisites);
			foreach (string b in buildings)
				p.Remove(b);

			return p.Count == 0;
		}

		bool ShouldMakeUnbuildable(IEnumerable<string> buildings)
		{
			if (prerequisites.Length == 0)
				return false;

			List<string> p = new List<string>(prerequisites);
			foreach (string b in buildings)
				p.Remove(b);

			return p.Count == prerequisites.Length;
		}

		public void CheckPrerequisites(IEnumerable<string> buildings, Race currentRace)
		{
			if ((canBuild && ShouldMakeUnbuildable(buildings)) || !((owner & currentRace) == currentRace))
				canBuild = false;
			else if (!canBuild && ShouldMakeBuildable(buildings))
				canBuild = true;
		}

		bool canBuild;
		public bool CanBuild { get { return canBuild; } }

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

		public string Tooltip
		{
			get
			{
				return string.Format("{0} ({1})\n{2}", friendlyName, tag, owner);
			}
		}
	}
}
