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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace OpenRA.Graphics
{
	public interface IPalette
	{
		uint this[int index] { get; }
		void CopyToArray(Array destination, int destinationOffset);
	}

	public interface IPaletteRemap { Color GetRemappedColor(Color original, int index); }

	public static class Palette
	{
		public const int Size = 256;

		public static Color GetColor(this IPalette palette, int index)
		{
			return Color.FromArgb((int)palette[index]);
		}

		public static ColorPalette AsSystemPalette(this IPalette palette)
		{
			ColorPalette pal;
			using (var b = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
				pal = b.Palette;

			for (var i = 0; i < Size; i++)
				pal.Entries[i] = palette.GetColor(i);

			// hack around a mono bug -- the palette flags get set wrong.
			if (Platform.CurrentPlatform != PlatformType.Windows)
				typeof(ColorPalette).GetField("flags",
					BindingFlags.Instance | BindingFlags.NonPublic).SetValue(pal, 1);

			return pal;
		}

		public static Bitmap AsBitmap(this IPalette palette)
		{
			var b = new Bitmap(Size, 1, PixelFormat.Format32bppArgb);
			var data = b.LockBits(new Rectangle(0, 0, b.Width, b.Height),
								  ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			var temp = new uint[Size];
			palette.CopyToArray(temp, 0);
			Marshal.Copy((int[])(object)temp, 0, data.Scan0, Size);
			b.UnlockBits(data);
			return b;
		}

		public static IPalette AsReadOnly(this IPalette palette)
		{
			if (palette is ImmutablePalette)
				return palette;
			return new ReadOnlyPalette(palette);
		}

		class ReadOnlyPalette : IPalette
		{
			IPalette palette;
			public ReadOnlyPalette(IPalette palette) { this.palette = palette; }
			public uint this[int index] { get { return palette[index]; } }
			public void CopyToArray(Array destination, int destinationOffset)
			{
				palette.CopyToArray(destination, destinationOffset);
			}
		}
	}

	public class ImmutablePalette : IPalette
	{
		readonly uint[] colors = new uint[Palette.Size];

		public uint this[int index]
		{
			get { return colors[index]; }
		}

		public void CopyToArray(Array destination, int destinationOffset)
		{
			Buffer.BlockCopy(colors, 0, destination, destinationOffset * 4, Palette.Size * 4);
		}

		public ImmutablePalette(string filename, int[] remap)
		{
			using (var s = File.OpenRead(filename))
				LoadFromStream(s, remap);
		}

		public ImmutablePalette(Stream s, int[] remapShadow)
		{
			LoadFromStream(s, remapShadow);
		}

		void LoadFromStream(Stream s, int[] remapShadow)
		{
			using (var reader = new BinaryReader(s))
				for (var i = 0; i < Palette.Size; i++)
				{
					var r = (byte)(reader.ReadByte() << 2);
					var g = (byte)(reader.ReadByte() << 2);
					var b = (byte)(reader.ReadByte() << 2);
					colors[i] = (uint)((255 << 24) | (r << 16) | (g << 8) | b);
				}

			colors[0] = 0; // Convert black background to transparency.
			foreach (var i in remapShadow)
				colors[i] = 140u << 24;
		}

		public ImmutablePalette(IPalette p, IPaletteRemap r)
			: this(p)
		{
			for (var i = 0; i < Palette.Size; i++)
				colors[i] = (uint)r.GetRemappedColor(this.GetColor(i), i).ToArgb();
		}

		public ImmutablePalette(IPalette p)
		{
			for (int i = 0; i < Palette.Size; i++)
				colors[i] = p[i];
		}

		public ImmutablePalette(IEnumerable<uint> sourceColors)
		{
			var i = 0;
			foreach (var sourceColor in sourceColors)
				colors[i++] = sourceColor;
		}
	}

	public class MutablePalette : IPalette
	{
		readonly uint[] colors = new uint[Palette.Size];

		public uint this[int index]
		{
			get { return colors[index]; }
			set { colors[index] = value; }
		}

		public void CopyToArray(Array destination, int destinationOffset)
		{
			Buffer.BlockCopy(colors, 0, destination, destinationOffset * 4, Palette.Size * 4);
		}

		public MutablePalette(IPalette p)
		{
			SetFromPalette(p);
		}

		public void SetColor(int index, Color color)
		{
			colors[index] = (uint)color.ToArgb();
		}

		public void SetFromPalette(IPalette p)
		{
			p.CopyToArray(colors, 0);
		}

		public void ApplyRemap(IPaletteRemap r)
		{
			for (var i = 0; i < Palette.Size; i++)
				colors[i] = (uint)r.GetRemappedColor(this.GetColor(i), i).ToArgb();
		}
	}
}
