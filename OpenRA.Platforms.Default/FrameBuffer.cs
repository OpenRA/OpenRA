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
using System.Diagnostics;
using System.IO;
using OpenRA.Primitives;

namespace OpenRA.Platforms.Default
{
	sealed class FrameBuffer : ThreadAffine, IFrameBuffer
	{
		readonly ITexture texture;
		readonly Size size;
		readonly Color clearColor;
		uint framebuffer, depth;
		bool disposed;
		bool scissored;

		public FrameBuffer(Size size, ITextureInternal texture, Color clearColor)
		{
			this.size = size;
			this.clearColor = clearColor;
			if (!Exts.IsPowerOf2(size.Width) || !Exts.IsPowerOf2(size.Height))
				throw new InvalidDataException($"Frame buffer size ({size.Width}x{size.Height}) must be a power of two");

			OpenGL.glGenFramebuffers(1, out framebuffer);
			OpenGL.CheckGLError();
			OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, framebuffer);
			OpenGL.CheckGLError();

			// Color
			this.texture = texture;
			texture.SetEmpty(size.Width, size.Height);
			OpenGL.glFramebufferTexture2D(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT0, OpenGL.GL_TEXTURE_2D, texture.ID, 0);
			OpenGL.CheckGLError();

			// Depth
			OpenGL.glGenRenderbuffers(1, out depth);
			OpenGL.CheckGLError();

			OpenGL.glBindRenderbuffer(OpenGL.GL_RENDERBUFFER, depth);
			OpenGL.CheckGLError();

			var glDepth = OpenGL.Profile == GLProfile.Embedded ? OpenGL.GL_DEPTH_COMPONENT16 : OpenGL.GL_DEPTH_COMPONENT;
			OpenGL.glRenderbufferStorage(OpenGL.GL_RENDERBUFFER, glDepth, size.Width, size.Height);
			OpenGL.CheckGLError();

			OpenGL.glFramebufferRenderbuffer(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_DEPTH_ATTACHMENT, OpenGL.GL_RENDERBUFFER, depth);
			OpenGL.CheckGLError();

			// Test for completeness
			var status = OpenGL.glCheckFramebufferStatus(OpenGL.GL_FRAMEBUFFER);
			if (status != OpenGL.GL_FRAMEBUFFER_COMPLETE)
			{
				var error = $"Error creating framebuffer: {status}\n{new StackTrace()}";
				OpenGL.WriteGraphicsLog(error);
				throw new InvalidOperationException("OpenGL Error: See graphics.log for details.");
			}

			// Restore default buffer
			OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);
			OpenGL.CheckGLError();
		}

		static int[] ViewportRectangle()
		{
			var v = new int[4];
			OpenGL.glGetIntegerv(OpenGL.GL_VIEWPORT, out v[0]);
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
			OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, framebuffer);
			OpenGL.CheckGLError();
			OpenGL.glViewport(0, 0, size.Width, size.Height);
			OpenGL.CheckGLError();
			OpenGL.glClearColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A);
			OpenGL.CheckGLError();
			OpenGL.glClear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
			OpenGL.CheckGLError();
		}

		public void Unbind()
		{
			if (scissored)
				throw new InvalidOperationException("Attempting to unbind FrameBuffer with an active scissor region.");

			VerifyThreadAffinity();
			OpenGL.glFlush();
			OpenGL.CheckGLError();
			OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);
			OpenGL.CheckGLError();
			OpenGL.glViewport(cv[0], cv[1], cv[2], cv[3]);
			OpenGL.CheckGLError();
		}

		public void EnableScissor(Rectangle rect)
		{
			VerifyThreadAffinity();

			OpenGL.glScissor(rect.X, rect.Y, Math.Max(rect.Width, 0), Math.Max(rect.Height, 0));
			OpenGL.CheckGLError();
			OpenGL.glEnable(OpenGL.GL_SCISSOR_TEST);
			OpenGL.CheckGLError();
			scissored = true;
		}

		public void DisableScissor()
		{
			VerifyThreadAffinity();
			OpenGL.glDisable(OpenGL.GL_SCISSOR_TEST);
			OpenGL.CheckGLError();
			scissored = false;
		}

		public ITexture Texture
		{
			get
			{
				VerifyThreadAffinity();
				return texture;
			}
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;
			texture.Dispose();

			OpenGL.glDeleteFramebuffers(1, ref framebuffer);
			OpenGL.CheckGLError();
			OpenGL.glDeleteRenderbuffers(1, ref depth);
			OpenGL.CheckGLError();
		}
	}
}
