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
using OpenTK.Graphics.OpenGL;

namespace OpenRA.Platforms.Default
{
	sealed class VertexBuffer<T> : ThreadAffine, IVertexBuffer<T>
			where T : struct
	{
		static readonly int VertexSize = Marshal.SizeOf(typeof(T));
		int buffer;
		bool disposed;

		public VertexBuffer(int size)
		{
			GL.GenBuffers(1, out buffer);
			ErrorHandler.CheckGlError();
			Bind();
			GL.BufferData(BufferTarget.ArrayBuffer,
				new IntPtr(VertexSize * size),
				new T[size],
				BufferUsageHint.DynamicDraw);
			ErrorHandler.CheckGlError();
		}

		public void SetData(T[] data, int length)
		{
			SetData(data, 0, length);
		}

		public void SetData(T[] data, int start, int length)
		{
			Bind();
			GL.BufferSubData(BufferTarget.ArrayBuffer,
				new IntPtr(VertexSize * start),
				new IntPtr(VertexSize * length),
				data);
			ErrorHandler.CheckGlError();
		}

		public void SetData(IntPtr data, int start, int length)
		{
			Bind();
			GL.BufferSubData(BufferTarget.ArrayBuffer,
				new IntPtr(VertexSize * start),
				new IntPtr(VertexSize * length),
				data);
			ErrorHandler.CheckGlError();
		}

		public void Bind()
		{
			VerifyThreadAffinity();
			GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
			ErrorHandler.CheckGlError();
			GL.VertexAttribPointer(Shader.VertexPosAttributeIndex, 3, VertexAttribPointerType.Float, false, VertexSize, IntPtr.Zero);
			ErrorHandler.CheckGlError();
			GL.VertexAttribPointer(Shader.TexCoordAttributeIndex, 4, VertexAttribPointerType.Float, false, VertexSize, new IntPtr(12));
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
			GL.DeleteBuffers(1, ref buffer);
		}
	}
}
