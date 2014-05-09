#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public static class CursorProvider
	{
		static HardwarePalette palette;
		static Dictionary<string, CursorSequence> cursors;
		static Cache<string, PaletteReference> palettes;

		static PaletteReference CreatePaletteReference(string name)
		{
			var pal = palette.GetPalette(name);
			if (pal == null)
				throw new InvalidOperationException("Palette `{0}` does not exist".F(name));

			return new PaletteReference(name, palette.GetPaletteIndex(name), pal);
		}

		public static void Initialize(string[] sequenceFiles)
		{
			cursors = new Dictionary<string, CursorSequence>();
			palettes = new Cache<string, PaletteReference>(CreatePaletteReference);
			var sequences = new MiniYaml(null, sequenceFiles.Select(s => MiniYaml.FromFile(s)).Aggregate(MiniYaml.MergeLiberal));
			var shadowIndex = new int[] { };

			if (sequences.NodesDict.ContainsKey("ShadowIndex"))
			{
				Array.Resize(ref shadowIndex, shadowIndex.Length + 1);
				Exts.TryParseIntegerInvariant(sequences.NodesDict["ShadowIndex"].Value,
					out shadowIndex[shadowIndex.Length - 1]);
			}

			palette = new HardwarePalette();
			foreach (var p in sequences.NodesDict["Palettes"].Nodes)
				palette.AddPalette(p.Key, new Palette(GlobalFileSystem.Open(p.Value.Value), shadowIndex), false);

			foreach (var s in sequences.NodesDict["Cursors"].Nodes)
				LoadSequencesForCursor(s.Key, s.Value);

			palette.Initialize();
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
			var cursorSequence = GetCursorSequence(cursorName);
			var cursorSprite = cursorSequence.GetSprite(cursorFrame);

			renderer.SetPalette(palette);
			renderer.SpriteRenderer.DrawSprite(cursorSprite,
			                                   lastMousePos - cursorSequence.Hotspot - (0.5f * cursorSprite.size).ToInt2(),
			                                   palettes[cursorSequence.Palette],
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
