using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using OpenRa.FileFormats;

namespace OpenRa.Game
{
	static class SequenceProvider
	{
		static Dictionary<string, Dictionary<string, Sequence>> units =
			new Dictionary<string, Dictionary<string, Sequence>>();

		static SequenceProvider()
		{
			XmlDocument document = new XmlDocument();
			document.Load(FileSystem.Open("sequences.xml"));

			foreach (XmlElement eUnit in document.SelectNodes("/sequences/unit"))
				LoadSequencesForUnit(eUnit);
		}

		public static void ForcePrecache() { }	// force static ctor to run

		static void LoadSequencesForUnit(XmlElement eUnit)
		{
			string unitName = eUnit.GetAttribute("name");
			Dictionary<string, Sequence> sequences = new Dictionary<string, Sequence>();

			foreach (XmlElement eSequence in eUnit.SelectNodes("./sequence"))
				sequences.Add(eSequence.GetAttribute("name"), new Sequence(unitName, eSequence));

			units.Add(unitName, sequences);
		}

		public static Sequence GetSequence(string unitName, string sequenceName)
		{
			return units[unitName][sequenceName];
		}
	}
}
