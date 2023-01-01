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
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public sealed class HardwarePalette : IDisposable
	{
		public ITexture Texture { get; }
		public ITexture ColorShifts { get; }

		public int Height { get; private set; }
		readonly Dictionary<string, ImmutablePalette> palettes = new Dictionary<string, ImmutablePalette>();
		readonly Dictionary<string, MutablePalette> mutablePalettes = new Dictionary<string, MutablePalette>();
		readonly Dictionary<string, int> indices = new Dictionary<string, int>();
		byte[] buffer = Array.Empty<byte>();
		float[] colorShiftBuffer = Array.Empty<float>();

		public HardwarePalette()
		{
			Texture = Game.Renderer.Context.CreateTexture();
			ColorShifts = Game.Renderer.Context.CreateTexture();
		}

		public bool Contains(string name)
		{
			return mutablePalettes.ContainsKey(name) || palettes.ContainsKey(name);
		}

		public IPalette GetPalette(string name)
		{
			if (mutablePalettes.TryGetValue(name, out var mutable))
				return mutable.AsReadOnly();
			if (palettes.TryGetValue(name, out var immutable))
				return immutable;
			throw new InvalidOperationException($"Palette `{name}` does not exist");
		}

		public int GetPaletteIndex(string name)
		{
			if (!indices.TryGetValue(name, out var ret))
				throw new InvalidOperationException($"Palette `{name}` does not exist");
			return ret;
		}

		public void AddPalette(string name, ImmutablePalette p, bool allowModifiers)
		{
			if (palettes.ContainsKey(name))
				throw new InvalidOperationException($"Palette {name} has already been defined");

			// PERF: the first row in the palette textures is reserved as a placeholder for non-indexed sprites
			// that do not have a color-shift applied. This provides a quick shortcut to avoid querying the
			// color-shift texture for every pixel only to find that most are not shifted.
			var index = palettes.Count + 1;
			indices.Add(name, index);
			palettes.Add(name, p);

			if (index >= Height)
			{
				Height = Exts.NextPowerOf2(index + 1);
				Array.Resize(ref buffer, Height * Palette.Size * 4);
				Array.Resize(ref colorShiftBuffer, Height * 4);
			}

			if (allowModifiers)
				mutablePalettes.Add(name, new MutablePalette(p));
			else
				CopyPaletteToBuffer(index, p);
		}

		public void ReplacePalette(string name, IPalette p)
		{
			if (mutablePalettes.ContainsKey(name))
				CopyPaletteToBuffer(indices[name], mutablePalettes[name] = new MutablePalette(p));
			else if (palettes.ContainsKey(name))
				CopyPaletteToBuffer(indices[name], palettes[name] = new ImmutablePalette(p));
			else
				throw new InvalidOperationException($"Palette `{name}` does not exist");
			CopyBufferToTexture();
		}

		public void SetColorShift(string name, float hueOffset, float satOffset, float minHue, float maxHue)
		{
			var index = GetPaletteIndex(name);
			colorShiftBuffer[4 * index + 0] = hueOffset;
			colorShiftBuffer[4 * index + 1] = satOffset;
			colorShiftBuffer[4 * index + 2] = minHue;
			colorShiftBuffer[4 * index + 3] = maxHue;
		}

		public bool HasColorShift(string name)
		{
			var index = GetPaletteIndex(name);
			return colorShiftBuffer[4 * index + 2] != 0 || colorShiftBuffer[4 * index + 3] != 0;
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
			foreach (var kvp in mutablePalettes)
				CopyPaletteToBuffer(indices[kvp.Key], kvp.Value);
		}

		void CopyBufferToTexture()
		{
			Texture.SetData(buffer, Palette.Size, Height);
			ColorShifts.SetFloatData(colorShiftBuffer, 1, Height);
		}

		public void ApplyModifiers(IEnumerable<IPaletteModifier> paletteMods)
		{
			foreach (var mod in paletteMods)
				mod.AdjustPalette(mutablePalettes);

			// Update our texture with the changes.
			CopyModifiablePalettesToBuffer();
			CopyBufferToTexture();

			// Reset modified palettes back to their original colors, ready for next time.
			foreach (var kvp in mutablePalettes)
			{
				var originalPalette = palettes[kvp.Key];
				var modifiedPalette = kvp.Value;
				modifiedPalette.SetFromPalette(originalPalette);
			}
		}

		public void Dispose()
		{
			Texture.Dispose();
			ColorShifts.Dispose();
		}
	}
}
