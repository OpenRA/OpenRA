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

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public class PaletteFromEmbeddedSpritePaletteInfo : TraitInfo, IProvidesCursorPaletteInfo
	{
		[PaletteDefinition]
		[FieldLoader.Require]
		[Desc("Internal palette name")]
		public readonly string Name = null;

		[FieldLoader.Require]
		[Desc("Sequence image holding the palette definition")]
		public readonly string Image = null;

		[FieldLoader.Require]
		[SequenceReference(nameof(Image))]
		[Desc("Sequence holding the palette definition")]
		public readonly string Sequence = null;

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		[Desc("Whether this palette is available for cursors.")]
		public readonly bool CursorPalette = false;

		public override object Create(ActorInitializer init) { return new PaletteFromEmbeddedSpritePalette(this); }

		string IProvidesCursorPaletteInfo.Palette => CursorPalette ? Name : null;

		ImmutablePalette IProvidesCursorPaletteInfo.ReadPalette(IReadOnlyFileSystem fileSystem)
		{
			var sequence = (DefaultSpriteSequence)Game.ModData.DefaultSequences.Values.First().GetSequence(Image, Sequence);
			return new ImmutablePalette(sequence.EmbeddedPalette);
		}
	}

	public class PaletteFromEmbeddedSpritePalette : ILoadsPalettes, IProvidesAssetBrowserPalettes
	{
		readonly PaletteFromEmbeddedSpritePaletteInfo info;
		public PaletteFromEmbeddedSpritePalette(PaletteFromEmbeddedSpritePaletteInfo info) { this.info = info; }

		public void LoadPalettes(WorldRenderer wr)
		{
			var sequence = (DefaultSpriteSequence)wr.World.Map.Rules.Sequences.GetSequence(info.Image, info.Sequence);
			wr.AddPalette(info.Name, new ImmutablePalette(sequence.EmbeddedPalette), info.AllowModifiers);
		}

		public IEnumerable<string> PaletteNames { get { yield return info.Name; } }
	}
}
