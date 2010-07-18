#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using OpenRA.FileFormats.Graphics;
using Tao.OpenGl;

namespace OpenRA.GlRenderer
{
	public class IndexBuffer : IIndexBuffer, IDisposable
	{
		int buffer;

		public IndexBuffer(GraphicsDevice dev, int size)
		{
			Gl.glGenBuffers(1, out buffer);
			GraphicsDevice.CheckGlError();
		}

		public void SetData(ushort[] data)
		{
			Bind();
			Gl.glBufferData(Gl.GL_ELEMENT_ARRAY_BUFFER,
				new IntPtr(2 * data.Length), data, Gl.GL_DYNAMIC_DRAW);
			GraphicsDevice.CheckGlError();
		}

		public void Bind()
		{
			Gl.glBindBuffer(Gl.GL_ELEMENT_ARRAY_BUFFER, buffer);
			GraphicsDevice.CheckGlError();
		}

		bool disposed;
		public void Dispose()
		{
			if (disposed) return;
			GC.SuppressFinalize(this);
			Gl.glDeleteBuffers(1, ref buffer);
			GraphicsDevice.CheckGlError();
			disposed = true;
		}

		//~IndexBuffer() { Dispose(); }
	}
}
