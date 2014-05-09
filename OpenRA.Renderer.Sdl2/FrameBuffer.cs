#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
using OpenTK.Graphics.OpenGL;

namespace OpenRA.Renderer.Sdl2
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

			GL.Ext.GenFramebuffers(1, out framebuffer);
			ErrorHandler.CheckGlError();
			GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, framebuffer);
			ErrorHandler.CheckGlError();

			// Color
			texture = new Texture();
			texture.SetEmpty(size.Width, size.Height);
			GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext, TextureTarget.Texture2D, texture.ID, 0);
			ErrorHandler.CheckGlError();

			// Depth
			GL.Ext.GenRenderbuffers(1, out depth);
			ErrorHandler.CheckGlError();

			GL.Ext.BindRenderbuffer(RenderbufferTarget.RenderbufferExt, depth);
			ErrorHandler.CheckGlError();

			GL.Ext.RenderbufferStorage(RenderbufferTarget.RenderbufferExt, (RenderbufferStorage)All.DepthComponent, size.Width, size.Height);
			ErrorHandler.CheckGlError();

			GL.Ext.FramebufferRenderbuffer(FramebufferTarget.FramebufferExt, FramebufferAttachment.DepthAttachmentExt, RenderbufferTarget.RenderbufferExt, depth);
			ErrorHandler.CheckGlError();

			// Test for completeness
			var status = GL.Ext.CheckFramebufferStatus(FramebufferTarget.FramebufferExt);
			if (status != FramebufferErrorCode.FramebufferCompleteExt)
			{
				var error = "Error creating framebuffer: {0}\n{1}".F(status, new StackTrace());
				ErrorHandler.WriteGraphicsLog(error);
				throw new InvalidOperationException("OpenGL Error: See graphics.log for details.");
			}

			// Restore default buffer
			GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);
			ErrorHandler.CheckGlError();
		}

		static int[] ViewportRectangle()
		{
			var v = new int[4];
			unsafe
			{
				fixed (int* ptr = &v[0])
				{
					GL.GetInteger(GetPName.Viewport, ptr);
				}
			}

			ErrorHandler.CheckGlError();
			return v;
		}

		void FinalizeInner()
		{
			GL.Ext.DeleteFramebuffers(1, ref framebuffer);
			ErrorHandler.CheckGlError();
			GL.Ext.DeleteRenderbuffers(1, ref depth);
			ErrorHandler.CheckGlError();
		}

		~FrameBuffer() { Game.RunAfterTick(FinalizeInner); }

		int[] cv = new int[4];
		public void Bind()
		{
			// Cache viewport rect to restore when unbinding
			cv = ViewportRectangle();

			GL.Flush();
			ErrorHandler.CheckGlError();
			GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, framebuffer);
			ErrorHandler.CheckGlError();
			GL.Viewport(0, 0, size.Width, size.Height);
			ErrorHandler.CheckGlError();
			GL.ClearColor(0, 0, 0, 0);
			ErrorHandler.CheckGlError();
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			ErrorHandler.CheckGlError();
		}

		public void Unbind()
		{
			GL.Flush();
			ErrorHandler.CheckGlError();
			GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);
			ErrorHandler.CheckGlError();
			GL.Viewport(cv[0], cv[1], cv[2], cv[3]);
			ErrorHandler.CheckGlError();
		}

		public ITexture Texture { get { return texture; } }
	}
}
