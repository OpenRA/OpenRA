#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using System.IO;
using OpenRA.FileFormats.Graphics;
using OpenRA.Graphics;

[assembly: Renderer(typeof(OpenRA.Renderer.Null.NullGraphicsDevice))]

namespace OpenRA.Renderer.Null
{
	public class NullGraphicsDevice : IGraphicsDevice
	{
		public Size WindowSize { get; internal set; }

		public NullGraphicsDevice(int width, int height, WindowMode window, bool vsync)
		{
			WindowSize = new Size(width, height);
		}

		public void EnableScissor(int left, int top, int width, int height) { }
		public void DisableScissor() { }

		public void Clear(Color c) { }

		public void Present(IInputHandler ih)
		{
			Game.HasInputFocus = false;
			ih.ModifierKeys(Modifiers.None);
		}

		public void DrawIndexedPrimitives(PrimitiveType pt, Range<int> vertices, Range<int> indices) { }
		public void DrawIndexedPrimitives(PrimitiveType pt, int numVerts, int numPrimitives) { }

		public IVertexBuffer<Vertex> CreateVertexBuffer(int size) { return new NullVertexBuffer<Vertex>(); }
		public IIndexBuffer CreateIndexBuffer(int size) { return new NullIndexBuffer(); }
		public ITexture CreateTexture() { return new NullTexture(); }
		public ITexture CreateTexture(Bitmap bitmap) { return new NullTexture(); }
		public IShader CreateShader(Stream stream) { return new NullShader(); }
	}
}
