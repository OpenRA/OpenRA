#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System;
using System.Reflection;

namespace OpenRA.FileFormats
{
	public class Palette
	{
		uint[] colors;
		public Color GetColor(int index)
		{
			return Color.FromArgb((int)colors[index]);
		}

		public void SetColor(int index, Color color)
		{
			colors[index] = (uint)color.ToArgb();
		}

		public void SetColor(int index, uint color)
		{
			colors[index] = (uint)color;
		}

		public uint[] Values
		{
			get { return colors; }
		}

		public Palette(Stream s, bool remapTransparent)
		{
			colors = new uint[256];

			using (BinaryReader reader = new BinaryReader(s))
			{
				for (int i = 0; i < 256; i++)
				{
					byte r = (byte)(reader.ReadByte() << 2);
					byte g = (byte)(reader.ReadByte() << 2);
					byte b = (byte)(reader.ReadByte() << 2);
					colors[i] = (uint)((255 << 24) | (r << 16) | (g << 8) | b);
				}
			}

			colors[0] = 0;
			if (remapTransparent)
			{
				colors[1] = 178u << 24; // Hack for d2k; may have side effects
				colors[3] = 178u << 24;
				colors[4] = 140u << 24;
			}
		}

		public Palette(Palette p, IPaletteRemap r)
		{
			colors = new uint[256];
			for(int i = 0; i < 256; i++)
				colors[i] = (uint)r.GetRemappedColor(Color.FromArgb((int)p.colors[i]),i).ToArgb();
		}

		public Palette(Palette p)
		{
			colors = (uint[])p.colors.Clone();
		}

		public ColorPalette AsSystemPalette()
		{
			ColorPalette pal;
			using (var b = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
				pal = b.Palette;

			for (var i = 0; i < 256; i++)
				pal.Entries[i] = GetColor(i);

			// hack around a mono bug -- the palette flags get set wrong.
			if (Platform.CurrentPlatform != PlatformType.Windows)
				typeof(ColorPalette).GetField("flags",
					BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pal, 1);

			return pal;
		}

		public static Palette Load( string filename, bool remap )
		{
			using(var s = File.OpenRead(filename))
				return new Palette(s, remap);
		}
	}

	public interface IPaletteRemap { Color GetRemappedColor(Color original, int index);	}
}
