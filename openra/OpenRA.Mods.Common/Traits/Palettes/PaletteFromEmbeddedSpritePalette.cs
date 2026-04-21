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
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.SpriteLoaders;
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
		[Desc("Filename of sprite that contains the palette definition.")]
		public readonly string Filename = null;

		[Desc("Image frame associated with the palette definition, if relevant.")]
		public readonly int Frame = 0;

		[Desc("Allow palette modifiers to change the palette.")]
		public readonly bool AllowModifiers = true;

		[Desc("Whether this palette is available for cursors.")]
		public readonly bool CursorPalette = false;

		public override object Create(ActorInitializer init) { return new PaletteFromEmbeddedSpritePalette(this); }

		string IProvidesCursorPaletteInfo.Palette => CursorPalette ? Name : null;

		ImmutablePalette IProvidesCursorPaletteInfo.ReadPalette(IReadOnlyFileSystem fileSystem)
		{
			FrameLoader.GetFrames(fileSystem, Filename, Game.ModData.SpriteLoaders, out var metadata);
			var palettes = metadata?.GetOrDefault<EmbeddedSpritePalette>();
			if (palettes == null || !palettes.TryGetPaletteForFrame(Frame, out var embeddedPalette))
				throw new YamlException($"Cannot export palette from {Filename}: frame {Frame} does not define an embedded palette");

			return new ImmutablePalette(embeddedPalette);
		}
	}

	public class PaletteFromEmbeddedSpritePalette : ILoadsPalettes, IProvidesAssetBrowserPalettes
	{
		readonly PaletteFromEmbeddedSpritePaletteInfo info;
		public PaletteFromEmbeddedSpritePalette(PaletteFromEmbeddedSpritePaletteInfo info) { this.info = info; }

		public void LoadPalettes(WorldRenderer wr)
		{
			FrameLoader.GetFrames(wr.World.Map, info.Filename, Game.ModData.SpriteLoaders, out var metadata);
			var palettes = metadata?.GetOrDefault<EmbeddedSpritePalette>();
			if (palettes == null || !palettes.TryGetPaletteForFrame(info.Frame, out var embeddedPalette))
				throw new YamlException($"Cannot export palette from {info.Filename}: frame {info.Frame} does not define an embedded palette");

			wr.AddPalette(info.Name, new ImmutablePalette(embeddedPalette), info.AllowModifiers);
		}

		public IEnumerable<string> PaletteNames { get { yield return info.Name; } }
	}
}
