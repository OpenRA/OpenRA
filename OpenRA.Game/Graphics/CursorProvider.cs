#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Graphics
{
	public sealed class CursorProvider
	{
		public readonly IReadOnlyDictionary<string, CursorSequence> Cursors;
		public readonly IReadOnlyDictionary<string, ImmutablePalette> Palettes;

		public CursorProvider(ModData modData)
		{
			var fileSystem = modData.DefaultFileSystem;
			var sequenceYaml = MiniYaml.Merge(modData.Manifest.Cursors.Select(
				s => MiniYaml.FromStream(fileSystem.Open(s), s)));

			var shadowIndex = new int[] { };

			var nodesDict = new MiniYaml(null, sequenceYaml).ToDictionary();
			if (nodesDict.ContainsKey("ShadowIndex"))
			{
				Array.Resize(ref shadowIndex, shadowIndex.Length + 1);
				Exts.TryParseIntegerInvariant(nodesDict["ShadowIndex"].Value,
					out shadowIndex[shadowIndex.Length - 1]);
			}

			var palettes = new Dictionary<string, ImmutablePalette>();
			foreach (var p in nodesDict["Palettes"].Nodes)
				palettes.Add(p.Key, new ImmutablePalette(fileSystem.Open(p.Value.Value), shadowIndex));

			Palettes = palettes.AsReadOnly();

			var frameCache = new FrameCache(fileSystem, modData.SpriteLoaders);
			var cursors = new Dictionary<string, CursorSequence>();
			foreach (var s in nodesDict["Cursors"].Nodes)
				foreach (var sequence in s.Value.Nodes)
					cursors.Add(sequence.Key, new CursorSequence(frameCache, sequence.Key, s.Key, s.Value.Value, sequence.Value));

			Cursors = cursors.AsReadOnly();
		}

		public static bool CursorViewportZoomed { get { return Game.Settings.Graphics.CursorDouble && Game.Settings.Graphics.PixelDouble; } }

		public bool HasCursorSequence(string cursor)
		{
			return Cursors.ContainsKey(cursor);
		}

		public CursorSequence GetCursorSequence(string cursor)
		{
			try { return Cursors[cursor]; }
			catch (KeyNotFoundException)
			{
				throw new InvalidOperationException("Cursor does not have a sequence `{0}`".F(cursor));
			}
		}
	}
}
