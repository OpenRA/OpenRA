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
using OpenRA.FileFormats.Graphics;
using Tao.OpenGl;

using ElemType = System.UInt32;

namespace OpenRA.Renderer.Cg
{
	public class IndexBuffer : IIndexBuffer, IDisposable
	{
		int buffer;

		public IndexBuffer(GraphicsDevice dev, int size)
		{
			Gl.glGenBuffers(1, out buffer);
			GraphicsDevice.CheckGlError();
			Bind();
			Gl.glBufferData(Gl.GL_ELEMENT_ARRAY_BUFFER,
				new IntPtr(sizeof(ElemType) * size),
				new ElemType[ size ],
				Gl.GL_DYNAMIC_DRAW);
			GraphicsDevice.CheckGlError();
		}

		public void SetData(ElemType[] data, int length)
		{
			Bind();
			Gl.glBufferSubData(Gl.GL_ELEMENT_ARRAY_BUFFER,
				IntPtr.Zero,
				new IntPtr(sizeof(ElemType) * length),
				data);
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
