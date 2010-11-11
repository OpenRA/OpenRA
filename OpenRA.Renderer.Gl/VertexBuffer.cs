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
using System.Runtime.InteropServices;
using OpenRA.FileFormats.Graphics;
using Tao.OpenGl;

namespace OpenRA.Renderer.Glsl
{
	public class VertexBuffer<T> : IVertexBuffer<T>, IDisposable
			where T : struct
	{
		int buffer;

		public VertexBuffer(GraphicsDevice dev, int size)
		{
			Gl.glGenBuffers(1, out buffer);
			GraphicsDevice.CheckGlError();
			Bind();
			Gl.glBufferData(Gl.GL_ARRAY_BUFFER,
				new IntPtr(Marshal.SizeOf(typeof(T)) * size),
				new T[ size ],
				Gl.GL_DYNAMIC_DRAW);
			GraphicsDevice.CheckGlError();
		}

		public void SetData(T[] data, int length)
		{
			Bind();
			Gl.glBufferSubData(Gl.GL_ARRAY_BUFFER,
				IntPtr.Zero,
				new IntPtr(Marshal.SizeOf(typeof(T)) * length),
				data);
			GraphicsDevice.CheckGlError();
		}

		public void Bind()
		{
			Gl.glBindBuffer(Gl.GL_ARRAY_BUFFER, buffer);
			GraphicsDevice.CheckGlError();
			Gl.glVertexPointer(3, Gl.GL_FLOAT, Marshal.SizeOf(typeof(T)), IntPtr.Zero);
			GraphicsDevice.CheckGlError();
			Gl.glTexCoordPointer(4, Gl.GL_FLOAT, Marshal.SizeOf(typeof(T)), new IntPtr(12));
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

		//~VertexBuffer() { Dispose(); }
	}
}
