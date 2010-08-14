#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.FileFormats.Graphics;
using System.Linq;

namespace OpenRA.Graphics
{
	class HardwarePalette
	{
		public const int MaxPalettes = 64;
		int allocated = 0;
		
		ITexture texture;
		Dictionary<string, Palette> palettes;
		Dictionary<string, int> indices;
		
		public HardwarePalette(Map map)
		{
			palettes = new Dictionary<string, Palette>();
			indices = new Dictionary<string, int>();
			texture = Game.Renderer.Device.CreateTexture();
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
		
		public void AddPalette(string name, Palette p)
		{
			Console.WriteLine("Adding palette "+name);
			palettes.Add(name, p);
			indices.Add(name, allocated++);
		}
		
		public void UpdatePalette(string name, Palette p)	
		{
			palettes[name] = p;
		}
		
		public void Update(IEnumerable<IPaletteModifier> paletteMods)
		{
			var copy = palettes.ToDictionary(p => p.Key, p => new Palette(p.Value));
			
			foreach (var mod in paletteMods)
				mod.AdjustPalette(copy);
			
			var data = new uint[MaxPalettes,256];
			foreach (var pal in copy)
			{
				var j = indices[pal.Key];
				var c = pal.Value.Values;
				for (var i = 0; i < 256; i++)
					data[j,i] = c[i];
			}
	        
			// Doesn't work
			texture.SetData(data);
			Game.Renderer.PaletteTexture = texture;
		}
	}
}
