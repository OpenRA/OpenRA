#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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

			OpenGL.glGenFramebuffersEXT(1, out framebuffer);
			ErrorHandler.CheckGlError();
			OpenGL.glBindFramebufferEXT(OpenGL.FRAMEBUFFER_EXT, framebuffer);
			ErrorHandler.CheckGlError();

			// Color
			texture = new Texture();
			texture.SetEmpty(size.Width, size.Height);
			OpenGL.glFramebufferTexture2DEXT(OpenGL.FRAMEBUFFER_EXT, OpenGL.COLOR_ATTACHMENT0_EXT, OpenGL.GL_TEXTURE_2D, texture.ID, 0);
			ErrorHandler.CheckGlError();

			// Depth
			OpenGL.glGenRenderbuffersEXT(1, out depth);
			ErrorHandler.CheckGlError();

			OpenGL.glBindRenderbufferEXT(OpenGL.RENDERBUFFER_EXT, depth);
			ErrorHandler.CheckGlError();

			OpenGL.glRenderbufferStorageEXT(OpenGL.RENDERBUFFER_EXT, OpenGL.GL_DEPTH_COMPONENT, size.Width, size.Height);
			ErrorHandler.CheckGlError();

			OpenGL.glFramebufferRenderbufferEXT(OpenGL.FRAMEBUFFER_EXT, OpenGL.DEPTH_ATTACHMENT_EXT, OpenGL.RENDERBUFFER_EXT, depth);
			ErrorHandler.CheckGlError();

			// Test for completeness
			var status = OpenGL.glCheckFramebufferStatus(OpenGL.FRAMEBUFFER_EXT);
			if (status != OpenGL.FRAMEBUFFER_COMPLETE_EXT)
			{
				var error = "Error creating framebuffer: {0}\n{1}".F(status, new StackTrace());
				ErrorHandler.WriteGraphicsLog(error);
				throw new InvalidOperationException("OpenGL Error: See graphics.log for details.");
			}

			// Restore default buffer
			OpenGL.glBindFramebufferEXT(OpenGL.FRAMEBUFFER_EXT, 0);
			ErrorHandler.CheckGlError();
		}

		static int[] ViewportRectangle()
		{
			var v = new int[4];
			unsafe
			{
				fixed (int* ptr = &v[0])
					OpenGL.glGetIntegerv(OpenGL.GL_VIEWPORT, ptr);
			}

			ErrorHandler.CheckGlError();
			return v;
		}

		int[] cv = new int[4];
		public void Bind()
		{
			VerifyThreadAffinity();

			// Cache viewport rect to restore when unbinding
			cv = ViewportRectangle();

			OpenGL.glFlush();
			ErrorHandler.CheckGlError();
			OpenGL.glBindFramebufferEXT(OpenGL.FRAMEBUFFER_EXT, framebuffer);
			ErrorHandler.CheckGlError();
			OpenGL.glViewport(0, 0, size.Width, size.Height);
			ErrorHandler.CheckGlError();
			OpenGL.glClearColor(0, 0, 0, 0);
			ErrorHandler.CheckGlError();
			OpenGL.glClear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
			ErrorHandler.CheckGlError();
		}

		public void Unbind()
		{
			VerifyThreadAffinity();
			OpenGL.glFlush();
			ErrorHandler.CheckGlError();
			OpenGL.glBindFramebufferEXT(OpenGL.FRAMEBUFFER_EXT, 0);
			ErrorHandler.CheckGlError();
			OpenGL.glViewport(cv[0], cv[1], cv[2], cv[3]);
			ErrorHandler.CheckGlError();
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

			OpenGL.glDeleteFramebuffersEXT(1, ref framebuffer);
			ErrorHandler.CheckGlError();
			OpenGL.glDeleteRenderbuffersEXT(1, ref depth);
			ErrorHandler.CheckGlError();
		}
	}
}
