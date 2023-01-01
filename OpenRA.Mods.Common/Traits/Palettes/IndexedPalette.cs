#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Define a palette by swapping palette indices.")]
	public class IndexedPaletteInfo : TraitInfo, IRulesetLoaded
	{
		[PaletteDefinition]
		[FieldLoader.Require]
		[Desc("Internal palette name")]
		public readonly string Name = null;

		[PaletteReference]
		[Desc("The name of the palette to base off.")]
		public readonly string BasePalette = null;

		[FieldLoader.Require]
		[Desc("Indices from BasePalette to be swapped with ReplaceIndex.")]
		public readonly int[] Index = Array.Empty<int>();

		[FieldLoader.Require]
		[Desc("Indices from BasePalette to replace from Index.")]
		public readonly int[] ReplaceIndex = Array.Empty<int>();

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		public override object Create(ActorInitializer init) { return new IndexedPalette(this); }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (Index.Length != ReplaceIndex.Length)
				throw new YamlException($"ReplaceIndex length does not match Index length for palette {Name}");
		}
	}

	public class IndexedPalette : ILoadsPalettes
	{
		readonly IndexedPaletteInfo info;

		public IndexedPalette(IndexedPaletteInfo info)
		{
			this.info = info;
		}

		public void LoadPalettes(WorldRenderer wr)
		{
			var basePalette = wr.Palette(info.BasePalette).Palette;
			var pal = new ImmutablePalette(basePalette, new IndexedColorRemap(basePalette, info.Index, info.ReplaceIndex));
			wr.AddPalette(info.Name, pal, info.AllowModifiers);
		}
	}

	public class IndexedColorRemap : IPaletteRemap
	{
		readonly Dictionary<int, int> replacements = new Dictionary<int, int>();
		readonly IPalette basePalette;

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
			return replacements.TryGetValue(index, out var c)
				? basePalette.GetColor(c) : original;
		}
	}
}
