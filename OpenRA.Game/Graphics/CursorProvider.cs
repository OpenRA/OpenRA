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
	public static class CursorProvider
	{
		static Dictionary<string, CursorSequence> cursors;

		public static void Initialize(string[] sequenceFiles)
		{
			cursors = new Dictionary<string, CursorSequence>();
			
			foreach (var f in sequenceFiles)
				LoadSequenceSource(f);
		}

		static void LoadSequenceSource(string filename)
		{
			XmlDocument document = new XmlDocument();
			document.Load(FileSystem.Open(filename));
				
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
