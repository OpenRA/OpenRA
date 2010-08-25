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
using System.IO;
using System.Linq;
using System.Xml;
using OpenRA.FileFormats;

namespace OpenRA.Graphics
{
	public static class SequenceProvider
	{
		static Dictionary<string, Dictionary<string, Sequence>> units;
		static Dictionary<string, CursorSequence> cursors;

		public static void Initialize(string[] sequenceFiles)
		{
			units = new Dictionary<string, Dictionary<string, Sequence>>();
			cursors = new Dictionary<string, CursorSequence>();
			
			foreach (var f in sequenceFiles)
				LoadSequenceSource(f);
		}

		static void LoadSequenceSource(string filename)
		{
			XmlDocument document = new XmlDocument();
			document.Load(FileSystem.Open(filename));

			foreach (XmlElement eUnit in document.SelectNodes("/sequences/unit"))
				LoadSequencesForUnit(eUnit);

			foreach (XmlElement eCursor in document.SelectNodes("/sequences/cursor"))
				LoadSequencesForCursor(eCursor);
		}

		static void LoadSequencesForCursor(XmlElement eCursor)
		{
			Game.modData.LoadScreen.Display();
			string cursorSrc = eCursor.GetAttribute("src");
			string palette = eCursor.GetAttribute("palette");

			foreach (XmlElement eSequence in eCursor.SelectNodes("./sequence"))
				cursors.Add(eSequence.GetAttribute("name"), new CursorSequence(cursorSrc, palette, eSequence));

		}

		static void LoadSequencesForUnit(XmlElement eUnit)
		{
			Game.modData.LoadScreen.Display();
			string unitName = eUnit.GetAttribute("name");
			try {
				var sequences = eUnit.SelectNodes("./sequence").OfType<XmlElement>()
					.Select(e => new Sequence(unitName, e))
					.ToDictionary(s => s.Name);
				
				units.Add(unitName, sequences);
			} catch (FileNotFoundException) {} // Do nothing; we can crash later if we actually wanted art	
		}

		public static Sequence GetSequence(string unitName, string sequenceName)
		{
			try { return units[unitName][sequenceName]; }
			catch (KeyNotFoundException)
			{
				throw new InvalidOperationException(
					"Unit `{0}` does not have a sequence `{1}`".F(unitName, sequenceName));
			}
		}

		public static bool HasSequence(string unit, string seq)
		{
			return units[unit].ContainsKey(seq);
		}
		
		public static bool HasCursorSequence(string cursor)
		{
			return cursors.ContainsKey(cursor);
		}

		public static CursorSequence GetCursorSequence(string cursor)
		{
			try { return cursors[cursor]; }
			catch (KeyNotFoundException)
			{
				throw new InvalidOperationException(
					"Cursor does not have a sequence `{0}`".F(cursor));
			}
		}
	}
}
