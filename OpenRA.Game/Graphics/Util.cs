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
using System.Runtime.InteropServices;
using OpenRA.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public static class Util
	{
		// yes, our channel order is nuts.
		static readonly int[] ChannelMasks = [2, 1, 0, 3];

		public static uint[] CreateQuadIndices(int quads)
		{
			var indices = new uint[quads * 6];
			ReadOnlySpan<uint> cornerVertexMap = [0, 1, 2, 2, 3, 0];
			for (var i = 0; i < indices.Length; i++)
				indices[i] = cornerVertexMap[i % 6] + (uint)(4 * (i / 6));

			return indices;
		}

		public static void FastCreateQuad(Vertex[] vertices, in float3 o, Sprite r, int2 samplers, int paletteTextureIndex, int nv,
			in float3 size, in float3 tint, float alpha, float rotation = 0f)
		{
			float3 a, b, c, d;

			// Rotate sprite if rotation angle is not equal to 0
			if (rotation != 0f)
			{
				var center = o + 0.5f * size;
				var angleSin = (float)Math.Sin(-rotation);
				var angleCos = (float)Math.Cos(-rotation);

				// Rotated offset for +/- x with +/- y
				var ra = 0.5f * new float3(
					size.X * angleCos - size.Y * angleSin,
					size.X * angleSin + size.Y * angleCos,
					(size.X * angleSin + size.Y * angleCos) * size.Z / size.Y);

				// Rotated offset for +/- x with -/+ y
				var rb = 0.5f * new float3(
					size.X * angleCos + size.Y * angleSin,
					size.X * angleSin - size.Y * angleCos,
					(size.X * angleSin - size.Y * angleCos) * size.Z / size.Y);

				a = center - ra;
				b = center + rb;
				c = center + ra;
				d = center - rb;
			}
			else
			{
				a = o;
				b = new float3(o.X + size.X, o.Y, o.Z);
				c = new float3(o.X + size.X, o.Y + size.Y, o.Z + size.Z);
				d = new float3(o.X, o.Y + size.Y, o.Z + size.Z);
			}

			FastCreateQuad(vertices, a, b, c, d, r, samplers, paletteTextureIndex, tint, alpha, nv);
		}

		public static void FastCreateQuad(Vertex[] vertices,
			in float3 a, in float3 b, in float3 c, in float3 d,
			Sprite r, int2 samplers, int paletteTextureIndex,
			in float3 tint, float alpha, int nv)
		{
			float sl = 0;
			float st = 0;
			float sr = 0;
			float sb = 0;

			// See combined.vert for documentation on the channel attribute format
			var attribC = r.Channel == TextureChannel.RGBA ? 0x02u : ((uint)r.Channel << 1) | 0x01u;
			attribC |= (uint)samplers.X << 6;
			if (r is SpriteWithSecondaryData ss)
			{
				sl = ss.SecondaryLeft;
				st = ss.SecondaryTop;
				sr = ss.SecondaryRight;
				sb = ss.SecondaryBottom;

				attribC |= ((uint)ss.SecondaryChannel) << 4 | 0x08;
				attribC |= (uint)samplers.Y << 9;
			}

			attribC |= ((uint)paletteTextureIndex & 0xFFFFu) << 16;

			vertices[nv] = new Vertex(a, r.Left, r.Top, sl, st, attribC, tint, alpha);
			vertices[nv + 1] = new Vertex(b, r.Right, r.Top, sr, st, attribC, tint, alpha);
			vertices[nv + 2] = new Vertex(c, r.Right, r.Bottom, sr, sb, attribC, tint, alpha);
			vertices[nv + 3] = new Vertex(d, r.Left, r.Bottom, sl, sb, attribC, tint, alpha);
		}

		public static void FastCopyIntoChannel(Sprite dest, byte[] src, SpriteFrameType srcType, bool premultiplied = false)
		{
			var destData = dest.Sheet.GetData();
			var stride = dest.Sheet.Size.Width;
			var x = dest.Bounds.Left;
			var y = dest.Bounds.Top;
			var width = dest.Bounds.Width;
			var height = dest.Bounds.Height;

			if (dest.Channel == TextureChannel.RGBA)
			{
				CopyIntoRgba(src, srcType, premultiplied, destData, x, y, width, height, stride);
			}
			else
			{
				// Copy into single channel of destination.
				var destStride = stride * 4;
				var destOffset = destStride * y + x * 4 + ChannelMasks[(int)dest.Channel];
				var destSkip = destStride - 4 * width;

				var srcOffset = 0;
				for (var j = 0; j < height; j++)
				{
					for (var i = 0; i < width; i++, srcOffset++)
					{
						destData[destOffset] = src[srcOffset];
						destOffset += 4;
					}

					destOffset += destSkip;
				}
			}
		}

		static void CopyIntoRgba(
			byte[] src, SpriteFrameType srcType, bool premultiplied, byte[] dest, int x, int y, int width, int height, int stride)
		{
			var si = 0;
			var di = y * stride + x;
			var d = MemoryMarshal.Cast<byte, uint>(dest);

			// SpriteFrameType.Brga32 is a common source format, and it matches the destination format.
			// Provide a fast past that just performs memory copies.
			if (srcType == SpriteFrameType.Bgra32)
			{
				var s = MemoryMarshal.Cast<byte, uint>(src);
				if (premultiplied)
				{
					for (var h = 0; h < height; h++)
					{
						s[si..(si + width)].CopyTo(d[di..(di + width)]);
						si += width;
						di += stride;
					}
				}
				else
				{
					for (var h = 0; h < height; h++)
					{
						for (var w = 0; w < width; w++)
							d[di++] = PremultiplyPixel(s[si++]);

						di += stride - width;
					}
				}

				return;
			}

			switch (srcType)
			{
				case SpriteFrameType.Bgr24:
					for (var h = 0; h < height; h++)
					{
						for (var w = 0; w < width; w++)
						{
							var b = src[si++];
							var g = src[si++];
							var r = src[si++];

							var pixel = 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;
							d[di++] = premultiplied ? pixel : PremultiplyPixel(pixel);
						}

						di += stride - width;
					}

					break;

				case SpriteFrameType.Rgba32:
					for (var h = 0; h < height; h++)
					{
						for (var w = 0; w < width; w++)
						{
							var b = src[si++];
							var g = src[si++];
							var r = src[si++];
							var a = src[si++];

							var pixel = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
							d[di++] = premultiplied ? pixel : PremultiplyPixel(pixel);
						}

						di += stride - width;
					}

					break;

				case SpriteFrameType.Rgb24:
					for (var h = 0; h < height; h++)
					{
						for (var w = 0; w < width; w++)
						{
							var b = src[si++];
							var g = src[si++];
							var r = src[si++];

							var pixel = 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b;
							d[di++] = premultiplied ? pixel : PremultiplyPixel(pixel);
						}

						di += stride - width;
					}

					break;

				default:
					throw new InvalidOperationException($"Unknown SpriteFrameType {srcType}");
			}
		}

		public static void FastCopyIntoSprite(Sprite dest, Png src)
		{
			var destData = dest.Sheet.GetData();
			var stride = dest.Sheet.Size.Width;
			var x = dest.Bounds.Left;
			var y = dest.Bounds.Top;
			var width = dest.Bounds.Width;
			var height = dest.Bounds.Height;

			var destSpan = MemoryMarshal.Cast<byte, uint>(destData);
			var srcData = src.Data;

			var diBase = y * stride + x;
			var si = 0;

			switch (src.Type)
			{
				case SpriteFrameType.Indexed8:
					var palette = src.Palette;
					for (var h = 0; h < height; h++)
					{
						var di = diBase + h * stride;
						for (var w = 0; w < width; w++)
						{
							var entry = srcData[si++];
							var c = palette[entry];

							// Inline PremultiplyAlpha + ToArgb
							var a = (uint)c.A;
							var r = (c.R * a + 128) / 255;
							var g = (c.G * a + 128) / 255;
							var b = (c.B * a + 128) / 255;

							destSpan[di++] = (a << 24) | (r << 16) | (g << 8) | b;
						}
					}

					break;

				case SpriteFrameType.Rgba32:
					for (var h = 0; h < height; h++)
					{
						var di = diBase + h * stride;
						for (var w = 0; w < width; w++)
						{
							var r = srcData[si++];
							var g = srcData[si++];
							var b = srcData[si++];
							var a = srcData[si++];

							// Inline Premultiply
							var pr = (r * a + 128) / 255;
							var pg = (g * a + 128) / 255;
							var pb = (b * a + 128) / 255;

							destSpan[di++] = (uint)((a << 24) | (pr << 16) | (pg << 8) | pb);
						}
					}

					break;

				case SpriteFrameType.Rgb24:
					for (var h = 0; h < height; h++)
					{
						var di = diBase + h * stride;
						for (var w = 0; w < width; w++)
						{
							var r = srcData[si++];
							var g = srcData[si++];
							var b = srcData[si++];

							destSpan[di++] = 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;
						}
					}

					break;

				// PNGs don't support BGR[A], so no need to include them here
				default:
					throw new InvalidOperationException($"Unknown SpriteFrameType {src.Type}");
			}
		}

		/// <summary>Rotates a quad about its center in the x-y plane.</summary>
		/// <param name="tl">The top left vertex of the quad.</param>
		/// <param name="size">A float3 containing the X, Y, and Z lengths of the quad.</param>
		/// <param name="rotation">The number of radians to rotate by.</param>
		/// <returns>An array of four vertices representing the rotated quad (top-left, top-right, bottom-right, bottom-left).</returns>
		public static float3[] RotateQuad(float3 tl, float3 size, float rotation)
		{
			var center = tl + 0.5f * size;
			var angleSin = (float)Math.Sin(-rotation);
			var angleCos = (float)Math.Cos(-rotation);

			// Rotated offset for +/- x with +/- y
			var ra = 0.5f * new float3(
				size.X * angleCos - size.Y * angleSin,
				size.X * angleSin + size.Y * angleCos,
				(size.X * angleSin + size.Y * angleCos) * size.Z / size.Y);

			// Rotated offset for +/- x with -/+ y
			var rb = 0.5f * new float3(
				size.X * angleCos + size.Y * angleSin,
				size.X * angleSin - size.Y * angleCos,
				(size.X * angleSin - size.Y * angleCos) * size.Z / size.Y);

			return
			[
				center - ra,
				center + rb,
				center + ra,
				center - rb
			];
		}

		/// <summary>
		/// Returns the bounds of an object. Used for determining which objects need to be rendered on screen, and which do not.
		/// </summary>
		/// <param name="offset">The top left vertex of the object.</param>
		/// <param name="size">A float 3 containing the X, Y, and Z lengths of the object.</param>
		/// <param name="rotation">The angle to rotate the object by (use 0f if there is no rotation).</param>
		public static Rectangle BoundingRectangle(float3 offset, float3 size, float rotation)
		{
			if (rotation == 0f)
				return new Rectangle((int)offset.X, (int)offset.Y, (int)size.X, (int)size.Y);

			var rotatedQuad = RotateQuad(offset, size, rotation);
			var minX = rotatedQuad[0].X;
			var maxX = rotatedQuad[0].X;
			var minY = rotatedQuad[0].Y;
			var maxY = rotatedQuad[0].Y;
			for (var i = 1; i < rotatedQuad.Length; i++)
			{
				minX = Math.Min(rotatedQuad[i].X, minX);
				maxX = Math.Max(rotatedQuad[i].X, maxX);
				minY = Math.Min(rotatedQuad[i].Y, minY);
				maxY = Math.Max(rotatedQuad[i].Y, maxY);
			}

			return new Rectangle(
				(int)minX,
				(int)minY,
				(int)Math.Ceiling(maxX) - (int)minX,
				(int)Math.Ceiling(maxY) - (int)minY);
		}

		public static Color PremultiplyAlpha(Color c)
		{
			if (c.A == byte.MaxValue)
				return c;

			return Color.FromArgb(c.A, (byte)((c.R * c.A + 128) >> 8), (byte)((c.G * c.A + 128) >> 8), (byte)((c.B * c.A + 128) >> 8));
		}

		static uint PremultiplyPixel(uint pixel)
		{
			var a = (pixel >> 24) & 0xFF;
			var r = (pixel >> 16) & 0xFF;
			var g = (pixel >> 8) & 0xFF;
			var b = pixel & 0xFF;

			if (a == byte.MaxValue)
				return pixel;

			if (a == 0)
				return 0;

			// Optimization: (channel  * alpha + 128) / 255 is faster and accurate enough
			var pr = (r * a + 128) / 255;
			var pg = (g * a + 128) / 255;
			var pb = (b * a + 128) / 255;

			return (a << 24) | (pr << 16) | (pg << 8) | pb;
		}

		public static Color PremultipliedColorLerp(float t, Color c1, Color c2)
		{
			// Colors must be lerped in a non-multiplied color space
			var a1 = 255f / c1.A;
			var a2 = 255f / c2.A;
			return PremultiplyAlpha(Color.FromArgb(
				(int)(t * c2.A + (1 - t) * c1.A),
				(int)((byte)(t * a2 * c2.R + 0.5f) + (1 - t) * (byte)(a1 * c1.R + 0.5f)),
				(int)((byte)(t * a2 * c2.G + 0.5f) + (1 - t) * (byte)(a1 * c1.G + 0.5f)),
				(int)((byte)(t * a2 * c2.B + 0.5f) + (1 - t) * (byte)(a1 * c1.B + 0.5f))));
		}
	}
}
