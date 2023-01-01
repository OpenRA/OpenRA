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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Define a player palette by swapping palette indices.")]
	public class IndexedPlayerPaletteInfo : TraitInfo, IRulesetLoaded
	{
		[PaletteReference]
		[Desc("The name of the palette to base off.")]
		public readonly string BasePalette = null;

		[PaletteDefinition(true)]
		[Desc("The prefix for the resulting player palettes")]
		public readonly string BaseName = "player";

		[Desc("Remap these indices to player colors.")]
		public readonly int[] RemapIndex = Array.Empty<int>();

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		public readonly Dictionary<string, int[]> PlayerIndex;

		public override object Create(ActorInitializer init) { return new IndexedPlayerPalette(this); }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			foreach (var p in PlayerIndex)
				if (p.Value.Length != (RemapIndex.Length == 0 ? 256 : RemapIndex.Length))
					throw new YamlException($"PlayerIndex for player `{p.Key}` length does not match RemapIndex!");
		}
	}

	public class IndexedPlayerPalette : ILoadsPlayerPalettes
	{
		readonly IndexedPlayerPaletteInfo info;

		public IndexedPlayerPalette(IndexedPlayerPaletteInfo info)
		{
			this.info = info;
		}

		public void LoadPlayerPalettes(WorldRenderer wr, string playerName, Color color, bool replaceExisting)
		{
			var basePalette = wr.Palette(info.BasePalette).Palette;
			ImmutablePalette pal;

			if (info.PlayerIndex.TryGetValue(playerName, out var remap))
				pal = new ImmutablePalette(basePalette, new IndexedColorRemap(basePalette, info.RemapIndex.Length == 0 ? Enumerable.Range(0, 256).ToArray() : info.RemapIndex, remap));
			else
				pal = new ImmutablePalette(basePalette);

			wr.AddPalette(info.BaseName + playerName, pal, info.AllowModifiers, replaceExisting);
		}
	}
}
