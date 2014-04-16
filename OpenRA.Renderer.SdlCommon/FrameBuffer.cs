#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Graphics;
using Tao.OpenGl;

namespace OpenRA.Renderer.SdlCommon
{
	public class FrameBuffer : IFrameBuffer
	{
		Texture texture;
		Size size;
		int framebuffer, depth;

		public FrameBuffer(Size size)
		{
			this.size = size;
			if (!Exts.IsPowerOf2(size.Width) || !Exts.IsPowerOf2(size.Height))
				throw new InvalidDataException("Frame buffer size ({0}x{1}) must be a power of two".F(size.Width, size.Height));

			Gl.glGenFramebuffersEXT(1, out framebuffer);
			ErrorHandler.CheckGlError();
			Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, framebuffer);
			ErrorHandler.CheckGlError();

			// Color
			texture = new Texture();
			texture.SetEmpty(size.Width, size.Height);
			Gl.glFramebufferTexture2DEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_COLOR_ATTACHMENT0_EXT, Gl.GL_TEXTURE_2D, texture.ID, 0);
			ErrorHandler.CheckGlError();

			// Depth
			Gl.glGenRenderbuffersEXT(1, out depth);
			ErrorHandler.CheckGlError();

			Gl.glBindRenderbufferEXT(Gl.GL_RENDERBUFFER_EXT, depth);
			ErrorHandler.CheckGlError();

			Gl.glRenderbufferStorageEXT(Gl.GL_RENDERBUFFER_EXT, Gl.GL_DEPTH_COMPONENT, size.Width, size.Height);
			ErrorHandler.CheckGlError();

			Gl.glFramebufferRenderbufferEXT(Gl.GL_FRAMEBUFFER_EXT, Gl.GL_DEPTH_ATTACHMENT_EXT, Gl.GL_RENDERBUFFER_EXT, depth);
			ErrorHandler.CheckGlError();

			// Test for completeness
			var status = Gl.glCheckFramebufferStatusEXT(Gl.GL_FRAMEBUFFER_EXT);
			if (status != Gl.GL_FRAMEBUFFER_COMPLETE_EXT)
			{
				var error = "Error creating framebuffer: {0}\n{1}".F((ErrorHandler.GlError)status, new StackTrace());
				ErrorHandler.WriteGraphicsLog(error);
				throw new InvalidOperationException("OpenGL Error: See graphics.log for details.");
			}

			// Restore default buffer
			Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, 0);
			ErrorHandler.CheckGlError();
		}

		static int[] ViewportRectangle()
		{
			var v = new int[4];
			unsafe
			{
				fixed (int* ptr = &v[0])
				{
					IntPtr intPtr = new IntPtr((void*)ptr);
					Gl.glGetIntegerv(Gl.GL_VIEWPORT, intPtr);
				}
			}

			ErrorHandler.CheckGlError();
			return v;
		}

		void FinalizeInner()
		{
			Gl.glDeleteFramebuffersEXT(1, ref framebuffer);
			ErrorHandler.CheckGlError();
			Gl.glDeleteRenderbuffersEXT(1, ref depth);
			ErrorHandler.CheckGlError();
		}

		~FrameBuffer() { Game.RunAfterTick(FinalizeInner); }

		int[] cv = new int[4];
		public void Bind()
		{
			// Cache viewport rect to restore when unbinding
			cv = ViewportRectangle();

			Gl.glFlush();
			ErrorHandler.CheckGlError();
			Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, framebuffer);
			ErrorHandler.CheckGlError();
			Gl.glViewport(0, 0, size.Width, size.Height);
			ErrorHandler.CheckGlError();
			Gl.glClearColor(0, 0, 0, 0);
			ErrorHandler.CheckGlError();
			Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
			ErrorHandler.CheckGlError();
		}

		public void Unbind()
		{
			Gl.glFlush();
			ErrorHandler.CheckGlError();
			Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, 0);
			ErrorHandler.CheckGlError();
			Gl.glViewport(cv[0], cv[1], cv[2], cv[3]);
			ErrorHandler.CheckGlError();
		}

		public ITexture Texture { get { return texture; } }
	}
}
