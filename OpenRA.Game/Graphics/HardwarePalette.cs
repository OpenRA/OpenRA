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
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public sealed class HardwarePalette : IDisposable
	{
		public ITexture Texture { get; private set; }
		public int Height { get; private set; }
		readonly Dictionary<string, ImmutablePalette> palettes = new Dictionary<string, ImmutablePalette>();
		readonly Dictionary<string, MutablePalette> modifiablePalettes = new Dictionary<string, MutablePalette>();
		readonly IReadOnlyDictionary<string, MutablePalette> readOnlyModifiablePalettes;
		readonly Dictionary<string, int> indices = new Dictionary<string, int>();
		byte[] buffer = new byte[0];

		public HardwarePalette()
		{
			Texture = Game.Renderer.Device.CreateTexture();
			readOnlyModifiablePalettes = modifiablePalettes.AsReadOnly();
		}

		public bool Contains(string name)
		{
			return modifiablePalettes.ContainsKey(name) || palettes.ContainsKey(name);
		}

		public IPalette GetPalette(string name)
		{
			MutablePalette mutable;
			if (modifiablePalettes.TryGetValue(name, out mutable))
				return mutable.AsReadOnly();
			ImmutablePalette immutable;
			if (palettes.TryGetValue(name, out immutable))
				return immutable;
			throw new InvalidOperationException("Palette `{0}` does not exist".F(name));
		}

		public int GetPaletteIndex(string name)
		{
			int ret;
			if (!indices.TryGetValue(name, out ret))
				throw new InvalidOperationException("Palette `{0}` does not exist".F(name));
			return ret;
		}

		public void AddPalette(string name, ImmutablePalette p, bool allowModifiers)
		{
			if (palettes.ContainsKey(name))
				throw new InvalidOperationException("Palette {0} has already been defined".F(name));

			int index = palettes.Count;
			indices.Add(name, index);
			palettes.Add(name, p);

			if (palettes.Count > Height)
			{
				Height = Exts.NextPowerOf2(palettes.Count);
				Array.Resize(ref buffer, Height * Palette.Size * 4);
			}

			if (allowModifiers)
				modifiablePalettes.Add(name, new MutablePalette(p));
			else
				CopyPaletteToBuffer(index, p);
		}

		public void ReplacePalette(string name, IPalette p)
		{
			if (modifiablePalettes.ContainsKey(name))
				CopyPaletteToBuffer(indices[name], modifiablePalettes[name] = new MutablePalette(p));
			else if (palettes.ContainsKey(name))
				CopyPaletteToBuffer(indices[name], palettes[name] = new ImmutablePalette(p));
			else
				throw new InvalidOperationException("Palette `{0}` does not exist".F(name));
			CopyBufferToTexture();
		}

		public void Initialize()
		{
			CopyModifiablePalettesToBuffer();
			CopyBufferToTexture();
		}

		void CopyPaletteToBuffer(int index, IPalette p)
		{
			p.CopyToArray(buffer, index * Palette.Size);
		}

		void CopyModifiablePalettesToBuffer()
		{
			foreach (var kvp in modifiablePalettes)
				CopyPaletteToBuffer(indices[kvp.Key], kvp.Value);
		}

		void CopyBufferToTexture()
		{
			Texture.SetData(buffer, Palette.Size, Height);
		}

		public void ApplyModifiers(IEnumerable<IPaletteModifier> paletteMods)
		{
			foreach (var mod in paletteMods)
				mod.AdjustPalette(readOnlyModifiablePalettes);

			// Update our texture with the changes.
			CopyModifiablePalettesToBuffer();
			CopyBufferToTexture();

			// Reset modified palettes back to their original colors, ready for next time.
			foreach (var kvp in modifiablePalettes)
			{
				var originalPalette = palettes[kvp.Key];
				var modifiedPalette = kvp.Value;
				modifiedPalette.SetFromPalette(originalPalette);
			}
		}

		public void Dispose()
		{
			Texture.Dispose();
		}
	}
}
