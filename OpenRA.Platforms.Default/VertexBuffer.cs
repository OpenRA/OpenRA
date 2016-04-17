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

			var ptr = GCHandle.Alloc(new T[size], GCHandleType.Pinned);
			try
			{
				OpenGL.glBufferData(OpenGL.GL_ARRAY_BUFFER,
					new IntPtr(VertexSize * size),
					ptr.AddrOfPinnedObject(),
					OpenGL.GL_DYNAMIC_DRAW);
			}
			finally
			{
				ptr.Free();
			}

			OpenGL.CheckGLError();
		}

		public void SetData(T[] data, int length)
		{
			SetData(data, 0, length);
		}

		public void SetData(T[] data, int start, int length)
		{
			Bind();

			var ptr = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				OpenGL.glBufferSubData(OpenGL.GL_ARRAY_BUFFER,
					new IntPtr(VertexSize * start),
					new IntPtr(VertexSize * length),
					ptr.AddrOfPinnedObject());
			}
			finally
			{
				ptr.Free();
			}

			OpenGL.CheckGLError();
		}

		public void SetData(IntPtr data, int start, int length)
		{
			Bind();
			OpenGL.glBufferSubData(OpenGL.GL_ARRAY_BUFFER,
				new IntPtr(VertexSize * start),
				new IntPtr(VertexSize * length),
				data);
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
		}

		~VertexBuffer()
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
			OpenGL.glDeleteBuffers(1, ref buffer);
		}
	}
}
