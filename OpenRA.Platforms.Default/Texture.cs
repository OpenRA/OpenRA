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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace OpenRA.Platforms.Default
{
	sealed class Texture : ThreadAffine, ITexture
	{
		uint texture;
		TextureScaleFilter scaleFilter;

		public uint ID { get { return texture; } }
		public Size Size { get; private set; }

		bool disposed;

		public TextureScaleFilter ScaleFilter
		{
			get
			{
				return scaleFilter;
			}

			set
			{
				VerifyThreadAffinity();
				if (scaleFilter == value)
					return;

				scaleFilter = value;
				PrepareTexture();
			}
		}

		public Texture()
		{
			OpenGL.glGenTextures(1, out texture);
			OpenGL.CheckGLError();
		}

		public Texture(Bitmap bitmap)
		{
			OpenGL.glGenTextures(1, out texture);
			OpenGL.CheckGLError();
			SetData(bitmap);
		}

		void PrepareTexture()
		{
			OpenGL.CheckGLError();
			OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, texture);
			OpenGL.CheckGLError();

			var filter = scaleFilter == TextureScaleFilter.Linear ? OpenGL.GL_LINEAR : OpenGL.GL_NEAREST;
			OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, filter);
			OpenGL.CheckGLError();
			OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, filter);
			OpenGL.CheckGLError();

			OpenGL.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_CLAMP_TO_EDGE);
			OpenGL.CheckGLError();
			OpenGL.glTexParameterf(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_CLAMP_TO_EDGE);
			OpenGL.CheckGLError();

			OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_BASE_LEVEL, 0);
			OpenGL.CheckGLError();
			OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAX_LEVEL, 0);
			OpenGL.CheckGLError();
		}

		public void SetData(byte[] colors, int width, int height)
		{
			VerifyThreadAffinity();
			if (!Exts.IsPowerOf2(width) || !Exts.IsPowerOf2(height))
				throw new InvalidDataException("Non-power-of-two array {0}x{1}".F(width, height));

			Size = new Size(width, height);
			unsafe
			{
				fixed (byte* ptr = &colors[0])
				{
					var intPtr = new IntPtr((void*)ptr);
					PrepareTexture();
					OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA8, width, height,
						0, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, intPtr);
					OpenGL.CheckGLError();
				}
			}
		}

		// An array of RGBA
		public void SetData(uint[,] colors)
		{
			VerifyThreadAffinity();
			var width = colors.GetUpperBound(1) + 1;
			var height = colors.GetUpperBound(0) + 1;

			if (!Exts.IsPowerOf2(width) || !Exts.IsPowerOf2(height))
				throw new InvalidDataException("Non-power-of-two array {0}x{1}".F(width, height));

			Size = new Size(width, height);
			unsafe
			{
				fixed (uint* ptr = &colors[0, 0])
				{
					var intPtr = new IntPtr((void*)ptr);
					PrepareTexture();
					OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA8, width, height,
						0, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, intPtr);
					OpenGL.CheckGLError();
				}
			}
		}

		public void SetData(Bitmap bitmap)
		{
			VerifyThreadAffinity();
			var allocatedBitmap = false;
			if (!Exts.IsPowerOf2(bitmap.Width) || !Exts.IsPowerOf2(bitmap.Height))
			{
				bitmap = new Bitmap(bitmap, bitmap.Size.NextPowerOf2());
				allocatedBitmap = true;
			}

			try
			{
				Size = new Size(bitmap.Width, bitmap.Height);
				var bits = bitmap.LockBits(bitmap.Bounds(),
					ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

				PrepareTexture();
				OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA8, bits.Width, bits.Height,
					0, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, bits.Scan0); // TODO: weird strides
				OpenGL.CheckGLError();
				bitmap.UnlockBits(bits);
			}
			finally
			{
				if (allocatedBitmap)
					bitmap.Dispose();
			}
		}

		public byte[] GetData()
		{
			VerifyThreadAffinity();
			var data = new byte[4 * Size.Width * Size.Height];

			OpenGL.CheckGLError();
			OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, texture);
			unsafe
			{
				fixed (byte* ptr = &data[0])
				{
					var intPtr = new IntPtr((void*)ptr);
					OpenGL.glGetTexImage(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_BGRA,
						OpenGL.GL_UNSIGNED_BYTE, intPtr);
				}
			}

			OpenGL.CheckGLError();
			return data;
		}

		public void SetEmpty(int width, int height)
		{
			VerifyThreadAffinity();
			if (!Exts.IsPowerOf2(width) || !Exts.IsPowerOf2(height))
				throw new InvalidDataException("Non-power-of-two array {0}x{1}".F(width, height));

			Size = new Size(width, height);
			PrepareTexture();
			OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA8, width, height,
				0, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, IntPtr.Zero);
			OpenGL.CheckGLError();
		}

		~Texture()
		{
			Game.RunAfterTick(() => Dispose(false));
		}

		public void Dispose()
		{
			Game.RunAfterTick(() => Dispose(true));
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (disposed)
				return;
			disposed = true;
			OpenGL.glDeleteTextures(1, ref texture);
		}
	}
}
