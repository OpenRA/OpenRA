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
using System.IO;
using OpenRA.Primitives;

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

		public static IPalette AsReadOnly(this IPalette palette)
		{
			if (palette is ImmutablePalette)
				return palette;
			return new ReadOnlyPalette(palette);
		}

		class ReadOnlyPalette : IPalette
		{
			readonly IPalette palette;
			public ReadOnlyPalette(IPalette palette) { this.palette = palette; }
			public uint this[int index] => palette[index];

			public void CopyToArray(Array destination, int destinationOffset)
			{
				palette.CopyToArray(destination, destinationOffset);
			}
		}
	}

	public class ImmutablePalette : IPalette
	{
		readonly uint[] colors = new uint[Palette.Size];

		public uint this[int index] => colors[index];

		public void CopyToArray(Array destination, int destinationOffset)
		{
			Buffer.BlockCopy(colors, 0, destination, destinationOffset * 4, Palette.Size * 4);
		}

		public ImmutablePalette(string filename, int[] remapTransparent, int[] remap)
		{
			using (var s = File.OpenRead(filename))
				LoadFromStream(s, remapTransparent, remap);
		}

		public ImmutablePalette(Stream s, int[] remapTransparent, int[] remapShadow)
		{
			LoadFromStream(s, remapTransparent, remapShadow);
		}

		void LoadFromStream(Stream s, int[] remapTransparent, int[] remapShadow)
		{
			using (var reader = new BinaryReader(s))
				for (var i = 0; i < Palette.Size; i++)
				{
					var r = (byte)(reader.ReadByte() << 2);
					var g = (byte)(reader.ReadByte() << 2);
					var b = (byte)(reader.ReadByte() << 2);

					// Replicate high bits into the (currently zero) low bits.
					r |= (byte)(r >> 6);
					g |= (byte)(g >> 6);
					b |= (byte)(b >> 6);

					colors[i] = (uint)((255 << 24) | (r << 16) | (g << 8) | b);
				}

			foreach (var i in remapTransparent)
				colors[i] = 0;

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
			for (var i = 0; i < Palette.Size; i++)
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
			get => colors[index];
			set => colors[index] = value;
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
