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
using System.Drawing;
using OpenRA;
using OpenRA.Graphics;

[assembly: Renderer(typeof(OpenRA.Renderer.Null.DeviceFactory))]

namespace OpenRA.Renderer.Null
{
	public class DeviceFactory : IDeviceFactory
	{
		public IGraphicsDevice Create(Size size, WindowMode windowMode)
		{
			return new NullGraphicsDevice(size, windowMode);
		}
	}

	public sealed class NullGraphicsDevice : IGraphicsDevice
	{
		public Size WindowSize { get; internal set; }

		public NullGraphicsDevice(Size size, WindowMode window)
		{
			Console.WriteLine("Using Null renderer");
			WindowSize = size;
		}

		public void Dispose() { }

		public void EnableScissor(int left, int top, int width, int height) { }
		public void DisableScissor() { }

		public void EnableDepthBuffer() { }
		public void DisableDepthBuffer() { }

		public void SetBlendMode(BlendMode mode) { }

		public void Clear() { }
		public void Present() { }

		public void PumpInput(IInputHandler ih)
		{
			Game.HasInputFocus = false;
			ih.ModifierKeys(Modifiers.None);
		}

		public void DrawPrimitives(PrimitiveType pt, int firstVertex, int numVertices) { }
		public void SetLineWidth(float width) { }

		public IVertexBuffer<Vertex> CreateVertexBuffer(int size) { return new NullVertexBuffer<Vertex>(); }
		public ITexture CreateTexture() { return new NullTexture(); }
		public ITexture CreateTexture(Bitmap bitmap) { return new NullTexture(); }
		public IFrameBuffer CreateFrameBuffer(Size s) { return new NullFrameBuffer(); }
		public IShader CreateShader(string name) { return new NullShader(); }
	}

	public class NullShader : IShader
	{
		public void SetVec(string name, float x) { }
		public void SetVec(string name, float x, float y) { }
		public void SetVec(string name, float[] vec, int length) { }
		public void SetTexture(string param, ITexture texture) { }
		public void SetMatrix(string param, float[] mtx) { }
		public void Render(Action a) { }
	}

	public class NullTexture : ITexture
	{
		public void SetData(Bitmap bitmap) { }
		public void SetData(uint[,] colors) { }
		public void SetData(byte[] colors, int width, int height) { }
		public byte[] GetData() { return new byte[0]; }
		public Size Size { get { return new Size(0, 0); } }
	}

	public class NullFrameBuffer : IFrameBuffer
	{
		public void Bind() { }
		public void Unbind() { }
		public ITexture Texture { get { return new NullTexture(); } }
	}

	class NullVertexBuffer<T> : IVertexBuffer<T>
	{
		public void Bind() { }
		public void SetData(T[] vertices, int length) { }
	}
}
