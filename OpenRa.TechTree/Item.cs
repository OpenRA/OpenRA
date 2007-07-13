using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using OpenRa.FileFormats;
using System.IO;

namespace OpenRa.TechTree
{
	public class Item
	{
		public Item(string tag, string friendlyName, IniSection section, bool isStructure)
		{
			this.tag = tag;
			this.friendlyName = friendlyName;

			owner = ParseOwner(section);
			techLevel = ParseTechLevel(section);
			Tuple<string[], string[]> pre = ParsePrerequisites(section, tag);
			alliedPrerequisites = pre.a;
			sovietPrerequisites = pre.b;
		}

		static int ParseTechLevel(IniSection section)
		{
			return int.Parse(section.GetValue("TechLevel", "-1"));
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

		static Tuple<string[],string[]> ParsePrerequisites(IniSection section, string tag)
		{
			List<string> allied = new List<string>(section.GetValue("Prerequisite", "").ToUpper().Split(
				new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

			List<string> soviet = new List<string>(allied);

			if (allied.Remove("STEK"))
				allied.Add("ATEK");

			if (soviet.Remove("ATEK"))
				soviet.Add("STEK");

			if (soviet.Remove("TENT"))
				soviet.Add("BARR");

			if (allied.Remove("BARR"))
				allied.Add("TENT");

			if ((tag.Length == 2 && tag[0] == 'E') || tag == "MEDI" || tag == "THF" || tag == "SPY")
			{
				if (!allied.Contains("TENT"))
					allied.Add("TENT");
				if (!soviet.Contains("BARR"))
					soviet.Add("BARR");
			}

			if (tag == "LST")
			{
				if (!soviet.Contains("SPEN"))
					soviet.Add("SPEN");

				if (!allied.Contains("SYRD"))
					allied.Add("SYRD");
			}

			return new Tuple<string[], string[]>(
				allied.ToArray(), soviet.ToArray());
		}

		public readonly string tag, friendlyName;
		readonly int techLevel;
		readonly Race owner;
		readonly string[] alliedPrerequisites, sovietPrerequisites;

		bool ShouldMakeBuildable(IEnumerable<string> buildings, string[] racePrerequisites)
		{
			if (techLevel < 0)
				return false;

			if (racePrerequisites.Length == 0)
				return true;

			List<string> p = new List<string>(racePrerequisites);
			foreach (string b in buildings)
				p.Remove(b);

			return p.Count == 0;
		}

		bool ShouldMakeUnbuildable(IEnumerable<string> buildings, string[] racePrerequisites)
		{
			if (racePrerequisites.Length == 0)
				return false;

			List<string> p = new List<string>(racePrerequisites);
			foreach (string b in buildings)
				p.Remove(b);

			return p.Count == racePrerequisites.Length;
		}

		public void CheckPrerequisites(IEnumerable<string> buildings, Race currentRace)
		{
			string[] racePrerequisites = (currentRace == Race.Allies) ? alliedPrerequisites : sovietPrerequisites;

			if ((canBuild && ShouldMakeUnbuildable(buildings, racePrerequisites)) || !((owner & currentRace) == currentRace))
				canBuild = false;
			else if (!canBuild && ShouldMakeBuildable(buildings, racePrerequisites))
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
					return BitmapBuilder.FromBytes(h.Image, reader.Size, palette);

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
