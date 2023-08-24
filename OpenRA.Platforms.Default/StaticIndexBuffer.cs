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

namespace OpenRA.Platforms.Default
{
	sealed class StaticIndexBuffer : ThreadAffine, IDisposable, IIndexBuffer
	{
		const int UintSize = 4;
		uint buffer;
		bool disposed;

		public StaticIndexBuffer(uint[] indices)
		{
			OpenGL.glGenBuffers(1, out buffer);
			OpenGL.CheckGLError();
			Bind();

			var ptr = GCHandle.Alloc(indices, GCHandleType.Pinned);
			try
			{
				OpenGL.glBufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER,
					new IntPtr(UintSize * indices.Length),
					ptr.AddrOfPinnedObject(),
					OpenGL.GL_STATIC_DRAW);
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
			OpenGL.glBindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, buffer);
			OpenGL.CheckGLError();
		}

		public void Dispose()
		{
			if (disposed)
				return;

			disposed = true;
			OpenGL.glDeleteBuffers(1, ref buffer);
			OpenGL.CheckGLError();
		}
	}
}
