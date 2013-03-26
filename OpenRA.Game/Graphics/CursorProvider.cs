#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public static class CursorProvider
	{
		public static HardwarePalette Palette;
		static Dictionary<string, CursorSequence> cursors;

		public static void Initialize(string[] sequenceFiles)
		{
			cursors = new Dictionary<string, CursorSequence>();
			var sequences = new MiniYaml(null, sequenceFiles.Select(s => MiniYaml.FromFile(s)).Aggregate(MiniYaml.MergeLiberal));
			int[] ShadowIndex = { };

			if (sequences.NodesDict.ContainsKey("ShadowIndex"))
			{
				Array.Resize(ref ShadowIndex, ShadowIndex.Length + 1);
				ShadowIndex[ShadowIndex.Length - 1] = Convert.ToInt32(sequences.NodesDict["ShadowIndex"].Value);
			}

			var palettes = new Dictionary<string, Palette>();
			foreach (var s in sequences.NodesDict["Palettes"].Nodes)
				palettes.Add(s.Key, new Palette(FileSystem.Open(s.Value.Value), ShadowIndex));

			Palette = new HardwarePalette();
			foreach (var p in palettes)
				Palette.AddPalette(p.Key, p.Value, false);

			// Generate initial palette texture
			Palette.Update(new IPaletteModifier[] {});

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

		public static void DrawCursor(Renderer renderer, string cursorName, int2 lastMousePos, int cursorFrame)
		{
			var cursorSequence = CursorProvider.GetCursorSequence(cursorName);
			var cursorSprite = cursorSequence.GetSprite(cursorFrame);
						
			renderer.SpriteRenderer.DrawSprite(cursorSprite,
			                                   lastMousePos - cursorSequence.Hotspot,
			                                   Palette.GetPaletteIndex(cursorSequence.Palette),
			                                   cursorSprite.size);
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
