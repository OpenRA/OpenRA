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

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	[Desc("Define a player palette by swapping palette indices.")]
	public class IndexedPlayerPaletteInfo : ITraitInfo, IRulesetLoaded
	{
		[Desc("The name of the palette to base off.")]
		[PaletteReference] public readonly string BasePalette = null;

		[Desc("The prefix for the resulting player palettes")]
		[PaletteDefinition(true)] public readonly string BaseName = "player";

		[Desc("Remap these indices to player colors.")]
		public readonly int[] RemapIndex = { };

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		public readonly Dictionary<string, int[]> PlayerIndex;

		public object Create(ActorInitializer init) { return new IndexedPlayerPalette(this); }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			foreach (var p in PlayerIndex)
				if (p.Value.Length != RemapIndex.Length)
					throw new YamlException("PlayerIndex for player `{0}` length does not match RemapIndex!".F(p.Key));
		}
	}

	public class IndexedPlayerPalette : ILoadsPlayerPalettes
	{
		readonly IndexedPlayerPaletteInfo info;

		public IndexedPlayerPalette(IndexedPlayerPaletteInfo info)
		{
			this.info = info;
		}

		public void LoadPlayerPalettes(WorldRenderer wr, string playerName, HSLColor color, bool replaceExisting)
		{
			var basePalette = wr.Palette(info.BasePalette).Palette;
			ImmutablePalette pal;
			int[] remap;

			if (info.PlayerIndex.TryGetValue(playerName, out remap))
				pal = new ImmutablePalette(basePalette, new IndexedColorRemap(basePalette, info.RemapIndex, remap));
			else
				pal = new ImmutablePalette(basePalette);

			wr.AddPalette(info.BaseName + playerName, pal, info.AllowModifiers, replaceExisting);
		}
	}

	public class IndexedColorRemap : IPaletteRemap
	{
		Dictionary<int, int> replacements = new Dictionary<int, int>();
		IPalette basePalette;

		public IndexedColorRemap(IPalette basePalette, int[] ramp, int[] remap)
		{
			this.basePalette = basePalette;
			if (ramp.Length != remap.Length)
				throw new InvalidDataException("ramp and remap lengths do no match.");

			for (var i = 0; i < ramp.Length; i++)
				replacements[ramp[i]] = remap[i];
		}

		public Color GetRemappedColor(Color original, int index)
		{
			int c;
			return replacements.TryGetValue(index, out c)
				? basePalette.GetColor(c) : original;
		}
	}
}
