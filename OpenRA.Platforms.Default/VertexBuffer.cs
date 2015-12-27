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
			ErrorHandler.CheckGlError();
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

			ErrorHandler.CheckGlError();
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

			ErrorHandler.CheckGlError();
		}

		public void SetData(IntPtr data, int start, int length)
		{
			Bind();
			OpenGL.glBufferSubData(OpenGL.GL_ARRAY_BUFFER,
				new IntPtr(VertexSize * start),
				new IntPtr(VertexSize * length),
				data);
			ErrorHandler.CheckGlError();
		}

		public void Bind()
		{
			VerifyThreadAffinity();
			OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, buffer);
			ErrorHandler.CheckGlError();
			OpenGL.glVertexAttribPointer(Shader.VertexPosAttributeIndex, 3, OpenGL.GL_FLOAT, false, VertexSize, IntPtr.Zero);
			ErrorHandler.CheckGlError();
			OpenGL.glVertexAttribPointer(Shader.TexCoordAttributeIndex, 4, OpenGL.GL_FLOAT, false, VertexSize, new IntPtr(12));
			ErrorHandler.CheckGlError();
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
