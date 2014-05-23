#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public class HardwarePalette
	{
		public const int MaxPalettes = 256;

		public ITexture Texture { get; private set; }
		readonly Dictionary<string, Palette> palettes = new Dictionary<string, Palette>();
		readonly Dictionary<string, Palette> modifiablePalettes = new Dictionary<string, Palette>();
		readonly Dictionary<string, int> indices = new Dictionary<string, int>();
		readonly uint[,] data = new uint[MaxPalettes, Palette.Size];
		bool palettesModified;

		public HardwarePalette()
		{
			Texture = Game.Renderer.Device.CreateTexture();
		}

		public Palette GetPalette(string name)
		{
			Palette ret;
			if (!palettes.TryGetValue(name,out ret))
				throw new InvalidOperationException("Palette `{0}` does not exist".F(name));
			return ret;
		}

		public int GetPaletteIndex(string name)
		{
			int ret;
			if (!indices.TryGetValue(name,out ret))
				throw new InvalidOperationException("Palette `{0}` does not exist".F(name));
			return ret;
		}

		public void AddPalette(string name, Palette p, bool allowModifiers)
		{
			if (palettes.Count >= MaxPalettes)
				throw new InvalidOperationException("Limit of {0} palettes reached. Cannot add {1}.".F(MaxPalettes, name));
			if (palettes.ContainsKey(name))
				throw new InvalidOperationException("Palette {0} has already been defined".F(name));

			int index = palettes.Count;
			indices.Add(name, index);
			palettes.Add(name, p);
			if (allowModifiers)
				modifiablePalettes.Add(name, new Palette(p));
			else
			{
				// Copy fixed palette to buffer.
				var values = p.Values;
				for (var i = 0; i < Palette.Size; i++)
					data[index, i] = values[i];
			}
		}

		public void Initialize()
		{
			ApplyModifiablePalettesToTexture();
		}

		void ApplyModifiablePalettesToTexture()
		{
			foreach (var kvp in modifiablePalettes)
			{
				var index = indices[kvp.Key];
				var values = kvp.Value.Values;
				for (var i = 0; i < Palette.Size; i++)
					data[index, i] = values[i];
			}
			Texture.SetData(data);
		}

		public void ApplyModifiers(IEnumerable<IPaletteModifier> paletteMods)
		{
			bool wasModified = palettesModified;

			// Reset modifiable palettes to match the original colors.
			if (palettesModified)
			{
				foreach (var name in indices.Keys)
				{
					Palette modifiablePalette;
					if (!modifiablePalettes.TryGetValue(name, out modifiablePalette))
						continue;
					var modifiableValues = modifiablePalette.Values;
					var values = palettes[name].Values;
					for (var i = 0; i < Palette.Size; i++)
						modifiableValues[i] = values[i];
				}
				palettesModified = false;
			}

			foreach (var mod in paletteMods)
			{
				mod.AdjustPalette(modifiablePalettes);
				palettesModified = true;
			}

			if (wasModified || palettesModified)
				ApplyModifiablePalettesToTexture();
		}
	}
}
