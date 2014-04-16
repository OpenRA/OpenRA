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
		int allocated = 0;

		public ITexture Texture { get; private set; }
		Dictionary<string, Palette> palettes;
		Dictionary<string, int> indices;
		Dictionary<string, bool> allowsMods;

		public HardwarePalette()
		{
			palettes = new Dictionary<string, Palette>();
			indices = new Dictionary<string, int>();
			allowsMods = new Dictionary<string, bool>();
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
			if (palettes.ContainsKey(name))
				throw new InvalidOperationException("Palette {0} has already been defined".F(name));

			palettes.Add(name, p);
			indices.Add(name, allocated++);
			allowsMods.Add(name, allowModifiers);
		}

		uint[,] data = new uint[MaxPalettes, 256];
		public void ApplyModifiers(IEnumerable<IPaletteModifier> paletteMods)
		{
			var copy = palettes.ToDictionary(p => p.Key, p => new Palette(p.Value));
			var modifiable = copy.Where(p => allowsMods[p.Key]).ToDictionary(p => p.Key, p => p.Value);

			foreach (var mod in paletteMods)
				mod.AdjustPalette(modifiable);

			foreach (var pal in copy)
			{
				var j = indices[pal.Key];
				var c = pal.Value.Values;
				for (var i = 0; i < 256; i++)
					data[j,i] = c[i];
			}

			Texture.SetData(data);
		}

		public void Initialize()
		{
			ApplyModifiers(new IPaletteModifier[] {});
		}
	}
}
