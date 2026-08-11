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
using System.Runtime.InteropServices;
using OpenRA.Graphics;

namespace OpenRA.Platforms.Default
{
	sealed class VertexBuffer<T> : ThreadAffine, IDisposable, IVertexBuffer<T>
			where T : struct
	{
		static readonly int VertexSize = Marshal.SizeOf<T>();
		uint buffer;
		uint vao;
		bool disposed;

		public VertexBuffer(IShaderBindings bindings, int size)
		{
			OpenGL.glGenVertexArrays(1, out vao);
			OpenGL.CheckGLError();
			OpenGL.glBindVertexArray(vao);
			Sdl2GraphicsContext.ActiveVAO = vao;
			OpenGL.CheckGLError();

			OpenGL.glGenBuffers(1, out buffer);
			OpenGL.CheckGLError();
			OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, buffer);
			OpenGL.CheckGLError();

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

			var attributes = bindings.Attributes;
			for (ushort i = 0; i < attributes.Length; i++)
			{
				var attribute = attributes[i];
				OpenGL.glEnableVertexAttribArray(i);

				if (attribute.Type == ShaderVertexAttributeType.Float)
					OpenGL.glVertexAttribPointer(i, attribute.Components, OpenGL.GL_FLOAT, false, bindings.Stride, new IntPtr(attribute.Offset));
				else
					OpenGL.glVertexAttribIPointer(i, attribute.Components, (int)attribute.Type, bindings.Stride, new IntPtr(attribute.Offset));
			}

			OpenGL.CheckGLError();
		}

		public VertexBuffer(IShaderBindings bindings, T[] data, bool dynamic = true)
		{
			OpenGL.glGenVertexArrays(1, out vao);
			OpenGL.CheckGLError();
			OpenGL.glBindVertexArray(vao);
			Sdl2GraphicsContext.ActiveVAO = vao;
			OpenGL.CheckGLError();

			OpenGL.glGenBuffers(1, out buffer);
			OpenGL.CheckGLError();
			OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, buffer);
			OpenGL.CheckGLError();

			var ptr = GCHandle.Alloc(data, GCHandleType.Pinned);
			try
			{
				OpenGL.glBufferData(OpenGL.GL_ARRAY_BUFFER,
					new IntPtr(VertexSize * data.Length),
					ptr.AddrOfPinnedObject(),
					dynamic ? OpenGL.GL_DYNAMIC_DRAW : OpenGL.GL_STATIC_DRAW);
			}
			finally
			{
				ptr.Free();
			}

			OpenGL.CheckGLError();

			var attributes = bindings.Attributes;
			for (ushort i = 0; i < attributes.Length; i++)
			{
				var attribute = attributes[i];
				OpenGL.glEnableVertexAttribArray(i);

				if (attribute.Type == ShaderVertexAttributeType.Float)
					OpenGL.glVertexAttribPointer(i, attribute.Components, OpenGL.GL_FLOAT, false, bindings.Stride, new IntPtr(attribute.Offset));
				else
					OpenGL.glVertexAttribIPointer(i, attribute.Components, (int)attribute.Type, bindings.Stride, new IntPtr(attribute.Offset));
			}

			OpenGL.CheckGLError();
		}

		public void SetData(T[] data, int length)
		{
			SetData(data, 0, 0, length);
		}

		public void SetData(ref T[] data, int length)
		{
			SetData(data, 0, 0, length);
		}

		public void SetData(T[] data, int offset, int start, int length)
		{
			Bind();

			OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, buffer);
			OpenGL.CheckGLError();

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
			if (Sdl2GraphicsContext.ActiveVAO != vao)
			{
				OpenGL.glBindVertexArray(vao);
				Sdl2GraphicsContext.ActiveVAO = vao;
			}

			OpenGL.CheckGLError();
		}

		public void Dispose()
		{
			if (disposed)
				return;

			disposed = true;
			OpenGL.glDeleteBuffers(1, ref buffer);
			OpenGL.CheckGLError();

			OpenGL.glDeleteVertexArrays(1, ref vao);
			OpenGL.CheckGLError();
		}
	}
}
