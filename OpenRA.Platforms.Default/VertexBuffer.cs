#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Runtime.InteropServices;

namespace OpenRA.Platforms.Default
{
	sealed class VertexBuffer<T> : ThreadAffine, IVertexBuffer<T>
			where T : struct
	{
		static readonly int VertexSize = Marshal.SizeOf(typeof(T));
		uint buffer;
		bool disposed;

		public VertexBuffer(int size)
		{
			OpenGL.glGenBuffers(1, out buffer);
			OpenGL.CheckGLError();
			Bind();

			// Generates a buffer with uninitialized memory.
			OpenGL.glBufferData(OpenGL.GL_ARRAY_BUFFER,
					new IntPtr(VertexSize * size),
					IntPtr.Zero,
					OpenGL.GL_DYNAMIC_DRAW);
			OpenGL.CheckGLError();

			// We need to zero all the memory. Let's generate a smallish array and copy that over the whole buffer.
			var zeroedArrayElementSize = Math.Min(size, 2048);
			var ptr = GCHandle.Alloc(new T[zeroedArrayElementSize], GCHandleType.Pinned);
			try
			{
				for (var offset = 0; offset < size; offset += zeroedArrayElementSize)
				{
					var length = Math.Min(zeroedArrayElementSize, size - offset);
					OpenGL.glBufferSubData(OpenGL.GL_ARRAY_BUFFER,
						new IntPtr(VertexSize * offset),
						new IntPtr(VertexSize * length),
						ptr.AddrOfPinnedObject());
					OpenGL.CheckGLError();
				}
			}
			finally
			{
				ptr.Free();
			}
		}

		public void SetData(T[] data, int length)
		{
			SetData(data, 0, 0, length);
		}

		public void SetData(T[] data, int offset, int start, int length)
		{
			Bind();

			var ptr = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				OpenGL.glBufferSubData(OpenGL.GL_ARRAY_BUFFER,
					new IntPtr(VertexSize * start),
					new IntPtr(VertexSize * length),
					ptr.AddrOfPinnedObject() + VertexSize * offset);
			}
			finally
			{
				ptr.Free();
			}

			OpenGL.CheckGLError();
		}

		public void Bind()
		{
			VerifyThreadAffinity();
			OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, buffer);
			OpenGL.CheckGLError();
			OpenGL.glVertexAttribPointer(Shader.VertexPosAttributeIndex, 3, OpenGL.GL_FLOAT, false, VertexSize, IntPtr.Zero);
			OpenGL.CheckGLError();
			OpenGL.glVertexAttribPointer(Shader.TexCoordAttributeIndex, 4, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(12));
			OpenGL.CheckGLError();
			OpenGL.glVertexAttribPointer(Shader.TexMetadataAttributeIndex, 2, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(28));
			OpenGL.CheckGLError();
			OpenGL.glVertexAttribPointer(Shader.TintAttributeIndex, 4, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(36));
			OpenGL.CheckGLError();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (disposed)
				return;
			disposed = true;
			OpenGL.glDeleteBuffers(1, ref buffer);
			OpenGL.CheckGLError();
		}
	}
}
