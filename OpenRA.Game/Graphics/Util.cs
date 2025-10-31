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
			var attribC = r.Channel == TextureChannel.RGBA ? 0x02 : ((byte)r.Channel) << 1 | 0x01;
			attribC |= samplers.X << 6;
			if (r is SpriteWithSecondaryData ss)
			{
				sl = ss.SecondaryLeft;
				st = ss.SecondaryTop;
				sr = ss.SecondaryRight;
				sb = ss.SecondaryBottom;

				attribC |= ((byte)ss.SecondaryChannel) << 4 | 0x08;
				attribC |= samplers.Y << 9;
			}

			attribC |= (paletteTextureIndex & 0xFFFF) << 16;

			var uAttribC = (uint)attribC;
			vertices[nv] = new Vertex(a, r.Left, r.Top, sl, st, uAttribC, tint, alpha);
			vertices[nv + 1] = new Vertex(b, r.Right, r.Top, sr, st, uAttribC, tint, alpha);
			vertices[nv + 2] = new Vertex(c, r.Right, r.Bottom, sr, sb, uAttribC, tint, alpha);
			vertices[nv + 3] = new Vertex(d, r.Left, r.Bottom, sl, sb, uAttribC, tint, alpha);
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
				for (var h = 0; h < height; h++)
				{
					s[si..(si + width)].CopyTo(d[di..(di + width)]);

					if (!premultiplied)
					{
						for (var w = 0; w < width; w++)
						{
							d[di] = PremultiplyAlpha(Color.FromArgb(d[di])).ToArgb();
							di++;
						}

						di -= width;
					}

					si += width;
					di += stride;
				}

				return;
			}

			for (var h = 0; h < height; h++)
			{
				for (var w = 0; w < width; w++)
				{
					byte r, g, b, a;
					switch (srcType)
					{
						case SpriteFrameType.Bgra32:
						case SpriteFrameType.Bgr24:
							b = src[si++];
							g = src[si++];
							r = src[si++];
							a = srcType == SpriteFrameType.Bgra32 ? src[si++] : byte.MaxValue;
							break;

						case SpriteFrameType.Rgba32:
						case SpriteFrameType.Rgb24:
							r = src[si++];
							g = src[si++];
							b = src[si++];
							a = srcType == SpriteFrameType.Rgba32 ? src[si++] : byte.MaxValue;
							break;

						default:
							throw new InvalidOperationException($"Unknown SpriteFrameType {srcType}");
					}

					var c = Color.FromArgb(a, r, g, b);
					if (!premultiplied)
						c = PremultiplyAlpha(c);
					d[di++] = c.ToArgb();
				}

				di += stride - width;
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

			var si = 0;
			var di = y * stride + x;
			var d = MemoryMarshal.Cast<byte, uint>(destData);

			for (var h = 0; h < height; h++)
			{
				for (var w = 0; w < width; w++)
				{
					Color c;
					switch (src.Type)
					{
						case SpriteFrameType.Indexed8:
							c = src.Palette[src.Data[si++]];
							break;

						case SpriteFrameType.Rgba32:
						case SpriteFrameType.Rgb24:
							var r = src.Data[si++];
							var g = src.Data[si++];
							var b = src.Data[si++];
							var a = src.Type == SpriteFrameType.Rgba32 ? src.Data[si++] : byte.MaxValue;
							c = Color.FromArgb(a, r, g, b);
							break;

						// PNGs don't support BGR[A], so no need to include them here
						default:
							throw new InvalidOperationException($"Unknown SpriteFrameType {src.Type}");
					}

					d[di++] = PremultiplyAlpha(c).ToArgb();
				}

				di += stride - width;
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
			var a = c.A / 255f;
			return Color.FromArgb(c.A, (byte)(c.R * a + 0.5f), (byte)(c.G * a + 0.5f), (byte)(c.B * a + 0.5f));
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
