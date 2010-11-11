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
using System;

[assembly: Renderer(typeof(OpenRA.Renderer.Null.NullGraphicsDevice))]

namespace OpenRA.Renderer.Null
{
	public class NullGraphicsDevice : IGraphicsDevice
	{
		public Size WindowSize { get; internal set; }

		public NullGraphicsDevice(int width, int height, WindowMode window, bool vsync)
		{
			Console.WriteLine("Using Null renderer");
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
		public IShader CreateShader(string name) { return new NullShader(); }
	}
	
	public class NullIndexBuffer : IIndexBuffer
	{
		public void Bind() {}
		public void SetData(ushort[] indices, int length) {}
	}
	
	public class NullShader : IShader
	{
		public void SetValue(string name, float x, float y) { }
		public void SetValue(string param, ITexture texture) { }
		public void Commit() { }
		public void Render(Action a) { }
	}
	
	public class NullTexture : ITexture
	{
		public void SetData(Bitmap bitmap) { }
		public void SetData(uint[,] colors) { }
		public void SetData(byte[] colors, int width, int height) { }
	}
	
	class NullVertexBuffer<T> : IVertexBuffer<T>
	{
		public void Bind() { }
		public void SetData(T[] vertices, int length) { }
	}
}
