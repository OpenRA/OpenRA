#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats.Graphics;

namespace OpenRA.Graphics
{
	public static class Util
	{
		static float[] channelSelect = { 0.75f, 0.25f, -0.25f, -0.75f };

		public static void FastCreateQuad(Vertex[] vertices, float2 o, Sprite r, int palette, int nv, float2 size)
		{
			var attrib = new float2(palette / (float)HardwarePalette.MaxPalettes, channelSelect[(int)r.channel]);

			vertices[nv] = new Vertex(o,
				r.FastMapTextureCoords(0), attrib);
			vertices[nv + 1] = new Vertex(new float2(o.X + size.X, o.Y),
				r.FastMapTextureCoords(1), attrib);
			vertices[nv + 2] = new Vertex(new float2(o.X + size.X, o.Y + size.Y),
				r.FastMapTextureCoords(3), attrib);
			vertices[nv + 3] = new Vertex(new float2(o.X, o.Y + size.Y),
				r.FastMapTextureCoords(2), attrib);
		}

		static readonly int[] channelMasks = { 2, 1, 0, 3 };	// yes, our channel order is nuts.

		public static void FastCopyIntoChannel(Sprite dest, byte[] src) { FastCopyIntoChannel(dest, 0, src); }
		public static void FastCopyIntoChannel(Sprite dest, int channelOffset, byte[] src)
		{
			var data = dest.sheet.Data;
			var srcStride = dest.bounds.Width;
			var destStride = dest.sheet.Size.Width * 4;
			var destOffset = destStride * dest.bounds.Top + dest.bounds.Left * 4 + channelMasks[(int)dest.channel + channelOffset];
			var destSkip = destStride - 4 * srcStride;
			var height = dest.bounds.Height;

			var srcOffset = 0;
			for (var j = 0; j < height; j++)
			{
				for (int i = 0; i < srcStride; i++, srcOffset++)
				{
					data[destOffset] = src[srcOffset];
					destOffset += 4;
				}
				destOffset += destSkip;
			}
		}
	}
}
