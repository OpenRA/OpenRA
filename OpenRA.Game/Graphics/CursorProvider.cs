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
	public class CursorProvider
	{
		HardwarePalette palette;
		Dictionary<string, CursorSequence> cursors;
		Cache<string, PaletteReference> palettes;

		public CursorProvider(ModData modData)
		{
			var sequenceFiles = modData.Manifest.Cursors;

			cursors = new Dictionary<string, CursorSequence>();
			palettes = new Cache<string, PaletteReference>(CreatePaletteReference);
			var sequences = new MiniYaml(null, sequenceFiles.Select(s => MiniYaml.FromFile(s)).Aggregate(MiniYaml.MergeLiberal));
			var shadowIndex = new int[] { };

			var nodesDict = sequences.GetNodesDict();
			if (nodesDict.ContainsKey("ShadowIndex"))
			{
				Array.Resize(ref shadowIndex, shadowIndex.Length + 1);
				Exts.TryParseIntegerInvariant(nodesDict["ShadowIndex"].Value,
					out shadowIndex[shadowIndex.Length - 1]);
			}

			palette = new HardwarePalette();
			foreach (var p in nodesDict["Palettes"].Nodes)
				palette.AddPalette(p.Key, new Palette(GlobalFileSystem.Open(p.Value.Value), shadowIndex), false);

			var spriteLoader = new SpriteLoader(new string[0], new SheetBuilder(SheetType.Indexed));
			foreach (var s in nodesDict["Cursors"].Nodes)
				LoadSequencesForCursor(spriteLoader, s.Key, s.Value);

			palette.Initialize();
		}

		PaletteReference CreatePaletteReference(string name)
		{
			var pal = palette.GetPalette(name);
			if (pal == null)
				throw new InvalidOperationException("Palette `{0}` does not exist".F(name));

			return new PaletteReference(name, palette.GetPaletteIndex(name), pal);
		}

		void LoadSequencesForCursor(SpriteLoader loader, string cursorSrc, MiniYaml cursor)
		{
			foreach (var sequence in cursor.Nodes)
				cursors.Add(sequence.Key, new CursorSequence(loader, cursorSrc, cursor.Value, sequence.Value));
		}

		public bool HasCursorSequence(string cursor)
		{
			return cursors.ContainsKey(cursor);
		}

		public void DrawCursor(Renderer renderer, string cursorName, int2 lastMousePos, int cursorFrame)
		{
			var cursorSequence = GetCursorSequence(cursorName);
			var cursorSprite = cursorSequence.GetSprite(cursorFrame);

			renderer.SetPalette(palette);
			renderer.SpriteRenderer.DrawSprite(cursorSprite,
			                                   lastMousePos - cursorSequence.Hotspot - (0.5f * cursorSprite.size).ToInt2(),
			                                   palettes[cursorSequence.Palette],
			                                   cursorSprite.size);
		}

		public CursorSequence GetCursorSequence(string cursor)
		{
			try { return cursors[cursor]; }
			catch (KeyNotFoundException)
			{
				throw new InvalidOperationException("Cursor does not have a sequence `{0}`".F(cursor));
			}
		}
	}
}
