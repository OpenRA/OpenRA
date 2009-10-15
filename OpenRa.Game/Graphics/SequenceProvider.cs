using System.Collections.Generic;
using System.Xml;
using OpenRa.FileFormats;

namespace OpenRa.Game.Graphics
{
	static class SequenceProvider
	{
		static Dictionary<string, Dictionary<string, Sequence>> units =
			new Dictionary<string, Dictionary<string, Sequence>>();

		static Dictionary<string, CursorSequence> cursors = new Dictionary<string, CursorSequence>();

		static SequenceProvider()
		{
			XmlDocument document = new XmlDocument();
			document.Load(FileSystem.Open("sequences.xml"));

			foreach (XmlElement eUnit in document.SelectNodes("/sequences/unit"))
				LoadSequencesForUnit(eUnit);

			foreach (XmlElement eCursor in document.SelectNodes("/sequences/cursor"))
				LoadSequencesForCursor(eCursor);
		}

		static void LoadSequencesForCursor(XmlElement eCursor)
		{
			string cursorSrc = eCursor.GetAttribute("src");

			foreach (XmlElement eSequence in eCursor.SelectNodes("./sequence"))
				cursors.Add(eSequence.GetAttribute("name"), new CursorSequence(cursorSrc, eSequence));

			Log.Write("* LoadSequencesForCursor() done");
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

		public static CursorSequence GetCursorSequence(string cursor)
		{
			return cursors[cursor];
		}
	}
}
