#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace OpenRA.Graphics
{
	public class Palette
	{
		public const int Size = 256;
		public static Palette Load(string filename, int[] remap)
		{
			using (var s = File.OpenRead(filename))
				return new Palette(s, remap);
		}

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

		public void ApplyRemap(IPaletteRemap r)
		{
			for (int i = 0; i < Size; i++)
				colors[i] = (uint)r.GetRemappedColor(Color.FromArgb((int)colors[i]), i).ToArgb();
		}

		public Palette(Stream s, int[] remapShadow)
		{
			colors = new uint[Size];

			using (BinaryReader reader = new BinaryReader(s))
			{
				for (int i = 0; i < Size; i++)
				{
					byte r = (byte)(reader.ReadByte() << 2);
					byte g = (byte)(reader.ReadByte() << 2);
					byte b = (byte)(reader.ReadByte() << 2);
					colors[i] = (uint)((255 << 24) | (r << 16) | (g << 8) | b);
				}
			}

			colors[0] = 0; // convert black background to transparency
			foreach (int i in remapShadow)
				colors[i] = 140u << 24;
		}

		public Palette(Palette p, IPaletteRemap r)
		{
			colors = (uint[])p.colors.Clone();
			ApplyRemap(r);
		}

		public Palette(Palette p)
		{
			colors = (uint[])p.colors.Clone();
		}

		public Palette(uint[] data)
		{
			if (data.Length != Size)
				throw new InvalidDataException("Attempting to create palette with incorrect array size");
			colors = (uint[])data.Clone();
		}

		public ColorPalette AsSystemPalette()
		{
			ColorPalette pal;
			using (var b = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
				pal = b.Palette;

			for (var i = 0; i < Size; i++)
				pal.Entries[i] = GetColor(i);

			// hack around a mono bug -- the palette flags get set wrong.
			if (Platform.CurrentPlatform != PlatformType.Windows)
				typeof(ColorPalette).GetField("flags",
					BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pal, 1);

			return pal;
		}

		public Bitmap AsBitmap()
		{
			var b = new Bitmap(Size, 1, PixelFormat.Format32bppArgb);
			var data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
								  ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			unsafe
			{
				uint* c = (uint*)data.Scan0;
				for (var x = 0; x < Size; x++)
					*(c + x) = colors[x];
			}

			b.UnlockBits(data);
			return b;
		}
	}

	public interface IPaletteRemap { Color GetRemappedColor(Color original, int index); }
}
