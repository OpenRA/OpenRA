#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
			var sequences = new MiniYaml(null, sequenceFiles.Select(s => MiniYaml.FromFile(s)).Aggregate(MiniYaml.MergeLiberal));
			var transparent = false;

			if (sequences.NodesDict.ContainsKey("Transparent"))
				transparent = true;

			foreach (var s in sequences.NodesDict["Palettes"].Nodes)
				Game.modData.Palette.AddPalette(s.Key, new Palette(FileSystem.Open(s.Value.Value), transparent));

			foreach (var s in sequences.NodesDict["Cursors"].Nodes)
				LoadSequencesForCursor(s.Key, s.Value);
		}

		static void LoadSequencesForCursor(string cursorSrc, MiniYaml cursor)
		{
			Game.modData.LoadScreen.Display();

			foreach (var sequence in cursor.Nodes)
				cursors.Add(sequence.Key, new CursorSequence(cursorSrc, cursor.Value, sequence.Value));
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
