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

namespace OpenRA.Graphics
{
	class HardwarePalette
	{
		public const int MaxPalettes = 64;
		int allocated = 0;
		
		// We need to store the Palettes themselves for the remap palettes to work
		// We should probably try to fix this somehow
		Dictionary<string, Palette> palettes;
		Dictionary<string, int> indices;

		public HardwarePalette(Map map)
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
			/*for (int i = 0; i < 256; i++)
			{
				this[new Point(i, allocated)] = p.GetColor(i);
			}*/
			return allocated++;
		}
		
		public void UpdatePalette(string name, Palette p)	
		{
			palettes[name] = p;
			var j = indices[name];
			/*
			for (int i = 0; i < 256; i++)
			{
				this[new Point(i, j)] = p.GetColor(i);
			}*/
		}
		
		public void Update(IEnumerable<IPaletteModifier> paletteMods)
		{
			//var b = new Bitmap(Bitmap);
			//foreach (var mod in paletteMods)
			//	mod.AdjustPalette(b);
			
			var data = new uint[256,MaxPalettes];
			foreach (var pal in palettes)
			{
				var j = indices[pal.Key];
				var c = pal.Value.Values;
				for (var i = 0; i < 256; i++)
					data[i,j] = c[i];
			}
	        
			// Doesn't work
			Texture.SetData(data);
			/*
			// Works
			var foo = new Bitmap(256,MaxPalettes);
			for (int j = 0; j < MaxPalettes; j++)
				for (int i = 0; i < 256; i++)
					foo.SetPixel(i,j,Color.FromArgb((int)data[i,j]));
			
			
			Texture.SetData(foo);
			*/
			Game.Renderer.PaletteTexture = Texture;
		}
		
		ITexture texture;
		public ITexture Texture
		{
			get
			{
				if (texture == null)
					texture = Game.Renderer.Device.CreateTexture(new Bitmap(MaxPalettes, 256));

				return texture;
			}
		}
	}
}
