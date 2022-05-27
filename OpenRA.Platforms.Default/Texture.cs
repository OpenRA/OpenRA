#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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

		public uint ID => texture;
		public Size Size { get; private set; }

		bool disposed;

		public TextureScaleFilter ScaleFilter
		{
			get => scaleFilter;

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

		void SetData(IntPtr data, int width, int height)
		{
			PrepareTexture();
			var glInternalFormat = OpenGL.Profile == GLProfile.Embedded ? OpenGL.GL_BGRA : OpenGL.GL_RGBA8;
			OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, glInternalFormat, width, height,
				0, OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, data);
			OpenGL.CheckGLError();
		}

		public void SetData(byte[] colors, int width, int height)
		{
			VerifyThreadAffinity();
			if (!Exts.IsPowerOf2(width) || !Exts.IsPowerOf2(height))
				throw new InvalidDataException($"Non-power-of-two array {width}x{height}");

			Size = new Size(width, height);
			unsafe
			{
				fixed (byte* ptr = &colors[0])
					SetData(new IntPtr(ptr), width, height);
			}
		}

		public void SetFloatData(float[] data, int width, int height)
		{
			VerifyThreadAffinity();
			if (!Exts.IsPowerOf2(width) || !Exts.IsPowerOf2(height))
				throw new InvalidDataException("Non-power-of-two array {0}x{1}".F(width, height));

			Size = new Size(width, height);
			unsafe
			{
				fixed (float* ptr = &data[0])
				{
					PrepareTexture();
					OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA16F, width, height,
						0, OpenGL.GL_RGBA, OpenGL.GL_FLOAT, new IntPtr(ptr));
					OpenGL.CheckGLError();
				}
			}
		}

		public byte[] GetData()
		{
			VerifyThreadAffinity();
			var data = new byte[4 * Size.Width * Size.Height];

			// GLES doesn't support glGetTexImage so data must be read back via a frame buffer
			if (OpenGL.Profile == GLProfile.Embedded)
			{
				// Query the active framebuffer so we can restore it afterwards
				OpenGL.glGetIntegerv(OpenGL.GL_FRAMEBUFFER_BINDING, out var lastFramebuffer);

				OpenGL.glGenFramebuffers(1, out var framebuffer);
				OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, framebuffer);
				OpenGL.CheckGLError();

				OpenGL.glFramebufferTexture2D(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT0, OpenGL.GL_TEXTURE_2D, texture, 0);
				OpenGL.CheckGLError();

				var canReadBGRA = OpenGL.Features.HasFlag(OpenGL.GLFeatures.ESReadFormatBGRA);

				unsafe
				{
					fixed (byte* ptr = &data[0])
					{
						var intPtr = new IntPtr(ptr);

						var format = canReadBGRA ? OpenGL.GL_BGRA : OpenGL.GL_RGBA;
						OpenGL.glReadPixels(0, 0, Size.Width, Size.Height, format, OpenGL.GL_UNSIGNED_BYTE, intPtr);
						OpenGL.CheckGLError();
					}
				}

				// Convert RGBA to BGRA
				if (!canReadBGRA)
				{
					for (var i = 0; i < 4 * Size.Width * Size.Height; i += 4)
					{
						var temp = data[i];
						data[i] = data[i + 2];
						data[i + 2] = temp;
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
				throw new InvalidDataException($"Non-power-of-two array {width}x{height}");

			Size = new Size(width, height);
			SetData(IntPtr.Zero, width, height);
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			OpenGL.glDeleteTextures(1, ref texture);
		}
	}
}
