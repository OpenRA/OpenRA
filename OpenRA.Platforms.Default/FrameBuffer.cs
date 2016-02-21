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
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace OpenRA.Platforms.Default
{
	sealed class FrameBuffer : ThreadAffine, IFrameBuffer
	{
		readonly Texture texture;
		readonly Size size;
		uint framebuffer, depth;
		bool disposed;

		public FrameBuffer(Size size)
		{
			this.size = size;
			if (!Exts.IsPowerOf2(size.Width) || !Exts.IsPowerOf2(size.Height))
				throw new InvalidDataException("Frame buffer size ({0}x{1}) must be a power of two".F(size.Width, size.Height));

			OpenGL.glGenFramebuffers(1, out framebuffer);
			OpenGL.CheckGLError();
			OpenGL.glBindFramebuffer(OpenGL.FRAMEBUFFER_EXT, framebuffer);
			OpenGL.CheckGLError();

			// Color
			texture = new Texture();
			texture.SetEmpty(size.Width, size.Height);
			OpenGL.glFramebufferTexture2D(OpenGL.FRAMEBUFFER_EXT, OpenGL.COLOR_ATTACHMENT0_EXT, OpenGL.GL_TEXTURE_2D, texture.ID, 0);
			OpenGL.CheckGLError();

			// Depth
			OpenGL.glGenRenderbuffers(1, out depth);
			OpenGL.CheckGLError();

			OpenGL.glBindRenderbuffer(OpenGL.RENDERBUFFER_EXT, depth);
			OpenGL.CheckGLError();

			OpenGL.glRenderbufferStorage(OpenGL.RENDERBUFFER_EXT, OpenGL.GL_DEPTH_COMPONENT, size.Width, size.Height);
			OpenGL.CheckGLError();

			OpenGL.glFramebufferRenderbuffer(OpenGL.FRAMEBUFFER_EXT, OpenGL.DEPTH_ATTACHMENT_EXT, OpenGL.RENDERBUFFER_EXT, depth);
			OpenGL.CheckGLError();

			// Test for completeness
			var status = OpenGL.glCheckFramebufferStatus(OpenGL.FRAMEBUFFER_EXT);
			if (status != OpenGL.FRAMEBUFFER_COMPLETE_EXT)
			{
				var error = "Error creating framebuffer: {0}\n{1}".F(status, new StackTrace());
				OpenGL.WriteGraphicsLog(error);
				throw new InvalidOperationException("OpenGL Error: See graphics.log for details.");
			}

			// Restore default buffer
			OpenGL.glBindFramebuffer(OpenGL.FRAMEBUFFER_EXT, 0);
			OpenGL.CheckGLError();
		}

		static int[] ViewportRectangle()
		{
			var v = new int[4];
			unsafe
			{
				fixed (int* ptr = &v[0])
					OpenGL.glGetIntegerv(OpenGL.GL_VIEWPORT, ptr);
			}

			OpenGL.CheckGLError();
			return v;
		}

		int[] cv = new int[4];
		public void Bind()
		{
			VerifyThreadAffinity();

			// Cache viewport rect to restore when unbinding
			cv = ViewportRectangle();

			OpenGL.glFlush();
			OpenGL.CheckGLError();
			OpenGL.glBindFramebuffer(OpenGL.FRAMEBUFFER_EXT, framebuffer);
			OpenGL.CheckGLError();
			OpenGL.glViewport(0, 0, size.Width, size.Height);
			OpenGL.CheckGLError();
			OpenGL.glClearColor(0, 0, 0, 0);
			OpenGL.CheckGLError();
			OpenGL.glClear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
			OpenGL.CheckGLError();
		}

		public void Unbind()
		{
			VerifyThreadAffinity();
			OpenGL.glFlush();
			OpenGL.CheckGLError();
			OpenGL.glBindFramebuffer(OpenGL.FRAMEBUFFER_EXT, 0);
			OpenGL.CheckGLError();
			OpenGL.glViewport(cv[0], cv[1], cv[2], cv[3]);
			OpenGL.CheckGLError();
		}

		public ITexture Texture
		{
			get
			{
				VerifyThreadAffinity();
				return texture;
			}
		}

		~FrameBuffer()
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
			if (disposing)
				texture.Dispose();

			OpenGL.glDeleteFramebuffers(1, ref framebuffer);
			OpenGL.CheckGLError();
			OpenGL.glDeleteRenderbuffers(1, ref depth);
			OpenGL.CheckGLError();
		}
	}
}
