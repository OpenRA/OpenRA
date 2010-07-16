#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Runtime.InteropServices;
using OpenRA.FileFormats.Graphics;
using Tao.OpenGl;

namespace OpenRA.GlRenderer
{
	public class VertexBuffer<T> : IVertexBuffer<T>, IDisposable
			where T : struct
	{
		int buffer;

		public VertexBuffer(GraphicsDevice dev, int size)
		{
			Gl.glGenBuffers(1, out buffer);
			GraphicsDevice.CheckGlError();
		}

		public void SetData(T[] data)
		{
			Bind();
			Gl.glBufferData(Gl.GL_ARRAY_BUFFER,
				new IntPtr(Marshal.SizeOf(typeof(T)) * data.Length), data, Gl.GL_DYNAMIC_DRAW);
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
