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
using System.Runtime.InteropServices;
using OpenRA.FileFormats.Graphics;
using OpenTK;
using OpenTK.Compatibility;
using Tao.OpenGl;

namespace OpenRA.Renderer.SdlCommon
{
	public class VertexBuffer<T> : IVertexBuffer<T>
			where T : struct
	{
		static readonly int VertexSize = Marshal.SizeOf(typeof(T));
		int buffer;

		public VertexBuffer(int size)
		{
			Gl.glGenBuffersARB(1, out buffer);
			ErrorHandler.CheckGlError();
			Bind();
			Gl.glBufferDataARB(Gl.GL_ARRAY_BUFFER_ARB,
				new IntPtr(VertexSize * size),
				new T[size],
				Gl.GL_DYNAMIC_DRAW_ARB);
			ErrorHandler.CheckGlError();
		}

		public void SetData(T[] data, int length)
		{
			Bind();
			Gl.glBufferSubDataARB(Gl.GL_ARRAY_BUFFER_ARB,
				IntPtr.Zero,
				new IntPtr(VertexSize * length),
				data);
			ErrorHandler.CheckGlError();
		}

		public void Bind()
		{
			Gl.glBindBufferARB(Gl.GL_ARRAY_BUFFER_ARB, buffer);
			ErrorHandler.CheckGlError();
			Gl.glVertexPointer(3, Gl.GL_FLOAT, VertexSize, IntPtr.Zero);
			ErrorHandler.CheckGlError();
			Gl.glTexCoordPointer(4, Gl.GL_FLOAT, VertexSize, new IntPtr(12));
			ErrorHandler.CheckGlError();
		}

		void FinalizeInner() { Gl.glDeleteBuffersARB(1, ref buffer); }
		~VertexBuffer() { Game.RunAfterTick(FinalizeInner); }
	}
}
