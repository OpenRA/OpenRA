#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRa.FileFormats;
using OpenRa.Traits;

namespace OpenRa.Graphics
{
	class HardwarePalette : Sheet
	{
		const int maxEntries = 16;
		int allocated = 0;
		
		// We need to store the Palettes themselves for the remap palettes to work
		// We should probably try to fix this somehow
		static Dictionary<string, Palette> palettes;
		static Dictionary<string, int> indices;
		public HardwarePalette(Renderer renderer, Map map)
			: base(renderer,new Size(256, maxEntries))
		{
			palettes = new Dictionary<string, Palette>();
			indices = new Dictionary<string, int>();
		}
		
		public Palette GetPalette(string name)
		{
			try { return palettes[name]; }
			catch (KeyNotFoundException)
			{
				throw new InvalidOperationException(
					"Palette `{0}` does not exist".F(name));
			}
		}

		public int GetPaletteIndex(string name)
		{
			try { return indices[name]; }
			catch (KeyNotFoundException)
			{
				throw new InvalidOperationException(
					"Palette `{0}` does not exist".F(name));
			}
		}
		
		public int AddPalette(string name, Palette p)
		{
			palettes.Add(name, p);
			indices.Add(name, allocated);
			for (int i = 0; i < 256; i++)
			{
				this[new Point(i, allocated)] = p.GetColor(i);
			}
			return allocated++;
		}

		public void Update(IEnumerable<IPaletteModifier> paletteMods)
		{
			var b = new Bitmap(Bitmap);
			foreach (var mod in paletteMods)
				mod.AdjustPalette(b);

			Texture.SetData(b);
			Game.renderer.PaletteTexture = Texture;
		}
	}
}
