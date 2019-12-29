#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using OpenRA.Primitives;

namespace OpenRA.Platforms.Default
{
	sealed class Texture : ThreadAffine, ITextureInternal
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
					var glInternalFormat = OpenGL.Features.HasFlag(OpenGL.GLFeatures.GLES) ? OpenGL.GL_BGRA : OpenGL.GL_RGBA8;
					OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, glInternalFormat, width, height,
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
					var glInternalFormat = OpenGL.Features.HasFlag(OpenGL.GLFeatures.GLES) ? OpenGL.GL_BGRA : OpenGL.GL_RGBA8;
					OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, glInternalFormat, width, height,
						0, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, intPtr);
					OpenGL.CheckGLError();
				}
			}
		}

		public byte[] GetData()
		{
			VerifyThreadAffinity();
			var data = new byte[4 * Size.Width * Size.Height];

			// GLES doesn't support glGetTexImage so data must be read back via a frame buffer
			if (OpenGL.Features.HasFlag(OpenGL.GLFeatures.GLES))
			{
				// Query the active framebuffer so we can restore it afterwards
				int lastFramebuffer;
				OpenGL.glGetIntegerv(OpenGL.GL_FRAMEBUFFER_BINDING, out lastFramebuffer);

				uint framebuffer;
				OpenGL.glGenFramebuffers(1, out framebuffer);
				OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, framebuffer);
				OpenGL.CheckGLError();

				OpenGL.glFramebufferTexture2D(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT0, OpenGL.GL_TEXTURE_2D, texture, 0);
				OpenGL.CheckGLError();

				unsafe
				{
					fixed (byte* ptr = &data[0])
					{
						var intPtr = new IntPtr((void*)ptr);
						OpenGL.glReadPixels(0, 0, Size.Width, Size.Height, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, intPtr);
						OpenGL.CheckGLError();
					}
				}

				OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, (uint)lastFramebuffer);
				OpenGL.glDeleteFramebuffers(1, ref framebuffer);
				OpenGL.CheckGLError();
			}
			else
			{
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
			}

			return data;
		}

		public void SetEmpty(int width, int height)
		{
			VerifyThreadAffinity();
			if (!Exts.IsPowerOf2(width) || !Exts.IsPowerOf2(height))
				throw new InvalidDataException("Non-power-of-two array {0}x{1}".F(width, height));

			Size = new Size(width, height);
			PrepareTexture();
			var glInternalFormat = OpenGL.Features.HasFlag(OpenGL.GLFeatures.GLES) ? OpenGL.GL_BGRA : OpenGL.GL_RGBA8;
			OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, glInternalFormat, width, height,
				0, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, IntPtr.Zero);
			OpenGL.CheckGLError();
		}

		public void Dispose()
		{
			Dispose(true);
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
