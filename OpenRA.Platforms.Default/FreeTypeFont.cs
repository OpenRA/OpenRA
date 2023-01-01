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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using OpenRA.Primitives;

namespace OpenRA.Platforms.Default
{
	[SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore",
		Justification = "C-style naming is kept for consistency with the underlying native API.")]
	static class FreeType
	{
		internal const uint OK = 0x00;
		internal const int FT_LOAD_RENDER = 0x04;

		internal static readonly int FaceRecGlyphOffset = IntPtr.Size == 8 ? 152 : 84; // offsetof(FT_FaceRec, glyph)
		internal static readonly int GlyphSlotMetricsOffset = IntPtr.Size == 8 ? 48 : 24; // offsetof(FT_GlyphSlotRec, metrics)
		internal static readonly int GlyphSlotBitmapOffset = IntPtr.Size == 8 ? 152 : 76; // offsetof(FT_GlyphSlotRec, bitmap)
		internal static readonly int GlyphSlotBitmapLeftOffset = IntPtr.Size == 8 ? 192 : 100; // offsetof(FT_GlyphSlotRec, bitmap_left)
		internal static readonly int GlyphSlotBitmapTopOffset = IntPtr.Size == 8 ? 196 : 104; // offsetof(FT_GlyphSlotRec, bitmap_top)
		internal static readonly int MetricsWidthOffset = 0; // offsetof(FT_Glyph_Metrics, width)
		internal static readonly int MetricsHeightOffset = IntPtr.Size == 8 ? 8 : 4; // offsetof(FT_Glyph_Metrics, height)
		internal static readonly int MetricsAdvanceOffset = IntPtr.Size == 8 ? 32 : 16; // offsetof(FT_Glyph_Metrics, horiAdvance)
		internal static readonly int BitmapPitchOffset = 8; // offsetof(FT_Bitmap, pitch)
		internal static readonly int BitmapBufferOffset = IntPtr.Size == 8 ? 16 : 12; // offsetof(FT_Bitmap, buffer)

		[DllImport("freetype6", CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint FT_Init_FreeType(out IntPtr library);

		[DllImport("freetype6", CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint FT_New_Memory_Face(IntPtr library, IntPtr file_base, int file_size, int face_index, out IntPtr aface);

		[DllImport("freetype6", CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint FT_Done_Face(IntPtr face);

		[DllImport("freetype6", CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint FT_Set_Pixel_Sizes(IntPtr face, uint pixel_width, uint pixel_height);

		[DllImport("freetype6", CallingConvention = CallingConvention.Cdecl)]
		internal static extern uint FT_Load_Char(IntPtr face, uint char_code, int load_flags);
	}

	public sealed class FreeTypeFont : IFont
	{
		static readonly FontGlyph EmptyGlyph = new FontGlyph
		{
			Offset = int2.Zero,
			Size = new Size(0, 0),
			Advance = 0,
			Data = null
		};

		static IntPtr library = IntPtr.Zero;
		readonly GCHandle faceHandle;
		readonly IntPtr face;
		bool disposed;

		public FreeTypeFont(byte[] data)
		{
			if (library == IntPtr.Zero && FreeType.FT_Init_FreeType(out library) != FreeType.OK)
				throw new InvalidOperationException("Failed to initialize FreeType");

			faceHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
			if (FreeType.FT_New_Memory_Face(library, faceHandle.AddrOfPinnedObject(), data.Length, 0, out face) != FreeType.OK)
				throw new InvalidDataException("Failed to initialize font");
		}

		public FontGlyph CreateGlyph(char c, int size, float deviceScale)
		{
			var scaledSize = (uint)(size * deviceScale);
			if (FreeType.FT_Set_Pixel_Sizes(face, scaledSize, scaledSize) != FreeType.OK)
				return EmptyGlyph;

			if (FreeType.FT_Load_Char(face, c, FreeType.FT_LOAD_RENDER) != FreeType.OK)
				return EmptyGlyph;

			// Extract the glyph data we care about
			// HACK: This uses raw pointer offsets to avoid defining structs and types that are 95% unnecessary
			var glyph = Marshal.ReadIntPtr(IntPtr.Add(face, FreeType.FaceRecGlyphOffset)); // face->glyph

			var metrics = IntPtr.Add(glyph, FreeType.GlyphSlotMetricsOffset); // face->glyph->metrics
			var metricsWidth = Marshal.ReadIntPtr(IntPtr.Add(metrics, FreeType.MetricsWidthOffset)); // face->glyph->metrics.width
			var metricsHeight = Marshal.ReadIntPtr(IntPtr.Add(metrics, FreeType.MetricsHeightOffset)); // face->glyph->metrics.width
			var metricsAdvance = Marshal.ReadIntPtr(IntPtr.Add(metrics, FreeType.MetricsAdvanceOffset)); // face->glyph->metrics.horiAdvance

			var bitmap = IntPtr.Add(glyph, FreeType.GlyphSlotBitmapOffset); // face->glyph->bitmap
			var bitmapPitch = Marshal.ReadInt32(IntPtr.Add(bitmap, FreeType.BitmapPitchOffset)); // face->glyph->bitmap.pitch
			var bitmapBuffer = Marshal.ReadIntPtr(IntPtr.Add(bitmap, FreeType.BitmapBufferOffset)); // face->glyph->bitmap.buffer

			var bitmapLeft = Marshal.ReadInt32(IntPtr.Add(glyph, FreeType.GlyphSlotBitmapLeftOffset)); // face->glyph.bitmap_left
			var bitmapTop = Marshal.ReadInt32(IntPtr.Add(glyph, FreeType.GlyphSlotBitmapTopOffset)); // face->glyph.bitmap_top

			// Convert FreeType's 26.6 fixed point format to integers by discarding fractional bits
			var glyphSize = new Size((int)metricsWidth >> 6, (int)metricsHeight >> 6);
			var glyphAdvance = (int)metricsAdvance >> 6;

			var g = new FontGlyph
			{
				Advance = glyphAdvance,
				Offset = new int2(bitmapLeft, -bitmapTop),
				Size = glyphSize,
				Data = new byte[glyphSize.Width * glyphSize.Height]
			};

			unsafe
			{
				var p = (byte*)bitmapBuffer;
				var k = 0;
				for (var j = 0; j < glyphSize.Height; j++)
				{
					for (var i = 0; i < glyphSize.Width; i++)
						g.Data[k++] = p[i];

					p += bitmapPitch;
				}
			}

			return g;
		}

		public void Dispose()
		{
			if (!disposed)
			{
				if (faceHandle.IsAllocated)
				{
					FreeType.FT_Done_Face(face);

					faceHandle.Free();
					disposed = true;
				}
			}
		}
	}
}
